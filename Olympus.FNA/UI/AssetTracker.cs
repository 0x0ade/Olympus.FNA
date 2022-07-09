using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;

namespace OlympUI {
    public abstract class AssetTracker<TValue, TMeta> where TMeta : struct {

        private static AssetTracker<TValue, TMeta>? _Instance;
        private static bool _InstanceCreated;

        public static AssetTracker<TValue, TMeta>? Instance {
            get {
                if (_InstanceCreated) {
                    return _Instance;
                }

                foreach (Type type in UIReflection.GetAllTypes(typeof(AssetTracker<TValue, TMeta>))) {
                    if (type.IsAbstract)
                        continue;

                    _Instance = (AssetTracker<TValue, TMeta>?) Activator.CreateInstance(type)!;
                }

                _InstanceCreated = true;
                return _Instance;
            }
        }

        public abstract void Created(Reloadable<TValue, TMeta> asset);

        public abstract void Collected(Reloadable<TValue, TMeta> asset);

        public abstract void Loaded(Reloadable<TValue, TMeta> asset);

        public abstract void Unloaded(Reloadable<TValue, TMeta> asset);

        public abstract void MetaUpdated(Reloadable<TValue, TMeta> asset, TMeta prev, TMeta next);

    }

    public sealed class TextureTracker : AssetTracker<Texture2D, Texture2DMeta> {

        public static new TextureTracker Instance => (TextureTracker) AssetTracker<Texture2D, Texture2DMeta>.Instance!;

        public int UsedCount;
        public int TotalCount;
        public long UsedMemory;
        public long TotalMemory;

        public override void Created(Reloadable<Texture2D, Texture2DMeta> asset) {
            Texture2DMeta meta = asset.Meta;

            TotalCount++;
            TotalMemory += meta.MemorySizePoT;
        }

        public override void Collected(Reloadable<Texture2D, Texture2DMeta> asset) {
            Texture2DMeta meta = asset.Meta;

            TotalCount--;
            TotalMemory -= meta.MemorySizePoT;

#if DEBUG
            // if (asset.ValueRaw is not null && (!string.IsNullOrEmpty(asset.ID) || asset.Unloader is not null) && Debugger.IsAttached) {
            if (asset.ValueRaw is not null && Debugger.IsAttached) {
                Debugger.Break();
            }
#endif
        }

        public override void Loaded(Reloadable<Texture2D, Texture2DMeta> asset) {
            Texture2DMeta meta = asset.Meta;

            UsedCount++;
            UsedMemory += meta.MemorySizePoT;
        }

        public override void Unloaded(Reloadable<Texture2D, Texture2DMeta> asset) {
            Texture2DMeta meta = asset.Meta;

            UsedCount--;
            UsedMemory -= meta.MemorySizePoT;
        }

        public override void MetaUpdated(Reloadable<Texture2D, Texture2DMeta> asset, Texture2DMeta prev, Texture2DMeta next) {
            TotalMemory -= prev.MemorySizePoT;
            TotalMemory += next.MemorySizePoT;

            if (asset.ValueRaw is not null) {
                UsedMemory -= prev.MemorySizePoT;
                UsedMemory += next.MemorySizePoT;
            }
        }

    }
}
