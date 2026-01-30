using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    internal class ZipHelper
    {
        /// <summary>
        /// 解压ZIP文件到指定目录
        /// </summary>
        /// <param name="zipPath">ZIP文件路径</param>
        /// <param name="extractPath">解压目标目录</param>
        public static void ExtractZipFile(string zipPath, string extractPath)
        {
            if (!File.Exists(zipPath))
            {
                Debug.LogError($"ZIP文件不存在: {zipPath}");
                return;
            }

            if (!Directory.Exists(extractPath))
            {
                Directory.CreateDirectory(extractPath);
            }

            try
            {
                ZipFile.ExtractToDirectory(zipPath, extractPath);
                
                // 刷新Unity资源
                AssetDatabase.Refresh();
                
                Debug.Log($"成功解压ZIP文件: {zipPath} -> {extractPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"解压ZIP文件时出错: {ex.Message}");
                throw;
            }
        }
        
    }
}