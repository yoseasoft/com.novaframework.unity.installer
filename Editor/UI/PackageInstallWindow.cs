using UnityEditor;
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    internal class PackageInstallWindow : EditorWindow
    {
        private static PackageInstallWindow _window;
        private PackageConfigurationView _packageView;

        public static void ShowWindow()
        {
            _window = GetWindow<PackageInstallWindow>();
            _window.titleContent = new GUIContent("Package安装中心");
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
            GUILayout.Label("包安装中心", titleStyle);
            EditorGUILayout.Space(10);

            _packageView.DrawView();
        }
    }
}
