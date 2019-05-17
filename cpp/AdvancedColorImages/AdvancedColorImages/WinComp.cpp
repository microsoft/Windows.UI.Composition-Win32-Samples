//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
// THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//*********************************************************

#include "stdafx.h"
#include "WinComp.h"



//
//  FUNCTION: EnsureDispatcherQueue
//
//  PURPOSE: It is necessary for a DisptacherQueue to be available on the same thread in which
//	the Compositor runs on. Events for the Compositor are fired using this DispatcherQueue
//
DispatcherQueueController WinComp::EnsureDispatcherQueue()
{
	namespace abi = ABI::Windows::System;

	DispatcherQueueOptions options
	{
		sizeof(DispatcherQueueOptions),
		DQTYPE_THREAD_CURRENT,
		DQTAT_COM_ASTA
	};

	DispatcherQueueController controller{ nullptr };
	check_hresult(CreateDispatcherQueueController(options, reinterpret_cast<abi::IDispatcherQueueController * *>(put_abi(controller))));

	return controller;
}

//
//  FUNCTION:Initialize
//
//  PURPOSE: Initializes all the key member variables, including the Compositor. This sample hosts directX content inside a visual 
//
void WinComp::Initialize(HWND hwnd)
{
	namespace abi = ABI::Windows::UI::Composition;

	m_window = hwnd;
	Compositor compositor;
	m_compositor = compositor;
	m_dxRenderer = new DirectXTileRenderer();
	m_dxRenderer->Initialize(m_compositor, TileDrawingManager::TILESIZE, TileDrawingManager::MAXSURFACESIZE);
	m_TileDrawingManager.SetRenderer(m_dxRenderer);

}

void WinComp::TryRedirectForManipulation(PointerPoint pp)
{
	//Redirecting the Pointer input for manipulation by the InteractionTracker
	m_interactionSource.TryRedirectForManipulation(pp);
}

void WinComp::TryUpdatePositionBy(float3 const& amount)
{
	m_tracker.TryUpdatePositionBy(amount);
}
//
//  FUNCTION: PrepareVisuals
//
//  PURPOSE: Creates the Visual tree and hooks it up to the desktopWindowTarget 
//
void WinComp::PrepareVisuals()
{
	namespace abi = ABI::Windows::UI::Composition::Desktop;

	//Creates a DesktoWindowTarget that can host Windows.UI.Composition Visual tree inside an HWnd
	auto interop = m_compositor.as<abi::ICompositorDesktopInterop>();
	check_hresult(interop->CreateDesktopWindowTarget(m_window, true, reinterpret_cast<abi::IDesktopWindowTarget * *>(put_abi(m_target))));

	auto root = m_compositor.CreateSpriteVisual();
	//Create a background with Gray color brush.
	root.Brush(m_compositor.CreateColorBrush({ 0xFF, 0xFE, 0xFE , 0xFE }));

	root.Size(GetWindowSize());
	m_target.Root(root);

	auto visuals = root.Children();
	AddD2DVisual(visuals, 0.0f, 0.0f);
}

//
//  FUNCTION: AddD2DVisual
//
//  PURPOSE: Creates a SurfaceBrush to host Direct2D content in this visual.
//
void WinComp::AddD2DVisual(VisualCollection const& visuals, float x, float y)
{
	auto compositor = visuals.Compositor();
	m_contentVisual = compositor.CreateSpriteVisual();
	m_contentVisual.Brush(m_TileDrawingManager.GetRenderer()->getSurfaceBrush());

	m_contentVisual.Size(GetWindowSize());
	m_contentVisual.Offset({ x, y, 0.0f, });

	visuals.InsertAtTop(m_contentVisual);
}

//
//  FUNCTION: UpdateViewPort
//
//  PURPOSE: This is called when the Viewport size has changed, because of events like maximize, resize window etc.
//
void WinComp::UpdateViewPort(boolean changeContentVisual)
{
	//return if the m_window hasn't been set.
	if (m_window != NULL) {
		RECT windowRect;
		::GetWindowRect(m_window, &windowRect);
		Size windowSize;
		windowSize.Height = (windowRect.bottom - windowRect.top) / m_lastTrackerScale;
		windowSize.Width = (windowRect.right - windowRect.left) / m_lastTrackerScale;

		if (changeContentVisual) {
			m_contentVisual.Size(windowSize);
		}
		m_TileDrawingManager.UpdateViewportSize(windowSize);
		m_TileDrawingManager.UpdateVisibleRegion(m_lastTrackerPosition / m_lastTrackerScale);
	}
}

//
//  FUNCTION: GetWindowSize
//
//  PURPOSE: Helper function for get the size of the HWnd.
//
Size WinComp::GetWindowSize()
{
	RECT windowRect;
	::GetWindowRect(m_window, &windowRect);
	return Size({ (float)(windowRect.right - windowRect.left), (float)(windowRect.bottom - windowRect.top) });
}

//
//  FUNCTION: StartAnimation
//
//  PURPOSE: Use CompositionPropertySet and Expression Animations to manipulate the Virtual Surface.
//
void WinComp::StartAnimation(CompositionSurfaceBrush brush)
{
	m_animatingPropset = m_compositor.CreatePropertySet();
	m_animatingPropset.InsertScalar(L"xcoord", 1.0f);
	m_animatingPropset.StartAnimation(L"xcoord", m_moveSurfaceExpressionAnimation);

	m_animatingPropset.InsertScalar(L"ycoord", 1.0f);
	m_animatingPropset.StartAnimation(L"ycoord", m_moveSurfaceUpDownExpressionAnimation);

	m_animatingPropset.InsertScalar(L"scale", 1.0f);
	m_animatingPropset.StartAnimation(L"scale", m_scaleSurfaceUpDownExpressionAnimation);

	m_animateMatrix = m_compositor.CreateExpressionAnimation(L"Matrix3x2(props.scale, 0.0, 0.0, props.scale, props.xcoord, props.ycoord)");
	m_animateMatrix.SetReferenceParameter(L"props", m_animatingPropset);

	brush.StartAnimation(L"TransformMatrix", m_animateMatrix);
}

//
//  FUNCTION: ConfigureInteraction
//
//  PURPOSE: Configure InteractionTracker on this visual, to enable touch, PTP and mousewheel based interactions.
//
void WinComp::ConfigureInteraction()
{
	m_interactionSource = VisualInteractionSource::Create(m_contentVisual);
	m_interactionSource.PositionXSourceMode(InteractionSourceMode::EnabledWithInertia);
	m_interactionSource.PositionYSourceMode(InteractionSourceMode::EnabledWithInertia);
	m_interactionSource.ScaleSourceMode(InteractionSourceMode::EnabledWithInertia);
	m_interactionSource.ManipulationRedirectionMode(VisualInteractionSourceRedirectionMode::CapableTouchpadAndPointerWheel);

	m_tracker = InteractionTracker::CreateWithOwner(m_compositor, *this);
	m_tracker.InteractionSources().Add(m_interactionSource);

	m_moveSurfaceExpressionAnimation = m_compositor.CreateExpressionAnimation(L"-tracker.Position.X");
	m_moveSurfaceExpressionAnimation.SetReferenceParameter(L"tracker", m_tracker);

	m_moveSurfaceUpDownExpressionAnimation = m_compositor.CreateExpressionAnimation(L"-tracker.Position.Y");
	m_moveSurfaceUpDownExpressionAnimation.SetReferenceParameter(L"tracker", m_tracker);

	m_scaleSurfaceUpDownExpressionAnimation = m_compositor.CreateExpressionAnimation(L"tracker.Scale");
	m_scaleSurfaceUpDownExpressionAnimation.SetReferenceParameter(L"tracker", m_tracker);

	m_tracker.MinPosition(float3(0, 0, 0));
	m_tracker.MaxPosition(float3(TileDrawingManager::MAXSURFACESIZE, TileDrawingManager::MAXSURFACESIZE, 0));

	m_tracker.MinScale(0.2f);
	m_tracker.MaxScale(3.0f);

	StartAnimation(m_TileDrawingManager.GetRenderer()->getSurfaceBrush());
}

// interactionTrackerowner methods.

void WinComp::CustomAnimationStateEntered(InteractionTracker sender, InteractionTrackerCustomAnimationStateEnteredArgs args)
{
}

void WinComp::IdleStateEntered(InteractionTracker sender, InteractionTrackerIdleStateEnteredArgs args)
{
	if (m_zooming)
	{
		//dont update the content visual, because the window size hasnt changed.
		UpdateViewPort(false);
	}
	m_zooming = false;
}

void WinComp::InertiaStateEntered(InteractionTracker sender, InteractionTrackerInertiaStateEnteredArgs args)
{
}

void WinComp::InteractingStateEntered(InteractionTracker sender, InteractionTrackerInteractingStateEnteredArgs args)
{

}

void WinComp::RequestIgnored(InteractionTracker sender, InteractionTrackerRequestIgnoredArgs args)
{
}

void WinComp::ValuesChanged(InteractionTracker sender, InteractionTrackerValuesChangedArgs args)
{
	if (m_lastTrackerScale == args.Scale())
	{
		m_TileDrawingManager.UpdateVisibleRegion(sender.Position() / m_lastTrackerScale);
	}
	else
	{
		// Don't run tilemanager during a zoom
		m_zooming = true;
	}

	m_lastTrackerScale = args.Scale();
	m_lastTrackerPosition = sender.Position();
}
// Based on image and display parameters, choose the best rendering options.
void WinComp::UpdateDefaultRenderOptions()
{
	if (!m_isImageValid)
	{
		// Render options are only meaningful if an image is already loaded.
		return;
	}
	//Todo provide knobs for adjusting tonemapping, brightness adjustment etc.

	UpdateRenderOptions();
}

// Common method for updating options on the renderer.
void WinComp::UpdateRenderOptions()
{
	m_dxRenderer->SetRenderOptions(
		RenderEffectKind::None,
		static_cast<float>(3),
		m_dispInfo,
		GetWindowSize()
	);

}
IAsyncAction WinComp::LoadDefaultImage()
{
	Uri uri(L"https://mediaplatstorage1.blob.core.windows.net/windows-universal-samples-media/image-scrgb-icc.jxr");

	/*create_task(StorageFile::CreateStreamedFileFromUriAsync(L"image-scRGB-ICC.jxr", uri, nullptr)).then([=](StorageFile const& imageFile)
		{
			LoadImage(imageFile);
		});
	*/

	StorageFile imageFile{ co_await StorageFile::CreateStreamedFileFromUriAsync(L"image-scRGB-ICC.jxr", uri, nullptr) };
	//Windows::Storage::StorageFolder storageFolder{ Windows::Storage::ApplicationData::Current().LocalFolder() };
	//StorageFile imageFile{ co_await storageFolder.GetFileAsync(L"hdr-image.jpg") };

	co_await LoadImageFromFile(imageFile);
	//processOp.get();

}


IAsyncAction WinComp::OpenFilePicker(HWND hwnd)
{
	FileOpenPicker picker;
	picker.as<IInitializeWithWindow>()->Initialize(hwnd);
	picker.SuggestedStartLocation(PickerLocationId::Desktop);
	picker.FileTypeFilter().Append(L".jxr");
	picker.FileTypeFilter().Append(L".jpg");
	picker.FileTypeFilter().Append(L".png");
	picker.FileTypeFilter().Append(L".tif");

	StorageFile imageFile{ co_await picker.PickSingleFileAsync() };
	co_await LoadImageFromFile(imageFile);
	//processOp.get();


}

void WinComp::LoadImageFromFileName(LPCWSTR szFileName)
{

	ImageInfo info{ m_dxRenderer->LoadImageFromWic(szFileName) };
	m_dxRenderer->CreateImageDependentResources();
	m_dxRenderer->FitImageToWindow(GetWindowSize());
	// Image loading is done at this point.
	m_isImageValid = true;
	UpdateDefaultRenderOptions();
}

IAsyncOperation<int> WinComp::LoadImageFromFile(StorageFile  imageFile)
{

	IRandomAccessStream ras{ co_await imageFile.OpenAsync(Windows::Storage::FileAccessMode::Read) };

	com_ptr<IStream> iStream{ nullptr };
	check_hresult(CreateStreamOverRandomAccessStream(winrt::get_unknown(ras), __uuidof(iStream), iStream.put_void()));
	ImageInfo info{ m_dxRenderer->LoadImageFromWic(iStream.get()) };

	// Image loading is done at this point.
	m_isImageValid = true;
	UpdateDefaultRenderOptions();

	co_return 1;

}
