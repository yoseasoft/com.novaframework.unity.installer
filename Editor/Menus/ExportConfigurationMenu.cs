using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    internal static class ExportConfigurationMenu
    {
        [MenuItem("Tools/导出配置 _F3", false, 1000)]
        public static void ExportConfiguration()
        {
            ExportConfiguration(true); // 菜单导出时选中文件
        }
        
        public static void ExportConfiguration(bool selectFile = false) // 添加参数控制是否选中文件
        {
            try
            {
                // 从UserSettings获取配置，使用try-catch包装以防反序列化错误
                Dictionary<string, string> environmentDirectories = null;
                try
                {
                    environmentDirectories = UserSettings.GetObject<Dictionary<string, string>>(Constants.NovaFramework_Installer_DIRECTORY_CONFIG_KEY);
                }
                catch (Exception ex)
                {
                    // Debug.LogWarning($"读取环境目录配置失败，使用默认配置。错误: {ex.Message}");
                    string errorMessage = ex.Message;
                    environmentDirectories = DataManager.GetDefaultSystemVariables();
                }
                
                // 如果从UserSettings获取不到有效配置，使用默认配置
                if (environmentDirectories == null || environmentDirectories.Count == 0)
                {
                    environmentDirectories = DataManager.GetDefaultSystemVariables();
                }
                
                List<AssemblyDefinitionInfo> assemblyConfigs = null;
                
                try
                {
                    assemblyConfigs = UserSettings.GetObject<List<AssemblyDefinitionInfo>>(Constants.NovaFramework_Installer_ASSEMBLY_CONFIG_KEY);
                }
                catch (Exception ex)
                {
                    // Debug.LogWarning($"读取程序集配置失败，使用默认配置。错误: {ex.Message}");
                    string errorMessage = ex.Message;
                    assemblyConfigs = new List<AssemblyDefinitionInfo>();
                }
               
                if (assemblyConfigs == null)
                {
                    assemblyConfigs = new List<AssemblyDefinitionInfo>();
                }
                
                // 创建SystemEnvironmentConfig对象
                var envConfig = new SystemEnvironmentConfig
                {
                    variables = new List<EnvironmentVariable>(),
                    modules = new List<ModuleConfig>(),
                    aot_libraries = new List<string>()
                };
                
                // 添加环境目录到变量中
                foreach (var dir in environmentDirectories)
                {
                    envConfig.variables.Add(new EnvironmentVariable { key = dir.Key, value = dir.Value });
                }
                
                // 添加程序集配置到modules中
                foreach (var config in assemblyConfigs)
                {
                    // 确保每个自定义程序集配置都包含Game标签
                    var tags = config.loadableStrategies ?? new List<string>();
                    if (!tags.Contains(AssemblyTags.Game))
                    {
                        tags.Add(AssemblyTags.Game);
                    }
                    
                    envConfig.modules.Add(new ModuleConfig 
                    { 
                        name = config.name, 
                        order = config.order, 
                        tags = tags
                    });
                }
                
                // 确保默认模块配置存在
                // 检查是否已存在Game和GameHotfix模块，如果不存在则添加
                if (!envConfig.modules.Any(m => m.name == AssemblyTags.Game))
                {
                    envConfig.modules.Add(new ModuleConfig 
                    { 
                        name = AssemblyTags.Game, 
                        order = 1102, 
                        tags = new List<string> { AssemblyTags.Game, AssemblyTags.Compile } 
                    });
                }
                
                if (!envConfig.modules.Any(m => m.name == "GameHotfix"))
                {
                    envConfig.modules.Add(new ModuleConfig 
                    { 
                        name = "GameHotfix", 
                        order = 1103, 
                        tags = new List<string> { AssemblyTags.Game, AssemblyTags.Compile, AssemblyTags.Hotfix } 
                    });
                }
                
                // 从PackageManager获取已安装包的assembly-definition信息并添加到配置
                AddInstalledPackageAssemblyDefinitions(envConfig, assemblyConfigs);
                
                // 扫描AOT目录下的DLL文件并添加到aot_libraries列表
                ScanAndAddAotLibraries(envConfig);
                
                // 确保Resources目录存在
                // 确保Resources目录存在
                string resourcesPath = Path.Combine(Application.dataPath, "Resources");
                if (!Directory.Exists(resourcesPath))
                {
                    Directory.CreateDirectory(resourcesPath);
                }
                
                // 保存配置到system_environments.json
                string configPath = Constants.SYSTEM_ENVIRONMENTS_ABSOLUTE_PATH;
                
                // 确保目录存在
                string directoryPath = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                string outputJson = JsonUtility.ToJson(envConfig, true);
                File.WriteAllText(configPath, outputJson);
                
                Debug.Log($"配置已导出到: {configPath}");
                
                // 刷新Asset数据库
                AssetDatabase.Refresh();
                
                // 只有在selectFile为true时才选中导出的配置文件（通常是通过菜单导出时）
                if (selectFile)
                {
                    UnityEngine.Object configFile = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Constants.SYSTEM_ENVIRONMENTS_PATH);
                    if (configFile != null)
                    {
                        Selection.activeObject = configFile;
                        EditorGUIUtility.PingObject(configFile);
                    }
                    else
                    {
                        Debug.LogWarning($"无法选中配置文件: {Constants.SYSTEM_ENVIRONMENTS_PATH}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"导出配置时发生错误: {e.Message}");
                EditorUtility.DisplayDialog("导出失败", $"导出配置时发生错误: {e.Message}", "确定");
            }
        }
        
        // 从PackageManager获取已安装包的assembly-definition信息并添加到配置
        private static void AddInstalledPackageAssemblyDefinitions(SystemEnvironmentConfig envConfig, List<AssemblyDefinitionInfo> userAssemblyConfigs)
        {
            try
            {
                // 从PackageManager获取已选择的包信息（即已安装的包）
                var selectedPackages = PackageManager.GetSelectedPackageInfos();
                
                // 遍历已选择的包，获取其assembly-definition信息
                foreach (var package in selectedPackages)
                {
                    if (package.assemblyDefinitionInfo != null)
                    {
                        // 检查此程序集定义是否已存在于用户配置中
                        bool alreadyExists = userAssemblyConfigs.Exists(config => config.name == package.assemblyDefinitionInfo.name);
                        
                        if (!alreadyExists)
                        {
                            // 将包的assembly-definition信息添加到模块配置中
                            envConfig.modules.Add(new ModuleConfig
                            {
                                name = package.assemblyDefinitionInfo.name,
                                order = package.assemblyDefinitionInfo.order,
                                tags = package.assemblyDefinitionInfo.loadableStrategies // 这是List<string>
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"添加已安装包的assembly-definition信息时出错: {ex.Message}");
            }
        }
        
        // 扫描AOT目录下的DLL文件并添加到aot_libraries列表
        private static void ScanAndAddAotLibraries(SystemEnvironmentConfig envConfig)
        {
            // 从UserSettings中获取配置的系统变量
            Dictionary<string, string> environmentDirectories = null;
            try
            {
                environmentDirectories = UserSettings.GetObject<Dictionary<string, string>>(Constants.NovaFramework_Installer_DIRECTORY_CONFIG_KEY);
            }
            catch (Exception e)
            {
                // Debug.LogWarning($"读取AOT目录配置失败，使用默认值: {e.Message}");
                //为了去掉警告
                string errorMessage = e.Message;
            }
            
            // 如果从UserSettings获取不到有效配置，使用默认配置
            if (environmentDirectories == null || environmentDirectories.Count == 0)
            {
                environmentDirectories = DataManager.GetDefaultSystemVariables();
            }
            
            string aotDirectoryPath = "";
            if (environmentDirectories != null && environmentDirectories.ContainsKey("AOT_LIBRARY_PATH"))
            {
                string aotLibraryPath = environmentDirectories["AOT_LIBRARY_PATH"];
                if (!string.IsNullOrEmpty(aotLibraryPath))
                {
                    // 使用AOT_LIBRARY_PATH配置的路径 + Windows子目录
                    aotDirectoryPath = Path.Combine(aotLibraryPath, "Windows");
                    // 如果路径是相对Assets的，转换为完整路径
                    if (aotDirectoryPath.StartsWith("Assets/"))
                    {
                        aotDirectoryPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), aotDirectoryPath);
                    }
                }
            }
            
            // 如果没有从UserSettings中找到AOT路径配置，使用默认路径
            if (string.IsNullOrEmpty(aotDirectoryPath))
            {
                aotDirectoryPath = Path.Combine(Application.dataPath, "_Resources", "Aot", "Windows");
            }
            
            if (Directory.Exists(aotDirectoryPath))
            {
                // 查找所有.dll.bytes文件
                string[] dllByteFiles = Directory.GetFiles(aotDirectoryPath, "*.dll.bytes", SearchOption.TopDirectoryOnly);
                foreach (string dllByteFile in dllByteFiles)
                {
                    string fileName = Path.GetFileName(dllByteFile);
                    
                    // 提取原始DLL名称（去掉.bytes后缀）
                    string dllName = fileName.Substring(0, fileName.Length - ".bytes".Length);

                    
                    if (!envConfig.aot_libraries.Contains(dllName))
                    {
                        envConfig.aot_libraries.Add(dllName);

                    }
                }
            }
           
        }
        
       
    }
}