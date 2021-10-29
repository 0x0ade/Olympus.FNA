using FontStashSharp;
using FontStashSharp.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace OlympUI {
    public static unsafe class Assets {

        #region FNA3D Helpers

        private delegate IntPtr d_FNA3D_ReadImageStream(Stream stream, out int width, out int height, out int len, int forceW = -1, int forceH = -1, bool zoom = false);
        private static d_FNA3D_ReadImageStream FNA3D_ReadImageStream =
            typeof(Game).Assembly
            .GetType("Microsoft.Xna.Framework.Graphics.FNA3D")
            ?.GetMethod("ReadImageStream")
            ?.CreateDelegate<d_FNA3D_ReadImageStream>()
            ?? throw new Exception("FNA3D_ReadImageStream not found!");

        private delegate void d_FNA3D_Image_Free(IntPtr mem);
        private static d_FNA3D_Image_Free FNA3D_Image_Free =
            typeof(Game).Assembly
            .GetType("Microsoft.Xna.Framework.Graphics.FNA3D")
            ?.GetMethod("FNA3D_Image_Free")
            ?.CreateDelegate<d_FNA3D_Image_Free>()
            ?? throw new Exception("FNA3D_Image_Free not found!");

        #endregion

        public static ulong ReloadID = 0;

        private static readonly Dictionary<string, object> _Gotten = new();

        public static readonly Reloadable<DynamicSpriteFont> Font = GetFont(
            20,
            "fonts/Poppins-Regular",
            "fonts/NotoSansCJKjp-Regular",
            "fonts/NotoSansCJKkr-Regular",
            "fonts/NotoSansCJKsc-Regular",
            "fonts/NotoSansCJKtc-Regular"
        );

        public static readonly Reloadable<DynamicSpriteFont> FontSmall = GetFont(
            16,
            "fonts/Poppins-Regular",
            "fonts/NotoSansCJKjp-Regular",
            "fonts/NotoSansCJKkr-Regular",
            "fonts/NotoSansCJKsc-Regular",
            "fonts/NotoSansCJKtc-Regular"
        );

        public static readonly Reloadable<DynamicSpriteFont> FontMono = GetFont(16, "fonts/Perfect DOS VGA 437");

        public static readonly Reloadable<Texture2D> White = Get("White", () => {
            Texture2D tex = new(UI.Game.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            byte[] data = new byte[tex.Width * tex.Height * sizeof(uint)];
            Unsafe.InitBlock(ref data[0], 0xFF, (uint) data.Length);
            tex.SetData(data);
            return tex;
        });

        public static readonly Reloadable<Texture2D> GradientQuad = Get("GradientQuad", () => {
            Texture2D tex = new(UI.Game.GraphicsDevice, 1, 256, false, SurfaceFormat.Color);
            byte[] data = new byte[tex.Width * tex.Height * sizeof(uint)];
            Unsafe.InitBlock(ref data[0], 0xFF, (uint) data.Length);
            fixed (byte* ptr = data) {
                for (int i = 0; i < tex.Height; i++) {
                    float f = i / 255f;
                    f = f * f * f * i;
                    ptr[i * 4 + 0] = (byte) f;
                    ptr[i * 4 + 1] = (byte) f;
                    ptr[i * 4 + 2] = (byte) f;
                    ptr[i * 4 + 3] = (byte) f;
                }
            }
            tex.SetData(data);
            return tex;
        });

        public static readonly Reloadable<BasicEffect> BasicTextureEffect = Get("BasicTextureEffect", () => new BasicEffect(UI.Game.GraphicsDevice) {
            FogEnabled = false,
            LightingEnabled = false,
            TextureEnabled = true,
            VertexColorEnabled = false,
        });

        public static readonly Reloadable<BasicEffect> BasicColorEffect = Get("BasicColorEffect", () => new BasicEffect(UI.Game.GraphicsDevice) {
            FogEnabled = false,
            LightingEnabled = false,
            TextureEnabled = false,
            VertexColorEnabled = true,
        });

        public static readonly Reloadable<BasicEffect> BasicEffect = Get("BasicEffect", () => new BasicEffect(UI.Game.GraphicsDevice) {
            FogEnabled = false,
            LightingEnabled = false,
            TextureEnabled = true,
            VertexColorEnabled = true,
        });

        public static readonly Reloadable<RasterizerState> WireFrame = Get("WireFrame", () => new RasterizerState() {
            FillMode = FillMode.WireFrame,
            CullMode = CullMode.None
        });

        public static readonly Reloadable<Texture2D> Test = GetTexture("icon");

        public static readonly Reloadable<Texture2D> DebugUnused = GetTexture("debug/unused");
        public static readonly Reloadable<Texture2D> DebugDisposed = GetTexture("debug/disposed");


        /*
        public static readonly Reloadable<Texture2D> Overlay = GetTexture("overlay");

        public static readonly Reloadable<Texture2D> Splash = GetTexture("splash");
        */

        public static Reloadable<T> Get<T>(string id, Func<T?> loader) {
            if (_Gotten.TryGetValue(id, out object? value))
                return (Reloadable<T>) value;

            Reloadable<T> reloadable = new(id, loader);
            _Gotten[id] = reloadable;
            return reloadable;
        }


        public static Stream? OpenStream(string path, params string[] exts) {
            foreach (string ext in exts)
                if (OpenStream($"{path}.{ext}") is Stream stream)
                    return stream;
            return OpenStream(path);
        }
        public static Stream? OpenStream(string path) {
            try {
                return
#if DEBUG_CONTENT
                    (Path.GetFullPath($"{typeof(Assets).Assembly.Location}/../../../../../../Content/{path}") is string pathFull && File.Exists(pathFull)) ?
                    File.OpenRead(pathFull) :
#endif
                    TitleContainer.OpenStream($"Content/{path}");

            } catch (FileNotFoundException) {
                Console.WriteLine($"Couldn't find content file: {path}");
                return null;

            } catch (DirectoryNotFoundException) {
                Console.WriteLine($"Couldn't find content folder: {path}");
                return null;

            } catch (Exception e) {
                Console.WriteLine($"Failed loading content: {path}");
                Console.WriteLine(e);
                return null;
            }
        }

        public static byte[]? OpenData(string path, params string[] exts) {
            foreach (string ext in exts)
                if (OpenData($"{path}.{ext}") is byte[] data)
                    return data;
            return OpenData(path);
        }
        public static byte[]? OpenData(string path) {
            using Stream? s = OpenStream(path);
            if (s == null)
                return null;

            using MemoryStream ms = new();
            s.CopyTo(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return ms.ToArray();
        }

        public static Reloadable<DynamicSpriteFont> GetFont(int size, params string[] paths)
            => Get($"Font '{string.Join(", ", paths)}' Size '{size}'", () => OpenFont(paths).GetFont(size));
        public static FontSystem OpenFont(params string[] paths) {
            FontSystem font = new(new() {
                TextureWidth = 2048,
                TextureHeight = 2048,
                PremultiplyAlpha = true,
                // FontResolutionFactor = 2f,
                KernelWidth = 1,
                KernelHeight = 1,
            });
            font.AddFonts(paths);
            return font;
        }

        public static void AddFonts(this FontSystem font, params string[] paths) {
            foreach (string path in paths) {
                byte[]? data = OpenData(path, "ttf", "otf");
                if (data != null)
                    font.AddFont(data);
            }
        }

        public static Reloadable<Texture2D> GetTexturePremul(string path)
            => Get($"Texture (Premultiplied) '{path}'", () => OpenTexturePremul(path));
        public static Texture2D? OpenTexturePremul(string path) {
            using Stream? s = OpenStream(path, "png");
            if (s == null)
                return null;

            // Mipmaps are pain.

            GraphicsDevice gd = UI.Game.GraphicsDevice;
            GraphicsStateSnapshot gss = new(gd);

            IntPtr ptr = FNA3D_ReadImageStream(s, out int w, out int h, out int len);
            using Texture2D texRaw = new(gd, w, h, false, SurfaceFormat.Color);
            texRaw.SetDataPointerEXT(0, null, ptr, len);
            FNA3D_Image_Free(ptr);

            gd.SamplerStates[0] = SamplerState.LinearClamp;
            RenderTarget2D rt = new(gd, w, h, true, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            gd.SetRenderTarget(rt);
            using BasicMesh mesh = new(gd) {
                Shapes = {
                    new MeshShapes.Quad() {
                        XY1 = new(w * 0f, h * 0f),
                        XY2 = new(w * 1f, h * 0f),
                        XY3 = new(w * 0f, h * 1f),
                        XY4 = new(w * 1f, h * 1f),
                    },
                },
                MSAA = false,
                Texture = new(null, () => texRaw),
                BlendState = BlendState.Opaque,
            };
            mesh.Draw();

            gss.Apply();
            return rt;
        }

        public static Reloadable<Texture2D> GetTexture(string path)
            => Get($"Texture (Non-Premultiplied) '{path}'", () => OpenTexture(path));
        public static Texture2D? OpenTexture(string path) {
            using Stream? s = OpenStream(path, "png");
            if (s == null)
                return null;

            // Mipmaps are pain.

            GraphicsDevice gd = UI.Game.GraphicsDevice;
            GraphicsStateSnapshot gss = new(gd);

            IntPtr ptr = FNA3D_ReadImageStream(s, out int w, out int h, out int len);
            byte* raw = (byte*) ptr;
            for (int i = len - 1 - 3; i > -1; i -= 4) {
                byte a = raw[i + 3];
                if (a == 0 || a == 255)
                    continue;
                raw[i + 0] = (byte) (raw[i + 0] * a / 255D);
                raw[i + 1] = (byte) (raw[i + 1] * a / 255D);
                raw[i + 2] = (byte) (raw[i + 2] * a / 255D);
                // raw[i + 3] = a;
            }
            using Texture2D texRaw = new(gd, w, h, false, SurfaceFormat.Color);
            texRaw.SetDataPointerEXT(0, null, ptr, len);
            FNA3D_Image_Free(ptr);

            RenderTarget2D rt = new(gd, w, h, true, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            gd.SetRenderTarget(rt);
            using BasicMesh mesh = new(gd) {
                Shapes = {
                    new MeshShapes.Quad() {
                        XY1 = new(w * 0f, h * 0f),
                        XY2 = new(w * 1f, h * 0f),
                        XY3 = new(w * 0f, h * 1f),
                        XY4 = new(w * 1f, h * 1f),
                    },
                },
                MSAA = false,
                Texture = new(null, () => texRaw),
                BlendState = BlendState.Opaque,
            };
            mesh.Draw();

            gss.Apply();
            return rt;
        }

    }

    public sealed class Reloadable<T> : IDisposable {

        public ulong ReloadID;
        public string ID;
        public bool IsValid;
        public T? ValueRaw;
        public Func<T?> Loader;
        public Action<T?>? Unloader;

        public T? ValueMaybe {
            get {
                Reload();
                return ValueRaw;
            }
        }
        public T Value => ValueMaybe ?? throw new Exception($"Failed loading: {(string.IsNullOrEmpty(ID) ? "<temporary pseudo-reloadable resource>" : ID)}");

        public Reloadable(string? id, Func<T?> loader, Action<T?>? unloader = null) {
            ID = id ?? "";
            Loader = loader;
            Unloader = unloader;
        }

        public void Dispose() {
            if (!IsValid)
                return;

            if (Unloader != null) {
                Unloader(ValueRaw);
            } else if (!string.IsNullOrEmpty(ID) && ValueRaw is IDisposable disposable) {
                disposable.Dispose();
            }
            IsValid = false;
        }

        public void Reload(bool force = false) {
            if (ReloadID != Assets.ReloadID) {
                ReloadID = Assets.ReloadID;
                IsValid = false;
            }

            if (IsValid) {
                if (!force)
                    return;
                Dispose();
            }

            IsValid = true;
            ValueRaw = Loader();
        }

        public static implicit operator T(Reloadable<T> rl) => rl.Value;
        // public static implicit operator T?(Reloadable<T?> rl) => rl.ValueMaybe;

    }
}
