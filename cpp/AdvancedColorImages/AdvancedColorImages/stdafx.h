// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once
#include <Unknwn.h>
#include "targetver.h"

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers

// C RunTime Header Files
#include <stdlib.h>
#include <malloc.h>
#include <memory.h>
#include <tchar.h>
#include <shcore.h>
#include <shobjidl_core.h>
#include <ppltasks.h>


#include <winrt/Windows.System.h>
#include <winrt/Windows.Storage.h>
#include <winrt/Windows.Storage.Streams.h>
#include <winrt/Windows.Storage.Pickers.h>
#include <winrt/Windows.UI.Composition.Desktop.h>
#include <winrt/Windows.UI.Composition.Interactions.h>
#include <winrt/Windows.UI.Input.h>
#include <winrt/Windows.Graphics.h>
#include <winrt/Windows.Graphics.Display.h>
#include <winrt/Windows.Graphics.DirectX.h>
#include <winrt/Windows.Web.Syndication.h>

WINRT_WARNING_PUSH

#include <dxgi1_6.h>
#include <d2d1_3.h>
#include <d3d11_3.h>
#include <Dwrite_3.h>
#include <wincodec.h>
#include <windows.foundation.h>
#include <windows.foundation.numerics.h>
#include <windows.ui.composition.interop.h>
#include <ShellScalingAPI.h>
#include <DispatcherQueue.h>


// TODO: reference additional headers your program requires here
