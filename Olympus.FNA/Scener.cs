using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OlympUI;
using Olympus.NativeImpls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Olympus {
    public static class Scener {

        private static readonly Dictionary<Type, Scene> Generated = new();

        public static readonly Stack<Scene> Stack = new();
        public static Scene Current => Stack.Peek();

        public static Group RootContainer = new() {
            Layout = {
                Layouts.FillFull()
            }
        };

        public static void Update(float dt) {
            if (Stack.Count > 0) {
                Stack.Peek().Update(dt);
            }
        }

        public static void Draw() {
            if (Stack.Count > 0) {
                Stack.Peek().Draw();
            }
        }

        public static T Push<T>(params object[] args) where T : Scene, new() {
            if (!Generated.TryGetValue(typeof(T), out Scene? scene))
                Generated[typeof(T)] = scene = new T();
            RootContainer.Children.Clear();
            RootContainer.Children.Add(scene.Root);
            Stack.Push(scene);
            scene.Enter(args);
            return (T) scene;
        }

    }
}
