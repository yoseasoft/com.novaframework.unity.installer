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

namespace NovaFramework.Editor.Installer
{
    /// <summary>
    /// 程序集标签位标志枚举定义
    /// </summary>
    [Flags]
    public enum AssemblyTag
    {
        Core = 0x0001,      // 核心模块
        Module = 0x0002,    // 模块
        Game = 0x0004,      // 游戏相关
        Tutorial = 0x0010,  // 教程相关
        Test = 0x0020,      // 测试相关
        Shared = 0x0100,    // 共享组件
        Hotfix = 0x0200     // 热更新相关
    }

    /// <summary>
    /// 程序集标签辅助类
    /// </summary>
    public static class AssemblyTagHelper
    {
        /// <summary>
        /// 将标签字符串列表转换为位标志枚举值
        /// </summary>
        /// <param name="tagNames">标签字符串列表</param>
        /// <returns>位标志枚举值</returns>
        public static AssemblyTag ConvertTagsToBitmask(System.Collections.Generic.List<string> tagNames)
        {
            AssemblyTag mask = 0;
            
            if (tagNames == null) return mask;
            
            foreach (string tagName in tagNames)
            {
                AssemblyTag tag = tagName switch
                {
                    "Core" => AssemblyTag.Core,
                    "Module" => AssemblyTag.Module,  // 新的Module标签
                    "Game" => AssemblyTag.Game,
                    "Tutorial" => AssemblyTag.Tutorial,
                    "Test" => AssemblyTag.Test,
                    "Shared" => AssemblyTag.Shared,
                    "Hotfix" => AssemblyTag.Hotfix,
                    _ => 0
                };
                
                mask |= tag;
            }
            
            return mask;
        }
        
        /// <summary>
        /// 将位标志枚举值转换为标签字符串列表
        /// </summary>
        /// <param name="mask">位标志枚举值</param>
        /// <returns>标签字符串列表</returns>
        public static System.Collections.Generic.List<string> ConvertBitmaskToTags(AssemblyTag mask)
        {
            var tagNames = new System.Collections.Generic.List<string>();
            
            if ((mask & AssemblyTag.Core) != 0) tagNames.Add("Core");
            if ((mask & AssemblyTag.Module) != 0) tagNames.Add("Module"); 
            if ((mask & AssemblyTag.Game) != 0) tagNames.Add("Game");
            if ((mask & AssemblyTag.Tutorial) != 0) tagNames.Add("Tutorial");
            if ((mask & AssemblyTag.Test) != 0) tagNames.Add("Test");
            if ((mask & AssemblyTag.Shared) != 0) tagNames.Add("Shared");
            if ((mask & AssemblyTag.Hotfix) != 0) tagNames.Add("Hotfix");
            
            return tagNames;
        }
        
        /// <summary>
        /// 获取所有可用的标签名称
        /// </summary>
        /// <returns>所有可用的标签名称数组</returns>
        public static string[] GetAllTagNames()
        {
            return new[]
            {
                "Core",
                "Module",  
                "Game",
                "Tutorial",
                "Test",
                "Shared",
                "Hotfix"
            };
        }
    }
}