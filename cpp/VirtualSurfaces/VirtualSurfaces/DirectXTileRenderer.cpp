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
#include "DirectXTileRenderer.h"

DirectXTileRenderer::DirectXTileRenderer()
{
}

DirectXTileRenderer::~DirectXTileRenderer()
{
}

//
//  FUNCTION: Initialize
//
//  PURPOSE: Initializes all the necessary devices and structures needed for a DirectX Surface rendering operation.
//
void DirectXTileRenderer::Initialize(Compositor compositor, int tileSize) {
	namespace abi = ABI::Windows::UI::Composition;

	com_ptr<ID2D1Factory1> const& factory = CreateFactory();
	com_ptr<ID3D11Device> const& device = CreateDevice();
	com_ptr<IDXGIDevice> const dxdevice = device.as<IDXGIDevice>();

	m_compositor = compositor;
	m_tileSize = tileSize;
	com_ptr<abi::ICompositorInterop> interopCompositor = m_compositor.as<abi::ICompositorInterop>();
	com_ptr<ID2D1Device> d2device;
	check_hresult(factory->CreateDevice(dxdevice.get(), d2device.put()));
	check_hresult(interopCompositor->CreateGraphicsDevice(d2device.get(), reinterpret_cast<abi::ICompositionGraphicsDevice**>(put_abi(m_graphicsDevice))));
	InitializeTextLayout();
}

CompositionSurfaceBrush DirectXTileRenderer::getSurfaceBrush()
{
	if (m_surfaceBrush == nullptr) {
		m_surfaceBrush = CreateVirtualDrawingSurfaceBrush();
	}
	return m_surfaceBrush;
}

//
//  FUNCTION: DrawTileRange
//
//  PURPOSE: This function iterates through a list of Tiles and draws them wihtin a single BeginDraw/EndDraw session for performance reasons. 
//	OPTIMIZATION: This can fail when the surface to be drawn is really large in one go, expecially when the surface is zoomed in by a larger factor. 
//
bool DirectXTileRenderer::DrawTileRange(Rect rect, std::list<Tile> tiles)
{
	POINT offset;
	RECT updateRect = RECT{ static_cast<LONG>(rect.X),  static_cast<LONG>(rect.Y),  static_cast<LONG>(rect.X + rect.Width - 5),  static_cast<LONG>(rect.Y + rect.Height - 5) };
	winrt::com_ptr<::ID2D1DeviceContext> m_d2dDeviceContext;
	winrt::com_ptr<::ID2D1SolidColorBrush> m_textBrush;
	std::list<Tile>::iterator it;

	// Begin our update of the surface pixels. Passing nullptr to this call will update the entire surface. We only update the rect area that needs to be rendered.
	if (CheckForDeviceRemoved(m_surfaceInterop->BeginDraw(&updateRect, __uuidof(ID2D1DeviceContext), (void **)m_d2dDeviceContext.put(), &offset))) {
		m_d2dDeviceContext->Clear(D2D1::ColorF(D2D1::ColorF::Red, 0.f));

		//get the offset difference that can be applied to every tile before drawing.
		Tile firstTile= tiles.front();
		POINT differenceOffset{ offset.x - firstTile.rect.X, offset.y - firstTile.rect.Y };

		//iterate through the tiles and do DrawRectangle and DrawText calls on those.
		for (it = tiles.begin(); it != tiles.end(); ++it) {
			DrawTile(m_d2dDeviceContext, m_textBrush, *it, differenceOffset);
		}

		m_surfaceInterop->EndDraw();
		return true;
	}
	return false;
}


//
//  FUNCTION:DrawTile
//
//  PURPOSE: Core D2D/DWrite calls for drawing a rectangle and text on top of it.
//  OPTIMIZATION: Pre-create the color brushes in the device instead of on every DrawTile call.
//
void DirectXTileRenderer::DrawTile(com_ptr<::ID2D1DeviceContext> d2dDeviceContext, com_ptr<::ID2D1SolidColorBrush> m_textBrush, Tile tile, POINT differenceOffset )
{
	// Create a solid color brush for the text. A more sophisticated application might want
	// to cache and reuse a brush across all text elements instead, taking care to recreate
	// it in the event of device removed.
	winrt::check_hresult(d2dDeviceContext->CreateSolidColorBrush(
		D2D1::ColorF(D2D1::ColorF::DimGray, 1.0f), m_textBrush.put()));

	//Generating colors to distinguish each tile.
	m_colorCounter = (int)(m_colorCounter + 8) % 192 + 8.0f;
	D2D1::ColorF randomColor(m_colorCounter / 256, 1.0f, 0.0f, 0.5f);

	float offsetUpdatedX = tile.rect.X + differenceOffset.x;
	float offsetUpdatedY = tile.rect.Y + differenceOffset.y;
	int borderMargin = 5;

	D2D1_RECT_F tileRectangle{ offsetUpdatedX ,  offsetUpdatedY, offsetUpdatedX+tile.rect.Width-borderMargin, offsetUpdatedY + tile.rect.Height-borderMargin };

	winrt::com_ptr<::ID2D1SolidColorBrush> tilebrush;
	//Draw the rectangle
	winrt::check_hresult(d2dDeviceContext->CreateSolidColorBrush(
		randomColor, tilebrush.put()));

	d2dDeviceContext->FillRectangle(tileRectangle, tilebrush.get());
	DrawText(tile.row, tile.column, tileRectangle, d2dDeviceContext, m_textBrush);
}

//
//  FUNCTION: CheckForDeviceRemoved
//
//  PURPOSE: We may detect device loss on BeginDraw calls. This helper handles this condition or other
//  errors.
//
bool DirectXTileRenderer::CheckForDeviceRemoved(HRESULT hr)
{
	if (SUCCEEDED(hr))
	{
		// Everything is fine -- go ahead and draw
		return true;
	}
	else if (hr == DXGI_ERROR_DEVICE_REMOVED)
	{
		// We can't draw at this time, but this failure is recoverable. Just skip drawing for
		// now. We will be asked to draw again once the Direct3D device is recreated
		return false;
	}
	// Any other error is unexpected and, therefore, fatal.
	winrt::check_hresult(hr);
	return true;
}

//
//  FUNCTION:Trim
//
//  PURPOSE: Helper function that calls the trim on the virtualSurface
//
void DirectXTileRenderer::Trim(Rect trimRect)
{
	RectInt32 trimRects[1];
	trimRects[0] = RectInt32{ (int)trimRect.X, (int)trimRect.Y, (int)trimRect.Width, (int)trimRect.Height };
	
	m_virtualSurfaceBrush.Trim(trimRects);
}


//
//  FUNCTION: DrawText
//
//  PURPOSE: DirectWrite calls to draw the text "x,y" in the tile
//
void DirectXTileRenderer::DrawText(int tileRow, int tileColumn, D2D1_RECT_F rect, winrt::com_ptr<::ID2D1DeviceContext> m_d2dDeviceContext,
	winrt::com_ptr<::ID2D1SolidColorBrush> m_textBrush)
{
	std::wstring text{ std::to_wstring(tileRow) + L"," + std::to_wstring(tileColumn)  };

	winrt::com_ptr<::IDWriteTextLayout> textLayout;
	winrt::check_hresult(
		m_dWriteFactory->CreateTextLayout(
			text.c_str(),
			(uint32_t)text.size(),
			m_textFormat.get(),
			60,
			30,
			textLayout.put()
		)
	);

	// Draw the line of text at the specified offset, which corresponds to the top-left
	// corner of our drawing surface. Notice we don't call BeginDraw on the D2D device
	// context; this has already been done for us by the composition API.
	// position the text in the center as much as possible.
	m_d2dDeviceContext->DrawTextLayout(D2D1::Point2F((float)rect.left + (m_tileSize / 2 - 30), (float)rect.top+(m_tileSize/2-30)), textLayout.get(),
		m_textBrush.get());

}

//
//  FUNCTION:InitializeTextLayout
//
//  PURPOSE: Creates the text layout
//
void DirectXTileRenderer::InitializeTextLayout()
{
	winrt::check_hresult(
		::DWriteCreateFactory(
			DWRITE_FACTORY_TYPE_SHARED,
			__uuidof(m_dWriteFactory),
			reinterpret_cast<::IUnknown**>(m_dWriteFactory.put())
		)
	);

	winrt::check_hresult(
		m_dWriteFactory->CreateTextFormat(
			L"Segoe UI",
			nullptr,
			DWRITE_FONT_WEIGHT_REGULAR,
			DWRITE_FONT_STYLE_NORMAL,
			DWRITE_FONT_STRETCH_NORMAL,
			30.f,
			L"en-US",
			m_textFormat.put()
		)
	);

}

//
//  FUNCTION:CreateFactory
//
//  PURPOSE: Utility function to create the D2DFactory 
//
com_ptr<ID2D1Factory1> DirectXTileRenderer::CreateFactory()
{
	D2D1_FACTORY_OPTIONS options{};
	com_ptr<ID2D1Factory1> factory;

	check_hresult(D2D1CreateFactory(
		D2D1_FACTORY_TYPE_SINGLE_THREADED,
		options,
		factory.put()));

	return factory;
}


//
//  FUNCTION:CreateDevice
//
//  PURPOSE: Utility function to create the D3D11 device
//
HRESULT DirectXTileRenderer::CreateDevice(D3D_DRIVER_TYPE const type, com_ptr<ID3D11Device>& device)
{
	WINRT_ASSERT(!device);

	return D3D11CreateDevice(
		nullptr,
		type,
		nullptr,
		D3D11_CREATE_DEVICE_BGRA_SUPPORT,
		nullptr, 0,
		D3D11_SDK_VERSION,
		device.put(),
		nullptr,
		nullptr);
}

com_ptr<ID3D11Device> DirectXTileRenderer::CreateDevice()
{
	com_ptr<ID3D11Device> device;
	HRESULT hr = CreateDevice(D3D_DRIVER_TYPE_HARDWARE, device);

	if (DXGI_ERROR_UNSUPPORTED == hr)
	{
		hr = CreateDevice(D3D_DRIVER_TYPE_WARP, device);
	}

	check_hresult(hr);
	return device;
}

//
//  FUNCTION: CreateVirtualDrawingSurface
//
//  PURPOSE: Creates a VirtualDrawingSurface into which the D2D contents will be drawn.
//
CompositionDrawingSurface DirectXTileRenderer::CreateVirtualDrawingSurface(SizeInt32 size)
{
	auto graphicsDevice2 = m_graphicsDevice.as<ICompositionGraphicsDevice2>();

	m_virtualSurfaceBrush = graphicsDevice2.CreateVirtualDrawingSurface(
		size,
		DirectXPixelFormat::B8G8R8A8UIntNormalized,
		DirectXAlphaMode::Premultiplied);

	return m_virtualSurfaceBrush;
}

//
//  FUNCTION: CreateVirtualDrawingSurfaceBrush
//
//  PURPOSE: Creates a VirtualDrawingSurface into which the D2D contents will be drawn. Returns a CompositionSurfaceBrush that can be applied to a Composition Visual.
//
CompositionSurfaceBrush DirectXTileRenderer::CreateVirtualDrawingSurfaceBrush()
{
	namespace abi = ABI::Windows::UI::Composition;

	SizeInt32 size;
	size.Width = m_tileSize * 10000;
	size.Height = m_tileSize * 10000;

	m_surfaceInterop = CreateVirtualDrawingSurface(size).as<abi::ICompositionDrawingSurfaceInterop>();

	ICompositionSurface surface = m_surfaceInterop.as<ICompositionSurface>();

	CompositionSurfaceBrush surfaceBrush = m_compositor.CreateSurfaceBrush(surface);
	surfaceBrush.Stretch(CompositionStretch::None);

	surfaceBrush.HorizontalAlignmentRatio(0);
	surfaceBrush.VerticalAlignmentRatio(0);
	//surfaceBrush.TransformMatrix = System::Numerics::Matrix3x2.CreateTranslation(20.0f, 20.0f);
	surfaceBrush.TransformMatrix(make_float3x2_translation(20.0f, 20.0f));

	return surfaceBrush;
}