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

using System.Collections.Generic;
using NovaFramework.Editor.Manifest;
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
        
        static PackageManager()
        {
            // 初始化时加载配置文件
            LoadData();
        }
        
        public static void LoadData()
        {
            RepoManifest.Instance.LoadData();
            
            _systemPathInfos = RepoManifest.Instance.localPaths;
            _packageObjectList = RepoManifest.Instance.modules;
            
            // 进一步处理xml的package数据，
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
            
            //同步持久化数据
            List<string> persistedPackageNames = DataManager.LoadPersistedSelectedPackages();
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
    }
}