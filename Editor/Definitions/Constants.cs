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

        // 默认资源路径
        public static string DEFAULT_INSTALLER_ROOT_PATH
        {
            get
            {
                return PersistencePath.CurrentUsingRepositoryUrlOfTargetModule(LocalPackageNameOfInstallerModule);
            }
        }
        
        // 配置文件路径常量

        public static readonly string REPO_MANIFEST_PATH = Path.Combine(DEFAULT_INSTALLER_ROOT_PATH, "Editor Default Resources/Config/repo_manifest.xml").Replace("\\", "/");
        
        // 系统环境配置路径（相对，基于项目根目录）
        public static readonly string SYSTEM_ENVIRONMENTS_PATH = "Assets/Resources/system_environments.json";
        
        // 系统环境配置绝对路径
        public static readonly string SYSTEM_ENVIRONMENTS_ABSOLUTE_PATH =
            Path.Combine(Path.GetDirectoryName(Application.dataPath), SYSTEM_ENVIRONMENTS_PATH).Replace("\\", "/");
        
        // Game ZIP文件路径常量
        public static readonly string GAME_ZIP_PATH = Path.Combine(DEFAULT_INSTALLER_ROOT_PATH, "Editor Default Resources/BasePack/Game.zip").Replace("\\", "/");
        
        public const string SAVE_PACKAGE_RELATIVE_PATH = "NovaFrameworkData/framework_repo";
        public static readonly string FRAMEWORK_REPO_PATH = Path.Combine(Path.GetDirectoryName(Application.dataPath), SAVE_PACKAGE_RELATIVE_PATH).Replace("\\", "/");
        
        // UserSettings 键值常量
        public const string NovaFramework_Installer_DIRECTORY_CONFIG_KEY = "NovaFramework_Installer.DirectoryConfig.SystemVariables";
        public const string NovaFramework_Installer_ASSEMBLY_CONFIG_KEY = "NovaFramework_Installer.AssemblyConfig.Configs";
        public const string NovaFramework_Installer_PACKAGE_NAME_LIST_KEY = "NovaFramework_Installer.PACKAGE_NAME_LIST_KEY";      // 安装包列表名
        public const string NovaFramework_Installer_INSTALLER_COMPLETE_KEY = "NovaFramework_Installer.INSTALLER_COMPLETE_MARK";     //完成安装的key
        
        //用于git更新的包名
        public const string INSTALLER_PACKAGE_NAME = LocalPackageNameOfInstallerModule;
        public const string COMMON_PACKAGE_NAME = PersistencePath.LocalPackageNameOfCommonModule;
    } 
}