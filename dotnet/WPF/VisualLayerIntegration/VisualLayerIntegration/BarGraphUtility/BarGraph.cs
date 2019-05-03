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

using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using System;
using System.Linq;
using SnVector2 = System.Numerics.Vector2;
using SnVector3 = System.Numerics.Vector3;
using Windows.UI;
using Windows.UI.Composition;
using SysWin = System.Windows;

namespace BarGraphUtility
{
    sealed class BarGraph: IDisposable
    {
        private double[] _graphData;

        private readonly Compositor _compositor;

        private float _graphWidth, _graphHeight;
        private float _shapeGraphContainerHeight, _shapeGraphContainerWidth, _shapeGraphOffsetY, _shapeGraphOffsetX;
        private float _barWidth, _barSpacing;
        private double _maxBarValue;

        private GraphBarStyle _graphBarStyle;
        private readonly Windows.UI.Color[] _graphBarColors;

        private Bar[] _bars;

        private readonly WindowRenderTarget _textRenderTarget;
        private readonly SolidColorBrush _textSceneColorBrush;
        private TextFormat _textFormatTitle;
        private TextFormat _textFormatHorizontal;
        private TextFormat _textFormatVertical;

        private ShapeVisual _shapeContainer;
        private CompositionLineGeometry _xAxisLine;
        private CompositionLineGeometry _yAxisLine;
        private ContainerVisual _mainContainer;

        private static SharpDX.Mathematics.Interop.RawColor4 _black = new SharpDX.Mathematics.Interop.RawColor4(0, 0, 0, 255);
        private static SharpDX.Mathematics.Interop.RawColor4 _white = new SharpDX.Mathematics.Interop.RawColor4(255, 255, 255, 255);

        private static float _textSize = 20.0f;

        private readonly AmbientLight _ambientLight;
        private readonly SpotLight _barOutlineLight;
        private readonly PointLight _barLight;
        private string _graphTitle;
        private string _xAxisLabel;
        private string _yAxisLabel;
        private readonly ContainerVisual _barRoot;

        public ContainerVisual GraphRoot { get; }

        public enum GraphBarStyle
        {
            Single,
            Random,
            PerBarLinearGradient,
            AmbientAnimatingPerBarLinearGradient
        }

        // Constructor for bar graph.
        // To insert graph, call the constructor then use barGraph.Root to get the container to parent.
        public BarGraph(Compositor compositor,
            string title, 
            string xAxisLabel, 
            string yAxisLabel, 
            float width, 
            float height, 
            double dpiX, 
            double dpiY,             
            WindowRenderTarget renderTarget,
            double[] data = null,
            bool AnimationsOn = true, 
            GraphBarStyle graphBarStyle = GraphBarStyle.Single,
            Windows.UI.Color[] barColors = null)
        {
            _compositor = compositor;
            _graphWidth = (float)(width * dpiX / 96.0);
            _graphHeight = (float)(height * dpiY / 96.0);

            _graphData = data;

            _graphTitle = title;
            _xAxisLabel = xAxisLabel;
            _yAxisLabel = yAxisLabel;

            _graphBarStyle = graphBarStyle;
            _graphBarColors = barColors ?? new Windows.UI.Color[] { Colors.Blue };

            _textRenderTarget = renderTarget;
            _textSceneColorBrush = new SolidColorBrush(renderTarget, _black);

            // Generate graph structure.
            GraphRoot = GenerateGraphStructure();

            _barRoot = _compositor.CreateContainerVisual();
            GraphRoot.Children.InsertAtBottom(_barRoot);

            // Ambient light
            _ambientLight = _compositor.CreateAmbientLight();
            _ambientLight.Color = Colors.White;
            _ambientLight.Targets.Add(_mainContainer);

            // Spot light
            var innerConeColor = Colors.White;
            var outerConeColor = Colors.AntiqueWhite;
            _barOutlineLight = _compositor.CreateSpotLight();
            _barOutlineLight.InnerConeColor = innerConeColor;
            _barOutlineLight.OuterConeColor = outerConeColor;
            _barOutlineLight.CoordinateSpace = _mainContainer;
            _barOutlineLight.InnerConeAngleInDegrees = 45;
            _barOutlineLight.OuterConeAngleInDegrees = 80;
            _barOutlineLight.Offset = new SnVector3(0, 0, 80);

            // Point light
            _barLight = _compositor.CreatePointLight();
            _barLight.Color = outerConeColor;
            _barLight.CoordinateSpace = _mainContainer;
            _barLight.Intensity = 0.5f;
            _barLight.Offset = new SnVector3(0, 0, 120);

            // If data has been provided, initialize bars and animations; otherwise, leave graph empty.
            if (_graphData != null && _graphData.Length > 0)
            {
                _bars = new Bar[_graphData.Length];
                var bars = CreateBars(_graphData);
                AddBarsToTree(bars);
            }
        }

        private void UpdateSizeAndPositions()
        {
            _shapeGraphOffsetY = _graphHeight * 1 / 15;
            _shapeGraphOffsetX = _graphWidth * 1 / 15;
            _shapeGraphContainerHeight = _graphHeight - _shapeGraphOffsetY * 2;
            _shapeGraphContainerWidth = _graphWidth - _shapeGraphOffsetX * 2;

            _mainContainer.Offset = new SnVector3(_shapeGraphOffsetX, _shapeGraphOffsetY, 0);
            
            _barWidth = ComputeBarWidth();
            _barSpacing = (float)(0.5 * _barWidth);

            _shapeContainer.Offset = new SnVector3(_shapeGraphOffsetX, _shapeGraphOffsetY, 0);
            _shapeContainer.Size = new SnVector2(_shapeGraphContainerWidth, _shapeGraphContainerHeight);

            _xAxisLine.Start = new SnVector2(0, _shapeGraphContainerHeight - _shapeGraphOffsetY);
            _xAxisLine.End = new SnVector2(_shapeGraphContainerWidth - _shapeGraphOffsetX, _shapeGraphContainerHeight - _shapeGraphOffsetY);

            _yAxisLine.Start = new SnVector2(0, _shapeGraphContainerHeight - _shapeGraphOffsetY);
            _yAxisLine.End = new SnVector2(0, 0);
        }

        private ContainerVisual GenerateGraphStructure()
        {
            _mainContainer = _compositor.CreateContainerVisual();

            // Create shape tree to hold.
            _shapeContainer = _compositor.CreateShapeVisual();

            _xAxisLine = _compositor.CreateLineGeometry();
            _yAxisLine = _compositor.CreateLineGeometry();

            var xAxisShape = _compositor.CreateSpriteShape(_xAxisLine);
            xAxisShape.StrokeBrush = _compositor.CreateColorBrush(Colors.Black);
            xAxisShape.FillBrush = _compositor.CreateColorBrush(Colors.Black);

            var yAxisShape = _compositor.CreateSpriteShape(_yAxisLine);
            yAxisShape.StrokeBrush = _compositor.CreateColorBrush(Colors.Black);

            _shapeContainer.Shapes.Add(xAxisShape);
            _shapeContainer.Shapes.Add(yAxisShape);

            _mainContainer.Children.InsertAtTop(_shapeContainer);

            UpdateSizeAndPositions();

            // Draw text.
            DrawText(_textRenderTarget, _graphTitle, _xAxisLabel, _yAxisLabel, _textSize);

            // Return root node for graph.
            return _mainContainer;
        }

        public void UpdateSize(SysWin.DpiScale dpi, double newWidth, double newHeight)
        {
            var newDpiX = dpi.PixelsPerInchX;
            var newDpiY = dpi.PixelsPerInchY;

            var oldHeight = _graphHeight;
            var oldWidth = _graphWidth;
            _graphHeight = (float)(newWidth * newDpiY / 96.0);
            _graphWidth = (float)(newHeight * newDpiX / 96.0);

            UpdateSizeAndPositions();

            // Update bars.
            for (var i = 0; i < _graphData.Length; i++)
            {
                var bar = _bars[i];

                var xOffset = _shapeGraphOffsetX + _barSpacing + (_barWidth + _barSpacing) * i;
                var height = bar.Height;
                if (oldHeight != newHeight)
                {
                    height = (float)GetAdjustedBarHeight(_maxBarValue, _graphData[i]);
                }

                bar.UpdateSize(_barWidth, height);
                bar.Root.Offset = new SnVector3(xOffset, _shapeGraphContainerHeight, 0);
                bar.OutlineRoot.Offset = new SnVector3(xOffset, _shapeGraphContainerHeight, 0);
            }

            // Scale text size.
            _textSize = _textSize * _graphHeight / oldHeight;
            // Update text render target and redraw text.
            _textRenderTarget.DotsPerInch = new Size2F((float)newDpiX, (float)newDpiY);
            _textRenderTarget.Resize(new Size2((int)(newWidth * newDpiX / 96.0), (int)(newWidth * newDpiY / 96.0)));
            DrawText(_textRenderTarget, _graphTitle, _xAxisLabel, _yAxisLabel, _textSize);
        }

        public void DrawText(WindowRenderTarget renderTarget, string titleText, string xAxisText, string yAxisText, float baseTextSize)
        {
            var sgOffsetY = renderTarget.Size.Height * 1 / 15;
            var sgOffsetX = renderTarget.Size.Width * 1 / 15;
            var containerHeight = renderTarget.Size.Height - sgOffsetY * 2;
            var containerWidth = renderTarget.Size.Width - sgOffsetX * 2; // not used?
            var textWidth = (int)containerHeight;
            var textHeight = (int)sgOffsetY;

            var factoryDWrite = new SharpDX.DirectWrite.Factory();

            _textFormatTitle = new TextFormat(factoryDWrite, "Segoe", baseTextSize * 5 / 4)
            {
                TextAlignment = TextAlignment.Center,
                ParagraphAlignment = ParagraphAlignment.Center
            };
            _textFormatHorizontal = new TextFormat(factoryDWrite, "Segoe", baseTextSize)
            {
                TextAlignment = TextAlignment.Center,
                ParagraphAlignment = ParagraphAlignment.Far
            };
            _textFormatVertical = new TextFormat(factoryDWrite, "Segoe", baseTextSize)
            {
                TextAlignment = TextAlignment.Center,
                ParagraphAlignment = ParagraphAlignment.Far
            };

            renderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
            renderTarget.TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Cleartype;

            var ClientRectangleTitle = new RectangleF(0, 0, textWidth, textHeight);
            var ClientRectangleXAxis = new RectangleF(0,
                containerHeight - textHeight + sgOffsetY * 2, textWidth, textHeight);
            var ClientRectangleYAxis = new RectangleF(-sgOffsetX,
                containerHeight - textHeight + sgOffsetY, textWidth, textHeight);

            _textSceneColorBrush.Color = _black;

            // Draw title and x axis text.
            renderTarget.BeginDraw();

            renderTarget.Clear(_white);
            renderTarget.DrawText(titleText, _textFormatTitle, ClientRectangleTitle, _textSceneColorBrush);
            renderTarget.DrawText(xAxisText, _textFormatHorizontal, ClientRectangleXAxis, _textSceneColorBrush);

            renderTarget.EndDraw();

            // Rotate render target to draw y axis text.
            renderTarget.Transform = Matrix3x2.Rotation((float)(-Math.PI / 2), new SharpDX.Vector2(0, containerHeight));

            renderTarget.BeginDraw();

            renderTarget.DrawText(yAxisText, _textFormatVertical, ClientRectangleYAxis, _textSceneColorBrush);

            renderTarget.EndDraw();

            // Rotate the RenderTarget back.
            renderTarget.Transform = Matrix3x2.Identity;
        }

        // Dispose of resources.
        public void Dispose()
        {
            _textRenderTarget.Dispose();
            _textSceneColorBrush.Dispose();
            _textFormatTitle.Dispose();
            _textFormatHorizontal.Dispose();
            _textFormatVertical.Dispose();
            _ambientLight.Dispose();
            _barOutlineLight.Dispose();
            _barLight.Dispose();
        }

        private Bar[] CreateBars(double[] data)
        {
            // Clear
            _bars = new Bar[data.Length];

            var barBrushHelper = new BarBrushHelper(_compositor);
            var brushes = new CompositionBrush[data.Length];
            CompositionBrush brush = null;

            switch (_graphBarStyle)
            {
                default: // fall through to single by default
                case GraphBarStyle.Single:
                    brush = barBrushHelper.CreateColorBrush(_graphBarColors[0]);
                    break;
                case GraphBarStyle.Random:
                    brushes = barBrushHelper.CreateRandomColorBrushes(data.Length);
                    break;
                case GraphBarStyle.PerBarLinearGradient:
                    brush = barBrushHelper.CreateLinearGradientBrushes(_graphBarColors);
                    break;
                case GraphBarStyle.AmbientAnimatingPerBarLinearGradient:
                    brush = barBrushHelper.CreateAnimatedLinearGradientBrushes(_graphBarColors);
                    break;
            }

            var maxValue = _maxBarValue = Enumerable.Max(data);
            for (var i = 0; i < data.Length; i++)
            {
                var xOffset = _shapeGraphOffsetX + _barSpacing + (_barWidth + _barSpacing) * i;
                var height = GetAdjustedBarHeight(maxValue, data[i]);
                var barBrush = brush ?? brushes[i];

                var bar = new Bar(_compositor, _shapeGraphContainerHeight, (float)height, _barWidth, "something", _graphData[i], barBrush);
                bar.OutlineRoot.Offset = new SnVector3(xOffset, _shapeGraphContainerHeight, 0);
                bar.Root.Offset = new SnVector3(xOffset, _shapeGraphContainerHeight, 0);

                _bars[i] = bar;
            }
            return _bars;
        }

        private void AddBarsToTree(Bar[] bars)
        {
            _barRoot.Children.RemoveAll();
            for (var i = 0; i < bars.Length; i++)
            {
                _barRoot.Children.InsertAtTop(bars[i].OutlineRoot);
                _barRoot.Children.InsertAtTop(bars[i].Root);
            }

            UpdateLightTargets();
        }

        public void UpdateGraphData(string title, string xAxisTitle, string yAxisTitle, double[] newData)
        {
            // Update properties.
            _graphTitle = title;
            _xAxisLabel = xAxisTitle;
            _yAxisLabel = yAxisTitle;

            // Update text.
            DrawText(_textRenderTarget, _graphTitle, _xAxisLabel, _yAxisLabel, _textSize);

            // Generate bars.
            // If the same number of data points, update bars with new data. Otherwise, wipe and create new.
            if (_graphData != null && _graphData.Length == newData.Length)
            {
                var maxValue = Enumerable.Max(newData);
                for (var i = 0; i < _graphData.Length; i++)
                {
                    // Animate bar height.
                    var oldBar = _bars[i];
                    var newBarHeight = GetAdjustedBarHeight(maxValue, newData[i]);

                    // Update Bar.
                    oldBar.Height = (float)newBarHeight; // Trigger height animation.
                }
            }
            else
            {
                _graphData = newData;
                UpdateSizeAndPositions();
                var bars = CreateBars(newData);
                AddBarsToTree(bars);
            }

            // Reset to new data.
            _graphData = newData;
        }

        private void UpdateLightTargets()
        {
            // Target bars outlines with light.
            _barOutlineLight.Targets.RemoveAll();
            for (var i = 0; i < _bars.Length; i++)
            {
                var bar = _bars[i];
                _barOutlineLight.Targets.Add(bar.OutlineRoot);
            }

            // Target bars with softer point light.
            _barLight.Targets.RemoveAll();
            for (var i = 0; i < _bars.Length; i++)
            {
                var bar = _bars[i];
                _barLight.Targets.Add(bar.Root);
            }
        }

        public void UpdateLight(SysWin.Point relativePoint)
        {
            _barOutlineLight.Offset = new SnVector3((float)relativePoint.X,
                (float)relativePoint.Y, _barOutlineLight.Offset.Z);
            _barLight.Offset = new SnVector3((float)relativePoint.X,
                (float)relativePoint.Y, _barLight.Offset.Z);
        }

        // Adjust bar height relative to the max bar value.
        private double GetAdjustedBarHeight(double maxValue, double originalValue)
        {
            return (_shapeGraphContainerHeight - _shapeGraphOffsetY) * (originalValue / maxValue);
        }

        // Return computed bar width for graph. Default spacing is 1/2 bar width.
        private float ComputeBarWidth()
        {
            if(_graphData != null)
            {
                var spacingUnits = (_graphData.Length + 1) / 2;

                return ((_shapeGraphContainerWidth - (2 * _shapeGraphOffsetX)) / (_graphData.Length + spacingUnits));
            }
            return -1;
        }
    }
}
