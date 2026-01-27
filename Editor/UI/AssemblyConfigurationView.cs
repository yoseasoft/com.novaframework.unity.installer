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


using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    public class AssemblyConfigurationView
    {
        // 程序集配置相关
        private List<AssemblyDefinitionInfo> _assemblyConfigs;
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
            EditorGUILayout.HelpBox("在此处可以配置项目自定义程序集", MessageType.Info);
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
            if (GUILayout.Button("添加", GUILayout.Width(120), GUILayout.Height(30)))
            {
                var newConfig = new AssemblyDefinitionInfo
                {
                    name = "New.Assembly",
                    order = _assemblyConfigs.Count + 1,
                    loadableStrategies = new List<string> { "Compile" } // 默认使用Compile标签
                };
                _assemblyConfigs.Add(newConfig);
            }
            
            // 添加按钮间的间距
            GUILayout.Space(10);
            
            // 保存程序集配置按钮，使用标准按钮样式
            if (GUILayout.Button("保存", GUILayout.Width(120), GUILayout.Height(30)))
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
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("名称:", GUILayout.Width(50));
            config.name = EditorGUILayout.TextField(config.name, GUILayout.ExpandWidth(true));
            
            if (GUILayout.Button("移除", GUILayout.Width(60)))
            {
                _assemblyConfigs.RemoveAt(index);
                GUIUtility.ExitGUI(); // 退出GUI以防止索引错误
                return;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("顺序:", GUILayout.Width(50));
            config.order = EditorGUILayout.IntField(config.order, GUILayout.Width(100));
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("标签:", GUILayout.Width(50));
            config.loadableStrategies = EditAssemblyTagsField(config.loadableStrategies);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        void SaveAssemblyConfiguration()
        {
            // 保存到CoreEngine.Editor.UserSettings
            if (_assemblyConfigs != null)
            {
                // 确保每个配置项都有Game标签
                foreach (var config in _assemblyConfigs)
                {
                    if (config.loadableStrategies != null && !config.loadableStrategies.Contains(AssemblyTags.Game))
                    {
                        config.loadableStrategies.Add(AssemblyTags.Game);
                    }
                }
                
                UserSettings.SetObject(Constants.ASSEMBLY_CONFIG_KEY, _assemblyConfigs);
                EditorUtility.DisplayDialog("保存成功", "程序集配置已保存", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "无法保存：程序集配置为空", "确定");
            }
        }
        
        
        public void RefreshData()
        {
            try
            {
                // 从CoreEngine.Editor.UserSettings加载配置
                _assemblyConfigs = UserSettings.GetObject<List<AssemblyDefinitionInfo>>(Constants.ASSEMBLY_CONFIG_KEY);
                if (_assemblyConfigs == null)
                {
                    _assemblyConfigs = new List<AssemblyDefinitionInfo>();
                }
            }
            catch (System.Runtime.Serialization.SerializationException)
            {
                // 如果遇到旧类型的序列化异常，尝试从旧类型转换
                try
                {
                    // 尝试获取旧类型的配置
                    var oldConfigs = UserSettings.GetObject<List<object>>(Constants.ASSEMBLY_CONFIG_KEY);
                    if (oldConfigs != null)
                    {
                        _assemblyConfigs = new List<AssemblyDefinitionInfo>();
                        foreach (var oldConfig in oldConfigs)
                        {
                            // 尝试通过反射或其他方式转换旧配置到新配置
                            var newConfig = new AssemblyDefinitionInfo();
                            
                            // 获取旧对象的类型
                            var oldType = oldConfig.GetType();
                            
                            // 尝试获取旧对象的属性值
                            var nameProperty = oldType.GetProperty("name");
                            var orderProperty = oldType.GetProperty("order");
                            var tagNamesProperty = oldType.GetProperty("tagNames");
                            
                            if (nameProperty != null)
                                newConfig.name = nameProperty.GetValue(oldConfig)?.ToString() ?? "";
                            if (orderProperty != null)
                                newConfig.order = (int)(orderProperty.GetValue(oldConfig) ?? 1);
                            if (tagNamesProperty != null)
                            {
                                var tagNamesValue = tagNamesProperty.GetValue(oldConfig) as List<string>;
                                newConfig.loadableStrategies = tagNamesValue ?? new List<string>();
                            }
                            else
                            {
                                newConfig.loadableStrategies = new List<string>();
                                
                            }
                            
                            _assemblyConfigs.Add(newConfig);
                        }
                    }
                    else
                    {
                        _assemblyConfigs = new List<AssemblyDefinitionInfo>();
                    }
                }
                catch
                {
                    // 如果转换失败，使用空列表
                    _assemblyConfigs = new List<AssemblyDefinitionInfo>();
                }
            }
            catch (System.Exception e)
            {
                // 对于其他异常，使用空列表
                Debug.LogWarning($"读取程序集配置失败，使用默认配置: {e.Message}");
                _assemblyConfigs = new List<AssemblyDefinitionInfo>();
            }
            
        }
        
        
        // 定义可用的程序集标签选项，使用AssemblyTag枚举定义
        private static readonly string[] AvailableTags = { 
            AssemblyTags.Shared, 
            AssemblyTags.Hotfix, 
            AssemblyTags.Compile 
        }; // 可选标签
        
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
                    menu.AddItem(new GUIContent(tag), isSelected, OnTagToggle, new TagToggleData { tags = currentTags, tag = currentTag });
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
            
        }
    }
}