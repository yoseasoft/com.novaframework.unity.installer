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
    internal class ConfigurationWindow : EditorWindow
    {
        private static ConfigurationWindow _window;
        
        private int _selectedTab = 0;
        private string[] _tabNames = { "插件配置", "环境目录配置", "程序集配置" };
        
        private PackageConfigurationView _packageView;
        private DirectoryConfigurationView _directoryView;
        private AssemblyConfigurationView _assemblyView;
        
     
        private bool _showWizardButtons = false; // 是否显示向导按钮
        private int _currentStep = 0;
       
        public static void ShowWindow()
        {
            _window = (ConfigurationWindow)GetWindow(typeof(ConfigurationWindow));
            _window.titleContent = new GUIContent("框架配置中心");
            _window.minSize = new Vector2(800, 700);
            _window.Show();
        }
        
        /// <summary>
        /// 通过反射调用的方法，用于外部包设置向导模式
        /// </summary>
        public static void SetWizardMode(bool show)
        {
            _window = (_window == null) ? (ConfigurationWindow)GetWindow(typeof(ConfigurationWindow)) : _window;
            if (_window != null)
            {
                _window._showWizardButtons = show;
                _window._currentStep = 0; // 重置步骤到开始位置
            }
        }
        
        /// <summary>
        /// 启动自动配置向导
        /// </summary>
        public static void StartAutoConfiguration()
        {
            SetWizardMode(true);
            ShowWindow();
        }
        
      
        void OnEnable()
        {
            //重新加载数据
            PackageManager.LoadData();
            
            _packageView = new PackageConfigurationView();
            _directoryView = new DirectoryConfigurationView();
            _assemblyView = new AssemblyConfigurationView();
        }
        
        void OnGUI()
        {
            // 添加标题
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 18; // 恢复正常字体大小
            titleStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("框架配置中心", titleStyle);
            EditorGUILayout.Space(10);
            
            // 标签页选择，恢复正常样式
            GUIStyle tabStyle = new GUIStyle(GUI.skin.button);
            tabStyle.fontSize = 14; // 恢复正常标签字体
            tabStyle.fixedHeight = 30;
            
            // 如果正在显示向导按钮，则锁定标签页显示
            if (_showWizardButtons)
            {
                // 显示当前配置步骤的标签页
                _selectedTab = GUILayout.Toolbar(0, new string[] { _tabNames[_currentStep] }, tabStyle);
                
                EditorGUILayout.Space(5); // 添加一点间距，让内容与标签分开
                
                // 根据当前步骤显示对应的配置界面
                switch (_currentStep)
                {
                    case 0:
                        _packageView.DrawView();
                        break;
                    case 1:
                        _directoryView.DrawView();
                        break;
                    case 2:
                        if (_assemblyView != null)
                        {
                            _assemblyView.DrawView();
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("程序集配置视图未初始化", MessageType.Error);
                        }
                        break;
                }
                
                EditorGUILayout.Space(10);
                
                // 显示当前步骤信息
                string currentStepInfo = GetWizardStepInfo();
                EditorGUILayout.HelpBox(currentStepInfo, MessageType.Info);
                
                EditorGUILayout.Space(10);
                
                // 显示向导按钮
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                if (_currentStep < 2) // 前两步显示"下一步"
                {
                    if (GUILayout.Button("下一步", GUILayout.Width(120), GUILayout.Height(30)))
                    {
                        ExecuteCurrentStep();
                        _currentStep++;
                    }
                }
                else if (_currentStep == 2) // 最后一步显示"完成"
                {
                    if (GUILayout.Button("完成", GUILayout.Width(120), GUILayout.Height(30)))
                    {
                        ExecuteCurrentStep(); // 执行最后一步
                        FinishAutoConfiguration();
                    }
                }
                
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // 正常模式下显示所有标签页
                _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames, tabStyle);
                
                EditorGUILayout.Space(5); // 添加一点间距，让内容与标签分开
                
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
                        if (_assemblyView != null)
                        {
                            _assemblyView.DrawView();
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("程序集配置视图未初始化", MessageType.Error);
                        }
                        break;
                }
            }
            
          
        }
        
        private string GetWizardStepInfo()
        {
            switch (_currentStep)
            {
                case 0:
                    return "第一步：请检查并确认插件配置。点击‘下一步’将自动保存当前配置。";
                case 1:
                    return "第二步：请检查并确认环境目录配置。点击‘下一步’将自动保存当前配置。";
                case 2:
                    return "第三步：请检查并确认程序集配置。点击‘完成’将保存配置并导出配置，结束后提示运行游戏。";
                default:
                    return "配置向导";
            }
        }
        
        private void ExecuteCurrentStep()
        {
            switch (_currentStep)
            {
                case 0: // 第一步：处理插件配置
                    // 在插件配置视图中自动保存（通过调用相应方法）
                    GitManager.HandleSelectPackages(DataManager.LoadPersistedSelectedPackages(), PackageManager.GetSelectedPackageNames());
                    DataManager.SavePersistedSelectedPackages(PackageManager.GetSelectedPackageNames());
                    UnityEditor.PackageManager.Client.Resolve();
                    Debug.Log("自动配置：完成插件配置并保存");
                    break;
                case 1: // 第二步：处理环境目录配置
                    // 保存目录配置
                    _directoryView.SaveDirectoryConfiguration();
                    Debug.Log("自动配置：完成环境目录配置并保存");
                    break;
                case 2: // 第三步：处理程序集配置
                    // 保存程序集配置
                    _assemblyView.SaveAssemblyConfiguration();
                    Debug.Log("自动配置：完成程序集配置并保存");
                    break;
            }
        }
        
        private void FinishAutoConfiguration()
        {
            // 先导出配置
            ExportConfigurationMenu.ExportConfiguration();
            Debug.Log("自动配置：完成配置导出");
            
            _showWizardButtons = false; // 隐藏向导按钮
            
            // 提示用户配置已完成
            EditorUtility.DisplayDialog("配置完成", "框架配置已全部完成！\n现在您可以开始运行游戏了。", "确定");
        }
        
      
    }
}