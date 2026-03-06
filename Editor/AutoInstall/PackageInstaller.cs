using System;
using System.Collections.Generic;
using System.Linq;
using NovaFramework.Editor.Manifest;
using NovaFramework.Editor.Preference;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;

namespace NovaFramework.Editor.Installer
{
    /// <summary>
    /// 负责包的过滤、顺序安装和工程刷新
    /// </summary>
    internal static class PackageInstaller
    {
        private static int _completedPackageCount;
        private static int _totalPackageCount;

        public static List<string> PackagesToInstall { get; private set; } = new List<string>();

        /// <summary>
        /// 安装所选的包
        /// </summary>
        public static void InstallSelectedPackages(Action<string> addLog, Action<string> setError)
        {
            try
            {
                var selectedPackages = PackageManager.GetSelectedPackageObjects();
                addLog($"已选择 {selectedPackages.Count} 个包待安装");

                var packagesList = selectedPackages.Select(p => p.name).ToList();

                if (packagesList.Count > 0)
                {
                    InstallPackagesSequentially(packagesList, addLog);
                }
                else
                {
                    addLog("没有需要安装的包，跳过包安装步骤");
                }
            }
            catch (Exception ex)
            {
                setError($"安装插件包时出错: {ex.Message}");
                Logger.Error($"安装插件包时出错: {ex.Message}");
            }
        }

        private static void InstallPackagesSequentially(List<string> packagesList, Action<string> addLog)
        {
            PackagesToInstall = new List<string>();
            foreach (var packageName in packagesList)
            {
                if (packageName != Constants.COMMON_PACKAGE_NAME)
                {
                    PackageObject packageInfo = PackageManager.GetPackageObjectByName(packageName);
                    if (packageInfo != null)
                    {
                        PackagesToInstall.Add(packageName);
                    }
                }
            }

            if (PackagesToInstall.Count == 0)
            {
                addLog("没有需要安装的包，跳过包安装步骤");
                RefreshProject(addLog);
                return;
            }

            _totalPackageCount = PackagesToInstall.Count;
            _completedPackageCount = 0;

            addLog($"开始顺序安装 {_totalPackageCount} 个包...");

            foreach (var packageName in PackagesToInstall)
            {
                InstallSinglePackage(packageName, addLog);
            }

            DataManager.SavePersistedSelectedPackages(PackageManager.GetSelectedPackageNames());
            RefreshProject(addLog);
        }

        private static void InstallSinglePackage(string packageName, Action<string> addLog)
        {
            Logger.Info($"[AutoInstall] 开始安装包: {packageName}");
            try
            {
                PackageObject packageInfo = PackageManager.GetPackageObjectByName(packageName);
                if (packageInfo == null)
                {
                    Logger.Warn($"[AutoInstall] 未找到包信息: {packageName}");
                    _completedPackageCount++;
                    return;
                }

                addLog($"  正在配置: {packageName}");
                GitManager.InstallPackage(packageInfo);

                Logger.Info($"[AutoInstall] 包安装完成: {packageName}");
                addLog($"  完成: {packageName}");
                _completedPackageCount++;
            }
            catch (Exception ex)
            {
                addLog($"  警告: 配置包 {packageName} 时发生异常: {ex.Message}");
                Logger.Error($"[AutoInstall] 安装包 {packageName} 异常: {ex.Message}");
                _completedPackageCount++;
            }
        }

        /// <summary>
        /// 刷新工程并触发编译，通过 SessionState 等待域重载后继续
        /// </summary>
        private static void RefreshProject(Action<string> addLog)
        {
            addLog("正在刷新Package Manager配置...");
            Logger.Info("[AutoInstall] 开始刷新Package Manager配置");

            Client.Resolve();

            string completionMessage = $"成功安装并完成 {_totalPackageCount} 个包的配置，工程已刷新";
            addLog(completionMessage);
            Logger.Info($"[AutoInstall] {completionMessage}");

            // 标记待执行，包列表已通过 DataManager.SavePersistedSelectedPackages 持久化
            SessionState.SetBool(Constants.SESSION_KEY_PENDING, true);

            addLog("正在编译程序集，请稍候...");
            Logger.Info("[AutoInstall] 触发编译，等待域重载后执行 InstallationStep");

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            CompilationPipeline.RequestScriptCompilation();
        }
    }
}
