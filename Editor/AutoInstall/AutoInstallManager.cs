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
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;

namespace NovaFramework.Editor.Installer
{
    internal class AutoInstallManager
    {
        // 进度窗口引用 - 使用更抽象的类型
        private static object _progressWindow;
        
        // 外部进度窗口回调
        private static object _externalProgressWindow;
        private static System.Action<int, string> _setStepCallback;
        private static System.Action<int, int, string> _setPackageProgressCallback;
        private static System.Action<string> _addLogCallback;
        private static System.Action<string> _setErrorCallback;
        
        // 是否使用外部进度窗口
        private static bool _useExternalProgress = false;

        public static void StartAutoInstall()
        {
            // 检查是否有外部进度窗口设置
            if (!_useExternalProgress)
            {
                // 如果没有外部进度窗口，尝试创建原始的进度窗口（为了向后兼容）
                // 但这里我们需要使用反射来创建，避免直接引用
                var progressWindowType = GetTypeByName("NovaFramework.Editor.Installer.AutoInstallProgressWindow");
                if (progressWindowType != null)
                {
                    var showWindowMethod = progressWindowType.GetMethod("ShowWindow", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    if (showWindowMethod != null)
                    {
                        _progressWindow = showWindowMethod.Invoke(null, null);
                        
                        // 获取方法来调用AddLog
                        var addLogMethod = progressWindowType.GetMethod("AddLog", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        if (addLogMethod != null)
                        {
                            addLogMethod.Invoke(_progressWindow, new object[] { "正在初始化安装环境..." });
                        }
                    }
                }
                else
                {
                    // 如果无法创建原始窗口，只输出日志
                    UnityEngine.Debug.Log("正在初始化安装环境...");
                }
            }
            else
            {
                // 使用外部进度窗口的回调
                AddLogToProgress("正在初始化安装环境...");
            }
            
            // 延迟执行安装任务，确保窗口先完成渲染
            EditorApplication.delayCall += DoStartInstall;
        }
        
        // 公共方法：设置外部进度窗口回调
        public static void SetExternalProgressCallbacks(
            object externalProgressWindow,
            System.Action<int, string> setStepCallback,
            System.Action<int, int, string> setPackageProgressCallback,
            System.Action<string> addLogCallback,
            System.Action<string> setErrorCallback)
        {
            _externalProgressWindow = externalProgressWindow;
            _setStepCallback = setStepCallback;
            _setPackageProgressCallback = setPackageProgressCallback;
            _addLogCallback = addLogCallback;
            _setErrorCallback = setErrorCallback;
            _useExternalProgress = true;
        }
        
        // 辅助方法：通过名称获取类型（用于反射）
        private static Type GetTypeByName(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }
        
        // 内部辅助方法：将操作转发到适当的进度窗口
        private static void SetStepToProgress(int stepValue, string detail = "")
        {
            if (_useExternalProgress)
            {
                _setStepCallback?.Invoke(stepValue, detail);
            }
            else
            {
                // 使用反射调用原窗口的方法
                var progressWindowType = GetTypeByName("NovaFramework.Editor.Installer.AutoInstallProgressWindow");
                if (progressWindowType != null && _progressWindow != null)
                {
                    var installStepType = GetTypeByName("NovaFramework.Editor.Installer.AutoInstallProgressWindow+InstallStep");
                    if (installStepType != null)
                    {
                        var enumValue = Enum.ToObject(installStepType, stepValue);
                        var setStepMethod = progressWindowType.GetMethod("SetStep", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        if (setStepMethod != null)
                        {
                            setStepMethod.Invoke(_progressWindow, new object[] { enumValue, detail });
                        }
                    }
                }
            }
        }
        
        private static void SetPackageProgressToProgress(int currentIndex, int totalCount, string packageName)
        {
            if (_useExternalProgress)
            {
                _setPackageProgressCallback?.Invoke(currentIndex, totalCount, packageName);
            }
            else
            {
                // 使用反射调用原窗口的方法
                var progressWindowType = GetTypeByName("NovaFramework.Editor.Installer.AutoInstallProgressWindow");
                if (progressWindowType != null && _progressWindow != null)
                {
                    var setPackageProgressMethod = progressWindowType.GetMethod("SetPackageProgress", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    if (setPackageProgressMethod != null)
                    {
                        setPackageProgressMethod.Invoke(_progressWindow, new object[] { currentIndex, totalCount, packageName });
                    }
                }
            }
        }
        
        private static void AddLogToProgress(string message)
        {
            if (_useExternalProgress)
            {
                _addLogCallback?.Invoke(message);
            }
            else
            {
                // 使用反射调用原窗口的方法
                var progressWindowType = GetTypeByName("NovaFramework.Editor.Installer.AutoInstallProgressWindow");
                if (progressWindowType != null && _progressWindow != null)
                {
                    var addLogMethod = progressWindowType.GetMethod("AddLog", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    if (addLogMethod != null)
                    {
                        addLogMethod.Invoke(_progressWindow, new object[] { message });
                    }
                }
                else
                {
                    // 如果无法调用方法，至少输出到控制台
                    UnityEngine.Debug.Log(message);
                }
            }
        }
        
        private static void SetErrorToProgress(string errorMessage)
        {
            if (_useExternalProgress)
            {
                _setErrorCallback?.Invoke(errorMessage);
            }
            else
            {
                // 使用反射调用原窗口的方法
                var progressWindowType = GetTypeByName("NovaFramework.Editor.Installer.AutoInstallProgressWindow");
                if (progressWindowType != null && _progressWindow != null)
                {
                    var setErrorMethod = progressWindowType.GetMethod("SetError", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    if (setErrorMethod != null)
                    {
                        setErrorMethod.Invoke(_progressWindow, new object[] { errorMessage });
                    }
                }
                else
                {
                    // 如果无法调用方法，输出错误到控制台
                    UnityEngine.Debug.LogError(errorMessage);
                }
            }
        }
        
        private static void DoStartInstall()
        {
            SetStepToProgress(1); // CheckEnvironment 对应值为1
            
            // 检查是否已经安装过了
            if (IsAlreadyInstalled())
            {
                AddLogToProgress("检测到框架已经安装过了，无需重复安装。");
                SetStepToProgress(11); // Complete 对应值为11
                
                EditorUtility.DisplayDialog(
                    "自动安装", 
                    "检测到框架已经安装过了，无需重复安装。", 
                    "确定"
                );
                return;
            }
            
            AddLogToProgress("环境检查通过，开始安装流程...");
            
            // 开始自动安装流程...
            InstallRequiredPackages();
        }
        
        // 检查是否已经安装过了
        public static bool IsAlreadyInstalled()
        {
            // 防止在安装过程中因部分文件已存在而误判为已完整安装
            bool installed = UserSettings.GetBool(Constants.NovaFramework_Installer_INSTALLER_COMPLETE_KEY);

            return installed;
        }
        
        private static void InstallRequiredPackages()
        {
            SetStepToProgress(2); // LoadPackageInfo 对应值为2
            
            try
            {
                // 使用PackageManager加载所有包信息
                PackageManager.LoadData();
                
                var allPackages = PackageManager.PackageInfoList;
                
                if (allPackages == null || allPackages.Count == 0)
                {
                    throw new Exception("无法加载包信息");
                }
                
                AddLogToProgress($"成功加载 {allPackages.Count} 个包信息");
                
                // 获取已选择的包（包括必需包及其依赖）
                var selectedPackages = PackageManager.GetSelectedPackageInfos();
                
                AddLogToProgress($"已选择 {selectedPackages.Count} 个包待安装");
                
                // 更新框架设置
                DataManager.SavePersistedSelectedPackages(PackageManager.GetSelectedPackageNames());

                // 实际安装包 - 异步方式
                var packagesList = selectedPackages.Select(p => p.name).ToList();
                
                if (packagesList.Count > 0)
                {
                    SetStepToProgress(3); // InstallPackages 对应值为3
                    InstallPackagesSequentially(packagesList, 0);
                }
                else
                {
                    AddLogToProgress("没有需要安装的包，跳过包安装步骤");
                    CreateDirectories();
                }
            }
            catch (Exception ex)
            {
                SetErrorToProgress($"安装插件包时出错: {ex.Message}");
                Debug.LogError($"安装插件包时出错: {ex.Message}");
            }
        }
        
        // 异步顺序安装包列表
        private static void InstallPackagesSequentially(List<string> packagesList, int currentIndex)
        {
            if (currentIndex >= packagesList.Count)
            {
                // 所有包已添加到manifest.json，不需要显式调用ResolveAllPackages
                // GitManager.InstallPackage 会自动触发包管理器更新
                AddLogToProgress("所有包配置完成...");
                
                // 刷新资源
                AddLogToProgress("正在刷新资源...");
               
                // 直接执行下一步
                CreateDirectories();
                
                return;
            }
            
            var packageName = packagesList[currentIndex];
            
            // 先更新进度窗口
            SetPackageProgressToProgress(currentIndex + 1, packagesList.Count, packageName);
            
            // 直接执行实际安装
            DoInstallSinglePackage(packagesList, currentIndex, packageName);
        }
        
        // 执行单个包的安装
        private static void DoInstallSinglePackage(List<string> packagesList, int currentIndex, string packageName)
        {

            
            try
            {
                // 开始安装包，跳过com.novaframework.unity.core.common包安装
                if (packageName == Constants.COMMON_PACKAGE_NAME)
                {
                    AddLogToProgress($"  跳过: {packageName} (公共包)");

                    // 延迟执行下一个，让UI有机会更新
                    EditorApplication.delayCall += () =>
                    {
                        InstallPackagesSequentially(packagesList, currentIndex + 1);
                    };
                    return;
                }
                
                PackageInfo packageInfo = PackageManager.GetPackageInfoByName(packageName);
                if (packageInfo == null)
                {
                    AddLogToProgress($"  警告: 未找到包信息 {packageName}，跳过");
                    Debug.LogWarning($"[AutoInstall] 未找到包信息: {packageName}");
                    EditorApplication.delayCall += () =>
                    {
                        InstallPackagesSequentially(packagesList, currentIndex + 1);
                    };
                    return;
                }
                

                
                // 使用标志来跟踪是否回调已完成
                bool callbackExecuted = false;
                
                GitManager.InstallPackage(packageInfo, () =>
                {
                    AddLogToProgress($"  完成: {packageName}");
                    
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
                            Debug.LogWarning($"[AutoInstall] 回调超时，强制执行下一步: {packageName}");
                            AddLogToProgress($"  警告: {packageName} (回调超时，强制继续)");
                            
                            // 强制执行下一步
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
                AddLogToProgress($"  警告: 配置包 {packageName} 时发生异常: {ex.Message}");
                Debug.LogError($"[AutoInstall] 配置包 {packageName} 时发生异常: {ex.Message}\n{ex.StackTrace}");
                // 即使当前包配置失败，也继续配置下一个包
                EditorApplication.delayCall += () =>
                {
                    InstallPackagesSequentially(packagesList, currentIndex + 1);
                };
            }
        }
        

        
        internal static void CreateDirectories()
        {
            SetStepToProgress(4); // CreateDirectories 对应值为4
            
            // 延迟执行，让进度界面先刷新
            EditorApplication.delayCall += DoCreateDirectories;
        }
        
        private static void DoCreateDirectories()
        {
            try
            {
                // 获取默认系统变量配置
                var systemVariables = DataManager.GetDefaultSystemVariables();
                
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
                
                // 批量创建所有需要的目录
                foreach (string dirPath in directoriesToCreate)
                {
                    Directory.CreateDirectory(dirPath);
                }
                
                AddLogToProgress($"已创建 {directoriesToCreate.Count} 个目录");
                
                // 保存系统变量配置
                DataManager.SaveSystemVariables(systemVariables);
                AddLogToProgress("已保存系统变量配置");

                // 延迟继续下一步，让UI有机会更新
                EditorApplication.delayCall += () =>
                {
                    InstallBasePackage();
                };
            }
            catch (Exception ex)
            {
                SetErrorToProgress($"创建环境目录时出错: {ex.Message}");
                Debug.LogError($"创建环境目录时出错: {ex.Message}");
            }
        }
        
        private static void InstallBasePackage()
        {
            SetStepToProgress(5); // InstallBasePack 对应值为5
            
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
                    AddLogToProgress("已解压基础包到 Assets/Sources");
                }
                else
                {
                    AddLogToProgress("未找到UI.zip文件，跳过基础包解压步骤");
                    Debug.LogWarning("未找到UI.zip文件，跳过基础包解压步骤");
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
                AddLogToProgress("已创建主场景 Assets/Scenes/main.unity");
                
                // 3. 复制DLL到AOT/Windows目录
                SetStepToProgress(6); // CopyAotLibraries 对应值为6
                
                var systemVariables = DataManager.LoadSystemVariables();
                
                // 确保AOT库文件被复制到正确位置
                string aotDestinationPath = "";
                
                if (systemVariables.ContainsKey("AOT_LIBRARY_PATH"))
                {
                    aotDestinationPath = Path.Combine(Application.dataPath, "..", systemVariables["AOT_LIBRARY_PATH"], "Windows");
                }
                else
                {
                    // 如果AOT_LIBRARY_PATH未定义，使用默认路径Assets/_Resources/Aot/Windows
                    aotDestinationPath = Path.Combine(Application.dataPath, "_Resources", "Aot", "Windows");
                    AddLogToProgress("AOT_LIBRARY_PATH 未在系统变量中定义，使用默认路径: " + aotDestinationPath);
                }
                
                if (!Directory.Exists(aotDestinationPath))
                {
                    Directory.CreateDirectory(aotDestinationPath);
                }
                
                // 从工具包内的AOT目录复制DLL.byte文件到目标AOT/Windows目录
                string sourceAotPath = Path.Combine(Constants.DEFAULT_INSTALLER_ROOT_PATH, "Editor Default Resources/Aot/Windows");
                if (Directory.Exists(sourceAotPath))
                {
                    string[] dllFiles = Directory.GetFiles(sourceAotPath, "*.dll.bytes", SearchOption.TopDirectoryOnly);
                    
                    foreach (string dllFile in dllFiles)
                    {
                        string fileName = Path.GetFileName(dllFile);
                        string destinationPath = Path.Combine(aotDestinationPath, fileName);
                        File.Copy(dllFile, destinationPath, true); // true表示覆盖已存在的文件
                    }
                    
                    AddLogToProgress($"已复制 {dllFiles.Length} 个AOT库文件");
                }
                else
                {
                    AddLogToProgress($"AOT源目录不存在，跳过DLL复制");
                    Debug.LogWarning($"AOT源目录不存在: {sourceAotPath}，跳过DLL.byte复制");
                }
               

                
                // 延迟生成Nova框架配置，让UI有机会更新
                EditorApplication.delayCall += () =>
                {
                    GenerateNovaFrameworkConfig();
                };
            }
            catch (Exception ex)
            {
                SetErrorToProgress($"安装基础包时出错: {ex.Message}");
                Debug.LogError($"安装基础包时出错: {ex.Message}");
                Debug.LogError($"堆栈跟踪: {ex.StackTrace}");
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
           
            Debug.LogWarning("在任何可能的路径中都未找到Game.zip文件");
            return null;
        }
        
        private static void GenerateNovaFrameworkConfig()
        {
            SetStepToProgress(7); // GenerateConfig 对应值为7
            
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
                
                AddLogToProgress("框架配置准备完成");
                
                // 延迟完成安装，让UI有机会更新
                EditorApplication.delayCall += () =>
                {
                    CompleteInstallation();
                };
            }
            catch (Exception ex)
            {
                SetErrorToProgress($"生成框架配置时出错: {ex.Message}");
                Debug.LogError($"生成框架配置时出错: {ex.Message}");
                Debug.LogError($"堆栈跟踪: {ex.StackTrace}");
            }
        }
        
        
        private static void CompleteInstallation()
        {
            // 复制Configs目录下所有文件到Assets/Resources/
            SetStepToProgress(8); // CopyResources 对应值为8
            
            // 延迟执行，让进度界面先刷新
            EditorApplication.delayCall += () =>
            {
                CopyConfigsToResources();
                
                // 延迟导出配置
                EditorApplication.delayCall += () =>
                {
                    SetStepToProgress(9); // ExportConfig 对应值为9
                    
                    EditorApplication.delayCall += () =>
                    {
                        ExportConfigurationMenu.ExportConfiguration(false);
                        AddLogToProgress("已导出 system_environments.json 配置文件");
                        
                        // 延迟打开场景
                        EditorApplication.delayCall += () =>
                        {
                            SetStepToProgress(10); // OpenScene 对应值为10
                            
                            EditorApplication.delayCall += () =>
                            {
                                OpenMainScene();
                                
                                // 延迟创建安装完成标记文件
                                EditorApplication.delayCall += () =>
                                {
                                    UserSettings.SetBool(Constants.NovaFramework_Installer_INSTALLER_COMPLETE_KEY, true);
                                    SetStepToProgress(11); // Complete 对应值为11

                                    Client.Resolve();
                                };
                            };
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
                AddLogToProgress("已打开主场景: " + mainScenePath);
            }
            else
            {
                AddLogToProgress("主场景文件不存在，请手动创建");
                Debug.LogWarning("主场景文件不存在: " + mainScenePath + "，请手动创建或复制");
            }
            
        }
        
        // 新增方法：复制Configs目录下所有文件到Assets/Resources/
        private static void CopyConfigsToResources()
        {
            try
            {
                string configsPath = Path.Combine(Constants.DEFAULT_INSTALLER_ROOT_PATH, "Editor Default Resources/Config");
                string resourcesPath = Path.Combine(Application.dataPath, "Resources");
                
                if (!Directory.Exists(configsPath))
                {
                    AddLogToProgress($"Configs目录不存在，跳过资源复制");
                    Debug.LogWarning($"Configs目录不存在: {configsPath}");
                    return;
                }
                
                if (!Directory.Exists(resourcesPath))
                {
                    Directory.CreateDirectory(resourcesPath);
                }
                
                // 只复制特定的配置文件
                string[] requiredFiles = { "AppConfigures.asset", "AppSettings.asset" };
                int copiedCount = 0;
                
                foreach (string fileName in requiredFiles)
                {
                    string sourceFilePath = Path.Combine(configsPath, fileName);
                    string destFilePath = Path.Combine(resourcesPath, fileName);
                    
                    if (File.Exists(sourceFilePath))
                    {
                        // 复制文件
                        File.Copy(sourceFilePath, destFilePath, true); // true表示覆盖已存在的文件
                        copiedCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"配置文件不存在: {sourceFilePath}");
                    }
                }
                
                AddLogToProgress($"已复制 {copiedCount} 个配置文件到 Assets/Resources");
                
                // 刷新Unity资源
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                AddLogToProgress($"复制配置文件时出错: {ex.Message}");
                Debug.LogError($"复制配置文件时出错: {ex.Message}");
            }
        }
    }
}