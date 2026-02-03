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
using NovaFramework.Editor.Manifest;
using UnityEditor;
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    internal class DirectoryConfigurationView
    {
        // 环境目录配置相关
        private Dictionary<string, string> _systemVariables;
        private List<LocalPathObject> _systemPathInfos; // 从PackageManager获取的系统路径信息
        private Vector2 _dirScrollPos;
        private double _lastSaveTime = 0;
        private const double SAVE_DELAY = 0.3; // 300毫秒延迟


        public DirectoryConfigurationView()
        {
            RefreshData();
        }

        public void DrawView()
        {
            // 使用标准的布局，不添加额外的边框
            EditorGUILayout.HelpBox("在此处可以修改系统变量目录配置", MessageType.Info);
            
            // 添加标准间距
            EditorGUILayout.Space(10);
            
            // 使用固定高度而不是ExpandHeight(true)，确保为底部按钮预留空间
            float availableHeight = Mathf.Max(200, Mathf.Min(400, Screen.height * 0.5f)); // 限制最大高度，使用屏幕高度的50%，但最少200像素，最多400像素，确保内容可显示
            _dirScrollPos = EditorGUILayout.BeginScrollView(_dirScrollPos, GUILayout.Height(availableHeight));
            
            // 使用从PackageManager获取的系统路径信息来显示配置
            int pathIndex = 0; // 用于唯一标识每个输入框
            foreach (var pathInfo in _systemPathInfos)
            {
                EditorGUILayout.BeginHorizontal("box");
                
                // 显示路径信息的标题，如果没有标题则显示名称
                string displayTitle = string.IsNullOrEmpty(pathInfo.title) ? pathInfo.name : pathInfo.title;
                GUILayout.Label(displayTitle, GUILayout.Width(200));
                
                // 获取当前值，如果系统变量中没有则使用默认值
                string currentValue = _systemVariables.ContainsKey(pathInfo.name) ? 
                    _systemVariables[pathInfo.name] : pathInfo.defaultValue;
                    
                // 生成唯一的文本框ID
                string textFieldId = pathInfo.name + "_textField";
                GUI.SetNextControlName(textFieldId);
                
                // 显示当前路径，但不允许直接编辑
                string newValue = EditorGUILayout.TextField(currentValue, GUILayout.ExpandWidth(true));
                
                // 更新显示值到字典并标记需要保存
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
                    
                    // 延迟保存更改
                    DelaySaveDirectoryConfiguration();
                }
                
                pathIndex++; // 递增索引
                
                // 添加浏览按钮，让用户可以选择目录
                if (GUILayout.Button("浏览", GUILayout.Width(60)))
                {
                    string selectedPath = EditorUtility.OpenFolderPanel($"选择 {displayTitle} 目录", newValue, "");
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        // 将绝对路径转换为Assets后的路径
                        int assetsIndex = selectedPath.IndexOf("Assets");
                        if (assetsIndex >= 0)
                        {
                            selectedPath = selectedPath.Substring(assetsIndex);
                        }
                        else
                        {
                            // 如果路径不包含Assets，则转换为相对路径（相对于项目根目录）
                            string projectPath = Path.GetDirectoryName(Application.dataPath);
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
                        
                        // 自动保存更改
                        SaveDirectoryConfiguration();
                        
                        // 强制重绘界面以更新显示
                        GUI.changed = true;
                        
                        // 标记GUI已更改以确保更新
                        GUI.changed = true;
                        
                        // 立即强制重绘当前窗口
                        if (EditorWindow.focusedWindow != null)
                        {
                            EditorWindow.focusedWindow.Repaint();
                        }
                        
                        // 使用 delayCall 确保在下一帧再次重绘
                        EditorApplication.delayCall += () =>
                        {
                            if (EditorWindow.focusedWindow != null)
                            {
                                EditorWindow.focusedWindow.Repaint();
                            }
                        };
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            // 添加底部间距
            EditorGUILayout.Space(20); // 标准底部间距
            
            // 操作按钮，水平排列并居中显示
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // 左侧弹性空间
            
            // 添加按钮间的间距
            GUILayout.Space(10);
            
            if (GUILayout.Button("重置", GUILayout.Width(120), GUILayout.Height(30)))
            {
                _systemVariables = GetDefaultSystemVariablesFromPathInfos();
                // 自动保存重置后的配置
                SaveDirectoryConfiguration();
            }
            
            GUILayout.FlexibleSpace(); // 右侧弹性空间
            EditorGUILayout.EndHorizontal();
        }
        
        public void SaveDirectoryConfiguration()
        {
            // 保存到CoreEngine.Editor.UserSettings
            UserSettings.SetObject(Constants.NovaFramework_Installer_DIRECTORY_CONFIG_KEY, _systemVariables);
            
            // 不自动导出配置，仅保存到UserSettings
        }
        

        
        public void RefreshData()
        {
            // 从PackageManager加载系统路径信息
            PackageManager.LoadData();
            _systemPathInfos = PackageManager.SystemPathInfos;
            
            // 优先从CoreEngine.Editor.UserSettings加载配置，如果不存在则从DataManager加载
            try
            {
                _systemVariables = UserSettings.GetObject<Dictionary<string, string>>(Constants.NovaFramework_Installer_DIRECTORY_CONFIG_KEY);
            }
            catch (Exception e) { 
                string errorMessage = e.Message;
                _systemVariables = DataManager.LoadSystemVariables();
            }
            
            if (_systemVariables == null)
            {
                _systemVariables = DataManager.LoadSystemVariables();
            }
            
            // 确保所有系统路径信息中的变量都在_systemVariables字典中
            foreach (var pathInfo in _systemPathInfos)
            {
                if (!_systemVariables.ContainsKey(pathInfo.name))
                {
                    // 只有required=true的路径才设值，否则就置空
                    _systemVariables[pathInfo.name] = pathInfo.required ? pathInfo.defaultValue : "";
                }
            }
        }
        
        // 根据系统路径信息获取默认系统变量
        private Dictionary<string, string> GetDefaultSystemVariablesFromPathInfos()
        {
            var variables = new Dictionary<string, string>();
            foreach (var pathInfo in _systemPathInfos)
            {
                // 只有required=true的路径才设值，否则就置空
                variables[pathInfo.name] = pathInfo.required ? pathInfo.defaultValue : "";
            }
            return variables;
        }
        
        // 延迟保存配置，避免频繁保存
        private void DelaySaveDirectoryConfiguration()
        {
            _lastSaveTime = EditorApplication.timeSinceStartup + SAVE_DELAY;
            EditorApplication.update += PerformDelayedSave;
        }
        
        private void PerformDelayedSave()
        {
            if (EditorApplication.timeSinceStartup >= _lastSaveTime)
            {
                EditorApplication.update -= PerformDelayedSave;
                SaveDirectoryConfiguration();
            }
        }
        
    }
}