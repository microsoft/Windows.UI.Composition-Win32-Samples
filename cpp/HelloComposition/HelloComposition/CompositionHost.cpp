#include "stdafx.h"
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

void CompositionHost::EnsureDispatcherQueue()
{
	if (_dispatcherQueue == nullptr)
	{
		ComPtr<IDispatcherQueueStatics> cpDispatcherQueueStatics;
		GetActivationFactory(
			HStringReference(RuntimeClass_Windows_System_DispatcherQueue).Get(),
			cpDispatcherQueueStatics.GetAddressOf());

		DispatcherQueueOptions options;
		options.dwSize = sizeof(DispatcherQueueOptions);
		options.threadType = DQTYPE_THREAD_CURRENT;
		options.apartmentType = DQTAT_COM_ASTA;

		CreateDispatcherQueueController(options, _dispatcherQueueController.GetAddressOf());
		_dispatcherQueueController->get_DispatcherQueue(_dispatcherQueue.GetAddressOf());
	}
}

void CompositionHost::Initialize(HWND hwnd)
{
	//
	// Set up Windows.UI.Composition Device, Target and root Visual.
	//
	EnsureDispatcherQueue();
	//
	// First setup composition device
	//
	ComPtr<IActivationFactory> factory;
	CoInitialize(nullptr);
	GetActivationFactory(
		HStringReference(RuntimeClass_Windows_UI_Composition_Compositor).Get(),
		&factory);

	ComPtr<IInspectable> cpCompositor;
	HRESULT hr = factory->ActivateInstance(&cpCompositor);
	cpCompositor.As(&_compositor);
	_compositor.As(&_compositorDesktopInterop);

	HStringReference effectSourceClass(RuntimeClass_Windows_UI_Composition_CompositionEffectSourceParameter);
	GetActivationFactory(effectSourceClass.Get(), &_effectSourceFactory);


	//DO(_compositorDesktopInterop->CreateDesktopWindowTarget(hwnd, false, &_hwndTarget));
	_compositorDesktopInterop->CreateDesktopWindowTarget(hwnd, false, &_hwndTarget);
}

void CompositionHost::CreateCompContent() {

	//DO(_compositor->CreateContainerVisual(&_compRootVisual));
	_compositor->CreateContainerVisual(&_compRootVisual);
	ComPtr<IVisual> compRootVisualInstance;
	//DO(_compRootVisual.As(&compRootVisualInstance));
	_compRootVisual.As(&compRootVisualInstance);

	ComPtr<ICompositionTarget> _compositionTarget;
	//DO(_hwndTarget.As(&_compositionTarget));
	//DO(_compositionTarget->put_Root(compRootVisualInstance.Get()));
	_hwndTarget.As(&_compositionTarget);
	_compositionTarget->put_Root(compRootVisualInstance.Get());

	Vector2 size; size.X = 200; size.Y = 200;
	Vector3 offset;	offset.X = 300;  offset.Y = 40; offset.Z = 0;

	AddChildContent(size, offset, CreateColorBrush());
}

void CompositionHost::AddChildContent(Vector2 size, Vector3 offset, ComPtr<ICompositionBrush> brush)
{
	ComPtr<ISpriteVisual> child;
	_compositor->CreateSpriteVisual(&child);

	child->put_Brush(brush.Get());

	ComPtr<IVisual> child1;
	child.As(&child1);

	child1->put_Size(size);
	child1->put_Offset(offset);

	ComPtr<IVisualCollection> children;
	_compRootVisual->get_Children(&children);
	children->InsertAtBottom(child1.Get());
}


ComPtr<ICompositionBrush> CompositionHost::CreateColorBrush()
{
	auto colorSourceEffect = Make<ColorSourceEffect>();

	Color color;
	color.A = 0xFF; // 255
	color.R = 0x00; // 0
	color.G = 0x00; // 0
	color.B = 0xCC; // 204

	colorSourceEffect->put_Name(HStringReference(L"Blue").Get());
	colorSourceEffect->put_Color(color);

	//Create Brush from Effect 
	ComPtr<ICompositionEffectFactory> _effectFactory;
	ComPtr<ICompositionEffectBrush> _colorBrush;

	_compositor->CreateEffectFactory(colorSourceEffect.Get(), &_effectFactory);
	_effectFactory->CreateBrush(&_colorBrush);
	ComPtr<ICompositionBrush> brush;
	_colorBrush.As(&brush);
	return brush;
}

ComPtr<ICompositionBrush> CompositionHost::CreateHostBackdropBrush()
{
	ComPtr<ICompositionBackdropBrush> _hostBackDropBrush;
	ComPtr<ICompositor3> _compositor3;
	_compositor.As(&_compositor3);
	_compositor3->CreateHostBackdropBrush(&_hostBackDropBrush);
	ComPtr<ICompositionBrush> brush;
	_hostBackDropBrush.As(&brush);
	return brush;
}

ComPtr<ICompositionBrush> CompositionHost::CreateBackdropBrush()
{
	ComPtr<ICompositionBackdropBrush> _backDropBrush;
	ComPtr<ICompositor2> _compositor2;
	_compositor.As(&_compositor2);
	_compositor2->CreateBackdropBrush(&_backDropBrush);
	ComPtr<ICompositionBrush> brush;
	_backDropBrush.As(&brush);
	return brush;
}


void CompositionHost::ClearComposition()
{
	_compRootVisual = nullptr;

	ComPtr<IClosable> closable;
	_compositor.As(&closable);
	closable->Close();
	_compositor = nullptr;
}