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

        public NativeLinux() {
        }

        public override void Run() {
            string? forceDriver = null;

            Retry:

            if (!string.IsNullOrEmpty(forceDriver))
                SDL.SDL_SetHintWithPriority("FNA3D_FORCE_DRIVER", forceDriver, SDL.SDL_HintPriority.SDL_HINT_OVERRIDE);

            using (App app = App = new()) {
                GraphicsDeviceManager? gdm = (GraphicsDeviceManager) App.Services.GetService(typeof(IGraphicsDeviceManager));

                WrappedGraphicsDeviceManager wrappedGDM = new(gdm);

                App.Services.RemoveService(typeof(IGraphicsDeviceManager));
                App.Services.RemoveService(typeof(IGraphicsDeviceService));
                App.Services.AddService(typeof(IGraphicsDeviceManager), wrappedGDM);
                App.Services.AddService(typeof(IGraphicsDeviceService), wrappedGDM);

                wrappedGDM.CreateDevice();
                wrappedGDM.CanCreateDevice = false;
                wrappedGDM.ApplyChangesOnCreateDevice = true;

                if (!string.IsNullOrEmpty(forceDriver) && FNAHooks.FNA3DDriver != forceDriver)
                    throw new Exception($"Tried to force FNA3D to use {forceDriver} but got {FNAHooks.FNA3DDriver}.");

                if (FNAHooks.FNA3DDevice is FNA3DOpenGLDeviceInfo openGL &&
                    openGL.Device.StartsWith("SVGA3D; build:") &&
                    openGL.Vendor == "VMware, Inc.") {
                    Console.WriteLine("Detected OpenGL driver which is prone to crashing. Enforcing Vulkan.");
                    forceDriver = "Vulkan";
                    goto Retry;
                }

                App.Run();
            }
        }

    }
}
