using System;
using System.Collections.Generic;

namespace NovaFramework.Editor.Installer
{
    internal class PackageXMLInfo
    {
        public Dictionary<string, string> environmentVariables = new Dictionary<string, string>();
        public List<SystemPathInfo> systemPathInfos = new List<SystemPathInfo>();
        public List<PackageInfo> packageInfos = new List<PackageInfo>();
    }
    
    internal class SystemPathInfo
    {
        public string name;
        public string defaultValue;
        public string title;
        public bool isRequired = false;
    }
    
    [Serializable]
    internal class AssemblyDefinitionInfo
    {
        public string name;
        public int order = 0; // 排序值
        public List<string> loadableStrategies = new List<string>();
    }
    
    [Serializable]
    internal class PackageInfo
    {
        public string name;
        public string displayName;
        public string title;
        public string description; // 添加描述字段
        public string gitUrl;
        public bool isRequired;
        public bool isSelected;
        public List<string> reverseDependencies = new List<string>(); //反向依赖项（引用该包的包）
        public List<string> dependencies = new List<string>(); //依赖项
        public List<string> repulsions = new List<string>(); // 与该包冲突的包列表
        public AssemblyDefinitionInfo assemblyDefinitionInfo; // 程序集定义信息
        public string assetsPath;
    }
}

