#pragma once
#include <winrt/Windows.UI.Composition.Desktop.h>
#include <windows.ui.composition.interop.h>
#include <DispatcherQueue.h>

class CompositionHost
{
public:
	~CompositionHost();
	static CompositionHost* GetInstance();

	void Initialize(HWND hwnd);
	void AddElement(float size, float x, float y);

private:
	CompositionHost();

	void CreateDesktopWindowTarget(HWND window);
	void EnsureDispatcherQueue();
	void CreateCompositionRoot();
	
	winrt::Windows::UI::Composition::Compositor m_compositor{ nullptr };
	winrt::Windows::UI::Composition::Desktop::DesktopWindowTarget m_target{ nullptr };
	winrt::Windows::System::DispatcherQueueController m_dispatcherQueueController{ nullptr };
};

