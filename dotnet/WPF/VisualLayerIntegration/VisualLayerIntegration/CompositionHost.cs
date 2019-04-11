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
    sealed class CompositionHost : HwndHost
    {
        private object _dispatcherQueue;
        private ICompositionTarget _compositionTarget;
       
        public IntPtr hwndHost { get; private set; }
        public Compositor Compositor { get; private set; }

        public event EventHandler<HwndMouseEventArgs> MouseLClick;
        public event EventHandler<HwndMouseEventArgs> MouseMoved;
        public event EventHandler<InvalidateDrawingEventArgs> InvalidateDrawing;

        public CompositionHost() {   }

        public void SetChild(Visual v)
        {
            if (Compositor == null)
            {
                InitializeComposition(hwndHost);
            }
            _compositionTarget.Root = v;
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
            _dispatcherQueue = InitializeCoreDispatcher();

            // Get compositor and target for hwnd.
            InitializeComposition(hwndHost);

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
        private void InitializeComposition(IntPtr hwndHost)
        {
            Compositor = new Compositor();
            var compositorDesktopInterop = (ICompositorDesktopInterop)(object)Compositor;
            compositorDesktopInterop.CreateDesktopWindowTarget(hwndHost, true, out _compositionTarget);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            if (_compositionTarget.Root != null)
            {
                _compositionTarget.Root.Dispose();
            }
            User32.DestroyWindow(hwnd.Handle);
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

        private void RaiseHwndMouseMove(HwndMouseEventArgs args)
        {
            MouseMoved?.Invoke(this, args);
        }

        private void RaiseHwndMouseLClick(HwndMouseEventArgs args)
        {
            MouseLClick?.Invoke(this, args);
        }

        #region PInvoke declarations

        internal enum DISPATCHERQUEUE_THREAD_APARTMENTTYPE
        {
            DQTAT_COM_NONE = 0,
            DQTAT_COM_ASTA = 1,
            DQTAT_COM_STA = 2
        };

        internal enum DISPATCHERQUEUE_THREAD_TYPE
        {
            DQTYPE_THREAD_DEDICATED = 1,
            DQTYPE_THREAD_CURRENT = 2,
        };

        internal struct DispatcherQueueOptions
        {
            public int dwSize;

            [MarshalAs(UnmanagedType.I4)]
            public DISPATCHERQUEUE_THREAD_TYPE threadType;

            [MarshalAs(UnmanagedType.I4)]
            public DISPATCHERQUEUE_THREAD_APARTMENTTYPE apartmentType;
        };

        [DllImport("coremessaging.dll")]
        internal static extern IntPtr CreateDispatcherQueueController(DispatcherQueueOptions options,
                                                [MarshalAs(UnmanagedType.IUnknown)]
                                               out object dispatcherQueueController);
        
        #endregion PInvoke declarations
    }

    sealed class InvalidateDrawingEventArgs : EventArgs
    {
        public double Width { get; set; }
        public double Height { get; set; }
    }

    sealed class HwndMouseEventArgs : EventArgs
    {
        public Point point { get; set; }

        public HwndMouseEventArgs(Point point)
        {
            this.point = point;
        }
        public HwndMouseEventArgs() { }
    }


    #region COM Interop

    [ComImport]
    [Guid("29E691FA-4567-4DCA-B319-D0F207EB6807")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ICompositorDesktopInterop
    {
        void CreateDesktopWindowTarget(IntPtr hwndTarget, bool isTopmost, out ICompositionTarget target);
    }

    [ComImport]
    [Guid("A1BEA8BA-D726-4663-8129-6B5E7927FFA6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
    internal interface ICompositionTarget
    {
        Windows.UI.Composition.Visual Root
        {
            get;
            set;
        }
    }

    #endregion COM Interop

}
