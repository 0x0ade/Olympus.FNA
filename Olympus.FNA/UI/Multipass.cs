using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public class Multipass : IEnumerable<Multipass.Pass> {

        public event Action Drawer;
        public List<Pass> Passes = new();

        public bool Ended;

        public Multipass(Action drawer) {
            Drawer = drawer;
        }

        public void Add(Pass pass)
            => Passes.Add(pass);

        public void Draw() {
            Ended = false;
            foreach (Pass pass in Passes) {
                pass.Begin?.Invoke(this);
                if (Ended)
                    return;
                Drawer();
                pass.End?.Invoke(this);
                if (Ended)
                    return;
            }
        }

        public void End()
            => Ended = true;

        public IEnumerator<Pass> GetEnumerator()
            => Passes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => Passes.GetEnumerator();

        public class Pass {

            public Action<Multipass>? Begin;
            public Action<Multipass>? End;

            public Pass() {
            }

            public Pass(Action<Multipass>? begin) {
                Begin = begin;
            }

            public Pass(Action<Multipass>? begin, Action<Multipass>? end) {
                Begin = begin;
                End = end;
            }

        }

    }
}
