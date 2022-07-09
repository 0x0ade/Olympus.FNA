using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace OlympUI {
    public readonly struct GraphicsStateSnapshot {

        private readonly GraphicsDevice GraphicsDevice;

        public readonly RenderTargetBinding[] RenderTargets = null!;
        public readonly Texture? Texture;
        public readonly SamplerState SamplerState = null!;
        private readonly bool HasVertexTexture;
        public readonly Texture? VertexTexture;
        public readonly SamplerState? VertexSamplerState;
        public readonly BlendState BlendState = null!;
        public readonly DepthStencilState DepthStencilState = null!;
        public readonly RasterizerState RasterizerState = null!;
        public readonly Rectangle ScissorRectangle;
        public readonly Viewport Viewport;
        public readonly Color BlendFactor;
        public readonly int MultiSampleMask;
        public readonly int ReferenceStencil;

        public GraphicsStateSnapshot(GraphicsDevice graphicsDevice) {
            GraphicsDevice = graphicsDevice;
            RenderTargets = graphicsDevice.GetRenderTargets();
            Texture = graphicsDevice.Textures[0];
            SamplerState = graphicsDevice.SamplerStates[0];
            try {
                VertexTexture = graphicsDevice.VertexTextures[0];
                VertexSamplerState = graphicsDevice.VertexSamplerStates[0];
                HasVertexTexture = true;
            } catch (IndexOutOfRangeException) {
                VertexTexture = null;
                VertexSamplerState = null;
                HasVertexTexture = false;
            }
            BlendState = graphicsDevice.BlendState;
            DepthStencilState = graphicsDevice.DepthStencilState;
            RasterizerState = graphicsDevice.RasterizerState;
            ScissorRectangle = graphicsDevice.ScissorRectangle;
            Viewport = graphicsDevice.Viewport;
            BlendFactor = graphicsDevice.BlendFactor;
            MultiSampleMask = graphicsDevice.MultiSampleMask;
            ReferenceStencil = graphicsDevice.ReferenceStencil;
        }

        public void Apply() {
            GraphicsDevice graphicsDevice = GraphicsDevice;
            graphicsDevice.Textures[0] = null;
            if (HasVertexTexture) {
                graphicsDevice.VertexTextures[0] = null;
                graphicsDevice.VertexSamplerStates[0] = VertexSamplerState;
            }
            if (RenderTargets.Length == 0)
                graphicsDevice.SetRenderTarget(null);
            else
                graphicsDevice.SetRenderTargets(RenderTargets);
            graphicsDevice.Textures[0] = Texture;
            graphicsDevice.SamplerStates[0] = SamplerState;
            if (HasVertexTexture) {
                graphicsDevice.VertexTextures[0] = VertexTexture;
                graphicsDevice.VertexSamplerStates[0] = VertexSamplerState;
            }
            graphicsDevice.BlendState = BlendState;
            graphicsDevice.DepthStencilState = DepthStencilState;
            graphicsDevice.RasterizerState = RasterizerState;
            graphicsDevice.ScissorRectangle = ScissorRectangle;
            graphicsDevice.Viewport = Viewport;
            graphicsDevice.BlendFactor = BlendFactor;
            graphicsDevice.MultiSampleMask = MultiSampleMask;
            graphicsDevice.ReferenceStencil = ReferenceStencil;
        }

    }
}
