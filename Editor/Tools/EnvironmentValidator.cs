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
    internal class EnvironmentValidator
    {
        // 验证环境是否已正确安装
        public static ValidationResult ValidateEnvironment()
        {
            ValidationResult result = new ValidationResult();
            
            // 检查必需的包是否已安装
            result.CheckPackages = ValidateRequiredPackages();
            
            // 检查必需的目录结构
            result.CheckDirectories = ValidateRequiredDirectories();
            
            // 检查配置文件是否存在
            result.CheckConfigFiles = ValidateConfigFiles();
            
            // 检查程序集配置
            result.CheckAssemblyConfigs = ValidateAssemblyConfigs();
            
            // 检查AOT库文件
            result.CheckAotLibraries = ValidateAotLibraries();
            
            // 总体验证结果
            result.IsValid = result.CheckPackages.IsValid && 
                            result.CheckDirectories.IsValid && 
                            result.CheckConfigFiles.IsValid && 
                            result.CheckAssemblyConfigs.IsValid &&
                            result.CheckAotLibraries.IsValid;
            
            return result;
        }
        
        // 验证必需的包
        private static ValidationItem ValidateRequiredPackages()
        {
            ValidationItem item = new ValidationItem
            {
                Name = "必需包",
                IsValid = true,
                // 预估容量以优化性能
                Details = new List<string>()
            };

            try
            {
                // 获取当前已选择的包
                var selectedPackageNames = DataManager.LoadPersistedSelectedPackages();

                // 检查是否有已选中的包
                if (selectedPackageNames.Count > 0)
                {
                    item.Details.Add($"找到 {selectedPackageNames.Count} 个已选中的包");

                    // 读取 manifest.json 的 dependencies 区域，按配置判断是否安装
                    string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                    string manifestPath = Path.Combine(projectRoot, "Packages", "manifest.json");

                    if (!File.Exists(manifestPath))
                    {
                        item.Details.Add($"✗ 未找到 manifest.json 文件: {manifestPath}");
                        item.IsValid = false;
                    }
                    else
                    {
                        string manifestJson = File.ReadAllText(manifestPath);
                        int dependenciesIndex = manifestJson.IndexOf("\"dependencies\"", StringComparison.Ordinal);
                        string dependenciesBlock = string.Empty;

                        if (dependenciesIndex >= 0)
                        {
                            int braceStart = manifestJson.IndexOf('{', dependenciesIndex);
                            if (braceStart >= 0)
                            {
                                int braceCount = 0;
                                for (int i = braceStart; i < manifestJson.Length; i++)
                                {
                                    char c = manifestJson[i];
                                    if (c == '{')
                                    {
                                        braceCount++;
                                    }
                                    else if (c == '}')
                                    {
                                        braceCount--;
                                        if (braceCount == 0)
                                        {
                                            int length = i - braceStart + 1;
                                            dependenciesBlock = manifestJson.Substring(braceStart, length);
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        foreach (var packageName in selectedPackageNames)
                        {
                            bool existsInManifest = !string.IsNullOrEmpty(dependenciesBlock) &&
                                                    dependenciesBlock.Contains($"\"{packageName}\"", StringComparison.Ordinal);

                            if (existsInManifest)
                            {
                                item.Details.Add($"✓ {packageName} 已在 manifest.json 的 dependencies 中配置");
                            }
                            else
                            {
                                item.Details.Add($"✗ {packageName} 未在 manifest.json 的 dependencies 中找到配置");
                                item.IsValid = false;
                            }
                        }
                    }
                }
                else
                {
                    item.Details.Add("未找到已选中的包");
                    // 不将此项设为无效，因为可能项目刚刚初始化，没有选择任何包
                    // item.IsValid = false;
                }
            }
            catch (Exception ex)
            {
                item.Details.Add($"检查包时出错: {ex.Message}");
                item.IsValid = false;
            }

            return item;
        }
        
        // 验证必需的目录
        private static ValidationItem ValidateRequiredDirectories()
        {
            ValidationItem item = new ValidationItem
            {
                Name = "必需目录",
                IsValid = true,
                Details = new List<string>()
            };
            
            try
            {
                string[] requiredDirs = {
                    "Assets/Resources", 
                    "Assets/Sources",
                    "Assets/Scenes",
                    "Assets/_Resources",
                    "Assets/_Resources/Aot",
                    "Assets/_Resources/Code"
                };
                
                foreach (string dir in requiredDirs)
                {
                    string fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, dir);
                    if (Directory.Exists(fullPath))
                    {
                        item.Details.Add($"✓ {dir} 目录存在");
                    }
                    else
                    {
                        item.Details.Add($"✗ {dir} 目录不存在");
                        item.IsValid = false;
                    }
                }
            }
            catch (Exception ex)
            {
                item.Details.Add($"检查目录时出错: {ex.Message}");
                item.IsValid = false;
            }
            
            return item;
        }
        
        // 验证配置文件
        private static ValidationItem ValidateConfigFiles()
        {
            ValidationItem item = new ValidationItem
            {
                Name = "配置文件",
                IsValid = true,
                Details = new List<string>()
            };
            
            try
            {
                string configFile = Constants.SYSTEM_ENVIRONMENTS_PATH;
                string fullPath = Constants.SYSTEM_ENVIRONMENTS_ABSOLUTE_PATH;
                
                if (File.Exists(fullPath))
                {
                    item.Details.Add($"✓ {configFile} 配置文件存在");
                }
                else
                {
                    item.Details.Add($"✗ {configFile} 配置文件不存在");
                    item.IsValid = false;
                }
                
                // 验证主场景文件
                string mainScenePath = "Assets/Scenes/main.unity";
                string fullScenePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, mainScenePath);
                if (File.Exists(fullScenePath))
                {
                    item.Details.Add($"✓ {mainScenePath} 场景文件存在");
                }
                else
                {
                    item.Details.Add($"✗ {mainScenePath} 场景文件不存在");
                    item.IsValid = false;
                }
            }
            catch (Exception ex)
            {
                item.Details.Add($"检查配置文件时出错: {ex.Message}");
                item.IsValid = false;
            }
            
            return item;
        }
        
        // 验证程序集配置
        private static ValidationItem ValidateAssemblyConfigs()
        {
            ValidationItem item = new ValidationItem
            {
                Name = "程序集配置",
                IsValid = true,
                Details = new List<string>()
            };
            
            try
            {
                string assemblyConfigPath = Constants.SYSTEM_ENVIRONMENTS_ABSOLUTE_PATH;
                
                if (File.Exists(assemblyConfigPath))
                {
                    item.Details.Add("程序集配置文件存在");
                }
                else
                {
                    item.Details.Add("程序集配置文件不存在");
                    item.IsValid = false;
                }
            }
            catch (Exception ex)
            {
                item.Details.Add($"检查程序集配置时出错: {ex.Message}");
                item.IsValid = false;
            }
            
            return item;
        }
        
        // 验证AOT库文件
        private static ValidationItem ValidateAotLibraries()
        {
            ValidationItem item = new ValidationItem
            {
                Name = "AOT库文件",
                IsValid = true,
                Details = new List<string>()
            };
            
            try
            {
                // 获取系统变量配置
                var systemVariables = DataManager.LoadSystemVariables();
                
                string aotLibraryPath = "Assets/_Resources/Aot/Windows"; // 默认路径
                
                if (systemVariables.ContainsKey("AOT_LIBRARY_PATH"))
                {
                    aotLibraryPath = systemVariables["AOT_LIBRARY_PATH"] + "/Windows";
                }
                
                string fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, aotLibraryPath);
                
                if (Directory.Exists(fullPath))
                {
                    item.Details.Add($"✓ AOT库目录存在: {aotLibraryPath}");
                }
                else
                {
                    item.Details.Add($"✗ AOT库目录不存在: {aotLibraryPath}");
                    item.IsValid = false;
                }
            }
            catch (Exception ex)
            {
                item.Details.Add($"检查AOT库文件时出错: {ex.Message}");
                item.IsValid = false;
            }
            
            return item;
        }
        
        // 显示验证结果
        public static void ShowValidationResult()
        {
            ValidationResult result = ValidateEnvironment();
            
            // 创建验证结果窗口
            ValidationResultsWindow window = (ValidationResultsWindow)EditorWindow.GetWindow<ValidationResultsWindow>(false, "环境验证结果", true);
            window.position = new Rect(100, 100, 700, 500);
            window.SetValidationResult(result);
            window.Show();
        }
        

    }

    internal class ValidationResult
    {
        public bool IsValid { get; set; }
        public ValidationItem CheckPackages { get; set; }
        public ValidationItem CheckDirectories { get; set; }
        public ValidationItem CheckConfigFiles { get; set; }
        public ValidationItem CheckAssemblyConfigs { get; set; }
        public ValidationItem CheckAotLibraries { get; set; }
    }

    internal class ValidationItem
    {
        public string Name { get; set; }
        public bool IsValid { get; set; }
        public List<string> Details { get; set; }
    }
    
    // 验证结果窗口
    internal class ValidationResultsWindow : EditorWindow
    {
        private ValidationResult _validationResult;
        private Vector2 _scrollPosition;
        
        public void SetValidationResult(ValidationResult result)
        {
            this._validationResult = result;
        }
        
        void OnGUI()
        {
            if (_validationResult == null)
            {
                GUILayout.Label("验证结果为空");
                return;
            }
            
            GUILayout.Label("环境验证结果", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 });
            EditorGUILayout.Space();
            
            // 显示总体状态
            GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
            statusStyle.fontSize = 14;
            statusStyle.normal.textColor = _validationResult.IsValid ? Color.green : Color.red;
            
            string overallStatus = _validationResult.IsValid ? "✓ 验证通过" : "✗ 验证未通过";
            GUILayout.Label($"整体状态: {overallStatus}", statusStyle);
            EditorGUILayout.Space(10);
            
            // 创建滚动视图来显示详细信息
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            // 显示各个验证项的详细结果
            DisplayValidationItem(_validationResult.CheckPackages, "包安装");
            DisplayValidationItem(_validationResult.CheckDirectories, "目录结构");
            DisplayValidationItem(_validationResult.CheckConfigFiles, "配置文件");
            DisplayValidationItem(_validationResult.CheckAssemblyConfigs, "程序集配置");
            DisplayValidationItem(_validationResult.CheckAotLibraries, "AOT库文件");
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("刷新验证", GUILayout.Height(30)))
            {
                EnvironmentValidator.ShowValidationResult();
                this.Close();
            }
        }
        
        void DisplayValidationItem(ValidationItem item, string category)
        {
            // 为每个验证项创建一个组
            GUIStyle headerStyle = new GUIStyle(EditorStyles.foldout);
            headerStyle.fontSize = 12;
            
            string status = item.IsValid ? "✓" : "✗";
            string headerText = $"{status} {item.Name} ({category}) - {item.Details.Count} 项";
            
            GUILayout.Label(headerText, EditorStyles.boldLabel);
            
            // 显示详细信息
            foreach (string detail in item.Details)
            {
                GUIStyle detailStyle = new GUIStyle(EditorStyles.label);
                
                if (detail.Contains("✗"))
                {
                    detailStyle.normal.textColor = Color.red;
                    detailStyle.fontStyle = FontStyle.Bold;
                    GUILayout.Label($"  • {detail}", detailStyle);
                }
                else if (detail.Contains("✓"))
                {
                    detailStyle.normal.textColor = Color.green;
                    GUILayout.Label($"  • {detail}", detailStyle);
                }
                else
                {
                    GUILayout.Label($"  • {detail}", detailStyle);
                }
            }
            
            EditorGUILayout.Separator();
            EditorGUILayout.Space(5);
        }
    }
}