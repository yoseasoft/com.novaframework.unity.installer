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
using UnityEngine.SceneManagement;

namespace NovaFramework.Editor.Installer
{
    public class ConfigurationWindow : EditorWindow
    {
        private static ConfigurationWindow _window;
        
        private int _selectedTab = 0;
        private string[] _tabNames = { "插件配置", "环境目录配置", "程序集配置" };
        
        private PackageConfigurationView _packageView;
        private DirectoryConfigurationView _directoryView;
        private AssemblyConfigurationView _assemblyView;
        
        public static void ShowWindow()
        {
            _window = (ConfigurationWindow)EditorWindow.GetWindow(typeof(ConfigurationWindow));
            _window.titleContent = new GUIContent("框架配置中心");
            _window.minSize = new Vector2(800, 700);
            _window.Show();
        }
        
        void OnEnable()
        {
            _packageView = new PackageConfigurationView();
            _directoryView = new DirectoryConfigurationView();
            _assemblyView = new AssemblyConfigurationView();
        }
        
        void OnGUI()
        {
            // 添加大号标题
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 24;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("框架配置中心", titleStyle);
            EditorGUILayout.Space(10);
            
            // 标签页选择，增加高度和字体大小
            GUIStyle tabStyle = new GUIStyle(GUI.skin.button);
            tabStyle.fontSize = 16;
            tabStyle.fixedHeight = 35;
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames, tabStyle);
            EditorGUILayout.Space(10);
            
            // 根据选中的标签页显示不同内容
            switch (_selectedTab)
            {
                case 0:
                    _packageView.DrawView();
                    break;
                case 1:
                    _directoryView.DrawView();
                    break;
                case 2:
                    _assemblyView.DrawView();
                    break;
            }

        }
             
    }
}