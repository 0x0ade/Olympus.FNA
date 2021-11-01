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
    public class MaskEffect : MiniEffect {

        private static byte[]? Data;

        protected EffectParameter MaskXYWHParam;
        protected bool MaskXYWHValid;
        protected Vector4 MaskXYWHValue = new(0f, 0f, 1f, 1f);
        public Vector4 MaskXYWH {
            get => MaskXYWHValue;
            set => _ = (MaskXYWHValue = value, MaskXYWHValid = false);
        }

        public MaskEffect(GraphicsDevice graphicsDevice)
            : base(graphicsDevice, Data ??= Assets.OpenData($"effects/{nameof(MaskEffect)}.fxo")) {
            SetupParams();
        }

        protected MaskEffect(GraphicsDevice graphicsDevice, byte[]? effectCode)
            : base(graphicsDevice, effectCode) {
            SetupParams();
        }

        protected MaskEffect(MaskEffect cloneSource)
            : base(cloneSource) {
            SetupParams();
        }

        [MemberNotNull(nameof(MaskXYWHParam))]
        private void SetupParams() {
            MaskXYWHParam = Parameters["MaskXYWH"];
        }

        public override Effect Clone()
            => new MaskEffect(this);

        protected override void OnApply() {
            base.OnApply();

            if (!MaskXYWHValid) {
                MaskXYWHValid = true;
                MaskXYWHParam.SetValue(MaskXYWHValue);
            }
        }

    }
}
