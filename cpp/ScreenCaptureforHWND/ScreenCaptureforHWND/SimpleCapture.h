#pragma once
#define NUMVERTICES 6
#include "Vertex.h"

class SimpleCapture
{
public:
    SimpleCapture(
        winrt::Windows::Graphics::DirectX::Direct3D11::IDirect3DDevice const& device,
        winrt::Windows::Graphics::Capture::GraphicsCaptureItem const& item,
		HWND const& drawingHandle);
    ~SimpleCapture() { Close(); }

    void StartCapture(HWND drawingHandle);
    winrt::Windows::UI::Composition::ICompositionSurface CreateSurface(
        winrt::Windows::UI::Composition::Compositor const& compositor);

    void Close();

private:
    void OnFrameArrived(
        winrt::Windows::Graphics::Capture::Direct3D11CaptureFramePool const& sender,
        winrt::Windows::Foundation::IInspectable const& args);

    void CheckClosed()
    {
        if (m_closed.load() == true)
        {
            throw winrt::hresult_error(RO_E_CLOSED);
        }
    }

private:
    winrt::Windows::Graphics::Capture::GraphicsCaptureItem m_item{ nullptr };
    winrt::Windows::Graphics::Capture::Direct3D11CaptureFramePool m_framePool{ nullptr };
    winrt::Windows::Graphics::Capture::GraphicsCaptureSession m_session{ nullptr };
    winrt::Windows::Graphics::DirectX::Direct3D11::IDirect3DDevice m_device{ nullptr };
    //winrt::Windows::Graphics::SizeInt32 m_lastSize;

    
    IDXGIFactory2* m_Factory;
    ID3D11VertexShader* m_VertexShader;
    ID3D11PixelShader* m_PixelShader;
    ID3D11InputLayout* m_InputLayout;
    ID3D11BlendState* m_BlendState;
    ID3D11RenderTargetView* m_RTV;
    ID3D11SamplerState* m_SamplerLinear;
    winrt::com_ptr<ID3D11DeviceContext> m_d3dContext{ nullptr };
    winrt::com_ptr<ID3D11Device> m_3dDevice{ nullptr };
    IDXGISwapChain1* m_SwapChain;
    ID3D11Texture2D* m_SharedSurf;
    
    HWND m_WindowHandle;

    HRESULT CreateSharedSurf();
    HRESULT SetViewPort(UINT Width, UINT Height);
    HRESULT InitShaders();
    HRESULT MakeRTV();
    //winrt::com_ptr<IDXGISwapChain1> m_swapChain{ nullptr };
    //winrt::com_ptr<ID3D11DeviceContext> m_d3dContext{ nullptr };

    std::atomic<bool> m_closed = false;
	winrt::Windows::Graphics::Capture::Direct3D11CaptureFramePool::FrameArrived_revoker m_frameArrived;
};