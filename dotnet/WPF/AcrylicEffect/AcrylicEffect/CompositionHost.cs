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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using WinComp.Interop;
using Windows.UI.Composition;

namespace AcrylicEffect
{
    sealed class CompositionHost : HwndHost
    {
        private readonly object _dispatcherQueue;
        private readonly List<IDisposable> _registeredDisposables = new List<IDisposable>();
        private readonly ICompositorDesktopInterop _compositorDesktopInterop;

        private ICompositionTarget _compositionTarget;

        public IntPtr HwndHost { get; private set; }
        public Compositor Compositor { get; private set; }

        public CompositionHost()
        {
            // Create dispatcher queue.
            _dispatcherQueue = InitializeCoreDispatcher();

            Compositor = new Compositor();
            _compositorDesktopInterop = (ICompositorDesktopInterop)(object)Compositor;
        }

        public void SetChild(Visual v)
        {
            _compositionTarget.Root = v;
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            // Create Window
            HwndHost = IntPtr.Zero;
            HwndHost = User32.CreateWindowExW(
                                   dwExStyle: User32.WS_EX.WS_EX_TRANSPARENT,
                                   lpClassName: "Message",
                                   lpWindowName: "CompositionHost",
                                   dwStyle: User32.WS.WS_CHILD,
                                   x: 0, y: 0,
                                   nWidth: 0, nHeight: 0,
                                   hWndParent: hwndParent.Handle,
                                   hMenu: IntPtr.Zero,
                                   hInstance: IntPtr.Zero,
                                   lpParam: IntPtr.Zero);

            // Get compositor and target for hwnd.
            _compositorDesktopInterop.CreateDesktopWindowTarget(HwndHost, true, out _compositionTarget);

            return new HandleRef(this, HwndHost);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            if (_compositionTarget.Root != null)
            {
                _compositionTarget.Root.Dispose();
            }

            User32.DestroyWindow(hwnd.Handle);

            foreach (var d in _registeredDisposables)
            {
                d.Dispose();
            }
        }

        // Register a given IDisposable object for disposal during cleanup.
        public void RegisterForDispose(IDisposable d)
        {
            _registeredDisposables.Add(d);
        }

        private object InitializeCoreDispatcher()
        {
            DispatcherQueueOptions options = new DispatcherQueueOptions
            {
                apartmentType = DISPATCHERQUEUE_THREAD_APARTMENTTYPE.DQTAT_COM_STA,
                threadType = DISPATCHERQUEUE_THREAD_TYPE.DQTYPE_THREAD_CURRENT,
                dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions))
            };

            var hresult = CreateDispatcherQueueController(options, out object queue);
            if (hresult != 0)
            {
                Marshal.ThrowExceptionForHR(hresult);
            }

            return queue;
        }

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

        [StructLayout(LayoutKind.Sequential)]
        internal struct DispatcherQueueOptions
        {
            public int dwSize;

            [MarshalAs(UnmanagedType.I4)]
            public DISPATCHERQUEUE_THREAD_TYPE threadType;

            [MarshalAs(UnmanagedType.I4)]
            public DISPATCHERQUEUE_THREAD_APARTMENTTYPE apartmentType;
        };

        [DllImport("coremessaging.dll")]
        internal static extern int CreateDispatcherQueueController(DispatcherQueueOptions options,
                                                [MarshalAs(UnmanagedType.IUnknown)]
                                               out object dispatcherQueueController);
    }

    [ComImport]
    [Guid("29E691FA-4567-4DCA-B319-D0F207EB6807")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ICompositorDesktopInterop
    {
        void CreateDesktopWindowTarget(IntPtr hwndTarget, bool isTopmost, out ICompositionTarget target);
    }

    [ComImport]
    [Guid("A1BEA8BA-D726-4663-8129-6B5E7927FFA6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
    public interface ICompositionTarget
    {
        Visual Root
        {
            get;
            set;
        }
    }
}
