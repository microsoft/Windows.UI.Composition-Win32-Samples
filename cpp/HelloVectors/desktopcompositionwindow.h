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

#pragma once
#include "pch.h"
#include <functional>
extern "C" IMAGE_DOS_HEADER __ImageBase;

using namespace winrt;
using namespace winrt;
using namespace Windows::UI;
using namespace Windows::UI::Composition;
using namespace Windows::UI::Composition::Desktop;

auto CreateDispatcherQueueController()
{
	namespace abi = ABI::Windows::System;

	DispatcherQueueOptions options
	{
		sizeof(DispatcherQueueOptions),
		DQTYPE_THREAD_CURRENT,
		DQTAT_COM_STA
	};

	Windows::System::DispatcherQueueController controller{ nullptr };
	check_hresult(CreateDispatcherQueueController(options, reinterpret_cast<abi::IDispatcherQueueController**>(put_abi(controller))));
	return controller;
}

template <typename T>
struct DesktopWindow
{
	DesktopWindow() {}

	static T* GetThisFromHandle(HWND const window) noexcept
	{
		return reinterpret_cast<T *>(GetWindowLongPtr(window, GWLP_USERDATA));
	}

	static LRESULT __stdcall WndProc(HWND const window, UINT const message, WPARAM const wparam, LPARAM const lparam) noexcept
	{
		WINRT_ASSERT(window);

		if (WM_NCCREATE == message)
		{
			auto cs = reinterpret_cast<CREATESTRUCT *>(lparam);
			T* that = static_cast<T*>(cs->lpCreateParams);
			WINRT_ASSERT(that);
			WINRT_ASSERT(!that->m_window);
			that->m_window = window;
			SetWindowLongPtr(window, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(that));
			EnableNonClientDpiScaling(window);
			m_currentDpi = GetDpiForWindow(window);
		}
		else if (T* that = GetThisFromHandle(window))
		{
			return that->MessageHandler(message, wparam, lparam);
		}

		return DefWindowProc(window, message, wparam, lparam);
	}

	// DPI Change handler. On WM_DPICHANGE, resize the window.
	static LRESULT HandleDpiChange(HWND hWnd, WPARAM wParam, LPARAM lParam)
	{
		//HWND hWndStatic = GetWindow(hWnd, GW_CHILD);
		if (hWnd != nullptr)
		{
			UINT uDpi = HIWORD(wParam);

			// Resize the window.
			auto lprcNewScale = reinterpret_cast<RECT *>(lParam);

			SetWindowPos(hWnd, nullptr, lprcNewScale->left, lprcNewScale->top,
				lprcNewScale->right - lprcNewScale->left,
				lprcNewScale->bottom - lprcNewScale->top,
				SWP_NOZORDER | SWP_NOACTIVATE);

			if (T *that = GetThisFromHandle(hWnd))
			{
				that->NewScale(uDpi);
			}
		}
		return 0;
	}

	LRESULT MessageHandler(UINT const message, WPARAM const wparam, LPARAM const lparam) noexcept
	{
		switch (message) {
		    case WM_DPICHANGED:
		    {
			    return HandleDpiChange(m_window, wparam, lparam);
		    }
		    
		    case WM_DESTROY: 
		    {
		    	PostQuitMessage(0);
		    	return 0;
		    }
		    
		    case WM_SIZE: 
			{
		    	UINT width = LOWORD(lparam);
		    	UINT height = HIWORD(lparam);
		    
		    	mCurrentWidth = width;
		    	mCurrentHeight = height;
		    	if (T *that = GetThisFromHandle(m_window)) 
				{
		    		that->DoResize(width, height);
		    	}
		    }
		}
		return DefWindowProc(m_window, message, wparam, lparam);
	}

	void NewScale(UINT dpi) {}

	void DoResize(UINT width, UINT height) {}

protected:

	using base_type = DesktopWindow<T>;
	HWND m_window = nullptr;
	inline static UINT m_currentDpi = 0;
	int mCurrentWidth = 0;
	int mCurrentHeight = 0;
};

// Specialization of DesktopWindow that binds a composition tree to the HWND.
struct CompositionWindow : DesktopWindow<CompositionWindow>
{
	CompositionWindow(std::function<void(const Windows::UI::Composition::Compositor &, const Windows::UI::Composition::Visual &)> func) noexcept : CompositionWindow()
	{
		PrepareVisuals(m_compositor);
		func(m_compositor, m_root);
		NewScale(m_currentDpi);
	}

	CompositionWindow() noexcept
	{
		WNDCLASS wc{};
		wc.hCursor = LoadCursor(nullptr, IDC_ARROW);
		wc.hInstance = reinterpret_cast<HINSTANCE>(&__ImageBase);
		wc.lpszClassName = L"XAML island in Win32";
		wc.style = CS_HREDRAW | CS_VREDRAW;
		wc.lpfnWndProc = WndProc;
		RegisterClass(&wc);
		WINRT_ASSERT(!m_window);

		WINRT_VERIFY(CreateWindow(wc.lpszClassName,
			L"Vectors in Win32",
			WS_OVERLAPPEDWINDOW | WS_VISIBLE,
			CW_USEDEFAULT, CW_USEDEFAULT, 850, 874,
			nullptr, nullptr, wc.hInstance, this));

		WINRT_ASSERT(m_window);
	}

	~CompositionWindow()
	{
	}

	LRESULT MessageHandler(UINT const message, WPARAM const wparam, LPARAM const lparam) noexcept
	{
		// Handle messages here...
		return base_type::MessageHandler(message, wparam, lparam);
	}

	void NewScale(UINT dpi)
	{
		auto scaleFactor = (float)dpi / 100;

		if (m_root != nullptr && scaleFactor > 0)
		{
			m_root.Scale({ scaleFactor, scaleFactor, 1.0 });
		}
	}

	void DoResize(UINT width, UINT height) {
		m_currentWidth = width;
		m_currentHeight = height;
	}

	DesktopWindowTarget CreateDesktopWindowTarget(Compositor const& compositor, HWND window)
	{
		namespace abi = ABI::Windows::UI::Composition::Desktop;

		auto interop = compositor.as<abi::ICompositorDesktopInterop>();
		DesktopWindowTarget target{ nullptr };
		check_hresult(interop->CreateDesktopWindowTarget(window, true, reinterpret_cast<abi::IDesktopWindowTarget**>(put_abi(target))));
		return target;
	}

	void PrepareVisuals(Compositor const& compositor)
	{
		m_target = CreateDesktopWindowTarget(compositor, m_window);
		m_root = compositor.CreateSpriteVisual();
		m_root.RelativeSizeAdjustment({ 1.05f, 1.05f });
		m_root.Brush(compositor.CreateColorBrush({ 0xFF, 0xFF, 0xFF , 0xFF }));
		m_target.Root(m_root);
	}

private:
	UINT m_currentWidth = 600;
	UINT m_currentHeight = 600;
	HWND m_interopWindowHandle = nullptr;
	Compositor m_compositor;
	DesktopWindowTarget m_target{ nullptr };
	SpriteVisual m_root{ nullptr };
};