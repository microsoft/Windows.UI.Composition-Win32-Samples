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

#include "pch.h"
#include "SimpleCapture.h"
#include <windows.h>
#include <d3d11.h>
#include <dxgi1_2.h>
#include <sal.h>
#include <new>
#include <warning.h>
#include <DirectXMath.h>
#include "PixelShader.h"

using namespace winrt;
//using namespace Windows;
using namespace Windows::Foundation;
using namespace Windows::System;
//using namespace Windows::Graphics;
using namespace Windows::Graphics::Capture;
using namespace Windows::Graphics::DirectX;
using namespace Windows::Graphics::DirectX::Direct3D11;
using namespace Windows::Foundation::Numerics;
using namespace Windows::UI;
using namespace Windows::UI::Composition;

using namespace DirectX;

typedef struct _VERTEX
{
	DirectX::XMFLOAT3 Pos;
	DirectX::XMFLOAT2 TexCoord;
} VERTEX;

//
// A vertex with a position and texture coordinate
//
SimpleCapture::SimpleCapture(
	IDirect3DDevice const& device,
	GraphicsCaptureItem const& item,
	HWND const& drawingHandle)
{
    m_item = item;
    m_device = device;
	m_WindowHandle = drawingHandle;
	HRESULT hr = S_OK;
	// Set up 
    //auto d3dDevice = GetDXGIInterfaceFromObject<ID3D11Device>(m_device);
    //d3dDevice->GetImmediateContext(m_d3dContext.put());

	m_3dDevice = GetDXGIInterfaceFromObject<ID3D11Device>(m_device);
	m_3dDevice->GetImmediateContext(m_d3dContext.put());

	// Get DXGI factory
	IDXGIDevice* DxgiDevice = nullptr;
	hr = m_3dDevice->QueryInterface(__uuidof(IDXGIDevice), reinterpret_cast<void**>(&DxgiDevice));	
	if (FAILED(hr))
	{
		assert(false);
	}

	IDXGIAdapter* DxgiAdapter = nullptr;
	hr = DxgiDevice->GetParent(__uuidof(IDXGIAdapter), reinterpret_cast<void**>(&DxgiAdapter));
	DxgiDevice->Release();
	DxgiDevice = nullptr;
	if (FAILED(hr))
	{
		assert(false);
	}

	hr = DxgiAdapter->GetParent(__uuidof(IDXGIFactory2), reinterpret_cast<void**>(&m_Factory));
	DxgiAdapter->Release();
	DxgiAdapter = nullptr;
	if (FAILED(hr))
	{
		assert(false);
	}

	// Get window size
	RECT WindowRect;
	GetClientRect(m_WindowHandle, &WindowRect);
	UINT Width = WindowRect.right - WindowRect.left;
	UINT Height = WindowRect.bottom - WindowRect.top;

	// Create swapchain for window
	DXGI_SWAP_CHAIN_DESC1 SwapChainDesc;
	RtlZeroMemory(&SwapChainDesc, sizeof(SwapChainDesc));

	SwapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL;
	SwapChainDesc.BufferCount = 2;
	SwapChainDesc.Width = Width;
	SwapChainDesc.Height = Height;
	SwapChainDesc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
	SwapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
	SwapChainDesc.SampleDesc.Count = 1;
	SwapChainDesc.SampleDesc.Quality = 0;
	hr = m_Factory->CreateSwapChainForHwnd(m_3dDevice.get(), m_WindowHandle, &SwapChainDesc, nullptr, nullptr, &m_SwapChain);
	if (FAILED(hr))
	{
		assert(false);
	}

	// Disable the ALT-ENTER shortcut for entering full-screen mode
	hr = m_Factory->MakeWindowAssociation(m_WindowHandle, DXGI_MWA_NO_ALT_ENTER);
	if (FAILED(hr))
	{
		assert(false);
	}

	// Create shared texture
	hr = CreateSharedSurf();
	if (FAILED(hr))
	{
		assert(false);
	}
	// Make new render target view
	hr = MakeRTV();
	if (FAILED(hr))
	{
		assert(false);
	}
	// Set view port
	hr = SetViewPort(Width, Height);
	if (FAILED(hr))
	{
		assert(false);
	}
	// Create the sample state
	D3D11_SAMPLER_DESC SampDesc;
	RtlZeroMemory(&SampDesc, sizeof(SampDesc));
	SampDesc.Filter = D3D11_FILTER_MIN_MAG_MIP_LINEAR;
	SampDesc.AddressU = D3D11_TEXTURE_ADDRESS_CLAMP;
	SampDesc.AddressV = D3D11_TEXTURE_ADDRESS_CLAMP;
	SampDesc.AddressW = D3D11_TEXTURE_ADDRESS_CLAMP;
	SampDesc.ComparisonFunc = D3D11_COMPARISON_NEVER;
	SampDesc.MinLOD = 0;
	SampDesc.MaxLOD = D3D11_FLOAT32_MAX;
	hr = m_3dDevice->CreateSamplerState(&SampDesc, &m_SamplerLinear);
	if (FAILED(hr))
	{
		assert(false);
	}

	// Create the blend state
	D3D11_BLEND_DESC BlendStateDesc;
	BlendStateDesc.AlphaToCoverageEnable = FALSE;
	BlendStateDesc.IndependentBlendEnable = FALSE;
	BlendStateDesc.RenderTarget[0].BlendEnable = TRUE;
	BlendStateDesc.RenderTarget[0].SrcBlend = D3D11_BLEND_SRC_ALPHA;
	BlendStateDesc.RenderTarget[0].DestBlend = D3D11_BLEND_INV_SRC_ALPHA;
	BlendStateDesc.RenderTarget[0].BlendOp = D3D11_BLEND_OP_ADD;
	BlendStateDesc.RenderTarget[0].SrcBlendAlpha = D3D11_BLEND_ONE;
	BlendStateDesc.RenderTarget[0].DestBlendAlpha = D3D11_BLEND_ZERO;
	BlendStateDesc.RenderTarget[0].BlendOpAlpha = D3D11_BLEND_OP_ADD;
	BlendStateDesc.RenderTarget[0].RenderTargetWriteMask = D3D11_COLOR_WRITE_ENABLE_ALL;
	hr = m_3dDevice->CreateBlendState(&BlendStateDesc, &m_BlendState);
	if (FAILED(hr))
	{
		assert(false);
	}

	// Initialize shaders
	hr = InitShaders();
	if (FAILED(hr))
	{
		assert(false);
	}
	// END OF ADDED CHANGES
	auto size = m_item.Size();

    /*m_swapChain = CreateDXGISwapChain(
        d3dDevice, 
		static_cast<uint32_t>(size.Width),
		static_cast<uint32_t>(size.Height),
        static_cast<DXGI_FORMAT>(DirectXPixelFormat::B8G8R8A8UIntNormalized),
        2);*/

	// Create framepool, define pixel format (DXGI_FORMAT_B8G8R8A8_UNORM), and frame size. 
    m_framePool = Direct3D11CaptureFramePool::Create(
        m_device,
        DirectXPixelFormat::B8G8R8A8UIntNormalized,
        2,
		size);
    m_session = m_framePool.CreateCaptureSession(m_item);
    //m_lastSize = size;
	m_frameArrived = m_framePool.FrameArrived(auto_revoke, { this, &SimpleCapture::OnFrameArrived });
}
//
// Initialize shaders for drawing to screen
//
HRESULT SimpleCapture::InitShaders()
{
	HRESULT hr;

	UINT Size = ARRAYSIZE(g_VS1);
	hr = m_3dDevice->CreateVertexShader(g_VS1, Size, nullptr, &m_VertexShader);
	if (FAILED(hr))
	{
		assert(false);
		return hr;
	}

	D3D11_INPUT_ELEMENT_DESC Layout[] =
	{
		{"POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0, D3D11_INPUT_PER_VERTEX_DATA, 0},
		{"TEXCOORD", 0, DXGI_FORMAT_R32G32_FLOAT, 0, 12, D3D11_INPUT_PER_VERTEX_DATA, 0}
	};
	UINT NumElements = ARRAYSIZE(Layout);

	hr = m_3dDevice->CreateInputLayout(Layout, NumElements, g_VS1, Size, &m_InputLayout);
	if (FAILED(hr))
	{
		assert(false);
		return hr;
	}

	m_d3dContext->IASetInputLayout(m_InputLayout);

	Size = ARRAYSIZE(g_main);
	hr = m_3dDevice->CreatePixelShader(g_main, Size, nullptr, &m_PixelShader);
	if (FAILED(hr))
	{
		assert(false);
		return hr;
	}

	return S_OK;
}

HRESULT SimpleCapture::SetViewPort(UINT Width, UINT Height)
{
	D3D11_VIEWPORT VP;
	VP.Width = static_cast<FLOAT>(Width);
	VP.Height = static_cast<FLOAT>(Height);
	VP.MinDepth = 0.0f;
	VP.MaxDepth = 1.0f;
	VP.TopLeftX = 0;
	VP.TopLeftY = 0;
	m_d3dContext->RSSetViewports(1, &VP);
	return S_OK;
}

//
// Reset render target view
//
HRESULT SimpleCapture::MakeRTV()
{
	// Get backbuffer
	ID3D11Texture2D* BackBuffer = nullptr;
	HRESULT hr = m_SwapChain->GetBuffer(0, __uuidof(ID3D11Texture2D), reinterpret_cast<void**>(&BackBuffer));
	if (FAILED(hr))
	{
		assert(false);
		return hr;
	}
	// Create a render target view
	hr = m_3dDevice->CreateRenderTargetView(BackBuffer, nullptr, &m_RTV);
	BackBuffer->Release();
	if (FAILED(hr))
	{
		assert(false);
		return hr;
	}
	// Set new render target
	m_d3dContext->OMSetRenderTargets(1, &m_RTV, nullptr);

	return S_OK;
}

//
// Recreate shared texture
//
HRESULT SimpleCapture::CreateSharedSurf()
{
	auto size = m_item.Size();
	RECT magWindowRect;
	GetClientRect(m_WindowHandle, &magWindowRect); 

	// Create shared texture for all duplication threads to draw into
	D3D11_TEXTURE2D_DESC DeskTexD;
	RtlZeroMemory(&DeskTexD, sizeof(D3D11_TEXTURE2D_DESC));
	DeskTexD.Width = static_cast<uint32_t>(size.Width);
	DeskTexD.Height = static_cast<uint32_t>(size.Height);
	DeskTexD.MipLevels = 1;
	DeskTexD.ArraySize = 1;
	DeskTexD.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
	DeskTexD.SampleDesc.Count = 1;
	DeskTexD.Usage = D3D11_USAGE_DEFAULT;
	DeskTexD.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
	DeskTexD.CPUAccessFlags = 0;
	DeskTexD.MiscFlags = 0;

	HRESULT hr = m_3dDevice->CreateTexture2D(&DeskTexD, nullptr, &m_SharedSurf);
	if (FAILED(hr))
	{
		assert(false);
		return hr;
	}	
	return S_OK;
}

//
// 

// Start sending capture frames
void SimpleCapture::StartCapture(HWND drawingHandle)
{
    CheckClosed();
    m_session.StartCapture();
}

ICompositionSurface SimpleCapture::CreateSurface(
    Compositor const& compositor)
{
    CheckClosed();
	return CreateCompositionSurfaceForSwapChain(compositor, m_SwapChain);
}

// Process captured frames
void SimpleCapture::Close()
{
    auto expected = false;
    if (m_closed.compare_exchange_strong(expected, true))
    {
		m_frameArrived.revoke();
		m_framePool.Close();
        m_session.Close();

        m_SwapChain = nullptr;
        m_framePool = nullptr;
        m_session = nullptr;
        m_item = nullptr;
    }
}

void SimpleCapture::OnFrameArrived(
    Direct3D11CaptureFramePool const& sender,
    winrt::Windows::Foundation::IInspectable const&)
{
	auto newSize = false;
	{
		auto frame = sender.TryGetNextFrame();
		auto frameContentSize = frame.ContentSize();

		auto frameSurface = GetDXGIInterfaceFromObject<ID3D11Texture2D>(frame.Surface());

		m_d3dContext->CopyResource(m_SharedSurf, frameSurface.get());

		// Vertices for drawing whole texture
		VERTEX Vertices[NUMVERTICES] =
		{
			{XMFLOAT3(-1.0f, -1.0f, 0), XMFLOAT2(0.0f, 1.0f)},
			{XMFLOAT3(-1.0f, 1.0f, 0), XMFLOAT2(0.0f, 0.0f)},
			{XMFLOAT3(1.0f, -1.0f, 0), XMFLOAT2(1.0f, 1.0f)},
			{XMFLOAT3(1.0f, -1.0f, 0), XMFLOAT2(1.0f, 1.0f)},
			{XMFLOAT3(-1.0f, 1.0f, 0), XMFLOAT2(0.0f, 0.0f)},
			{XMFLOAT3(1.0f, 1.0f, 0), XMFLOAT2(1.0f, 0.0f)},
		};

		D3D11_TEXTURE2D_DESC FrameDesc;
		m_SharedSurf->GetDesc(&FrameDesc);

		D3D11_SHADER_RESOURCE_VIEW_DESC ShaderDesc;
		ShaderDesc.Format = FrameDesc.Format;
		ShaderDesc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
		ShaderDesc.Texture2D.MostDetailedMip = FrameDesc.MipLevels - 1;
		ShaderDesc.Texture2D.MipLevels = FrameDesc.MipLevels;

		// Create new shader resource view
		ID3D11ShaderResourceView* ShaderResource = nullptr;
		
		HRESULT hr = m_3dDevice->CreateShaderResourceView(m_SharedSurf, &ShaderDesc, &ShaderResource);
		if (FAILED(hr))
		{
			assert(false);
		}

		// Set resources
		UINT Stride = sizeof(VERTEX);
		UINT Offset = 0;
		FLOAT blendFactor[4] = { 0.f, 0.f, 0.f, 0.f };
		m_d3dContext->OMSetBlendState(nullptr, blendFactor, 0xffffffff);
		m_d3dContext->OMSetRenderTargets(1, &m_RTV, nullptr);
		m_d3dContext->VSSetShader(m_VertexShader, nullptr, 0);
		m_d3dContext->PSSetShader(m_PixelShader, nullptr, 0);
		m_d3dContext->PSSetShaderResources(0, 1, &ShaderResource);
		m_d3dContext->PSSetSamplers(0, 1, &m_SamplerLinear);
		m_d3dContext->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

		D3D11_BUFFER_DESC BufferDesc;
		RtlZeroMemory(&BufferDesc, sizeof(BufferDesc));
		BufferDesc.Usage = D3D11_USAGE_DEFAULT;
		BufferDesc.ByteWidth = sizeof(VERTEX) * NUMVERTICES;
		BufferDesc.BindFlags = D3D11_BIND_VERTEX_BUFFER;
		BufferDesc.CPUAccessFlags = 0;
		D3D11_SUBRESOURCE_DATA InitData;
		RtlZeroMemory(&InitData, sizeof(InitData));
		InitData.pSysMem = Vertices;

		ID3D11Buffer* VertexBuffer = nullptr;

		// Create vertex buffer
		hr = m_3dDevice->CreateBuffer(&BufferDesc, &InitData, &VertexBuffer);
		if (FAILED(hr))
		{
			assert(false);
			ShaderResource->Release();
			ShaderResource = nullptr;
		}
		m_d3dContext->IASetVertexBuffers(0, 1, &VertexBuffer, &Stride, &Offset);

		m_d3dContext->Draw(NUMVERTICES, 0);

		VertexBuffer->Release();
		VertexBuffer = nullptr;

		// Release shader resource
		ShaderResource->Release();
		ShaderResource = nullptr;

		DXGI_PRESENT_PARAMETERS presentParameters = { 0 };
		m_SwapChain->Present1(1, 0, &presentParameters);
	}
    /*auto newSize = false;
    {
        auto frame = sender.TryGetNextFrame();
		auto frameContentSize = frame.ContentSize();

        if (frameContentSize.Width != m_lastSize.Width ||
			frameContentSize.Height != m_lastSize.Height)
        {
            // The thing we have been capturing has changed size.
            // We need to resize our swap chain first, then blit the pixels.
            // After we do that, retire the frame and then recreate our frame pool.
            newSize = true;
            m_lastSize = frameContentSize;
            m_swapChain->ResizeBuffers(
                2, 
				static_cast<uint32_t>(m_lastSize.Width),
				static_cast<uint32_t>(m_lastSize.Height),
                static_cast<DXGI_FORMAT>(DirectXPixelFormat::B8G8R8A8UIntNormalized), 
                0);
        }

        {
            auto frameSurface = GetDXGIInterfaceFromObject<ID3D11Texture2D>(frame.Surface());
            
            com_ptr<ID3D11Texture2D> backBuffer;
            check_hresult(m_swapChain->GetBuffer(0, guid_of<ID3D11Texture2D>(), backBuffer.put_void()));

            m_d3dContext->CopyResource(backBuffer.get(), frameSurface.get());
        }
    }

    DXGI_PRESENT_PARAMETERS presentParameters = { 0 };
    m_swapChain->Present1(1, 0, &presentParameters);

    if (newSize)
    {
        m_framePool.Recreate(
            m_device,
            DirectXPixelFormat::B8G8R8A8UIntNormalized,
            2,
            m_lastSize);
    }*/
}

