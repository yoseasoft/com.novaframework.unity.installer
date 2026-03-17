using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;

namespace NovaFramework.Editor.Installer
{
    internal enum InstallStep
    {
        None,
        CheckEnvironment,    // 检查环境
        InstallPackages,     // 安装包
        Complete             // 完成
    }
    
    /// <summary>
    /// 自动安装流程入口，负责流程编排和域重载恢复
    /// 包安装逻辑见 PackageInstaller，InstallationStep 执行见 InstallationStepExecutor
    /// </summary>
    internal static class AutoInstallManager
    {
        private static AutoInstallProgressWindow _progressWindow;

        private static readonly Dictionary<InstallStep, string> StepDescriptions = new Dictionary<InstallStep, string>
        {
            { InstallStep.None, "准备中..." },
            { InstallStep.CheckEnvironment, "正在检查环境..." },
            { InstallStep.InstallPackages, "正在安装依赖包..." },
            { InstallStep.Complete, "安装完成！" }
        };

        public static string GetStepDescription(InstallStep step)
        {
            return StepDescriptions.ContainsKey(step) ? StepDescriptions[step] : step.ToString();
        }

        public static void SetStep(InstallStep step, string detail = "")
        {
            int totalSteps = StepDescriptions.Count;
            _progressWindow?.SetStep(step, (int)step, totalSteps, GetStepDescription(step), detail);
        }

        public static void AddLog(string message)
        {
            _progressWindow?.AddLog(message);
        }

        public static void SetError(string errorMessage)
        {
            _progressWindow?.SetError(errorMessage);
        }

        public static void StartAutoInstall()
        {
            Logger.Info("[AutoInstall] 开始自动安装流程");
            _progressWindow = AutoInstallProgressWindow.ShowWindow();
            _progressWindow.OnClosed = OnProgressWindowClosed;
            _progressWindow.AddLog("正在初始化安装环境...");

            // 延迟到下一帧执行，让窗口先渲染出来
            EditorApplication.delayCall += () =>
            {
                SetStep(InstallStep.InstallPackages);
                PackageInstaller.InstallSelectedPackages(AddLog, SetError);
            };
        }

        public static bool IsAlreadyInstalled()
        {
            string asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(Constants.InstallerEditorAssemblyName);
            if (string.IsNullOrEmpty(asmdefPath))
            {
                Logger.Warn($"程序集 {Constants.InstallerEditorAssemblyName} 的asmdef路径为空");
                return false;
            }

            return true;
        }

        private static void OnProgressWindowClosed(bool completedSuccessfully)
        {
            Logger.Info($"[AutoInstall] 进度窗口已关闭，安装状态: {(completedSuccessfully ? "成功" : "失败或中断")}");
            _progressWindow = null;
        }
    }
}
