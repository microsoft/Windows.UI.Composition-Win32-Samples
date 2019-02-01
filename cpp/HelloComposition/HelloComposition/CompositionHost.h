#pragma once
#include <winrt/Windows.UI.Composition.Desktop.h>
#include <windows.ui.composition.interop.h>
#include <DispatcherQueue.h>

using namespace winrt;
using namespace Windows::System;
using namespace Windows::UI;
using namespace Windows::UI::Composition;
using namespace Windows::UI::Composition::Desktop;
using namespace Windows::Foundation::Numerics;

class CompositionHost
{
public:
	~CompositionHost();
	static CompositionHost* GetInstance();

	void Initialize(HWND hwnd);
	void AddElement(float size, float x, float y);

private:
	CompositionHost();
	static CompositionHost* s_instance;

	void CreateDesktopWindowTarget(HWND window);
	void EnsureDispatcherQueue();
	void CreateCompositionRoot();
	
	Windows::UI::Composition::Compositor m_compositor{ nullptr };
	Windows::UI::Composition::Desktop::DesktopWindowTarget m_target{ nullptr };
	Windows::System::DispatcherQueueController m_dispatcherQueueController{ nullptr };
};

