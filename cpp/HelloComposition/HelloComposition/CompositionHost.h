#pragma once
// This line needs to be the first line in this file.  
#include <Unknwn.h>

// WinRT
#include <roapi.h>
#include <winstring.h>
#include <activation.h>
#include <wrl\client.h>
#include <wrl\event.h>
#include <wrl\wrappers\corewrappers.h>

#include <windows.ui.composition.h>
#include <windows.ui.composition.interop.h>
#include <windows.ui.composition.desktop.h>

#include <DispatcherQueue.h>
#include <Windows.Graphics.Effects.h>

#include <d3d11_2.h>
#include <d2d1_2.h>
#include <d2d1_2helper.h>
#include <wincodec.h>
#pragma comment(lib, "d3d11")
#pragma comment(lib, "d2d1")

#include "microsoft.ui.composition.effects_impl.h"

// Using
using namespace Microsoft::WRL;
using namespace Microsoft::WRL::Wrappers;
using namespace ABI::Windows::System;
using namespace ABI::Windows::Foundation;
using namespace ABI::Windows::UI::Composition;
using namespace ABI::Windows::UI::Composition::Desktop;

using namespace ABI::Windows::Foundation::Numerics;
using namespace ABI::Windows::UI;
using namespace Microsoft::UI::Composition::Effects;

using namespace ABI::Windows::Graphics::Effects;
using namespace ABI::Windows::Graphics::DirectX;

class CompositionHost
{
public:
	~CompositionHost();
	static CompositionHost* GetInstance();

	void Initialize(HWND hwnd);
	void ClearComposition();
	void CreateCompContent();
	void AddChildContent(Vector2 size, Vector3 offset, ComPtr<ICompositionBrush> brush);


private:
	CompositionHost();
	static CompositionHost* s_instance;
private:
	void EnsureDispatcherQueue();
	ComPtr<ICompositionBrush> CreateColorBrush();
	ComPtr<ICompositionBrush> CreateHostBackdropBrush();
	ComPtr<ICompositionBrush> CreateBackdropBrush();


	ComPtr<ICompositor> _compositor;// Step 2
	ComPtr<ICompositorDesktopInterop> _compositorDesktopInterop;
	ComPtr<ICompositionEffectSourceParameterFactory> _effectSourceFactory;
	ComPtr<IDesktopWindowTarget> _hwndTarget;
	ComPtr<IContainerVisual> _compRootVisual;

	ComPtr<IDispatcherQueue> _dispatcherQueue;
	ComPtr<IDispatcherQueueController> _dispatcherQueueController;
};

