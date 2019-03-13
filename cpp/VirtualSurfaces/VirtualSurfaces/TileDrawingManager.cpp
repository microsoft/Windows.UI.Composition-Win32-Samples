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
}

void TileDrawingManager::setRenderer(DirectXTileRenderer* renderer) {
	m_currentRenderer = renderer;
};

DirectXTileRenderer* TileDrawingManager::getRenderer()
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

	int requiredTopTileRow = max((int)m_currentPosition.y / TILESIZE - DrawAheadTileCount, 0);
	int requiredBottomTileRow = (int)(m_currentPosition.y + m_viewPortSize.Height) / TILESIZE + DrawAheadTileCount;
	int requiredLeftTileColumn = max((int)m_currentPosition.x / TILESIZE - DrawAheadTileCount, 0);
	int requiredRightTileColumn = (int)(m_currentPosition.x + m_viewPortSize.Width) / TILESIZE + DrawAheadTileCount;

	currentTopLeftTileRow = (int)m_currentPosition.y / TILESIZE;
	currentTopLeftTileColumn = (int)m_currentPosition.x / TILESIZE;

	int numberOfRows = (drawnTopTileRow - requiredTopTileRow );
	int numberOfColumns = (drawnRightTileColumn-drawnLeftTileColumn )+1;
	
	if(numberOfRows>0 && numberOfColumns>0)
	{
		DrawTileRange(drawnLeftTileColumn, requiredTopTileRow, numberOfColumns, numberOfRows);
		stateUpdate = true;
	}

	numberOfRows = (requiredBottomTileRow - drawnBottomTileRow);
	numberOfColumns = (drawnRightTileColumn - drawnLeftTileColumn) + 1;

	if (numberOfRows > 0 && numberOfColumns > 0)
	{
		DrawTileRange(drawnLeftTileColumn, drawnBottomTileRow+1, numberOfColumns, numberOfRows);
		stateUpdate = true;
	}

	drawnTopTileRow = min(requiredTopTileRow, drawnTopTileRow);
	drawnBottomTileRow = max(requiredBottomTileRow, drawnBottomTileRow);

	numberOfRows = ( drawnBottomTileRow - drawnTopTileRow)+1;
	numberOfColumns = (drawnLeftTileColumn - requiredLeftTileColumn) ;

	if (numberOfRows > 0 && numberOfColumns > 0)
	{
		DrawTileRange(requiredLeftTileColumn, drawnTopTileRow, numberOfColumns, numberOfRows);
		stateUpdate = true;
	}

	numberOfRows = (drawnBottomTileRow - drawnTopTileRow)+1;
	numberOfColumns = (requiredRightTileColumn - drawnRightTileColumn) ;

	if (numberOfRows > 0 && numberOfColumns > 0)
	{
		DrawTileRange(drawnRightTileColumn + 1, drawnTopTileRow, numberOfColumns, numberOfRows);
		stateUpdate = true;
	}

	drawnLeftTileColumn = min(requiredLeftTileColumn, drawnLeftTileColumn);
	drawnRightTileColumn = max(requiredRightTileColumn, drawnRightTileColumn);

	//
	// Trimming the tiles that are not visible on screen
	//

	if (stateUpdate)
	{
		Trim(requiredLeftTileColumn, requiredTopTileRow, requiredRightTileColumn, requiredBottomTileRow);
	}
}


//
//  FUNCTION: GetTileForCoordinates
//
//  PURPOSE: Helper function that gets the tile object for a particular row and column combination.
//
void TileDrawingManager::UpdateViewportSize(Size newSize)
{
	m_viewPortSize = newSize;
	horizontalVisibleTileCount = (int)ceil(newSize.Width / TILESIZE);
	verticalVisibleTileCount = (int)ceil(newSize.Height / TILESIZE);
	DrawVisibleTilesByRange();
}

//
//  FUNCTION: GetTileForCoordinates
//
//  PURPOSE: Helper function that gets the tile object for a particular row and column combination.
//
Tile TileDrawingManager::GetTileForCoordinates(int row, int column)
{
	int x = column * TILESIZE;
	int y = row * TILESIZE;
	Rect rect((float)x, (float)y, TILESIZE, TILESIZE);
	return Tile{ rect,row,column };
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
//  PURPOSE: Converts the tile co-ordinates into a list of Tile Objects that can be sent to the renderer.
//
list<Tile> TileDrawingManager::GetTilesForRange(int tileStartColumn, int tileStartRow, int numColumns, int numRows)
{
	list<Tile> returnTiles;
	//get Tile objects for each tile that needs to be rendered.
	for (int i = tileStartColumn; i < tileStartColumn + numColumns; i++) {
		for (int j = tileStartRow; j < tileStartRow + numRows; j++) {

			returnTiles.push_back(GetTileForCoordinates(j, i));
		}
	}
	return returnTiles;
}


void TileDrawingManager::DrawTileRange(int tileStartColumn, int tileStartRow, int numColumns, int numRows) 
{
	m_currentRenderer->DrawTileRange(GetRectForTileRange(tileStartColumn, tileStartRow, numColumns, numRows),
		GetTilesForRange(tileStartColumn, tileStartRow, numColumns, numRows));
}


//
//  FUNCTION: DrawVisibleTilesByRange
//
//  PURPOSE: This function combines all the tiles into a single call, so the rendering is faster as opposed to calling BeginDraw on each tile.
//
void TileDrawingManager::DrawVisibleTilesByRange()
{
	DrawTileRange(0, 0, horizontalVisibleTileCount + DrawAheadTileCount, verticalVisibleTileCount + DrawAheadTileCount);

	drawnRightTileColumn = horizontalVisibleTileCount - 1 + DrawAheadTileCount;
	drawnBottomTileRow = verticalVisibleTileCount - 1 + DrawAheadTileCount;

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

	drawnLeftTileColumn = leftColumn;
	drawnRightTileColumn = rightColumn;
	drawnTopTileRow = topRow;
	drawnBottomTileRow = bottomRow;
}
