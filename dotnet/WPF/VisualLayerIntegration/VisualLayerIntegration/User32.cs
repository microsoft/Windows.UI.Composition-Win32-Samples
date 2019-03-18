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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WNDCLASSEX
        {
            [MarshalAs(UnmanagedType.U4)]
            int cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public int style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;

            public static WNDCLASSEX Build()
            {
                var nw = new WNDCLASSEX();
                nw.cbSize = Marshal.SizeOf(typeof(WNDCLASSEX));
                return nw;
            }
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

        [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateWindowExW(
           WS_EX dwExStyle,
           IntPtr classAtom,
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

        [DllImport("user32", SetLastError = true, CharSet =CharSet.Unicode)]
        public static extern ushort RegisterClassEx([In] in WNDCLASSEX lpwcx);
    }

}