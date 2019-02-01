#include "pch.h"
#include "CompositionHost.h"

CompositionHost* CompositionHost::s_instance;

CompositionHost::CompositionHost()
{
}

CompositionHost* CompositionHost::GetInstance()
{
	if (s_instance == NULL)
		s_instance = new CompositionHost();
	return s_instance;
}

CompositionHost::~CompositionHost()
{
	delete s_instance;
}

void CompositionHost::Initialize(HWND hwnd)
{
	EnsureDispatcherQueue();
	if (m_dispatcherQueueController) m_compositor = Compositor();

	if (m_compositor)
	{
		CreateDesktopWindowTarget(hwnd);
		CreateCompositionRoot();
	}
}

void CompositionHost::EnsureDispatcherQueue()
{
	namespace abi = ABI::Windows::System;
	
	if (m_dispatcherQueueController == nullptr)
	{
		DispatcherQueueOptions options;
		options.dwSize = sizeof(DispatcherQueueOptions);
		options.threadType = DQTYPE_THREAD_CURRENT;
		options.apartmentType = DQTAT_COM_ASTA;

		Windows::System::DispatcherQueueController controller{ nullptr };
		check_hresult(CreateDispatcherQueueController(options, reinterpret_cast<abi::IDispatcherQueueController**>(put_abi(controller))));
		m_dispatcherQueueController = controller;
	}
}

void CompositionHost::CreateDesktopWindowTarget(HWND window)
{
	namespace abi = ABI::Windows::UI::Composition::Desktop;

	auto interop = m_compositor.as<abi::ICompositorDesktopInterop>();
	DesktopWindowTarget target{ nullptr };
	check_hresult(interop->CreateDesktopWindowTarget(window, false, reinterpret_cast<abi::IDesktopWindowTarget**>(put_abi(target))));
	m_target = target;
}

void CompositionHost::CreateCompositionRoot()
{
	auto root = m_compositor.CreateContainerVisual();
	root.RelativeSizeAdjustment({ 1.0f, 1.0f });
	root.Offset({ 24, 24, 0 });
	m_target.Root(root);
}

void CompositionHost::AddElement(float size, float x, float y)
{
	if (m_target.Root())
	{
		auto visuals = m_target.Root().as<ContainerVisual>().Children();

		auto compositor = visuals.Compositor();
		auto visual = compositor.CreateSpriteVisual();

		visual.Brush(compositor.CreateColorBrush({ 0xDC, 0x5B, 0x9B, 0xD5 }));
		visual.Size({ size, size });
		visual.Offset({ x, y, 0.0f, });

		visuals.InsertAtTop(visual);
	}
}