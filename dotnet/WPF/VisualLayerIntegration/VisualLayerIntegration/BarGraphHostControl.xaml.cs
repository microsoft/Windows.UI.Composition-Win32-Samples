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
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Windows.UI.Composition;
using SysWin = System.Windows;


namespace VisualLayerIntegration
{
    /// <summary>
    /// Interaction logic for BarGraphHostControl.xaml
    /// </summary>
    public partial class BarGraphHostControl : UserControl
    {
        private CompositionHost _compositionHost;
        private Compositor _compositor;
        private Windows.UI.Composition.ContainerVisual _graphContainer;
        private BarGraph _currentGraph;

        private double _currentDpiX = 96.0;
        private double _currentDpiY = 96.0;

        protected WindowRenderTarget _windowRenderTarget;
        private static RawColor4 _white = new RawColor4(255, 255, 255, 255);

        public BarGraphHostControl()
        {
            InitializeComponent();
            Loaded += BarGraphHostControl_Loaded;
            DataContextChanged += BarGraphHostControl_DataContextChanged;
        }

        private void BarGraphHostControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateGraph();
        }

        private void BarGraphHostControl_Loaded(object sender, RoutedEventArgs e)
        {
            // If the user changes the DPI scale setting for the screen the app is on,
            // the CompositionHostControl is reloaded. Don't redo this set up if it's
            // already been done.
            if (_compositionHost == null)
            {
                var currentDpi = VisualTreeHelper.GetDpi(this);
                _currentDpiX = currentDpi.PixelsPerInchX;
                _currentDpiY = currentDpi.PixelsPerInchY;

                currentDpi = VisualTreeHelper.GetDpi(this);

                _compositionHost = new CompositionHost();
                CompositionHostElement.Child = _compositionHost;
                _compositor = _compositionHost.Compositor;
                _graphContainer = _compositor.CreateContainerVisual();
                _compositionHost.SetChild(_graphContainer);

                _compositionHost.MouseMoved += HostControl_MouseMoved;
                _compositionHost.InvalidateDrawing += CompositionHost_InvalidateDrawing;

                // Create properties for render target
                var factory2D = new SharpDX.Direct2D1.Factory();
                var width = (float)CompositionHostElement.ActualWidth;
                var height = (float)CompositionHostElement.ActualHeight;

                var properties = new HwndRenderTargetProperties();
                properties.Hwnd = _compositionHost.hwndHost;
                properties.PixelSize = new SharpDX.Size2((int)(width * _currentDpiX / 96.0), (int)(width * _currentDpiY / 96.0));
                properties.PresentOptions = PresentOptions.None;

                // Create render target
                _windowRenderTarget = new WindowRenderTarget(factory2D, new RenderTargetProperties(new SharpDX.Direct2D1.PixelFormat(Format.Unknown, SharpDX.Direct2D1.AlphaMode.Premultiplied)), properties);
                _windowRenderTarget.DotsPerInch = new Size2F((float)_currentDpiX, (float)_currentDpiY);
                _windowRenderTarget.Resize(new Size2((int)(width * _currentDpiX / 96.0), (int)(width * _currentDpiY / 96.0)));
            }
        }

        private void HostControl_MouseMoved(object sender, HwndMouseEventArgs e)
        {
            // Adjust light position.
            if (_currentGraph != null)
            {
                // Convert mouse position to DIP (is raised in physical pixels).
                var posDip = GetPointInDIP(e.point);

                var adjustedTopLeft = GetControlPointInDIP(CompositionHostElement);

                // Get point relative to control.
                var relativePoint = new SysWin.Point(posDip.X - adjustedTopLeft.X, posDip.Y - adjustedTopLeft.Y);

                // Update light position.
                _currentGraph.UpdateLight(relativePoint);
            }
        }

        private SysWin.Point GetPointInDIP(SysWin.Point point)
        {
            var posDipX = point.X / (_currentDpiX / 96.0);
            var posDipY = point.Y / (_currentDpiY / 96.0);
            return new SysWin.Point(posDipX, posDipY);
        }

        private SysWin.Point GetControlPointInDIP(UIElement control)
        {
            // Get bounds of hwnd host control.
            // Top left of control relative to screen.
            var controlTopLeft = control.PointToScreen(new SysWin.Point(0, 0));
            // Convert screen coord to DIP.
            var adjustedX = controlTopLeft.X / (_currentDpiX / 96.0);
            var adjustedY = controlTopLeft.Y / (_currentDpiY / 96.0);
            return new SysWin.Point(adjustedX, adjustedY);
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            if (this.ActualWidth > 0 && _currentGraph != null)
            {
                _currentDpiX = newDpi.PixelsPerInchX;
                _currentDpiY = newDpi.PixelsPerInchY;
                _currentGraph.UpdateSize(newDpi, CompositionHostElement.ActualWidth, CompositionHostElement.ActualHeight);
            }
        }

        // Handle Composition tree creation and updates.
        public void UpdateGraph()
        {
            Customer customer = DataContext as Customer;
            if (customer != null)
            {
                var graphTitle = customer.FirstName + " Investment History";
                var xAxisTitle = "Investment #";
                var yAxisTitle = "# Shares of Stock";

                // If graph already exists update values. Otherwise, create new graph.
                if (_graphContainer.Children.Count > 0 && _currentGraph != null)
                {
                    _currentGraph.UpdateGraphData(graphTitle, xAxisTitle, yAxisTitle, customer.Data);
                }
                else
                {
                    var graph = new BarGraph(_compositor, _compositionHost.hwndHost, graphTitle, xAxisTitle, yAxisTitle,
                        (float)CompositionHostElement.ActualWidth, (float)CompositionHostElement.ActualHeight, _currentDpiX, _currentDpiY, customer.Data, _windowRenderTarget,
                        true, BarGraph.GraphBarStyle.PerBarLinearGradient,
                        new Windows.UI.Color[] { Windows.UI.Color.FromArgb(255, 246, 65, 108), Windows.UI.Color.FromArgb(255, 255, 246, 183) });

                    _currentGraph = graph;
                    _graphContainer.Children.InsertAtTop(graph.GraphRoot);
                }
            }
        }

        private void CompositionHost_InvalidateDrawing(object sender, InvalidateDrawingEventArgs e)
        {
            var width = e.Width;
            var height = e.Height;

            // Clear render target backbround
            _windowRenderTarget.BeginDraw();
            _windowRenderTarget.Clear(_white);
            _windowRenderTarget.EndDraw();

            // Update graph
            if (_currentGraph != null)
            {
                var currentDpi = VisualTreeHelper.GetDpi(this);
                _currentGraph.UpdateSize(currentDpi, width, height);
            }
        }
    }
}
