using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Composition;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Graphics.DirectX;
using Windows.Graphics.Effects;
using Windows.Storage;
using Windows.UI.Composition;

namespace AcrylicEffect
{
    public partial class Form1 : CompositionHostForm
    {
        private readonly CanvasDevice canvasDevice;
        private readonly IGraphicsEffect acrylicEffect;

        private static double rectWidth;
        private static double rectHeight;
        private static bool isAcrylicVisible = false;
        private static SpriteVisual acrylicVisual;
        private static double currentDpi;
        private static CompositionGraphicsDevice compositionGraphicsDevice;
        private static CompositionSurfaceBrush noiseSurfaceBrush;

        private ContainerVisual pictureOverlayVisual;

        public Form1() : base()
        {
            InitializeComponent();

            currentDpi = DeviceDpi / 96;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            ToggleAcrylic();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            pictureOverlayVisual = compositor.CreateContainerVisual();
            pictureOverlayVisual.Offset = new Vector3(pictureBox1.Bounds.Left, pictureBox1.Bounds.Top, 0);
            pictureOverlayVisual.Size = new Vector2(pictureBox1.Width, pictureBox1.Height);
            containerVisual.Children.InsertAtTop(pictureOverlayVisual);

            rectWidth = pictureBox1.Width / 2;
            rectHeight = pictureBox1.Height / 2;

            // Get graphics device.
            compositionGraphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(compositor, canvasDevice);

            // Create surface. 
            var noiseDrawingSurface = compositionGraphicsDevice.CreateDrawingSurface(
                new Windows.Foundation.Size(rectWidth, rectHeight),
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                DirectXAlphaMode.Premultiplied);

            // Draw to surface and create surface brush.
            var noiseFilePath = AppDomain.CurrentDomain.BaseDirectory + "Assets\\noise.png";
            LoadSurface(noiseDrawingSurface, noiseFilePath);
            noiseSurfaceBrush = compositor.CreateSurfaceBrush(noiseDrawingSurface);

            // Add composition content to tree.
            AddCompositionContent();
        }


        public void AddCompositionContent()
        {
            var acrylicVisualOffset = new Vector3(
                (float)(rectWidth * currentDpi) / 2,
                (float)(rectHeight * currentDpi) / 2,
                0);

            // Create visual and set brush.
            acrylicVisual = CreateCompositionVisual(acrylicVisualOffset);
            acrylicVisual.Brush = CreateAcrylicEffectBrush();
        }

        SpriteVisual CreateCompositionVisual(Vector3 offset)
        {
            var visual = compositor.CreateSpriteVisual();
            visual.Size = new Vector2((float)rectWidth, (float)rectHeight);
            visual.Scale = new Vector3((float)currentDpi, (float)currentDpi, 1);
            visual.Offset = offset;

            return visual;
        }

        async void LoadSurface(CompositionDrawingSurface surface, string path)
        {
            // Load from stream.
            var storageFile = await StorageFile.GetFileFromPathAsync(path);
            var stream = await storageFile.OpenAsync(FileAccessMode.Read);
            var bitmap = await CanvasBitmap.LoadAsync(canvasDevice, stream);

            // Draw to surface.
            using (var ds = CanvasComposition.CreateDrawingSession(surface))
            {
                ds.Clear(Windows.UI.Colors.Transparent);

                var rect = new Windows.Foundation.Rect(0, 0, rectWidth, rectHeight);
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
                                    Color = Windows.UI.Color.FromArgb(26, 255, 255, 255)
                                }
                            },
                            new ColorSourceEffect
                            {
                                Name = "OverlayColor",
                                Color = Windows.UI.Color.FromArgb(204, 255, 255, 255)
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

        CompositionEffectBrush CreateAcrylicEffectBrush()
        {
            // Compile the effect.
            var effectFactory = compositor.CreateEffectFactory(acrylicEffect);

            // Create Brush.
            var acrylicEffectBrush = effectFactory.CreateBrush();

            // Set sources.
            var destinationBrush = compositor.CreateBackdropBrush();
            acrylicEffectBrush.SetSourceParameter("Backdrop", destinationBrush);
            acrylicEffectBrush.SetSourceParameter("Noise", noiseSurfaceBrush);

            return acrylicEffectBrush;
        }

        //public void Dispose()
        //{
        //    _acrylicVisual.Dispose();
        //    _noiseSurfaceBrush.Dispose();

        //    _canvasDevice.Dispose();
        //    _compositionGraphicsDevice.Dispose();
        //}

        internal void ToggleAcrylic()
        {
            // Toggle visibility of acrylic visual by adding or removing from tree.
            if (isAcrylicVisible)
            {
                pictureOverlayVisual.Children.Remove(acrylicVisual);
            }
            else
            {
                pictureOverlayVisual.Children.InsertAtTop(acrylicVisual);
            }

            isAcrylicVisible = !isAcrylicVisible;
        }
    }
}
