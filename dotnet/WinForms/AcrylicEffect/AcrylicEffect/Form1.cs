using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Composition;
using System;
using System.Numerics;
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
        private static CompositionGraphicsDevice compositionGraphicsDevice;
        private static CompositionSurfaceBrush noiseSurfaceBrush;

        private ContainerVisual pictureOverlayVisual;

        public Form1() : base()
        {
            InitializeComponent();

            // Get graphics device.
            canvasDevice = CanvasDevice.GetSharedDevice();

            // Create effect graph.
            acrylicEffect = CreateAcrylicEffectGraph();
        }

        public new void Dispose()
        {
            base.Dispose();

            acrylicVisual.Dispose();
            noiseSurfaceBrush.Dispose();

            canvasDevice.Dispose();
            compositionGraphicsDevice.Dispose();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // This container visual is optional - content could go directly into the root
            // containerVisual. This lets you overlay just the picture box, and you could add
            // other container visuals to overlay other areas of the UI.
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
            var noiseFilePath = AppDomain.CurrentDomain.BaseDirectory + "Assets\\NoiseAsset_256X256.png";
            LoadSurface(noiseDrawingSurface, noiseFilePath);
            noiseSurfaceBrush = compositor.CreateSurfaceBrush(noiseDrawingSurface);

            // Add composition content to tree.
            AddCompositionContent();
        }

        public void AddCompositionContent()
        {
            var acrylicVisualOffset = new Vector3(
                (float)rectWidth / 2,
                (float)rectHeight / 2,
                0);

            // Create visual and set brush.
            acrylicVisual = CreateCompositionVisual(acrylicVisualOffset);
            acrylicVisual.Brush = CreateAcrylicEffectBrush();
        }

        private SpriteVisual CreateCompositionVisual(Vector3 offset)
        {
            var visual = compositor.CreateSpriteVisual();
            visual.Size = new Vector2((float)rectWidth, (float)rectHeight);
            visual.Offset = offset;

            return visual;
        }

        private async void LoadSurface(CompositionDrawingSurface surface, string path)
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

        private IGraphicsEffect CreateAcrylicEffectGraph()
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
                                    Color = Windows.UI.Color.FromArgb(26, 255, 255, 255)
                                }
                            },
                            new ColorSourceEffect
                            {
                                Color = Windows.UI.Color.FromArgb(153, 255, 255, 255)
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

        private CompositionEffectBrush CreateAcrylicEffectBrush()
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

        private void ToggleAcrylic()
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

        private void Button1_Click(object sender, EventArgs e)
        {
            ToggleAcrylic();
        }
    }
}
