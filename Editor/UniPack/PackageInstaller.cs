/// -------------------------------------------------------------------------------
/// NovaEngine Installer Framework
///
/// Copyright (C) 2025, Hainan Yuanyou Information Technology Co., Ltd. Guangzhou Branch
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

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEditor;

namespace NovaEngine.Installer
{
    /// <summary>
    /// 包安装器对象类，用于安装插件包及包管理器的注册服务<br/>
    /// 安装的插件包位于本地工程根目录下的 `NovaFrameworkData` 文件夹中，
    /// </summary>
    internal static class PackageInstaller
    {
        static string gitSaveRootDir = Path.Combine(UnityEngine.Application.dataPath, "../NovaFrameworkData/framework_repo");
        static string gitHttpUrl = "https://github.com/yoseasoft";
        static string gitPackageConfigPath = gitSaveRootDir + "/gitPackageConfig.txt";
        static string manifestJsonPath = Path.Combine(UnityEngine.Application.dataPath, "../Packages/manifest.json");

        [MenuItem("Nova/Install/Install Git Repository Packages")]
        public static void InstallGitPackages()
        {
            string packagePath = "";
            string packageName = "";

            if (File.Exists(gitPackageConfigPath))
            {
                string[] lines = File.ReadAllLines(gitPackageConfigPath);
                foreach (string line in lines)
                {
                    packageName = line;
                    packagePath = Path.Combine(gitSaveRootDir, packageName);
                    if (Directory.Exists(packagePath))
                    {
                        Logger.Info($"已经存在 {packageName} 更新");
                        UpdateSingleGitPackage(packageName);
                    }
                    else
                    {
                        Logger.Info($"克隆 {packageName} ");
                        InstallSingleGitPackage(packageName);
                    }

                    Thread.Sleep(1);
                }
            }
            else
            {
                Logger.Error($"配置文件不存在 ：{gitPackageConfigPath}");
            }

            Logger.Info("修改ManifestJson");
            //ModifyManifestJson();
        }

        [MenuItem("Nova/Install/Update Git Repository Packages")]
        private static void UpdateGitRes()
        {
            string[] packageNameArray = GetPackageConfig();
            foreach (var packageName in packageNameArray)
            {
                UpdateSingleGitPackage(packageName);
            }
        }

        private static void InstallSingleGitPackage(string packageName)
        {
            string url = gitHttpUrl + "/" + packageName;
            Utils.Git.ExcuteGitClone(url, gitSaveRootDir);
        }

        private static void UpdateSingleGitPackage(string packageName)
        {
            string packageDir = Path.Combine(gitSaveRootDir, packageName);
            Utils.Git.ExcuteGitPull(packageDir);
        }

        private static string[] GetPackageConfig()
        {
            if (File.Exists(gitPackageConfigPath))
            {
                string[] lines = File.ReadAllLines(gitPackageConfigPath);
                return lines;
            }
            else
            {
                Logger.Error($"文件不存在 {gitPackageConfigPath}");
            }

            return null;
        }

        private static void ModifyManifestJson()
        {
            Dictionary<string, string> packageDict = new Dictionary<string, string>();
            string[] lines = GetPackageConfig();
            foreach (string line in lines)
            {
                string pack_json_path = gitSaveRootDir + "/" + line + "/" + "package.json";
                string[] contentArray = File.ReadAllLines(pack_json_path);
                string packageName = GetPackageName(contentArray[1]);
                packageDict.Add(line, packageName);

                Logger.Info(line + " " + packageName);
            }

            string[] jsonContent = File.ReadAllLines(manifestJsonPath, System.Text.Encoding.UTF8);

            //用文本方式处理json
            StringBuilder sb = new StringBuilder();
            foreach (string line in jsonContent)
            {
                //先填充需要加入的
                if (line.TrimStart().StartsWith("\"dependencies\":"))
                {
                    sb.AppendLine(line);
                    foreach (var item in packageDict)
                    {
                        string config_line = $"\"{item.Value}\": \"file:../NovaFrameworkData/framework_repo/{item.Key}\",";
                        sb.AppendLine(config_line);
                    }
                    continue;
                }

                //没有的部分移除
                if (line.Contains("file:../NovaFrameworkData/framework_repo"))
                {
                    //想删除的（没有删除的权限）
                    //int colonIndex = line.IndexOf(':');
                    //string packageName = line.Substring(0, colonIndex).Trim('"', ' ', '\t');
                    //if (!packagesList.Contains(packageName))
                    //{
                    //    string packagePath = Path.Combine(gitSaveRootDir, packageName);
                    //    if (Directory.Exists(packagePath))
                    //    {
                    //        Directory.Delete(packagePath, true);
                    //    }
                    //}
                    continue;
                }
                sb.AppendLine(line);
            }

            File.WriteAllText(manifestJsonPath, sb.ToString());
        }

        private static string GetPackageName(string line)
        {
            string packageName = "";
            // 步骤1：找到冒号后第一个双引号的位置
            int colonIndex = line.IndexOf(':');
            if (colonIndex == -1)
            {
                Logger.Error("未找到冒号，格式异常");
                return packageName;
            }
            int startQuoteIndex = line.IndexOf('"', colonIndex + 1);
            if (startQuoteIndex == -1)
            {
                Logger.Error("未找到包名起始双引号");
                return packageName;
            }

            // 步骤2：找到包名结束的双引号位置
            int endQuoteIndex = line.IndexOf('"', startQuoteIndex + 1);
            if (endQuoteIndex == -1)
            {
                Logger.Error("未找到包名结束双引号");
                return packageName;
            }

            // 步骤3：截取并输出目标包名
            packageName = line.Substring(startQuoteIndex + 1, endQuoteIndex - startQuoteIndex - 1);
            Logger.Info("提取的包名：" + packageName);

            return packageName;
        }
    }
}
