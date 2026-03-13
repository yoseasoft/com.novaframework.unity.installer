/// -------------------------------------------------------------------------------
/// Copyright (C) 2025 - 2026, Hainan Yuanyou Information Technology Co., Ltd. Guangzhou Branch
///
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
///
/// The above copyright notice and this permission notice shall be included in
/// all copies or substantial portions of the Software.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
/// THE SOFTWARE.
/// -------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using NovaFramework.Editor.Manifest;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    internal static class MainMenu
    {
        [MenuItem("Tools/自动安装 _F8", priority = 2, validate = true)]
        private static bool ValidateAutoInstall()
        {
            // 检查是否已经安装过了，如果已安装则不显示菜单项
            return !AutoInstallManager.IsAlreadyInstalled();
        }
        
        [MenuItem("Tools/自动安装 _F8", priority = 2)]
        public static void ShowAutoInstall()
        {
            AutoInstallManager.StartAutoInstall();
        }
        
        [MenuItem("Tools/Package安装中心 _F1", priority = 3)]
        public static void ShowPackageInstallCenter()
        {
            PackageInstallWindow.ShowWindow();
        }
        
        [MenuItem("Tools/Package一键安装", priority = 3)]
        public static void OneClickInstallPackages()
        {
            // 读取 selected_packages.txt
            string txtPath = Path.Combine(Application.dataPath, "../NovaFrameworkData/selected_packages.txt").Replace("\\", "/");
            if (!File.Exists(txtPath))
            {
                EditorUtility.DisplayDialog("提示", $"未找到 selected_packages.txt，请先在Package安装中心生成包列表配置 {txtPath}", "确定");
                return;
            }

            string content = File.ReadAllText(txtPath).Trim();
            if (string.IsNullOrEmpty(content))
            {
                EditorUtility.DisplayDialog("提示", "selected_packages.txt 为空，请先在Package安装中心生成包列表配置", "确定");
                return;
            }

            // 解析包名列表，过滤掉 common 和 installer
            var packageNames = content.Split('\n')
                .Select(line => line.Trim())
                .Where(name => !string.IsNullOrEmpty(name)
                               && name != Constants.LocalPackageNameOfCommonModule
                               && name != Constants.LocalPackageNameOfInstallerModule)
                .ToList();

            if (packageNames.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有需要安装的模块", "确定");
                return;
            }

            // 查找对应的 PackageObject
            var packagesToInstall = new List<PackageObject>();
            foreach (var name in packageNames)
            {
                PackageObject pkg = PackageManager.GetPackageObjectByName(name);
                if (pkg != null)
                {
                    packagesToInstall.Add(pkg);
                }
                else
                {
                    Logger.Warn($"[一键安装] 未在清单中找到包: {name}，跳过");
                }
            }

            if (packagesToInstall.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "txt中的包均未在清单中找到", "确定");
                return;
            }

            string displayNames = string.Join("\n", packagesToInstall.Select(p => p.title));
            if (!EditorUtility.DisplayDialog("确认一键安装",
                    $"将要安装以下 {packagesToInstall.Count} 个模块:\n{displayNames}\n\n模块将下载到 NovaFrameworkData/framework_repo 并配置到 manifest.json",
                    "确定", "取消"))
            {
                return;
            }

            // 逐个安装
            var installedNames = new List<string>();
            foreach (var package in packagesToInstall)
            {
                Logger.Info($"[一键安装] 开始安装模块: {package.name}");
                GitManager.InstallPackage(package);
                installedNames.Add(package.name);
            }

            Logger.Info($"[一键安装] 安装完成，共 {installedNames.Count} 个模块，正在刷新...");

            UnityEditor.PackageManager.Client.Resolve();
            // AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            // CompilationPipeline.RequestScriptCompilation();
        }
        
        [MenuItem("Tools/配置中心 _F3", priority = 4)]
        public static void ShowConfigurationCenter()
        {
            ConfigurationCenterWindow.ShowWindow();
        }
        
        [MenuItem("Tools/检查更新", priority = 5)]
        public static void ShowUpdateChecker()
        {
            GitManager.UpdateSinglePackage(Constants.LocalPackageNameOfCommonModule);
            GitManager.UpdateSinglePackage(Constants.LocalPackageNameOfInstallerModule);
        }
        
        [MenuItem("Tools/验证环境", priority = 6)]
        public static void ValidateEnvironment()
        {
            EnvironmentValidator.ShowValidationResult();
        }
        
        
    }
}