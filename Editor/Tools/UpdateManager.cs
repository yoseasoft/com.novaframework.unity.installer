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
    public class UpdateManager
    {
        private const string UPDATE_CHECK_URL = "https://github.com/yoseasoft/com.novaframework.unity.installer.git";
        private const string LOCAL_VERSION_FILE = "Assets/Editor/FrameworkInstaller/version.txt";
        
        public class UpdateInfo
        {
            public bool HasUpdate { get; set; }
            public string LatestVersion { get; set; }
            public string DownloadUrl { get; set; }
            public string ReleaseNotes { get; set; }
        }
        
        public static UpdateInfo CheckForUpdates()
        {
            try
            {
                // 使用Git命令检查远程仓库是否有更新
                // 这里暂时使用模拟实现，实际应该调用Git命令检查远程仓库
                
                // 模拟检查更新（在实际实现中，这将调用Git命令）
                var updateInfo = new UpdateInfo
                {
                    HasUpdate = false, // 暂时设为false，实际实现时需要检查
                    LatestVersion = "1.0.0", // 从远程仓库获取的实际版本号
                    DownloadUrl = "", // 从远程仓库获取的实际下载URL
                    ReleaseNotes = "暂无更新信息" // 从远程仓库获取的实际发布说明
                };
                
                // 这里应该实现实际的Git命令来检查更新
                // 例如：git remote update && git status -uno
                Debug.Log("正在使用Git检查更新...");
                
                return updateInfo;
            }
            catch (Exception ex)
            {
                Debug.LogError($"检查更新时出错: {ex.Message}");
                return new UpdateInfo { HasUpdate = false, ReleaseNotes = $"检查更新失败: {ex.Message}" };
            }
        }
        
        public static void PerformUpdate()
        {
            try
            {
                // 这里应该实现实际的更新过程
                // 1. 下载更新包
                // 2. 解压更新包
                // 3. 替换相关文件
                // 4. 更新配置文件
                
                Debug.Log("更新功能已准备就绪，等待实现下载逻辑...");
                
                EditorUtility.DisplayDialog("更新", "更新功能已准备就绪，实际更新需要连接到远程仓库", "确定");
            }
            catch (Exception ex)
            {
                Debug.LogError($"执行更新时出错: {ex.Message}");
                EditorUtility.DisplayDialog("错误", $"执行更新时出错: {ex.Message}", "确定");
            }
        }
        
        private static string GetLocalVersion()
        {
            if (File.Exists(LOCAL_VERSION_FILE))
            {
                return File.ReadAllText(LOCAL_VERSION_FILE).Trim();
            }
            return "unknown";
        }
        
        public static void CheckForUpdatesMenu()
        {
            var updateInfo = CheckForUpdates();
            
            if (updateInfo.HasUpdate)
            {
                bool shouldUpdate = EditorUtility.DisplayDialog(
                    "发现新版本",
                    $"发现新版本 {updateInfo.LatestVersion}\n\n更新内容:\n{updateInfo.ReleaseNotes}\n\n是否要更新?",
                    "更新", "稍后"
                );
                
                if (shouldUpdate)
                {
                    PerformUpdate();
                }
            }
            else
            {
                EditorUtility.DisplayDialog("检查更新", "当前已是最新版本", "确定");
            }
        }
    }
}