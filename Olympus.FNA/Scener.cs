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

        public static event Action<Scene?, Scene>? OnChange;

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

        public static T Get<T>() where T : Scene, new() {
            if (!Generated.TryGetValue(typeof(T), out Scene? scene))
                Generated[typeof(T)] = scene = new T();
            return (T) scene;
        }

        public static T Set<T>(params object[] args) where T : Scene, new()
            => (T) Set(Get<T>(), args);

        public static Scene Set(Scene scene, params object[] args) {
            Scene? prev = Stack.Count == 0 ? null : Stack.Peek();
            RootContainer.Children.Clear();
            RootContainer.Children.Add(scene.Root);
            RootContainer.InvalidateFullDown();
            Stack.Clear();
            Stack.Push(scene);
            scene.Enter(args);
            OnChange?.Invoke(prev, scene);
            return scene;
        }

        public static T Push<T>(params object[] args) where T : Scene, new()
            => (T) Push(Get<T>(), args);

        public static Scene Push(Scene scene, params object[] args) {
            Scene? prev = Stack.Count == 0 ? null : Stack.Peek();
            RootContainer.Children.Clear();
            RootContainer.Children.Add(scene.Root);
            RootContainer.InvalidateFullDown();
            Stack.Push(scene);
            scene.Enter(args);
            OnChange?.Invoke(prev, scene);
            return scene;
        }

    }
}
