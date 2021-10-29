using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OlympUI;
using Olympus.NativeImpls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace OlympUI {
    public class GraphicsStateSnapshot {

        private readonly GraphicsDevice GraphicsDevice;

        public RenderTargetBinding[] RenderTargets;
        public Texture? Texture;
        public SamplerState SamplerState;
        private bool HasVertexTexture;
        public Texture? VertexTexture;
        public SamplerState? VertexSamplerState;
        public BlendState BlendState;
        public DepthStencilState DepthStencilState;
        public RasterizerState RasterizerState;
        public Rectangle ScissorRectangle;
        public Viewport Viewport;
        public Color BlendFactor;
        public int MultiSampleMask;
        public int ReferenceStencil;

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
