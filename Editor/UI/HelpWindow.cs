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
using UnityEditor.SceneManagement;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace NovaFramework.Editor.Installer
{
    public class HelpWindow : EditorWindow
    {
        private static HelpWindow _window;
        private Vector2 _scrollPosition;
        
        public static void ShowWindow()
        {
            _window = (HelpWindow)EditorWindow.GetWindow(typeof(HelpWindow));
            _window.titleContent = new GUIContent("框架安装器 - 使用指南");
            _window.minSize = new Vector2(700, 500);
            _window.Show();
        }
        
        void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            GUILayout.Label("框架安装器 - 使用指南", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 });
            EditorGUILayout.Space(10);
            
            GUILayout.Label("\n核心功能：", EditorStyles.boldLabel);
            
            GUILayout.Label("• 插件选择：选择需要的插件，系统自动处理依赖关系");
            GUILayout.Label("• 环境配置：设置框架所需的路径变量");
            GUILayout.Label("• 程序集配置：管理程序集的加载顺序和标签");
            
            EditorGUILayout.Space(10);
            
            GUILayout.Label("\n快速访问：", EditorStyles.boldLabel);
            
            GUILayout.Label("• 自动安装 (F8)：启动自动安装流程");
            GUILayout.Label("• 配置中心 (Alt+C or Cmd+Shift+C)：进行各项配置");
            GUILayout.Label("• 验证环境：检查配置是否正确");
            GUILayout.Label("• 检查更新：获取最新版本");
            
            EditorGUILayout.Space(10);
            
            GUILayout.Label("\n使用步骤：", EditorStyles.boldLabel);
            
            GUILayout.Label("1. 打开配置中心，选择所需插件");
            GUILayout.Label("2. 配置环境目录路径");
            GUILayout.Label("3. 设置程序集加载顺序");
            GUILayout.Label("4. 保存配置并重启Unity");
            
            EditorGUILayout.Space(10);
            
            GUILayout.Label("\n注意事项：", EditorStyles.boldLabel);
            
            GUILayout.Label("• 配置修改后需重启Unity方可完全生效");
            GUILayout.Label("• 程序集order值越小，加载优先级越高");
            GUILayout.Label("• 依赖插件会自动选中，取消前请确认无其他插件依赖");
            
            EditorGUILayout.Space(15);
            
            if (GUILayout.Button("打开配置中心", GUILayout.Height(35)))
            {
                ConfigurationWindow.ShowWindow();
            }
            
            EditorGUILayout.Space(8);
            
            if (GUILayout.Button("验证环境", GUILayout.Height(30)))
            {
                EnvironmentValidator.ShowValidationResult();
            }
            
            EditorGUILayout.Space(8);
            
            if (GUILayout.Button("关闭", GUILayout.Height(30)))
            {
                Close();
            }
            
            EditorGUILayout.EndScrollView();
        }
    }
}