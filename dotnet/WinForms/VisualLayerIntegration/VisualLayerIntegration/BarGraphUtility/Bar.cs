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
    class Bar
    {
        private Compositor _compositor;
        private float _height;
        private CompositionRectangleGeometry rectGeometry;
        private CompositionRectangleGeometry rectOutlineGeometry;
        private ShapeVisual shapeVisual;
        private ShapeVisual shapeOutlineVisual;
        private CompositionSpriteShape barVisual;

        public CompositionBrush Brush { get; set; }
        public float Height
        {
            get { return _height; }
            set
            {
                _height = value;
                if (Root != null)
                {
                    rectGeometry.Size = new Vector2(value, Width);
                    rectOutlineGeometry.Size = new Vector2(value, Width);
                }
            }
        }
        public float Width { get; set; }
        public float Value { get; set; }
        public string Label { get; set; }

        public ShapeVisual Root { get; private set; }
        public ShapeVisual OutlineRoot { get; private set; }

        public Bar(Compositor compositor, float maxBarHeight, float height, float width, string label, float value, CompositionBrush brush = null)
        {
            _compositor = compositor;

            Height = height;
            Width = width;
            Value = value;
            Label = label;

            if (brush == null)
            {
                brush = compositor.CreateColorBrush(Colors.Blue);
            }
            Brush = brush;

            CreateBar(maxBarHeight);
        }

        public void UpdateSize(float width, float height)
        {
            Width = width;
            Height = height;

            rectGeometry.Size = new Vector2(Height, Width);
            rectOutlineGeometry.Size = new Vector2(Height, Width);
        }

        public void CreateBar(float maxHeight)
        {
            var strokeThickness = 8;

            // Define shape visual for bar outline.
            shapeOutlineVisual = _compositor.CreateShapeVisual();
            shapeOutlineVisual.Size = new System.Numerics.Vector2(maxHeight, maxHeight);
            shapeOutlineVisual.RotationAngleInDegrees = -90f;

            // Create geometry and shape for the bar outline.
            rectOutlineGeometry = _compositor.CreateRectangleGeometry();
            rectOutlineGeometry.Size = new System.Numerics.Vector2(Height, Width); //reverse width and height since rect will be at a 90* angle
            var barOutlineVisual = _compositor.CreateSpriteShape(rectOutlineGeometry);
            barOutlineVisual.StrokeThickness = (float)strokeThickness;
            barOutlineVisual.StrokeBrush = Brush;

            shapeOutlineVisual.Shapes.Add(barOutlineVisual);

            // Define shape visual.
            shapeVisual = _compositor.CreateShapeVisual();
            shapeVisual.Size = new System.Numerics.Vector2(maxHeight, maxHeight);
            shapeVisual.RotationAngleInDegrees = -90f;

            // Create rectangle geometry and shape for the bar.
            rectGeometry = _compositor.CreateRectangleGeometry();
            // Reverse width and height since rect will be at a 90* angle.
            rectGeometry.Size = new System.Numerics.Vector2(Height, Width); 
            barVisual = _compositor.CreateSpriteShape(rectGeometry);
            barVisual.FillBrush = Brush;

            shapeVisual.Shapes.Add(barVisual);

            Root = shapeVisual;
            OutlineRoot = shapeOutlineVisual;

            // Add implict animation to bar.
            var implicitAnimations = _compositor.CreateImplicitAnimationCollection();
            // Trigger animation when the size property changes. 
            implicitAnimations["Size"] = CreateAnimation();
            rectGeometry.ImplicitAnimations = implicitAnimations;
            rectOutlineGeometry.ImplicitAnimations = implicitAnimations;
        }

        Vector2KeyFrameAnimation CreateAnimation()
        {
            Vector2KeyFrameAnimation animation = _compositor.CreateVector2KeyFrameAnimation();
            animation.InsertExpressionKeyFrame(0f, "this.StartingValue");
            animation.InsertExpressionKeyFrame(1f, "this.FinalValue");
            animation.Target = "Size";
            animation.Duration = TimeSpan.FromSeconds(1);
            return animation;
        }
    }
}
