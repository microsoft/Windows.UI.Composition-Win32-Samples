//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace WinComp.Interop
{

    public static class User32
    {
        public enum WS_EX
        {
            None = 0,
            WS_EX_LAYERED = 0x80000,
            WS_EX_TRANSPARENT = 0x00000020,
        }

        public enum WS
        {
            None = 0,
            WS_CHILD = 0x40000000,
            WS_VISIBLE = 0x10000000,
        }

        public enum WM
        {
            WM_PAINT = 0xf,
            WM_SETCURSOR = 0x20,
            WM_WINDOWPOSCHANGING = 0x46,
            WM_NCHITTEST = 0x84,
            WM_MOUSEMOVE = 0x200,
            WM_LBUTTONDOWN = 0x201,
        }
        
        [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateWindowExW(
           WS_EX dwExStyle,
           string lpClassName,
           string lpWindowName,
           WS dwStyle,
           int x,
           int y,
           int nWidth,
           int nHeight,
           IntPtr hWndParent,
           IntPtr hMenu,
           IntPtr hInstance,
           IntPtr lpParam);

        [DllImport("user32", SetLastError = true)]
        public static extern bool DestroyWindow(IntPtr hWnd);

    }

}