using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public abstract class Mesh : IDisposable {

        public GraphicsDevice GraphicsDevice;

        public DynamicVertexBuffer? VertexBuffer;
        public DynamicIndexBuffer? IndexBuffer;

        protected bool IsDisposed;
        protected bool ReloadQueued;

        public Mesh(GraphicsDevice graphicsDevice) {
            GraphicsDevice = graphicsDevice;
        }

        ~Mesh() {
            Dispose(false);
        }

        public virtual void QueueReload() => ReloadQueued = true;
        public abstract void Reload();

        public abstract void Draw();

        protected virtual void Dispose(bool disposing) {
            if (IsDisposed)
                return;
            IsDisposed = true;

            VertexBuffer?.Dispose();
            VertexBuffer = null;

            IndexBuffer?.Dispose();
            IndexBuffer = null;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class Mesh<TEffect, TVertex> : Mesh where TEffect : Effect where TVertex : unmanaged {

        public Reloadable<TEffect> Effect;
        public bool DisposeEffect;
        public TVertex[] Vertices = new TVertex[0];
        public int VerticesMax = -1;
        public ushort[] Indices = new ushort[0];
        public int IndicesMax = -1;

        private List<TVertex>? VerticesNext;
        private List<ushort>? IndicesNext;

        public Func<GraphicsDevice, TEffect, bool>? BeforeDraw;

        public bool WireFrame;
        public bool MSAA = true;

        protected int ReloadQueuedDelay = 0;

        public Mesh(GraphicsDevice graphicsDevice, Reloadable<TEffect> effect)
            : base(graphicsDevice) {
            Effect = effect;
        }

        public override unsafe void Reload() {
            TVertex[] vertices = Vertices;
            int verticesMax = VerticesMax;
            ushort[] indices = Indices;
            int indicesMax = IndicesMax;

            if (VerticesNext != null) {
                List<TVertex> next = VerticesNext;
                VerticesNext = null;
                if (vertices.Length > next.Count) {
                    fixed (TVertex* ptr = vertices)
                        for (int i = next.Count - 1; i >= 0; --i)
                            ptr[i] = next[i];
                    VerticesMax = verticesMax = next.Count;
                } else {
                    Vertices = vertices = next.ToArray();
                    VerticesMax = verticesMax = -1;
                }
            }

            if (IndicesNext != null) {
                List<ushort> next = IndicesNext;
                IndicesNext = null;
                if (indices.Length > next.Count) {
                    fixed (ushort* ptr = indices)
                        for (int i = next.Count - 1; i >= 0; --i)
                            ptr[i] = next[i];
                    IndicesMax = indicesMax = next.Count;
                } else {
                    Indices = indices = next.ToArray();
                    IndicesMax = indicesMax = -1;
                }
            }

            if (verticesMax < 0)
                verticesMax = vertices.Length;

            if (indicesMax < 0)
                indicesMax = indices.Length;

            if (VertexBuffer != null && VertexBuffer.VertexCount < verticesMax) {
                VertexBuffer.Dispose();
                VertexBuffer = null;
            }
            if (IndexBuffer != null && IndexBuffer.IndexCount < indicesMax) {
                IndexBuffer.Dispose();
                IndexBuffer = null;
            }

            if (verticesMax == 0 || indicesMax == 0)
                return;

            if (VertexBuffer == null || VertexBuffer.IsDisposed || VertexBuffer.GraphicsDevice != GraphicsDevice)
                VertexBuffer = new(GraphicsDevice, typeof(TVertex), verticesMax, BufferUsage.None);
            VertexBuffer.SetData(vertices, 0, verticesMax);

            if (IndexBuffer == null || IndexBuffer.IsDisposed || IndexBuffer.GraphicsDevice != GraphicsDevice)
                IndexBuffer = new(GraphicsDevice, IndexElementSize.SixteenBits, indicesMax, BufferUsage.None);
            IndexBuffer.SetData(indices, 0, indicesMax);

            ReloadQueued = false;
        }

        public override void QueueReload()
            => _ = (VerticesNext = null, IndicesNext = null, ReloadQueued = true);

        public virtual void QueueReload(List<TVertex> vertices, List<ushort> indices)
            => _ = (VerticesNext = vertices, IndicesNext = indices, ReloadQueued = true);

        protected virtual bool AutoBeforeDraw() {
            GraphicsDevice gd = GraphicsDevice;
            TEffect effect = Effect;

            if (!(BeforeDraw?.Invoke(gd, effect) ?? true))
                return false;

            gd.BlendState = BlendState.AlphaBlend;
            gd.DepthStencilState = DepthStencilState.Default;
            if (WireFrame) {
                gd.RasterizerState = Assets.WireFrame;
            } else if (MSAA) {
                gd.RasterizerState = UI.RasterizerStateCullCounterClockwiseScissoredWithMSAA;
            } else {
                gd.RasterizerState = UI.RasterizerStateCullCounterClockwiseScissoredNoMSAA;
            }
            gd.SamplerStates[0] = SamplerState.LinearClamp;

            return true;
        }

        public override void Draw() {
            // FIXME: Figure out why panels have wrong meshes!
            if (ReloadQueued)
                ReloadQueuedDelay = 3;
            if ((ReloadQueuedDelay > 0 && --ReloadQueuedDelay > 0) ||
                VertexBuffer == null || VertexBuffer.IsDisposed || VertexBuffer.GraphicsDevice != GraphicsDevice ||
                IndexBuffer == null || IndexBuffer.IsDisposed || IndexBuffer.GraphicsDevice != GraphicsDevice)
                Reload();

            int verticesMax = VerticesMax;
            if (verticesMax < 0)
                verticesMax = Vertices.Length;

            int indicesMax = IndicesMax;
            if (indicesMax < 0)
                indicesMax = Indices.Length;
            indicesMax /= 3;

            if (VertexBuffer == null || IndexBuffer == null || verticesMax == 0 || indicesMax == 0)
                return;

            GraphicsDevice gd = GraphicsDevice;
            TEffect effect = Effect;

            AutoBeforeDraw();

            gd.Indices = IndexBuffer;
            gd.SetVertexBuffer(VertexBuffer);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes) {
                pass.Apply();
                gd.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    0,
                    0, verticesMax,
                    0, indicesMax
                );
            }
        }

        protected override void Dispose(bool disposing) {
            if (IsDisposed)
                return;
            base.Dispose(disposing);

            if (DisposeEffect)
                Effect.Dispose();
        }

    }

    public class BasicMesh : Mesh<BasicEffect, VertexPositionColorTexture> {

        public Reloadable<Texture2D> Texture = Assets.White;
        public bool DisposeTexture;
        public Color Color = Color.White;

        public Matrix? Transform;

        private MeshShapes _Shapes;
        public MeshShapes Shapes {
            get => _Shapes;
            set {
                _Shapes = value;
                value.Apply(this);
            }
        }

        public BasicMesh(GraphicsDevice graphicsDevice)
            : base(graphicsDevice, Assets.BasicEffect) {
            _Shapes = new() {
                Mesh = this
            };
        }

        public Matrix CreateTransform()
            => CreateTransform(Vector2.Zero);
        public Matrix CreateTransform(Vector2 offset) {
            Viewport view = GraphicsDevice.Viewport;
            return Matrix.CreateOrthographicOffCenter(
                -offset.X, -offset.X + view.Width,
                -offset.Y + view.Height, -offset.Y,
                view.MinDepth, view.MaxDepth
            );
        }

        protected override bool AutoBeforeDraw() {
            GraphicsDevice gd = GraphicsDevice;
            BasicEffect effect = Effect;

            if (!base.AutoBeforeDraw())
                return false;

            gd.Textures[0] = Texture;
            effect.DiffuseColor = new Vector3(Color.R / 255f, Color.G / 255f, Color.B / 255f);
            effect.Alpha = Color.A / 255f;

            effect.World = Transform ?? CreateTransform();

            return true;
        }

        protected override void Dispose(bool disposing) {
            if (IsDisposed)
                return;
            base.Dispose(disposing);

            if (DisposeTexture)
                Texture.Dispose();
        }

        public void Draw(Matrix? transform) {
            Transform = transform;
            Draw();
        }

    }
}
