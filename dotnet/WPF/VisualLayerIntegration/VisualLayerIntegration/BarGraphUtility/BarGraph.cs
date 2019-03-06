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

using Windows.UI;
using Windows.UI.Composition;

using SharpDX;
using SharpDX.DirectWrite;
using SharpDX.Direct2D1;
using TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode;
using SharpDX.DXGI;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BarGraphUtility
{
    class BarGraph
    {
        private float[] graphData;

        private Compositor compositor;
        private IntPtr hwnd;

        private float graphWidth, graphHeight;
        private float graphTextWidth, graphTextHeight;
        private float shapeGraphContainerHeight, shapeGraphContainerWidth, shapeGraphOffsetY, shapeGraphOffsetX;
        private float barWidth, barSpacing;
        private float maxBarValue;

        private GraphBarStyle graphBarStyle;
        private List<Windows.UI.Color> graphBarColors;

        private Hashtable barValueMap;

        private WindowRenderTarget textRenderTarget;
        private SolidColorBrush textSceneColorBrush;
        private TextFormat textFormatTitle;
        private TextFormat textFormatHorizontal;
        private TextFormat textFormatVertical;

        private ShapeVisual shapeContainer;
        private CompositionLineGeometry xAxisLine;
        private CompositionLineGeometry yAxisLine;
        private ContainerVisual mainContainer;

        private int textRectWidth;
        private int textRectHeight;
        private static SharpDX.Mathematics.Interop.RawColor4 black = new SharpDX.Mathematics.Interop.RawColor4(0, 0, 0, 255);
        private static SharpDX.Mathematics.Interop.RawColor4 white = new SharpDX.Mathematics.Interop.RawColor4(255, 255, 255, 255);

        private static float textSize = 20.0f;

        private AmbientLight ambientLight;
        private SpotLight barOutlineLight;
        private PointLight barLight;

        #region public setters
        public string Title { get; set; }
        public string XAxisLabel { get; set; }
        public string YAxisLabel { get; set; }
        public ContainerVisual BarRoot { get; }
        public ContainerVisual GraphRoot { get; }
        #endregion

        public enum GraphBarStyle
        {
            Single = 0,
            Random = 1,
            PerBarLinearGradient = 3,
            AmbientAnimatingPerBarLinearGradient = 4
        }

        // Constructor for bar graph.
        // For now, only does single bars, no grouping.
        // As of 12/6 to insert graph, call the constructor then use barGraph.Root to get the container to parent.
        public BarGraph(Compositor compositor, IntPtr hwnd, string title, string xAxisLabel,     // required parameters
            string yAxisLabel, float width, float height, double dpiX, double dpiY, float[] data,// required parameters
            bool AnimationsOn = true, GraphBarStyle graphBarStyle = GraphBarStyle.Single,        // optional parameters
            List<Windows.UI.Color> barColors = null)
        {
            this.compositor = compositor;
            this.hwnd = hwnd;
            this.graphWidth = (float)(width * dpiX / 96.0);
            this.graphHeight = (float)(height * dpiY / 96.0);

            this.graphTextWidth = (float)(width);
            this.graphTextHeight = (float)(height);

            this.graphData = data;

            Title = title;
            XAxisLabel = xAxisLabel;
            YAxisLabel = yAxisLabel;

            this.graphBarStyle = graphBarStyle;

            if (barColors != null)
            {
                graphBarColors = barColors;
            }
            else
            {
                graphBarColors = new List<Windows.UI.Color>() { Colors.Blue };
            }

            // Configure options for text.
            var Factory2D = new SharpDX.Direct2D1.Factory();

            var properties = new HwndRenderTargetProperties();
            properties.Hwnd = this.hwnd;
            properties.PixelSize = new SharpDX.Size2((int)(width * dpiX / 96.0), (int)(width * dpiY / 96.0));
            properties.PresentOptions = PresentOptions.None;

            textRenderTarget = new WindowRenderTarget(Factory2D, new RenderTargetProperties(new PixelFormat(Format.Unknown, SharpDX.Direct2D1.AlphaMode.Premultiplied)), properties);
            textRenderTarget.DotsPerInch = new Size2F((float)dpiX, (float)dpiY);
            textRenderTarget.Resize(new Size2((int)(width * dpiX / 96.0), (int)(width * dpiY / 96.0)));

            // Generate graph structure.
            var graphRoot = GenerateGraphStructure();
            GraphRoot = graphRoot;

            BarRoot = this.compositor.CreateContainerVisual();
            GraphRoot.Children.InsertAtBottom(BarRoot);

            // If data has been provided, initialize bars and animations; otherwise, leave graph empty.
            if (graphData.Length > 0)
            {
                barValueMap = new Hashtable();
                var bars = CreateBars(graphData);
                AddBarsToTree(bars);
            }
        }

        private void UpdateSizeAndPositions()
        {
            shapeGraphOffsetY = graphHeight * 1 / 15;
            shapeGraphOffsetX = graphWidth * 1 / 15;
            shapeGraphContainerHeight = graphHeight - shapeGraphOffsetY * 2;
            shapeGraphContainerWidth = graphWidth - shapeGraphOffsetX * 2;
            textRectWidth = (int)shapeGraphContainerWidth;
            textRectHeight = (int)shapeGraphOffsetY;

            graphTextWidth = (float)(graphWidth);
            graphTextHeight = (float)(graphHeight);

            mainContainer.Offset = new System.Numerics.Vector3(shapeGraphOffsetX, shapeGraphOffsetY, 0);

            barWidth = ComputeBarWidth();
            barSpacing = (float)(0.5 * barWidth);

            shapeContainer.Offset = new System.Numerics.Vector3(shapeGraphOffsetX, shapeGraphOffsetY, 0);
            shapeContainer.Size = new System.Numerics.Vector2(shapeGraphContainerWidth, shapeGraphContainerHeight);

            xAxisLine.Start = new System.Numerics.Vector2(0, shapeGraphContainerHeight - shapeGraphOffsetY);
            xAxisLine.End = new System.Numerics.Vector2(shapeGraphContainerWidth - shapeGraphOffsetX, shapeGraphContainerHeight - shapeGraphOffsetY);

            yAxisLine.Start = new System.Numerics.Vector2(0, shapeGraphContainerHeight - shapeGraphOffsetY);
            yAxisLine.End = new System.Numerics.Vector2(0, 0);
        }

        private ContainerVisual GenerateGraphStructure()
        {
            mainContainer = compositor.CreateContainerVisual();

            // Create shape tree to hold.
            shapeContainer = compositor.CreateShapeVisual();

            xAxisLine = compositor.CreateLineGeometry();
            yAxisLine = compositor.CreateLineGeometry();

            var xAxisShape = compositor.CreateSpriteShape(xAxisLine);
            xAxisShape.StrokeBrush = compositor.CreateColorBrush(Colors.Black);
            xAxisShape.FillBrush = compositor.CreateColorBrush(Colors.Black);

            var yAxisShape = compositor.CreateSpriteShape(yAxisLine);
            yAxisShape.StrokeBrush = compositor.CreateColorBrush(Colors.Black);

            shapeContainer.Shapes.Add(xAxisShape);
            shapeContainer.Shapes.Add(yAxisShape);

            mainContainer.Children.InsertAtTop(shapeContainer);

            UpdateSizeAndPositions();

            // Draw text.
            DrawText(textRenderTarget, Title, XAxisLabel, YAxisLabel, textSize);

            // Return root node for graph.
            return mainContainer;
        }

        public void UpdateDPI(double newDpiX, double newDpiY, double newWidth, double newHeight)
        {
            var oldHeight = graphHeight;
            var oldWidth = graphWidth;
            graphHeight = (float)(newWidth * newDpiY / 96.0);
            graphWidth = (float)(newHeight * newDpiX / 96.0);

            UpdateSizeAndPositions();

            // Update bars.
            for (int i = 0; i < barValueMap.Count; i++)
            {
                Bar bar = (Bar)barValueMap[i];

                var xOffset = shapeGraphOffsetX + barSpacing + (barWidth + barSpacing) * i;
                var height = bar.Height;
                if (oldHeight != newHeight)
                {
                    height = GetAdjustedBarHeight(maxBarValue, graphData[i]);
                }

                bar.UpdateSize(barWidth, height);
                bar.Root.Offset = new System.Numerics.Vector3(xOffset, shapeGraphContainerHeight, 0);
                bar.OutlineRoot.Offset = new System.Numerics.Vector3(xOffset, shapeGraphContainerHeight, 0);
            }

            // Update text render target and redraw text.
            textRenderTarget.DotsPerInch = new Size2F((float)newDpiX, (float)newDpiY);
            textRenderTarget.Resize(new Size2((int)(newWidth * newDpiX / 96.0), (int)(newWidth * newDpiY / 96.0)));
            DrawText(textRenderTarget, Title, XAxisLabel, YAxisLabel, textSize);
        }

        public void DrawText(WindowRenderTarget renderTarget, string titleText, string xAxisText, string yAxisText, float baseTextSize)
        {
            var sgOffsetY = graphTextHeight * 1 / 15;
            var sgOffsetX = graphTextWidth * 1 / 15;
            var containerHeight = graphTextHeight - sgOffsetY * 2;
            var containerWidth = graphTextWidth - sgOffsetX * 2;
            var textWidth = (int)containerHeight;
            var textHeight = (int)sgOffsetY;


            var FactoryDWrite = new SharpDX.DirectWrite.Factory();

            textFormatTitle = new TextFormat(FactoryDWrite, "Segoe", baseTextSize * 5 / 4)
            {
                TextAlignment = TextAlignment.Center,
                ParagraphAlignment = ParagraphAlignment.Center
            };
            textFormatHorizontal = new TextFormat(FactoryDWrite, "Segoe", baseTextSize)
            {
                TextAlignment = TextAlignment.Center,
                ParagraphAlignment = ParagraphAlignment.Far
            };
            textFormatVertical = new TextFormat(FactoryDWrite, "Segoe", baseTextSize)
            {
                TextAlignment = TextAlignment.Center,
                ParagraphAlignment = ParagraphAlignment.Far
            };

            renderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
            renderTarget.TextAntialiasMode = TextAntialiasMode.Cleartype;

            textSceneColorBrush = new SolidColorBrush(renderTarget, black);

            RectangleF ClientRectangleTitle = new RectangleF(0, 0, textWidth, textHeight);
            RectangleF ClientRectangleXAxis = new RectangleF(0,
                containerHeight - textHeight + sgOffsetY * 2, textWidth, textHeight);
            RectangleF ClientRectangleYAxis = new RectangleF(-sgOffsetX,
                containerHeight - textHeight + sgOffsetY, textWidth, textHeight);

            textSceneColorBrush.Color = black;

            //Draw title and x axis text.
            renderTarget.BeginDraw();

            renderTarget.Clear(white);
            renderTarget.DrawText(titleText, textFormatTitle, ClientRectangleTitle, textSceneColorBrush);
            renderTarget.DrawText(xAxisText, textFormatHorizontal, ClientRectangleXAxis, textSceneColorBrush);

            renderTarget.EndDraw();

            // Rotate render target to draw y axis text.
            renderTarget.Transform = Matrix3x2.Rotation((float)(-Math.PI / 2), new Vector2(0, containerHeight));

            renderTarget.BeginDraw();

            renderTarget.DrawText(yAxisText, textFormatVertical, ClientRectangleYAxis, textSceneColorBrush);

            renderTarget.EndDraw();

            // Rotate the RenderTarget back.
            renderTarget.Transform = Matrix3x2.Identity;
        }

        //Dispose of resources.
        public void Dispose()
        {
            textSceneColorBrush.Dispose();
            textFormatTitle.Dispose();
            textFormatHorizontal.Dispose();
            textFormatVertical.Dispose();
        }

        private Bar[] CreateBars(float[] data)
        {
            //Clear hashmap.
            barValueMap.Clear();

            var barBrushHelper = new BarGraphUtility.BarBrushHelper(compositor);
            var brushes = new CompositionBrush[data.Length];
            switch (graphBarStyle)
            {
                case GraphBarStyle.Single:
                    brushes = barBrushHelper.GenerateSingleColorBrush(data.Length, graphBarColors[0]);
                    break;
                case GraphBarStyle.Random:
                    brushes = barBrushHelper.GenerateRandomColorBrushes(data.Length);
                    break;
                case GraphBarStyle.PerBarLinearGradient:
                    brushes = barBrushHelper.GeneratePerBarLinearGradient(data.Length, graphBarColors);
                    break;
                case GraphBarStyle.AmbientAnimatingPerBarLinearGradient:
                    brushes = barBrushHelper.GenerateAmbientAnimatingPerBarLinearGradient(data.Length, graphBarColors);
                    break;
                default:
                    brushes = barBrushHelper.GenerateSingleColorBrush(data.Length, graphBarColors[0]);
                    break;
            }

            var maxValue = maxBarValue = GetMaxBarValue(data);
            var bars = new Bar[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                var xOffset = shapeGraphOffsetX + barSpacing + (barWidth + barSpacing) * i;
                var height = GetAdjustedBarHeight(maxValue, graphData[i]);

                var bar = new BarGraphUtility.Bar(compositor, shapeGraphContainerHeight, height, barWidth, "something", graphData[i], brushes[i]);
                bar.OutlineRoot.Offset = new System.Numerics.Vector3(xOffset, shapeGraphContainerHeight, 0);
                bar.Root.Offset = new System.Numerics.Vector3(xOffset, shapeGraphContainerHeight, 0);

                barValueMap.Add(i, bar);

                bars[i] = bar;
            }
            return bars;
        }

        private void AddBarsToTree(Bar[] bars)
        {
            BarRoot.Children.RemoveAll();
            for (int i = 0; i < bars.Length; i++)
            {
                BarRoot.Children.InsertAtTop(bars[i].OutlineRoot);
                BarRoot.Children.InsertAtTop(bars[i].Root);
            }

            AddLight();
        }

        public void GetClickedBar(Point point)
        {
            Bar clickedBar = null;
            for (int i = 0; i < barValueMap.Count; i++)
            {
                Bar bar = (Bar)barValueMap[i];

                var barOffset = bar.Root.Offset;
                var barSize = bar.Root.Size;

                //TODO i dont think we need to transform bar offset??? size??? (actually probably do need to translate to DiP screencoords)

                //If point is within bounds of the bar, mark as 
                //       clickedBar = thingy
                //         break
            }

            if (clickedBar != null)
            {
                //TODO create visual to mimic a 

                //TODO add shadow to visual

            }
        }

        public void UpdateGraphData(string title, string xAxisTitle, string yAxisTitle, float[] newData)
        {
            // Update properties.
            Title = title;
            XAxisLabel = xAxisTitle;
            YAxisLabel = yAxisTitle;

            // Update text.
            DrawText(textRenderTarget, Title, XAxisLabel, YAxisLabel, textSize);

            // Generate bars.
            // If the same number of data points, update bars with new data. Otherwise, wipe and create new.
            if (graphData.Length == newData.Length)
            {
                var maxValue = GetMaxBarValue(newData);
                for (int i = 0; i < graphData.Length; i++)
                {
                    // Animate bar height.
                    var oldBar = (Bar)(barValueMap[i]);
                    var newBarHeight = GetAdjustedBarHeight(maxValue, newData[i]);

                    // Update Bar.
                    oldBar.Height = newBarHeight; // Trigger height animation.
                    oldBar.Label = "something2";
                    oldBar.Value = newData[i];
                }
            }
            else
            {
                var bars = CreateBars(newData);
                AddBarsToTree(bars);
            }

            // Reset to new data.
            graphData = newData;
        }

        private void AddLight()
        {
            ambientLight = compositor.CreateAmbientLight();
            ambientLight.Color = Colors.White;
            ambientLight.Targets.Add(mainContainer);

            var innerConeColor = Colors.White;
            var outerConeColor = Colors.AntiqueWhite;

            barOutlineLight = compositor.CreateSpotLight();
            barOutlineLight.InnerConeColor = innerConeColor;
            barOutlineLight.OuterConeColor = outerConeColor;
            barOutlineLight.CoordinateSpace = mainContainer;
            barOutlineLight.InnerConeAngleInDegrees = 45;
            barOutlineLight.OuterConeAngleInDegrees = 80;

            barOutlineLight.Offset = new System.Numerics.Vector3(0, 0, 80);

            // Target bars outlines with light.
            for (int i = 0; i < barValueMap.Count; i++)
            {
                Bar bar = (Bar)barValueMap[i];
                barOutlineLight.Targets.Add(bar.OutlineRoot);
            }


            barLight = compositor.CreatePointLight();
            barLight.Color = outerConeColor;
            barLight.CoordinateSpace = mainContainer;
            barLight.Intensity = 0.5f;

            barLight.Offset = new System.Numerics.Vector3(0, 0, 120);

            // Target bars with softer point light.
            for (int i = 0; i < barValueMap.Count; i++)
            {
                Bar bar = (Bar)barValueMap[i];
                barLight.Targets.Add(bar.Root);
            }
        }

        public void UpdateLight(System.Windows.Point relativePoint)
        {
            barOutlineLight.Offset = new System.Numerics.Vector3((float)relativePoint.X,
                (float)relativePoint.Y, barOutlineLight.Offset.Z);
            barLight.Offset = new System.Numerics.Vector3((float)relativePoint.X,
                (float)relativePoint.Y, barLight.Offset.Z);
        }

        private float GetMaxBarValue(float[] data)
        {
            float max = data[0];
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] > max)
                {
                    max = data[i];
                }
            }
            return max;
        }

        // Adjust bar height relative to the max bar value.
        private float GetAdjustedBarHeight(float maxValue, float originalValue)
        {
            return (shapeGraphContainerHeight - shapeGraphOffsetY) * (originalValue / maxValue);
        }

        // Return computed bar width for graph. Default spacing is 1/2 bar width.
        private float ComputeBarWidth()
        {
            var spacingUnits = (graphData.Length + 1) / 2;

            return ((shapeGraphContainerWidth - (2 * shapeGraphOffsetX)) / (graphData.Length + spacingUnits));
        }
    }
}
