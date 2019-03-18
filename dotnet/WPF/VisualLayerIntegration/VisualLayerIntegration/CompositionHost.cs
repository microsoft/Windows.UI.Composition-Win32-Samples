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
using System.Windows;
using System.Windows.Interop;
using WinComp.Interop;
using Windows.UI.Composition;

namespace VisualLayerIntegration
{
      class CompositionHost : HwndHost
    {
        object dispatcherQueue;
        int hostHeight, hostWidth;
        double hostDpiX, hostDpiY;
        private ICompositionTarget compositionTarget;
        internal const int
            WS_CHILD = 0x40000000,
            WS_VISIBLE = 0x10000000,
            LBS_NOTIFY = 0x00000001,
            HOST_ID = 0x00000002,
            LISTBOX_ID = 0x00000001,
            WS_VSCROLL = 0x00200000,
            WS_BORDER = 0x00800000;


        public Visual Child
        {
            set
            {
                if (Compositor == null)
                {
                    InitComposition(hwndHost);
                }
                compositionTarget.Root = value;
            }
        }
        public IntPtr hwndHost { get; private set; }
        public Compositor Compositor { get; private set; }

        public CompositionHost(double height, double width, double dpiX, double dpiY)
        {
            hostHeight = (int)height;
            hostWidth = (int)width;
            hostDpiX = (double)dpiX;
            hostDpiY = (double)dpiY;
        }

        // Create window and content
        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            // Create Window.
            hwndHost = User32.CreateWindowExW(
                                   dwExStyle: 0,
                                   lpClassName: "Message",
                                   lpWindowName: "CompositionHost",
                                   dwStyle: User32.WS.WS_CHILD,
                                   x: 0, y: 0,
                                   nWidth: 0, nHeight: 0,
                                   hWndParent: hwndParent.Handle,
                                   hMenu: IntPtr.Zero,
                                   hInstance: IntPtr.Zero,
                                   lpParam: IntPtr.Zero);

            // Create dispatcher queue.
            dispatcherQueue = InitializeCoreDispatcher();

            // Get compositor and target for hwnd.
            InitComposition(hwndHost);

            return new HandleRef(this, hwndHost);
        }

        // Create dispatcher queue.
        private object InitializeCoreDispatcher()
        {
            var options = new DispatcherQueueOptions();
            options.apartmentType = DISPATCHERQUEUE_THREAD_APARTMENTTYPE.DQTAT_COM_STA;
            options.threadType = DISPATCHERQUEUE_THREAD_TYPE.DQTYPE_THREAD_CURRENT;
            options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));

            CreateDispatcherQueueController(options, out object queue);
            return queue;
        }

        // Get compositor and target for hwnd.
        private void InitComposition(IntPtr hwndHost)
        {
            ICompositorDesktopInterop interop;

            this.Compositor = new Compositor();
            object iunknown = Compositor as object;
            interop = (ICompositorDesktopInterop)iunknown;
            interop.CreateDesktopWindowTarget(hwndHost, true, out IntPtr raw);

            object rawObject = Marshal.GetObjectForIUnknown(raw);
            compositionTarget = (ICompositionTarget)rawObject;

            if (raw == null) { throw new Exception("QI Failed"); }

        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            if (compositionTarget.Root != null)
            {
                compositionTarget.Root.Dispose();
            }
            DestroyWindow(hwnd.Handle);
        }

        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((User32.WM)msg)
            {
                case User32.WM.WM_MOUSEMOVE:
                    var pos = PointToScreen(new Point((short)(((int)lParam) & 0xffff), (short)(((int)lParam) >> 16)));
                    RaiseHwndMouseMove(new HwndMouseEventArgs(pos));
                    break;
                case User32.WM.WM_LBUTTONDOWN:
                    RaiseHwndMouseLClick(new HwndMouseEventArgs());
                    break;
                case User32.WM.WM_PAINT:
                    RaisePaint();
                    break;
            }
            
            return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
        }

        void RaisePaint()
        {
            var args = new InvalidateDrawingEventArgs();
            args.Width = ActualWidth;
            args.Height = ActualHeight;
            InvalidateDrawing?.Invoke(this, args);
        }

        protected virtual void RaiseHwndMouseMove(HwndMouseEventArgs args)
        {
            MouseMoved?.Invoke(this, args);
        }

        protected virtual void RaiseHwndMouseLClick(HwndMouseEventArgs args)
        {
            MouseLClick?.Invoke(this, args);
        }

        public event EventHandler<HwndMouseEventArgs> MouseLClick;
        public event EventHandler<HwndMouseEventArgs> MouseMoved;
        public event EventHandler<InvalidateDrawingEventArgs> InvalidateDrawing;

        #region PInvoke declarations

        // Win32 enum we're duplicating.
        //typedef enum DISPATCHERQUEUE_THREAD_APARTMENTTYPE
        //{
        //    DQTAT_COM_NONE,
        //    DQTAT_COM_ASTA,
        //    DQTAT_COM_STA
        //};
        internal enum DISPATCHERQUEUE_THREAD_APARTMENTTYPE
        {
            DQTAT_COM_NONE = 0,
            DQTAT_COM_ASTA = 1,
            DQTAT_COM_STA = 2
        };

        // Win32 enum we're duplicating.
        //typedef enum DISPATCHERQUEUE_THREAD_TYPE
        //{
        //    DQTYPE_THREAD_DEDICATED,
        //    DQTYPE_THREAD_CURRENT
        //};
        internal enum DISPATCHERQUEUE_THREAD_TYPE
        {
            DQTYPE_THREAD_DEDICATED = 1,
            DQTYPE_THREAD_CURRENT = 2,
        };

        // Win32 struct we're duplicating.
        //struct DispatcherQueueOptions
        //{
        //    DWORD dwSize;
        //    DISPATCHERQUEUE_THREAD_TYPE threadType;
        //    DISPATCHERQUEUE_THREAD_APARTMENTTYPE apartmentType;
        //};
        [StructLayout(LayoutKind.Sequential)]
        internal struct DispatcherQueueOptions
        {
            public int dwSize;

            [MarshalAs(UnmanagedType.I4)]
            public DISPATCHERQUEUE_THREAD_TYPE threadType;

            [MarshalAs(UnmanagedType.I4)]
            public DISPATCHERQUEUE_THREAD_APARTMENTTYPE apartmentType;
        };

        // Win32 method signature we're duplicating.
        //HRESULT CreateDispatcherQueueController(
        //  DispatcherQueueOptions options,
        //  ABI::Windows::System::IDispatcherQueueController** dispatcherQueueController
        //);
        [DllImport("coremessaging.dll", EntryPoint = "CreateDispatcherQueueController", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateDispatcherQueueController(DispatcherQueueOptions options,
                                                [MarshalAs(UnmanagedType.IUnknown)]
                                               out object dispatcherQueueController);


        [DllImport("user32.dll", EntryPoint = "CreateWindowEx", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateWindowEx(int dwExStyle,
                                                      string lpszClassName,
                                                      string lpszWindowName,
                                                      int style,
                                                      int x, int y,
                                                      int width, int height,
                                                      IntPtr hwndParent,
                                                      IntPtr hMenu,
                                                      IntPtr hInst,
                                                      [MarshalAs(UnmanagedType.AsAny)] object pvParam);

        [DllImport("user32.dll", EntryPoint = "DestroyWindow", CharSet = CharSet.Unicode)]
        internal static extern bool DestroyWindow(IntPtr hwnd);

        #endregion PInvoke declarations
    }

    public class InvalidateDrawingEventArgs : EventArgs
    {
        public double Width { get; set; }
        public double Height { get; set; }
    }

    public class HwndMouseEventArgs : EventArgs
    {
        public Point point { get; set; }

        public HwndMouseEventArgs(Point point)
        {
            this.point = point;
        }
        public HwndMouseEventArgs() { }
    }


    #region COM Interop

    // COM interface we're duplicating.
    //#undef INTERFACE
    //#define INTERFACE ICompositorDesktopInterop
    //    DECLARE_INTERFACE_IID_(ICompositorDesktopInterop, IUnknown, "29E691FA-4567-4DCA-B319-D0F207EB6807")
    //    {
    //        IFACEMETHOD(CreateDesktopWindowTarget)(
    //            _In_ HWND hwndTarget,
    //            _In_ BOOL isTopmost,
    //            _COM_Outptr_ IDesktopWindowTarget * *result
    //            ) PURE;
    //    };
    [ComImport]
    [Guid("29E691FA-4567-4DCA-B319-D0F207EB6807")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ICompositorDesktopInterop
    {
        void CreateDesktopWindowTarget(IntPtr hwndTarget, bool isTopmost, out IntPtr test);
    }

    // COM interface we're duplicating.
    //[contract(Windows.Foundation.UniversalApiContract, 2.0)]
    //[exclusiveto(Windows.UI.Composition.CompositionTarget)]
    //[uuid(A1BEA8BA - D726 - 4663 - 8129 - 6B5E7927FFA6)]
    //interface ICompositionTarget : IInspectable
    //{
    //    [propget] HRESULT Root([out] [retval] Windows.UI.Composition.Visual** value);
    //    [propput] HRESULT Root([in] Windows.UI.Composition.Visual* value);
    //}

    [ComImport]
    [Guid("A1BEA8BA-D726-4663-8129-6B5E7927FFA6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
    public interface ICompositionTarget
    {
        Windows.UI.Composition.Visual Root
        {
            get;
            set;
        }
    }

    #endregion COM Interop

}
