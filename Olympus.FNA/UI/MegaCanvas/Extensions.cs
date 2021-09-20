using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI.MegaCanvas {
    public static class Extensions {

        public static bool TryGetSmallest<T>(this T?[] list, int width, int height, [NotNullWhen(true)] out T? best, out int index) where T : ISizeable {
            best = default;
            index = -1;
            for (int i = list.Length - 1; i >= 0; --i) {
                T? entry = list[i];
                if (entry != null && !entry.IsDisposed &&
                    width <= entry.Width && height <= entry.Height && (
                        index == -1 || best == null ||
                        (entry.Width < best.Width && entry.Height < best.Height) ||
                        (width < height ? entry.Width <= best.Width : entry.Height <= best.Height)
                )) {
                    best = entry;
                    index = i;
                    if (width == entry.Width && height == entry.Height)
                        return true;
                }
            }
            return index != -1;
        }

        public static bool TryGetSmallest<T>(this List<T?> list, int width, int height, [NotNullWhen(true)] out T? best, out int index) where T : ISizeable {
            best = default;
            index = -1;
            for (int i = list.Count - 1; i >= 0; --i) {
                T? entry = list[i];
                if (entry != null && !entry.IsDisposed &&
                    width <= entry.Width && height <= entry.Height && (
                        index == -1 || best == null ||
                        (entry.Width < best.Width && entry.Height < best.Height) ||
                        (width < height ? entry.Width <= best.Width : entry.Height <= best.Height)
                )) {
                    best = entry;
                    index = i;
                    if (width == entry.Width && height == entry.Height)
                        return true;
                }
            }
            return index != -1;
        }

        public static List<T?> RemoveNulls<T>(this List<T?> list) {
            for (int i = list.Count - 1; i >= 0; --i) {
                T? entry = list[i];
                if (entry == null)
                    list.RemoveAt(i);
            }
            return list;
        }

        public static long GetMemoryUsage(this Texture2D tex)
            => tex.Width * tex.Height * 4;

    }
}
