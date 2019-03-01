# ScreenCaptureforHWND 
A simple sample using the Windows.Graphics.Capture APIs in a Win32 application.

## Win32 vs UWP
For the most part, using the API is the same between Win32 and UWP. However, there are some small differences.

1. The `GraphicsCapturePicker` won't be able to infer your window in a Win32, so you'll have to provide your window's HWND.
2. `Direct3D11CaptureFramePool` requires a `DispatcherQueue` much like the Composition APIs. You'll need to create a dispatcher for your thread.
