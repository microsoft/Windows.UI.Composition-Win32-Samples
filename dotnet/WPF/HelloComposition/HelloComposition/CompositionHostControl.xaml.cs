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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Windows.UI.Composition;
using Windows.UI.Composition.Scenes;
using Windows.Foundation;
using System.Runtime.InteropServices;

namespace HelloComposition
{
    [ComImport,
Guid("5b0d3235-4dba-4d44-865e-8f1d0e4fd04d"),
InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMemoryBufferByteAccess
    {
        unsafe void GetBuffer(byte** bytes, uint* capacity);
    }

    /// <summary>
    /// Interaction logic for CompositionHostControl.xaml
    /// </summary>
    public partial class CompositionHostControl : UserControl
    {
        CompositionHost compositionHost;
        Compositor compositor;
        Windows.UI.Composition.ContainerVisual containerVisual;
        DpiScale currentDpi;
 
        public CompositionHostControl()
        {
            InitializeComponent();
            Loaded += CompositionHostControl_Loaded;
        }

        private void CompositionHostControl_Loaded(object sender, RoutedEventArgs e)
        {
            // If the user changes the DPI scale setting for the screen the app is on,
            // the CompositionHostControl is reloaded. Don't redo this set up if it's
            // already been done.
            if (compositionHost is null)
            {
                currentDpi = VisualTreeHelper.GetDpi(this);

                compositionHost = new CompositionHost(CompositionHostElement.ActualHeight, CompositionHostElement.ActualWidth);
                CompositionHostElement.Child = compositionHost;
                compositor = compositionHost.Compositor;
                containerVisual = compositor.CreateContainerVisual();
                compositionHost.Child = containerVisual;
            }
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);
            currentDpi = newDpi;
            Vector3 newScale = new Vector3((float)newDpi.DpiScaleX, (float)newDpi.DpiScaleY, 1);

            foreach (SpriteVisual child in containerVisual.Children)
            {
                child.Scale = newScale;
                var newOffsetX = child.Offset.X * ((float)newDpi.DpiScaleX / (float)oldDpi.DpiScaleX);
                var newOffsetY = child.Offset.Y * ((float)newDpi.DpiScaleY / (float)oldDpi.DpiScaleY);
                child.Offset = new Vector3(newOffsetX, newOffsetY, 1);

                // Adjust animations for DPI change.
                AnimateSquare(child, 0);
            }
        }

        public void AddElement(float size, float offsetX, float offsetY)
        {
            var visual = compositor.CreateSpriteVisual();
            visual.Size = new Vector2(size, size);
            visual.Scale = new Vector3((float)currentDpi.DpiScaleX, (float)currentDpi.DpiScaleY, 1);
            visual.Brush = compositor.CreateColorBrush(GetRandomColor());
            visual.Offset = new Vector3(offsetX * (float)currentDpi.DpiScaleX, offsetY * (float)currentDpi.DpiScaleY, 0);

            containerVisual.Children.InsertAtTop(visual);

            AnimateSquare(visual, 3);

            var sceneVisual = SceneVisual.Create(compositor);

            containerVisual.Children.InsertAtTop(sceneVisual);

            var sceneMesh = SceneMesh.Create(compositor);
            var rootNode = SceneNode.Create(compositor);

            var meshRenderer = SceneMeshRendererComponent.Create(compositor);
            var material = SceneMetallicRoughnessMaterial.Create(compositor);

            material.BaseColorFactor = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);

            meshRenderer.Mesh = sceneMesh;
            meshRenderer.Material = material;

            FillMeshWithSphere(sceneMesh, 10.0f, 100);

            sceneVisual.Offset = new Vector3(offsetX * (float)currentDpi.DpiScaleX, offsetY * (float)currentDpi.DpiScaleY, 0);
            sceneVisual.Root = rootNode;

            rootNode.Components.Add(meshRenderer);
        }

        private void AnimateSquare(SpriteVisual visual, int delay)
        {
            float offsetX = (float)(visual.Offset.X); // Already adjusted for DPI.

            // Adjust values for DPI scale, then find the Y offset that aligns the bottom of the square
            // with the bottom of the host container. This is the value to animate to.
            var hostHeightAdj = CompositionHostElement.ActualHeight * currentDpi.DpiScaleY;
            var squareSizeAdj = visual.Size.Y * currentDpi.DpiScaleY;
            float bottom = (float)(hostHeightAdj - squareSizeAdj);

            // Create the animation only if it's needed.
            if (visual.Offset.Y != bottom)
            {
                Vector3KeyFrameAnimation animation = compositor.CreateVector3KeyFrameAnimation();
                animation.InsertKeyFrame(1f, new Vector3(offsetX, bottom, 0f));
                animation.Duration = TimeSpan.FromSeconds(2);
                animation.DelayTime = TimeSpan.FromSeconds(delay);
                visual.StartAnimation("Offset", animation);
            }
        }

        private Windows.UI.Color GetRandomColor()
        {
            Random random = new Random();
            byte r = (byte)random.Next(0, 255);
            byte g = (byte)random.Next(0, 255);
            byte b = (byte)random.Next(0, 255);
            return Windows.UI.Color.FromArgb(255, r, g, b);
        }

        private static void FillMeshWithSphere(SceneMesh mesh, float radius = 200, int numApproxVertices = 9)
        {
            //
            // Use Longitude and Latitude to create a mesh
            //
            // Latitude of 0 is top of sphere
            // Latitude of PI is bottom of sphere
            //
            // Longitude of 0 is x = 1, y = 0
            //

            int numLevels = (int)Math.Ceiling(Math.Sqrt(numApproxVertices));

            if (numLevels < 3)
            {
                numLevels = 3;
            }

            float[] sphereHeights = new float[numLevels];
            float[] sphereMult = new float[numLevels];
            float[] sphereLongX = new float[numLevels];
            float[] sphereLongY = new float[numLevels];

            float flStart = (float)Math.PI / 2;
            float flStop = (float)-Math.PI / 2;

            for (int latLevel = 0; latLevel < numLevels; latLevel++)
            {
                float interp = ((float)latLevel) / (float)(numLevels - 1);

                sphereHeights[latLevel] = (float)Math.Sin((1.0f - interp) * flStart + interp * flStop);
                sphereMult[latLevel] = (float)Math.Cos((1.0f - interp) * flStart + interp * flStop);
            }

            flStart = 0.0f;
            flStop = 2.0f * (float)Math.PI;

            for (int longLevel = 0; longLevel < numLevels; longLevel++)
            {
                float interp = ((float)longLevel) / (float)(numLevels);

                sphereLongX[longLevel] = (float)Math.Cos((1.0f - interp) * flStart + interp * flStop);
                sphereLongY[longLevel] = (float)Math.Sin((1.0f - interp) * flStart + interp * flStop);
            }

            // Start with the triangles coming from the north/south pole of the sphere
            int numVertices = 2;
            int numTriangles = 2 * numLevels;
            int numInteriorRows = 0;

            numVertices += numLevels * (numLevels - 2);

            numInteriorRows = numLevels - 2;

            if (numLevels > 3)
            {
                numTriangles += numInteriorRows * numLevels * 2;
            }

            var spherePositions = new float[numVertices * 3];

            int currentVertex = 0;

            // North Pole
            spherePositions[currentVertex * 3] = 0.0f;
            spherePositions[currentVertex * 3 + 1] = 0.0f;
            spherePositions[currentVertex * 3 + 2] = sphereHeights[0];

            currentVertex++;

            for (int intRow = 0; intRow < numInteriorRows; intRow++)
            {
                for (int iCurLong = 0; iCurLong < numLevels; iCurLong++)
                {
                    spherePositions[currentVertex * 3] = sphereLongX[iCurLong] * sphereMult[intRow + 1];
                    spherePositions[currentVertex * 3 + 1] = sphereLongY[iCurLong] * sphereMult[intRow + 1];
                    spherePositions[currentVertex * 3 + 2] = sphereHeights[intRow + 1];


                    currentVertex++;
                }
            }

            spherePositions[currentVertex * 3] = 0.0f;
            spherePositions[currentVertex * 3 + 1] = 0.0f;
            spherePositions[currentVertex * 3 + 2] = sphereHeights[numLevels - 1];

            currentVertex++;

            for (int i = 0; i < numVertices * 3; i++)
            {
                spherePositions[i] *= radius;
            }

            mesh.PrimitiveTopology = Windows.Graphics.DirectX.DirectXPrimitiveTopology.TriangleList;

            mesh.FillMeshAttribute(
                SceneAttributeSemantic.Vertex,
                Windows.Graphics.DirectX.DirectXPixelFormat.R32G32B32Float,
                CreateMemoryBufferWithArray(spherePositions));

            float[] normals = new float[currentVertex * 3];

            for (int iNormal = 0; iNormal < currentVertex; iNormal++)
            {
                float length = (float)Math.Sqrt(spherePositions[iNormal * 3] * spherePositions[iNormal * 3] + spherePositions[iNormal * 3 + 1] * spherePositions[iNormal * 3 + 1] + spherePositions[iNormal * 3 + 2] * spherePositions[iNormal * 3 + 2]);

                normals[iNormal * 3] = spherePositions[iNormal * 3] / length;
                normals[iNormal * 3 + 1] = spherePositions[iNormal * 3 + 1] / length;
                normals[iNormal * 3 + 2] = spherePositions[iNormal * 3 + 2] / length;
            }

            mesh.FillMeshAttribute(
                SceneAttributeSemantic.Normal,
                Windows.Graphics.DirectX.DirectXPixelFormat.R32G32B32Float,
                CreateMemoryBufferWithArray(normals));

            mesh.PrimitiveTopology = Windows.Graphics.DirectX.DirectXPrimitiveTopology.TriangleList;

            UInt32[] colors = new UInt32[currentVertex];

            for (int i = 0; i < currentVertex; i++)
            {
                colors[i] = 0xffffffff;
            }

            mesh.FillMeshAttribute(
                SceneAttributeSemantic.Color,
                Windows.Graphics.DirectX.DirectXPixelFormat.R32UInt,
                CreateMemoryBufferWithArray(colors));

            UInt16[] indices = new UInt16[numTriangles * 3];

            int offsetToRowVertex = 1;

            int currentIndex = 0;

            // Add Triangles connecting to the north pole
            for (int i = 0; i < numLevels; i++)
            {
                indices[currentIndex++] = (UInt16)(offsetToRowVertex + i);
                indices[currentIndex++] = (UInt16)(0);
                indices[currentIndex++] = (UInt16)(((i + 1) % numLevels) + offsetToRowVertex);
            }

            for (int iInterior = 0; iInterior < numInteriorRows - 1; iInterior++)
            {
                offsetToRowVertex = 1 + (iInterior * numLevels);
                int offsetToNextRowVertex = 1 + (iInterior + 1) * numLevels;

                for (int i = 0; i < numLevels; i++)
                {
                    indices[currentIndex++] = (UInt16)(offsetToNextRowVertex + i);
                    indices[currentIndex++] = (UInt16)(offsetToRowVertex + i);
                    indices[currentIndex++] = (UInt16)(((i + 1) % numLevels) + offsetToNextRowVertex);

                    indices[currentIndex++] = (UInt16)(offsetToRowVertex + i);
                    indices[currentIndex++] = (UInt16)(((i + 1) % numLevels) + offsetToRowVertex);
                    indices[currentIndex++] = (UInt16)(((i + 1) % numLevels) + offsetToNextRowVertex);
                }
            }

            offsetToRowVertex = 1 + numLevels * (numInteriorRows - 1);

            // Add Triangles connecting to the south pole
            for (int i = 0; i < numLevels; i++)
            {
                indices[currentIndex++] = (UInt16)(numVertices - 1);
                indices[currentIndex++] = (UInt16)(offsetToRowVertex + i);
                indices[currentIndex++] = (UInt16)(((i + 1) % numLevels) + offsetToRowVertex);
            }

            mesh.FillMeshAttribute(
                SceneAttributeSemantic.Index,
                Windows.Graphics.DirectX.DirectXPixelFormat.R16UInt,
                CreateMemoryBufferWithArray(indices));
        }

        private static MemoryBuffer CreateMemoryBufferWithArray(float[] srcArray)
        {
            Func<float, byte[]> serializer = value => BitConverter.GetBytes(value);

            return CreateMemoryBufferWithArrayWorker(srcArray, serializer, (uint)(srcArray.Length * sizeof(float)));
        }

        private static MemoryBuffer CreateMemoryBufferWithArray(UInt16[] srcArray)
        {
            Func<UInt16, byte[]> serializer = value => BitConverter.GetBytes(value);
            return CreateMemoryBufferWithArrayWorker(srcArray, serializer, (uint)(srcArray.Length * sizeof(UInt16)));
        }

        private static MemoryBuffer CreateMemoryBufferWithArray(UInt32[] srcArray)
        {
            Func<UInt32, byte[]> serializer = value => BitConverter.GetBytes(value);
            return CreateMemoryBufferWithArrayWorker(srcArray, serializer, (uint)(srcArray.Length * sizeof(UInt32)));
        }

        private static MemoryBuffer CreateMemoryBufferWithArrayWorker<T>(
            T[] srcArray,
            Func<T, byte[]> serializer,
            uint bufferSize)
        {
            MemoryBuffer mb = new MemoryBuffer(bufferSize);
            IMemoryBufferReference mbr = mb.CreateReference();
            IMemoryBufferByteAccess mba = (IMemoryBufferByteAccess)mbr;

            unsafe
            {
                byte* destBytes = null;
                uint destCapacity;
                mba.GetBuffer(&destBytes, &destCapacity);

                int iWrite = 0;
                foreach (T srcValue in srcArray)
                {
                    foreach (byte srcByte in serializer(srcValue))
                    {
                        destBytes[iWrite++] = srcByte;
                    }
                }
            }

            return mb;
        }
    }
}
