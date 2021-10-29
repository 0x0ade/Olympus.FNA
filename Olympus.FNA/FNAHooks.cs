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
    public static class FNAHooks {

        public static bool ApplyWindowChangesWithoutCenter;

        public static string? FNA3DDriver;
        public static string? FNA3DDevice;

        private static ILHook? ApplyWindowChangesPatch;
        private static ILHook? DebugFNA3DPatch;

        public static void Apply() {
            Undo();

            Type? t_FNAHooks =
                typeof(FNAHooks)
                ?? throw new Exception("Olympus without FNAPatches?");
            Type? t_SDL2_FNAPlatform =
                typeof(Game).Assembly.GetType("Microsoft.Xna.Framework.SDL2_FNAPlatform")
                ?? throw new Exception("FNA without SDL2_FNAPlatform?");

            ApplyWindowChangesPatch = new(
                t_SDL2_FNAPlatform.GetMethod("ApplyWindowChanges"),
                il => {
                    ILCursor c = new(il);
                    c.GotoNext(i => i.MatchCall(typeof(SDL), "SDL_SetWindowPosition"));
                    c.Next.Operand = il.Import(t_FNAHooks.GetMethod(nameof(ApplyWindowChangesPatch_SetWindowPosition), BindingFlags.NonPublic | BindingFlags.Static));
                }
            );

            DebugFNA3DPatch = new(
                typeof(GraphicsDevice).GetConstructor(new Type[] { typeof(GraphicsAdapter), typeof(GraphicsProfile), typeof(PresentationParameters) }),
                il => {
                    ILCursor c = new(il);
                    c.GotoNext(
                        i => i.MatchLdcI4(0) || i.MatchLdcI4(1),
                        i => i.MatchCall("Microsoft.Xna.Framework.Graphics.FNA3D", "FNA3D_CreateDevice")
                    );
                    c.Next.OpCode = OpCodes.Call;
                    c.Next.Operand = il.Import(t_FNAHooks.GetMethod(nameof(GraphicsDevice_DebugFNA3D), BindingFlags.NonPublic | BindingFlags.Static));
                }
            );

            FNALoggerEXT.LogInfo += OnLogInfo;
        }

        public static void Undo() {
            ApplyWindowChangesPatch?.Dispose();
            DebugFNA3DPatch?.Dispose();
            FNALoggerEXT.LogInfo -= OnLogInfo;
        }

        private static void ApplyWindowChangesPatch_SetWindowPosition(IntPtr window, int x, int y) {
            if (!ApplyWindowChangesWithoutCenter)
                SDL.SDL_SetWindowPosition(window, x, y);
        }

        private static byte GraphicsDevice_DebugFNA3D() {
#if DEBUG
            return Environment.GetEnvironmentVariable("OLYMPUS_DEBUG_FNA3D") == "0" ? (byte) 0 : (byte) 1;
#else
            return Environment.GetEnvironmentVariable("OLYMPUS_DEBUG_FNA3D") == "1" ? (byte) 1 : (byte) 0;
#endif
        }

        private static void OnLogInfo(string line) {
            Console.WriteLine(line);

            if (line.StartsWith("FNA3D Driver: ")) {
                FNA3DDriver = line.Substring("FNA3D Driver: ".Length).Trim();

            } else if (line.StartsWith("D3D11 Adapter: ")) {
                FNA3DDevice = line.Substring("D3D11 Adapter: ".Length).Trim();

            } else if (line.StartsWith("Vulkan Device: ")) {
                FNA3DDevice = line.Substring("Vulkan Device: ".Length).Trim();

            } else if (line.StartsWith("OpenGL Renderer: ")) {
                FNA3DDevice = line.Substring("OpenGL Renderer: ".Length).Trim();
            }
        }

    }
}
