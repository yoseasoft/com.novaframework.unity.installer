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

using UnityEditor;

namespace NovaFramework.Editor.Installer
{
    internal static class MainMenu
    {
        
        [MenuItem("Tools/自动安装 _F8", priority = 2, validate = true)]
        private static bool ValidateAutoInstall()
        {
            // 检查是否已经安装过了，如果已安装则不显示菜单项
            return !AutoInstallManager.IsAlreadyInstalled();
        }
        
        [MenuItem("Tools/自动安装 _F8", priority = 2)]
        public static void ShowAutoInstall()
        {
            AutoInstallManager.StartAutoInstall();
        }
        
        [MenuItem("Tools/配置中心 &_C", priority = 3)]
        public static void ShowConfigurationCenter()
        {
            ConfigurationWindow.ShowWindow();
        }
        
        [MenuItem("Tools/检查更新", priority = 4)]
        public static void ShowUpdateChecker()
        {
            GitManager.UpdateSinglePackage(Constants.COMMON_PACKAGE_NAME);
            GitManager.UpdateSinglePackage(Constants.INSTALLER_PACKAGE_NAME);
        }
        
        [MenuItem("Tools/验证环境", priority = 5)]
        public static void ValidateEnvironment()
        {
            EnvironmentValidator.ShowValidationResult();
        }
        
    }
}