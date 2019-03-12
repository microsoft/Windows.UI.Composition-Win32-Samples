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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Windows.UI.Composition;

namespace VisualLayerIntegration
{
    /// <summary>
    /// Interaction logic for BarGraphHostControl.xaml
    /// </summary>
    public partial class BarGraphHostControl : UserControl
    {
        private CompositionHost compositionHost;
        private Compositor compositor;
        private Windows.UI.Composition.ContainerVisual graphContainer;
        private BarGraph currentGraph;

        private double currentDpiX = 96.0;
        private double currentDpiY = 96.0;

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
            if (compositionHost is null)
            {
                var currentDpi = VisualTreeHelper.GetDpi(this);
                currentDpiX = currentDpi.PixelsPerInchX;
                currentDpiY = currentDpi.PixelsPerInchY;

                currentDpi = VisualTreeHelper.GetDpi(this);

                compositionHost = new CompositionHost(CompositionHostElement.ActualWidth, CompositionHostElement.ActualHeight, currentDpiX, currentDpiY);
                CompositionHostElement.Child = compositionHost;
                compositor = compositionHost.Compositor;
                graphContainer = compositor.CreateContainerVisual();
                compositionHost.Child = graphContainer;

                compositionHost.MouseMoved += HostControl_MouseMoved;
            }
        }

        private void HostControl_MouseMoved(object sender, HwndMouseEventArgs e)
        {
            // Adjust light position.
            if (currentGraph != null)
            {
                // Convert mouse position to DIP (is raised in physical pixels).
                var posDip = GetPointInDIP(e.point);

                Point adjustedTopLeft = GetControlPointInDIP(CompositionHostElement);

                // Get point relative to control.
                Point relativePoint = new Point(posDip.X - adjustedTopLeft.X, posDip.Y - adjustedTopLeft.Y);

                // Update light position.
                currentGraph.UpdateLight(relativePoint);
            }
        }

        private Point GetPointInDIP(Point point)
        {
            var posDipX = point.X / (currentDpiX / 96.0);
            var posDipY = point.Y / (currentDpiY / 96.0);
            return new Point(posDipX, posDipY);
        }

        private Point GetControlPointInDIP(UIElement control)
        {
            // Get bounds of hwnd host control.
            // Top left of control relative to screen.
            Point controlTopLeft = control.PointToScreen(new Point(0, 0));
            // Convert screen coord to DIP.
            var adjustedX = controlTopLeft.X / (currentDpiX / 96.0);
            var adjustedY = controlTopLeft.Y / (currentDpiY / 96.0);
            return new Point(adjustedX, adjustedY);
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            currentDpiX = newDpi.PixelsPerInchX;
            currentDpiY = newDpi.PixelsPerInchY;

            if (this.ActualWidth > 0 && currentGraph != null)
            {
                currentGraph.UpdateSize(currentDpiX, currentDpiY, CompositionHostElement.ActualWidth, CompositionHostElement.ActualHeight);
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
                if (graphContainer.Children.Count > 0 && currentGraph != null)
                {
                    currentGraph.UpdateGraphData(graphTitle, xAxisTitle, yAxisTitle, customer.Data);
                }
                else
                {
                    BarGraph graph = new BarGraph(compositor, compositionHost.hwndHost, graphTitle, xAxisTitle, yAxisTitle,
                        (float)CompositionHostElement.ActualWidth, (float)CompositionHostElement.ActualHeight, currentDpiX, currentDpiY, customer.Data,
                        true, BarGraph.GraphBarStyle.PerBarLinearGradient,
                        new List<Windows.UI.Color> { Windows.UI.Color.FromArgb(255, 246, 65, 108), Windows.UI.Color.FromArgb(255, 255, 246, 183) });

                    currentGraph = graph;
                    graphContainer.Children.InsertAtTop(graph.GraphRoot);
                }
            }
        }

        private void CompositionHostElement_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (currentGraph != null)
            {
                var currentDpi = VisualTreeHelper.GetDpi(this);
                currentGraph.UpdateSize(currentDpi.PixelsPerInchX, currentDpi.PixelsPerInchY, e.NewSize.Width, e.NewSize.Height);
            }
        }
    }
}
