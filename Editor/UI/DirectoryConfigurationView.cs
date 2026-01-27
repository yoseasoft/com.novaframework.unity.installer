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
    public class DirectoryConfigurationView
    {
        // 环境目录配置相关
        private Dictionary<string, string> _systemVariables;
        private List<SystemPathInfo> _systemPathInfos; // 从PackageManager获取的系统路径信息
        private Vector2 _dirScrollPos;

        public DirectoryConfigurationView()
        {
            RefreshData();
        }

        public void DrawView()
        {
            // 增大帮助文本的字体
            GUIStyle helpStyle = new GUIStyle(EditorStyles.helpBox);
            helpStyle.fontSize = 14;
            EditorGUILayout.BeginVertical(helpStyle);
            EditorGUILayout.HelpBox("在此处可以修改系统变量目录配置", MessageType.Info);
            EditorGUILayout.EndVertical();
            
            // 添加间距
            EditorGUILayout.Space(10);
            
            // 使用固定高度而不是ExpandHeight(true)，确保为底部按钮预留空间
            float availableHeight = Mathf.Max(250, Screen.height * 0.6f); // 使用屏幕高度的60%，但最少250像素，确保内容可显示
            _dirScrollPos = EditorGUILayout.BeginScrollView(_dirScrollPos, GUILayout.Height(availableHeight));
            
            // 使用从PackageManager获取的系统路径信息来显示配置
            foreach (var pathInfo in _systemPathInfos)
            {
                EditorGUILayout.BeginHorizontal("box");
                
                // 显示路径信息的标题，如果没有标题则显示名称
                string displayTitle = string.IsNullOrEmpty(pathInfo.title) ? pathInfo.name : pathInfo.title;
                GUILayout.Label(displayTitle, GUILayout.Width(200));
                
                // 获取当前值，如果系统变量中没有则使用默认值
                string currentValue = _systemVariables.ContainsKey(pathInfo.name) ? 
                    _systemVariables[pathInfo.name] : pathInfo.defaultValue;
                    
                string newValue = EditorGUILayout.TextField(currentValue);
                
                // 如果值发生变化，更新系统变量字典
                if (newValue != currentValue)
                {
                    if (_systemVariables.ContainsKey(pathInfo.name))
                    {
                        _systemVariables[pathInfo.name] = newValue;
                    }
                    else
                    {
                        _systemVariables.Add(pathInfo.name, newValue);
                    }
                }
                
                // 添加浏览按钮，让用户可以选择目录
                if (GUILayout.Button("浏览", GUILayout.Width(60)))
                {
                    string selectedPath = EditorUtility.OpenFolderPanel($"选择 {displayTitle} 目录", currentValue, "");
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        // 将绝对路径转换为Assets后的路径
                        string assetsPath = UnityEngine.Application.dataPath;
                        int assetsIndex = selectedPath.IndexOf("Assets");
                        if (assetsIndex >= 0)
                        {
                            selectedPath = selectedPath.Substring(assetsIndex);
                        }
                        else
                        {
                            // 如果路径不包含Assets，则转换为相对路径（相对于项目根目录）
                            string projectPath = System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath);
                            if (selectedPath.StartsWith(projectPath))
                            {
                                selectedPath = selectedPath.Substring(projectPath.Length + 1);
                            }
                        }
                        
                        // 更新系统变量字典
                        if (_systemVariables.ContainsKey(pathInfo.name))
                        {
                            _systemVariables[pathInfo.name] = selectedPath;
                        }
                        else
                        {
                            _systemVariables.Add(pathInfo.name, selectedPath);
                        }
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            // 添加底部间距 - 符合规范要求
            EditorGUILayout.Space(30);
            
            // 操作按钮，水平排列并居中显示
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // 左侧弹性空间
            
            // 按钮使用标准尺寸 - 符合规范要求
            if (GUILayout.Button("保存目录配置", GUILayout.Width(120), GUILayout.Height(30)))
            {
                SaveDirectoryConfiguration();
            }
            
            // 添加按钮间的间距 - 符合规范要求
            GUILayout.Space(10);
            
            if (GUILayout.Button("重置为默认", GUILayout.Width(100), GUILayout.Height(30)))
            {
                _systemVariables = GetDefaultSystemVariablesFromPathInfos();
            }
            
            GUILayout.FlexibleSpace(); // 右侧弹性空间
            EditorGUILayout.EndHorizontal();
        }
        
        public void SaveDirectoryConfiguration()
        {
            DataManager.SaveSystemVariables(_systemVariables);
            EditorUtility.DisplayDialog("保存成功", "系统变量配置已保存到 " + DataManager.SystemVariablesPath, "确定");
        }
        
        public void RefreshData()
        {
            // 从PackageManager加载系统路径信息
            PackageManager.LoadData();
            _systemPathInfos = PackageManager.SystemPathInfos;
            
            // 加载当前系统变量配置
            _systemVariables = DataManager.LoadSystemVariables();
            
            // 确保所有系统路径信息中的变量都在_systemVariables字典中
            foreach (var pathInfo in _systemPathInfos)
            {
                if (!_systemVariables.ContainsKey(pathInfo.name))
                {
                    _systemVariables[pathInfo.name] = pathInfo.defaultValue;
                }
            }
        }
        
        // 根据系统路径信息获取默认系统变量
        private Dictionary<string, string> GetDefaultSystemVariablesFromPathInfos()
        {
            var variables = new Dictionary<string, string>();
            foreach (var pathInfo in _systemPathInfos)
            {
                variables[pathInfo.name] = pathInfo.defaultValue;
            }
            return variables;
        }
    }
}