using System;
using System.Collections.Generic;
using NovaFramework.Editor.Manifest;
using NovaFramework.Editor.Preference;

namespace NovaFramework.Editor.Installer
{
    /// <summary>
    /// 负责通过反射查找并执行 InstallationStep
    /// </summary>
    internal static class InstallationStepExecutor
    {
        /// <summary>
        /// 执行所有已安装包中的 InstallationStep
        /// </summary>
        public static void ExecuteAllInstallMethod(List<string> packagesToInstall, Action<string> addLog)
        {
            addLog("开始执行安装步骤...");
            Logger.Info("[AutoInstall] 开始从包配置中查找并执行 InstallationStep");

            try
            {
                var installableModules = CollectInstallableModules(packagesToInstall);

                if (installableModules.Count == 0)
                {
                    addLog("未找到任何可安装的 InstallationStep");
                    Logger.Info("[AutoInstall] 未找到任何可安装的 InstallationStep");
                    return;
                }

                addLog($"找到 {installableModules.Count} 个安装步骤");
                Logger.Info($"[AutoInstall] 找到 {installableModules.Count} 个可安装的 InstallationStep");

                int completedCount = 0;
                int totalCount = installableModules.Count;

                foreach (var module in installableModules)
                {
                    try
                    {
                        var stepTypes = AssemblyUtils.FindAllTypesFromAssembly<InstallationStep>(module.name, true);

                        if (stepTypes.Count == 0)
                        {
                            Logger.Warn($"[AutoInstall] 在程序集 {module.name} 中未找到 InstallationStep 实现");
                            completedCount++;
                            continue;
                        }

                        foreach (var stepType in stepTypes)
                        {
                            ExecuteSingleInstallMethod(stepType, module.name, addLog, () =>
                            {
                                completedCount++;
                                if (completedCount >= totalCount)
                                {
                                    addLog("所有安装步骤执行完成");
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[AutoInstall] 执行程序集 {module.name} 的 InstallationStep 失败: {ex.Message}");
                        completedCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[AutoInstall] 执行 InstallationStep 时发生异常: {ex.Message}");
            }
        }

        private static List<ImportModuleObject> CollectInstallableModules(List<string> packagesToInstall)
        {
            var installableModules = new List<ImportModuleObject>();

            foreach (var packageName in packagesToInstall)
            {
                var packageInfo = PackageManager.GetPackageObjectByName(packageName);
                if (packageInfo?.installationObject?.importModules == null) continue;

                foreach (var module in packageInfo.installationObject.importModules)
                {
                    if (module.installable)
                    {
                        installableModules.Add(module);
                        Logger.Info($"[AutoInstall] 发现可安装模块: {module.name} (来自包: {packageName})");
                    }
                    else
                    {
                        Logger.Info($"[AutoInstall] 跳过不可安装模块: {module.name} (installable=false)");
                    }
                }
            }

            return installableModules;
        }

        private static void ExecuteSingleInstallMethod(Type stepType, string assemblyName, Action<string> addLog, Action onComplete)
        {
            try
            {
                var instance = Activator.CreateInstance(stepType) as InstallationStep;
                if (instance != null)
                {
                    Logger.Info($"[AutoInstall] 执行 InstallationStep: {stepType.Name} (来自程序集: {assemblyName})");
                    addLog($"  执行: {stepType.Name}");

                    instance.Install(
                        () => { addLog($"  {stepType.Name} 执行中..."); },
                        () =>
                        {
                            Logger.Info($"[AutoInstall] InstallationStep 完成: {stepType.Name}");
                            onComplete?.Invoke();
                        },
                        () =>
                        {
                            Logger.Error($"[AutoInstall] InstallationStep 出错: {stepType.Name}");
                            onComplete?.Invoke();
                        },
                        addLog);
                }
                else
                {
                    Logger.Warn($"[AutoInstall] 无法创建 InstallationStep 实例: {stepType.Name}");
                    onComplete?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[AutoInstall] 执行 InstallationStep 失败: {stepType.Name}, 错误: {ex.Message}");
                onComplete?.Invoke();
            }
        }
        
        /// <summary>
        /// 执行指定包中的 InstallationStep.Uninstall
        /// </summary>
        public static void ExecuteSingleUninstallMethod(string packageName)
        {
            var packageInfo = PackageManager.GetPackageObjectByName(packageName);
            if (packageInfo?.installationObject?.importModules == null) return;

            foreach (var module in packageInfo.installationObject.importModules)
            {
                if (!module.installable) continue;

                var stepTypes = AssemblyUtils.FindAllTypesFromAssembly<InstallationStep>(module.name, true);
                foreach (var stepType in stepTypes)
                {
                    try
                    {
                        var instance = Activator.CreateInstance(stepType) as InstallationStep;
                        if (instance != null)
                        {
                            Logger.Info($"[AutoInstall] 执行 Uninstall: {stepType.Name} (来自包: {packageName})");
                            instance.Uninstall(
                                () => { Logger.Info($"[AutoInstall] Uninstall 执行中: {stepType.Name}"); },
                                () => { Logger.Info($"[AutoInstall] Uninstall 完成: {stepType.Name}"); },
                                () => { Logger.Error($"[AutoInstall] Uninstall 出错: {stepType.Name}"); });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[AutoInstall] 执行 Uninstall 失败: {stepType.Name}, 错误: {ex.Message}");
                    }
                }
            }
        }

    }
}
