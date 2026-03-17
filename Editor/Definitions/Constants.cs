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

using System.IO;
using UnityEngine;
using UnityEditor;

namespace NovaFramework.Editor.Installer
{
    /// <summary>
    /// 框架安装器的常量定义
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// 安装模块的本地包名
        /// </summary>
        public const string LocalPackageNameOfInstallerModule = @"com.novaframework.unity.installer";
        public const string LocalPackageNameOfCommonModule = PersistencePath.LocalPackageNameOfCommonModule;

        public const string InstallerEditorAssemblyName = "NovaEditor.Installer";
       
        public const string SAVE_PACKAGE_RELATIVE_PATH = "NovaFrameworkData/framework_repo";
        public static readonly string FRAMEWORK_REPO_PATH = Path.Combine(Path.GetDirectoryName(Application.dataPath), SAVE_PACKAGE_RELATIVE_PATH).Replace("\\", "/");
        
        // UserSettings 键值常量
        public const string NovaFramework_Installer_DIRECTORY_CONFIG_KEY = "NovaFramework_Installer.DirectoryConfig.SystemVariables";
        public const string NovaFramework_Installer_ASSEMBLY_CONFIG_KEY = "NovaFramework_Installer.AssemblyConfig.Configs";
        public const string NovaFramework_Installer_PACKAGE_NAME_LIST_KEY = "NovaFramework_Installer.PACKAGE_NAME_LIST_KEY";      // 安装包列表名
        public const string NovaFramework_Installer_INSTALLER_COMPLETE_KEY = "NovaFramework_Installer.INSTALLER_COMPLETE_MARK";     //完成安装的key
        public const string NovaFramework_Installer_PACKAGES_INSTALLED_KEY = "NovaFramework_Installer.PACKAGES_INSTALLED_MARK";  // 插件包安装完成标记
        //public const string SESSION_KEY_PENDING = "NovaFramework.AutoInstall.Pending";
        //public const string SESSION_KEY_STEP_PACKAGES = "NovaFramework.AutoInstall.StepPackages";
        
    } 
}