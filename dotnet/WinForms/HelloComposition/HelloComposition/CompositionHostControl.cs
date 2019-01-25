﻿using System;
using System.Numerics;
using Windows.UI.Composition;

namespace HelloComposition
{
    class CompositionHostControl : CompositionHost
    {

        public CompositionHostControl() : base()
        {
            containerVisual = compositor.CreateContainerVisual();
            Child = containerVisual;
        }

        public void AddElement(float size, float offsetX, float offsetY)
        {
            var visual = compositor.CreateSpriteVisual();
            visual.Size = new Vector2(size, size); // Requires references
            visual.Brush = compositor.CreateColorBrush(GetRandomColor());
            visual.Offset = new Vector3(offsetX, offsetY, 0);
            containerVisual.Children.InsertAtTop(visual);

            AnimateSquare(compositor, visual, offsetX);
        }

        private void AnimateSquare(Compositor compositor, SpriteVisual visual, float offset)
        {
            Vector3KeyFrameAnimation animation = compositor.CreateVector3KeyFrameAnimation();
            float windowBottom = Height - visual.Size.Y;
            animation.InsertKeyFrame(1f, new Vector3(offset, windowBottom, 0f));
            animation.Duration = TimeSpan.FromSeconds(2);
            animation.DelayTime = TimeSpan.FromSeconds(3);
            visual.StartAnimation("Offset", animation);
        }

        private Windows.UI.Color GetRandomColor()
        {
            Random random = new Random();
            byte r = (byte)random.Next(0, 255);
            byte g = (byte)random.Next(0, 255);
            byte b = (byte)random.Next(0, 255);
            return Windows.UI.Color.FromArgb(255, r, g, b);
        }
    }
}
