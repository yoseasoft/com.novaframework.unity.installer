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
using UnityEditor;
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    internal static class DataManager 
    {
        // 加载持久化数据中的包
        public static List<string> LoadPersistedSelectedPackages()
        {
            List<string> persistedPackageNames = UserSettings.GetObject<List<string>>(Constants.NovaFramework_Installer_PACKAGE_NAME_LIST_KEY);
            if (persistedPackageNames == null)
            {
                persistedPackageNames = new List<string>();
                SavePersistedSelectedPackages(persistedPackageNames);
            }
            return persistedPackageNames;
        }
        
        // 持久化保存已选择的包
        public static void SavePersistedSelectedPackages(List<string> selectPackageNames)
        {
            UserSettings.SetObject(Constants.NovaFramework_Installer_PACKAGE_NAME_LIST_KEY, selectPackageNames);
        }

        public static void ResetPersistedSelectedPackages()
        {
            UserSettings.SetObject(Constants.NovaFramework_Installer_PACKAGE_NAME_LIST_KEY, new List<string>());
        }
    }
}