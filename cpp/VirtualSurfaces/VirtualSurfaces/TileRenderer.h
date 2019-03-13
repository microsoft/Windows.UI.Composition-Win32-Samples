#pragma once
#include "stdafx.h";

using namespace winrt;
using namespace Windows::Foundation;

class TileRenderer
{
public:
	virtual void DrawTile(Rect rect, int tileRow, int tileColumn);
	virtual void Trim(Rect trimRect);
};
