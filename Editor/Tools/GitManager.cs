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
using System.Threading;
using NovaFramework.Editor.Manifest;
using UnityEngine;
using UnityEditor;

namespace NovaFramework.Editor.Installer
{
    //处理git安装的一系列操作
    internal static class GitManager
    {
        // 最大重试次数（解决文件临时占用问题）
        private const int MAX_RETRY_COUNT = 3;
        // 重试间隔（毫秒）
        private const int RETRY_DELAY_MS = 500;

        private const string SAVE_ROOT_PARH = "file:./../" + Constants.SAVE_PACKAGE_RELATIVE_PATH;
        
        private static void InstallPackageFromGit(PackageObject package, string destinationPath)
        {
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }
            
            bool cloneSuccess = GitUtils.CloneRepository(package.gitRepositoryUrl, destinationPath);
            
            if (cloneSuccess)
            {
                // 等待文件系统稳定后再更新包清单
                WaitForFileSystemStable(() =>
                {
                    string packageSavePath = Path.Combine(SAVE_ROOT_PARH, package.name).Replace("\\", "/");
                    PackageManifestUtils.AddPackageToManifest(package.name, packageSavePath);
                });
            }
            else
            {
                UnityEngine.Debug.LogError($"[GitManager] 从Git仓库安装失败: {package.gitRepositoryUrl}");
            }
        }
        
        public static void UninstallPackage(string oldPkgName)
        {
            string folderPath = Path.Combine(Constants.FRAMEWORK_REPO_PATH, oldPkgName);
            ForceDeleteDirectory(folderPath);
            PackageManifestUtils.RemovePackageFromManifest(oldPkgName);
        }

        /// <summary>
        /// 安装指定名称的包
        /// </summary>
        /// <param name="packageName">包名称</param>
        /// <param name="onComplete">安装完成回调</param>
        public static void InstallPackage(PackageObject packageObject, System.Action onComplete = null)
        {
            try
            {
                if (packageObject == null)
                {
                    UnityEngine.Debug.LogError("[GitManager] InstallPackage: packageObject 为 null");
                    onComplete?.Invoke(); // 即使为空也要调用回调，否则流程会中断
                    return;
                }
                
                string packagePath = Path.Combine(Constants.FRAMEWORK_REPO_PATH, packageObject.name);
                
                InstallPackageFromGit(packageObject, packagePath);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GitManager] InstallPackage 异常: {ex.Message}\n{ex.StackTrace}");
            }
            
            // 确保回调始终被调用
            onComplete?.Invoke();
        }
        


        /// <summary>
        /// oldPackages中有，newPackages中没有的包，则删除
        /// oldPackages中没有，newPackages中有的包，则安装
        /// </summary>
        /// <param name="oldPackages"></param>
        /// <param name="newPackages"></param>
        public static void HandleSelectPackages(List<string> oldPackages, List<string> newPackages)
        {
            // 1. 找出需要删除的包（在旧列表中存在，但在新列表中不存在）
            foreach (var oldPkgName in oldPackages)
            {
                if (!newPackages.Contains(oldPkgName))
                {
                    // 从旧列表中存在但不在新列表中的包需要被卸载
                    UninstallPackage(oldPkgName);
                }
            }
            
            // 2. 找出需要安装的包（在新列表中存在，但在旧列表中不存在）
            foreach (var newPkgName in newPackages)
            {
                if (!oldPackages.Contains(newPkgName))
                {
                    // 在新列表中存在但不在旧列表中的包需要被安装
                    PackageObject packageObject = PackageManager.GetPackageObjectByName(newPkgName);
                    if (packageObject != null && !string.IsNullOrEmpty(packageObject.gitRepositoryUrl))
                    {
                        string packagePath = Path.Combine(Constants.FRAMEWORK_REPO_PATH, newPkgName);
                        InstallPackageFromGit(packageObject, packagePath);
                    }
                }
            }
        }
        
        /// <summary>
        /// 强制删除目录（适配Git/Unity场景，处理隐藏/只读/被占用文件）
        /// </summary>
        /// <param name="targetDir">要删除的目录路径</param>
        private static void ForceDeleteDirectory(string targetDir)
        {
            // 校验目录是否存在
            if (!Directory.Exists(targetDir))
            {
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
            while (retryCount < MAX_RETRY_COUNT)
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
                    if (retryCount >= MAX_RETRY_COUNT) throw;
                    Thread.Sleep(RETRY_DELAY_MS); // 等待后重试
                }
            }
        }

        /// <summary>
        /// 带重试机制删除目录
        /// </summary>
        private static void DeleteDirectoryWithRetry(string dirPath)
        {
            int retryCount = 0;
            while (retryCount < MAX_RETRY_COUNT)
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
                    if (retryCount >= MAX_RETRY_COUNT) throw;
                    Thread.Sleep(RETRY_DELAY_MS);
                }
            }
        }
        
        public static void UpdatePackages(List<PackageObject> selectPackages)
        {
            foreach (var package in selectPackages)
            {
                UpdateSinglePackage(package.name);
            }
        }

        public static void UpdateSinglePackage(string packageName)
        {
            // 获取包的存储路径
            string packagePath = Path.Combine(Constants.FRAMEWORK_REPO_PATH, packageName);
                    
            if (Directory.Exists(packagePath))
            {
                // 如果包已存在，使用pull更新
                if (!GitUtils.PullRepository(packagePath))
                {
                    Debug.LogError($"包 {packageName} 更新失败");
                }
            }
            else
            {
                Debug.LogError($"包 {packageName} 不存在，请先安装");
            }
        }
        
        /// <summary>
        /// 等待文件系统稳定
        /// </summary>
        /// <param name="onComplete">稳定后执行的回调</param>
        private static void WaitForFileSystemStable(System.Action onComplete)
        {
            // 简单的轮询等待，确保文件系统操作完成
            int checkCount = 0;
            const int maxChecks = 50; // 最多检查50次
            
            System.Action checkStability = null;
            EditorApplication.CallbackFunction callbackFunc = null;
            
            checkStability = () =>
            {
                checkCount++;
                
                // 检查是否有文件正在被使用或写入
                bool isStable = true;
                
                try
                {
                    // 检查目标目录是否稳定
                    if (Directory.Exists(SAVE_ROOT_PARH))
                    {
                        var files = Directory.GetFiles(SAVE_ROOT_PARH, "*", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            if (IsFileLocked(file))
                            {
                                isStable = false;
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    // 如果无法访问文件，认为不稳定
                    isStable = false;
                }
                
                if (isStable || checkCount >= maxChecks)
                {
                    onComplete?.Invoke();
                }
                else
                {
                    // 继续等待 - 创建一个新的CallbackFunction包装器
                    callbackFunc = () => {
                        EditorApplication.delayCall -= callbackFunc;
                        checkStability();
                    };
                    EditorApplication.delayCall += callbackFunc;
                }
            };
            
            checkStability();
        }
        
        /// <summary>
        /// 检查文件是否被锁定
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>如果文件被锁定返回true，否则返回false</returns>
        private static bool IsFileLocked(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;
                
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                // 文件被占用
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                // 没有访问权限，可能被占用
                return true;
            }
            
            return false;
        }
    }
}
