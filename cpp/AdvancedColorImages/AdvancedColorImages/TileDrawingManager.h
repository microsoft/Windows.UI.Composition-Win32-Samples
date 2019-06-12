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

#include "DirectXTileRenderer.h"

using namespace std;
using namespace winrt;
using namespace Windows::Foundation::Numerics;
using namespace Windows::Foundation;

class TileDrawingManager
{
public:
	TileDrawingManager();
	~TileDrawingManager();
	void UpdateVisibleRegion(float3 currentPosition);
	void UpdateViewportSize(Size newSize);
	void SetRenderer(DirectXTileRenderer* renderer);
	DirectXTileRenderer* GetRenderer();

	const static int TILESIZE = 100;
	const static int MAXSURFACESIZE = TILESIZE * 10000;
	const static int DRAWAHEADTILECOUNT = 0; //Number of tiles to draw ahead 

private:

	list<Tile> GetTilesForRange(int tileStartColumn, int tileStartRow, int numColumns, int numRows);
	void DrawVisibleTilesByRange();
	Rect GetRectForTileRange(int tileStartColumn, int tileStartRow, int numColumns, int numRows);
	Rect GetClipRectForRange(int tileStartColumn, int tileStartRow, int numColumns, int numRows);
	void Trim(int leftColumn, int topRow, int rightColumn, int bottomRow);
	void DrawTileRange(int tileStartColumn, int tileStartRow, int numColumns, int numRows);

	//member variables

	//These variables reflect the current state of the surface and which tiles are screen.
	//Helps us figure out the new set of tiles that need to be rendered when there's change because of manipulation
	//or viewport size changes.
	int                     m_drawnTopTileRow = 0;//Keeps track of the top tile row that is currently drawn
	int                     m_drawnBottomTileRow = 0;//Keeps track of the bottom tile row that is currently drawn
	int                     m_drawnLeftTileColumn = 0;//Keeps track of the left tile colum that is currently drawn
	int                     m_drawnRightTileColumn = 0;//Keeps track of the right rile column that is currently drawn

	int                     m_horizontalVisibleTileCount = 0;//Number of horizonal tiles visible.
	int                     m_verticalVisibleTileCount = 0;//Number of vertical tiles visible.

	Size                    m_viewPortSize;//Size of the viewport.
	float3                  m_currentPosition;//Current position
	int                     m_currentTopLeftTileRow = 0;//Row number of the top left tile that is currently visible
	int                     m_currentTopLeftTileColumn = 0;//Column number of the top left tile that is currently visible.

	DirectXTileRenderer* m_currentRenderer;
};
