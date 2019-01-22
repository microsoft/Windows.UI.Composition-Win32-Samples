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
        private float[] _graphData;

        private Compositor _compositor;
        private IntPtr _hwnd;

        private float _graphWidth, _graphHeight;
        private float _graphTextWidth, _graphTextHeight;
        private float _shapeGraphContainerHeight, _shapeGraphContainerWidth, _shapeGraphOffsetY, _shapeGraphOffsetX;
        private float _barWidth, _barSpacing;
        private float _maxBarValue;

        private GraphBarStyle _graphBarStyle;
        private List<Windows.UI.Color> _graphBarColors;

        //int key = position#; Bar value = Bar
        private Hashtable _barValueMap;   

        private WindowRenderTarget _textRenderTarget;
        private SolidColorBrush _textSceneColorBrush;
        private TextFormat _textFormatTitle;
        private TextFormat _textFormatHorizontal;
        private TextFormat _textFormatVertical;

        private ShapeVisual shapeContainer;
        private CompositionLineGeometry xAxisLine;
        private CompositionLineGeometry yAxisLine;
        private ContainerVisual mainContainer;

        private int _textRectWidth;
        private int _textRectHeight;
        private static SharpDX.Mathematics.Interop.RawColor4 black = new SharpDX.Mathematics.Interop.RawColor4(0, 0, 0, 255);
        private static SharpDX.Mathematics.Interop.RawColor4 white = new SharpDX.Mathematics.Interop.RawColor4(255, 255, 255, 255);

        private static float textSize = 20.0f;

        private AmbientLight _ambientLight;
        private SpotLight _barOutlineLight;
        private PointLight _barLight;

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

        /*
         * Constructor for bar graph. 
         * For now only does single bars, no grouping
         * As of 12/6 to insert graph, call the constructor then use barGraph.Root to get the container to parent
         */
        public BarGraph(Compositor compositor, IntPtr hwnd, string title, string xAxisLabel, string yAxisLabel, float width, float height, double dpiX, double dpiY, float[] data,//required parameters
            bool AnimationsOn = true, GraphBarStyle graphBarStyle = GraphBarStyle.Single, //optional parameters
            List<Windows.UI.Color> barColors = null)
        {
            _compositor = compositor;
            _hwnd = hwnd;
            this._graphWidth = (float)(width * dpiX / 96.0);
            this._graphHeight = (float)(height * dpiY / 96.0);

            this._graphTextWidth = (float)(width);
            this._graphTextHeight = (float)(height);

            this._graphData = data;

            Title = title;
            XAxisLabel = xAxisLabel;
            YAxisLabel = yAxisLabel;
            
            _graphBarStyle = graphBarStyle;

            if(barColors != null)
            {
                _graphBarColors = barColors;
            }
            else
            {
                _graphBarColors = new List<Windows.UI.Color>() { Colors.Blue };
            }

            // Configure options for text
            var Factory2D = new SharpDX.Direct2D1.Factory();

            HwndRenderTargetProperties properties = new HwndRenderTargetProperties();
            properties.Hwnd = _hwnd;
            properties.PixelSize = new SharpDX.Size2((int)(width * dpiX / 96.0), (int)(width * dpiY / 96.0));
            properties.PresentOptions = PresentOptions.None;

            _textRenderTarget = new WindowRenderTarget(Factory2D, new RenderTargetProperties(new PixelFormat(Format.Unknown, SharpDX.Direct2D1.AlphaMode.Premultiplied)), properties);
            _textRenderTarget.DotsPerInch = new Size2F((float)dpiX, (float)dpiY);
            _textRenderTarget.Resize(new Size2((int)(width * dpiX / 96.0), (int)(width * dpiY / 96.0)));

            // Generate graph structure
            var graphRoot = GenerateGraphStructure();
            GraphRoot = graphRoot;

            BarRoot = _compositor.CreateContainerVisual();
            GraphRoot.Children.InsertAtBottom(BarRoot);

            //If data has been provided init bars and animations, else leave graph empty
            if (_graphData.Length > 0)
            {
                _barValueMap = new Hashtable();
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
            _textRectWidth = (int)_shapeGraphContainerWidth;
            _textRectHeight = (int)_shapeGraphOffsetY;            

            mainContainer.Offset = new System.Numerics.Vector3(_shapeGraphOffsetX, _shapeGraphOffsetY, 0);

            _barWidth = ComputeBarWidth();
            _barSpacing = (float)(0.5 * _barWidth);

            shapeContainer.Offset = new System.Numerics.Vector3(_shapeGraphOffsetX, _shapeGraphOffsetY, 0);
            shapeContainer.Size = new System.Numerics.Vector2(_shapeGraphContainerWidth, _shapeGraphContainerHeight); 
            
            xAxisLine.Start = new System.Numerics.Vector2(0, _shapeGraphContainerHeight - _shapeGraphOffsetY);
            xAxisLine.End = new System.Numerics.Vector2(_shapeGraphContainerWidth - _shapeGraphOffsetX, _shapeGraphContainerHeight - _shapeGraphOffsetY);

            yAxisLine.Start = new System.Numerics.Vector2(0, _shapeGraphContainerHeight - _shapeGraphOffsetY);
            yAxisLine.End = new System.Numerics.Vector2(0, 0);
        }

        private ContainerVisual GenerateGraphStructure()
        {
            mainContainer = _compositor.CreateContainerVisual();

            // Create shape tree to hold 
            shapeContainer = _compositor.CreateShapeVisual();
            
            xAxisLine = _compositor.CreateLineGeometry();
            yAxisLine = _compositor.CreateLineGeometry();

            var xAxisShape = _compositor.CreateSpriteShape(xAxisLine);
            xAxisShape.StrokeBrush = _compositor.CreateColorBrush(Colors.Black);
            xAxisShape.FillBrush = _compositor.CreateColorBrush(Colors.Black);

            var yAxisShape = _compositor.CreateSpriteShape(yAxisLine);
            yAxisShape.StrokeBrush = _compositor.CreateColorBrush(Colors.Black);

            shapeContainer.Shapes.Add(xAxisShape);
            shapeContainer.Shapes.Add(yAxisShape);

            mainContainer.Children.InsertAtTop(shapeContainer);

            UpdateSizeAndPositions();

            // Draw text
            DrawText(_textRenderTarget, Title, XAxisLabel, YAxisLabel, textSize);

            // Return root node for graph
            return mainContainer;
        }

        public void UpdateDPI(double newDpiX, double newDpiY, double newWidth, double newHeight)
        {
            var oldHeight = _graphHeight;
            var oldWidth = _graphWidth;
            _graphHeight = (float)(newWidth * newDpiY / 96.0);
            _graphWidth = (float)(newHeight * newDpiX / 96.0);

            UpdateSizeAndPositions();

            // Update bars
            for (int i = 0; i < _barValueMap.Count; i++)
            {
                Bar bar = (Bar)_barValueMap[i];

                var xOffset = _shapeGraphOffsetX + _barSpacing + (_barWidth + _barSpacing) * i;
                var height = bar.Height;
                if (oldHeight != newHeight)
                {
                    height = GetAdjustedBarHeight(_maxBarValue, _graphData[i]);
                }

                bar.UpdateSize(_barWidth, height);
                bar.Root.Offset = new System.Numerics.Vector3(xOffset, _shapeGraphContainerHeight, 0);
                bar.OutlineRoot.Offset = new System.Numerics.Vector3(xOffset, _shapeGraphContainerHeight, 0);
            }

            // Update text render target and redraw text
            _textRenderTarget.DotsPerInch = new Size2F((float)newDpiX, (float)newDpiY);
            _textRenderTarget.Resize(new Size2((int)(newWidth * newDpiX / 96.0), (int)(newWidth * newDpiY / 96.0)));
            DrawText(_textRenderTarget, Title, XAxisLabel, YAxisLabel, textSize);
        }
       
        public void DrawText(WindowRenderTarget renderTarget, string titleText, string xAxisText, string yAxisText, float baseTextSize)
        {
            var sgOffsetY = _graphTextHeight * 1 / 15;
            var sgOffsetX = _graphTextWidth * 1 / 15;
            var containerHeight = _graphTextHeight - sgOffsetY * 2;
            var containerWidth = _graphTextWidth - sgOffsetX * 2;
            var textWidth = (int)containerHeight;
            var textHeight = (int)sgOffsetY;


            var FactoryDWrite = new SharpDX.DirectWrite.Factory();

            _textFormatTitle = new TextFormat(FactoryDWrite, "Segoe", baseTextSize*5/4) {   TextAlignment = TextAlignment.Center, ParagraphAlignment = ParagraphAlignment.Center };
            _textFormatHorizontal = new TextFormat(FactoryDWrite, "Segoe", baseTextSize) { TextAlignment = TextAlignment.Center, ParagraphAlignment = ParagraphAlignment.Far };
            _textFormatVertical = new TextFormat(FactoryDWrite, "Segoe", baseTextSize) { TextAlignment = TextAlignment.Center, ParagraphAlignment = ParagraphAlignment.Far };

            renderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
            renderTarget.TextAntialiasMode = TextAntialiasMode.Cleartype;

            _textSceneColorBrush = new SolidColorBrush(renderTarget, black);

            RectangleF ClientRectangleTitle = new RectangleF(0, 0, textWidth, textHeight);
            RectangleF ClientRectangleXAxis = new RectangleF(0, containerHeight - textHeight + sgOffsetY * 2, textWidth, textHeight);
            RectangleF ClientRectangleYAxis = new RectangleF(-sgOffsetX, containerHeight - textHeight + sgOffsetY, textWidth, textHeight);

            _textSceneColorBrush.Color = black;

            //Draw title and x axis text

            renderTarget.BeginDraw();

            renderTarget.Clear(white);
            renderTarget.DrawText(titleText, _textFormatTitle, ClientRectangleTitle, _textSceneColorBrush);
            renderTarget.DrawText(xAxisText, _textFormatHorizontal, ClientRectangleXAxis, _textSceneColorBrush);

            renderTarget.EndDraw();

            // Rotate render target to draw y axis text
            renderTarget.Transform = Matrix3x2.Rotation((float)(-Math.PI / 2), new Vector2(0, containerHeight));

            renderTarget.BeginDraw();

            renderTarget.DrawText(yAxisText, _textFormatVertical, ClientRectangleYAxis, _textSceneColorBrush);

            renderTarget.EndDraw();

            // Rotate the RenderTarget back
            renderTarget.Transform = Matrix3x2.Identity;
        }

        /*
         * Dispose of resources
         */ 
        public void Dispose()
        {
            _textSceneColorBrush.Dispose();
            _textFormatTitle.Dispose();
            _textFormatHorizontal.Dispose();
            _textFormatVertical.Dispose();
        }

        private Bar[] CreateBars(float[] data)
        {
            //Clear hashmap 
            _barValueMap.Clear();

            var barBrushHelper = new BarGraphUtility.BarBrushHelper(_compositor);
            CompositionBrush[] brushes = new CompositionBrush[data.Length];
            switch (_graphBarStyle)
            {
                case GraphBarStyle.Single:
                    brushes = barBrushHelper.GenerateSingleColorBrush(data.Length, _graphBarColors[0]);
                    break;
                case GraphBarStyle.Random:
                    brushes = barBrushHelper.GenerateRandomColorBrushes(data.Length);
                    break;
                case GraphBarStyle.PerBarLinearGradient:
                    brushes = barBrushHelper.GeneratePerBarLinearGradient(data.Length, _graphBarColors);
                    break;
                case GraphBarStyle.AmbientAnimatingPerBarLinearGradient:
                    brushes = barBrushHelper.GenerateAmbientAnimatingPerBarLinearGradient(data.Length, _graphBarColors);
                    break;
                default:
                    brushes = barBrushHelper.GenerateSingleColorBrush(data.Length, _graphBarColors[0]);
                    break;
            }
           
            var maxValue = _maxBarValue = GetMaxBarValue(data);
            var bars = new Bar[data.Length];
            for(int i=0; i<data.Length; i++)
            {
                var xOffset = _shapeGraphOffsetX + _barSpacing + (_barWidth + _barSpacing) * i;
                var height = GetAdjustedBarHeight(maxValue, _graphData[i]);

                var bar = new BarGraphUtility.Bar(_compositor, _shapeGraphContainerHeight, height, _barWidth, "something", _graphData[i], brushes[i]);
                bar.OutlineRoot.Offset = new System.Numerics.Vector3(xOffset, _shapeGraphContainerHeight , 0);
                bar.Root.Offset = new System.Numerics.Vector3(xOffset, _shapeGraphContainerHeight, 0);

                _barValueMap.Add(i, bar);

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
            for (int i = 0; i < _barValueMap.Count; i++)
            {
                Bar bar = (Bar)_barValueMap[i];

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
            // Update properties
            Title = title;
            XAxisLabel = xAxisTitle;
            YAxisLabel = yAxisTitle;

            // Update text
            DrawText(_textRenderTarget, Title, XAxisLabel, YAxisLabel, textSize);

            // Generate bars 
            // If the same number of data points, update bars with new data. Else wipe and create new.
            if (_graphData.Length == newData.Length)
            {
                var maxValue = GetMaxBarValue(newData);
                for (int i=0; i< _graphData.Length; i++)
                {
                    // Animate bar height
                    var oldBar = (Bar)(_barValueMap[i]);
                    var newBarHeight = GetAdjustedBarHeight(maxValue, newData[i]);

                    // Update Bar
                    oldBar.Height = newBarHeight; //Trigger height animation
                    oldBar.Label = "something2";
                    oldBar.Value = newData[i];
                }
            }
            else
            {
                var bars = CreateBars(newData);
                AddBarsToTree(bars);
            }

            // Reset to new data
            _graphData = newData;
        }

        private void AddLight()
        {
            _ambientLight = _compositor.CreateAmbientLight();
            _ambientLight.Color = Colors.White;
            _ambientLight.Targets.Add(mainContainer);

            var innerConeColor = Colors.White;
            var outerConeColor = Colors.AntiqueWhite;

            _barOutlineLight = _compositor.CreateSpotLight();
            _barOutlineLight.InnerConeColor = innerConeColor;
            _barOutlineLight.OuterConeColor = outerConeColor;
            _barOutlineLight.CoordinateSpace = mainContainer;
            _barOutlineLight.InnerConeAngleInDegrees = 45;
            _barOutlineLight.OuterConeAngleInDegrees = 80;

            _barOutlineLight.Offset = new System.Numerics.Vector3(0, 0, 80);

            // Target bars outlines with light
            for (int i = 0; i < _barValueMap.Count; i++)
            {
                Bar bar = (Bar)_barValueMap[i];
                _barOutlineLight.Targets.Add(bar.OutlineRoot);
            }


            _barLight = _compositor.CreatePointLight();
            _barLight.Color = outerConeColor;
            _barLight.CoordinateSpace = mainContainer;
            _barLight.Intensity = 0.5f;

            _barLight.Offset = new System.Numerics.Vector3(0, 0, 120);  

            // Target bars with softer point light
            for (int i = 0; i < _barValueMap.Count; i++)
            {
                Bar bar = (Bar)_barValueMap[i];
                _barLight.Targets.Add(bar.Root);
            }
        }

        public void UpdateLight(System.Windows.Point relativePoint)
        {
            _barOutlineLight.Offset = new System.Numerics.Vector3((float)relativePoint.X, (float)relativePoint.Y, _barOutlineLight.Offset.Z);
            _barLight.Offset = new System.Numerics.Vector3((float)relativePoint.X, (float)relativePoint.Y, _barLight.Offset.Z);
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

        /*
         * Adjust bar height relative to the max bar value
         */ 
        private float GetAdjustedBarHeight(float maxValue, float originalValue)
        {
            return (_shapeGraphContainerHeight-_shapeGraphOffsetY) * (originalValue / maxValue);
        }

        /*
         * Return computed bar width for graph. Default spacing is 1/2 bar width.
         */
        private float ComputeBarWidth()
        {
            var spacingUnits = (_graphData.Length + 1) / 2;
            
            return ((_shapeGraphContainerWidth - (2*_shapeGraphOffsetX)) / (_graphData.Length + spacingUnits));
        }

    }
}
