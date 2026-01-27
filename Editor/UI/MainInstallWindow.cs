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
using UnityEditor;
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    public class MainInstallWindow : EditorWindow
    {
        private static MainInstallWindow _window;
        private bool _isInstalling = false;
        private AutoInstallManager.InstallProgress _currentProgress;
        
        public static void ShowWindow()
        {
            _window = (MainInstallWindow)EditorWindow.GetWindow(typeof(MainInstallWindow));
            _window.titleContent = new GUIContent("自动安装");
            _window.minSize = new Vector2(500, 300);
            _window.Show();
        }
        
        void OnGUI()
        {
            GUILayout.Label("框架自动安装", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 });
            EditorGUILayout.Space(20);
            
            if (!_isInstalling)
            {
                GUILayout.Label("点击下面的按钮开始自动安装框架：", EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space(10);
                
                if (GUILayout.Button("开始自动安装", GUILayout.Height(40)))
                {
                    StartAutoInstall();
                }
            }
            else
            {
                // 显示安装进度
                if (_currentProgress != null)
                {
                    GUILayout.Label(_currentProgress.CurrentStep, EditorStyles.boldLabel);
                    EditorGUILayout.Space(5);
                    
                    GUILayout.Label(_currentProgress.StatusMessage, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.Space(10);
                    
                    // 进度条
                    Rect progressRect = EditorGUILayout.GetControlRect();
                    progressRect.height = 20;
                    EditorGUI.ProgressBar(progressRect, _currentProgress.Progress, 
                        $"{Mathf.RoundToInt(_currentProgress.Progress * 100)}%");
                    
                    EditorGUILayout.Space(10);
                    
                    if (_currentProgress.IsCompleted)
                    {
                        GUILayout.Label("安装完成！", EditorStyles.boldLabel);
                        if (GUILayout.Button("关闭", GUILayout.Height(30)))
                        {
                            _isInstalling = false;
                            _currentProgress = null;
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("取消安装", GUILayout.Height(30)))
                        {
                            _isInstalling = false;
                            _currentProgress = null;
                        }
                    }
                }
            }
            
            EditorGUILayout.Space(20);
            
            // 显示当前配置状态
            DrawStatusInfo();
        }
        
        void StartAutoInstall()
        {
            _isInstalling = true;
            _currentProgress = null;
            
            // 在后台线程中执行安装
            EditorApplication.delayCall += () =>
            {
                AutoInstallManager.StartAutoInstall(UpdateProgress);
            };
        }
        
        void UpdateProgress(AutoInstallManager.InstallProgress progress)
        {
            _currentProgress = progress;
            
            // 在主线程中更新UI
            EditorApplication.delayCall += () =>
            {
                Repaint();
                
                if (progress.IsCompleted)
                {
                    _isInstalling = false;
                }
            };
        }
        
        void DrawStatusInfo()
        {
            GUILayout.Label("当前配置状态：", EditorStyles.boldLabel);
            
            // 检查框架设置
            var frameworkSetting = DataManager.LoadFrameworkSetting();
            GUILayout.Label($"框架设置: {(frameworkSetting.selectedPackages.Count > 0 ? "已配置" : "未配置")}");
            
            // 检查系统变量
            bool isSystemVariablesConfigured = DataManager.IsSystemVariablesConfigured();
            GUILayout.Label($"系统变量: {(isSystemVariablesConfigured ? "已配置" : "未配置")}");
            
            // 检查程序集配置
            var assemblyConfigs = DataManager.LoadAssemblyConfig();
            GUILayout.Label($"程序集配置: {(assemblyConfigs.Count > 0 ? "已配置" : "未配置")}");
        }
    }
}