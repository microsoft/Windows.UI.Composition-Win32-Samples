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
#pragma once

using namespace winrt;
using namespace Windows::System;
using namespace Windows::UI;
using namespace Windows::UI::Composition;
using namespace Windows::UI::Composition::Desktop;
using namespace Windows::Graphics;
using namespace Windows::Graphics::Display;
using namespace Windows::Graphics::DirectX;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Numerics;

namespace abi = ABI::Windows::UI::Composition;

struct ImageInfo
{
	unsigned int                                    bitsPerPixel;
	unsigned int                                    bitsPerChannel;
	bool                                            isFloat;
	Windows::Foundation::Size                       size;
	unsigned int                                    numProfiles;
	Windows::Graphics::Display::AdvancedColorKind   imageKind;
};

/// <summary>
/// Supported render effects which are inserted into the render pipeline.
/// Includes HDR tonemappers and useful visual tools.
/// Each render effect is implemented as a custom Direct2D effect.
/// </summary>
enum class RenderEffectKind
{
	//ReinhardTonemap,
	//FilmicTonemap,
	None,
	//SdrOverlay,
	//LuminanceHeatmap
};

struct Tile
{
	Tile(int row, int column, int tileSize);
	Rect rect;
	int row;
	int column;
};

class DirectXTileRenderer
{
public:
	void Initialize(Compositor const& compositor, int tileSize, int surfaceSize);
	void Trim(Rect trimRect);
	CompositionSurfaceBrush getSurfaceBrush();
	bool DrawTile(Rect rect);
	void SetRenderOptions(RenderEffectKind effect, float brightnessAdjustment, AdvancedColorInfo const& acInfo, Size windowSize);
	float FitImageToWindow(Size panelSize);
	ImageInfo LoadImageFromWic(_In_ IStream* imageStream);
	ImageInfo LoadImageFromWic(LPCWSTR szFileName);
	void CreateImageDependentResources();

private:
	void InitializeTextFormat();
	void CreateFactory();
	HRESULT CreateDevice(D3D_DRIVER_TYPE const type);
	void CreateDevice();
	CompositionSurfaceBrush CreateVirtualDrawingSurfaceBrush();
	CompositionDrawingSurface CreateVirtualDrawingSurface(SizeInt32 size);
	bool CheckForDeviceRemoved(HRESULT hr);
	void UpdateImageTransformState();
	void CreateDeviceIndependentResources();
	void UpdateWhiteLevelScale(float brightnessAdjustment, float sdrWhiteLevel);
	ImageInfo LoadImageCommon(_In_ IWICBitmapSource* source);
	void PopulateImageInfoACKind(_Inout_ ImageInfo* info);
	void EmitHdrMetadata();
	void UpdateImageColorContext();
	void ComputeHdrMetadata();

	//member variables
	com_ptr<IDWriteFactory>                 m_dWriteFactory;
	com_ptr<ID2D1DeviceContext5>            m_d2dContext;
	com_ptr<IDWriteTextFormat>              m_textFormat;
	com_ptr<ICompositionGraphicsDevice>     m_graphicsDevice;
	com_ptr<ICompositionGraphicsDevice2>    m_graphicsDevice2;
	CompositionVirtualDrawingSurface        m_virtualSurface = nullptr;
	CompositionSurfaceBrush                 m_surfaceBrush = nullptr;
	Compositor                              m_compositor = nullptr;
	float                                   m_colorCounter = 0.0;
	int                                     m_tileSize = 0;
	int                                     m_surfaceSize = 0;
	com_ptr<ABI::Windows::UI::Composition::ICompositionDrawingSurfaceInterop> m_surfaceInterop;

	// WIC and Direct2D resources.
	com_ptr<ID3D11Device>					 m_d3dDevice;
	com_ptr<ID2D1Device>					 m_d2dDevice;
	com_ptr<ID2D1Factory1>					 m_d2dFactory;
	com_ptr<IWICFormatConverter>             m_formatConvert;
	com_ptr<IWICColorContext>                m_wicColorContext;
	com_ptr<ID2D1ImageSourceFromWic>         m_imageSource;
	com_ptr<ID2D1TransformedImageSource>     m_scaledImage;
	com_ptr<ID2D1Effect>                     m_colorManagementEffect;
	com_ptr<ID2D1Effect>                     m_whiteScaleEffect;
	com_ptr<ID2D1Effect>                     m_reinhardEffect;
	com_ptr<ID2D1Effect>                     m_filmicEffect;
	com_ptr<ID2D1Effect>                     m_sdrOverlayEffect;
	com_ptr<ID2D1Effect>                     m_heatmapEffect;
	com_ptr<ID2D1Effect>                     m_histogramPrescale;
	com_ptr<ID2D1Effect>                     m_histogramEffect;
	com_ptr<ID2D1Effect>                     m_finalOutput;
	com_ptr<IWICImagingFactory2>			 m_wicFactory;

	// Other renderer members.
	RenderEffectKind                        m_renderEffectKind;
	float                                   m_zoom;
	float                                   m_minZoom;
	D2D1_POINT_2F                           m_imageOffset;
	D2D1_POINT_2F                           m_pointerPos;
	float                                   m_maxCLL; // In nits.
	float                                   m_brightnessAdjust;
	AdvancedColorInfo						m_dispInfo{nullptr};
	ImageInfo                               m_imageInfo;
	bool                                    m_isComputeSupported;
};
