/// -------------------------------------------------------------------------------
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
using System.Reflection;
using NovaFramework.Editor.Manifest;
using NovaFramework.Editor.Preference;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using NovaFramework;

namespace NovaFramework.Editor.Installer
{
    internal class AutoInstallManager
    {
        // 进度窗口引用
        private static AutoInstallProgressWindow _progressWindow;
        private static string _launcherPackageName = "com.novaframework.unity.launcher";

        public static void StartAutoInstall()
        {
            Logger.Info("[AutoInstall] 开始自动安装流程");
            _progressWindow = AutoInstallProgressWindow.ShowWindow();
            _progressWindow.AddLog("正在初始化安装环境...");
            // 延迟执行安装任务，确保窗口先完成渲染
            EditorApplication.delayCall += () =>
            {
                DoStartInstall();
            };
        }

        // 重置安装状态，用于重新运行安装流程
        public static void ResetInstallStatus()
        {
            UserSettings.SetBool(Constants.NovaFramework_Installer_INSTALLER_COMPLETE_KEY, false);
            UserSettings.SetBool(Constants.NovaFramework_Installer_PACKAGES_INSTALLED_KEY, false);
            Logger.Info("安装状态已重置，可以重新运行安装流程");
        }

        private static void DoStartInstall()
        {
      
            _progressWindow?.SetStep(AutoInstallProgressWindow.InstallStep.CheckEnvironment);

            // 检查是否已经完成全部安装（包括配置）
            if (IsAlreadyInstalled())
            {
                return;
            }

            _progressWindow?.AddLog("环境检查通过，开始安装流程...");

            // 开始自动安装流程...
            InstallRequiredPackages();
        }

        // 检查是否已经安装过了
        public static bool IsAlreadyInstalled()
        {
            // 检查是否已完成全部安装（包括配置）
            bool installed = UserSettings.GetBool(Constants.NovaFramework_Installer_INSTALLER_COMPLETE_KEY);

            return installed;
        }

        private static void InstallRequiredPackages()
        {
            Logger.Info("[AutoInstall] 开始检查包信息...");
            _progressWindow?.SetStep(AutoInstallProgressWindow.InstallStep.LoadPackageInfo);
            
            try
            {
                // 使用PackageManager加载所有包信息
                PackageManager.LoadData();

                var allPackages = PackageManager.PackageObjectList;
               
                if (allPackages == null || allPackages.Count == 0)
                {
                    throw new Exception("无法加载包信息");
                }
               
                _progressWindow?.AddLog($"成功加载 {allPackages.Count} 个包信息");
               
                // 获取已选择的包（包括必需包及其依赖）
                var selectedPackages = PackageManager.GetSelectedPackageObjects();
               
                _progressWindow?.AddLog($"已选择 {selectedPackages.Count} 个包待安装");
               
                // 更新框架设置
                DataManager.SavePersistedSelectedPackages(PackageManager.GetSelectedPackageNames());
               
                // 实际安装包 - 异步方式
                var packagesList = selectedPackages.Select(p => p.name).ToList();
               
                if (packagesList.Count > 0)
                {
                    _progressWindow?.SetStep(AutoInstallProgressWindow.InstallStep.InstallPackages);
                    InstallPackagesSequentially(packagesList, 0);
                }
                else
                {
                    _progressWindow?.AddLog("没有需要安装的包，跳过包安装步骤");
                    CreateDirectories();
                }
            }
            catch (Exception ex)
            {
                _progressWindow?.SetError($"安装插件包时出错: {ex.Message}");
                Logger.Error($"安装插件包时出错: {ex.Message}");
            }
        }

        // 异步顺序安装包列表
        private static void InstallPackagesSequentially(List<string> packagesList, int currentIndex)
        {
           
            if (currentIndex >= packagesList.Count)
            {
                _progressWindow?.AddLog("所有包配置完成...正在刷新资源...");
                // 直接执行下一步
                CreateDirectories();

                return;
            }

            var packageName = packagesList[currentIndex];

            // 先更新进度窗口
            _progressWindow?.SetPackageProgress(currentIndex + 1, packagesList.Count, packageName);

            EditorApplication.delayCall += () =>
            {
                // 直接执行实际安装
                DoInstallSinglePackage(packagesList, currentIndex, packageName);
            };
            
        }


        // 执行单个包的安装
        private static void DoInstallSinglePackage(List<string> packagesList, int currentIndex, string packageName)
        {
            Logger.Info($"[AutoInstall] 开始安装单个包: {packageName}");
            try
            {
                // 开始安装包，跳过com.novaframework.unity.core.common包安装
                if (packageName == Constants.COMMON_PACKAGE_NAME)
                {
                    // 延迟执行下一个，让UI有机会更新
                    EditorApplication.delayCall += () =>
                    {
                        InstallPackagesSequentially(packagesList, currentIndex + 1);
                    };
                    return;
                }

                PackageObject packageInfo = PackageManager.GetPackageObjectByName(packageName);
                if (packageInfo == null)
                {
                    EditorApplication.delayCall += () =>
                    {
                        InstallPackagesSequentially(packagesList, currentIndex + 1);
                    };
                    return;
                }
                
                Logger.Info($"[AutoInstall] 找到包信息: {packageName}, 开始Git安装");

                // 使用标志来跟踪是否回调已完成
                bool callbackExecuted = false;

                GitManager.InstallPackage(packageInfo, () =>
                {
                    Logger.Info($"[AutoInstall] Git安装完成回调: {packageName}");
                    _progressWindow?.AddLog($"  完成: {packageName}");
                
                    callbackExecuted = true;
                
                    // 使用 delayCall 替代之前的队列机制，因为 delayCall 在 Git 操作后应该正常工作
                    EditorApplication.delayCall += () =>
                    {
                        InstallPackagesSequentially(packagesList, currentIndex + 1);
                    };
                });

                
                // 添加一个计时器来检测回调是否执行，如果没执行则强制继续
                // 使用 EditorApplication.update 来定期检查回调是否被执行
                int checkCount = 0;
                int maxChecks = 100; // 最多检查100次 (约5秒)
               
                EditorApplication.update += CheckCallbackAndUpdate;
               
                void CheckCallbackAndUpdate()
                {
                    checkCount++; 
                   
                    if (callbackExecuted || checkCount >= maxChecks)
                    {
                        
                        // 停止监听更新事件
                        EditorApplication.update -= CheckCallbackAndUpdate;
                        if (!callbackExecuted)
                        {
                            // 如果回调未执行，记录警告并强制执行下一步
                            Logger.Warn($"[AutoInstall] 回调超时，强制执行下一步: {packageName}");
                            _progressWindow?.AddLog($"  警告: {packageName} (回调超时，强制继续)");
                            //强制执行下一步
                            EditorApplication.delayCall += () =>
                            {
                              
                                InstallPackagesSequentially(packagesList, currentIndex + 1);
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _progressWindow?.AddLog($"  警告: 配置包 {packageName} 时发生异常: {ex.Message}");
                 // 即使当前包配置失败，也继续配置下一个包
                EditorApplication.delayCall += () => { 
                    InstallPackagesSequentially(packagesList, currentIndex + 1); 
                };
            }
        }



        internal static void CreateDirectories()
        {
            _progressWindow?.SetStep(AutoInstallProgressWindow.InstallStep.CreateDirectories);

            // 延迟执行，让进度界面先刷新
            EditorApplication.delayCall += DoCreateDirectories;
        }

        private static void DoCreateDirectories()
        {
            Logger.Info("[AutoInstall] DoCreateDirectories called");
            try
            {
                // 获取默认系统变量配置
                var systemVariables = DataManager.GetDefaultSystemVariables();

                Logger.Info($"[AutoInstall] Retrieved {systemVariables.Count} system variables");

                // 准备需要创建的目录列表，避免重复的路径操作
                var directoriesToCreate = new List<string>();

                // 添加环境变量指定的目录
                foreach (var kvp in systemVariables)
                {
                    string fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, kvp.Value);
                    if (!Directory.Exists(fullPath))
                    {
                        directoriesToCreate.Add(fullPath);
                    }
                }

                // 添加框架所需的固定目录
                string resourcesPath = Path.Combine(Application.dataPath, "_Resources");
                if (!Directory.Exists(resourcesPath))
                {
                    directoriesToCreate.Add(resourcesPath);
                }

                string aotPath = Path.Combine(resourcesPath, "Aot");
                if (!Directory.Exists(aotPath))
                {
                    directoriesToCreate.Add(aotPath);
                }

                string codePath = Path.Combine(resourcesPath, "Code");
                if (!Directory.Exists(codePath))
                {
                    directoriesToCreate.Add(codePath);
                }

                string assetsResourcesPath = Path.Combine(Application.dataPath, "Resources");
                if (!Directory.Exists(assetsResourcesPath))
                {
                    directoriesToCreate.Add(assetsResourcesPath);
                }

                Logger.Info($"[AutoInstall] About to create {directoriesToCreate.Count} directories");

                // 批量创建所有需要的目录
                foreach (string dirPath in directoriesToCreate)
                {
                    Logger.Info($"[AutoInstall] Creating directory: {dirPath}");
                    Directory.CreateDirectory(dirPath);
                }

                _progressWindow?.AddLog($"已创建 {directoriesToCreate.Count} 个目录");

                // 保存系统变量配置
                DataManager.SaveSystemVariables(systemVariables);
                _progressWindow?.AddLog("已保存系统变量配置");

                Logger.Info("[AutoInstall] Directory creation completed, moving to next step");

                // 延迟继续下一步，让UI有机会更新
                EditorApplication.delayCall += () => { InstallBasePackage(); };
            }
            catch (Exception ex)
            {
                _progressWindow?.SetError($"创建环境目录时出错: {ex.Message}");
                Logger.Error($"创建环境目录时出错: {ex.Message}");
            }
        }

        private static void InstallBasePackage()
        {
            _progressWindow?.SetStep(AutoInstallProgressWindow.InstallStep.InstallBasePack);

            // 延迟执行，让进度界面先刷新
            EditorApplication.delayCall += DoInstallBasePackage;
        }

        private static void DoInstallBasePackage()
        {
            try
            {
                // 1. 解压基础包到Sources目录
                string sourcesPath = Path.Combine(Application.dataPath, "..", "Assets", "Sources");
                if (!Directory.Exists(sourcesPath))
                {
                    Directory.CreateDirectory(sourcesPath);
                }

                // 尝试多种可能的路径来查找UI.zip
                string uiZipPath = FindUIZipFile();
                if (!string.IsNullOrEmpty(uiZipPath))
                {
                    ZipHelper.ExtractZipFile(uiZipPath, sourcesPath);
                    _progressWindow?.AddLog("已解压基础包到 Assets/Sources");
                }


                // 2. 创建主场景到Scenes目录
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

                // 3. 复制DLL到AOT/Windows目录
                _progressWindow?.SetStep(AutoInstallProgressWindow.InstallStep.CopyAotLibraries);

                var systemVariables = DataManager.LoadSystemVariables();

                // 确保AOT库文件被复制到正确位置
                string aotDestinationPath = "";

                if (systemVariables.ContainsKey("AOT_LIBRARY_PATH"))
                {
                    aotDestinationPath = Path.Combine(Application.dataPath, "..", systemVariables["AOT_LIBRARY_PATH"],
                        "Windows");
                }
                else
                {
                    // 如果AOT_LIBRARY_PATH未定义，使用默认路径Assets/_Resources/Aot/Windows
                    aotDestinationPath = Path.Combine(Application.dataPath, "_Resources", "Aot", "Windows");
                    _progressWindow?.AddLog("AOT_LIBRARY_PATH 未在系统变量中定义，使用默认路径: " + aotDestinationPath);
                }

                if (!Directory.Exists(aotDestinationPath))
                {
                    Directory.CreateDirectory(aotDestinationPath);
                }

                // 从工具包内的AOT目录复制DLL.byte文件到目标AOT/Windows目录
                string sourceAotPath = Path.Combine(Constants.DEFAULT_INSTALLER_ROOT_PATH,
                    "Editor Default Resources/Aot/Windows");
                if (Directory.Exists(sourceAotPath))
                {
                    string[] dllFiles = Directory.GetFiles(sourceAotPath, "*.dll.bytes", SearchOption.TopDirectoryOnly);

                    foreach (string dllFile in dllFiles)
                    {
                        string fileName = Path.GetFileName(dllFile);
                        string destinationPath = Path.Combine(aotDestinationPath, fileName);
                        File.Copy(dllFile, destinationPath, true); // true表示覆盖已存在的文件
                    }

                    _progressWindow?.AddLog($"已复制 {dllFiles.Length} 个AOT库文件");
                }
                else
                {
                    _progressWindow?.AddLog($"AOT源目录不存在，跳过DLL复制");
                }
                
                // 4.复制默认资源
                CopyDefaultRes();

                // 延迟生成Nova框架配置，让UI有机会更新
                EditorApplication.delayCall += () =>
                {
                    // 调用所有自定义模块的安装方法
                    GenerateNovaFrameworkConfig();
                };
            }
            catch (Exception ex)
            {
                _progressWindow?.SetError($"安装基础包时出错: {ex.Message}");
            }
        }

        // 新增方法：查找Game.zip文件
        public static string FindUIZipFile()
        {
            string path = Constants.GAME_ZIP_PATH;

            if (File.Exists(path))
            {
                return path;
            }

            Logger.Warn("在任何可能的路径中都未找到Game.zip文件");
            return null;
        }

        private static void GenerateNovaFrameworkConfig()
        {
            _progressWindow?.SetStep(AutoInstallProgressWindow.InstallStep.GenerateConfig);

            // 延迟执行，让进度界面先刷新
            EditorApplication.delayCall += DoGenerateNovaFrameworkConfig;
        }

        private static void DoGenerateNovaFrameworkConfig()
        {
            try
            {
                // 确保Resources目录存在
                string resourcesPath = Path.Combine(Application.dataPath, "Resources");
                if (!Directory.Exists(resourcesPath))
                {
                    Directory.CreateDirectory(resourcesPath);
                }

                _progressWindow?.AddLog("框架配置准备完成");

                // 延迟完成安装，让UI有机会更新
                EditorApplication.delayCall += () => { CompleteInstallation(); };
            }
            catch (Exception ex)
            {
                _progressWindow?.SetError($"生成框架配置时出错: {ex.Message}");
                Logger.Error($"生成框架配置时出错: {ex.Message}");
            }
        }


        private static void CompleteInstallation()
        {
            // 复制Configs目录下所有文件到Assets/Resources/
            _progressWindow?.SetStep(AutoInstallProgressWindow.InstallStep.CopyResources);


            // 延迟导出配置
            EditorApplication.delayCall += () =>
            {
                _progressWindow?.SetStep(AutoInstallProgressWindow.InstallStep.ExportConfig);

                EditorApplication.delayCall += () =>
                {
                    ExportConfigurationMenu.ExportConfiguration();
                    _progressWindow?.AddLog("已导出 system_environments.json 配置文件");


                    // 延迟打开场景
                    EditorApplication.delayCall += () =>
                    {
                        _progressWindow?.SetStep(AutoInstallProgressWindow.InstallStep.OpenScene);

                        EditorApplication.delayCall += () =>

                        OpenMainScene();

                        // 延迟创建安装完成标记文件
                        EditorApplication.delayCall += () =>
                        {
                            UserSettings.SetBool(Constants.NovaFramework_Installer_INSTALLER_COMPLETE_KEY, true);
                            _progressWindow?.SetStep(AutoInstallProgressWindow.InstallStep.Complete);
                            // 移除launcher模块
                            RemoveLauncherModule();

                        };
                    };


                };
            };
        }




        private static void OpenMainScene()
        {
            string mainScenePath = "Assets/Scenes/main.unity";
            if (File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName, mainScenePath)))
            {
                EditorSceneManager.OpenScene(mainScenePath);
                _progressWindow?.AddLog("已打开主场景: " + mainScenePath);
            }

        }

        // 移除launcher模块
        private static void RemoveLauncherModule()
        {
            Logger.Info("安装完成移除launcher模块...");

            // 先尝试移除launcher模块
            Events.registeredPackages += OnPackagesRegistered;
            var removeRequest = Client.Remove(_launcherPackageName);

            // 等待移除请求完成
            int timeout = 0;
            while (!removeRequest.IsCompleted && timeout < 50) // 最多等待5秒
            {
                System.Threading.Thread.Sleep(100);
                timeout++;
            }

            // 检查移除结果
            if (removeRequest.Status != StatusCode.Success)
            {
                Logger.Info("launcher模块移除失败或未找到，直接调用Client.Resolve()");
                Client.Resolve();
            }
        }

        private static void OnPackagesRegistered(PackageRegistrationEventArgs args)
        {
            // 移除事件监听器以避免重复调用
            Events.registeringPackages -= OnPackagesRegistered;
            Logger.Info("OnPackagesRegistered... 包安装完成，创建配置");

        }
        
  
        public static void CopyDefaultRes()
        {
            string sourceGuiPath = Path.Combine(Constants.DEFAULT_INSTALLER_ROOT_PATH,
                "Editor Default Resources/GUI");
            string sourceTexturePath = Path.Combine(Constants.DEFAULT_INSTALLER_ROOT_PATH,
                "Editor Default Resources/Texture");
            
              // 确保源目录存在再进行复制操作
            if (Directory.Exists(sourceGuiPath))
            {
                string[] guiFiles = Directory.GetFiles(sourceGuiPath, "*.prefab", SearchOption.TopDirectoryOnly);
                Logger.Info($"[AutoInstall] 在GUI目录中找到 {guiFiles.Length} 个prefab文件");

                foreach (string guiprefab in guiFiles)
                {
                    string fileName = Path.GetFileName(guiprefab);
                    string aotDestinationPath = Path.Combine(Application.dataPath, "_Resources", "GUI");
                    
                    // 确保目标目录存在
                    Directory.CreateDirectory(aotDestinationPath);
                    
                    string destinationFilePath = Path.Combine(aotDestinationPath, fileName);
                    File.Copy(guiprefab, destinationFilePath, true); // true表示覆盖已存在的文件
                    Logger.Info($"[AutoInstall] 已复制 prefab: {fileName}");
                }

                _progressWindow?.AddLog($"已复制 {guiFiles.Length} 个prefab");
                Logger.Info($"[AutoInstall] 成功复制 {guiFiles.Length} 个prefab文件");
            }
            else
            {
                string logMessage = $"GUI源目录不存在: {sourceGuiPath}";
                _progressWindow?.AddLog(logMessage);
                Logger.Info($"[AutoInstall] {logMessage}");
            }
            
            // 确保源目录存在再进行复制操作
            if (Directory.Exists(sourceTexturePath))
            {
                string[] textureFiles = Directory.GetFiles(sourceTexturePath, "*.png", SearchOption.TopDirectoryOnly);
                Logger.Info($"[AutoInstall] 在Texture目录中找到 {textureFiles.Length} 个png文件");

                foreach (string textureFile in textureFiles)
                {
                    string fileName = Path.GetFileName(textureFile);
                    string textureDestinationPath = Path.Combine(Application.dataPath, "_Resources", "Texture");
                    
                    // 确保目标目录存在
                    Directory.CreateDirectory(textureDestinationPath);
                    
                    string destinationFilePath = Path.Combine(textureDestinationPath, fileName);
                    File.Copy(textureFile, destinationFilePath, true); // true表示覆盖已存在的文件
                    Logger.Info($"[AutoInstall] 已复制 texture: {fileName}");
                }

                _progressWindow?.AddLog($"已复制 {textureFiles.Length} 个Texture");
                Logger.Info($"[AutoInstall] 成功复制 {textureFiles.Length} 个texture文件");
            }
            else
            {
                string logMessage = $"Texture源目录不存在: {sourceTexturePath}";
                _progressWindow?.AddLog(logMessage);
                Logger.Info($"[AutoInstall] {logMessage}");
            }
            
            Logger.Info($"[AutoInstall] 复制默认资源完成");
        }
        
         /// <summary>
        /// 调用所有实现了IModuleInstallHandler的类的Install方法
        /// </summary>
        public static void InvokeAllInstall()
        {
            var interfaceHandlers = GetModuleInstallHandlersFromInterface();
            
            Logger.Info($"[AutoInstall] ======== InvokeAllInstall");
            // 调用接口实现的Install方法
            foreach (var handler in interfaceHandlers)
            {
                try
                {
                    Logger.Info($"正在执行模块安装 (接口): {handler.GetType().Name}");
                    handler.Install(() => {
                        Logger.Info($"模块安装完成 (接口): {handler.GetType().Name}");
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error($"执行模块安装失败 {handler.GetType().Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 调用所有实现了IModuleInstallHandler的类的Uninstall方法
        /// </summary>
        public static void InvokeAllUninstall()
        {
            var interfaceHandlers = GetModuleInstallHandlersFromInterface();
            
            // 调用接口实现的Uninstall方法
            foreach (var handler in interfaceHandlers)
            {
                try
                {
                    Logger.Info($"正在执行模块卸载 (接口): {handler.GetType().Name}");
                    handler.Uninstall(() => {
                        Logger.Info($"模块卸载完成 (接口): {handler.GetType().Name}");
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error($"执行模块卸载失败 {handler.GetType().Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 获取所有IModuleInstallHandler的实例
        /// </summary>
        /// <returns>IModuleInstallHandler实例列表</returns>
        private static List<InstallationStep> GetModuleInstallHandlersFromInterface()
        {
            var handlers = new List<InstallationStep>();
            
            try
            {
                // 加载仓库清单数据
                RepoManifest.Instance.LoadData();
                var packageObjects = RepoManifest.Instance.modules;
                
                if (packageObjects == null || packageObjects.Count == 0)    
                {
                    Logger.Warn("未找到任何包配置信息");
                    return handlers;
                }
                
                // 按PID排序包对象
                var sortedPackages = packageObjects.OrderBy(p => p.pid).ToList();
                
                // 在自动安装阶段，只处理必需包(required=true)及其依赖
                var requiredPackageNames = new HashSet<string>();
                
                // 先添加所有必需包
                foreach (var package in sortedPackages)
                {
                    if (package.required)
                    {
                        requiredPackageNames.Add(package.name);
                    }
                }
                
                // 添加必需包的依赖
                foreach (var packageName in requiredPackageNames.ToList())
                {
                    var package = sortedPackages.FirstOrDefault(p => p.name == packageName);
                    if (package?.dependencies != null)
                    {
                        foreach (var dep in package.dependencies)
                        {
                            requiredPackageNames.Add(dep);
                        }
                    }
                }
                
                foreach (var package in sortedPackages)
                {
                    // 检查包是否为必需包或其依赖
                    if (!requiredPackageNames.Contains(package.name))
                    {
                        continue;
                    }
                    
                    // 检查包是否有安装配置
                    if (package.installationObject?.importModules == null)
                    {
                        continue;
                    }
                    
                    // 遍历该包的所有import-strategy配置
                    foreach (var importModule in package.installationObject.importModules)
                    {
                        // 检查installable属性是否为true
                        if (!importModule.installable)
                        {
                            continue;
                        }
                        
                        // 通过name获取程序集
                        string assemblyName = importModule.name;
                        if (string.IsNullOrEmpty(assemblyName))
                        {
                            continue;
                        }
                        
                        // 查找对应的程序集
                        var assembly = AppDomain.CurrentDomain.GetAssemblies()
                            .FirstOrDefault(a => a.GetName().Name == assemblyName);
                        
                        if (assembly == null)
                        {
                            Logger.Warn($"未找到程序集: {assemblyName}");
                            continue;
                        }
                        
                        try
                        {
                            // 查找该程序集中实现了InstallationStep的类型
                            var types = assembly.GetTypes()
                                .Where(x => typeof(InstallationStep).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);
                            
                            foreach (var type in types)
                            {
                                var instance = Activator.CreateInstance(type) as InstallationStep;
                                if (instance != null)
                                {
                                    handlers.Add(instance);
                                    Logger.Info($"找到安装处理器: {type.FullName} (来自包: {package.name})");
                                }
                            }
                        }
                        catch (ReflectionTypeLoadException)
                        {
                            Logger.Warn($"无法加载程序集中的类型: {assemblyName}");
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"获取IModuleInstallHandler实例时出错: {ex.Message}");
            }
            
            return handlers;
        }

    }

}