using UnityEditor;
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    internal class ConfigurationWindow : EditorWindow
    {
        private static ConfigurationWindow _window;
        private PackageConfigurationView _packageView;

        public static void ShowWindow()
        {
            _window = GetWindow<ConfigurationWindow>();
            _window.titleContent = new GUIContent("框架配置中心");
            _window.minSize = new Vector2(800, 700);
            _window.Show();
        }

        void OnEnable()
        {
            PackageManager.LoadData();
            _packageView = new PackageConfigurationView();
        }

        void OnGUI()
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("框架配置中心", titleStyle);
            EditorGUILayout.Space(10);

            _packageView.DrawView();
        }
    }
}
