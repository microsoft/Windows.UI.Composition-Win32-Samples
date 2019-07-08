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
        private readonly IGraphicsEffect _acrylicEffect;
        private readonly CompositionHost _compositionHost;
        private readonly Compositor _compositor;
        private readonly ContainerVisual _containerVisual;

        private static double _rectWidth;
        private static double _rectHeight;
        private static bool _isAcrylicVisible = false;
        private static SpriteVisual _acrylicVisual;
        private static DpiScale _currentDpi;
        private static CompositionGraphicsDevice _compositionGraphicsDevice;
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
            
            // Create effect graph.
            _acrylicEffect = CreateAcrylicEffectGraph();
        }

        private void CompositionHostControl_Loaded(object sender, RoutedEventArgs e)
        {
            _currentDpi = WindowsMedia.VisualTreeHelper.GetDpi(this);

            _rectWidth = CompositionHostElement.ActualWidth / 2;
            _rectHeight = CompositionHostElement.ActualHeight / 2;

            // Get graphics device.
            _compositionGraphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(_compositor, _canvasDevice);

            // Create surface. 
            var noiseDrawingSurface = _compositionGraphicsDevice.CreateDrawingSurface(
                new Windows.Foundation.Size(_rectWidth, _rectHeight),
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                DirectXAlphaMode.Premultiplied);

            // Draw to surface and create surface brush.
            var noiseFilePath = AppDomain.CurrentDomain.BaseDirectory + "Assets\\NoiseAsset_256X256.png";
            LoadSurface(noiseDrawingSurface, noiseFilePath);
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
            var acrylicVisualOffset = new Vector3(
                (float)(_rectWidth * _currentDpi.DpiScaleX) / 2,
                (float)(_rectHeight * _currentDpi.DpiScaleY) / 2,
                0);

            // Create visual and set brush.
            _acrylicVisual = CreateCompositionVisual(acrylicVisualOffset);         
            _acrylicVisual.Brush = CreateAcrylicEffectBrush();
        }
        
        SpriteVisual CreateCompositionVisual(Vector3 offset)
        {
            var visual = _compositor.CreateSpriteVisual();
            visual.Size = new Vector2((float)_rectWidth, (float)_rectHeight);
            visual.Scale = new Vector3((float)_currentDpi.DpiScaleX, (float)_currentDpi.DpiScaleY, 1);
            visual.Offset = offset;

            return visual;
        }

        async void LoadSurface(CompositionDrawingSurface surface, string path)
        {
            // Load from stream.
            var storageFile = await StorageFile.GetFileFromPathAsync(path);
            var stream = await storageFile.OpenAsync(FileAccessMode.Read);
            var bitmap = await CanvasBitmap.LoadAsync(_canvasDevice, stream);

            // Draw to surface.
            using (var ds = CanvasComposition.CreateDrawingSession(surface))
            {
                ds.Clear(Colors.Transparent);

                var rect = new Windows.Foundation.Rect(0, 0, _rectWidth, _rectHeight);
                ds.DrawImage(bitmap, 0, 0, rect);
            }

            stream.Dispose();
            bitmap.Dispose();
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
                                Background = new SaturationEffect
                                {
                                    Saturation = 2,
                                    Source = new GaussianBlurEffect
                                    {
                                        Source = new CompositionEffectSourceParameter("Backdrop"),
                                        BorderMode = EffectBorderMode.Hard,
                                        BlurAmount = 30
                                    },
                                },                                
                                Foreground = new ColorSourceEffect()
                                {
                                    Color = Color.FromArgb(26, 255, 255, 255)
                                }
                            },
                            new ColorSourceEffect
                            {
                                Color = Color.FromArgb(153, 255, 255, 255)
                            }
                        }
                },
                Foreground = new OpacityEffect
                {
                    Opacity = 0.03f,
                    Source = new BorderEffect()
                    {
                        ExtendX = CanvasEdgeBehavior.Wrap,
                        ExtendY = CanvasEdgeBehavior.Wrap,
                        Source = new CompositionEffectSourceParameter("Noise")
                    },
                },
            };
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
            _acrylicVisual.Dispose();
            _noiseSurfaceBrush.Dispose();

            _canvasDevice.Dispose();
            _compositionGraphicsDevice.Dispose();
    }

        internal void ToggleAcrylic()
        {
            // Toggle visibility of acrylic visual by adding or removing from tree.
            if (_isAcrylicVisible)
            {
                _containerVisual.Children.Remove(_acrylicVisual);
            }
            else
            {
                _containerVisual.Children.InsertAtTop(_acrylicVisual);
            }

            _isAcrylicVisible = !_isAcrylicVisible;
        }
    }
}
