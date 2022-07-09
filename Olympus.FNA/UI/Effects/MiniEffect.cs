using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics.CodeAnalysis;

namespace OlympUI {
    public class MiniEffect : Effect {

        protected const int MiniEffectParamCount = 2;

        public static readonly MiniEffectCache Cache = new(
            $"effects/{nameof(MiniEffect)}.fxo",
            (gd, _) => new MiniEffect(gd)
        );

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
            : base(graphicsDevice, Cache.GetData()) {
            Name = GetType().Name;
            SetupParams();
        }

        protected MiniEffect(GraphicsDevice graphicsDevice, byte[]? effectCode)
            : base(graphicsDevice, effectCode) {
            Name = GetType().Name;
            SetupParams();
        }

        protected MiniEffect(MiniEffect cloneSource)
            : base(cloneSource) {
            Name = GetType().Name;
            SetupParams();
        }

        public override Effect Clone()
            => new MiniEffect(this);

        [MemberNotNull(nameof(TransformParam))]
        [MemberNotNull(nameof(ColorParam))]
        private void SetupParams() {
            TransformParam = Parameters[0];
            ColorParam = Parameters[1];
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

    public class MiniEffectCache {

        protected static readonly object?[] DefaultWarmupKeys = { null };

        public readonly string Name;

        private readonly string? Path;
        private readonly Func<GraphicsDevice, object?, MiniEffect>? Gen;

        private byte[]? Data;
        private IReloadable<MiniEffect, NullMeta>? Effect;

        protected MiniEffectCache(string name) {
            Name = name;
        }

        public MiniEffectCache(string path, Func<GraphicsDevice, object?, MiniEffect> gen) {
            Name = System.IO.Path.GetFileNameWithoutExtension(path);
            Path = path;
            Gen = gen;
        }

        public virtual object?[] GetWarmupKeys() {
            return DefaultWarmupKeys;
        }

        public virtual byte[]? GetData(object? arg = null) {
            return Path is null ? null :
                Data ??= Assets.OpenData(Path);
        }

        public virtual IReloadable<MiniEffect, NullMeta> GetEffect(Func<GraphicsDevice> gd, object? arg = null) {
            return Path is null || Gen is null ? throw new InvalidOperationException() :
                Effect ??= new Reloadable<MiniEffect, NullMeta>(System.IO.Path.GetFileNameWithoutExtension(Path), default, () => Gen(gd(), arg));
        }

    }

    public abstract class MiniEffectCache<T> : MiniEffectCache {

        protected MiniEffectCache()
            : base(nameof(T)) {
        }

        public sealed override byte[]? GetData(object? arg = null)
            => GetData((T) (arg ?? throw new ArgumentNullException(nameof(arg))));

        public abstract byte[]? GetData(T arg);

        public sealed override IReloadable<MiniEffect, NullMeta> GetEffect(Func<GraphicsDevice> gd, object? arg = null)
            => GetEffect(gd, (T) (arg ?? throw new ArgumentNullException(nameof(arg))));

        public abstract IReloadable<MiniEffect, NullMeta> GetEffect(Func<GraphicsDevice> gd, T arg);

    }
}
