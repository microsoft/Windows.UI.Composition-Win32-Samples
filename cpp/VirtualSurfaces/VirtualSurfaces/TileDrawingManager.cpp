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
#include "TileDrawingManager.h"


TileDrawingManager::TileDrawingManager()
{	

}

TileDrawingManager::~TileDrawingManager()
{
	delete m_currentRenderer;
}

void TileDrawingManager::SetRenderer(DirectXTileRenderer* renderer) {
	m_currentRenderer = renderer;
};

DirectXTileRenderer* TileDrawingManager::GetRenderer()
{
	return m_currentRenderer;
}

//
//  FUNCTION: UpdateVisibleRegion
//
//  PURPOSE: More unloaded surface is now visible on screen because of some event like manipulations(zoom, pan, etc.). This method, figures
//	out the new areas that need to be rendered and fires the draw calls. This is the core of the tile drawing logic
//
void TileDrawingManager::UpdateVisibleRegion(float3 currentPosition)
{
	m_currentPosition = currentPosition;
	bool stateUpdate = false;

	int requiredTopTileRow = max((int)m_currentPosition.y / TILESIZE - DRAWAHEADTILECOUNT, 0);
	int requiredBottomTileRow = (int)(m_currentPosition.y + m_viewPortSize.Height) / TILESIZE + DRAWAHEADTILECOUNT;
	int requiredLeftTileColumn = max((int)m_currentPosition.x / TILESIZE - DRAWAHEADTILECOUNT, 0);
	int requiredRightTileColumn = (int)(m_currentPosition.x + m_viewPortSize.Width) / TILESIZE + DRAWAHEADTILECOUNT;

	m_currentTopLeftTileRow = (int)m_currentPosition.y / TILESIZE;
	m_currentTopLeftTileColumn = (int)m_currentPosition.x / TILESIZE;
	
	//Draws the tiles that are required above the drawn top row.
	int numberOfRows = (m_drawnTopTileRow - requiredTopTileRow );
	int numberOfColumns = (m_drawnRightTileColumn-m_drawnLeftTileColumn )+1;
	if(numberOfRows>0 && numberOfColumns>0)
	{
		DrawTileRange(m_drawnLeftTileColumn, requiredTopTileRow, numberOfColumns, numberOfRows);
		stateUpdate = true;
	}

	//Draws the tiles that are required below the drawn bottom row.
	numberOfRows = (requiredBottomTileRow - m_drawnBottomTileRow);
	numberOfColumns = (m_drawnRightTileColumn - m_drawnLeftTileColumn) + 1;
	if (numberOfRows > 0 && numberOfColumns > 0)
	{
		DrawTileRange(m_drawnLeftTileColumn, m_drawnBottomTileRow+1, numberOfColumns, numberOfRows);
		stateUpdate = true;
	}

	//Update the current drawn top tile row and current drawn bottom tile row.
	m_drawnTopTileRow = min(requiredTopTileRow, m_drawnTopTileRow);
	m_drawnBottomTileRow = max(requiredBottomTileRow, m_drawnBottomTileRow);

	//Draws the tiles that are required to the left of the drawn columns.
	numberOfRows = ( m_drawnBottomTileRow - m_drawnTopTileRow)+1;
	numberOfColumns = (m_drawnLeftTileColumn - requiredLeftTileColumn) ;
	if (numberOfRows > 0 && numberOfColumns > 0)
	{
		DrawTileRange(requiredLeftTileColumn, m_drawnTopTileRow, numberOfColumns, numberOfRows);
		stateUpdate = true;
	}

	//Draws the tiles that are required to the right of the drawn columns.
	numberOfRows = (m_drawnBottomTileRow - m_drawnTopTileRow)+1;
	numberOfColumns = (requiredRightTileColumn - m_drawnRightTileColumn) ;
	if (numberOfRows > 0 && numberOfColumns > 0)
	{
		DrawTileRange(m_drawnRightTileColumn + 1, m_drawnTopTileRow, numberOfColumns, numberOfRows);
		stateUpdate = true;
	}

	//Update the current drawn left tile columns and current drawn right tile columns.
	m_drawnLeftTileColumn = min(requiredLeftTileColumn, m_drawnLeftTileColumn);
	m_drawnRightTileColumn = max(requiredRightTileColumn, m_drawnRightTileColumn);

	// Trimming the tiles that are not visible on screen
	if (stateUpdate)
	{
		Trim(requiredLeftTileColumn, requiredTopTileRow, requiredRightTileColumn, requiredBottomTileRow);
	}
}

//
//  FUNCTION: UpdateViewportSize
//
//  PURPOSE: Updates the Viewport Size of the application.
//
void TileDrawingManager::UpdateViewportSize(Size newSize)
{
	m_viewPortSize = newSize;
	//Using the ceil operator to make sure the Virtual Surfaces is loaded with tiles that occupy the entirity of the viewport
	//not leaving any empty areas on it.
	m_horizontalVisibleTileCount = (int)ceil(newSize.Width / TILESIZE);
	m_verticalVisibleTileCount = (int)ceil(newSize.Height / TILESIZE);
	DrawVisibleTilesByRange();
}

//
//  FUNCTION: GetRectForTileRange
//
//  PURPOSE: Gets the rect that needs to be updated for this range of tiles.
//
Rect TileDrawingManager::GetRectForTileRange(int tileStartColumn, int tileStartRow, int numColumns, int numRows)
{
	int x = tileStartColumn * TILESIZE;
	int y = tileStartRow * TILESIZE;
	return Rect((float)x, (float)y, (float)(numColumns * TILESIZE), (float)(numRows * TILESIZE));
}

//
//  FUNCTION: GetTilesForRange
//
//  PURPOSE: Converts the tile coordinates into a list of Tile Objects that can be sent to the renderer.
//
list<Tile> TileDrawingManager::GetTilesForRange(int tileStartColumn, int tileStartRow, int numColumns, int numRows)
{
	list<Tile> returnTiles;
	//get Tile objects for each tile that needs to be rendered.
	for (int i = tileStartColumn; i < tileStartColumn + numColumns; i++) {
		for (int j = tileStartRow; j < tileStartRow + numRows; j++) {
			returnTiles.push_back(Tile(j, i,TILESIZE));
		}
	}
	return returnTiles;
}


void TileDrawingManager::DrawTileRange(int tileStartColumn, int tileStartRow, int numColumns, int numRows) 
{
	m_currentRenderer->DrawTileRange(GetRectForTileRange(tileStartColumn, tileStartRow, numColumns, numRows), GetTilesForRange(tileStartColumn, tileStartRow, numColumns, numRows));
}


//
//  FUNCTION: DrawVisibleTilesByRange
//
//  PURPOSE: This function combines all the tiles into a single call, so the rendering is faster as opposed to calling BeginDraw on each tile.
//
void TileDrawingManager::DrawVisibleTilesByRange()
{
	//The DRAWAHEADTILECOUNT draws tiles that the configured number of tiles outside the viewport to make sure the user doesnt see a lot 
	//of empty areas when scrolling.
	DrawTileRange(0, 0, m_horizontalVisibleTileCount + DRAWAHEADTILECOUNT, m_verticalVisibleTileCount + DRAWAHEADTILECOUNT);

	//update the tiles that are already drawn, so only the new tiles will have to be rendered when panning.
	m_drawnRightTileColumn = m_horizontalVisibleTileCount - 1 + DRAWAHEADTILECOUNT;
	m_drawnBottomTileRow = m_verticalVisibleTileCount - 1 + DRAWAHEADTILECOUNT;

}

//
//  FUNCTION: Trim()
//
//  PURPOSE: Trims the tiles that are outside these co-ordinates. So only the contents that are visible are rendered, to save on memory.
//
void TileDrawingManager::Trim(int leftColumn, int topRow, int rightColumn, int bottomRow)
{
	auto trimRect = GetRectForTileRange(
		leftColumn,
		topRow,
		rightColumn - leftColumn + 1,
		bottomRow - topRow + 1);

	m_currentRenderer->Trim(trimRect);

	m_drawnLeftTileColumn = leftColumn;
	m_drawnRightTileColumn = rightColumn;
	m_drawnTopTileRow = topRow;
	m_drawnBottomTileRow = bottomRow;
}
