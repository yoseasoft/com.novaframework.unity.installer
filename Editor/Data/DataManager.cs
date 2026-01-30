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
    internal static class DataManager 
    {
        // 加载持久化数据中的包
        public static List<string> LoadPersistedSelectedPackages()
        {
            List<string> persistedPackageNames = UserSettings.GetObject<List<string>>(Constants.NovaFramework_Installer_PACKAGE_NAME_LIST_KEY);
            if (persistedPackageNames == null)
            {
                persistedPackageNames = new List<string>();
                SavePersistedSelectedPackages(persistedPackageNames);
            }
            return persistedPackageNames;
        }
        
        // 持久化保存已选择的包
        public static void SavePersistedSelectedPackages(List<string> selectPackageNames)
        {
            UserSettings.SetObject(Constants.NovaFramework_Installer_PACKAGE_NAME_LIST_KEY, selectPackageNames);
        }

        public static void ResetPersistedSelectedPackages()
        {
            UserSettings.SetObject(Constants.NovaFramework_Installer_PACKAGE_NAME_LIST_KEY, new List<string>());
        }
        
  
        // 保存系统变量配置
        public static void SaveSystemVariables(Dictionary<string, string> variables)
        {
            // 从现有system_environments.json加载配置以保留其他配置
            var envConfig = LoadSystemEnvironmentsConfig();
            
            // 只更新变量部分，保留模块和其他配置
            // 预估容量以优化性能
            envConfig.variables.Clear();
            envConfig.variables.Capacity = Math.Max(envConfig.variables.Capacity, variables.Count);
            foreach (var kvp in variables)
            {
                envConfig.variables.Add(new EnvironmentVariable { key = kvp.Key, value = kvp.Value });
            }
            
            // 保存回system_environments.json
            string configPath = Constants.SYSTEM_ENVIRONMENTS_ABSOLUTE_PATH;
            string outputJson = JsonUtility.ToJson(envConfig, true);
            File.WriteAllText(configPath, outputJson);
            AssetDatabase.Refresh();
        }
        
        // 加载系统变量配置
        public static Dictionary<string, string> LoadSystemVariables()
        {
            var envConfig = LoadSystemEnvironmentsConfig();
            
            var variables = new Dictionary<string, string>();
            if (envConfig.variables != null)
            {
                foreach (var variable in envConfig.variables)
                {
                    variables[variable.key] = variable.value;
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
            
            // 使用系统路径信息中的默认值，但只有required=true的才使用默认值，其他使用空字符串
            foreach (var pathInfo in systemPathInfos)
            {
                // 如果是必需的，使用默认值，否则使用空字符串
                string value = pathInfo.isRequired ? pathInfo.defaultValue : "";
                variables[pathInfo.name] = value;
            }
            
            return variables;
        }
        
        
        
        // 加载系统环境配置
        public static SystemEnvironmentConfig LoadSystemEnvironmentsConfig()
        {
            string configPath = Constants.SYSTEM_ENVIRONMENTS_ABSOLUTE_PATH;
            
            if (!File.Exists(configPath))
            {
                return new SystemEnvironmentConfig();
            }
            
            string json = File.ReadAllText(configPath);
            
            // 检查文件内容是否为空
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning($"配置文件为空: {configPath}，返回默认配置");
                return new SystemEnvironmentConfig();
            }
            
            try
            {
                return JsonUtility.FromJson<SystemEnvironmentConfig>(json);
            }
            catch (UnityException ex)
            {
                Debug.LogWarning($"配置文件格式错误或损坏: {configPath}，返回默认配置。错误详情: {ex.Message}");
                return new SystemEnvironmentConfig();
            }
        }
        
      
       
    }
}