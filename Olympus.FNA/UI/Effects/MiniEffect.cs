using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public class MiniEffect : Effect {

        private static byte[]? Data;

        protected EffectParameter TransformParam;
        protected bool TransformValid;
        protected Matrix TransformValue = Matrix.Identity;
        public Matrix Transform {
            get => TransformValue;
            set => _ = (TransformValue = value, TransformValid = false);
        }

        protected EffectParameter ColorParam;
        protected bool ColorValid;
        protected Vector4 ColorValue = new(1f, 1f, 1f, 1f);
        public Vector4 Color {
            get => ColorValue;
            set => _ = (ColorValue = value, ColorValid = false);
        }

        public MiniEffect(GraphicsDevice graphicsDevice)
            : base(graphicsDevice, Data ??= Assets.OpenData($"effects/{nameof(MiniEffect)}.fxo")) {
            SetupParams();
        }

        protected MiniEffect(GraphicsDevice graphicsDevice, byte[]? effectCode)
            : base(graphicsDevice, effectCode) {
            SetupParams();
        }

        protected MiniEffect(MiniEffect cloneSource)
            : base(cloneSource) {
            SetupParams();
        }

        public override Effect Clone()
            => new MiniEffect(this);

        [MemberNotNull(nameof(TransformParam))]
        [MemberNotNull(nameof(ColorParam))]
        private void SetupParams() {
            TransformParam = Parameters["Transform"];
            ColorParam = Parameters["Color"];
        }

        protected override void OnApply() {
            if (!TransformValid) {
                TransformValid = true;
                TransformParam.SetValue(TransformValue);
            }

            if (!ColorValid) {
                ColorValid = true;
                ColorParam.SetValue(ColorValue);
            }
        }

    }
}
