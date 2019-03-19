# ScreenCaptureforHWND 
A simple sample using the Windows.Graphics.Capture APIs in a Win32 application.

This sample uses new APIs available in **19H1 insider builds, SDK 18334 or greater**.
`CreateForWindow` (HMON) APIs are in the Windows.Graphics.Capture.Interop.h header.

>Note: Minimized windows are enumerated but not captured

## Win32 vs UWP
For the most part, using the API is the same between Win32 and UWP. However, there are some small differences.

1. The `GraphicsCapturePicker` won't be able to infer your window in Win32, so you'll have to provide your window's HWND.
2. `Direct3D11CaptureFramePool` requires a `DispatcherQueue` much like the Composition APIs. You'll need to create a dispatcher for your thread.


### Insiders

[Windows Insider Preview Downloads](https://www.microsoft.com/en-us/software-download/windowsinsiderpreviewSDK)

### More Information

[Windows.Graphics.Capture Namespace](https://docs.microsoft.com/uwp/api/windows.graphics.capture)
