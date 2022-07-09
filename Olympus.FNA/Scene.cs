using OlympUI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Olympus {
    public abstract class Scene {

        protected Element? _Root;
        public Element Root => _Root ??= PostGenerate(Generate());

        public readonly List<Action> Refreshes = new();
        private readonly Dictionary<Element, Task> Refreshing = new();

        public App App => App.Instance;

        public virtual string Name { get; set; }

        public virtual bool Alert { get; set; }
        public virtual bool Locked { get; set; }

        public Scene() {
            string name = GetType().Name;
            if (name.EndsWith("Scene"))
                name = name.Substring(0, name.Length - "Scene".Length);
            Name = name;
        }

        protected Action<Element> RegisterRefresh<T>(Func<T, Task> reload) where T : Element
            => el => {
                Refreshes.Add(() => {
                    if (!Refreshing.TryGetValue(el, out Task? task) || task.IsCompleted) {
                        Refreshing[el] = Task.Run(async () => await reload((T) el));
                    }
                });
            };

        public abstract Element Generate();
        public virtual Element PostGenerate(Element root) => root;

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

    }
}
