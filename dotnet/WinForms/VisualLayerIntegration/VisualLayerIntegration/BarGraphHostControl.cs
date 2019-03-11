//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using BarGraphUtility;
using System.Collections.Generic;
using System.Drawing;

namespace VisualLayerIntegration
{
    class BarGraphHostControl : CompositionHost
    {
        private BarGraph currentGraph;

        private double currentDpiX = 96.0;
        private double currentDpiY = 96.0;

        public BarGraphHostControl() : base()
        {
            MouseMoved += BarGraphHostControl_MouseMoved;
        }

        /*
        * Handle mouse movement
        */
        private void BarGraphHostControl_MouseMoved(object sender, HwndMouseEventArgs e)
        {
            //Adjust light position
            if (currentGraph != null)
            {
                Point adjustedTopLeft = GetControlPointInDIP();

                //Get point relative to control
                Point relativePoint = new Point(e.point.X - adjustedTopLeft.X, e.point.Y - adjustedTopLeft.Y);

                //Update light position
                currentGraph.UpdateLight(relativePoint);
            }
        }

        private Point GetControlPointInDIP()
        {
            //Get bounds of hwnd host control
            Point controlTopLeft = PointToScreen(new Point(0, 0));  //top left of control relative to screen
            //Convert screen coord to DIP
            var adjustedX = (int)(controlTopLeft.X / (currentDpiX / 96.0));
            var adjustedY = (int)(controlTopLeft.Y / (currentDpiY / 96.0));
            return new Point(adjustedX, adjustedY);
        }

        /*
        * Handle Composition tree creation and updates
        */
        public void UpdateGraph(Customer customer)
        {
            var graphTitle = customer.FirstName + " Investment History";
            var xAxisTitle = "Investment #";
            var yAxisTitle = "# Shares of Stock";

            // If graph already exists update values. Else create new graph.
            if (containerVisual.Children.Count > 0 && currentGraph != null)
            {
                currentGraph.UpdateGraphData(graphTitle, xAxisTitle, yAxisTitle, customer.Data);
            }
            else
            {
                BarGraph graph = new BarGraph(compositor, hwnd, graphTitle, xAxisTitle, yAxisTitle,
                    (float)Width, (float)Height, currentDpiX, currentDpiY, customer.Data,   //TODO update DPI variable
                    true, BarGraph.GraphBarStyle.PerBarLinearGradient,
                    new List<Windows.UI.Color> { Windows.UI.Color.FromArgb(255, 0, 85, 255), Windows.UI.Color.FromArgb(255, 0, 130, 4) });

                containerVisual.Children.InsertAtTop(graph.GraphRoot);
                currentGraph = graph;
            }
        }

        public void UpdateSize(double currentDpi)
        {
            if (Width > 0 && currentGraph != null)
            {
                currentGraph.UpdateSize(currentDpi, currentDpi, Width, Height);
            }
        }
    }
}

