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
    internal class ResetManager
    {
        [MenuItem("Tools/重置安装 %#R", priority = 9)]
        public static void ShowResetDialog()
        {
            bool confirm = EditorUtility.DisplayDialog(
                "重置安装", 
                "此操作将删除所有安装的配置和文件，包括:\n" +
                "- 删除 Assets/Resources 目录（包含所有配置文件）\n" +
                "- 删除 Assets/Sources 目录\n" +
                "- 删除 Assets/_Resources 目录\n" +
                "- 删除 Assets/Scenes 目录\n" +
                "- 从 manifest.json 中移除所有相关包\n\n" +
                "此操作不可逆，确定要继续吗？", 
                "确定", 
                "取消"
            );

            if (confirm)
            {
                PerformReset();
            }
        }

        public static void PerformReset()
        {
            try
            {
                // 1. 清除UserSettings中的配置
                ClearUserSettings();
                
                // 2. 删除配置文件
                DeleteConfigFiles();
                
                // 3. 删除目录
                DeleteCreatedDirectories();
                
                // 4. 重置包管理
                ResetPackages();
                
                // 5. 刷新Unity
                AssetDatabase.Refresh();
                
                // 完成
                EditorUtility.DisplayDialog("重置完成", "框架安装已重置，所有相关文件和配置已被删除。", "确定");
            }
            catch (Exception e)
            {
                Debug.LogError($"重置过程中出现错误: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"重置过程中出现错误: {e.Message}", "确定");
            }
        }

        // 安全检查方法，确保要删除的路径在项目范围内且不是危险路径
        private static bool IsSafeToDelete(string path, string projectRoot)
        {
            try
            {
                // 规范化路径
                string normalizedPath = Path.GetFullPath(path);
                string normalizedProjectRoot = Path.GetFullPath(projectRoot);
                
                // 检查路径是否在项目根目录下
                if (!normalizedPath.StartsWith(normalizedProjectRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return false; // 路径在项目外，不安全
                }
                
                // 检查是否是项目根目录本身或Assets目录本身
                if (string.Equals(normalizedPath, normalizedProjectRoot, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(normalizedPath, Path.Combine(normalizedProjectRoot, "Assets"), StringComparison.OrdinalIgnoreCase))
                {
                    return false; // 不能删除项目根目录或Assets目录本身
                }
                
                // 检查路径中是否包含危险组件
                string relativePath = normalizedPath.Substring(normalizedProjectRoot.Length).TrimStart('/', '\\');
                string[] pathParts = relativePath.Split('/', '\\');
                
                foreach (string part in pathParts)
                {
                    if (part == ".." || part == ".")
                    {
                        return false; // 包含路径遍历组件，不安全
                    }
                }
                
                return true; // 路径安全
            }
            catch
            {
                return false; // 如果有任何错误，视为不安全
            }
        }
        
        private static void DeleteConfigFiles()
        {
            string[] configFiles = {
                Constants.SYSTEM_ENVIRONMENTS_PATH,  // 使用实际的配置文件路径
            };

            foreach (string configFile in configFiles)
            {
                string fullPath = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), configFile);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    Debug.Log($"已删除配置文件: {fullPath}");
                }
            }
            
            UserSettings.SetBool(Constants.NovaFramework_Installer_INSTALLER_COMPLETE_KEY, false);
           
            Debug.Log($"已重置安装完成标记文件: false");
        }

        private static void DeleteCreatedDirectories()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).ToString();
            
            string[] directories = {
                "Assets/Sources",
                "Assets/_Resources",       // 根据规范必须删除
                "Assets/Resources",        // 根据规范必须删除  
                "Assets/Scenes",           // 根据规范必须删除
            };

            foreach (string dir in directories)
            {
                string fullPath = Path.Combine(projectRoot, dir.Replace("/", Path.DirectorySeparatorChar.ToString()));
                string metaPath = fullPath + ".meta";
                
                // 安全检查：确保路径在项目内且不包含敏感路径
                if (IsSafeToDelete(fullPath, projectRoot))
                {
                    if (Directory.Exists(fullPath))
                    {
                        // 删除目录及其所有内容
                        Directory.Delete(fullPath, true);
                        Debug.Log($"已删除目录: {fullPath}");
                    }
                   
                    
                    // 同时删除可能存在的.meta文件
                    if (File.Exists(metaPath))
                    {
                        File.Delete(metaPath);
                        Debug.Log($"已删除元数据文件: {metaPath}");
                    }
                }
               
            }
            
            // 特别处理AOT_LIBRARY_PATH和LINK_LIBRARY_PATH目录
            // 首先尝试从现有配置加载（如果存在）
            var systemVariables = new Dictionary<string, string>();
            if (File.Exists(Constants.SYSTEM_ENVIRONMENTS_PATH))
            {
                var envConfig = DataManager.LoadSystemEnvironmentsConfig();
                if (envConfig.variables != null)
                {
                    foreach (var variable in envConfig.variables)
                    {
                        systemVariables[variable.key] = variable.value;
                    }
                }
            }
            else
            {
                // 如果配置文件不存在，使用默认配置
                systemVariables = DataManager.GetDefaultSystemVariables();
            }
            
            if (systemVariables.ContainsKey("SCRIPT_FILE_PATH"))
            {
                string aotPath = Path.Combine(projectRoot, systemVariables["SCRIPT_FILE_PATH"].Replace("/", Path.DirectorySeparatorChar.ToString()));
                // 再次进行安全检查
                if (IsSafeToDelete(aotPath, projectRoot))
                {
                    if (Directory.Exists(aotPath))
                    {
                        Directory.Delete(aotPath, true);
                    }
                }
            }
            
           
        }

        private static void ClearUserSettings()
        {
            // 清除UserSettings中的配置
            UserSettings.SetString(Constants.NovaFramework_Installer_DIRECTORY_CONFIG_KEY, null);
            // 对于SetObject，传递空列表而不是null以避免序列化错误
            UserSettings.SetObject<List<AssemblyDefinitionInfo>>(Constants.NovaFramework_Installer_ASSEMBLY_CONFIG_KEY, new List<AssemblyDefinitionInfo>());
            Debug.Log("已清除UserSettings中的配置");
        }
        
        private static void ResetPackages()
        {
            //跳过 com.novaframework.unity.core.common 包卸载
            foreach (var selectedPackageName in PackageManager.GetSelectedPackageNames())
            {
                if (selectedPackageName != Constants.COMMON_PACKAGE_NAME)
                {
                    GitManager.UninstallPackage(selectedPackageName);
                }
            }
            DataManager.ResetPersistedSelectedPackages();
        }
    }
}