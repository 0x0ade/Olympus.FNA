using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using OlympUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Olympus {
    public class Web : IDisposable {

        public readonly App App;

        public HttpClient Client = new();
        public JsonSerializer JSON = new();

        public Web(App app) {
            App = app;
        }

        public void Dispose() {
            Client.Dispose();
        }

        public async Task<T?> GetJSON<T>(string url) {
            using Stream s = await Client.GetStreamAsync(url);
            using StreamReader sr = new(s);
            using JsonTextReader jtr = new(sr);
            return JSON.Deserialize<T>(jtr);
        }

        public async Task<(byte[]? dataCompressed, byte[]? dataRaw, int w, int h)> GetTextureData(string url) {
            byte[] data;
            int w, h, len;
            IntPtr ptr;
            byte[] dataCompressed;

            try {
                dataCompressed = await Client.GetByteArrayAsync(url);
            } catch (Exception e) {
                Console.WriteLine($"Failed to download texture data \"{url}\":\n{e}");
                return (null, null, 0, 0);
            }

            if (dataCompressed.Length == 0)
                return (null, null, 0, 0);

            using (MemoryStream ms = new(dataCompressed))
                ptr = OlympUI.Assets.FNA3D_ReadImageStream(ms, out w, out h, out len);

            OlympUI.Assets.PremultiplyTextureData(ptr, w, h, len);

            unsafe {
                data = new byte[len];
                using MemoryStream ms = new(data);
                ms.Write(new ReadOnlySpan<byte>((void*) ptr, len));
            }

            OlympUI.Assets.FNA3D_Image_Free(ptr);

            return (dataCompressed, data, w, h);
        }

        public async Task<IReloadable<Texture2D, Texture2DMeta>?> GetTexture(string url) {
            // TODO: Preserve downloaded textures on disk instead of in RAM.
            (byte[]? dataCompressed, byte[]? dataRaw, int w, int h) = await GetTextureData(url);

            if (dataCompressed is null || dataRaw is null || dataRaw.Length == 0)
                return null;

            return App.MarkTemporary(Texture2DMeta.Reloadable($"Texture (Web) (Mipmapped) '{url}'", w, h, () => {
                unsafe {
                    Color[] data = new Color[w * h];

                    // TODO: Cache and unload dataRaw after timeout.

                    if (dataRaw is not null) {
                        fixed (byte* dataRawPtr = dataRaw)
                        fixed (Color* dataPtr = data)
                            Unsafe.CopyBlock(dataPtr, dataRawPtr, (uint) dataRaw.Length);
                        dataRaw = null;
                        return data;
                    }

                    IntPtr ptr;
                    int len;
                    using (MemoryStream ms = new(dataCompressed))
                        ptr = OlympUI.Assets.FNA3D_ReadImageStream(ms, out _, out _, out len);

                    OlympUI.Assets.PremultiplyTextureData(ptr, w, h, len);

                    fixed (Color* dataPtr = data)
                        Unsafe.CopyBlock(dataPtr, (void*) ptr, (uint) len);

                    OlympUI.Assets.FNA3D_Image_Free(ptr);
                    return data;
                }
            }));
        }

        public async Task<IReloadable<Texture2D, Texture2DMeta>?> GetTextureUnmipped(string url) {
            // TODO: Preserve downloaded textures on disk instead of in RAM.
            (byte[]? dataCompressed, byte[]? dataRaw, int w, int h) = await GetTextureData(url);

            if (dataCompressed is null || dataRaw is null || dataRaw.Length == 0)
                return null;

            return App.MarkTemporary(Texture2DMeta.Reloadable($"Texture (Web) (Unmipmapped) '{url}'", w, h, () => {
                unsafe {
                    Color[] data = new Color[w * h];

                    // TODO: Cache and unload dataRaw after timeout.

                    if (dataRaw is not null) {
                        fixed (byte* dataRawPtr = dataRaw)
                        fixed (Color* dataPtr = data)
                            Unsafe.CopyBlock(dataPtr, dataRawPtr, (uint) dataRaw.Length);
                        dataRaw = null;
                        return data;
                    }

                    IntPtr ptr;
                    int len;
                    using (MemoryStream ms = new(dataCompressed))
                        ptr = OlympUI.Assets.FNA3D_ReadImageStream(ms, out _, out _, out len);

                    OlympUI.Assets.PremultiplyTextureData(ptr, w, h, len);

                    fixed (Color* dataPtr = data)
                        Unsafe.CopyBlock(dataPtr, (void*) ptr, (uint) len);

                    OlympUI.Assets.FNA3D_Image_Free(ptr);
                    return data;
                }
            }));
        }

        public async Task<ModEntry[]> GetFeaturedEntries()
            => await GetJSON<ModEntry[]>(@"https://max480-random-stuff.appspot.com/celeste/gamebanana-featured") ?? Array.Empty<ModEntry>();

        public async Task<ModEntry[]> GetSearchEntries(string query)
            => await GetJSON<ModEntry[]>($@"https://max480-random-stuff.appspot.com/celeste/gamebanana-search?q={Uri.EscapeDataString(query)}&full=true") ?? Array.Empty<ModEntry>();

        public async Task<ModEntry[]> GetSortedEntries(int page, SortBy sort, string? itemtypeFilterType = null, string? itemtypeFilterValue = null)
            => await GetJSON<ModEntry[]>(
                $@"https://max480-random-stuff.appspot.com/celeste/gamebanana-list?{sort switch {
                    SortBy.Latest => "sort=latest&",
                    SortBy.Likes => "sort=likes&",
                    SortBy.Views => "sort=views&",
                    SortBy.Downloads => "sort=downloads&",
                    _ => "",
                }}{(
                    string.IsNullOrEmpty(itemtypeFilterValue) ? "" :
                    $"{itemtypeFilterType}={itemtypeFilterValue}&"
                )}page={page}&full=true") ?? Array.Empty<ModEntry>();

        public class ModEntry {
            public string? GameBananaType;
            public int GameBananaID;
            public string Author = "";
            public long CreatedDate;
            public string Name = "";
            public string PageURL = "";
            public int CategoryID;
            public string CategoryName = "";
            public string Description = "";
            public string Text = "";
            public int Views;
            public int Downloads;
            public int Likes;
            public string[] Screenshots = Array.Empty<string>();
            public string[] MirroredScreenshots = Array.Empty<string>();
            public File[] Files = Array.Empty<File>();
            public class File {
                public string Name = "";
                public string Description = "";
                public bool HasEverestYaml;
                public long Size;
                public long CreatedDate;
                public int Downloads;
                public string URL = "";
            }
        }

        public enum SortBy {
            None,
            Latest,
            Likes,
            Views,
            Downloads
        }

    }
}
