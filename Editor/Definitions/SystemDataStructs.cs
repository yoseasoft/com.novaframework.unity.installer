using System;
using System.Collections.Generic;

namespace NovaFramework.Editor.Installer
{
   // 为了兼容性，重新定义所需的数据结构
    [Serializable]
    internal class SystemEnvironmentConfig
    {
        public List<EnvironmentVariable> variables = new List<EnvironmentVariable>();
        public List<ModuleConfig> modules = new List<ModuleConfig>();
        public List<string> aot_libraries = new List<string>();
    }
    
    [Serializable]
    internal class EnvironmentVariable
    {
        public string key;
        public string value;
    }
    
    [Serializable]
    internal class ModuleConfig
    {
        public string name;
        public int order;
        public List<string> tags = new List<string>();
    }

}