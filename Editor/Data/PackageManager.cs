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

namespace NovaFramework.Editor.Installer
{
    /// <summary>
    /// 包管理器，负责处理包的数据逻辑
    /// 都是内存中的数据
    /// </summary>
    internal static class PackageManager
    {
        private static PackageXMLInfo _packageXMLInfo;
        private static List<PackageInfo> _packageInfoList = new List<PackageInfo>();
        
        public static List<SystemPathInfo> SystemPathInfos => _packageXMLInfo.systemPathInfos;

        public static List<PackageInfo> PackageInfoList => _packageInfoList;
        
        static PackageManager()
        {
            // 初始化时加载配置文件
            LoadData();
        }
        
        public static void LoadData()
        {
            // 尝试加载包数据
            string manifestPath = Constants.REPO_MANIFEST_PATH;
            _packageXMLInfo = PackageXMLParser.ParseXML(manifestPath);

            _packageInfoList = _packageXMLInfo.packageInfos;
            
            // 进一步处理xml的package数据，
            foreach (var pkg in _packageInfoList)
            {
                if (pkg.isRequired)
                {
                    pkg.isSelected = true;

                    //比如递归依赖的情况，比如A依赖B，B依赖C，那么A依赖C
                    List<string> recursivelyDependencies = GetPackageRecursivelyDependencies(pkg.name);
                    foreach (var depPkgName in recursivelyDependencies)
                    {
                        var existingPkg = _packageInfoList.Find(p => p.name == depPkgName);
                        if (existingPkg != null)
                        {
                            existingPkg.isSelected = true;
                        }
                    }
                }
            }
            
            //同步持久化数据
            List<string> persistedPackageNames = DataManager.LoadPersistedSelectedPackages();
            if (persistedPackageNames != null)
            {
                foreach (var pkg in _packageInfoList)
                {
                    if (persistedPackageNames.Contains(pkg.name))
                    {
                        pkg.isSelected = true;
                    }
                }
            }
        }

        /// <summary>
        /// 根据搜索过滤条件获取过滤后的包列表
        /// </summary>
        /// <param name="searchFilter">搜索过滤条件</param>
        /// <returns>过滤后的包列表</returns>
        public static List<PackageInfo> GetFilteredPackages(string searchFilter)
        {
            if (string.IsNullOrEmpty(searchFilter))
            {
                return new List<PackageInfo>(_packageInfoList);
            }
            else
            {
                return _packageInfoList.FindAll(pkg =>
                    pkg.displayName.ToLower().Contains(searchFilter.ToLower()) ||
                    pkg.name.ToLower().Contains(searchFilter.ToLower()) ||
                    (!string.IsNullOrEmpty(pkg.description) && pkg.description.ToLower().Contains(searchFilter.ToLower()))
                );
            }
        }

        /// <summary>
        /// 获取已选择的包列表
        /// </summary>
        /// <returns>已选择的包列表</returns>
        public static List<PackageInfo> GetSelectedPackageInfos()
        {
            return _packageInfoList.FindAll(pkg => pkg.isSelected);
        }
        
        /// <summary>
        /// 获取已选择的包名称列表
        /// </summary>
        /// <returns>已选择的包名称列表</returns>
        public static List<string> GetSelectedPackageNames()
        {
            List<PackageInfo> selectedPackages = GetSelectedPackageInfos();
            List<string> selectedPackageNames = new List<string>();
            
            foreach (var pkg in selectedPackages)
            {
                selectedPackageNames.Add(pkg.name);
            }
            
            return selectedPackageNames;
        }
        
        /// <summary>
        /// 获取指定名称的包信息
        /// </summary>
        /// <param name="name">包名称</param>
        /// <returns>包信息，如果未找到则返回null</returns>
        public static PackageInfo GetPackageInfoByName(string name)
        {
            return _packageInfoList?.Find(pkg => pkg.name == name);
        }

        //通过递归，找出所有的依赖包
        public static List<string> GetPackageRecursivelyDependencies(string name)
        {
            List<string> recursivelyDependencies = new List<string>();
            var packageInfo = GetPackageInfoByName(name);

            if (packageInfo.dependencies != null && packageInfo.dependencies.Count > 0)
            {
                recursivelyDependencies.AddRange(packageInfo.dependencies);
                
                foreach (var depName in packageInfo.dependencies)
                {
                    List<string> depNameList = GetPackageRecursivelyDependencies(depName);
                    recursivelyDependencies.AddRange(depNameList);
                }
            }

            return recursivelyDependencies;
        }
    }
}