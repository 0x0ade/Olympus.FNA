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
    public abstract class Scene {

        protected Element? _Root;
        public Element Root => _Root ??= Generate();

        public List<Action> Refreshes = new();
        private Dictionary<Element, Task> Refreshing = new();

        public virtual string Name { get; set; }

        public Scene() {
            string name = GetType().Name;
            if (name.EndsWith("Scene"))
                name = name.Substring(0, name.Length - "Scene".Length);
            Name = name;
        }

        protected Action<Element> RegisterRefresh<T>(Func<T, Task> reload) where T : Element
            => el => {
                Refreshes.Add(() => {
                    if (!Refreshing.TryGetValue(el, out Task? task) || task.IsCompleted)
                        Refreshing[el] = Task.Run(() => reload((T) el));
                });
            };

        public abstract Element Generate();

        public virtual void Refresh() {
            foreach (Action refresh in Refreshes)
                refresh();
        }

        public virtual void Enter(params object[] args) {
            Refresh();
        }

        public virtual void Leave() {
        }

        public virtual void Update(float dt) {
        }

        public virtual void Draw() {
        }

    }
}
