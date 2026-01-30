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
using System.Xml;
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    /// <summary>
    /// 解析包配置XML文件的工具类
    /// </summary>
    internal class PackageXMLParser
    {
        /// <summary>
        /// 解析XML文件并返回包信息列表
        /// </summary>
        /// <param name="xmlPath">XML文件路径</param>
        /// <returns>包信息列表</returns>
        public static PackageXMLInfo ParseXML(string xmlPath)
        {
            PackageXMLInfo _packageXMLInfo = new PackageXMLInfo();
            
            try
            {
                // 首先读取XML内容并替换变量
                string xmlContent = System.IO.File.ReadAllText(xmlPath);
                
                // 解析变量定义
                _packageXMLInfo.environmentVariables = ParseEnvironmentVariables(xmlContent);
                
                // 解析系统路径定义
                _packageXMLInfo.systemPathInfos = ParseSystemPaths(xmlContent);
                
                // 替换变量
                foreach (var variable in _packageXMLInfo.environmentVariables)
                {
                    xmlContent = xmlContent.Replace($"%{variable.Key}%", variable.Value);
                }
                
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlContent);
                
                XmlNodeList packageNodes = xmlDoc.SelectNodes("//package");
                
                foreach (XmlNode node in packageNodes)
                {
                    PackageInfo package = CreatePackageFromXmlNode(node);
                    _packageXMLInfo.packageInfos.Add(package);
                }
                
                // 计算反向依赖（哪些包依赖于当前包）
                CalculateReverseDependencies(_packageXMLInfo.packageInfos);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing manifest file: {e.Message}");
            }

            return _packageXMLInfo;
        }
        
        /// <summary>
        /// 从XML节点创建PackageInfo对象
        /// </summary>
        /// <param name="node">XML节点</param>
        /// <returns>PackageInfo对象</returns>
        private static PackageInfo CreatePackageFromXmlNode(XmlNode node)
        {
            PackageInfo package = new PackageInfo();
            
            // 设置基本属性
            package.name = GetXmlAttribute(node, "name");
            package.displayName = GetXmlAttribute(node, "displayName");
            package.title = GetXmlAttribute(node, "title");
            
            // 设置描述信息
            package.description = GetDescription(node, package.title);
            
            // 设置必需包标识
            package.isRequired = GetXmlAttributeAsBool(node, "required");
            
            // 设置默认选择状态（必需包默认选中）
            package.isSelected = package.isRequired;
            
            // 设置Git仓库URL
            SetGitRepositoryUrl(node, package);
            
            // 设置依赖关系和其它属性
            SetDependencies(node, package);

            // 设置程序集定义信息
            SetAssemblyDefinition(node, package);

            // 设置资源路径
            SetAssetPath(node, package);
            
            return package;
        }
        
        /// <summary>
        /// 获取XML属性值，如果不存在则返回空字符串
        /// </summary>
        /// <param name="node">XML节点</param>
        /// <param name="attributeName">属性名</param>
        /// <returns>属性值或空字符串</returns>
        private static string GetXmlAttribute(XmlNode node, string attributeName)
        {
            return node.Attributes?[attributeName]?.Value ?? "";
        }
        
        /// <summary>
        /// 获取XML属性值并转换为布尔值
        /// </summary>
        /// <param name="node">XML节点</param>
        /// <param name="attributeName">属性名</param>
        /// <returns>布尔值</returns>
        private static bool GetXmlAttributeAsBool(XmlNode node, string attributeName)
        {
            string attrValue = GetXmlAttribute(node, attributeName);
            return string.Equals(attrValue, "true", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// 获取描述信息，优先从description节点获取，否则使用标题
        /// </summary>
        /// <param name="node">XML节点</param>
        /// <param name="fallbackTitle">备用标题</param>
        /// <returns>描述信息</returns>
        private static string GetDescription(XmlNode node, string fallbackTitle)
        {
            XmlNode descriptionNode = node.SelectSingleNode("description");
            if (descriptionNode != null)
            {
                return descriptionNode.InnerText ?? "";
            }
            
            return fallbackTitle;
        }
        
        /// <summary>
        /// 设置Git仓库URL
        /// </summary>
        /// <param name="node">XML节点</param>
        /// <param name="package">包信息对象</param>
        private static void SetGitRepositoryUrl(XmlNode node, PackageInfo package)
        {
            XmlNode gitRepoNode = node.SelectSingleNode("git-repository");
            if (gitRepoNode != null)
            {
                package.gitUrl = GetXmlAttribute(gitRepoNode, "url");
            }
        }
        
        /// <summary>
        /// 设置包的依赖关系和其它属性
        /// </summary>
        /// <param name="node">XML节点</param>
        /// <param name="package">包信息对象</param>
        private static void SetDependencies(XmlNode node, PackageInfo package)
        {
            // 设置依赖关系
            XmlNode dependenciesNode = node.SelectSingleNode("dependencies");
            if (dependenciesNode != null)
            {
                XmlNodeList referenceNodes = dependenciesNode.SelectNodes("reference-package");
                foreach (XmlNode refNode in referenceNodes)
                {
                    string refName = refNode.InnerText.Trim();
                    if (!string.IsNullOrEmpty(refName))
                    {
                        package.dependencies.Add(refName);
                    }
                }
            }
            
            // 设置排斥关系
            XmlNode repulsionsNode = node.SelectSingleNode("repulsions");
            if (repulsionsNode != null)
            {
                XmlNodeList repulsionNodes = repulsionsNode.SelectNodes("reference-package");
                foreach (XmlNode refNode in repulsionNodes)
                {
                    string refName = refNode.InnerText.Trim();
                    if (!string.IsNullOrEmpty(refName))
                    {
                        package.repulsions.Add(refName);
                    }
                }
            }
        }
        
        /// <summary>
        /// 设置程序集定义信息
        /// </summary>
        /// <param name="node">XML节点</param>
        /// <param name="package">包信息对象</param>
        private static void SetAssemblyDefinition(XmlNode node, PackageInfo package)
        {
            XmlNode assemblyDefNode = node.SelectSingleNode("assembly-definition");
            if (assemblyDefNode != null)
            {
                AssemblyDefinitionInfo asmDef = new AssemblyDefinitionInfo();
                asmDef.name = GetXmlAttribute(assemblyDefNode, "name");
                
                // 获取order属性
                string orderStr = GetXmlAttribute(assemblyDefNode, "order");
                if (int.TryParse(orderStr, out int orderValue))
                {
                    asmDef.order = orderValue;
                }
                
                // 获取loadable-strategy节点
                XmlNodeList strategyNodes = assemblyDefNode.SelectNodes("loadable-strategy");
                foreach (XmlNode strategyNode in strategyNodes)
                {
                    string strategyValue = strategyNode.InnerText.Trim();
                    if (!string.IsNullOrEmpty(strategyValue))
                    {
                        asmDef.loadableStrategies.Add(strategyValue);
                    }
                }
                
                package.assemblyDefinitionInfo = asmDef;
            }
            else
            {
                // 如果没有assembly-definition节点，则保持为null
                package.assemblyDefinitionInfo = null;
            }
        }
        
        /// <summary>
        /// 设置assets-path
        /// </summary>
        /// <param name="node">XML节点</param>
        /// <param name="package">包信息对象</param>
        private static void SetAssetPath(XmlNode node, PackageInfo package)
        {
            XmlNode assetsPathNode = node.SelectSingleNode("assets-path");
            if (assetsPathNode != null)
            {
                string assetPath = assetsPathNode.InnerText.Trim();
                if (!string.IsNullOrEmpty(assetPath))
                {
                    package.assetsPath = assetPath;
                }
            }
        }
        
        /// <summary>
        /// 计算反向依赖（哪些包依赖于当前包）
        /// </summary>
        /// <param name="packages">包列表</param>
        private static void CalculateReverseDependencies(List<PackageInfo> packages)
        {
            foreach (var package in packages)
            {
                foreach (var otherPackage in packages)
                {
                    if (otherPackage != package && otherPackage.dependencies.Contains(package.name))
                    {
                        package.reverseDependencies.Add(otherPackage.name);
                    }
                }
            }
        }
        
        /// <summary>
        /// 解析XML中的变量定义
        /// </summary>
        /// <param name="xmlContent">XML内容</param>
        /// <returns>变量字典</returns>
        private static Dictionary<string, string> ParseVariables(string xmlContent)
        {
            Dictionary<string, string> variables = new Dictionary<string, string>();
            
            try
            {
                XmlDocument tempDoc = new XmlDocument();
                tempDoc.LoadXml(xmlContent);
                
                XmlNodeList setNodes = tempDoc.SelectNodes("//set[@name and @value]");
                
                foreach (XmlNode setNode in setNodes)
                {
                    string name = setNode.Attributes["name"]?.Value ?? "";
                    string value = setNode.Attributes["value"]?.Value ?? "";
                    
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                    {
                        variables[name] = value;
                        Debug.Log($"解析变量: {name} = {value}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"解析变量时出错: {e.Message}");
            }
            
            return variables;
        }
        
        /// <summary>
        /// 解析XML中的系统路径定义
        /// </summary>
        /// <param name="xmlContent">XML内容</param>
        private static List<SystemPathInfo> ParseSystemPaths(string xmlContent)
        {
            List<SystemPathInfo> systemPathInfos = new List<SystemPathInfo>();
            
            try
            {
                XmlDocument tempDoc = new XmlDocument();
                tempDoc.LoadXml(xmlContent);
                
                XmlNodeList systemPathNodes = tempDoc.SelectNodes("//system-path/local-path");
                
                foreach (XmlNode localPathNode in systemPathNodes)
                {
                    string name = localPathNode.Attributes["name"]?.Value ?? "";
                    string defaultValue = localPathNode.Attributes["defaultValue"]?.Value ?? "";
                    string title = localPathNode.Attributes["title"]?.Value ?? "";
                    string isRequiredStr = localPathNode.Attributes["required"]?.Value ?? "false"; // 注意：这里属性名是 'required' 而不是 'isRequired'
                    
                    if (!string.IsNullOrEmpty(name))
                    {
                        SystemPathInfo systemPath = new SystemPathInfo();
                        systemPath.name = name;
                        systemPath.defaultValue = defaultValue;
                        systemPath.title = title;
                        systemPath.isRequired = string.Equals(isRequiredStr, "true", StringComparison.OrdinalIgnoreCase);
                        
                        systemPathInfos.Add(systemPath);
                        
                        //Debug.Log($"解析系统路径: {name} = {defaultValue}, 标题: {title}, 必需: {systemPath.isRequired}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"解析系统路径时出错: {e.Message}");
            }

            return systemPathInfos;
        }
        
        /// <summary>
        /// 解析XML中的环境变量定义
        /// </summary>
        /// <param name="xmlContent">XML内容</param>
        private static Dictionary<string, string> ParseEnvironmentVariables(string xmlContent)
        {
            Dictionary<string, string> environmentVariables = new Dictionary<string, string>();
            
            try
            {
                XmlDocument tempDoc = new XmlDocument();
                tempDoc.LoadXml(xmlContent);
                
                XmlNodeList envVarNodes = tempDoc.SelectNodes("//environment-variable[@name and @value]");
                
                foreach (XmlNode envVarNode in envVarNodes)
                {
                    string name = envVarNode.Attributes["name"]?.Value ?? "";
                    string value = envVarNode.Attributes["value"]?.Value ?? "";
                    
                    if (!string.IsNullOrEmpty(name))
                    {
                        environmentVariables.Add(name, value);
                        
                        Debug.Log($"解析环境变量: {name} = {value}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"解析环境变量时出错: {e.Message}");
            }

            return environmentVariables;
        }
    }
}