using Microsoft.Xna.Framework;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Olympus {
    public class Program {

        public static void Main(string[] args) {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            Environment.SetEnvironmentVariable("FNA3D_FORCE_DRIVER", "D3D11");

            bool help = false;
            bool helpExit = false;
#if WINDOWS && !DEBUG
            bool console = false;
#endif
            OptionSet options = new() {
#if WINDOWS && !DEBUG
                { "console", "Open a debug console.", v => console = v != null },
#endif
                { "h|help", "Show this message and exit.", h => help = h != null },
            };

            List<string> extra;
            try {
                // parse the command line
                extra = options.Parse(args);
            } catch (OptionException e) {
                Console.Write("Olympus CLI error: ");
                Console.WriteLine(e.Message);
                Console.WriteLine();
                helpExit = true;
            }

            if (help) {
                options.WriteOptionDescriptions(Console.Out);
                if (helpExit)
                    return;
            }

#if WINDOWS
#if DEBUG
            AllocConsole();
            Console.SetError(Console.Out);
#else
            if (console) {
                AllocConsole();
                Console.SetError(Console.Out);
            }
#endif
#endif

            External.DllManager.PrepareResolver(typeof(Program).Assembly);
            // External.DllManager.PrepareResolver(typeof(Microsoft.Xna.Framework.Game).Assembly);

            FNAPatches.Apply();

            using App game = new();
            game.Run();
        }

#if WINDOWS
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();
#endif

    }
}
