using System;
using System.Collections.Generic;
using System.Linq;
using NovaFramework.Editor.Manifest;
using NovaFramework.Editor.Preference;
using UnityEditor;
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    /// <summary>
    /// 配置中心主窗口
    /// 左侧：已安装包的页签（显示 title）
    /// 右侧：选中包的 PreferenceWindow 子类页签及其绘制内容
    /// </summary>
    internal class ConfigurationCenterWindow : EditorWindow
    {
        // 左侧
        private List<ConfigurablePackage> _packages = new List<ConfigurablePackage>();
        private int _selectedPackageIndex;
        private Vector2 _leftScrollPos;

        // 右侧
        private int _selectedTabIndex;
        private Vector2 _rightScrollPos;

        // 缓存：包名 -> PreferenceWindow 实例列表
        private Dictionary<string, List<PreferenceWindow>> _preferenceCache = new Dictionary<string, List<PreferenceWindow>>();

        private GUIStyle _leftItemStyle;
        private GUIStyle _leftItemSelectedStyle;
        private bool _stylesInited;

        public static void ShowWindow()
        {
            var window = GetWindow<ConfigurationCenterWindow>();
            window.titleContent = new GUIContent("配置中心");
            window.minSize = new Vector2(900, 600);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshPackages();
        }

        private void RefreshPackages()
        {
            _packages.Clear();
            _preferenceCache.Clear();
            _selectedPackageIndex = 0;
            _selectedTabIndex = 0;

            PackageManager.LoadData();
            var selectedPackages = PackageManager.GetSelectedPackageObjects();

            foreach (var pkg in selectedPackages)
            {
                if (pkg.outputAssembliesObject?.localAssemblies == null) continue;

                var configurableModules = pkg.outputAssembliesObject.localAssemblies
                    .Where(m => m.configurable)
                    .ToList();

                if (configurableModules.Count > 0)
                {
                    _packages.Add(new ConfigurablePackage
                    {
                        packageObject = pkg,
                        configurableModules = configurableModules
                    });
                }
            }
        }

        private List<PreferenceWindow> GetOrCreatePreferences(ConfigurablePackage pkg)
        {
            if (_preferenceCache.TryGetValue(pkg.packageObject.name, out var cached))
                return cached;

            var preferences = new List<PreferenceWindow>();

            foreach (var module in pkg.configurableModules)
            {
                var types = AssemblyUtils.FindAllTypesFromAssembly<PreferenceWindow>(module.name, true);
                foreach (var type in types)
                {
                    // 使用 CreateInstance 创建 ScriptableObject 派生实例（EditorWindow 继承自 ScriptableObject）
                    var instance = CreateInstance(type) as PreferenceWindow;
                    if (instance != null)
                    {
                        instance.hideFlags = HideFlags.DontSave;
                        preferences.Add(instance);
                    }
                }
            }

            _preferenceCache[pkg.packageObject.name] = preferences;
            return preferences;
        }

        private void InitStyles()
        {
            if (_stylesInited) return;

            _leftItemStyle = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(10, 10, 8, 8),
                fontSize = 13
            };

            _leftItemSelectedStyle = new GUIStyle(_leftItemStyle);
            _leftItemSelectedStyle.normal.background = MakeTex(1, 1, new Color(0.24f, 0.48f, 0.9f, 0.6f));
            _leftItemSelectedStyle.normal.textColor = Color.white;
            _leftItemSelectedStyle.fontStyle = FontStyle.Bold;

            _stylesInited = true;
        }

        private void OnGUI()
        {
            InitStyles();

            if (_packages.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到可配置的包。请确保已安装包含 configurable 模块的包。", MessageType.Info);
                if (GUILayout.Button("刷新", GUILayout.Height(30)))
                    RefreshPackages();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(180), GUILayout.ExpandHeight(true));

            EditorGUILayout.LabelField("模块列表", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _leftScrollPos = EditorGUILayout.BeginScrollView(_leftScrollPos);

            for (int i = 0; i < _packages.Count; i++)
            {
                var style = (i == _selectedPackageIndex) ? _leftItemSelectedStyle : _leftItemStyle;
                if (GUILayout.Button(_packages[i].packageObject.title, style))
                {
                    if (_selectedPackageIndex != i)
                    {
                        _selectedPackageIndex = i;
                        _selectedTabIndex = 0;
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            var currentPkg = _packages[_selectedPackageIndex];
            var preferences = GetOrCreatePreferences(currentPkg);

            if (preferences.Count == 0)
            {
                EditorGUILayout.HelpBox($"「{currentPkg.packageObject.title}」没有找到 PreferenceWindow 实现。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // 右上角页签
            var tabNames = preferences.Select(p => p.PagingName).ToArray();
            _selectedTabIndex = Mathf.Clamp(_selectedTabIndex, 0, tabNames.Length - 1);

            GUIStyle tabStyle = new GUIStyle(EditorStyles.toolbarButton) { fontSize = 12, fixedHeight = 28 };
            _selectedTabIndex = GUILayout.Toolbar(_selectedTabIndex, tabNames, tabStyle);

            EditorGUILayout.Space(5);

            // 绘制选中页签的内容
            _rightScrollPos = EditorGUILayout.BeginScrollView(_rightScrollPos);
            preferences[_selectedTabIndex].OnDraw();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void OnDestroy()
        {
            // 清理缓存的实例
            foreach (var list in _preferenceCache.Values)
            {
                foreach (var pref in list)
                {
                    if (pref != null)
                        DestroyImmediate(pref);
                }
            }
            _preferenceCache.Clear();
        }

        private static Texture2D MakeTex(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            var tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private class ConfigurablePackage
        {
            public PackageObject packageObject;
            public List<ImportModuleObject> configurableModules;
        }
    }
}
