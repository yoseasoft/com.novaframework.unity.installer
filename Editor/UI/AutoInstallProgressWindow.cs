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
using UnityEditor;
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    /// <summary>
    /// 自动安装进度窗口
    /// </summary>
    internal class AutoInstallProgressWindow : EditorWindow
    {
        // 安装步骤定义
        public enum InstallStep
        {
            None,
            CheckEnvironment,    // 检查环境
            LoadPackageInfo,     // 加载包信息
            InstallPackages,     // 安装插件包
            CreateDirectories,   // 创建目录结构
            InstallBasePack,     // 安装基础包
            CopyAotLibraries,    // 复制AOT库
            GenerateConfig,      // 生成配置
            CopyResources,       // 复制资源文件
            ExportConfig,        // 导出配置
            OpenScene,           // 打开场景
            Complete             // 完成
        }

        // 步骤描述
        private static readonly Dictionary<InstallStep, string> StepDescriptions = new Dictionary<InstallStep, string>
        {
            { InstallStep.None, "准备中..." },
            { InstallStep.CheckEnvironment, "正在检查环境..." },
            { InstallStep.LoadPackageInfo, "正在加载包信息..." },
            { InstallStep.InstallPackages, "正在安装插件包..." },
            { InstallStep.CreateDirectories, "正在创建目录结构..." },
            { InstallStep.InstallBasePack, "正在安装基础包..." },
            { InstallStep.CopyAotLibraries, "正在复制AOT库文件..." },
            { InstallStep.GenerateConfig, "正在生成框架配置..." },
            { InstallStep.CopyResources, "正在复制资源文件..." },
            { InstallStep.ExportConfig, "正在导出配置..." },
            { InstallStep.OpenScene, "正在打开主场景..." },
            { InstallStep.Complete, "安装完成！" }
        };

        // 单例实例
        private static AutoInstallProgressWindow _instance;
        
        // 当前状态
        private InstallStep _currentStep = InstallStep.None;
        private string _currentDetail = "";
        private float _progress = 0f;
        private int _currentPackageIndex = 0;
        private int _totalPackageCount = 0;
        private List<string> _logs = new List<string>();
        private Vector2 _logScrollPosition;
        private bool _shouldScrollToBottom = true; // 是否应该滚动到底部
        private bool _isComplete = false;
        private bool _hasError = false;
        private string _errorMessage = "";

        // 动画相关
        private string _activityIndicator = "";

        // GUI样式
        private GUIStyle _titleStyle;
        private GUIStyle _stepStyle;
        private GUIStyle _detailStyle;
        private GUIStyle _logStyle;
        private GUIStyle _successStyle;
        private GUIStyle _errorStyle;
        private bool _stylesInitialized = false;
        


        /// <summary>
        /// 显示进度窗口
        /// </summary>
        public static AutoInstallProgressWindow ShowWindow()
        {
            if (_instance != null)
            {
                _instance.Close();
            }

            _instance = GetWindow<AutoInstallProgressWindow>(true, "自动安装进度", true);
            
            // 设置窗口大小
            Vector2 windowSize = new Vector2(520, 420);
            _instance.minSize = windowSize;
            _instance.maxSize = new Vector2(600, 500);
            
            // 将窗口居中显示（稍微偏左上，避免和Unity包管理弹窗重叠）
            Vector2 screenCenter = new Vector2(Screen.currentResolution.width / 2f, Screen.currentResolution.height / 2f);
            float offsetX = -100f; // 偏左
            float offsetY = -50f;  // 偏上
            Rect windowRect = new Rect(
                screenCenter.x - windowSize.x / 2f + offsetX,
                screenCenter.y - windowSize.y / 2f + offsetY,
                windowSize.x,
                windowSize.y
            );
            _instance.position = windowRect;
            
            _instance.Reset();
            _instance.ShowUtility();
            
            // 强制重绘确保界面显示
            _instance.Repaint();
            
            return _instance;
        }

        /// <summary>
        /// 关闭进度窗口
        /// </summary>
        public static void CloseWindow()
        {
            if (_instance != null)
            {
                _instance.Close();
                _instance = null;
            }
        }

        /// <summary>
        /// 获取当前实例
        /// </summary>
        public static AutoInstallProgressWindow Instance => _instance;

        /// <summary>
        /// 重置状态
        /// </summary>
        private void Reset()
        {
            _currentStep = InstallStep.None;
            _currentDetail = "";
            _progress = 0f;
            _currentPackageIndex = 0;
            _totalPackageCount = 0;
            _logs.Clear();
            _isComplete = false;
            _hasError = false;
            _errorMessage = "";
        }

        /// <summary>
        /// 设置当前步骤
        /// </summary>
        public void SetStep(InstallStep step, string detail = "")
        {
            _currentStep = step;
            _currentDetail = detail;
            
            // 计算进度
            int stepIndex = (int)step;
            int totalSteps = Enum.GetValues(typeof(InstallStep)).Length - 1; // 减去None
            _progress = (float)stepIndex / totalSteps;

            // 添加日志
            string logMessage = StepDescriptions.ContainsKey(step) ? StepDescriptions[step] : step.ToString();
            if (!string.IsNullOrEmpty(detail))
            {
                logMessage += " " + detail;
            }
            AddLog(logMessage);

            if (step == InstallStep.Complete)
            {
                _isComplete = true;
                _progress = 1f;
            }

            Repaint();
        }

        /// <summary>
        /// 设置包安装进度
        /// </summary>
        public void SetPackageProgress(int currentIndex, int totalCount, string packageName)
        {
            _currentPackageIndex = currentIndex;
            _totalPackageCount = totalCount;
            _currentDetail = $"({currentIndex}/{totalCount}) {packageName}";
            
            // 计算包安装阶段的进度（InstallPackages步骤占总进度的一部分）
            // InstallPackages 是第3步，总共11步
            // 包安装阶段进度范围：3/11 到 4/11 (约27% 到 36%)
            float baseProgress = 3f / 11f;  // InstallPackages 步骤的基础进度
            float stepRange = 1f / 11f;     // 每个步骤占的进度范围
            float packageProgress = (float)currentIndex / totalCount;
            _progress = baseProgress + (stepRange * packageProgress);
            
            AddLog($"  正在配置: {packageName}");
            Repaint();
        }

        /// <summary>
        /// 添加日志
        /// </summary>
        public void AddLog(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            _logs.Add($"[{timestamp}] {message}");
            
            // 限制日志数量
            if (_logs.Count > 100)
            {
                _logs.RemoveAt(0);
            }
            
            // 添加新日志时，应该自动滚动到底部
            _shouldScrollToBottom = true;
            
            Repaint();
        }

        /// <summary>
        /// 设置错误状态
        /// </summary>
        public void SetError(string errorMessage)
        {
            _hasError = true;
            _errorMessage = errorMessage;
            AddLog($"错误: {errorMessage}");
            Repaint();
        }

        /// <summary>
        /// 初始化GUI样式
        /// </summary>
        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            _stepStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };

            _detailStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };

            _logStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                richText = true,
                wordWrap = true
            };

            _successStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            _successStyle.normal.textColor = new Color(0.2f, 0.8f, 0.2f);

            _errorStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            _errorStyle.normal.textColor = new Color(0.9f, 0.2f, 0.2f);

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.Space(10);

            // 标题
            EditorGUILayout.LabelField("Nova Framework 自动安装", _titleStyle);
            
            EditorGUILayout.Space(15);

            // 当前步骤
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                string stepText = StepDescriptions.ContainsKey(_currentStep) ? StepDescriptions[_currentStep] : "准备中...";
                
                // 添加步骤进度信息（例如 3/11）
                int stepIndex = (int)_currentStep;
                int totalSteps = Enum.GetValues(typeof(InstallStep)).Length - 1; // 减去None
                string stepProgress = $" ({stepIndex}/{totalSteps})";
                if (stepIndex <= 0) stepProgress = ""; // 不显示 0/11 或负数
                
                EditorGUILayout.LabelField(stepText + stepProgress, _stepStyle);
                GUILayout.FlexibleSpace();
            }

            // 仅在非包安装步骤时显示详细信息
            if (!string.IsNullOrEmpty(_currentDetail) && _currentStep != InstallStep.InstallPackages)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    string detailText = _currentDetail;
                    if (!_isComplete && !_hasError) // 只在安装进行时添加动画指示器
                    {
                        detailText += _activityIndicator; // 将点动画直接附加到文本末尾
                    }
                    EditorGUILayout.LabelField(detailText, _detailStyle);
                    GUILayout.FlexibleSpace();
                }
            }

            EditorGUILayout.Space(10);

            // 进度条
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(20);
                Rect progressRect = EditorGUILayout.GetControlRect(GUILayout.Height(25));
                EditorGUI.ProgressBar(progressRect, _progress, $"{(_progress * 100):F0}%");
                GUILayout.Space(20);
            }

            // 包安装进度文本（放在进度条下方）
            if (_currentStep == InstallStep.InstallPackages && _totalPackageCount > 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    string packageProgressText = $"{_currentDetail}";
                    EditorGUILayout.LabelField(packageProgressText, _detailStyle);
                    GUILayout.FlexibleSpace();
                }
            }
            else if (!string.IsNullOrEmpty(_currentDetail) && _currentStep != InstallStep.InstallPackages)
            {
                // 只在非包安装步骤时显示详情文本
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    string detailText = _currentDetail;
                    if (!_isComplete && !_hasError) // 只在安装进行时添加动画指示器
                    {
                        detailText += _activityIndicator; // 将点动画直接附加到文本末尾
                    }
                    EditorGUILayout.LabelField(detailText, _detailStyle);
                    GUILayout.FlexibleSpace();
                }
            }

            EditorGUILayout.Space(15);

            // 日志区域
            EditorGUILayout.LabelField("安装日志", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandHeight(true)))
            {
                // 监听滚动变化，如果用户滚动则停止自动滚动到底部
                Vector2 newScrollPos = EditorGUILayout.BeginScrollView(_logScrollPosition);
                if (newScrollPos != _logScrollPosition)
                {
                    // 滚动位置改变，检查是否滚动到底部
                    float scrollThreshold = newScrollPos.y - (_logScrollPosition.y + 10f); // 10f为容差
                    _shouldScrollToBottom = scrollThreshold >= 0; // 如果是向下滚动，则继续自动滚动
                }
                _logScrollPosition = newScrollPos;
                
                foreach (string log in _logs)
                {
                    EditorGUILayout.LabelField(log, _logStyle);
                }
                
                // 仅在需要时自动滚动到底部
                if (Event.current.type == EventType.Repaint && _shouldScrollToBottom)
                {
                    _logScrollPosition.y = Mathf.Infinity;
                }
                
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(10);

            // 状态显示和按钮
            if (_hasError)
            {
                EditorGUILayout.LabelField("安装过程中出现错误", _errorStyle);
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
                
                if (GUILayout.Button("关闭", GUILayout.Height(30)))
                {
                    Close();
                }
            }
            else if (_isComplete)
            {
                // 移除了"安装完成！"的文本显示
                               
                // 创建绿色按钮样式
                GUIStyle greenButtonStyle = new GUIStyle(GUI.skin.button);
                greenButtonStyle.normal.textColor = new Color(0.2f, 0.8f, 0.2f); // 绿色
                greenButtonStyle.hover.textColor = new Color(0.2f, 0.9f, 0.2f); // 悬停时更亮的绿色
                greenButtonStyle.fontSize = 14; // 增大字体
                greenButtonStyle.fontStyle = FontStyle.Bold;
                
                if (GUILayout.Button("完成安装", greenButtonStyle, GUILayout.Height(35)))
                {
                    Close();
                }
            }
            else
            {
                // 安装进行中，显示提示
                EditorGUILayout.HelpBox("安装进行中，请勿关闭此窗口...", MessageType.Info);
            }

            EditorGUILayout.Space(10);
            
        
        }

        
        private void OnDestroy()
        {

            _instance = null;
            
            // 检查安装是否完成，如果完成则打开配置中心
            if (_isComplete && !_hasError)
            {
                // 延迟调用以确保当前窗口完全关闭
                EditorApplication.delayCall += () =>
                {
                    Debug.Log("安装完成，即将调用配置中心");
                    ConfigurationWindow.StartAutoConfiguration();
                };
            }
        }
    }
}