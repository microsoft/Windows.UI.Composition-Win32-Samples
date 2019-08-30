#include "pch.h"

extern "C" IMAGE_DOS_HEADER __ImageBase;

using namespace concurrency;
using namespace ::winrt::impl;
using namespace winrt;
using namespace Windows::Foundation;
using namespace Windows::UI;
using namespace Windows::UI::Composition;
using namespace Windows::UI::Composition::Desktop;
using namespace Microsoft::Graphics::Canvas;
using namespace Microsoft::Graphics::Canvas::UI;
using namespace Microsoft::Graphics::Canvas::Effects;
using namespace Microsoft::Graphics::Canvas::UI::Composition;
using namespace Windows::Graphics::DirectX;
using namespace Windows::Graphics::Effects;
using namespace Windows::Storage;
using namespace Windows::Storage::Pickers;
using namespace Windows::Web::Syndication;
using namespace Windows::Storage::Streams;


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

DesktopWindowTarget CreateDesktopWindowTarget(Compositor const& compositor, HWND window)
{
	namespace abi = ABI::Windows::UI::Composition::Desktop;

	auto interop = compositor.as<abi::ICompositorDesktopInterop>();
	DesktopWindowTarget target{ nullptr };
	check_hresult(interop->CreateDesktopWindowTarget(window, true, reinterpret_cast<abi::IDesktopWindowTarget**>(put_abi(target))));
	return target;
}

template <typename T>
struct DesktopWindow
{
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
		}
		else if (T* that = GetThisFromHandle(window))
		{
			return that->MessageHandler(message, wparam, lparam);
		}

		return DefWindowProc(window, message, wparam, lparam);
	}

	LRESULT MessageHandler(UINT const message, WPARAM const wparam, LPARAM const lparam) noexcept
	{
		if (WM_DESTROY == message)
		{
			PostQuitMessage(0);
			return 0;
		}

		return DefWindowProc(m_window, message, wparam, lparam);
	}

protected:

	using base_type = DesktopWindow<T>;
	HWND m_window = nullptr;
};

struct Window : DesktopWindow<Window>
{
	Window() noexcept
	{
		WNDCLASS wc{};
		wc.hCursor = LoadCursor(nullptr, IDC_ARROW);
		wc.hInstance = reinterpret_cast<HINSTANCE>(&__ImageBase);
		wc.lpszClassName = L"Sample";
		wc.style = CS_HREDRAW | CS_VREDRAW;
		wc.lpfnWndProc = WndProc;
		RegisterClass(&wc);
		WINRT_ASSERT(!m_window);
		DWORD Flags1 = WS_EX_COMPOSITED | WS_EX_LAYERED | WS_EX_TOPMOST | WS_EX_TRANSPARENT;
		DWORD Flags2 = WS_OVERLAPPEDWINDOW | WS_VISIBLE;
		HWND hWnd;
		
		hWnd = CreateWindowEx(Flags1, wc.lpszClassName,
			L"Sample",
			Flags2,
			0, 0, 1920, 1200,
			nullptr, nullptr, wc.hInstance, this);
				
		ShowWindow(hWnd, SW_SHOWDEFAULT);
		WINRT_ASSERT(m_window);
	}

	LRESULT MessageHandler(UINT const message, WPARAM const wparam, LPARAM const lparam) noexcept
	{
		// TODO: handle messages here...

		return base_type::MessageHandler(message, wparam, lparam);
	}

	void PrepareVisuals()
	{
		Compositor compositor;
		_compositor = compositor;
		m_target = CreateDesktopWindowTarget(_compositor, m_window);
		auto root = _compositor.CreateSpriteVisual();
		root.RelativeSizeAdjustment({ 1.0f, 1.0f });
		//root.Brush(_compositor.CreateColorBrush({ 0xFF, 0xEF, 0xE4 , 0xB0 }));
		m_target.Root(root);
		auto visuals = root.Children();

		AddBlurVisual(visuals, 0.0f, 0.0f);
	}

	void AddVisual(VisualCollection const& visuals, float x, float y)
	{
		auto visual = _compositor.CreateSpriteVisual();

		static Color colors[] =
		{
			{ 0xDC, 0x5B, 0x9B, 0xD5 },
			{ 0xDC, 0xFF, 0xC0, 0x00 },
			{ 0xDC, 0xED, 0x7D, 0x31 },
			{ 0xDC, 0x70, 0xAD, 0x47 },
		};

		static unsigned last = 0;
		unsigned const next = ++last % _countof(colors);
		visual.Brush(_compositor.CreateColorBrush(colors[next]));
		visual.Size({ 200.0f, 200.0f });
		visual.Offset({ x, y, 0.0f, });

		visuals.InsertAtTop(visual);
	}

	void AddBackdropVisual(VisualCollection const& visuals, float x, float y)
	{
		auto visual = _compositor.CreateSpriteVisual();
		visual.Brush(_compositor.CreateBackdropBrush());
		
		visual.Size({ 200.0f, 200.0f });
		visual.Offset({ x, y, 0.0f, });

		visuals.InsertAtTop(visual);
	}


	void AddBlurVisual(VisualCollection const& visuals, float x, float y)
	{
		auto visual = _compositor.CreateSpriteVisual();
		AddBlurEffect(visual);
		visual.Size({ 1920.0f, 1200.0f });
		visual.Offset({ x, y, 0.0f, });

		visuals.InsertAtTop(visual);
	}

	void AddEffect(SpriteVisual visual) 
	{
		ColorSourceEffect _acrylicEffect;
		_acrylicEffect.Color(ColorHelper::FromArgb(255, 0, 209, 193));
		auto effectFactory = _compositor.CreateEffectFactory(_acrylicEffect);
		auto acrylicEffectBrush = effectFactory.CreateBrush();
		visual.Brush(acrylicEffectBrush);
	}

	CompositionEffectBrush CreateAcrylicEffectBrush(Compositor _compositor)
	{
		IGraphicsEffect acrylicEffect;
		// Compile the effect.
		auto effectFactory = _compositor.CreateEffectFactory(acrylicEffect);
		
		// Create Brush.
		auto acrylicEffectBrush = effectFactory.CreateBrush();

		// Set sources.
		auto destinationBrush = _compositor.CreateBackdropBrush();
		acrylicEffectBrush.SetSourceParameter(L"Backdrop", destinationBrush);
		acrylicEffectBrush.SetSourceParameter(L"Noise", GetNoiseSurfaceBrush());

		return acrylicEffectBrush;
	}

	IGraphicsEffect CreateAcrylicEffectGraph()
	{
		BlendEffect acrylicEffect;
		
		//Acrylic Effect Graph
		GaussianBlurEffect blurEffect;
		blurEffect.Source(CompositionEffectSourceParameter(L"Backdrop"));
		blurEffect.BorderMode(EffectBorderMode::Hard);
		blurEffect.BlurAmount(30);

		SaturationEffect saturationEffect;
		saturationEffect.Saturation(2);
		saturationEffect.Source(blurEffect);

		ColorSourceEffect colorSourceEffect1;
		colorSourceEffect1.Color(ColorHelper::FromArgb(26, 255, 255, 255));

		BlendEffect backDropEffect;
		backDropEffect.Mode(BlendEffectMode::Exclusion);
		backDropEffect.Background(saturationEffect);
		backDropEffect.Foreground(colorSourceEffect1);

		ColorSourceEffect colorSourceEffect2;
		colorSourceEffect2.Color(ColorHelper::FromArgb(153, 255, 255, 255));
		BorderEffect borderEffect;
		borderEffect.ExtendX(CanvasEdgeBehavior::Wrap);
		borderEffect.ExtendY(CanvasEdgeBehavior::Wrap);
		borderEffect.Source(CompositionEffectSourceParameter(L"Noise"));

		OpacityEffect opacityEffect;
		opacityEffect.Opacity(0.03);
		opacityEffect.Source(borderEffect);

		CompositeEffect compositeEffect;
		compositeEffect.Mode(CanvasComposite::SourceOver);
		//TODO fix this - compositeEffect.Sources= {backDropEffect, colorSourceEffect2};
		acrylicEffect.Mode(BlendEffectMode::Overlay);
		acrylicEffect.Background(compositeEffect);
		acrylicEffect.Foreground(opacityEffect);
		
		return acrylicEffect;
	}

	CompositionSurfaceBrush GetNoiseSurfaceBrush()
	{

		// Get graphics device.
		auto compositionGraphicsDevice = CanvasComposition::CreateCompositionGraphicsDevice(_compositor, _canvasDevice);
		// Create surface. 
		auto noiseDrawingSurface = compositionGraphicsDevice.CreateDrawingSurface(
			Windows::Foundation::Size(_rectWidth, _rectHeight),
			DirectXPixelFormat::B8G8R8A8UIntNormalized,
			DirectXAlphaMode::Premultiplied);

		// Draw to surface and create surface brush.
		auto noiseFilePath = L"Assets\\NoiseAsset_256X256.png";
		LoadSurface(noiseDrawingSurface, noiseFilePath);
		auto noiseSurfaceBrush = _compositor.CreateSurfaceBrush(noiseDrawingSurface);
		return noiseSurfaceBrush;
	}



	void LoadSurface(CompositionDrawingSurface surface, const wchar_t* path)
	{
		// Load from stream.
		IAsyncOperation<StorageFile> storageFile =  StorageFile::GetFileFromPathAsync(path);
		auto stream = storageFile.get().OpenAsync(FileAccessMode::Read);
		auto bitmap = CanvasBitmap::LoadAsync(_canvasDevice, stream.get());

		// Draw to surface.
		auto ds = CanvasComposition::CreateDrawingSession(surface);
		ds.Clear(Colors::Transparent());
		auto rect = Windows::Foundation::Rect(0, 0, _rectWidth, _rectHeight);
		ds.DrawImage(bitmap.get(), 0, 0, rect);
		
	}

	void AddBlurEffect(SpriteVisual visual)
	{GaussianBlurEffect _acrylicEffect;
		_acrylicEffect.Source(CompositionEffectSourceParameter(L"Backdrop"));
		_acrylicEffect.BorderMode(EffectBorderMode::Hard),
		_acrylicEffect.BlurAmount(30);

		auto effectFactory = _compositor.CreateEffectFactory(_acrylicEffect);
		auto acrylicEffectBrush = effectFactory.CreateBrush();
		// Set sources.
		auto   destinationBrush = _compositor.CreateBackdropBrush();
		acrylicEffectBrush.SetSourceParameter(L"Backdrop", destinationBrush);
		visual.Brush(acrylicEffectBrush);
	}
private:
	int _rectWidth = 400;
	int _rectHeight = 400;

	DesktopWindowTarget m_target { nullptr };
	Compositor _compositor {nullptr};
	CanvasDevice _canvasDevice;
};

int __stdcall wWinMain(HINSTANCE, HINSTANCE, LPWSTR, int )
{
	init_apartment(apartment_type::single_threaded);
	auto controller = CreateDispatcherQueueController();

	Window window;

	window.PrepareVisuals();
	MSG message;

	while (GetMessage(&message, nullptr, 0, 0))
	{
		DispatchMessage(&message);
	}
}
