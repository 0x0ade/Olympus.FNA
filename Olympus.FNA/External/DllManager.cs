// Adapted from https://github.com/Mirsario/SteamAudio.NET
// MIT-licensed, originally by Mirsario.
#pragma warning disable IDE0008 // Use explicit type
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Olympus.External {
    internal static partial class DllManager {
        //This method implements a dllmap resolver for native libraries. It expects 'AssemblyName.dll.config' to be present next to the managed library's dll.
        internal static void PrepareResolver(Assembly wrapperAssembly) {
            string osString = OSUtils.GetOS();
            string cpuString = RuntimeInformation.OSArchitecture switch {
                Architecture.Arm => "arm",
                Architecture.Arm64 => "armv8",
                Architecture.X86 => "x86",
                _ => "x86-64",
            };
            string wordSizeString = RuntimeInformation.OSArchitecture switch {
                Architecture.X86 => "32",
                Architecture.Arm => "32",
                _ => "64",
            };
            var stringComparer = StringComparer.InvariantCultureIgnoreCase;

            bool StringNullOrEqual(string a, string b)
                => a is null || stringComparer.Equals(a, b);

            NativeLibrary.SetDllImportResolver(wrapperAssembly, (name, assembly, path) => {
                string configPath = wrapperAssembly.Location + ".config";

                if (!File.Exists(configPath)) {
                    return IntPtr.Zero;
                }

                XElement root = XElement.Load(configPath);

                var maps = root
                    .Elements("dllmap")
                    .Where(element => stringComparer.Equals(element.Attribute("dll")?.Value, name))
                    .Where(element => StringNullOrEqual(element.Attribute("os")?.Value, osString))
                    .Where(element => StringNullOrEqual(element.Attribute("cpu")?.Value, cpuString))
                    .Where(element => StringNullOrEqual(element.Attribute("wordsize")?.Value, wordSizeString));

                var map = maps.SingleOrDefault();

                if (map is null) {
                    throw new ArgumentException($"'{Path.GetFileName(configPath)}' - Found {maps.Count()} possible mapping candidates for dll '{name}'.");
                }

                return NativeLibrary.Load(map.Attribute("target").Value);
            });
        }

        internal static class OSUtils {
            public enum OS {
                Windows,
                Linux,
                OSX,
                FreeBSD
            }

            public static bool IsOS(OS os) => os switch {
                OS.Windows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                OS.Linux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
                OS.OSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
                OS.FreeBSD => RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD),
                _ => false
            };

            public static string GetOS() {
                if (IsOS(OS.Linux)) {
                    return "linux";
                }

                if (IsOS(OS.OSX)) {
                    return "osx";
                }

                if (IsOS(OS.FreeBSD)) {
                    return "freebsd";
                }

                return "windows";
            }
        }
    }
}
