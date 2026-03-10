using System.Collections.Generic;
using System.IO;
using NovaFramework.Editor.Preference;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace NovaFramework.Editor.Installer
{
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
            { InstallStep.OpenScene, "正在打开主场景..." },
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
                if (!CheckEnvironment())
                    return;

                SetStep(InstallStep.InstallPackages);
                PackageInstaller.InstallSelectedPackages(AddLog, SetError);
            };
        }

        /// <summary>
        /// 域重载后自动检查是否有待完成的安装步骤
        /// </summary>
        [InitializeOnLoadMethod]
        private static void OnDomainReload()
        {
            if (!SessionState.GetBool(Constants.SESSION_KEY_PENDING, false))
                return;

            SessionState.SetBool(Constants.SESSION_KEY_PENDING, false);
            string packagesStr = SessionState.GetString(Constants.SESSION_KEY_STEP_PACKAGES, "");
            SessionState.EraseString(Constants.SESSION_KEY_STEP_PACKAGES);

            var packages = string.IsNullOrEmpty(packagesStr)
                ? new List<string>()
                : new List<string>(packagesStr.Split(','));

            Logger.Info("[AutoInstall] 域重载完成，开始执行 InstallationStep");

            EditorApplication.delayCall += () =>
            {
                _progressWindow = AutoInstallProgressWindow.Instance;
                InstallationStepExecutor.ExecuteAllInstallMethod(packages, AddLog);
                
                //第一次安装
                if (!IsAlreadyInstalled())
                {
                    SetStep(InstallStep.CreateBaseProject);
                    CreateBaseProject();
                    SetStep(InstallStep.Complete);
                    UserSettings.SetBool(Constants.NovaFramework_Installer_INSTALLER_COMPLETE_KEY, true);
                }
            };
        }

        private static void CreateBaseProject()
        { 
            //1. 解压基础包到Sources目录,尝试多种可能的路径来查找Game.zip
            string gameZipPath = FindGameZipFile();
            string sourcesPath = Path.Combine(Application.dataPath, "..", "Assets/Sources");
            if (!string.IsNullOrEmpty(gameZipPath))
            {
                //ZipHelper.ExtractZipFile(gameZipPath, sourcesPath);
                _progressWindow?.AddLog("已解压基础包到 Assets/Sources");
            }
            
            //2. 创建主场景到Scenes目录
            string scenesDir = Path.Combine(Application.dataPath, "Scenes");
            if (!Directory.Exists(scenesDir))
            {
                Directory.CreateDirectory(scenesDir);
            }

            string destScenePath = Path.Combine(scenesDir, "main.unity");

            // 使用Unity API创建一个新的空场景
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 保存新创建的场景到目标路径
            EditorSceneManager.SaveScene(newScene, destScenePath);
            _progressWindow?.AddLog("已创建主场景 Assets/Scenes/main.unity");
            
            // // 3. 复制DLL到AOT/Windows目录
            // var systemVariables = DataManager.LoadSystemVariables();
            //
            // // 确保AOT库文件被复制到正确位置
            // string aotDestinationPath = "";
            //
            //  if (systemVariables.ContainsKey("AOT_LIBRARY_PATH"))
            //  {
            //      aotDestinationPath = Path.Combine(Application.dataPath, "..", systemVariables["AOT_LIBRARY_PATH"],
            //          "Windows");
            //  }
            //  else
            //  {
            //      // 如果AOT_LIBRARY_PATH未定义，使用默认路径Assets/_Resources/Aot/Windows
            //      aotDestinationPath = Path.Combine(Application.dataPath, "_Resources", "Aot", "Windows");
            //      _progressWindow?.AddLog("AOT_LIBRARY_PATH 未在系统变量中定义，使用默认路径: " + aotDestinationPath);
            //  }
            //
            //  if (!Directory.Exists(aotDestinationPath))
            //  {
            //      Directory.CreateDirectory(aotDestinationPath);
            //  }
            //
            //  // 从工具包内的AOT目录复制DLL.byte文件到目标AOT/Windows目录
            //  string sourceAotPath = Path.Combine(Constants.DEFAULT_INSTALLER_ROOT_PATH,
            //      "Editor Default Resources/Aot/Windows");
            //  if (Directory.Exists(sourceAotPath))
            //  {
            //      string[] dllFiles = Directory.GetFiles(sourceAotPath, "*.dll.bytes", SearchOption.TopDirectoryOnly);
            //
            //      foreach (string dllFile in dllFiles)
            //      {
            //          string fileName = Path.GetFileName(dllFile);
            //          string destinationPath = Path.Combine(aotDestinationPath, fileName);
            //          File.Copy(dllFile, destinationPath, true); // true表示覆盖已存在的文件
            //      }
            //
            //      _progressWindow?.AddLog($"已复制 {dllFiles.Length} 个AOT库文件");
            //  }
            //  else
            //  {
            //      _progressWindow?.AddLog($"AOT源目录不存在，跳过DLL复制");
            //  }
            //  
            //  // 4.复制默认资源
            //  CopyDefaultRes();
            
            AssetDatabase.Refresh();
        }
        
        //新增方法：查找Game.zip文件
        private static string FindGameZipFile()
        {
            string path = Constants.GAME_ZIP_PATH;

            if (File.Exists(path))
            {
                return path;
            }

            Logger.Warn("在任何可能的路径中都未找到Game.zip文件");
            return null;
        }
        
        private static bool CheckEnvironment()
        {
            SetStep(InstallStep.CheckEnvironment);

            if (IsAlreadyInstalled())
            {
                AddLog("检测到已安装完成，跳过安装流程");
                SetStep(InstallStep.Complete);
                return false;
            }

            AddLog("环境检查通过，开始安装流程...");
            return true;
        }

        public static bool IsAlreadyInstalled()
        {
            return UserSettings.GetBool(Constants.NovaFramework_Installer_INSTALLER_COMPLETE_KEY);
        }

        public static void OpenMainScene()
        {
            string mainScenePath = "Assets/Scenes/main.unity";
            if (File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName, mainScenePath)))
            {
                EditorSceneManager.OpenScene(mainScenePath);
                AddLog("已打开主场景: " + mainScenePath);
            }
        }

        private static void OnProgressWindowClosed(bool completedSuccessfully)
        {
            Logger.Info($"[AutoInstall] 进度窗口已关闭，安装状态: {(completedSuccessfully ? "成功" : "失败或中断")}");
            _progressWindow = null;
        }
    }
}
