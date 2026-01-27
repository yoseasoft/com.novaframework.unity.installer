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

namespace NovaFramework.Editor.Installer
{
    public class ResetManager
    {
        [MenuItem("Tools/重置安装 %#R", priority = 9)]
        public static void ShowResetDialog()
        {
            bool confirm = EditorUtility.DisplayDialog(
                "重置安装", 
                "此操作将删除所有安装的配置和文件，包括:\n" +
                "- 删除 Assets/Resources/FrameworkSetting.asset\n" +
                "- 删除 Assets/Resources/SystemVariables.json\n" +
                "- 删除 Assets/Resources/AssemblyConfig.json\n" +
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
                // 1. 删除配置文件
                DeleteConfigFiles();
                
                // 2. 删除目录
                DeleteCreatedDirectories();
                
                // 3. 重置包管理
                ResetPackages();
                
                // 4. 刷新Unity
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("重置完成", "框架安装已重置，所有相关文件和配置已被删除。", "确定");
            }
            catch (Exception e)
            {
                Debug.LogError($"重置过程中出现错误: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"重置过程中出现错误: {e.Message}", "确定");
            }
        }

        private static void DeleteConfigFiles()
        {
            string[] configFiles = {
                DataManager.FrameworkSettingPath,
                DataManager.SystemVariablesPath,
                DataManager.AssemblyConfigPath,
                "Assets/GameConfigs/ProjectConfig.asset" // 如果存在的话
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
        }

        private static void DeleteCreatedDirectories()
        {
            string[] directories = {
                "Assets/Sources",
                "Assets/_Resources",       // 根据规范必须删除
                "Assets/Resources",        // 根据规范必须删除  
                "Assets/Scenes",           // 根据规范必须删除
            };

            foreach (string dir in directories)
            {
                string fullPath = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), dir);
                string metaPath = fullPath + ".meta";
                
                if (Directory.Exists(fullPath))
                {
                    try
                    {
                        // 删除目录及其所有内容
                        Directory.Delete(fullPath, true);
                        Debug.Log($"已删除目录: {fullPath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"删除目录失败: {fullPath}, 错误: {ex.Message}");
                    }
                }
                else
                {
                    Debug.Log($"目录不存在，跳过删除: {fullPath}");
                }
                
                // 同时删除可能存在的.meta文件
                if (File.Exists(metaPath))
                {
                    try
                    {
                        File.Delete(metaPath);
                        Debug.Log($"已删除元数据文件: {metaPath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"删除元数据文件失败: {metaPath}, 错误: {ex.Message}");
                    }
                }
            }
            
            // 特别处理AOT_LIBRARY_PATH和LINK_LIBRARY_PATH目录
            // 首先尝试从现有配置加载（如果存在）
            var systemVariables = new Dictionary<string, string>();
            if (File.Exists(DataManager.SystemVariablesPath))
            {
                systemVariables = DataManager.LoadSystemVariables();
            }
            else
            {
                // 如果配置文件不存在，使用默认配置
                systemVariables = DataManager.GetDefaultSystemVariables();
            }
            
            if (systemVariables.ContainsKey("AOT_LIBRARY_PATH"))
            {
                string aotPath = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), systemVariables["AOT_LIBRARY_PATH"]);
                if (Directory.Exists(aotPath))
                {
                    try
                    {
                        Directory.Delete(aotPath, true);
                        Debug.Log($"已删除AOT目录: {aotPath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"删除AOT目录失败: {aotPath}, 错误: {ex.Message}");
                    }
                }
                else
                {
                    Debug.Log($"AOT目录不存在，跳过删除: {aotPath}");
                }
            }
            
            if (systemVariables.ContainsKey("LINK_LIBRARY_PATH"))
            {
                string linkPath = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), systemVariables["LINK_LIBRARY_PATH"]);
                if (Directory.Exists(linkPath))
                {
                    try
                    {
                        Directory.Delete(linkPath, true);
                        Debug.Log($"已删除链接库目录: {linkPath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"删除链接库目录失败: {linkPath}, 错误: {ex.Message}");
                    }
                }
                else
                {
                    Debug.Log($"链接库目录不存在，跳过删除: {linkPath}");
                }
            }
        }

        private static void ResetPackages()
        {
            PackageManager.ResetData();
            GitManager.SavePackage(PackageManager.GetSelectedPackages());
            DataManager.SaveSelectPackage(PackageManager.GetSelectedPackages());
        }
    }
}


