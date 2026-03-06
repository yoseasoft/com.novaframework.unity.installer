using UnityEditor;
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    /// <summary>
    /// 自动安装进度窗口 - GUI 绘制
    /// </summary>
    internal partial class AutoInstallProgressWindow
    {
        private GUIStyle _titleStyle;
        private GUIStyle _stepStyle;
        private GUIStyle _logStyle;
        private GUIStyle _errorStyle;
        private bool _stylesInitialized;

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

            _logStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                richText = true,
                wordWrap = true
            };

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
            EditorGUILayout.LabelField("Nova Framework 自动安装", _titleStyle);
            EditorGUILayout.Space(15);

            DrawStepInfo();
            DrawProgressBar();
            DrawLogArea();
            DrawBottomArea();

            EditorGUILayout.Space(10);
        }

        private void DrawStepInfo()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                string stepText = string.IsNullOrEmpty(_currentStepDescription) ? "准备中..." : _currentStepDescription;
                string stepProgress = _currentStep > 0 ? $" ({(int)_currentStep}/{(int)InstallStep.Complete})" : "";
                EditorGUILayout.LabelField(stepText + stepProgress, _stepStyle);
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(10);
        }

        private void DrawProgressBar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(20);
                Rect progressRect = EditorGUILayout.GetControlRect(GUILayout.Height(25));
                EditorGUI.ProgressBar(progressRect, _progress, $"{(_progress * 100):F0}%");
                GUILayout.Space(20);
            }

            EditorGUILayout.Space(15);
        }

        private void DrawLogArea()
        {
            EditorGUILayout.LabelField("安装日志", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandHeight(true)))
            {
                Vector2 newScrollPos = EditorGUILayout.BeginScrollView(_logScrollPosition);
                if (newScrollPos != _logScrollPosition)
                {
                    float scrollThreshold = newScrollPos.y - (_logScrollPosition.y + 10f);
                    _shouldScrollToBottom = scrollThreshold >= 0;
                }
                _logScrollPosition = newScrollPos;

                foreach (string log in _logs)
                {
                    EditorGUILayout.LabelField(log, _logStyle);
                }

                if (Event.current.type == EventType.Repaint && _shouldScrollToBottom)
                {
                    _logScrollPosition.y = Mathf.Infinity;
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(10);
        }

        private void DrawBottomArea()
        {
            if (_hasError)
            {
                EditorGUILayout.LabelField("安装过程中出现错误", _errorStyle);
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);

                if (GUILayout.Button("关闭", GUILayout.Height(30)))
                    Close();
            }
            else if (_isComplete)
            {
                GUIStyle greenButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold
                };
                greenButtonStyle.normal.textColor = new Color(0.2f, 0.8f, 0.2f);
                greenButtonStyle.hover.textColor = new Color(0.2f, 0.9f, 0.2f);

                if (GUILayout.Button("完成安装", greenButtonStyle, GUILayout.Height(35)))
                    Close();
            }
            else
            {
                EditorGUILayout.HelpBox("安装进行中，请勿关闭此窗口...", MessageType.Info);
            }
        }
    }
}
