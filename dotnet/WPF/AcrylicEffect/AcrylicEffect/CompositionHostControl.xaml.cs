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

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Composition;
using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using Windows.Graphics.DirectX;
using Windows.Graphics.Effects;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Composition;
using WindowsMedia = System.Windows.Media;

namespace AcrylicEffect
{
    /// <summary>
    /// Interaction logic for CompositionHostControl.xaml
    /// </summary>
    sealed partial class CompositionHostControl : UserControl, IDisposable
    {
        private readonly CanvasDevice _canvasDevice;
        private readonly IGraphicsEffect _saturationEffect;
        private readonly IGraphicsEffect _acrylicEffect;
        private readonly CompositionHost _compositionHost;
        private readonly Compositor _compositor;
        private readonly ContainerVisual _containerVisual;

        private static double _rectWidth;
        private static double _rectHeight;
        
        private static DpiScale _currentDpi;
        private static CompositionGraphicsDevice _compositionGraphicsDevice;
        private static CompositionSurfaceBrush _coloredSurfaceBrush;
        private static CompositionSurfaceBrush _noiseSurfaceBrush; 

        public CompositionHostControl()
        {
            InitializeComponent();            

            // Get graphics device.
            _canvasDevice = CanvasDevice.GetSharedDevice();

            // Create host and attach root container for hosted tree.
            _compositionHost = new CompositionHost();
            CompositionHostElement.Child = _compositionHost;
            _compositionHost.RegisterForDispose(this);

            _compositor = _compositionHost.Compositor;
            _containerVisual = _compositor.CreateContainerVisual();
            
            // Create effect graphs.
            _saturationEffect = new SaturationEffect
            {
                Saturation = 0.3f,
                Source = new CompositionEffectSourceParameter("mySource")
            };
            _acrylicEffect = CreateAcrylicEffectGraph();
        }

        private void CompositionHostControl_Loaded(object sender, RoutedEventArgs e)
        {
            _currentDpi = WindowsMedia.VisualTreeHelper.GetDpi(this);

            _rectWidth = CompositionHostElement.ActualWidth / 2;
            _rectHeight = CompositionHostElement.ActualHeight / 2;

            // Get graphics device.
            _compositionGraphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(_compositor, _canvasDevice);

            // Create surfaces. 
            var coloredDrawingSurface = _compositionGraphicsDevice.CreateDrawingSurface(
                new Windows.Foundation.Size(_rectWidth, _rectHeight),
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                DirectXAlphaMode.Premultiplied);
            var noiseDrawingSurface = _compositionGraphicsDevice.CreateDrawingSurface(
                new Windows.Foundation.Size(_rectWidth, _rectHeight),
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                DirectXAlphaMode.Premultiplied);

            // Draw to each surface and create surface brushes.
            DrawColorSurface(coloredDrawingSurface);
            _coloredSurfaceBrush = _compositor.CreateSurfaceBrush(coloredDrawingSurface);
            LoadNoiseSurface(noiseDrawingSurface);
            _noiseSurfaceBrush = _compositor.CreateSurfaceBrush(noiseDrawingSurface);

            // Add composition content to tree.
            _compositionHost.SetChild(_containerVisual);
            AddCompositionContent();
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);
            _currentDpi = newDpi;
            Vector3 newScale = new Vector3((float)newDpi.DpiScaleX, (float)newDpi.DpiScaleY, 1);

            // Adjust each child visual scale and offset.
            foreach (SpriteVisual child in _containerVisual.Children)
            {
                child.Scale = newScale;
                var newOffsetX = child.Offset.X * ((float)newDpi.DpiScaleX / (float)oldDpi.DpiScaleX);
                var newOffsetY = child.Offset.Y * ((float)newDpi.DpiScaleY / (float)oldDpi.DpiScaleY);
                child.Offset = new Vector3(newOffsetX, newOffsetY, 0);
            }
        }

        public void AddCompositionContent()
        {
            var desaturatedVisualOffset = new Vector3((float)(_rectWidth * _currentDpi.DpiScaleX), 0, 0);
            var acrylicVisualOffset = new Vector3(
                (float)(_rectWidth * _currentDpi.DpiScaleX) / 2,
                (float)(_rectHeight * _currentDpi.DpiScaleY) / 2,
                0);

            // Create visuals.
            var baselineVisual = CreateCompositionVisual(new Vector3());
            var desaturatedVisual = CreateCompositionVisual(desaturatedVisualOffset);
            var acrylicVisual = CreateCompositionVisual(acrylicVisualOffset);

            // Set brushes for each visual.            
            baselineVisual.Brush = _coloredSurfaceBrush;
            desaturatedVisual.Brush = CreateDesaturationEffectBrush(_coloredSurfaceBrush);
            acrylicVisual.Brush = CreateAcrylicEffectBrush();

            // Add visuals to tree.
            _containerVisual.Children.InsertAtTop(desaturatedVisual);            
            _containerVisual.Children.InsertAtTop(baselineVisual);            
            // Insert acrylic visual on top of others.
            _containerVisual.Children.InsertAtTop(acrylicVisual);
        }

        SpriteVisual CreateCompositionVisual(Vector3 offset)
        {
            var visual = _compositor.CreateSpriteVisual();
            visual.Size = new Vector2((float)_rectWidth, (float)_rectHeight);
            visual.Scale = new Vector3((float)_currentDpi.DpiScaleX, (float)_currentDpi.DpiScaleY, 1);
            visual.Offset = offset;

            return visual;
        }

        void DrawColorSurface(CompositionDrawingSurface surface)
        {
            using (var ds = CanvasComposition.CreateDrawingSession(surface))
            {
                // Clear surface.
                ds.Clear(Colors.Transparent);

                // Draw colored rectangle to surface.
                var rect = new Windows.Foundation.Rect(0, 0, _rectWidth, _rectHeight);
                ds.FillRectangle(rect, Colors.Blue);
                ds.DrawRectangle(rect, Colors.Green);
            }
        }

        async void LoadNoiseSurface(CompositionDrawingSurface surface)
        {
            var noiseFilePath = AppDomain.CurrentDomain.BaseDirectory + "Assets\\noise.png"; 

            // Load from stream.
            var storageFile = await StorageFile.GetFileFromPathAsync(noiseFilePath);
            var stream = await storageFile.OpenAsync(FileAccessMode.Read);
            var bitmap = await CanvasBitmap.LoadAsync(_canvasDevice, stream);

            // Draw to surface.
            using (var ds = CanvasComposition.CreateDrawingSession(surface))
            {
                ds.Clear(Colors.Transparent);

                var rect = new Windows.Foundation.Rect(0, 0, _rectWidth, _rectHeight);
                ds.DrawImage(bitmap, 0, 0, rect);
            }
        }

        IGraphicsEffect CreateAcrylicEffectGraph()
        {
            return new BlendEffect
            {
                Mode = BlendEffectMode.Overlay,
                Background = new CompositeEffect
                {
                    Mode = CanvasComposite.SourceOver,
                    Sources =
                            {
                            new BlendEffect
                            {
                                Mode = BlendEffectMode.Exclusion,
                                Background = new GaussianBlurEffect
                                {
                                    Name = "Blur",
                                    Source = new CompositionEffectSourceParameter("Backdrop"),
                                    BorderMode = EffectBorderMode.Hard,
                                    BlurAmount = 50
                                },
                                Foreground = new ColorSourceEffect()
                                {
                                    Name = "ExclusionColor",
                                    Color = Color.FromArgb(26, 255, 255, 255)
                                }
                            },
                            new ColorSourceEffect
                            {
                                Name = "OverlayColor",
                                Color = Color.FromArgb(204, 255, 255, 255)
                            }
                        }
                },
                Foreground = new OpacityEffect
                {
                    Name = "Noise",
                    Opacity = 0.04f,
                    Source = new CompositionEffectSourceParameter("Noise")
                }
            };
        }

        CompositionEffectBrush CreateDesaturationEffectBrush(CompositionBrush surfaceBrush)
        {
            // Compile the effect.
            var effectFactory = _compositor.CreateEffectFactory(_saturationEffect);

            // Create Brush and set source.
            var effectBrush = effectFactory.CreateBrush();
            effectBrush.SetSourceParameter("mySource", surfaceBrush);

            return effectBrush;
        }

        CompositionEffectBrush CreateAcrylicEffectBrush()
        {
            // Compile the effect.
            var effectFactory = _compositor.CreateEffectFactory(_acrylicEffect);

            // Create Brush.
            var acrylicEffectBrush = effectFactory.CreateBrush();

            // Set sources.
            var destinationBrush = _compositor.CreateBackdropBrush();
            acrylicEffectBrush.SetSourceParameter("Backdrop", destinationBrush);
            acrylicEffectBrush.SetSourceParameter("Noise", _noiseSurfaceBrush);

            return acrylicEffectBrush;
        }

        public void Dispose()
        {

            _noiseSurfaceBrush.Dispose();
            _coloredSurfaceBrush.Dispose();
        }
    }
}
