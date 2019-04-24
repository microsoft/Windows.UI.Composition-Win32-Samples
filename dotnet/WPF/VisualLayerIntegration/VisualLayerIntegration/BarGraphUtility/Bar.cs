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
using System.Numerics;
using Windows.UI;
using Windows.UI.Composition;

namespace BarGraphUtility
{
    sealed class Bar
    {
        private Compositor _compositor;
        private float _height;
        private CompositionRectangleGeometry _rectGeometry;
        private CompositionRectangleGeometry _rectOutlineGeometry;

        private const float _strokeThickness = 8;

        public Bar(Compositor compositor, float maxBarHeight, float height, float width, string label, double value, CompositionBrush brush = null)
        {
            _compositor = compositor;

            Height = height;
            Width = width;
            Value = value;
            Label = label;
            
            var barBrush = brush ?? compositor.CreateColorBrush(Colors.Blue);
            
            // Define shape visual for bar outline.
            var shapeOutlineVisual = _compositor.CreateShapeVisual();
            shapeOutlineVisual.Size = new Vector2(maxBarHeight, maxBarHeight);
            shapeOutlineVisual.RotationAngleInDegrees = -90f;

            // Create geometry and shape for the bar outline.
            _rectOutlineGeometry = _compositor.CreateRectangleGeometry();
            // Reverse width and height since rect will be at a 90* angle.
            _rectOutlineGeometry.Size = new Vector2(Height, Width);
            var barOutlineVisual = _compositor.CreateSpriteShape(_rectOutlineGeometry);
            barOutlineVisual.StrokeThickness = _strokeThickness;
            barOutlineVisual.StrokeBrush = barBrush;

            shapeOutlineVisual.Shapes.Add(barOutlineVisual);

            // Define shape visual. 
            var shapeVisual = _compositor.CreateShapeVisual();
            shapeVisual.Size = new Vector2(maxBarHeight, maxBarHeight);
            shapeVisual.RotationAngleInDegrees = -90f;

            // Create rectangle geometry and shape for the bar.
            _rectGeometry = _compositor.CreateRectangleGeometry();
            // Reverse width and height since rect will be at a 90* angle.
            _rectGeometry.Size = new Vector2(Height, Width);

            var barVisual = _compositor.CreateSpriteShape(_rectGeometry);
            barVisual.FillBrush = barBrush;

            shapeVisual.Shapes.Add(barVisual);

            Root = shapeVisual;
            OutlineRoot = shapeOutlineVisual;

            // Add implict animation to bar.
            var implicitAnimations = _compositor.CreateImplicitAnimationCollection();
            // Trigger animation when the size property changes. 
            implicitAnimations["Size"] = CreateAnimation();
            _rectGeometry.ImplicitAnimations = implicitAnimations;
            _rectOutlineGeometry.ImplicitAnimations = implicitAnimations;
        }

        public float Height
        {
            get { return _height; }
            set
            {
                _height = value;
                if (Root != null)
                {
                    _rectGeometry.Size = new Vector2(value, Width);
                    _rectOutlineGeometry.Size = new Vector2(value, Width);
                }
            }
        }

        public float Width { get; private set; }
        public double Value { get; private set; }
        public string Label { get; private set; }

        public ShapeVisual Root { get; private set; }
        public ShapeVisual OutlineRoot { get; private set; }

        public void UpdateSize(float width, float height)
        {
            Width = width;
            Height = height;

            _rectGeometry.Size = new Vector2(Height, Width);
            _rectOutlineGeometry.Size = new Vector2(Height, Width);
        }

        private Vector2KeyFrameAnimation CreateAnimation()
        {
            var animation = _compositor.CreateVector2KeyFrameAnimation();
            animation.InsertExpressionKeyFrame(0f, "this.StartingValue");
            animation.InsertExpressionKeyFrame(1f, "this.FinalValue");
            animation.Target = "Size";
            animation.Duration = TimeSpan.FromSeconds(1);
            return animation;
        }
    }
}
