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

// main.cpp : Defines the entry point for the application.

#include "stdafx.h"
#include "AdvancedColorImages.h"
#include "WinComp.h"

using namespace winrt;
using namespace Windows::UI;
using namespace Windows::UI::Composition;
using namespace Windows::UI::Composition::Desktop;
using namespace Windows::UI::Input;


#define MAX_LOADSTRING 100

// Global Variables:
HINSTANCE	hInst;                               // current instance
WCHAR		szTitle[MAX_LOADSTRING];             // The title bar text
WCHAR		szWindowClass[MAX_LOADSTRING];       // the main window class name
HWND		m_childHWnd;
WinComp* m_winComp;

// Forward declarations of functions included in this code module:
ATOM                MyRegisterClass(HINSTANCE hInstance);
BOOL                InitInstance(HINSTANCE, int);
LRESULT CALLBACK    WndProc(HWND, UINT, WPARAM, LPARAM);
INT_PTR CALLBACK    About(HWND, UINT, WPARAM, LPARAM);

int APIENTRY wWinMain(_In_ HINSTANCE hInstance,
	_In_opt_ HINSTANCE hPrevInstance,
	_In_ LPWSTR    lpCmdLine,
	_In_ int       nCmdShow)
{


	UNREFERENCED_PARAMETER(hPrevInstance);
	UNREFERENCED_PARAMETER(lpCmdLine);

	winrt::init_apartment(winrt::apartment_type::single_threaded);

	// Initialize global strings
	LoadStringW(hInstance, IDS_APP_TITLE, szTitle, MAX_LOADSTRING);
	LoadStringW(hInstance, IDC_ADVANCEDCOLORIMAGES, szWindowClass, MAX_LOADSTRING);
	MyRegisterClass(hInstance);
	// Perform application initialization:
	if (!InitInstance(hInstance, nCmdShow))
	{
		return FALSE;
	}

	HACCEL hAccelTable = LoadAccelerators(hInstance, MAKEINTRESOURCE(IDC_ADVANCEDCOLORIMAGES));

	MSG msg;

	// Main message loop:
	while (GetMessage(&msg, nullptr, 0, 0))
	{
		if (!TranslateAccelerator(msg.hwnd, hAccelTable, &msg))
		{
			TranslateMessage(&msg);
			DispatchMessage(&msg);
		}
	}
	return (int)msg.wParam;
}


//
//  FUNCTION: MyRegisterClass()
//
//  PURPOSE: Registers the window class.
//
ATOM MyRegisterClass(HINSTANCE hInstance)
{
	WNDCLASSEXW wcex;

	wcex.cbSize = sizeof(WNDCLASSEX);

	wcex.style = CS_HREDRAW | CS_VREDRAW;
	wcex.lpfnWndProc = WndProc;
	wcex.cbClsExtra = 0;
	wcex.cbWndExtra = 0;
	wcex.hInstance = hInstance;
	wcex.hIcon = LoadIcon(hInstance, MAKEINTRESOURCE(IDI_ADVANCEDCOLORIMAGES));
	wcex.hCursor = LoadCursor(nullptr, IDC_ARROW);
	wcex.hbrBackground = (HBRUSH)(COLOR_WINDOW + 1);
	wcex.lpszMenuName = MAKEINTRESOURCEW(IDC_ADVANCEDCOLORIMAGES);
	wcex.lpszClassName = szWindowClass;
	wcex.hIconSm = LoadIcon(wcex.hInstance, MAKEINTRESOURCE(IDI_SMALL));

	return RegisterClassExW(&wcex);
}

//
//   FUNCTION: InitInstance(HINSTANCE, int)
//
//   PURPOSE: Saves instance handle and creates main window
//
//   COMMENTS:
//
//        In this function, we save the instance handle in a global variable and
//        create and display the main program window.
//
BOOL InitInstance(HINSTANCE hInstance, int nCmdShow)
{
	hInst = hInstance; // Store instance handle in our global variable

	HWND hWndParent = CreateWindowW(szWindowClass, szTitle, WS_OVERLAPPEDWINDOW,
		CW_USEDEFAULT, 0, CW_USEDEFAULT, 0, nullptr, nullptr, hInstance, nullptr);

	//SetWindowPos(hWndParent, HWND_TOPMOST, 0, 0, 300, 300, SWP_SHOWWINDOW);
	if (!hWndParent)
	{
		return FALSE;
	}

	//Create the child HWND with the same size as the parent HWND, so it fills up the entire space. 

	RECT rect;
	::GetWindowRect(hWndParent, &rect);

	m_childHWnd = CreateWindowW(szWindowClass, szTitle, WS_CHILD,
		0, 0, rect.right - rect.left, rect.bottom - rect.top,
		hWndParent, nullptr, hInstance, nullptr);

	if (!m_childHWnd)
	{
		return FALSE;
	}
	m_winComp = winrt::make_self<WinComp>().detach();
	// Ensure that the DispatcherQueue is initialized. This is required by the Compositor. 
	auto controller = m_winComp->EnsureDispatcherQueue();

	ShowWindow(hWndParent, nCmdShow);
	UpdateWindow(hWndParent);
	ShowWindow(m_childHWnd, nCmdShow);
	UpdateWindow(m_childHWnd);
	m_winComp->Initialize(m_childHWnd);
	m_winComp->PrepareVisuals();
	m_winComp->ConfigureInteraction();
	m_winComp->LoadImageFromFileName(L"hdr-image.jxr");
	m_winComp->UpdateViewPort(true);
	return TRUE;
}

bool LocateImageFile(HWND hWnd, LPWSTR pszFileName, DWORD cchFileName)
{
	pszFileName[0] = L'\0';

	OPENFILENAME ofn;
	ZeroMemory(&ofn, sizeof(ofn));

	ofn.lStructSize = sizeof(ofn);
	ofn.hwndOwner = hWnd;
	ofn.lpstrFilter = L"All Image Files\0"	L"*.bmp;*.dib;*.wdp;*.mdp;*.hdp;*.gif;*.png;*.jpg;*.jpeg;*.tif;*.ico;*.jxr\0"
		L"JPEG File Interchange Format\0"	L"*.jpg;*.jpeg\0"
		L"JPEG XR Extented Range Format\0"	L"*.jxr\0"
		L"Windows Bitmap\0"					L"*.bmp;*.dib\0"
		L"High Definition Photo\0"			L"*.wdp;*.mdp;*.hdp\0"
		L"Graphics Interchange Format\0"	L"*.gif\0"
		L"Portable Network Graphics\0"		L"*.png\0"
		L"Tiff File\0"						L"*.tif\0"
		L"Icon\0"							L"*.ico\0"
		L"All Files\0"						L"*.*\0"
		L"\0";
	ofn.lpstrFile = pszFileName;
	ofn.nMaxFile = cchFileName;
	ofn.lpstrTitle = L"Open Image";
	ofn.Flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST;

	// Display the Open dialog box. 
	return (GetOpenFileName(&ofn) == TRUE);
}

//
//  FUNCTION: WndProc(HWND, UINT, WPARAM, LPARAM)
//
//  PURPOSE:  Processes messages for the main window.
//
//  WM_COMMAND  - process the application menu
//  WM_PAINT    - Paint the main window
//  WM_DESTROY  - post a quit message and return
//
//
LRESULT CALLBACK WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
	switch (message)
	{
	case WM_COMMAND:
	{
		int wmId = LOWORD(wParam);
		// Parse the menu selections:
		switch (wmId)
		{

		case IDM_FILE:
			WCHAR szFileName[MAX_PATH];

			if (LocateImageFile(hWnd, szFileName, ARRAYSIZE(szFileName)))
			{
				m_winComp->LoadImageFromFileName(szFileName);
			}
			else
			{
				MessageBox(hWnd, L"Failed to load image, select a new one.", L"Application Error", MB_ICONEXCLAMATION | MB_OK);
			}

			break;

		case IDM_ABOUT:
			DialogBox(hInst, MAKEINTRESOURCE(IDD_ABOUTBOX), hWnd, About);
			break;
		case IDM_EXIT:
			DestroyWindow(hWnd);
			break;
		default:
			return DefWindowProc(hWnd, message, wParam, lParam);
		}
	}
	break;
	case WM_POINTERDOWN:
	{
		//Redirect input events to the InteractionTracker for input events.
		PointerPoint pp = PointerPoint::GetCurrentPoint(GET_POINTERID_WPARAM(wParam));
		m_winComp->TryRedirectForManipulation(pp);
		break;
	}


	case WM_PAINT:
	{
		PAINTSTRUCT ps;
		HDC hdc = BeginPaint(hWnd, &ps);
		EndPaint(hWnd, &ps);
	}
	break;

	case WM_SIZE:
	{
		//Update the child HWND to the new size of the parent HWnd.
		RECT windowRect;
		::GetWindowRect(hWnd, &windowRect);
		::SetWindowPos(m_childHWnd, HWND_TOP, 0, 0, windowRect.right - windowRect.left, windowRect.bottom - windowRect.top, SWP_NOZORDER);
		if (m_winComp != nullptr)
			m_winComp->UpdateViewPort(true);
	}
	break;
	case WM_DESTROY:
		PostQuitMessage(0);
		break;
	default:
		return DefWindowProc(hWnd, message, wParam, lParam);
	}
	return 0;
}

// Message handler for about box.
INT_PTR CALLBACK About(HWND hDlg, UINT message, WPARAM wParam, LPARAM lParam)
{
	UNREFERENCED_PARAMETER(lParam);
	switch (message)
	{
	case WM_INITDIALOG:
		return (INT_PTR)TRUE;

	case WM_COMMAND:
		if (LOWORD(wParam) == IDOK || LOWORD(wParam) == IDCANCEL)
		{
			EndDialog(hDlg, LOWORD(wParam));
			return (INT_PTR)TRUE;
		}
		break;
	}
	return (INT_PTR)FALSE;
}
