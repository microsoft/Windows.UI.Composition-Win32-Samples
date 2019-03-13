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

#include "stdafx.h"
#include "TileDrawingManager.h"
#include <winrt/Windows.UI.Composition.Interactions.h>

using namespace winrt;
using namespace winrt::impl;
using namespace Windows::System;
using namespace Windows::UI;
using namespace Windows::UI::Composition;
using namespace Windows::UI::Composition::Interactions;
using namespace Windows::UI::Composition::Desktop;
using namespace Windows::UI::Input;
using namespace Windows::Graphics;
using namespace Windows::Graphics::Display;
using namespace Windows::Graphics::DirectX;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Numerics;


class WinComp : public implements<WinComp, IInteractionTrackerOwner, no_weak_ref>
{

#pragma region Singleton Pattern
public:
	~WinComp();
	static WinComp* GetInstance();
private:
	WinComp();
	static WinComp* s_instance;
#pragma endregion

public:
	void Initialize(HWND hwnd);
	void PrepareVisuals();
	DispatcherQueueController EnsureDispatcherQueue();
	void UpdateViewPort(boolean changeContentVisual);
	void ConfigureInteraction();
	void TryRedirectForManipulation(PointerPoint pp);
	void TryUpdatePositionBy(float3 const& amount);


	//interaction tracker owner implementation
	void InertiaStateEntered(InteractionTracker sender, InteractionTrackerInertiaStateEnteredArgs args);
	void InteractingStateEntered(InteractionTracker sender, InteractionTrackerInteractingStateEnteredArgs args);
	void RequestIgnored(InteractionTracker sender, InteractionTrackerRequestIgnoredArgs args);
	void ValuesChanged(InteractionTracker sender, InteractionTrackerValuesChangedArgs args);
	void CustomAnimationStateEntered(InteractionTracker sender, InteractionTrackerCustomAnimationStateEnteredArgs args);
	void IdleStateEntered(InteractionTracker sender, InteractionTrackerIdleStateEnteredArgs args);

private:

	DesktopWindowTarget CreateDesktopWindowTarget(Compositor const& compositor, HWND window);
	void AddD2DVisual(VisualCollection const& visuals, float x, float y);
	void StartAnimation(CompositionSurfaceBrush brush);
	Size GetWindowSize();

	//member variables
	Compositor					m_compositor{ nullptr };
	VisualInteractionSource		m_interactionSource{ nullptr };
	SpriteVisual				m_viewportVisual{ nullptr };
	SpriteVisual				m_contentVisual{ nullptr };
	InteractionTracker			m_tracker{ nullptr };
	DesktopWindowTarget			m_target{ nullptr };
	HWND						m_window = nullptr;

	//animation member variables
	ExpressionAnimation			m_moveSurfaceExpressionAnimation{ nullptr };
	ExpressionAnimation			m_moveSurfaceUpDownExpressionAnimation{ nullptr };
	ExpressionAnimation			m_scaleSurfaceUpDownExpressionAnimation{ nullptr };
	ExpressionAnimation			m_animateMatrix{ nullptr };
	CompositionPropertySet		m_animatingPropset{ nullptr };

	TileDrawingManager			m_TileDrawingManager;
	float						m_lastTrackerScale = 1.0f;
	float3						m_lastTrackerPosition{ 0.0f,0.0f,0.0f };
	bool						m_zooming;

};

