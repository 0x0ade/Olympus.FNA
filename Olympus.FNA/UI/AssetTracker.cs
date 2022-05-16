using FontStashSharp;
using LibTessDotNet;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public abstract class AssetTracker<TValue, TMeta> {

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
            TotalMemory += meta.MemorySize;
        }

        public override void Collected(Reloadable<Texture2D, Texture2DMeta> asset) {
            Texture2DMeta meta = asset.Meta;

            TotalCount--;
            TotalMemory -= meta.MemorySize;

#if DEBUG
            if (asset.ValueRaw is not null && (!string.IsNullOrEmpty(asset.ID) || asset.Unloader is not null) && Debugger.IsAttached) {
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

    }
}
