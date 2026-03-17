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
using NovaFramework.Editor.Manifest;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    /// <summary>
    /// 包管理器，负责处理包的数据逻辑
    /// 都是内存中的数据
    /// </summary>
    internal static class PackageManager
    {
        private static HashSet<string> _selectedPackageSet = new HashSet<string>();

        private static List<LocalPathObject> _systemPathInfos;
        private static List<PackageObject> _packageObjectList;

        public static List<LocalPathObject> SystemPathInfos => _systemPathInfos;
        public static List<PackageObject> PackageObjectList => _packageObjectList;
        
        private static string _selectedPackagesTxtPath = Path.Combine(Application.dataPath, "../NovaFrameworkData", "selected_packages.txt").Replace("\\", "/");
        
        static PackageManager()
        {
            // 初始化时加载配置文件
            LoadData();
        }
        
        public static void LoadData()
        {
            // 清空之前的选择状态，确保从文件重新加载
            _selectedPackageSet.Clear();
            
            RepoManifest.Instance.LoadData();
            
            _systemPathInfos = RepoManifest.Instance.localPaths;
            _packageObjectList = RepoManifest.Instance.modules;
            
            // 进一步处理xml的package数据
            foreach (var pkg in _packageObjectList)
            {
                if (pkg.required)
                {
                    _selectedPackageSet.Add(pkg.name);

                    //比如递归依赖的情况，比如A依赖B，B依赖C，那么A依赖C
                    List<string> recursivelyDependencies = GetPackageRecursivelyDependencies(pkg.name);
                    foreach (var depPkgName in recursivelyDependencies)
                    {
                        var existingPkg = _packageObjectList.Find(p => p.name == depPkgName);
                        if (existingPkg != null)
                        {
                            _selectedPackageSet.Add(existingPkg.name);
                        }
                    }
                }
            }
            
            //同步已经配置的记录
            List<string> persistedPackageNames = LoadSelectedPackagesTxt();
            if (persistedPackageNames != null)
            {
                foreach (var pkg in _packageObjectList)
                {
                    if (persistedPackageNames.Contains(pkg.name))
                    {
                        _selectedPackageSet.Add(pkg.name);
                    }
                }
            }
        }

        /// <summary>
        /// 根据搜索过滤条件获取过滤后的包列表
        /// </summary>
        /// <param name="searchFilter">搜索过滤条件</param>
        /// <returns>过滤后的包列表</returns>
        public static List<PackageObject> GetFilteredPackages(string searchFilter)
        {
            if (string.IsNullOrEmpty(searchFilter))
            {
                return new List<PackageObject>(_packageObjectList);
            }
            else
            {
                return _packageObjectList.FindAll(pkg =>
                    pkg.displayName.ToLower().Contains(searchFilter.ToLower()) ||
                    pkg.name.ToLower().Contains(searchFilter.ToLower()) ||
                    (!string.IsNullOrEmpty(pkg.description) && pkg.description.ToLower().Contains(searchFilter.ToLower()))
                );
            }
        }

        public static bool IsSelectedPackage(string name)
        {
            return _selectedPackageSet.Contains(name);
        }

        public static void SetSelectedStatus(string name, bool isSelected)
        {
            if (isSelected)
            {
                _selectedPackageSet.Add(name);
            }
            else
            {
                _selectedPackageSet.Remove(name);
            }
        }
        
        
        /// <summary>
        /// 获取已选择的包列表
        /// </summary>
        /// <returns>已选择的包列表</returns>
        public static List<PackageObject> GetSelectedPackageObjects()
        {
            var selectedPackages = new List<PackageObject>();
            
            foreach (var pkg in RepoManifest.Instance.modules)
            {
                if (_selectedPackageSet.Contains(pkg.name))
                {
                    selectedPackages.Add(pkg);
                }
            }
            
            return selectedPackages;
        }
        
        /// <summary>
        /// 获取已选择的包名称列表
        /// </summary>
        /// <returns>已选择的包名称列表</returns>
        public static List<string> GetSelectedPackageNames()
        {
            return new List<string>(_selectedPackageSet);
        }
        
        /// <summary>
        /// 获取指定名称的包信息
        /// </summary>
        /// <param name="name">包名称</param>
        /// <returns>包信息，如果未找到则返回null</returns>
        public static PackageObject GetPackageObjectByName(string name)
        {
            return _packageObjectList?.Find(pkg => pkg.name == name);
        }

        //通过递归，找出所有的依赖包
        public static List<string> GetPackageRecursivelyDependencies(string name)
        {
            List<string> recursivelyDependencies = new List<string>();
            var packageObject = GetPackageObjectByName(name);
            
            if (packageObject.dependencies != null && packageObject.dependencies.Count > 0)
            {
                recursivelyDependencies.AddRange(packageObject.dependencies);
                
                foreach (var depName in packageObject.dependencies)
                {
                    List<string> depNameList = GetPackageRecursivelyDependencies(depName);
                    recursivelyDependencies.AddRange(depNameList);
                }
            }

            return recursivelyDependencies;
        }
        
        private static List<string> LoadSelectedPackagesTxt()
        {
            if (!File.Exists(_selectedPackagesTxtPath))
            {
                return new List<string>();
            }

            try
            {
                string content = File.ReadAllText(_selectedPackagesTxtPath).Trim();
                if (string.IsNullOrEmpty(content))
                {
                    return new List<string>();
                }

                var lines = content.Split('\n');
                var result = new List<string>();
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        result.Add(trimmed);
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                Logger.Error($"读取包配置列表失败: {e.Message}");
                return new List<string>();
            }
        }

        public static void OneClickInstallPackages()
        {
            // 读取 selected_packages.txt
            string txtPath = Path.Combine(Application.dataPath, "../NovaFrameworkData/selected_packages.txt").Replace("\\", "/");
            if (!File.Exists(txtPath))
            {
                Logger.Warn("提示", $"未找到 selected_packages.txt，请先在Package安装中心生成包列表配置 {txtPath}", "确定");
                return;
            }

            string content = File.ReadAllText(txtPath).Trim();
            if (string.IsNullOrEmpty(content))
            {
                Logger.Warn("提示", "selected_packages.txt 为空，请先在Package安装中心生成包列表配置", "确定");
                return;
            }

            // 解析包名列表
            var packageNames = content.Split('\n')
                .Select(line => line.Trim())
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();

            if (packageNames.Count == 0)
            {
                Logger.Warn("提示", "没有需要安装的模块", "确定");
                return;
            }

            // 查找对应的 PackageObject
            var packagesToInstall = new List<PackageObject>();
            foreach (var name in packageNames)
            {
                PackageObject pkg = PackageManager.GetPackageObjectByName(name);
                if (pkg != null)
                {
                    packagesToInstall.Add(pkg);
                }
                else
                {
                    Logger.Warn($"[一键安装] 未在清单中找到包: {name}，跳过");
                }
            }

            if (packagesToInstall.Count == 0)
            {
                Logger.Warn("提示", "txt中的包均未在清单中找到", "确定");
                return;
            }

            string displayNames = string.Join("\n", packagesToInstall.Select(p => p.title));
            if (!EditorUtility.DisplayDialog("确认一键安装",
                    $"将要安装以下 {packagesToInstall.Count} 个模块:\n{displayNames}\n\n模块将下载到 NovaFrameworkData/framework_repo 并配置到 manifest.json",
                    "确定", "取消"))
            {
                return;
            }

            // 逐个安装
            var installedNames = new List<string>();
            foreach (var package in packagesToInstall)
            {
                Logger.Info($"[一键安装] 开始安装模块: {package.name}");
                GitManager.InstallPackage(package);
                installedNames.Add(package.name);
            }

            Logger.Info($"[一键安装] 安装完成，共 {installedNames.Count} 个模块，正在刷新...");

            UnityEditor.PackageManager.Client.Resolve();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            CompilationPipeline.RequestScriptCompilation();
        }
    }
}