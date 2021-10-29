using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public sealed class Root : Element {

        public override string? ID => "Root";

        internal bool Recollecting;
        internal bool ReflowingForce;

        private readonly AutoRotatingPool<List<Element>> AllPool = new(8);
        private readonly AutoRotatingPool<List<Element>> AllOffScreenPool = new(8);
        private readonly AutoRotatingPool<List<LayoutPass>> AllLayoutPassesPool = new(8);
        private readonly AutoRotatingPool<List<Element>> AllOnScreenPool = new(8);
        private readonly AutoRotatingPool<List<Element>> AllInteractivePool = new(8);

        public List<Element> All;
        public List<LayoutPass> AllLayoutPasses;
        public List<Element> AllOffScreen;
        public List<Element> AllOnScreen;
        public List<Element> AllInteractive;

        public Root() {
            All = AllPool.Next();
            AllLayoutPasses = AllLayoutPassesPool.Next();
            AllOffScreen = AllOffScreenPool.Next();
            AllOnScreen = AllOnScreenPool.Next();
            AllInteractive = AllInteractivePool.Next();
        }

        public Element? GetInteractiveChildAt(Point xy) {
            List<Element> all = AllInteractive;
            for (int i = all.Count - 1; i >= 0; --i) {
                Element hit = all[i];
                if (!hit.Contains(xy))
                    goto Next;

                for (Element? el = hit.Parent; el != null; el = el.Parent)
                    if (el.Clip && !el.Contains(xy))
                        goto Next;
                return hit;

                Next:
                continue;
            }

            return null;
        }

        public void InvalidateForce() {
            ReflowingForce = true;
        }

        public void InvalidateCollect() {
            Recollecting = true;
        }

        public bool AutoCollect() {
            if (!Recollecting)
                return false;
            Collect();
            return true;
        }

        public void CollectLayoutPasses() {
            AllLayoutPasses = AllLayoutPassesPool.Next();
            AllLayoutPasses.Clear();
            AllLayoutPasses.Add((LayoutPass) int.MaxValue);
            CollectAllLayoutPasses(AllLayoutPasses, this);
        }

        public void Collect() {
            CollectLayoutPasses();
            All = AllPool.Next();
            All.Clear();
            AllOffScreen = AllOffScreenPool.Next();
            AllOffScreen.Clear();
            AllOnScreen = AllOnScreenPool.Next();
            AllOnScreen.Clear();
            AllInteractive = AllInteractivePool.Next();
            AllInteractive.Clear();
            CollectAllOnScreen(All, AllOffScreen, AllOnScreen, AllInteractive, this, Collection + 1, 0, 0, new() {
                Left = (int) RealXY.X,
                Top = (int) RealXY.Y,
                Right = WH.X,
                Bottom = WH.Y
            }, true);
        }

        private static void CollectAllLayoutPasses(List<LayoutPass> allLayoutPasses, Element el) {
            foreach (Element child in el.Children) {
                foreach (LayoutHandlers.HandlerList handlers in child.Layout.Handlers) {
                    LayoutPass pass = handlers.Pass;
                    if (!allLayoutPasses.Contains(pass)) {
                        int min = 0;
                        int max = allLayoutPasses.Count;
                        int index = 0;
                        if (max >= 0) {
                            while (max - min > 1) {
                                int mid = min + (int) Math.Ceiling((max - min) / 2D);
                                LayoutPass midPass = allLayoutPasses[mid];
                                if (pass <= midPass) {
                                    max = mid;
                                } else {
                                    min = mid;
                                    index = mid + 1;
                                }
                            }
                            if (max == 1) {
                                if (pass <= allLayoutPasses[0]) {
                                    index = 0;
                                } else {
                                    index = 1;
                                }
                            }
                        }
                        allLayoutPasses.Insert(index, pass);
                    }
                }
                CollectAllLayoutPasses(allLayoutPasses, child);
            }
        }

        private static void CollectAllOffScreen(List<Element> all, List<Element> allOffScreen, Element el, uint collection) {
            el.Collection = collection;
            foreach (Element child in el.Children) {
                child.InternalSetOnScreen(null);
                all.Add(child);
                allOffScreen.Add(child);
                CollectAllOffScreen(all, allOffScreen, child, collection);
            }
        }

        private static void CollectAllOnScreen(
            List<Element> all, List<Element> allOffScreen, List<Element> allOnScreen, List<Element> allInteractive,
            Element parent, uint collection,
            int parentX, int parentY, RectLTRB parentClip, bool interactive
        ) {
            foreach (Element child in parent.Children) {
                all.Add(child);

                Vector2 xy = child.RealXY;
                Point size = child.WH;
                RectLTRB rect = new();
                rect.Left = (int) xy.X + parentX;
                rect.Top = (int) xy.Y + parentY;
                rect.Right = rect.Left + size.X;
                rect.Bottom = rect.Top + size.Y;

                if (
                    rect.Right < parentClip.Left || parentClip.Right < rect.Left ||
                    rect.Bottom < parentClip.Top || parentClip.Bottom < rect.Top
                ) {
                    child.InternalSetOnScreen(null);
                    allOffScreen.Add(child);
                    CollectAllOffScreen(all, allOffScreen, child, collection);

                } else {
                    RectLTRB visible = new();
                    visible.Left = Math.Max(parentClip.Left, rect.Left);
                    visible.Top = Math.Max(parentClip.Top, rect.Top);
                    visible.Right = Math.Min(parentClip.Right, rect.Right);
                    visible.Bottom = Math.Min(parentClip.Bottom, rect.Bottom);
                    child.InternalSetOnScreen(new(
                        visible.Left, visible.Top,
                        visible.Right - visible.Left, visible.Bottom - visible.Top
                    ));
                    allOnScreen.Add(child);

                    RectLTRB clip = child.Clip ? visible : parentClip;
                    
                    switch (interactive ? child.Interactive : InteractiveMode.Discard) {
                        case InteractiveMode.Discard:
                            CollectAllOnScreen(all, allOffScreen, allOnScreen, allInteractive, child, collection, rect.Left, rect.Top, clip, false);
                            break;

                        case InteractiveMode.Pass:
                            CollectAllOnScreen(all, allOffScreen, allOnScreen, allInteractive, child, collection, rect.Left, rect.Top, clip, true);
                            break;

                        case InteractiveMode.Process:
                            allInteractive.Add(child);
                            CollectAllOnScreen(all, allOffScreen, allOnScreen, allInteractive, child, collection, rect.Left, rect.Top, clip, true);
                            break;

                        case InteractiveMode.Block:
                            allInteractive.Add(child);
                            CollectAllOnScreen(all, allOffScreen, allOnScreen, allInteractive, child, collection, rect.Left, rect.Top, clip, false);
                            break;
                    }
                }
            }
        }

        private struct RectLTRB {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

    }
}
