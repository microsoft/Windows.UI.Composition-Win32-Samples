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
	bool DrawTileRange(Rect rect, std::list<Tile> const& tiles);

private:
	void DrawTile(ID2D1DeviceContext* d2dDeviceContext, ID2D1SolidColorBrush* textBrush, ID2D1SolidColorBrush* tileBrush, Tile tile, POINT differenceOffset);
	void DrawTextInTile(int tileRow, int tileColumn, D2D1_RECT_F rect, ID2D1DeviceContext*  d2dDeviceContext, ID2D1SolidColorBrush* textBrush);
	void InitializeTextFormat();
	com_ptr<ID2D1Factory1> CreateFactory();
	HRESULT CreateDevice(D3D_DRIVER_TYPE const type, com_ptr<ID3D11Device>& device);
	com_ptr<ID3D11Device> CreateDevice();
	CompositionSurfaceBrush CreateVirtualDrawingSurfaceBrush();
	CompositionDrawingSurface CreateVirtualDrawingSurface(SizeInt32 size);
	bool CheckForDeviceRemoved(HRESULT hr);

	//member variables
	com_ptr<IDWriteFactory>                 m_dWriteFactory;
	com_ptr<IDWriteTextFormat>              m_textFormat;
	com_ptr<ICompositionGraphicsDevice>     m_graphicsDevice ;
	com_ptr<ICompositionGraphicsDevice2>    m_graphicsDevice2 ;
	CompositionVirtualDrawingSurface        m_virtualSurface = nullptr;
	CompositionSurfaceBrush                 m_surfaceBrush = nullptr;
	Compositor                              m_compositor = nullptr;
	float                                   m_colorCounter = 0.0;
	int                                     m_tileSize = 0;
	int                                     m_surfaceSize = 0;
	com_ptr<ABI::Windows::UI::Composition::ICompositionDrawingSurfaceInterop> m_surfaceInterop ;

};




