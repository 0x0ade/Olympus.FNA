using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using OlympUI;
using SDL2;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static OlympUI.Assets;

namespace Olympus {
    public static class FNAPatches {

        public static bool ApplyWindowChangesWithoutCenter;

        private static ILHook? ApplyWindowChangesPatch;

        public static void Apply() {
            Undo();

            Type? t_FNAPatches =
                typeof(FNAPatches)
                ?? throw new Exception("Olympus without FNAPatches?");
            Type? t_SDL2_FNAPlatform =
                typeof(Game).Assembly.GetType("Microsoft.Xna.Framework.SDL2_FNAPlatform")
                ?? throw new Exception("FNA without SDL2_FNAPlatform?");

            ApplyWindowChangesPatch = new(
                t_SDL2_FNAPlatform.GetMethod("ApplyWindowChanges"),
                il => {
                    ILCursor c = new(il);
                    c.GotoNext(i => i.MatchCall(typeof(SDL), "SDL_SetWindowPosition"));
                    c.Next.Operand = il.Import(t_FNAPatches.GetMethod(nameof(ApplyWindowChangesPatch_SetWindowPosition), BindingFlags.NonPublic | BindingFlags.Static));
                }
            );
        }

        public static void Undo() {
            ApplyWindowChangesPatch?.Dispose();
        }

        private static void ApplyWindowChangesPatch_SetWindowPosition(IntPtr window, int x, int y) {
            if (!ApplyWindowChangesWithoutCenter)
                SDL.SDL_SetWindowPosition(window, x, y);
        }

    }
}
