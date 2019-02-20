using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Composition;

namespace BarGraphUtility
{
    /*
     * Create brushes to fill the bars
     */ 
    public class BarBrushHelper
    {
        private Compositor compositor;
        private Random rand;

        public BarBrushHelper(Compositor c)
        {
            compositor = c;
            rand = new Random();
        }

        internal CompositionBrush[] GenerateSingleColorBrush(int numBrushes, Color color)
        {            
            var brushes = new CompositionBrush[numBrushes];
            for(int i=0; i<numBrushes; i++)
            {
                brushes[i] = compositor.CreateColorBrush(color);
            }

            return brushes;
        }


        internal CompositionBrush[] GenerateRandomColorBrushes(int numBrushes)
        {
            int max = byte.MaxValue + 1; // 256
            var brushes = new CompositionBrush[numBrushes];
            for (int i = 0; i < numBrushes; i++)
            {
                byte r = Convert.ToByte(rand.Next(max));
                byte g = Convert.ToByte(rand.Next(max));
                byte b = Convert.ToByte(rand.Next(max));
                Color c = Color.FromArgb(Convert.ToByte(255), r, g, b);
                brushes[i] = compositor.CreateColorBrush(c);
            }

            return brushes;
        }

        internal CompositionBrush[] GeneratePerBarLinearGradient(int numBrushes, List<Color> colors)
        {
            var lgb = compositor.CreateLinearGradientBrush();
            lgb.RotationAngleInDegrees = 45;

            int i = 0;
            foreach (Color color in colors)
            {
                float offset =(float)(i / ((float)colors.Count-1));
                var stop = compositor.CreateColorGradientStop(offset, color);

                i++;
                lgb.ColorStops.Add(stop);
            }

            var brushes = new CompositionBrush[numBrushes];
            for (int j = 0; j < numBrushes; j++)
            {
                brushes[j] = lgb;
            }
            return brushes;
        }

        internal CompositionBrush[] GenerateAmbientAnimatingPerBarLinearGradient(int numBrushes, List<Color> colors)
        {
            var lgb = compositor.CreateLinearGradientBrush();
            lgb.RotationAngleInDegrees = 45;

            int i = 0;
            var animationDuration = 100;
            foreach (Color color in colors)
            {
                float offset = (float)(i / ((float)colors.Count - 1));

                var stop = compositor.CreateColorGradientStop(offset, color);
                lgb.ColorStops.Add(stop);
                InitLGBAnimation(stop, animationDuration, 1.0f);

                // Create a second mirrored stop for all colors but the first
                if (offset > 0)
                {
                    var stop2 = compositor.CreateColorGradientStop(-offset, color);
                    lgb.ColorStops.Add(stop2);
                    InitLGBAnimation(stop2, animationDuration, 1.0f);
                }

                i++;
            }
            
            var brushes = new CompositionBrush[numBrushes];
            for (int j = 0; j < numBrushes; j++)
            {
                brushes[j] = lgb;
            }

            return brushes;
        }

        private void InitLGBAnimation(CompositionColorGradientStop stop, int duration, float offsetAdjustment)
        {
            var animateStop = compositor.CreateScalarKeyFrameAnimation();
            animateStop.InsertKeyFrame(0.0f, stop.Offset);
            animateStop.InsertKeyFrame(0.5f, stop.Offset + offsetAdjustment);
            animateStop.InsertKeyFrame(1.0f, stop.Offset);
            animateStop.IterationBehavior = AnimationIterationBehavior.Forever;
            animateStop.Duration = TimeSpan.FromSeconds(duration);
            stop.StartAnimation(nameof(stop.Offset), animateStop);
        }

    }
}