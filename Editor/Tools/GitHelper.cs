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
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    public class GitHelper
    {
        public static bool CloneRepository(string url, string destinationPath)
        {
            try
            {
                // 确保目标目录的父目录存在
                string parentDir = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }
                
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"clone {url} \"{destinationPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    
                    process.WaitForExit();
                    
                    if (process.ExitCode == 0)
                    {
                        UnityEngine.Debug.Log($"成功克隆仓库: {url} 到 {destinationPath}");
                        AssetDatabase.Refresh();
                        return true;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"Git克隆失败: {error}");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Git操作异常: {e.Message}");
                return false;
            }
        }
        
        public static bool IsGitInstalled()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                
                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }
        
        public static bool PullRepository(string repositoryPath)
        {
            try
            {
                if (!Directory.Exists(repositoryPath))
                {
                    UnityEngine.Debug.LogError($"仓库路径不存在: {repositoryPath}");
                    return false;
                }
                
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "pull origin main", // 默认拉取main分支，可根据需要调整
                    WorkingDirectory = repositoryPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    
                    process.WaitForExit();
                    
                    if (process.ExitCode == 0)
                    {
                        UnityEngine.Debug.Log($"成功更新仓库: {repositoryPath}\n{output}");
                        AssetDatabase.Refresh();
                        return true;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"Git pull 失败: {error}");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Git pull 操作异常: {e.Message}");
                return false;
            }
        }
    }
}