using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    /// <summary>
    /// 自动安装进度窗口 - 窗口生命周期和状态管理
    /// GUI 绘制见 AutoInstallProgressWindow.GUI.cs
    /// </summary>
    internal partial class AutoInstallProgressWindow : EditorWindow
    {
        private static AutoInstallProgressWindow _instance;

        // 当前状态
        private InstallStep _currentStep = InstallStep.None;
        private string _currentStepDescription = "";
        private string _currentDetail = "";
        private float _progress;
        private List<string> _logs = new List<string>();
        private Vector2 _logScrollPosition;
        private bool _shouldScrollToBottom = true;
        private bool _isComplete;
        private bool _hasError;
        private string _errorMessage = "";

        public Action<bool> OnClosed;

        public static AutoInstallProgressWindow Instance => _instance;

        private void OnEnable()
        {
            _instance = this;
        }

        public static AutoInstallProgressWindow ShowWindow()
        {
            if (_instance != null)
                _instance.Close();

            _instance = GetWindow<AutoInstallProgressWindow>(true, "自动安装进度", true);

            Vector2 windowSize = new Vector2(520, 420);
            _instance.minSize = windowSize;
            _instance.maxSize = new Vector2(600, 500);

            Vector2 screenCenter = new Vector2(Screen.currentResolution.width / 2f, Screen.currentResolution.height / 2f);
            _instance.position = new Rect(
                screenCenter.x - windowSize.x / 2f - 100f,
                screenCenter.y - windowSize.y / 2f - 50f,
                windowSize.x,
                windowSize.y
            );

            _instance.Reset();
            _instance.ShowUtility();
            _instance.Repaint();

            return _instance;
        }

        public static void CloseWindow()
        {
            if (_instance != null)
            {
                _instance.Close();
                _instance = null;
            }
        }

        public void SetStep(InstallStep step, int curStepIndex, int totalStepsNum, string stepDescription, string detail = "")
        {
            _currentStep = step;
            _currentStepDescription = stepDescription;
            _currentDetail = detail;
            _progress = (float)curStepIndex / totalStepsNum;

            string logMessage = stepDescription;
            if (!string.IsNullOrEmpty(detail))
                logMessage += " " + detail;
            AddLog(logMessage);

            if (step == InstallStep.Complete)
            {
                _isComplete = true;
                _progress = 1f;
            }

            Repaint();
        }

        public void AddLog(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            _logs.Add($"[{timestamp}] {message}");

            if (_logs.Count > 100)
                _logs.RemoveAt(0);

            _shouldScrollToBottom = true;
            Repaint();
        }

        public void SetError(string errorMessage)
        {
            _hasError = true;
            _errorMessage = errorMessage;
            AddLog($"错误: {errorMessage}");
            Repaint();
        }

        private void Reset()
        {
            _currentStep = InstallStep.None;
            _currentStepDescription = "";
            _currentDetail = "";
            _progress = 0f;
            _logs.Clear();
            _isComplete = false;
            _hasError = false;
            _errorMessage = "";
        }

        private void OnDestroy()
        {
            _instance = null;
            OnClosed?.Invoke(_isComplete && !_hasError);
        }
    }
}
