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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;

namespace NovaFramework.Editor.Installer
{
    public class AutoInstallManager
    {
        public class InstallProgress
        {
            public string CurrentStep { get; set; }
            public float Progress { get; set; }
            public bool IsCompleted { get; set; }
            public string StatusMessage { get; set; }
        }
        
        public delegate void ProgressCallback(InstallProgress progress);
        
        public static void StartAutoInstall(ProgressCallback progressCallback = null)
        {
            // 检查是否已经安装过了
            if (IsAlreadyInstalled())
            {
                EditorUtility.DisplayDialog(
                    "自动安装", 
                    "检测到框架已经安装过了，无需重复安装。", 
                    "确定"
                );
                
                Debug.Log("检测到框架已经安装过了，无需重复安装");
                return;
            }
            
            Debug.Log("开始自动安装流程...");
            InstallRequiredPackages(progressCallback);
        }
        
        // 检查是否已经安装过了
        public static bool IsAlreadyInstalled()
        {
            // 使用环境验证工具来判断是否已安装
            var validation = EnvironmentValidator.ValidateEnvironment();
            
            // 如果环境验证通过，说明框架已安装
            bool installed = validation.IsValid;
            
            if (installed)
            {
                Debug.Log("检测到框架环境已安装");
            }
            else
            {
                Debug.Log("检测到框架尚未安装");
            }
            
            return installed;
        }
        
        private static void InstallRequiredPackages(ProgressCallback progressCallback)
        {
            Debug.Log("开始解析repo_manifest.xml并准备安装必需插件包");
            progressCallback?.Invoke(new InstallProgress 
            { 
                CurrentStep = "第一步：安装插件包", 
                Progress = 0.1f, 
                StatusMessage = "正在解析repo_manifest.xml配置表..." 
            });
            
            try
            {
                // 使用PackageManager加载所有包信息
                PackageManager.LoadData();
                var allPackages = PackageManager.AllPackages;
                
                if (allPackages == null || allPackages.Count == 0)
                {
                    throw new Exception("无法加载包信息");
                }
                
                Debug.Log($"解析到 {allPackages.Count} 个总包");
                
                // 获取已选择的包（包括必需包及其依赖）
                var selectedPackages = PackageManager.GetSelectedPackages();
                
                Debug.Log($"找到 {selectedPackages.Count} 个需要安装的包");
                
                // 更新框架设置
                var frameworkSetting = DataManager.LoadFrameworkSetting();
                frameworkSetting.selectedPackages.Clear();
                
                foreach (var package in selectedPackages)
                {
                    frameworkSetting.selectedPackages.Add(package);
                    Debug.Log($"将包 {package.name} 添加到框架设置");
                }
                
                DataManager.SaveFrameworkSetting(frameworkSetting);
                Debug.Log("保存框架设置完成");
                
                progressCallback?.Invoke(new InstallProgress 
                { 
                    CurrentStep = "第一步：安装插件包", 
                    Progress = 0.3f, 
                    StatusMessage = $"已找到 {selectedPackages.Count} 个插件包需要安装..." 
                });
                
                // 实际安装包 - 异步方式
                var packagesList = selectedPackages.Select(p => p.name).ToList();
                
                if (packagesList.Count > 0)
                {
                    InstallPackagesSequentially(packagesList, 0, progressCallback);
                }
                else
                {
                    progressCallback?.Invoke(new InstallProgress 
                    { 
                        CurrentStep = "第一步：安装插件包", 
                        Progress = 0.5f, 
                        StatusMessage = "没有需要安装的插件包" 
                    });
                    Debug.Log("没有需要安装的包，直接进入下一步");
                    CreateDirectories(progressCallback);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"安装插件包时出错: {ex.Message}");
                progressCallback?.Invoke(new InstallProgress 
                { 
                    CurrentStep = "错误", 
                    Progress = 0f, 
                    StatusMessage = $"安装插件包时出错: {ex.Message}",
                    IsCompleted = true
                });
            }
        }
        
        // 异步顺序安装包列表
        private static void InstallPackagesSequentially(List<string> packagesList, int currentIndex, ProgressCallback progressCallback)
        {
            if (currentIndex >= packagesList.Count)
            {
                Debug.Log($"所有 {packagesList.Count} 个包已添加到manifest.json，统一触发Resolve操作");
                
                // 统一调用Resolve，让Unity自动安装所有配置的包
                UnityEditor.PackageManager.Client.Resolve();
                
                Debug.Log("Resolve操作已触发，立即进入下一步");
                CreateDirectories(progressCallback);
                
                return;
            }
            
            var packageName = packagesList[currentIndex];
            Debug.Log($"正在配置第 {currentIndex + 1}/{packagesList.Count} 个包: {packageName}");
            
            progressCallback?.Invoke(new InstallProgress 
            { 
                CurrentStep = "第一步：安装插件包", 
                Progress = 0.3f + (0.2f * (float)currentIndex / packagesList.Count),
                StatusMessage = $"正在配置插件包: {packageName} ({currentIndex + 1}/{packagesList.Count})" 
            });
            
            // 添加当前包到配置，完成后配置下一个
            try
            {
                Debug.Log($"开始安装包: {packageName}");
                GitManager.InstallPackage(packageName, () =>
                {
                    Debug.Log($"包 {packageName} 已配置到manifest.json");
                    InstallPackagesSequentially(packagesList, currentIndex + 1, progressCallback);
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"配置包 {packageName} 时发生异常: {ex.Message}");
                // 即使当前包配置失败，也继续配置下一个包
                EditorApplication.delayCall += () =>
                {
                    InstallPackagesSequentially(packagesList, currentIndex + 1, progressCallback);
                };
            }
        }
        

        
        internal static void CreateDirectories(ProgressCallback progressCallback)
        {
            progressCallback?.Invoke(new InstallProgress 
            { 
                CurrentStep = "第二步：创建目录", 
                Progress = 0.5f, 
                StatusMessage = "正在创建环境目录结构..." 
            });
            
            try
            {
                // 获取默认系统变量配置
                var systemVariables = DataManager.GetDefaultSystemVariables();
                
                // 创建所有环境目录
                foreach (var kvp in systemVariables)
                {
                    string fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, kvp.Value);
                    if (!Directory.Exists(fullPath))
                    {
                        Directory.CreateDirectory(fullPath);
                        Debug.Log($"创建目录: {fullPath}");
                    }
                    else
                    {
                        Debug.Log($"目录已存在: {fullPath}");
                    }
                }
                
                // 确保主Resources目录存在
                string resourcesPath = Path.Combine(Application.dataPath, "_Resources");
                if (!Directory.Exists(resourcesPath))
                {
                    Directory.CreateDirectory(resourcesPath);
                    Debug.Log($"创建Resources目录: {resourcesPath}");
                }
                
                // 特别确保AOT和Code子目录存在
                string aotPath = Path.Combine(resourcesPath, "Aot");
                if (!Directory.Exists(aotPath))
                {
                    Directory.CreateDirectory(aotPath);
                    Debug.Log($"创建AOT目录: {aotPath}");
                }
                
                string codePath = Path.Combine(resourcesPath, "Code");
                if (!Directory.Exists(codePath))
                {
                    Directory.CreateDirectory(codePath);
                    Debug.Log($"创建Code目录: {codePath}");
                }
                
                // 保存系统变量配置
                DataManager.SaveSystemVariables(systemVariables);
                
                progressCallback?.Invoke(new InstallProgress 
                { 
                    CurrentStep = "第二步：创建目录", 
                    Progress = 0.6f, 
                    StatusMessage = "环境目录创建完成" 
                });
                
                // 继续下一步
                ConfigureAssemblies(progressCallback);
            }
            catch (Exception ex)
            {
                Debug.LogError($"创建环境目录时出错: {ex.Message}");
                progressCallback?.Invoke(new InstallProgress 
                { 
                    CurrentStep = "错误", 
                    Progress = 0.5f, 
                    StatusMessage = $"创建环境目录时出错: {ex.Message}",
                    IsCompleted = true
                });
            }
        }
        
        private static void ConfigureAssemblies(ProgressCallback progressCallback)
        {
            progressCallback?.Invoke(new InstallProgress 
            { 
                CurrentStep = "第三步：配置程序集", 
                Progress = 0.6f, 
                StatusMessage = "正在解析已安装插件的程序集配置..." 
            });
            
            try
            {
                // 从已安装的包中提取程序集配置
                var frameworkSetting = DataManager.LoadFrameworkSetting();
                var allPackages = PackageManager.AllPackages;
                var assemblyConfigs = new List<AssemblyDefinitionConfig>();
                
                // 查找已安装包中有assembly-definition配置的包
                foreach (var package in allPackages)
                {
                    if (frameworkSetting.selectedPackages.Exists(p => p.name == package.name) && 
                        package.assemblyDefinitionInfo != null && 
                        !string.IsNullOrEmpty(package.assemblyDefinitionInfo.name))
                    {
                        var config = new AssemblyDefinitionConfig
                        {
                            name = package.assemblyDefinitionInfo.name,
                            order = package.assemblyDefinitionInfo.order,
                            tagNames = new List<string>(package.assemblyDefinitionInfo.loadableStrategies)
                        };
                        
                        assemblyConfigs.Add(config);
                    }
                }
                
                // 保存程序集配置
                DataManager.SaveAssemblyConfig(assemblyConfigs);
                
                progressCallback?.Invoke(new InstallProgress 
                { 
                    CurrentStep = "第三步：配置程序集", 
                    Progress = 0.7f, 
                    StatusMessage = "程序集配置完成" 
                });
                
                // 继续下一步
                InstallBasePackage(progressCallback);
            }
            catch (Exception ex)
            {
                Debug.LogError($"配置程序集时出错: {ex.Message}");
                progressCallback?.Invoke(new InstallProgress 
                { 
                    CurrentStep = "错误", 
                    Progress = 0.6f, 
                    StatusMessage = $"配置程序集时出错: {ex.Message}",
                    IsCompleted = true
                });
            }
        }
        
        private static void InstallBasePackage(ProgressCallback progressCallback)
        {
            Debug.Log("开始安装基础包...");
            progressCallback?.Invoke(new InstallProgress 
            { 
                CurrentStep = "第四步：安装基础包", 
                Progress = 0.7f, 
                StatusMessage = "正在安装基础包..." 
            });
            
            try
            {
                // 1. 解压基础包到Sources目录
                string sourcesPath = Path.Combine(Application.dataPath, "..", "Assets", "Sources");
                if (!Directory.Exists(sourcesPath))
                {
                    Directory.CreateDirectory(sourcesPath);
                    Debug.Log($"创建Sources目录: {sourcesPath}");
                }
                Debug.Log("开始解压UI包到Sources目录...");
                // 尝试多种可能的路径来查找UI.zip
                string uiZipPath = FindUIZipFile();
                if (!string.IsNullOrEmpty(uiZipPath))
                {
                    ZipHelper.ExtractZipFile(uiZipPath, sourcesPath);
                    Debug.Log("UI包解压完成");
                }
                else
                {
                    Debug.LogWarning("未找到UI.zip文件，跳过基础包解压步骤");
                }
                
                progressCallback?.Invoke(new InstallProgress 
                { 
                    CurrentStep = "第四步：安装基础包", 
                    Progress = 0.75f, 
                    StatusMessage = "已解压UI包到Sources目录" 
                });
                
                // 2. 创建主场景到Scenes目录
                string scenesDir = Path.Combine(Application.dataPath, "Scenes");
                if (!Directory.Exists(scenesDir))
                {
                    Directory.CreateDirectory(scenesDir);
                    Debug.Log($"创建Scenes目录: {scenesDir}");
                }
                
                string destScenePath = Path.Combine(scenesDir, "main.unity");
                
                // 使用Unity API创建一个新的空场景
                Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                
                // 保存新创建的场景到目标路径
                EditorSceneManager.SaveScene(newScene, destScenePath);
                
                Debug.Log("成功创建并保存空主场景到Scenes目录");
                
                progressCallback?.Invoke(new InstallProgress 
                { 
                    CurrentStep = "第四步：安装基础包", 
                    Progress = 0.8f, 
                    StatusMessage = "已处理主场景到Scenes目录" 
                });
                
                // 3. 复制DLL到AOT/Windows目录
                var systemVariables = DataManager.LoadSystemVariables();
                Debug.Log($"加载了 {systemVariables.Count} 个系统变量");
                
                if (systemVariables.ContainsKey("AOT_LIBRARY_PATH"))
                {
                    string aotPath = Path.Combine(Application.dataPath, "..", systemVariables["AOT_LIBRARY_PATH"], "Windows");
                    if (!Directory.Exists(aotPath))
                    {
                        Directory.CreateDirectory(aotPath);
                        Debug.Log($"创建AOT Windows目录: {aotPath}");
                    }
                    
                    // 从工具包内的AOT目录复制DLL到配置的AOT/Windows目录
                    string sourceAotPath = Path.Combine(Application.dataPath, "Editor/FrameworkInstaller/Aot/Windows");
                    if (Directory.Exists(sourceAotPath))
                    {
                        string[] dllFiles = Directory.GetFiles(sourceAotPath, "*.*", SearchOption.TopDirectoryOnly);
                        Debug.Log($"找到 {dllFiles.Length} 个DLL文件需要复制");
                        
                        foreach (string dllFile in dllFiles)
                        {
                            string fileName = Path.GetFileName(dllFile);
                            string destinationPath = Path.Combine(aotPath, fileName);
                            File.Copy(dllFile, destinationPath, true); // true表示覆盖已存在的文件
                            Debug.Log($"复制DLL文件: {fileName}");
                        }
                        Debug.Log("DLL文件复制完成");
                    }
                    else
                    {
                        Debug.LogWarning($"AOT源目录不存在: {sourceAotPath}，跳过DLL复制");
                    }
                }
                else
                {
                    Debug.LogWarning("AOT_LIBRARY_PATH 未在系统变量中定义，跳过DLL复制");
                }
               
                progressCallback?.Invoke(new InstallProgress 
                { 
                    CurrentStep = "第四步：安装基础包", 
                    Progress = 0.9f, 
                    StatusMessage = "已处理DLL到AOT目录" 
                });
                
                // 生成Nova框架配置
                GenerateNovaFrameworkConfig(progressCallback);
            }
            catch (Exception ex)
            {
                Debug.LogError($"安装基础包时出错: {ex.Message}");
                Debug.LogError($"堆栈跟踪: {ex.StackTrace}");
                progressCallback?.Invoke(new InstallProgress 
                { 
                    CurrentStep = "错误", 
                    Progress = 0.7f, 
                    StatusMessage = $"安装基础包时出错: {ex.Message}",
                    IsCompleted = true
                });
            }
        }
        
        // 新增方法：查找Game.zip文件
        public static string FindUIZipFile()
        {
            string path = Constants.GAME_ZIP_PATH;
            
            if (File.Exists(path))
            {
                Debug.Log($"找到Game.zip文件: {path}");
                return path;
            }
           
            Debug.LogWarning("在任何可能的路径中都未找到Game.zip文件");
            return null;
        }
        
        private static void GenerateNovaFrameworkConfig(ProgressCallback progressCallback)
        {
            progressCallback?.Invoke(new InstallProgress 
            { 
                CurrentStep = "第四步：生成框架配置", 
                Progress = 0.9f, 
                StatusMessage = "正在生成Nova框架配置文件..." 
            });
            
            try
            {
                // 确保Resources目录存在
                string resourcesPath = Path.Combine(Application.dataPath, "Resources");
                if (!Directory.Exists(resourcesPath))
                {
                    Directory.CreateDirectory(resourcesPath);
                    Debug.Log($"创建Resources目录: {resourcesPath}");
                }
                
                // 生成默认程序集配置（如果不存在）
                GenerateDefaultAssemblyConfigs();
                
                progressCallback?.Invoke(new InstallProgress 
                { 
                    CurrentStep = "第四步：生成框架配置", 
                    Progress = 0.95f, 
                    StatusMessage = "框架配置生成完成" 
                });
                
                // 完成安装
                CompleteInstallation(progressCallback);
            }
            catch (Exception ex)
            {
                Debug.LogError($"生成框架配置时出错: {ex.Message}");
                Debug.LogError($"堆栈跟踪: {ex.StackTrace}");
                progressCallback?.Invoke(new InstallProgress 
                { 
                    CurrentStep = "错误", 
                    Progress = 0.9f, 
                    StatusMessage = $"生成框架配置时出错: {ex.Message}",
                    IsCompleted = true
                });
            }
        }
        
        // 生成默认程序集配置
        private static void GenerateDefaultAssemblyConfigs()
        {
            var assemblyConfigs = DataManager.LoadAssemblyConfig();
            
            // 检查是否已经有配置，如果没有则创建默认配置
            if (assemblyConfigs.Count == 0)
            {
                Debug.Log("未找到程序集配置，创建默认配置...");
                
                // 添加默认的Nova框架程序集配置
                var defaultConfigs = new List<AssemblyDefinitionConfig>
                {
                    new AssemblyDefinitionConfig
                    {
                        name = "Nova.Library",
                        order = 1,
                        tagNames = new List<string> { "Core", "Library" }
                    },
                    new AssemblyDefinitionConfig
                    {
                        name = "Nova.Engine",
                        order = 2,  // 根据规范设置为2
                        tagNames = new List<string> { "Core", "Engine" }
                    },
                    new AssemblyDefinitionConfig
                    {
                        name = "Nova.Basic",
                        order = 3,  // 根据规范设置为3
                        tagNames = new List<string> { "Core", "Basic" }
                    },
                    new AssemblyDefinitionConfig
                    {
                        name = "Nova.Import",
                        order = 4,  // 根据规范设置为4
                        tagNames = new List<string> { "Core", "Import" }
                    }
                };
                
                DataManager.SaveAssemblyConfig(defaultConfigs);
                Debug.Log("已创建默认的Nova框架程序集配置");
            }
            else
            {
                Debug.Log($"已存在 {assemblyConfigs.Count} 个程序集配置，无需创建默认配置");
            }
        }
        
        private static void CompleteInstallation(ProgressCallback progressCallback)
        {
            Debug.Log("开始完成安装流程...");
            progressCallback?.Invoke(new InstallProgress 
            { 
                CurrentStep = "第五步：完成安装", 
                Progress = 0.98f, 
                StatusMessage = "正在复制配置文件到Resources目录..."
            });
            
            // 复制Configs目录下所有文件到Assets/Resources/
            CopyConfigsToResources();
            
            progressCallback?.Invoke(new InstallProgress 
            { 
                CurrentStep = "第五步：完成安装", 
                Progress = 1.0f, 
                StatusMessage = "安装完成！正在打开主场景...",
                IsCompleted = true
            });
            
            Debug.Log("安装流程完成，正在打开主场景...");
            
            // 自动打开main.unity场景
            OpenMainScene();
        }
        
        private static void OpenMainScene()
        {
            string mainScenePath = "Assets/Scenes/main.unity";
            if (File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName, mainScenePath)))
            {
                EditorSceneManager.OpenScene(mainScenePath);
                Debug.Log("成功打开主场景: " + mainScenePath);
            }
            else
            {
                Debug.LogWarning("主场景文件不存在: " + mainScenePath + "，请手动创建或复制");
            }
        }
        
        // 新增方法：复制Configs目录下所有文件到Assets/Resources/
        private static void CopyConfigsToResources()
        {
            try
            {
                string configsPath = Path.Combine(Constants.DEFAULT_RESOURCES_ROOT_PATH, "Config");
                string resourcesPath = Path.Combine(Application.dataPath, "Resources");
                
                if (!Directory.Exists(configsPath))
                {
                    Debug.LogWarning($"Configs目录不存在: {configsPath}");
                    return;
                }
                
                if (!Directory.Exists(resourcesPath))
                {
                    Directory.CreateDirectory(resourcesPath);
                    Debug.Log($"创建Resources目录: {resourcesPath}");
                }
                
                // 获取Configs目录下所有文件
                string[] files = Directory.GetFiles(configsPath, "*", SearchOption.AllDirectories);
                
                foreach (string filePath in files)
                {
                    string fileName = Path.GetFileName(filePath);
                    string destFilePath = Path.Combine(resourcesPath, fileName);
                    
                    // 复制文件
                    File.Copy(filePath, destFilePath, true); // true表示覆盖已存在的文件
                    Debug.Log($"复制配置文件: {fileName} -> Assets/Resources/{fileName}");
                }
                
                // 刷新Unity资源
                AssetDatabase.Refresh();
                
                Debug.Log("配置文件复制完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"复制配置文件时出错: {ex.Message}");
            }
        }
        
        public static void ShowHelpWindow()
        {
            HelpWindow.ShowWindow();
        }
    }
}