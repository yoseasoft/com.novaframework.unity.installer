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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NovaFramework.Editor.Installer
{
    public class RichTextUtils
    {
        // 定义富文本样式
        public static GUIStyle GetRichTextStyle(Color color, int fontSize = 12, FontStyle fontStyle = FontStyle.Normal)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = color;
            style.fontSize = fontSize;
            style.fontStyle = fontStyle;
            style.wordWrap = true;
            return style;
        }

        
        public static GUIStyle GetBoldRichTextStyle(Color color, int fontSize = 12)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = color;
            style.fontSize = fontSize;
            style.fontStyle = FontStyle.Bold;
            return style;
        }
        
        // 专门用于按钮的样式
        public static GUIStyle GetButtonStyle(Color textColor, Color backgroundColor)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.textColor = textColor;
            buttonStyle.hover.textColor = textColor;
            buttonStyle.active.textColor = textColor;
            buttonStyle.focused.textColor = textColor;
            
            // 设置背景色
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, backgroundColor);
            tex.Apply();
            buttonStyle.normal.background = tex;
            
            return buttonStyle;
        }
        
        // 仅改变按钮文字颜色，保持原有背景
        public static GUIStyle GetButtonTextOnlyStyle(Color textColor)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.textColor = textColor;
            buttonStyle.hover.textColor = textColor;
            buttonStyle.active.textColor = textColor;
            buttonStyle.focused.textColor = textColor;
        
            // 不改变背景，保持原样
            return buttonStyle;
        }
    }
}

