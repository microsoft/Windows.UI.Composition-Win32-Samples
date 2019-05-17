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

static const float sc_MaxZoom = 1.0f; // Restrict max zoom to 1:1 scale.
static const unsigned int sc_MaxBytesPerPixel = 16; // Covers all supported image formats.
static const float sc_nominalRefWhite = 80.0f; // Nominal white nits for sRGB and scRGB.


// 400 bins with gamma of 10 lets us measure luminance to within 10% error for any
// luminance above ~1.5 nits, up to 1 million nits.
static const unsigned int sc_histNumBins = 400;
static const float        sc_histGamma = 0.1f;
static const unsigned int sc_histMaxNits = 1000000;

//
//  FUNCTION: Initialize
//
//  PURPOSE: Initializes all the necessary devices and structures needed for a DirectX Surface rendering operation.
//
void DirectXTileRenderer::Initialize(Compositor const& compositor, int tileSize, int surfaceSize) {
	namespace abi = ABI::Windows::UI::Composition;
	
	m_compositor = compositor;
	m_tileSize = tileSize;
	m_surfaceSize = surfaceSize;
	
	InitializeTextFormat();
	CreateDeviceIndependentResources();
	m_surfaceBrush = CreateVirtualDrawingSurfaceBrush();
}

CompositionSurfaceBrush DirectXTileRenderer::getSurfaceBrush()
{
	return m_surfaceBrush;
}


//
//  FUNCTION: DrawTile
//
//  PURPOSE: This function iterates through a list of Tiles and draws them wihtin a single BeginDraw/EndDraw session for performance reasons. 
//	OPTIMIZATION: This can fail when the surface to be drawn is really large in one go, expecially when the surface is zoomed in by a larger factor. 
//
bool DirectXTileRenderer::DrawTile(Rect rect)
{
	//making sure the update rect doesnt go past the maximum size of the surface.
	RECT updateRect = { static_cast<LONG>(rect.X), static_cast<LONG>(rect.Y), static_cast<LONG>(min((rect.X + rect.Width),m_surfaceSize)), static_cast<LONG>(min((rect.Y + rect.Height),m_surfaceSize)) };
	SIZE updateSize = { updateRect.right - updateRect.left, updateRect.bottom - updateRect.top };

	//Cannot update a surface larger than the max texture size of the hardware. 2048X2048 is the lowest max texture size for relevant hardware.
	int MAXTEXTURESIZE = D3D_FL9_1_REQ_TEXTURE2D_U_OR_V_DIMENSION;
	//3 is the buffer here.
	SIZE constrainedUpdateSize = { min(updateSize.cx, MAXTEXTURESIZE - 3), min(updateSize.cy, MAXTEXTURESIZE - 3) };

	float savedColorCounter = m_colorCounter;
	//Breaking the BeginDraw/EndDraw calls to update rects that dont exceed the max texture size.
	for (LONG y = updateRect.top; y < updateRect.bottom; y += constrainedUpdateSize.cy)
	{
		for (LONG x = updateRect.left; x < updateRect.right; x += constrainedUpdateSize.cx)
		{
			m_colorCounter = savedColorCounter;

			POINT offset{};
			RECT constrainedUpdateRect = RECT{ x,  y,  min(x + constrainedUpdateSize.cx, updateRect.right), min(y + constrainedUpdateSize.cy, updateRect.bottom) };
			com_ptr<ID2D1DeviceContext> d2dDeviceContext;
			com_ptr<ID2D1SolidColorBrush> tileBrush;

			// Begin our update of the surface pixels. Passing nullptr to this call will update the entire surface. We only update the rect area that needs to be rendered.
			if (!CheckForDeviceRemoved(m_surfaceInterop->BeginDraw(&constrainedUpdateRect, __uuidof(ID2D1DeviceContext), (void**)d2dDeviceContext.put(), &offset)))
			{
				return false;
			}

			d2dDeviceContext->Clear(D2D1::ColorF(D2D1::ColorF::Red, 0.f));

			//Create a solid color brush for the tiles and which will be set to a different color before rendering.
			check_hresult(d2dDeviceContext->CreateSolidColorBrush(D2D1::ColorF(D2D1::ColorF::Green, 1.0f), tileBrush.put()));

			// Set a transform to draw into this section of the virtual surface using the input coordate space
			d2dDeviceContext->SetTransform(D2D1::Matrix3x2F::Translation((FLOAT)(offset.x - x), (FLOAT)(offset.y - y)));

			D2D1_RECT_F d2dRect = { constrainedUpdateRect.left, constrainedUpdateRect.top, constrainedUpdateRect.right, constrainedUpdateRect.bottom };

			d2dDeviceContext->PushAxisAlignedClip(d2dRect, D2D1_ANTIALIAS_MODE_ALIASED);
			d2dDeviceContext->DrawImage(m_finalOutput.get());
			d2dDeviceContext->PopAxisAlignedClip();

			d2dDeviceContext->DrawRectangle(d2dRect, tileBrush.get(), 3.0f);

			m_surfaceInterop->EndDraw();
		}
	}

	return true;
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
	else if (hr == DXGI_ERROR_DEVICE_REMOVED || hr == DXGI_ERROR_DEVICE_RESET)
	{
		// We can't draw at this time, but this failure is recoverable. Just skip drawing for
		// now. We will be asked to draw again once the Direct3D device is recreated
		return false;
	}
	// Any other error is unexpected and, therefore, fatal.
	check_hresult(hr);
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
	m_virtualSurface.Trim(trimRects);
}

//
//  FUNCTION:InitializeTextFormat
//
//  PURPOSE: Creates the text format
//
void DirectXTileRenderer::InitializeTextFormat()
{
	check_hresult(::DWriteCreateFactory(
		DWRITE_FACTORY_TYPE_SHARED,
		__uuidof(m_dWriteFactory),
		reinterpret_cast<::IUnknown * *>(m_dWriteFactory.put())));

	check_hresult(m_dWriteFactory->CreateTextFormat(
		L"Segoe UI",
		nullptr,
		DWRITE_FONT_WEIGHT_BOLD,
		DWRITE_FONT_STYLE_NORMAL,
		DWRITE_FONT_STRETCH_NORMAL,
		60.f,
		L"en-US",
		m_textFormat.put()));
	m_textFormat->SetParagraphAlignment(DWRITE_PARAGRAPH_ALIGNMENT_CENTER);
	m_textFormat->SetTextAlignment(DWRITE_TEXT_ALIGNMENT_CENTER);
}

//
//  FUNCTION:CreateFactory
//
//  PURPOSE: Utility function to create the D2DFactory 
//
void DirectXTileRenderer::CreateFactory()
{
	D2D1_FACTORY_OPTIONS options{};
	
	check_hresult(D2D1CreateFactory(
		D2D1_FACTORY_TYPE_SINGLE_THREADED,
		options,
		m_d2dFactory.put()));

	
}

//
//  FUNCTION:CreateDevice
//
//  PURPOSE: Utility function to create the D3D11 device
//
HRESULT DirectXTileRenderer::CreateDevice(D3D_DRIVER_TYPE const type)
{
	WINRT_ASSERT(!m_d3dDevice);

	return D3D11CreateDevice(
		nullptr,
		type,
		nullptr,
		D3D11_CREATE_DEVICE_BGRA_SUPPORT,
		nullptr, 0,
		D3D11_SDK_VERSION,
		m_d3dDevice.put(),
		nullptr,
		nullptr);
}

void DirectXTileRenderer::CreateDevice()
{
	HRESULT hr = CreateDevice(D3D_DRIVER_TYPE_HARDWARE);

	if (DXGI_ERROR_UNSUPPORTED == hr)
	{
		hr = CreateDevice(D3D_DRIVER_TYPE_WARP);
	}

	check_hresult(hr);
}

//
//  FUNCTION: CreateVirtualDrawingSurface
//
//  PURPOSE: Creates a VirtualDrawingSurface into which the D2D contents will be drawn.
//
CompositionDrawingSurface DirectXTileRenderer::CreateVirtualDrawingSurface(SizeInt32 size)
{
	auto graphicsDevice2 = m_graphicsDevice.as<ICompositionGraphicsDevice2>();

	m_virtualSurface = graphicsDevice2.CreateVirtualDrawingSurface(
		size,
		DirectXPixelFormat::R16G16B16A16Float,
		DirectXAlphaMode::Premultiplied);

	return m_virtualSurface;
}

//
//  FUNCTION: CreateVirtualDrawingSurfaceBrush
//
//  PURPOSE: Creates a VirtualDrawingSurface into which the D2D contents will be drawn. Returns a CompositionSurfaceBrush that can be applied to a Composition Visual.
//
CompositionSurfaceBrush DirectXTileRenderer::CreateVirtualDrawingSurfaceBrush()
{
	//Virtual Surface's maximum size is 2^24, per dimension. In this sample the size will never exceed the m_surfaceSize (In this case it is 10000*TILESIZE).
	SizeInt32 size;
	size.Width = m_surfaceSize;
	size.Height = m_surfaceSize;

	m_surfaceInterop = CreateVirtualDrawingSurface(size).as<abi::ICompositionDrawingSurfaceInterop>();

	ICompositionSurface surface = m_surfaceInterop.as<ICompositionSurface>();

	CompositionSurfaceBrush surfaceBrush = m_compositor.CreateSurfaceBrush(surface);

	surfaceBrush.Stretch(CompositionStretch::None);
	surfaceBrush.HorizontalAlignmentRatio(0);
	surfaceBrush.VerticalAlignmentRatio(0);
	surfaceBrush.TransformMatrix(make_float3x2_translation(20.0f, 20.0f));

	return surfaceBrush;
}

// White level scale is used to multiply the color values in the image; allows the user to
// adjust the brightness of the image on an HDR display.
void DirectXTileRenderer::SetRenderOptions(
	RenderEffectKind effect,
	float brightnessAdjustment,
	AdvancedColorInfo const& acInfo,
	Size windowSize
)
{
	m_dispInfo = acInfo;
	m_renderEffectKind = effect;
	m_brightnessAdjust = brightnessAdjustment;

	auto sdrWhite = m_dispInfo ? m_dispInfo.SdrWhiteLevelInNits() : sc_nominalRefWhite;

	UpdateWhiteLevelScale(m_brightnessAdjust, sdrWhite);

	// Adjust the Direct2D effect graph based on RenderEffectKind.
	// Some RenderEffectKind values require us to apply brightness adjustment
	// after the effect as their numerical output is affected by any luminance boost.
	switch (m_renderEffectKind)
	{

		// Effect graph: ImageSource > ColorManagement > WhiteScale
	case RenderEffectKind::None:
		m_whiteScaleEffect.as(m_finalOutput);
		m_whiteScaleEffect->SetInputEffect(0, m_colorManagementEffect.get());
		break;
	}
}

// When connected to an HDR display, the OS renders SDR content (e.g. 8888 UNORM) at
// a user configurable white level; this typically is around 200-300 nits. It is the responsibility
// of an advanced color app (e.g. FP16 scRGB) to emulate the OS-implemented SDR white level adjustment,
// BUT only for non-HDR content (SDR or WCG).
void DirectXTileRenderer::UpdateWhiteLevelScale(float brightnessAdjustment, float sdrWhiteLevel)
{
	float scale = 1.0f;

	switch (m_imageInfo.imageKind)
	{
	case AdvancedColorKind::HighDynamicRange:
		// HDR content should not be compensated by the SdrWhiteLevel parameter.
		scale = 1.0f;
		break;

	case AdvancedColorKind::StandardDynamicRange:
	case AdvancedColorKind::WideColorGamut:
	default:
		scale = sdrWhiteLevel / sc_nominalRefWhite;
		break;
	}

	// The user may want to manually adjust brightness specifically for this image, on top of any
	// white level adjustment for SDR/WCG content. Brightness adjustment using a linear gamma scale
	// is mainly useful for HDR displays, but can be useful for HDR content tonemapped to an SDR/WCG display.
	scale *= brightnessAdjustment;

	// SDR white level scaling is performing by multiplying RGB color values in linear gamma.
	// We implement this with a Direct2D matrix effect.
	D2D1_MATRIX_5X4_F matrix = D2D1::Matrix5x4F(
		scale, 0, 0, 0,  // [R] Multiply each color channel
		0, scale, 0, 0,  // [G] by the scale factor in 
		0, 0, scale, 0,  // [B] linear gamma space.
		0, 0, 0, 1,  // [A] Preserve alpha values.
		0, 0, 0, 0); //     No offset.

	check_hresult(m_whiteScaleEffect->SetValue(D2D1_COLORMATRIX_PROP_COLOR_MATRIX, matrix));
}

// Reads the provided data stream and decodes an image from it using WIC. These resources are device-
// independent.
ImageInfo DirectXTileRenderer::LoadImageFromWic(_In_ IStream* imageStream)
{

	// Decode the image using WIC.
	com_ptr<IWICBitmapDecoder> decoder;
	check_hresult(
		m_wicFactory->CreateDecoderFromStream(
			imageStream,
			nullptr,
			WICDecodeMetadataCacheOnDemand,
			decoder.put()
		));

	com_ptr<IWICBitmapFrameDecode> frame;
	check_hresult(
		decoder->GetFrame(0, frame.put())
	);

	return LoadImageCommon(frame.get());
}


// Reads the provided File and decodes an image from it using WIC. These resources are device-
// independent.
ImageInfo DirectXTileRenderer::LoadImageFromWic(LPCWSTR szFileName)
{

	// Create a decoder
	IWICBitmapDecoder* pDecoder = nullptr;

	// Decode the image using WIC.
	com_ptr<IWICBitmapDecoder> decoder;
	check_hresult(
		m_wicFactory->CreateDecoderFromFilename(
			szFileName,                      // Image to be decoded
			nullptr,                         // Do not prefer a particular vendor
			GENERIC_READ,                    // Desired read access to the file
			WICDecodeMetadataCacheOnDemand,  // Cache metadata when needed
			decoder.put()					 // Pointer to the decoder
		));


	// Retrieve the first frame of the image from the decoder
	com_ptr<IWICBitmapFrameDecode> frame;
	check_hresult(
		decoder->GetFrame(0, frame.put())
	);

	return LoadImageCommon(frame.get());

}



// After initial decode, obtain image information and do common setup.
// Populates all members of ImageInfo.
ImageInfo DirectXTileRenderer::LoadImageCommon(_In_ IWICBitmapSource* source)
{
	m_imageInfo = {};

	// Attempt to read the embedded color profile from the image; only valid for WIC images.
	com_ptr<IWICBitmapFrameDecode> frame;
	HRESULT hr = (reinterpret_cast<::IUnknown*>(source)->QueryInterface(winrt::guid_of<IWICBitmapFrameDecode>(),
		reinterpret_cast<void**>(winrt::put_abi(frame))));
	if (hr >= 0)
	{
		check_hresult(
			m_wicFactory->CreateColorContext(m_wicColorContext.put())
		);

		IWICColorContext* temp = m_wicColorContext.get();

		check_hresult(
			frame->GetColorContexts(
				1,
				&temp,
				&m_imageInfo.numProfiles
			)
		);
	}

	// Check whether the image data is natively stored in a floating-point format, and
	// decode to the appropriate WIC pixel format.

	WICPixelFormatGUID pixelFormat;
	check_hresult(
		source->GetPixelFormat(&pixelFormat)
	);

	com_ptr<IWICComponentInfo> componentInfo;
	check_hresult(
		m_wicFactory->CreateComponentInfo(
			pixelFormat,
			componentInfo.put()
		)
	);

	com_ptr<IWICPixelFormatInfo2> pixelFormatInfo = componentInfo.as<IWICPixelFormatInfo2>();


	WICPixelFormatNumericRepresentation formatNumber;
	check_hresult(
		pixelFormatInfo->GetNumericRepresentation(&formatNumber)
	);

	check_hresult(pixelFormatInfo->GetBitsPerPixel(&m_imageInfo.bitsPerPixel));

	// Calculate the bits per channel (bit depth) using GetChannelMask.
	// This accounts for nonstandard color channel packing and padding, e.g. 32bppRGB.
	unsigned char channelMaskBytes[sc_MaxBytesPerPixel];
	ZeroMemory(channelMaskBytes, ARRAYSIZE(channelMaskBytes));
	unsigned int maskSize;

	check_hresult(
		pixelFormatInfo->GetChannelMask(
			0,  // Read the first color channel.
			ARRAYSIZE(channelMaskBytes),
			channelMaskBytes,
			&maskSize)
	);

	// Count up the number of bits set in the mask for the first color channel.
	for (unsigned int i = 0; i < maskSize * 8; i++)
	{
		unsigned int byte = i / 8;
		unsigned int bit = i % 8;
		if ((channelMaskBytes[byte] & (1 << bit)) != 0)
		{
			m_imageInfo.bitsPerChannel += 1;
		}
	}

	m_imageInfo.isFloat = (WICPixelFormatNumericRepresentationFloat == formatNumber) ? true : false;

	// When decoding, preserve the numeric representation (float vs. non-float)
	// of the native image data. This avoids WIC performing an implicit gamma conversion
	// which occurs when converting between a fixed-point/integer pixel format (sRGB gamma)
	// and a float-point pixel format (linear gamma). Gamma adjustment, if specified by
	// the ICC profile, will be performed by the Direct2D color management effect.

	WICPixelFormatGUID fmt = {};
	if (m_imageInfo.isFloat)
	{
		fmt = GUID_WICPixelFormat64bppPRGBAHalf; // Equivalent to DXGI_FORMAT_R16G16B16A16_FLOAT.
	}
	else
	{
		fmt = GUID_WICPixelFormat64bppPRGBA; // Equivalent to DXGI_FORMAT_R16G16B16A16_UNORM.
											 // Many SDR images (e.g. JPEG) use <=32bpp, so it
											 // is possible to further optimize this for memory usage.
	}

	check_hresult(
		m_wicFactory->CreateFormatConverter(m_formatConvert.put())
	);

	check_hresult(
		m_formatConvert->Initialize(
			source,
			fmt,
			WICBitmapDitherTypeNone,
			nullptr,
			0.0f,
			WICBitmapPaletteTypeCustom
		)
	);

	UINT width;
	UINT height;
	check_hresult(
		m_formatConvert->GetSize(&width, &height)
	);

	m_imageInfo.size = Size(static_cast<float>(width), static_cast<float>(height));

	PopulateImageInfoACKind(&m_imageInfo);

	return m_imageInfo;
}


// Simplified heuristic to determine what advanced color kind the image is.
// Requires that all fields other than imageKind are populated.
void DirectXTileRenderer::PopulateImageInfoACKind(_Inout_ ImageInfo* info)
{
	if (info->bitsPerPixel == 0 ||
		info->bitsPerChannel == 0 ||
		info->size.Width == 0 ||
		info->size.Height == 0)
	{
		check_hresult(E_INVALIDARG);
	}

	info->imageKind = AdvancedColorKind::StandardDynamicRange;

	// Bit depth > 8bpc or color gamut > sRGB signifies a WCG image.
	// The presence of a color profile is used as an approximation for wide gamut.
	if (info->bitsPerChannel > 8 || info->numProfiles >= 1)
	{
		info->imageKind = AdvancedColorKind::WideColorGamut;
	}

	// This application currently only natively supports HDR images with floating point.
	// An image encoded using the HDR10 colorspace is also HDR, but this
	// is not automatically detected by the application.
	if (info->isFloat == true)
	{
		info->imageKind = AdvancedColorKind::HighDynamicRange;
	}
}


// Call this after updating any spatial transform state to regenerate the effect graph.
void DirectXTileRenderer::UpdateImageTransformState()
{
	if (m_imageSource)
	{
		// When using ID2D1ImageSource, the recommend method of scaling is to use
		// ID2D1TransformedImageSource. It is inexpensive to recreate this object.
		D2D1_TRANSFORMED_IMAGE_SOURCE_PROPERTIES props =
		{
			D2D1_ORIENTATION_DEFAULT,
			m_zoom,
			m_zoom,
			D2D1_INTERPOLATION_MODE_LINEAR, // This is ignored when using DrawImage.
			D2D1_TRANSFORMED_IMAGE_SOURCE_OPTIONS_NONE
		};

		check_hresult(
			m_d2dContext->CreateTransformedImageSource(
				m_imageSource.get(),
				&props,
				m_scaledImage.put()
			)
		);

		// Set the new image as the new source to the effect pipeline.
		m_colorManagementEffect->SetInput(0, m_scaledImage.get());
	}
}


void DirectXTileRenderer::CreateDeviceIndependentResources()
{
	namespace abi = ABI::Windows::UI::Composition;

	CreateFactory();
	CreateDevice();
	com_ptr<IDXGIDevice> const dxdevice = m_d3dDevice.as<IDXGIDevice>();
	com_ptr<abi::ICompositorInterop> interopCompositor = m_compositor.as<abi::ICompositorInterop>();

	check_hresult(m_d2dFactory->CreateDevice(dxdevice.get(), m_d2dDevice.put()));
	check_hresult(interopCompositor->CreateGraphicsDevice(m_d2dDevice.get(), reinterpret_cast<abi::ICompositionGraphicsDevice * *>(put_abi(m_graphicsDevice))));
	check_hresult(
		CoCreateInstance(
			CLSID_WICImagingFactory2,
			nullptr,
			CLSCTX_INPROC_SERVER,
			__uuidof(m_wicFactory),
			m_wicFactory.put_void())
	);
}

// Set HDR10 metadata to allow HDR displays to optimize behavior based on our content.
void DirectXTileRenderer::EmitHdrMetadata()
{
	//auto acKind = m_dispInfo ? m_dispInfo.CurrentAdvancedColorKind : AdvancedColorKind::StandardDynamicRange;
	//TODO: hardcoded to HDR for now. Need to fix this later.
	//if (acKind == AdvancedColorKind::HighDynamicRange)
	{
		DXGI_HDR_METADATA_HDR10 metadata = {};

		// This sample doesn't do any chrominance (e.g. xy) gamut mapping, so just use default
		// color primaries values; a more sophisticated app will explicitly set these.
		// DXGI_HDR_METADATA_HDR10 defines primaries as 1/50000 of a unit in xy space.
		metadata.RedPrimary[0] = static_cast<UINT16>(m_dispInfo.RedPrimary().X   * 50000.0f);
		metadata.RedPrimary[1] = static_cast<UINT16>(m_dispInfo.RedPrimary().Y   * 50000.0f);
		metadata.GreenPrimary[0] = static_cast<UINT16>(m_dispInfo.GreenPrimary().X * 50000.0f);
		metadata.GreenPrimary[1] = static_cast<UINT16>(m_dispInfo.GreenPrimary().Y * 50000.0f);
		metadata.BluePrimary[0] = static_cast<UINT16>(m_dispInfo.BluePrimary().X  * 50000.0f);
		metadata.BluePrimary[1] = static_cast<UINT16>(m_dispInfo.BluePrimary().Y  * 50000.0f);
		metadata.WhitePoint[0] = static_cast<UINT16>(m_dispInfo.WhitePoint().X   * 50000.0f);
		metadata.WhitePoint[1] = static_cast<UINT16>(m_dispInfo.WhitePoint().Y   * 50000.0f);

		float effectiveMaxCLL = 0;

		switch (m_renderEffectKind)
		{
			// Currently only the "None" render effect results in pixel values that exceed
			// the OS-specified SDR white level, as it just passes through HDR color values.
		case RenderEffectKind::None:
			effectiveMaxCLL = max(m_maxCLL, 0.0f) * m_brightnessAdjust;
			break;

		default:
			effectiveMaxCLL = m_dispInfo.SdrWhiteLevelInNits() * m_brightnessAdjust;
			break;
		}

		// DXGI_HDR_METADATA_HDR10 defines MaxCLL in integer nits.
		metadata.MaxContentLightLevel = static_cast<UINT16>(effectiveMaxCLL);

		// The luminance analysis doesn't calculate MaxFrameAverageLightLevel. We also don't have mastering
		// information (i.e. reference display in a studio), so Min/MaxMasteringLuminance is not relevant.
		// Leave these values as 0.

		//TODO set 
		/*auto sc = m_deviceResources->GetSwapChain();

		ComPtr<IDXGISwapChain4> sc4;
		DX::ThrowIfFailed(sc->QueryInterface(IID_PPV_ARGS(&sc4)));
		DX::ThrowIfFailed(sc4->SetHDRMetaData(DXGI_HDR_METADATA_TYPE_HDR10, sizeof(metadata), &metadata));*/
	}
}



void DirectXTileRenderer::CreateImageDependentResources()
{
	// Create the Direct2D device object and a corresponding context.
	com_ptr<IDXGIDevice3> dxgiDevice;
	dxgiDevice = m_d3dDevice.as<IDXGIDevice3>();


	com_ptr<ID2D1Device5>            d2dDevice;
	d2dDevice = m_d2dDevice.as<ID2D1Device5>();

	check_hresult(
		d2dDevice->CreateDeviceContext(
			D2D1_DEVICE_CONTEXT_OPTIONS_NONE,
			m_d2dContext.put()
		)
	);

	// Load the image from WIC using ID2D1ImageSource.
	check_hresult(
		m_d2dContext->CreateImageSourceFromWic(
			m_formatConvert.get(),
			m_imageSource.put()
		)
	);

	check_hresult(
		m_d2dContext->CreateEffect(CLSID_D2D1ColorManagement, m_colorManagementEffect.put())
	);

	check_hresult(
		m_colorManagementEffect->SetValue(
			D2D1_COLORMANAGEMENT_PROP_QUALITY,
			D2D1_COLORMANAGEMENT_QUALITY_BEST   // Required for floating point and DXGI color space support.
		)
	);


	UpdateImageColorContext();

	// The destination color space is the render target's (swap chain's) color space. This app uses an
	// FP16 swap chain, which requires the colorspace to be scRGB.
	com_ptr<ID2D1ColorContext1> destColorContext;
	check_hresult(
		m_d2dContext->CreateColorContextFromDxgiColorSpace(
			DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709, // scRGB
			destColorContext.put()
		)
	);

	check_hresult(
		m_colorManagementEffect->SetValue(
			D2D1_COLORMANAGEMENT_PROP_DESTINATION_COLOR_CONTEXT,
			destColorContext.get()
		)
	);


	// White level scale is used to multiply the color values in the image; this allows the user
   // to adjust the brightness of the image on an HDR display.
	check_hresult(m_d2dContext->CreateEffect(CLSID_D2D1ColorMatrix, m_whiteScaleEffect.put()));

	// Input to white level scale may be modified in SetRenderOptions.
	m_whiteScaleEffect->SetInputEffect(0, m_colorManagementEffect.get());


}

// Derive the source color context from the image (embedded ICC profile or metadata).
void DirectXTileRenderer::UpdateImageColorContext()
{
	com_ptr<ID2D1ColorContext> sourceColorContext;

	// For most image types, automatically derive the color context from the image.
	if (m_imageInfo.numProfiles >= 1)
	{
		check_hresult(
			m_d2dContext->CreateColorContextFromWicColorContext(
				m_wicColorContext.get(),
				sourceColorContext.put()
			)
		);
	}
	else
	{
		// Since no embedded color profile/metadata exists, select a default
		// based on the pixel format: floating point == scRGB, others == sRGB.
		check_hresult(
			m_d2dContext->CreateColorContext(
				m_imageInfo.isFloat ? D2D1_COLOR_SPACE_SCRGB : D2D1_COLOR_SPACE_SRGB,
				nullptr,
				0,
				sourceColorContext.put()
			)
		);
	}

	check_hresult(
		m_colorManagementEffect->SetValue(
			D2D1_COLORMANAGEMENT_PROP_SOURCE_COLOR_CONTEXT,
			sourceColorContext.get()
		)
	);
}

// Uses a histogram to compute a modified version of MaxCLL (ST.2086 max content light level).
// Performs Begin/EndDraw on the D2D context.
void DirectXTileRenderer::ComputeHdrMetadata()
{
	// Initialize with a sentinel value.
	m_maxCLL = -1.0f;

	// MaxCLL is not meaningful for SDR or WCG images.
	if ((!m_isComputeSupported) ||
		(m_imageInfo.imageKind != AdvancedColorKind::HighDynamicRange))
	{
		return;
	}

	// MaxCLL is nominally calculated for the single brightest pixel in a frame.
	// But we take a slightly more conservative definition that takes the 99.99th percentile
	// to account for extreme outliers in the image.
	float maxCLLPercent = 0.9999f;


	m_d2dContext->BeginDraw();

	m_d2dContext->DrawImage(m_histogramEffect.get());

	// We ignore D2DERR_RECREATE_TARGET here. This error indicates that the device
	// is lost. It will be handled during the next call to Present.
	HRESULT hr = m_d2dContext->EndDraw();
	if (hr != D2DERR_RECREATE_TARGET)
	{
		check_hresult(hr);
	}

	float* histogramData = new float[sc_histNumBins];
	check_hresult(
		m_histogramEffect->GetValue(D2D1_HISTOGRAM_PROP_HISTOGRAM_OUTPUT,
			reinterpret_cast<BYTE*>(histogramData),
			sc_histNumBins * sizeof(float)
		)
	);

	unsigned int maxCLLbin = 0;
	float runningSum = 0.0f; // Cumulative sum of values in histogram is 1.0.
	for (int i = sc_histNumBins - 1; i >= 0; i--)
	{
		runningSum += histogramData[i];
		maxCLLbin = i;

		if (runningSum >= 1.0f - maxCLLPercent)
		{
			break;
		}
	}

	float binNorm = static_cast<float>(maxCLLbin) / static_cast<float>(sc_histNumBins);
	m_maxCLL = powf(binNorm, 1 / sc_histGamma) * sc_histMaxNits;

	// Some drivers have a bug where histogram will always return 0. Treat this as unknown.
	m_maxCLL = (m_maxCLL == 0.0f) ? -1.0f : m_maxCLL;
}


// Overrides any pan/zoom state set by the user to fit image to the window size.
// Returns the computed MaxCLL of the image in nits.
float DirectXTileRenderer::FitImageToWindow(Size panelSize)
{
	if (m_imageSource)
	{
		// Set image to be letterboxed in the window, up to the max allowed scale factor.
		float letterboxZoom = min(
			panelSize.Width / m_imageInfo.size.Width,
			panelSize.Height / m_imageInfo.size.Height);

		//m_zoom = min(sc_MaxZoom, letterboxZoom);
		//Hardcoding to 1 zoom. TODO: Fix this.
		m_zoom = 1.0f;

		// Center the image.
		m_imageOffset = D2D1::Point2F(
			(panelSize.Width - (m_imageInfo.size.Width * m_zoom)) / 2.0f,
			(panelSize.Height - (m_imageInfo.size.Height * m_zoom)) / 2.0f
		);

		UpdateImageTransformState();

		// HDR metadata is supposed to be independent of any rendering options, but
		// we can't compute it until the full effect graph is hooked up, which is here.
		ComputeHdrMetadata();
	}

	return m_maxCLL;
}

//
//  FUNCTION: Constructor for Tile Struct
//
//  PURPOSE: Creates a Tile object based on rows and columns
//
Tile::Tile(int lrow, int lcolumn, int tileSize)
{
	int x = lcolumn * tileSize;
	int y = lrow * tileSize;
	row = lrow;
	column = lcolumn;
	rect = Rect((float)x, (float)y, tileSize, tileSize);
}