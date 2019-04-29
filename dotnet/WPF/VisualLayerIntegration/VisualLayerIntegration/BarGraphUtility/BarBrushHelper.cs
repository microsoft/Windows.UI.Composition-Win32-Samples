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

using System;
using Windows.UI;
using Windows.UI.Composition;

namespace BarGraphUtility
{
    // Create brushes to fill the bars. 
    sealed class BarBrushHelper
    {
        private readonly Compositor _compositor;
        private readonly Random _rand = new Random();

        public BarBrushHelper(Compositor c)
        {
            _compositor = c;            
        }

        internal CompositionBrush CreateColorBrush(Color color)
        {
            return _compositor.CreateColorBrush(color);
        }

        internal CompositionBrush[] CreateRandomColorBrushes(int numBrushes)
        {
            var brushes = new CompositionBrush[numBrushes];
            for (var i = 0; i < numBrushes; i++)
            {
                var rgb = new byte[3];
                _rand.NextBytes(rgb);
                var c = Color.FromArgb(255, rgb[0], rgb[1], rgb[2]);
                brushes[i] = _compositor.CreateColorBrush(c);
            }

            return brushes;
        }

        internal CompositionBrush CreateLinearGradientBrushes(Color[] colors)
        {
            var linearGradientBrush = _compositor.CreateLinearGradientBrush();
            linearGradientBrush.RotationAngleInDegrees = 45;

            var i = 0;
            foreach (Color color in colors)
            {
                var offset = i / ((float)colors.Length - 1);
                var stop = _compositor.CreateColorGradientStop(offset, color);

                i++;
                linearGradientBrush.ColorStops.Add(stop);
            }

            return linearGradientBrush;
        }

        internal CompositionBrush CreateAnimatedLinearGradientBrushes(Color[] colors)
        {
            var linearGradientBrush = _compositor.CreateLinearGradientBrush();
            linearGradientBrush.RotationAngleInDegrees = 45;

            // Set long animation duration so that animated effect is slow
            var animationDuration = TimeSpan.FromSeconds(100);

            var i = 0;
            foreach (Color color in colors)
            {
                var offset = i / ((float)colors.Length - 1);

                var stop = _compositor.CreateColorGradientStop(offset, color);
                linearGradientBrush.ColorStops.Add(stop);
                AnimateLinearGradientstopOffset(stop, animationDuration, 1);

                // Create a second mirrored stop for all colors but the first.
                if (offset > 0)
                {
                    var stop2 = _compositor.CreateColorGradientStop(-offset, color);
                    linearGradientBrush.ColorStops.Add(stop2);
                    AnimateLinearGradientstopOffset(stop2, animationDuration, 1);
                }

                i++;
            }

            return linearGradientBrush;
        }

        private void AnimateLinearGradientstopOffset(CompositionColorGradientStop stop, TimeSpan duration, float offsetAdjustment)
        {
            var animateStop = _compositor.CreateScalarKeyFrameAnimation();
            animateStop.InsertKeyFrame(0, stop.Offset);
            animateStop.InsertKeyFrame(0.5f, stop.Offset + offsetAdjustment);
            animateStop.InsertKeyFrame(1, stop.Offset);
            animateStop.IterationBehavior = AnimationIterationBehavior.Forever;
            animateStop.Duration = duration;
            stop.StartAnimation(nameof(stop.Offset), animateStop);
        }
    }
}