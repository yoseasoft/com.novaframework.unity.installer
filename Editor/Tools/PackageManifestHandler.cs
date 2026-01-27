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
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    /// <summary>
    /// 处理包的manifest.json文件，包括安装、卸载、更新等操作
    /// </summary>
    public static class PackageManifestHandler
    {
        /// <summary>
        /// 安装包配置到项目
        /// </summary>
        /// <param name="package">要安装的包</param>
        public static void AddPackage(string savePath, PackageInfo package)
        {
            if (string.IsNullOrEmpty(package.gitUrl))
            {
                Debug.LogWarning($"包 {package.displayName} 没有有效的Git URL");
                return;
            }

            // 通过修改manifest.json添加包依赖
            AddPackageToManifest(savePath, package.name);
        }

        /// <summary>
        /// 从项目中卸载包
        /// </summary>
        /// <param name="package">要卸载的包</param>
        public static void RemovePackage(string packageName)
        {
            // 从manifest.json中移除包依赖
            RemovePackageFromManifest(packageName);
        }

        /// <summary>
        /// 更新选中的包
        /// </summary>
        /// <param name="selectedPackages">选中的包列表</param>
        public static void UpdateSelectedPackages(List<PackageInfo> selectedPackages)
        {
            foreach (var package in selectedPackages)
            {
                if (!string.IsNullOrEmpty(package.gitUrl))
                {
                    // 通过修改manifest.json更新包依赖，实际上是重新添加包以获取最新版本
                    UpdatePackageInManifest(package.gitUrl, package.name);
                }
            }
            
            // 刷新Unity的包管理器
            UnityEditor.PackageManager.Client.Resolve();
        }

        /// <summary>
        /// 添加包到manifest.json
        /// </summary>
        /// <param name="gitUrl">Git仓库URL</param>
        /// <param name="packageName">包名称</param>
        private static void AddPackageToManifest(string savePath, string packageName)
        {
            try
            {
                // 获取manifest.json文件路径
                string manifestPath = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), "Packages", "manifest.json");

                if (!File.Exists(manifestPath))
                {
                    Debug.LogError($"未找到manifest.json文件: {manifestPath}");
                    return;
                }

                // 读取现有的manifest.json内容
                string jsonContent = File.ReadAllText(manifestPath);

                // 查找 "dependencies" 的位置
                string dependenciesMarker = "\"dependencies\"";
                int dependenciesIndex = jsonContent.IndexOf(dependenciesMarker);

                if (dependenciesIndex == -1)
                {
                    Debug.LogError("未在manifest.json中找到dependencies部分");
                    return;
                }

                // 从dependencies标记后查找冒号和左大括号
                int colonIndex = jsonContent.IndexOf(":", dependenciesIndex);
                if (colonIndex == -1)
                {
                    Debug.LogError("未在manifest.json中找到dependencies后的冒号");
                    return;
                }

                int openBraceIndex = jsonContent.IndexOf('{', colonIndex);
                if (openBraceIndex == -1)
                {
                    Debug.LogError("未在manifest.json中找到dependencies后的左大括号");
                    return;
                }

                // 找到dependencies对象的开始位置（左大括号之后）
                int dependenciesObjStart = openBraceIndex + 1;

                // 提取dependencies对象部分
                int dependenciesObjEnd = FindMatchingBrace(jsonContent, openBraceIndex);
                if (dependenciesObjEnd == -1)
                {
                    Debug.LogError("manifest.json中dependencies部分格式错误");
                    return;
                }

                string dependenciesStr = jsonContent.Substring(dependenciesObjStart, dependenciesObjEnd - dependenciesObjStart - 1).Trim(); // -1 to exclude the closing brace
                string packageRef = savePath;

                // 检查包是否已存在
                if (dependenciesStr.Contains($"\"{packageName}\":"))
                {
                    Debug.Log($"包 {packageName} 已存在于manifest.json中");
                    return;
                }

                // 准备新的dependencies内容
                string newDependenciesContent;
                if (dependenciesStr.Length > 0 && !string.IsNullOrWhiteSpace(dependenciesStr)) // 如果dependencies不为空
                {
                    // 移除尾随的空格和可能的逗号
                    dependenciesStr = dependenciesStr.TrimEnd(' ', '\t', '\n', '\r', ',');
                    newDependenciesContent = $"{dependenciesStr},\n    \"{packageName}\": \"{packageRef}\"";
                }
                else // 如果dependencies为空
                {
                    newDependenciesContent = $"    \"{packageName}\": \"{packageRef}\"";
                }

                // 替换原内容
                string newJsonContent = jsonContent.Substring(0, dependenciesObjStart) +
                                       newDependenciesContent +
                                       jsonContent.Substring(dependenciesObjEnd - 1); // -1 to include the closing brace

                File.WriteAllText(manifestPath, newJsonContent);

                Debug.Log($"成功添加包 {packageName} 到manifest.json: {packageRef}");

                // 刷新Unity的包管理器
                UnityEditor.PackageManager.Client.Resolve();
            }
            catch (Exception e)
            {
                Debug.LogError($"添加包到manifest.json时出错: {e.Message}");
            }
        }

        /// <summary>
        /// 从manifest.json中移除包
        /// </summary>
        /// <param name="packageName">包名称</param>
        private static void RemovePackageFromManifest(string packageName)
        {
            try
            {
                // 获取manifest.json文件路径
                string manifestPath = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), "Packages", "manifest.json");

                if (!File.Exists(manifestPath))
                {
                    Debug.LogError($"未找到manifest.json文件: {manifestPath}");
                    return;
                }

                // 读取现有的manifest.json内容
                string jsonContent = File.ReadAllText(manifestPath);

                // 查找 "dependencies" 的位置
                string dependenciesMarker = "\"dependencies\"";
                int dependenciesIndex = jsonContent.IndexOf(dependenciesMarker);

                if (dependenciesIndex == -1)
                {
                    Debug.LogError("未在manifest.json中找到dependencies部分");
                    return;
                }

                // 从dependencies标记后查找冒号和左大括号
                int colonIndex = jsonContent.IndexOf(":", dependenciesIndex);
                if (colonIndex == -1)
                {
                    Debug.LogError("未在manifest.json中找到dependencies后的冒号");
                    return;
                }

                int openBraceIndex = jsonContent.IndexOf('{', colonIndex);
                if (openBraceIndex == -1)
                {
                    Debug.LogError("未在manifest.json中找到dependencies后的左大括号");
                    return;
                }

                // 找到dependencies对象的开始位置（左大括号之后）
                int dependenciesObjStart = openBraceIndex + 1;

                // 提取dependencies对象部分
                int dependenciesObjEnd = FindMatchingBrace(jsonContent, openBraceIndex);
                if (dependenciesObjEnd == -1)
                {
                    Debug.LogError("manifest.json中dependencies部分格式错误");
                    return;
                }

                string dependenciesStr = jsonContent.Substring(dependenciesObjStart, dependenciesObjEnd - dependenciesObjStart - 1).Trim(); // -1 to exclude the closing brace

                // 检查包是否存在
                if (!dependenciesStr.Contains($"\"{packageName}\":"))
                {
                    Debug.Log($"包 {packageName} 不存在于manifest.json中");
                    return;
                }

                // 分割dependencies字符串为单独的行，然后逐个处理
                string[] lines = dependenciesStr.Split('\n');
                List<string> newLines = new List<string>();
                bool foundPackage = false;

                foreach (string line in lines)
                {
                    if (line.Trim().StartsWith($"\"{packageName}\":"))
                    {
                        foundPackage = true; // 标记找到了要删除的包
                        continue; // 跳过这一行（相当于删除）
                    }

                    // 如果不是最后一行，保留逗号
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        newLines.Add(line);
                    }
                }

                // 如果没找到要删除的包，直接返回
                if (!foundPackage)
                {
                    Debug.Log($"包 {packageName} 不存在于manifest.json中");
                    return;
                }

                // 重新构建dependencies内容
                string newDependenciesStr = string.Join("\n", newLines);
                newDependenciesStr = newDependenciesStr.TrimEnd(',', ' ', '\t', '\n', '\r');

                // 替换原内容
                string newJsonContent = jsonContent.Substring(0, dependenciesObjStart) +
                                       newDependenciesStr +
                                       jsonContent.Substring(dependenciesObjEnd - 1); // -1 to include the closing brace

                File.WriteAllText(manifestPath, newJsonContent);

                Debug.Log($"成功从manifest.json移除包 {packageName}");

                // 刷新Unity的包管理器
                UnityEditor.PackageManager.Client.Resolve();
            }
            catch (Exception e)
            {
                Debug.LogError($"从manifest.json移除包时出错: {e.Message}");
            }
        }

        /// <summary>
        /// 更新manifest.json中的包
        /// </summary>
        /// <param name="gitUrl">Git仓库URL</param>
        /// <param name="packageName">包名称</param>
        private static void UpdatePackageInManifest(string gitUrl, string packageName)
        {
            try
            {
                // 获取manifest.json文件路径
                string manifestPath = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), "Packages", "manifest.json");

                if (!File.Exists(manifestPath))
                {
                    Debug.LogError($"未找到manifest.json文件: {manifestPath}");
                    return;
                }

                // 读取现有的manifest.json内容
                string jsonContent = File.ReadAllText(manifestPath);

                // 查找 "dependencies" 的位置
                string dependenciesMarker = "\"dependencies\"";
                int dependenciesIndex = jsonContent.IndexOf(dependenciesMarker);

                if (dependenciesIndex == -1)
                {
                    Debug.LogError("未在manifest.json中找到dependencies部分");
                    return;
                }

                // 从dependencies标记后查找冒号和左大括号
                int colonIndex = jsonContent.IndexOf(":", dependenciesIndex);
                if (colonIndex == -1)
                {
                    Debug.LogError("未在manifest.json中找到dependencies后的冒号");
                    return;
                }

                int openBraceIndex = jsonContent.IndexOf('{', colonIndex);
                if (openBraceIndex == -1)
                {
                    Debug.LogError("未在manifest.json中找到dependencies后的左大括号");
                    return;
                }

                // 找到dependencies对象的开始位置（左大括号之后）
                int dependenciesObjStart = openBraceIndex + 1;

                // 提取dependencies对象部分
                int dependenciesObjEnd = FindMatchingBrace(jsonContent, openBraceIndex);
                if (dependenciesObjEnd == -1)
                {
                    Debug.LogError("manifest.json中dependencies部分格式错误");
                    return;
                }

                string dependenciesStr = jsonContent.Substring(dependenciesObjStart, dependenciesObjEnd - dependenciesObjStart - 1).Trim(); // -1 to exclude the closing brace
                string packageRef = $"git+{gitUrl}"; // 确保使用git协议

                // 检查包是否已存在，如果存在则更新它
                if (dependenciesStr.Contains($"\"{packageName}\":"))
                {
                    // 使用正则表达式替换包引用，保留原始格式
                    string pattern = $"\"{Regex.Escape(packageName)}\"\\s*:\\s*\"[^\"]*\"";
                    string replacement = $"\"{packageName}\": \"{packageRef}\"";
                    string updatedDependenciesStr = Regex.Replace(dependenciesStr, pattern, replacement);

                    // 替换原内容
                    string newJsonContent = jsonContent.Substring(0, dependenciesObjStart) +
                                           updatedDependenciesStr +
                                           jsonContent.Substring(dependenciesObjEnd - 1); // -1 to include the closing brace

                    File.WriteAllText(manifestPath, newJsonContent);

                    Debug.Log($"成功更新包 {packageName} 到manifest.json: {gitUrl}");
                }
                else
                {
                    Debug.LogWarning($"包 {packageName} 不存在于manifest.json中，无法更新");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"更新包到manifest.json时出错: {e.Message}");
            }
        }

        // 辅助方法：找到匹配的大括号
        public static int FindMatchingBrace(string json, int startIndex)
        {
            int braceCount = 0;
            bool inString = false;
            char prevChar = '\0';

            for (int i = startIndex; i < json.Length; i++)
            {
                char c = json[i];

                // 检查是否在字符串内（需要考虑转义字符）
                if (c == '"' && prevChar != '\\')
                {
                    inString = !inString;
                }

                if (!inString)
                {
                    if (c == '{')
                    {
                        braceCount++;
                    }
                    else if (c == '}')
                    {
                        braceCount--;
                        if (braceCount == 0)
                        {
                            return i + 1; // 返回匹配的右大括号之后的位置
                        }
                    }
                }

                prevChar = c;
            }

            return -1; // 未找到匹配的大括号
        }
    }
}