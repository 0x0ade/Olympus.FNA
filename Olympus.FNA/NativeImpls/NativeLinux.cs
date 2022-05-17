using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OlympUI;
using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Olympus.NativeImpls {
    public unsafe partial class NativeLinux : NativeSDL2 {

        private WrappedGraphicsDeviceManager? WrappedGDM;

        public NativeLinux() {
        }

        public override void Run() {
            string? forceDriver = null;

            Retry:

            if (!string.IsNullOrEmpty(forceDriver))
                SDL.SDL_SetHintWithPriority("FNA3D_FORCE_DRIVER", forceDriver, SDL.SDL_HintPriority.SDL_HINT_OVERRIDE);

            using (App app = App = new()) {
                GraphicsDeviceManager? gdm = (GraphicsDeviceManager) App.Services.GetService(typeof(IGraphicsDeviceManager));

                WrappedGDM = new(gdm);

                // XNA - and thus in turn FNA - love to re-center the window on device changes.
                FNAHooks.ApplyWindowChangesWithoutRestore = true;
                FNAHooks.ApplyWindowChangesWithoutResize = true;
                FNAHooks.ApplyWindowChangesWithoutCenter = true;
                WrappedGDM!.CreateDevice();
                FNAHooks.ApplyWindowChangesWithoutRestore = false;
                FNAHooks.ApplyWindowChangesWithoutResize = false;
                FNAHooks.ApplyWindowChangesWithoutCenter = false;

                WrappedGDM.CanCreateDevice = false;
                WrappedGDM.ApplyChangesOnCreateDevice = true;

                if (!string.IsNullOrEmpty(forceDriver) && FNAHooks.FNA3DDriver != forceDriver)
                    throw new Exception($"Tried to force FNA3D to use {forceDriver} but got {FNAHooks.FNA3DDriver}.");

                if (FNAHooks.FNA3DDevice is FNA3DOpenGLDeviceInfo openGL &&
                    openGL.Device.StartsWith("SVGA3D; build:") &&
                    openGL.Vendor == "VMware, Inc.") {
                    Console.WriteLine("Detected OpenGL driver which is prone to crashing. Enforcing Vulkan.");
                    WrappedGDM.Dispose();
                    forceDriver = "Vulkan";
                    goto Retry;
                }

                if (forceDriver is null) {
                    Console.WriteLine("Detected OpenGL driver which is prone to crashing. Enforcing Vulkan.");
                    WrappedGDM.Dispose();
                    forceDriver = "Vulkan";
                    goto Retry;
                }

                App.Services.RemoveService(typeof(IGraphicsDeviceManager));
                App.Services.RemoveService(typeof(IGraphicsDeviceService));
                App.Services.AddService(typeof(IGraphicsDeviceManager), WrappedGDM);
                App.Services.AddService(typeof(IGraphicsDeviceService), WrappedGDM);

                App.Run();
            }
        }

    }
}
