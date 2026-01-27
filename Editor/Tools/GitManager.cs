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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    //处理git安装的一系列操作
    public static class GitManager
    {
        // 最大重试次数（解决文件临时占用问题）
        private const int MaxRetryCount = 3;
        // 重试间隔（毫秒）
        private const int RetryDelayMs = 500;

        private const string savePath = "file:./../" + Constants.SAVE_PACKAGE_RELATIVE_PATH;
        
        public static void InstallPackageFromGit(PackageInfo package, string destinationPath)
        {
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }
            
            if (GitHelper.CloneRepository(package.gitUrl, destinationPath))
            {
                Debug.Log($"从Git仓库安装成功: {package.gitUrl}");
                string packageSavePath = Path.Combine(savePath, package.name).Replace("\\", "/");
                PackageManifestHandler.AddPackage(packageSavePath, package);
            }
            else
            {
                Debug.LogError($"从Git仓库安装失败: {package.gitUrl}");
            }
        }
        
        public static void UninstallPackage(string folderPath, string folderName)
        {
            ForceDeleteDirectory(folderPath);
            PackageManifestHandler.RemovePackage(folderName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newPackages">新的所选包</param>
        public static void SavePackage(List<PackageInfo> newPackages)
        {
            if (!Directory.Exists(Constants.FRAMEWORK_REPO_PATH))
            {
                Directory.CreateDirectory(Constants.FRAMEWORK_REPO_PATH);
            }
            
            // 获取 FRAMEWORK_REPO_PATH 下的所有子文件夹
            string[] existingFolders = Directory.GetDirectories(Constants.FRAMEWORK_REPO_PATH);
            
            // 遍历现有文件夹，检查是否在 newPackages 中存在对应的包
            foreach (string folderPath in existingFolders)
            {
                string folderName = Path.GetFileName(folderPath);
                
                bool packageExists = false;
                foreach (var package in newPackages)
                {
                    if (package.name == folderName)
                    {
                        packageExists = true;
                        break;
                    }
                }
                
                // 如果文件夹在 newPackages 中不存在，则删除它
                if (!packageExists)
                {
                    UninstallPackage(folderPath, folderName);
                }
            }
            
            // 遍历 newPackages，检查是否需要克隆新的包
            foreach (var package in newPackages)
            {
                string packagePath = Path.Combine(Constants.FRAMEWORK_REPO_PATH, package.name);
                
                // 如果包不存在，则进行克隆
                if (!Directory.Exists(packagePath) && !string.IsNullOrEmpty(package.gitUrl))
                {
                    InstallPackageFromGit(package, packagePath);
                }
            }
        }


        
        /// <summary>
        /// 强制删除目录（适配Git/Unity场景，处理隐藏/只读/被占用文件）
        /// </summary>
        /// <param name="targetDir">要删除的目录路径</param>
        public static void ForceDeleteDirectory(string targetDir)
        {
            // 校验目录是否存在
            if (!Directory.Exists(targetDir))
            {
                Debug.Log($"目录 {targetDir} 不存在，无需删除");
                return;
            }

            try
            {
                // 1. 处理目录内的所有文件（移除隐藏/只读属性 + 重试删除）
                foreach (string file in Directory.EnumerateFiles(targetDir))
                {
                    FileInfo fileInfo = new FileInfo(file);

                    // 跳过系统文件（避免权限问题）
                    if ((fileInfo.Attributes & FileAttributes.System) == FileAttributes.System)
                    {
                        continue;
                    }

                    // 移除隐藏/只读属性（核心修复点）
                    fileInfo.Attributes = FileAttributes.Normal;

                    // 带重试机制删除文件（解决临时占用问题）
                    DeleteFileWithRetry(fileInfo);
                }

                // 2. 递归处理子目录
                foreach (string subDir in Directory.EnumerateDirectories(targetDir))
                {
                    ForceDeleteDirectory(subDir);
                }

                // 3. 移除当前目录的隐藏/只读属性
                DirectoryInfo dirInfo = new DirectoryInfo(targetDir);
                dirInfo.Attributes = FileAttributes.Directory;

                // 4. 删除空目录（带重试）
                DeleteDirectoryWithRetry(targetDir);

                Debug.Log($"目录 {targetDir} 删除成功");
            }
            catch (Exception ex)
            {
                Debug.LogError($"删除目录失败：{ex.Message}\n{ex.StackTrace}");
                Debug.LogError("请关闭Git/VS/Unity相关进程后重试");
            }
        }

        /// <summary>
        /// 带重试机制删除文件（解决文件临时被占用问题）
        /// </summary>
        private static void DeleteFileWithRetry(FileInfo fileInfo)
        {
            int retryCount = 0;
            while (retryCount < MaxRetryCount)
            {
                try
                {
                    if (fileInfo.Exists)
                    {
                        fileInfo.Delete();
                    }

                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    retryCount++;
                    if (retryCount >= MaxRetryCount) throw;
                    Thread.Sleep(RetryDelayMs); // 等待后重试
                }
            }
        }

        /// <summary>
        /// 带重试机制删除目录
        /// </summary>
        private static void DeleteDirectoryWithRetry(string dirPath)
        {
            int retryCount = 0;
            while (retryCount < MaxRetryCount)
            {
                try
                {
                    if (Directory.Exists(dirPath))
                    {
                        Directory.Delete(dirPath);
                    }

                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    retryCount++;
                    if (retryCount >= MaxRetryCount) throw;
                    Thread.Sleep(RetryDelayMs);
                }
            }
        }
        
        public static void UpdateSelectPackage(List<PackageInfo> selectPackages)
        { 
            foreach (var package in selectPackages)
            {
                if (!string.IsNullOrEmpty(package.gitUrl))
                {
                    // 获取包的存储路径
                    string packagePath = Path.Combine(Constants.FRAMEWORK_REPO_PATH, package.name);
                    
                    if (Directory.Exists(packagePath))
                    {
                        // 如果包已存在，使用pull更新
                        if (GitHelper.PullRepository(packagePath))
                        {
                            Debug.Log($"包 {package.name} 更新成功");
                        }
                        else
                        {
                            Debug.LogError($"包 {package.name} 更新失败");
                        }
                    }
                    else
                    {
                        // 如果包不存在，则执行初始安装
                        InstallPackageFromGit(package, packagePath);
                    }
                }
                else
                {
                    Debug.LogWarning($"包 {package.name} 没有设置Git URL，无法更新");
                }
            }
        }
        
        /// <summary>
        /// 安装指定名称的包
        /// </summary>
        /// <param name="packageName">包名称</param>
        /// <param name="onComplete">安装完成回调</param>
        public static void InstallPackage(string packageName, System.Action onComplete = null)
        {
            var package = PackageManager.GetPackageInfoByName(packageName);
            if (package != null)
            {
                string packagePath = Path.Combine(Constants.FRAMEWORK_REPO_PATH, package.name);
                
                if (!string.IsNullOrEmpty(package.gitUrl))
                {
                    InstallPackageFromGit(package, packagePath);
                }
                else
                {
                    Debug.LogWarning($"包 {package.name} 没有设置Git URL，无法安装");
                }
            }
            else
            {
                Debug.LogWarning($"未找到名为 {packageName} 的包");
            }
            
            onComplete?.Invoke();
        }
    }
}
