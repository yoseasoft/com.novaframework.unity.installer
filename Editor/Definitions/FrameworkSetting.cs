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
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    [Serializable]
    public class FrameworkSetting : ScriptableObject
    {
        [SerializeField] public List<PackageInfo> selectedPackages = new List<PackageInfo>();
        [SerializeField] public List<AssemblyDefinitionConfig> assemblyConfigs = new List<AssemblyDefinitionConfig>();
        
        // 添加方法来操作配置
        public void AddAssemblyConfig(AssemblyDefinitionConfig config)
        {
            assemblyConfigs.Add(config);
        }
        
        public void RemoveAssemblyConfig(AssemblyDefinitionConfig config)
        {
            assemblyConfigs.Remove(config);
        }
        
        public void RemoveAssemblyConfigAt(int index)
        {
            if (index >= 0 && index < assemblyConfigs.Count)
            {
                assemblyConfigs.RemoveAt(index);
            }
        }
    }
}