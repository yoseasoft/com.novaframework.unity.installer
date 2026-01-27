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

namespace NovaFramework.Editor.Installer
{
    /// <summary>
    /// 框架安装器的常量定义
    /// </summary>
    public static class Constants
    {

        // 默认资源路径
        private const string DEFAULT_ROOT_PATH1 = "Assets/Editor/FrameworkInstaller/Editor Default Resources";
        private const string DEFAULT_ROOT_PATH2 = "./NovaFrameworkData/framework_repo/com.novaframework.unity.installer/Editor Default Resources";
       
        public static string DEFAULT_RESOURCES_ROOT_PATH
        {
            get
            {
                if (Directory.Exists(DEFAULT_ROOT_PATH1))
                {
                    return  DEFAULT_ROOT_PATH1;
                }
                else
                {
                    return DEFAULT_ROOT_PATH2;
                }
                
            }
        }
        
        // 配置文件路径常量
        public static readonly string FRAMEWORK_SETTING_PATH = Path.Combine(DEFAULT_RESOURCES_ROOT_PATH, "Config/FrameworkSetting.asset");

        public static readonly string REPO_MANIFEST_PATH = Path.Combine(DEFAULT_RESOURCES_ROOT_PATH, "Config/repo_manifest.xml");
        
        // 程序配置路径常量
        public static readonly string ASSEMBLY_CONFIG_PATH = "Assets/Resources/AssemblyConfig.json"; 
        // 系统变量配置路径
        public static readonly string SYSTEM_VARIABLES_PATH = "Assets/Resources/SystemVariables.json";
        
        // UI ZIP文件路径常量
        public static readonly string GAME_ZIP_PATH = Path.Combine(DEFAULT_RESOURCES_ROOT_PATH, "BasePack/Game.zip");
        
        public const string SAVE_PACKAGE_RELATIVE_PATH = "NovaFrameworkData/framework_repo";
        public static readonly string FRAMEWORK_REPO_PATH = Path.Combine(Path.GetDirectoryName(Application.dataPath), SAVE_PACKAGE_RELATIVE_PATH);
        
      
    }
}