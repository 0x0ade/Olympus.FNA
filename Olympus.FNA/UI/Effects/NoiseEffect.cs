using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics.CodeAnalysis;

namespace OlympUI {
    public class NoiseEffect : MiniEffect {

        public static readonly new MiniEffectCache Cache = new(
            $"effects/{nameof(NoiseEffect)}.fxo",
            (gd, _) => new NoiseEffect(gd)
        );

        private EffectParameter MinMaxParam;
        private bool MinMaxValid;
        private Vector4 MinMaxValue = new(0f, 0f, 1f, 1f);
        public Vector2 Min {
            get => new(MinMaxValue.X, MinMaxValue.Y);
            set => _ = (MinMaxValue.X = value.X, MinMaxValue.Y = value.Y, MinMaxValid = false);
        }
        public Vector2 Max {
            get => new(MinMaxValue.Z, MinMaxValue.W);
            set => _ = (MinMaxValue.Z = value.X, MinMaxValue.W = value.Y, MinMaxValid = false);
        }

        private EffectParameter NoiseParam;
        private bool NoiseValid;
        private Vector3 NoiseValue;
        public Vector2 Spread {
            get => new(NoiseValue.X, NoiseValue.Y);
            set => _ = (NoiseValue.X = value.X, NoiseValue.Y = value.Y, NoiseValid = false);
        }
        public float Blend {
            get => NoiseValue.Z;
            set => _ = (NoiseValue.Z = value, NoiseValid = false);
        }

        public NoiseEffect(GraphicsDevice graphicsDevice)
            : base(graphicsDevice, Cache.GetData()) {
            SetupParams();
        }

        protected NoiseEffect(GraphicsDevice graphicsDevice, byte[]? effectCode)
            : base(graphicsDevice, effectCode) {
            SetupParams();
        }

        protected NoiseEffect(NoiseEffect cloneSource)
            : base(cloneSource) {
            SetupParams();
        }

        [MemberNotNull(nameof(MinMaxParam))]
        [MemberNotNull(nameof(NoiseParam))]
        private void SetupParams() {
            MinMaxParam = Parameters[MiniEffectParamCount + 0];
            NoiseParam = Parameters[MiniEffectParamCount + 1];
        }

        public override Effect Clone()
            => new NoiseEffect(this);

        protected override void OnApply() {
            base.OnApply();

            if (!MinMaxValid) {
                MinMaxValid = true;
                MinMaxParam.SetValue(MinMaxValue);
            }

            if (!NoiseValid) {
                NoiseValid = true;
                NoiseParam.SetValue(NoiseValue);
            }
        }

    }
}
