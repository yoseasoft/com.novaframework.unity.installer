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
    public class AssemblyConfigurationView
    {
        // 程序集配置相关
        private List<AssemblyDefinitionConfig> _assemblyConfigs;
        private Vector2 _assemblyScrollPos;

        public AssemblyConfigurationView()
        {
            RefreshData();
        }

        public void DrawView()
        {
            // 对程序集配置按order排序
            _assemblyConfigs.Sort((x, y) => x.order.CompareTo(y.order));
            
            // 增大帮助文本的字体
            GUIStyle helpStyle = new GUIStyle(EditorStyles.helpBox);
            helpStyle.fontSize = 20;
            EditorGUILayout.BeginVertical(helpStyle);
            EditorGUILayout.HelpBox("在此处可以配置项目自定义程序集，   注意：核心库为不可修改项", MessageType.Info);
            EditorGUILayout.EndVertical();
            
            // 添加间距
            EditorGUILayout.Space(10);
            
            // 使用固定高度而不是最大高度，确保为底部按钮预留空间
            float availableHeight = Mathf.Max(300, Screen.height * 0.6f); // 使用屏幕高度的60%，但最少300像素
            _assemblyScrollPos = EditorGUILayout.BeginScrollView(_assemblyScrollPos, GUILayout.Height(availableHeight));
            
            for (int i = 0; i < _assemblyConfigs.Count; i++)
            {
                DrawAssemblyItem(i);
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.EndScrollView();
            
            // 添加底部间距
            EditorGUILayout.Space(30);
            
            // 操作按钮，水平排列并居中显示
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // 左侧弹性空间
            
            // 添加新程序集按钮，使用标准按钮样式
            if (GUILayout.Button("添加程序集", GUILayout.Width(100), GUILayout.Height(30)))
            {
                var newConfig = new AssemblyDefinitionConfig
                {
                    name = "New.Assembly",
                    order = _assemblyConfigs.Count + 1,
                    tagNames = new List<string> { "Module" }
                };
                _assemblyConfigs.Add(newConfig);
            }
            
            // 添加按钮间的间距
            GUILayout.Space(10);
            
            // 保存程序集配置按钮，使用标准按钮样式
            if (GUILayout.Button("保存程序集配置", GUILayout.Width(120), GUILayout.Height(30)))
            {
                SaveAssemblyConfiguration();
            }
            
            GUILayout.FlexibleSpace(); // 右侧弹性空间
            EditorGUILayout.EndHorizontal();
        }
        
        void DrawAssemblyItem(int index)
        {
            if (index >= _assemblyConfigs.Count) return;
            
            var config = _assemblyConfigs[index];
            
            EditorGUILayout.BeginVertical("box");
            
            // 检查是否为核心程序集
            bool isCoreAssembly = config.name == "Nova.Library" || 
                                config.name == "Nova.Engine" || 
                                config.name == "Nova.Basic" || 
                                config.name == "Nova.Import";
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("名称:", GUILayout.Width(50));
            
            // 对核心程序集使用只读标签
            if (isCoreAssembly)
            {
                GUILayout.Label(config.name, GUILayout.ExpandWidth(true));
            }
            else
            {
                config.name = EditorGUILayout.TextField(config.name, GUILayout.ExpandWidth(true));
            }
            
            // 仅对非核心程序集显示移除按钮
            if (!isCoreAssembly)
            {
                if (GUILayout.Button("移除", GUILayout.Width(60)))
                {
                    _assemblyConfigs.RemoveAt(index);
                    GUIUtility.ExitGUI(); // 退出GUI以防止索引错误
                    return;
                }
            }
            else
            {
                // 对核心程序集显示提示标签，保持布局一致
                GUILayout.Label("核心", GUILayout.Width(60));
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("顺序:", GUILayout.Width(50));
            
            // 对核心程序集使用只读标签
            if (isCoreAssembly)
            {
                GUILayout.Label(config.order.ToString(), GUILayout.Width(100));
            }
            else
            {
                config.order = EditorGUILayout.IntField(config.order, GUILayout.Width(100));
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("标签:", GUILayout.Width(50));
            
            // 对核心程序集使用只读标签显示
            if (isCoreAssembly)
            {
                string tagsDisplay = config.tagNames != null && config.tagNames.Count > 0 ? 
                    string.Join(", ", config.tagNames.ToArray()) : "无";
                GUILayout.Label(tagsDisplay, GUILayout.ExpandWidth(true));
            }
            else
            {
                config.tagNames = EditAssemblyTagsField(config.tagNames);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        void SaveAssemblyConfiguration()
        {
        
            DataManager.SaveAssemblyConfig(_assemblyConfigs);
            EditorUtility.DisplayDialog("保存成功", "程序集配置已保存到 " + DataManager.AssemblyConfigPath, "确定");
        }
        
        // 将标签字符串列表转换为位标志掩码
        private int ConvertTagsToBitmask(List<string> tagNames)
        {
            int mask = 0;
            
            foreach (string tagName in tagNames)
            {
                switch (tagName)
                {
                    case "Core":
                        mask |= 0x0001;  // Core=0x0001
                        break;
                    case "Module":
                        mask |= 0x0002;  // Package=0x0002
                        break;
                    case "Game":
                        mask |= 0x0004;  // Game=0x0004
                        break;
                    case "Tutorial":
                        mask |= 0x0010;  // Tutorial=0x0010
                        break;
                    case "Test":
                        mask |= 0x0020;  // Test=0x0020
                        break;
                    case "Shared":
                        mask |= 0x0100;  // Shared=0x0100
                        break;
                    case "Hotfix":
                        mask |= 0x0200;  // Hotfix=0x0200
                        break;
                }
            }
            
            return mask;
        }
        
        // 将位标志掩码转换为标签字符串列表
        private List<string> ConvertBitmaskToTags(int mask)
        {
            List<string> tagNames = new List<string>();
            
            if ((mask & 0x0001) != 0) tagNames.Add("Core");
            if ((mask & 0x0002) != 0) tagNames.Add("Module");
            if ((mask & 0x0004) != 0) tagNames.Add("Game");
            if ((mask & 0x0010) != 0) tagNames.Add("Tutorial");
            if ((mask & 0x0020) != 0) tagNames.Add("Test");
            if ((mask & 0x0100) != 0) tagNames.Add("Shared");
            if ((mask & 0x0200) != 0) tagNames.Add("Hotfix");
            
            return tagNames;
        }
        
        public void RefreshData()
        {
            // 从数据管理器加载现有的程序集配置
            _assemblyConfigs = DataManager.LoadAssemblyConfig();
            
            // 从FrameworkSetting中获取当前选中的包
            var frameworkSetting = DataManager.LoadFrameworkSetting();
            var selectedPackages = frameworkSetting.selectedPackages;
            
            // 检查是否有选中的包包含了未保存在配置中的程序集，并添加它们
            foreach (var selectedPackage in selectedPackages)
            {
                if (selectedPackage.assemblyDefinitionInfo != null && 
                    !string.IsNullOrEmpty(selectedPackage.assemblyDefinitionInfo.name))
                {
                    // 检查是否已存在此程序集配置
                    var existingConfig = _assemblyConfigs.Find(c => c.name == selectedPackage.assemblyDefinitionInfo.name);
                    if (existingConfig == null)
                    {
                        // 如果不存在，添加新的配置（使用包中的默认值）
                        var newConfig = new AssemblyDefinitionConfig
                        {
                            name = selectedPackage.assemblyDefinitionInfo.name,
                            order = selectedPackage.assemblyDefinitionInfo.order,
                            tagNames = new List<string>(selectedPackage.assemblyDefinitionInfo.loadableStrategies)
                        };
                        // 兼容性处理：将旧的"Package"标签替换为新的"Module"标签
                        for (int i = 0; i < newConfig.tagNames.Count; i++)
                        {
                            if (newConfig.tagNames[i] == "Package")
                            {
                                newConfig.tagNames[i] = "Module";
                            }
                        }
                        _assemblyConfigs.Add(newConfig);
                    }
                    // 注意：这里不再更新已存在的配置，保持用户之前保存的设置
                }
            }
            
            // 确保核心程序集配置存在
            EnsureCoreAssemblyConfigs();
        }
        
        // 确保核心程序集配置存在
        private void EnsureCoreAssemblyConfigs()
        {
            var coreAssemblies = new List<(string name, int order, int tagMask)> {
                ("Nova.Library", 1, 0x0001),  // Core=0x0001
                ("Nova.Engine", 2, 0x0001),  // Core=0x0001
                ("Nova.Basic", 3, 0x0001),   // Core=0x0001
                ("Nova.Import", 4, 0x0001)   // Core=0x0001
            };
            
            foreach (var coreAssembly in coreAssemblies)
            {
                var existingConfig = _assemblyConfigs.Find(c => c.name == coreAssembly.name);
                if (existingConfig == null)
                {
                    var newConfig = new AssemblyDefinitionConfig
                    {
                        name = coreAssembly.name,
                        order = coreAssembly.order,
                        tagNames = ConvertBitmaskToTags(coreAssembly.tagMask)
                    };
                    _assemblyConfigs.Add(newConfig);
                }
            }
        }
        
        // 定义可用的程序集标签选项
        private static readonly string[] AvailableTags = AssemblyTagHelper.GetAllTagNames();
        
        // 多选标签编辑器
        private static List<string> EditAssemblyTagsField(List<string> currentTags, params GUILayoutOption[] options)
        {
            // 创建下拉菜单内容
            string displayText = currentTags.Count > 0 ? string.Join(",", currentTags.ToArray()) : "请选择标签";
            
            if (GUILayout.Button(displayText, EditorStyles.popup, options))
            {
                // 创建下拉菜单
                GenericMenu menu = new GenericMenu();
                
                for (int i = 0; i < AvailableTags.Length; i++)
                {
                    string tag = AvailableTags[i];
                    bool isSelected = currentTags.Contains(tag);
                    
                    // 创建一个临时变量来存储当前迭代的标签名
                    string currentTag = tag;
                    menu.AddItem(new GUIContent(tag), isSelected, OnTagToggle, new TagToggleData { tags = currentTags, tag = currentTag, shouldSave = false });
                }
                
                menu.ShowAsContext();
            }
            
            return currentTags;
        }
        
        // 用于传递给菜单项的数据结构
        private class TagToggleData
        {
            public List<string> tags;
            public string tag;
            public bool shouldSave;
        }
        
        // 处理标签切换的回调函数
        private static void OnTagToggle(object userData)
        {
            var data = userData as TagToggleData;
            if (data.tags.Contains(data.tag))
            {
                // 移除标签
                data.tags.Remove(data.tag);
            }
            else
            {
                // 添加标签
                data.tags.Add(data.tag);
            }
            
            // 保存配置
            if (data.shouldSave)
            {
                // 这里可以保存配置
            }
        }
    }
}