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
    internal class PackageConfigurationView
    {
        private Vector2 _scrollPosition;
        private Vector2 _selectedPackagesScrollPosition;
        private string _packageSearchFilter = ""; // 搜索过滤条件
        
        // 跟踪每个包的详情展开状态
        private Dictionary<string, bool> _packageDetailsVisibility = new Dictionary<string, bool>();
        
        public void DrawView()
        {
            DrawTopPanel();
            
            EditorGUILayout.Space(20); // 增加间距
            
            // 使用水平布局将包列表和已选包列表分开
            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();

            DrawButtomPanel();
        }

        private void DrawTopPanel()
        {
            EditorGUILayout.Space(20); // 增加间距
            
            GUILayout.Label("<color=#4CAF50> 选择需要安装的包</color>", RichTextUtils.GetBoldRichTextStyle(Color.white, 16), GUILayout.Height(30));
            EditorGUILayout.HelpBox("勾选需要安装的包，必需包已自动选中且无法取消。", MessageType.Info);
            EditorGUILayout.Space(10);
            
            // 添加搜索框
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("<color=#2196F3> 搜索包</color>", RichTextUtils.GetRichTextStyle(Color.white, 14), GUILayout.Width(60));
            int searchBoxWidth = Math.Max(100, (int)(Screen.width * 0.5 - 90)); // 使用屏幕宽度的50%
            string newSearchFilter = EditorGUILayout.TextField(_packageSearchFilter, GUILayout.Width(searchBoxWidth));
            if (newSearchFilter != _packageSearchFilter)
            {
                _packageSearchFilter = newSearchFilter;
            }
            
            GUIStyle clearButtonStyle = RichTextUtils.GetButtonTextOnlyStyle(Color.white);
            if (GUILayout.Button("清空", clearButtonStyle, GUILayout.Width(60)))
            { 
                _packageSearchFilter = ""; 
                GUI.FocusControl(null); // 移除焦点，强制重绘
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawLeftPanel()
        {
            // 左侧：所有包列表（带搜索过滤）
            EditorGUILayout.BeginVertical(GUILayout.Width(Screen.width * 0.55f)); // 增加左侧宽度到55%
            GUILayout.Label("<color=#FF9800>  所有包</color>", RichTextUtils.GetBoldRichTextStyle(Color.white, 14));
            
            // 滚动视图以容纳可能的大量包
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(470)); // 增加回50的高度（从420增加到470）
            
            // 获取过滤后的包列表
            var filteredPackages = PackageManager.GetFilteredPackages(_packageSearchFilter);
            
            foreach (var package in filteredPackages)
            {
                EditorGUILayout.BeginHorizontal("box");
                
                bool newSelection = package.isSelected;
                
                // 如果是必需包，显示灰色不可点击的勾选框
                if (package.isRequired)
                {
                    GUI.enabled = false; // 禁用交互
                    EditorGUILayout.Toggle(package.isSelected, GUILayout.Width(25));
                    GUI.enabled = true; // 恢复交互
                    
                    // 显示包信息，使用工具提示
                    string packageText = $"<color=#9E9E9E>{package.title}</color> <color=#FF9800>(必需)</color>"; // 按规范标注必需包
                    GUIContent label = new GUIContent(packageText, package.description);
                    GUILayout.Label(label, RichTextUtils.GetRichTextStyle(Color.white, 12), GUILayout.ExpandWidth(true));
                }
                else
                {
                    // 显示可点击的勾选框
                    newSelection = EditorGUILayout.Toggle(package.isSelected, GUILayout.Width(25));
                    
                    if (newSelection != package.isSelected)
                    {
                        //勾选前检查
                        if (TogglePackageCheck(package, newSelection))
                        {
                            package.isSelected = newSelection;
                        }
                    }
                    
                    // 显示包信息，使用工具提示
                    string packageText = $"<color=#E0E0E0>{package.title}</color>";
                    GUIContent label = new GUIContent(packageText, package.description);
                    GUILayout.Label(label, RichTextUtils.GetRichTextStyle(Color.white, 12), GUILayout.ExpandWidth(true));
                }
                
                // 添加小三角箭头按钮来切换详情显示/隐藏
                string toggleContent = _packageDetailsVisibility.ContainsKey(package.name) && 
                                      _packageDetailsVisibility[package.name] ? "▼" : "▶"; // 显示/隐藏箭头
                if (GUILayout.Button(toggleContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(20)))
                {
                    // 切换详情显示状态
                    TogglePackageDetailVisibility(package.name);
                }
                
                EditorGUILayout.EndHorizontal();
                
                // 如果详情显示状态为true，则显示详细信息
                if (_packageDetailsVisibility.ContainsKey(package.name) && 
                    _packageDetailsVisibility[package.name])
                {
                    // 将包的详细信息包装在一个垂直区域中
                    EditorGUILayout.BeginVertical("box"); // 使用box样式包围详细信息
                    EditorGUI.indentLevel++;
                    
                    if (!string.IsNullOrEmpty(package.name))
                    {
                        GUILayout.Label("<color=#FFFFFF>Name:</color> <color=#03A9F4>" + package.name + "</color>", RichTextUtils.GetRichTextStyle(Color.white, 10));
                    }
                    
                    if (!string.IsNullOrEmpty(package.displayName) && package.displayName != package.name)
                    {
                        GUILayout.Label("<color=#FFFFFF>displayName:</color> <color=#03A9F4>" + package.displayName + "</color>", RichTextUtils.GetRichTextStyle(Color.white, 10));
                    }
                    
                    if (!string.IsNullOrEmpty(package.title) && package.title != package.displayName)
                    {
                        GUILayout.Label("<color=#FFFFFF>title:</color> <color=#03A9F4>" + package.title + "</color>", RichTextUtils.GetRichTextStyle(Color.white, 10));
                    }
                    
                    // 只有当描述不为空时才显示描述信息
                    if (!string.IsNullOrEmpty(package.description))
                    {
                        GUILayout.Label("<color=#FFFFFF>描述:</color> <color=#03A9F4>" + package.description + "</color>", RichTextUtils.GetRichTextStyle(Color.white, 10));
                    }
                    
                    // 显示依赖信息
                    if (package.dependencies != null && package.dependencies.Count > 0)
                    {
                        GUILayout.Label("<color=#FFFFFF>依赖:</color> <color=#03A9F4>" + string.Join(", ", package.dependencies) + "</color>", RichTextUtils.GetRichTextStyle(Color.white, 10));
                    }
                    
                    // 显示反向依赖信息
                    // if (package.reverseDependencies != null && package.reverseDependencies.Count > 0)
                    // {
                    //     GUILayout.Label("被引用: " + string.Join(", ", package.reverseDependencies), new GUIStyle(EditorStyles.helpBox) { fontSize = 10 });
                    // }
                    
                    // 显示互斥信息
                    if (package.repulsions != null && package.repulsions.Count > 0)
                    {
                        GUILayout.Label("<color=#FFFFFF>互斥:</color> <color=#03A9F4>" + string.Join(", ", package.repulsions) + "</color>", RichTextUtils.GetRichTextStyle(Color.white, 10));
                    }
                    
                    // 显示Git URL（如果存在）
                    if (!string.IsNullOrEmpty(package.gitUrl))
                    {
                        GUILayout.Label("<color=#FFFFFF>Git地址:</color> <color=#03A9F4>" + package.gitUrl + "</color>", RichTextUtils.GetRichTextStyle(Color.white, 10));
                    }
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical(); // 结束垂直区域
                }
                
                EditorGUILayout.Space(2); // 减少间距
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawRightPanel()
        {
            GUILayout.Space(40); // 左侧整体右移20像素，增加左侧视觉空间

            // 右侧：已选包列表
            EditorGUILayout.BeginVertical(GUILayout.Width(Screen.width * 0.35f)); // 减少右侧宽度到35%，并将其靠右对齐
            GUILayout.Label("<color=#4CAF50>已选择的包</color>", RichTextUtils.GetBoldRichTextStyle(Color.white, 14));
            
            DrawSelectedPackagesPanel();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawButtomPanel()
        {
            
            EditorGUILayout.Space(30);
            
            EditorGUILayout.BeginHorizontal();
            
            GUIStyle updateButtonStyle = RichTextUtils.GetButtonTextOnlyStyle(Color.yellow);
            if (GUILayout.Button("一键更新所选包(Git)", updateButtonStyle, GUILayout.Height(35)))
            {
                Debug.Log("一键更新所选包(Git)");

                if (IsSamePackages(DataManager.LoadPersistedSelectedPackages(), PackageManager.GetSelectedPackageNames()))
                {
                    if (EditorUtility.DisplayDialog("确认更新", 
                            "确定要更新所有选中的包吗？此操作将会从Git仓库拉取最新版本。", 
                            "确定", "取消"))
                    {
                        GitManager.UpdatePackages(PackageManager.GetSelectedPackageInfos());
                        DataManager.SavePersistedSelectedPackages(PackageManager.GetSelectedPackageNames());
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("更新失败", "所选包列表有修改，请先保存选择", "确定");
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private bool IsSamePackages(List<string> oldPackages, List<string> newPackages)
        {
            if (oldPackages.Count == newPackages.Count)
            {
                for (int i = 0; i < oldPackages.Count; i++)
                {
                    if (!newPackages.Contains(oldPackages[i]))
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// 切换包的选择状态前的检查
        /// </summary>
        /// <param name="package">要切换的包</param>
        /// <param name="isSelected">新的选择状态</param>
        /// <returns>如果操作成功则返回true，否则返回false</returns>
        private bool TogglePackageCheck(PackageInfo package, bool isSelected)
        {
            if (package == null || package.isRequired) return false;

            List<string> recursivelyDependencies = PackageManager.GetPackageRecursivelyDependencies(package.name);
            List<PackageInfo> selectedPackages = PackageManager.GetSelectedPackageInfos();
            
            if (isSelected)
            {
                //自身是否跟已安装的包互斥
                if (package.repulsions != null && package.repulsions.Count > 0)
                {
                    foreach (var repulsionPkgName in package.repulsions)
                    {
                        var existPackage = selectedPackages.Find(p => p.name == repulsionPkgName);
                        if (existPackage != null)
                        {
                            PackageInfo repulsionPackageInfo = PackageManager.GetPackageInfoByName(repulsionPkgName);
                            EditorUtility.DisplayDialog("互斥冲突",
                                $"【{package.title}】与【{repulsionPackageInfo.title}】 互斥",
                                "确定");
                            return false;
                        }
                    }
                }
                
                //递归的依赖是否跟已安装的包互斥
                foreach (string depName in recursivelyDependencies)
                {
                    PackageInfo _pkgInfo = PackageManager.GetPackageInfoByName(depName);
                    if (_pkgInfo.repulsions != null && _pkgInfo.repulsions.Count > 0)
                    {
                        foreach (var repulsionPkgName in _pkgInfo.repulsions)
                        {
                            var existPackage = selectedPackages.Find(p => p.name == repulsionPkgName);
                            if (existPackage != null)
                            {
                                PackageInfo depPackageInfo = PackageManager.GetPackageInfoByName(depName);
                                
                                EditorUtility.DisplayDialog("互斥冲突",
                                    $"【{package.title}】中引用的【{depPackageInfo.title}】与已勾选的【{existPackage.title}】 互斥",
                                    "确定");
                                return false;
                            }
                        }
                    }
                }
                
                // 选中包时，自动选中其引用
                foreach (string depName in recursivelyDependencies)
                {
                    var depPackage = PackageManager.PackageInfoList.Find(p => p.name == depName);
                    if (depPackage != null && !depPackage.isSelected)
                    {
                        depPackage.isSelected = true;
                    }
                }

                // 使用EditorApplication.delayCall延迟执行保存操作，确保界面更新
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    GitManager.HandleSelectPackages(DataManager.LoadPersistedSelectedPackages(), PackageManager.GetSelectedPackageNames());
                    DataManager.SavePersistedSelectedPackages(PackageManager.GetSelectedPackageNames());
                    UnityEditor.PackageManager.Client.Resolve();
                };
                
                return true;
            }
            else
            {
                // 取消选中包时，检查是否有其他包依赖它
                foreach (var otherPackage in PackageManager.PackageInfoList)
                {
                    if (otherPackage.isSelected && otherPackage.dependencies.Contains(package.name))
                    {
                        // 如果有其他选中的包依赖此包，则不允许取消选中
                        EditorUtility.DisplayDialog("依赖冲突",
                            $"无法取消选中 【{package.title}】，因为 【{otherPackage.title}】 依赖于它。",
                            "确定");
                        return false;
                    }
                }
        
                // 如果没有包依赖此包，可以取消选中
                package.isSelected = false;
                
                // 使用EditorApplication.delayCall延迟执行保存操作，确保界面更新
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    GitManager.HandleSelectPackages(DataManager.LoadPersistedSelectedPackages(), PackageManager.GetSelectedPackageNames());
                    DataManager.SavePersistedSelectedPackages(PackageManager.GetSelectedPackageNames());
                    UnityEditor.PackageManager.Client.Resolve();
                };
                
                return true;
            }
        }
        
        
        private void DrawSelectedPackagesPanel()
        {
            //获取已选中的包
            var selectedPackages = PackageManager.GetSelectedPackageInfos();
            
            if (selectedPackages.Count == 0)
            {
                EditorGUILayout.HelpBox("当前没有选择任何包", MessageType.Info);
                return;
            }
            
            // 滚动视图以容纳可能的大量已选包
            _selectedPackagesScrollPosition = EditorGUILayout.BeginScrollView(_selectedPackagesScrollPosition, GUILayout.Height(470)); // 增加回50的高度（从420增加到470）
            
            foreach (var package in selectedPackages)
            {
                EditorGUILayout.BeginHorizontal("box");
                
                // 显示包名称，确保完整显示并支持工具提示
                string packageText = $"<color=#FFFFFF>{package.title}</color>";
                if (package.isRequired)
                {
                    packageText += " <color=#FF9800>(必需)</color>"; // 标注必需包
                }
                GUIContent label = new GUIContent(packageText, package.description);
                GUILayout.Label(label, RichTextUtils.GetRichTextStyle(Color.white, 12), GUILayout.ExpandWidth(true));
                
                // 只为非必需包显示移除按钮
                if (!package.isRequired)
                {
                    GUIStyle removeButtonStyle = RichTextUtils.GetButtonTextOnlyStyle(new Color(0.9f, 0.3f, 0.3f, 1f)); // 红色背景的移除按钮
                    if (GUILayout.Button("移除", removeButtonStyle, GUILayout.Width(50), GUILayout.Height(25)))
                    {
                        TogglePackageCheck(package, false);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(2);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// 切换包详情的可见性
        /// </summary>
        /// <param name="packageName">包名</param>
        private void TogglePackageDetailVisibility(string packageName)
        {
            if (_packageDetailsVisibility.ContainsKey(packageName))
            {
                _packageDetailsVisibility[packageName] = !_packageDetailsVisibility[packageName];
            }
            else
            {
                _packageDetailsVisibility[packageName] = true; // 默认展开
            }
        }
    }
}