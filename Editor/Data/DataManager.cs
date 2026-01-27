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
using UnityEditor;
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    public static class DataManager
    {
        // 数据存储路径
        public static readonly string FrameworkSettingPath = Constants.FRAMEWORK_SETTING_PATH;
        public static readonly string SystemVariablesPath = Constants.SYSTEM_VARIABLES_PATH;
        public static readonly string AssemblyConfigPath = Constants.ASSEMBLY_CONFIG_PATH;
        
        // 保存框架设置
        public static void SaveFrameworkSetting(FrameworkSetting setting)
        {
            if (setting == null) return;
            
            string directory = Path.GetDirectoryName(FrameworkSettingPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // 检查资源是否存在，如果存在则更新，否则创建新的
            FrameworkSetting existingSetting = AssetDatabase.LoadAssetAtPath<FrameworkSetting>(FrameworkSettingPath);
            if (existingSetting != null)
            {
                EditorUtility.CopySerialized(setting, existingSetting);
                AssetDatabase.SaveAssets();
            }
            else
            {
                AssetDatabase.CreateAsset(setting, FrameworkSettingPath);
            }
            
            AssetDatabase.Refresh();
        }
        
        // 加载框架设置
        public static FrameworkSetting LoadFrameworkSetting()
        {
            FrameworkSetting setting = AssetDatabase.LoadAssetAtPath<FrameworkSetting>(FrameworkSettingPath);
            if (setting == null)
            {
                setting = ScriptableObject.CreateInstance<FrameworkSetting>();
                // 初始化默认值
                setting.selectedPackages = new List<PackageInfo>();
            }
            return setting;
        }

        public static void SaveSelectPackage(List<PackageInfo> selectPackages)
        {
            FrameworkSetting setting = LoadFrameworkSetting();
            setting.selectedPackages = selectPackages;
        }
        
        // 保存系统变量配置
        public static void SaveSystemVariables(Dictionary<string, string> variables)
        {
            var jsonEntries = new List<SystemVariableEntry>();
            foreach (var kvp in variables)
            {
                jsonEntries.Add(new SystemVariableEntry { _key = kvp.Key, _path = kvp.Value });
            }
            
            string json = JsonUtility.ToJson(new SystemVariablesContainer { _entries = jsonEntries }, true);
            
            string directory = Path.GetDirectoryName(SystemVariablesPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(SystemVariablesPath, json);
            AssetDatabase.Refresh();
        }
        
        // 加载系统变量配置
        public static Dictionary<string, string> LoadSystemVariables()
        {
            if (!File.Exists(SystemVariablesPath))
            {
                // 返回默认配置
                return GetDefaultSystemVariables();
            }
            
            string json = File.ReadAllText(SystemVariablesPath);
            var container = JsonUtility.FromJson<SystemVariablesContainer>(json);
            
            var variables = new Dictionary<string, string>();
            if (container._entries != null)
            {
                foreach (var entry in container._entries)
                {
                    variables[entry._key] = entry._path;
                }
            }
            
            return variables;
        }
        
        // 获取默认系统变量
        public static Dictionary<string, string> GetDefaultSystemVariables()
        {
            var variables = new Dictionary<string, string>();
            
            // 从PackageManager获取系统路径信息
            PackageManager.LoadData();
            var systemPathInfos = PackageManager.SystemPathInfos;
            
            // 使用系统路径信息中的默认值
            foreach (var pathInfo in systemPathInfos)
            {
                variables[pathInfo.name] = pathInfo.defaultValue;
            }
            
            return variables;
        }
        
        // 保存程序集配置
        public static void SaveAssemblyConfig(List<AssemblyDefinitionConfig> configs)
        {
            var configData = new AssemblyConfigDataWrapper { _assemblyConfigs = configs };
            string json = JsonUtility.ToJson(configData, true);
            
            string directory = Path.GetDirectoryName(AssemblyConfigPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(AssemblyConfigPath, json);
            AssetDatabase.Refresh();
        }
        
        // 加载程序集配置
        public static List<AssemblyDefinitionConfig> LoadAssemblyConfig()
        {
            // 检测是否有AssemblyConfig.json文件，如果有就按这个json加载，没有的话返回空列表让系统生成新的
            string primaryPath = AssemblyConfigPath; // Assets/Resources/AssemblyConfig.json
            string configPath = null;
            
            // 优先检查主要路径
            if (File.Exists(primaryPath))
            {
                configPath = primaryPath;
            }
            
            
            if (configPath != null)
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    var wrapper = JsonUtility.FromJson<AssemblyConfigDataWrapper>(json);
                    return wrapper._assemblyConfigs ?? new List<AssemblyDefinitionConfig>();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"加载程序集配置失败: {ex.Message}");
                    return new List<AssemblyDefinitionConfig>();
                }
            }
            
            // 如果两个路径都没有配置文件，则返回空列表，让系统生成新的
            return new List<AssemblyDefinitionConfig>();
        }
        
        // 检查系统变量配置是否存在（真实存在文件，而非默认配置）
        public static bool IsSystemVariablesConfigured()
        {
            return File.Exists(SystemVariablesPath);
        }
        
        // 内部类定义
        [Serializable]
        private class SystemVariablesContainer
        {
            public List<SystemVariableEntry> _entries = new List<SystemVariableEntry>();
        }
        
        [Serializable]
        private class SystemVariableEntry
        {
            public string _key;
            public string _path;
        }
        
        [Serializable]
        private class AssemblyConfigDataWrapper
        {
            public List<AssemblyDefinitionConfig> _assemblyConfigs = new List<AssemblyDefinitionConfig>();
        }
    }
}