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

        public BufferTypes BufferType = BufferTypes.Auto;
        public VertexBuffer? VertexBuffer;
        public IndexBuffer? IndexBuffer;

        protected bool IsDisposed;
        protected bool NextQueued;
        protected int ReloadBufferAutoDraws;
        protected int ReloadBufferAutoReloads;

        public Mesh(GraphicsDevice graphicsDevice) {
            GraphicsDevice = graphicsDevice;
        }

        ~Mesh() {
            Dispose(false);
        }

        public virtual void QueueNext() => NextQueued = true;
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

        public enum BufferTypes {
            Auto,
            Static,
            Dynamic,
            None,
        }

    }

    public class Mesh<TEffect, TVertex> : Mesh where TEffect : Effect where TVertex : unmanaged, IVertexType {

        public Reloadable<TEffect> Effect;
        public bool DisposeEffect;
        public TVertex[] Vertices = new TVertex[0];
        public int VerticesMax = -1;
        public short[] Indices = new short[0];
        public int IndicesMax = -1;

        private TVertex[]? VerticesNext;
        private int VerticesMaxNext;
        private short[]? IndicesNext;
        private int IndicesMaxNext;

        public Func<GraphicsDevice, TEffect, bool>? BeforeDraw;

        public bool WireFrame;
        public bool MSAA = true;
        public BlendState BlendState = BlendState.AlphaBlend;
        public SamplerState SamplerState = SamplerState.LinearClamp;

        public Mesh(GraphicsDevice graphicsDevice, Reloadable<TEffect> effect)
            : base(graphicsDevice) {
            Effect = effect;
        }

        protected bool DequeueNext() {
            bool dequeued = false;

            if (VerticesNext != null) {
#if OLYMPUS_MESH_COPYQUEUE
                if (VerticesNext.Length > Vertices.Length)
                    Vertices = new TVertex[VerticesNext.Length];
                Array.Copy(VerticesNext, 0, Vertices, 0, Vertices.Length);
#else
                Vertices = VerticesNext;
#endif
                VerticesMax = VerticesMaxNext;
                VerticesNext = null;
                dequeued = true;
            }

            if (IndicesNext != null) {
#if OLYMPUS_MESH_COPYQUEUE
                if (IndicesNext.Length > Indices.Length)
                    indices = Indices = new ushort[IndicesNext.Length];
                Array.Copy(IndicesNext, 0, Indices, 0, Indices.Length);
#else
                Indices = IndicesNext;
#endif
                IndicesMax = IndicesMaxNext;
                IndicesNext = null;
                dequeued = true;
            }

            return dequeued;
        }

        public override void Reload() {
            if (NextQueued) {
                NextQueued = false;
                DequeueNext();
            }

            TVertex[] vertices = Vertices;
            int verticesMax = VerticesMax;
            short[] indices = Indices;
            int indicesMax = IndicesMax;

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

            if (VertexBuffer == null || VertexBuffer.IsDisposed || VertexBuffer.GraphicsDevice != GraphicsDevice) {
                if (BufferType == BufferTypes.Dynamic) {
                    VertexBuffer = new DynamicVertexBuffer(GraphicsDevice, typeof(TVertex), verticesMax, BufferUsage.None);
                } else {
                    VertexBuffer = new(GraphicsDevice, typeof(TVertex), verticesMax, BufferUsage.None);
                }
            }
            VertexBuffer.SetData(vertices, 0, verticesMax);

            if (IndexBuffer == null || IndexBuffer.IsDisposed || IndexBuffer.GraphicsDevice != GraphicsDevice) {
                if (BufferType == BufferTypes.Dynamic) {
                    IndexBuffer = new DynamicIndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, indicesMax, BufferUsage.None);
                } else {
                    IndexBuffer = new(GraphicsDevice, IndexElementSize.SixteenBits, indicesMax, BufferUsage.None);
                }
            }
            IndexBuffer.SetData(indices, 0, indicesMax);

            NextQueued = false;
        }

        public override void QueueNext()
            => _ = (VerticesNext = null, IndicesNext = null, NextQueued = true);

        public virtual void QueueNext(TVertex[] vertices, int verticesMax, short[] indices, int indicesMax)
            => _ = (VerticesNext = vertices, VerticesMaxNext = verticesMax, IndicesNext = indices, IndicesMaxNext = indicesMax, NextQueued = true);

        protected virtual bool AutoBeforeDraw() {
            GraphicsDevice gd = GraphicsDevice;
            TEffect effect = Effect;

            if (!(BeforeDraw?.Invoke(gd, effect) ?? true))
                return false;

            gd.BlendState = BlendState;
            gd.DepthStencilState = DepthStencilState.Default;
            if (WireFrame) {
                gd.RasterizerState = Assets.WireFrame;
            } else if (MSAA) {
                gd.RasterizerState = UI.RasterizerStateCullCounterClockwiseUnscissoredWithMSAA;
            } else {
                gd.RasterizerState = UI.RasterizerStateCullCounterClockwiseUnscissoredNoMSAA;
            }
            gd.SamplerStates[0] = SamplerState;

            return true;
        }

        public override void Draw() {
            bool forceReload = false;
            bool direct = false;

            if (BufferType == BufferTypes.None) {
                DequeueNext();
                direct = true;

            } else if (BufferType == BufferTypes.Auto) {
                if (NextQueued) {
                    NextQueued = false;
                    DequeueNext();
                    ReloadBufferAutoDraws = 0;
                    if (ReloadBufferAutoReloads < 16) {
                        ReloadBufferAutoReloads++;
                    } else {
                        if (VertexBuffer != null) {
                            VertexBuffer.Dispose();
                            VertexBuffer = null;
                        }
                        if (IndexBuffer != null) {
                            IndexBuffer.Dispose();
                            IndexBuffer = null;
                        }
                    }
                    direct = true;

                } else if (ReloadBufferAutoDraws < 128) {
                    ReloadBufferAutoDraws++;
                    ReloadBufferAutoReloads = 0;
                    if (ReloadBufferAutoDraws == 128) {
                        forceReload = true;
                    } else {
                        direct = true;
                    }
                }
            }

            if (!direct && (
                forceReload || NextQueued ||
                VertexBuffer == null || VertexBuffer.IsDisposed || VertexBuffer.GraphicsDevice != GraphicsDevice ||
                IndexBuffer == null || IndexBuffer.IsDisposed || IndexBuffer.GraphicsDevice != GraphicsDevice))
                Reload();

            int verticesMax = VerticesMax;
            if (verticesMax < 0)
                verticesMax = Vertices.Length;

            int indicesMax = IndicesMax;
            if (indicesMax < 0)
                indicesMax = Indices.Length;
            indicesMax /= 3;

            if (verticesMax == 0 || indicesMax == 0)
                return;

            GraphicsDevice gd = GraphicsDevice;
            TEffect effect = Effect;

            if (direct) {
                AutoBeforeDraw();

                foreach (EffectPass pass in effect.CurrentTechnique.Passes) {
                    pass.Apply();
                    gd.DrawUserIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        Vertices, 0, verticesMax,
                        Indices, 0, indicesMax
                    );
                }

            } else if (VertexBuffer != null && IndexBuffer != null) {
                AutoBeforeDraw();

                gd.Indices = IndexBuffer;
                gd.SetVertexBuffer(VertexBuffer);
                foreach (EffectPass pass in effect.CurrentTechnique.Passes) {
                    pass.Apply();
                    gd.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0, 0, verticesMax,
                        0, indicesMax
                    );
                }
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

    public class BasicMesh : Mesh<MiniEffect, VertexPositionColorTexture> {

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
            : base(graphicsDevice, Assets.MiniEffect) {
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
            MiniEffect effect = Effect;

            if (!base.AutoBeforeDraw())
                return false;

            gd.Textures[0] = Texture;
            effect.Color = Color.ToVector4();
            effect.Transform = Transform ?? CreateTransform();

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
