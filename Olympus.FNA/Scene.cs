using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OlympUI;
using Olympus.NativeImpls;
using System;
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

        public virtual string Name { get; set; }

        public Scene() {
            string name = GetType().Name;
            if (name.EndsWith("Scene"))
                name = name.Substring(0, name.Length - "Scene".Length);
            Name = name;
        }

        public abstract Element Generate();

        public virtual void Enter(params object[] args) {
        }

        public virtual void Leave() {
        }

        public virtual void Update(float dt) {
        }

        public virtual void Draw() {
        }

    }
}
