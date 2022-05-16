using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System.Runtime.CompilerServices {
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ModuleInitializerAttribute : Attribute {
    }
}

namespace Olympus.Gen {
    [Generator]
    public class RoslynHotpatchDummy : IIncrementalGenerator {
        
        public void Initialize(IncrementalGeneratorInitializationContext context) {
            // RoslynHotpatch.Initialize();
        }

    }

    public static class RoslynHotpatch {

        private static readonly Assembly a_CodeAnalysis = typeof(Microsoft.CodeAnalysis.CSharp.Conversion).Assembly;

        private static readonly List<IPatch> Patches = new();

        [ModuleInitializer]
        internal static void Initialize() {
#if DEBUG && false
            if (!Debugger.IsAttached)
                Debugger.Launch();
#endif

            string isInitializedKey = "Olympus.Gen.RoslynHotpatch.IsInitialized";
            if (AppDomain.CurrentDomain.GetData(isInitializedKey) is not null)
                return;

            try {
                Apply<Example>();

            } catch (Exception e) {
                foreach (IPatch patch in Patches) {
                    patch.Undo();
                }

                Patches.Clear();

#if DEBUG
#if false
                if (!Debugger.IsAttached)
                    Debugger.Launch();
#endif

                MessageBox(IntPtr.Zero, e.ToString(), "Olympus.Gen.RoslynHotpatch", 0);
#endif

                return;
            }

            AppDomain.CurrentDomain.SetData(isInitializedKey, true);
        }

        private static void Apply<T>() where T : IPatch, new() {
            T patch = new();
            Patches.Add(patch);
            patch.Apply();
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);

        private interface IPatch {
            void Apply();
            void Undo();
        }

        private class Example : IPatch {

            public void Apply() {
            }

            public void Undo() {
            }

        }

    }
}
