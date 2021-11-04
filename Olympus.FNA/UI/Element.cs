using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OlympUI.MegaCanvas;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OlympUI {
    public abstract class Element : IEnumerable<Element>, IDisposable {

        private static uint TotalElementCount;
        private static readonly Random RandIDGen = new(0);

        public static readonly Style DefaultStyle = new() {
        };

        public static Action<Element> Cast<T>(Action<T> cb) where T : Element
            => el => cb((T) el);

        public static readonly NullElement Null = new();

        protected bool IsDisposed;

        private readonly Style _Style;
        public Style Style {
            get => _Style;
            set => _Style.Apply(value);
        }

        private readonly Data _Data;
        public Data Data {
            get => _Data;
            set => _Data.Apply(value);
        }

        private EventHandlers _Events;
        public EventHandlers Events {
            get => _Events;
            set {
                if (value != _Events)
                    throw new InvalidOperationException();
            }
        }

        private LayoutHandlers _Layout;
        public LayoutHandlers Layout {
            get => _Layout;
            set {
                if (value != _Layout)
                    throw new InvalidOperationException();
            }
        }

        public Action<Element> Init {
            get => throw new CompilerSatisfactionException();
            set => value(this);
        }

        public uint Collection;

        internal uint UpdateID;

        protected uint ReflowID;
        protected uint ReflowLoopCycles;
        protected uint ReflowLoopCyclesMax = 10;
        public bool Reflowing {
            get => ReflowID != UI.GlobalReflowID;
            set {
                ReflowLoopProtect();
                ReflowID = value ? 0 : UI.GlobalReflowID;
            }
        }

        protected uint RepaintID;
        public bool Repainting {
            get => RepaintID != UI.GlobalRepaintID;
            set => RepaintID = value ? 0 : UI.GlobalRepaintID;
        }

        protected uint ConsecutiveCachedPaints;
        protected uint ConsecutiveUncachedPaints;

        protected RenderTarget2DRegion? CachedTexture;


        internal bool Awakened;

        public float UpdateHiddenTime;


        #region Helpers

        public Game Game => UI.Game;
        public SpriteBatch SpriteBatch => UI.SpriteBatch;

        #endregion


        #region Parameters

        /// <summary>
        /// User-defined or otherwise expected position.
        /// </summary>
        public Vector2 XY;
        public int X {
            get => (int) XY.X;
            set => XY.X = value;
        }
        public int Y {
            get => (int) XY.Y;
            set => XY.Y = value;
        }

        /// <summary>
        /// User-defined or otherwise expected size.
        /// </summary>
        public Point WH;
        public int W {
            get => WH.X;
            set => WH.X = value;
        }
        public int H {
            get => WH.Y;
            set => WH.Y = value;
        }

        public bool Visible = true;

        public virtual InteractiveMode Interactive { get; set; } = InteractiveMode.Pass;

        public virtual bool? ForceDrawAllChildren { get; protected set; }

        public virtual bool? Cached { get; set; } = null;
        public virtual Padding CachePadding { get; set; } = 16;
        public virtual CanvasPool? CachePool { get; set; }
        public virtual bool MSAA { get; set; } = false;

        protected bool _Clip = false;
        public virtual bool Clip {
            get => _Clip;
            set {
                if (_Clip == value)
                    return;
                _Clip = value;
                InvalidateFull();
            }
        }

        #endregion


        #region Hierarchy

        private readonly uint _UniqueID;
        private readonly int _RandID;

        private readonly string _IDFallback;
        private string? _ID;
        public virtual string? ID {
            get => _ID ?? _IDFallback;
            set => _ID = value?.NotEmpty();
        }

        public readonly HashSet<string> Classes = new();

        public Element? Parent;
        private readonly ObservableCollection<Element> _Children = new();
        public ObservableCollection<Element> Children {
            get => _Children;
            set {
                _Children.Clear();
                if (value == null)
                    return;
                foreach (Element child in value)
                    _Children.Add(child);
            }
        }

        public readonly SiblingCollection Siblings;

        public string Path {
            get {
                StringBuilder sb = new();
                sb.Append(ID);

                for (Element? el = Parent; el != null; el = el.Parent) {
                    sb.Insert(0, ".");
                    sb.Insert(0, el.ID);
                }

                return sb.ToString();
                
            }
        }

        public bool IsRooted {
            get {
                for (Element? el = this; el != null; el = el.Parent)
                    if (el == UI.Root)
                        return true;
                return false;
            }
        }

        public Element this[int i] => Children[i];
        public Element this[string id] => GetChild(id) ?? throw new Exception($"Element \"{Path}\" doesn't contain any child with ID \"{id}\"");

        public bool Is<T>() where T : Element => As<T>() != null;
        public virtual T? As<T>() where T : Element => this as T;

        #endregion


        #region Real positions

        private Vector2? _RealXY;

        /// <summary>
        /// Actual position inside of parent.
        /// </summary>
        public virtual Vector2 RealXY {
            get => _RealXY ?? XY;
            set => _RealXY = value;
        }
        public float RealX {
            get => RealXY.X;
            set => RealXY = new(value, RealXY.Y);
        }
        public float RealY {
            get => RealXY.Y;
            set => RealXY = new(RealXY.X, value);
        }
        public virtual void ResetRealXY() => _RealXY = null;

        public Rectangle RealXYWH {
            get {
                Vector2 xy = RealXY;
                Point wh = WH;
                return new((int) xy.X, (int) xy.Y, wh.X, wh.Y);
            }
        }

        public virtual Vector2 ScreenXY {
            get {
                Vector2 xy = new(0, 0);
                for (Element? el = this; el != null; el = el.Parent)
                    xy += el.RealXY;
                return xy;
            }
            set => RealXY = XY = value - ScreenXY;
        }

        public virtual Point InnerXY => new(0, 0);
        public virtual Point InnerWH => WH;

        public Rectangle ScreenXYWH {
            get {
                Vector2 xy = ScreenXY;
                Point wh = WH;
                return new((int) xy.X, (int) xy.Y, wh.X, wh.Y);
            }
        }

        #endregion


        #region Status

        public bool Alive => UI.Root.Collection == Collection;

        public bool Hovered => Owns(UI.Hovering);
        public bool Pressed => Owns(UI.Dragging) && Hovered;
        public bool Dragged => Owns(UI.Dragging);
        public bool Focused => Owns(UI.Focusing);

        public Rectangle? OnScreen { get; protected set; }
        internal void InternalSetOnScreen(Rectangle? value) => OnScreen = value;
        public bool Contains(Point xy) => OnScreen?.Contains(xy) ?? false;

        #endregion


        public Element() {
            _UniqueID = Interlocked.Increment(ref TotalElementCount);
            _RandID = RandIDGen.Next();
            _IDFallback = $"({GetType().Name}:{_UniqueID})";

            _Style = new(this);
            _Data = new();
            _Events = new(this);
            _Layout = new(this);

            Siblings = new(this);

            Children.CollectionChanged += OnCollectionChanged;
        }

        ~Element() {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing) {
            if (IsDisposed)
                return;
            IsDisposed = true;

            CachedTexture?.Dispose();
            CachedTexture = null;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #region Recursion

        public void Add(Element child)
            => Children.Add(child);

        public void Clear()
            => Children.Clear();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
        public IEnumerator<Element> GetEnumerator()
            => Children.GetEnumerator();

        public bool Owns(Element? other) {
            if (other == null)
                return false;
            for (Element? el = other; el != null; el = el.Parent)
                if (el == this)
                    return true;
            return false;
        }

        public void ForEach(Action<Element> cb) {
            cb(this);
            foreach (Element child in Children)
                child.ForEach(cb);
        }

        public T? ForEach<T>(Func<Element, T?> cb) where T : class {
            T? rv;
            if ((rv = cb(this)) != null)
                return rv;
            foreach (Element child in Children)
                if ((rv = child.ForEach(cb)) != null)
                    return rv;
            return null;
        }

        #endregion


        #region Invalidation

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    HashSet<Element> nulls = new();
                    foreach (Element item in e.NewItems ?? throw new NullReferenceException("Child add didn't give new items")) {
                        if (item is NullElement) {
                            nulls.Add(item);
                            continue;
                        }
                        item.Parent = this;
                        item.InvalidateFull();
                    }
                    foreach (Element item in nulls)
                        Children.Remove(item);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (Element item in e.OldItems ?? throw new NullReferenceException("Child remove didn't give old items")) {
                        item.Parent = null;
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    foreach (Element item in sender as ObservableCollection<Element> ?? throw new NullReferenceException("Child clear didn't give sender")) {
                        item.Parent = null;
                    }
                    break;

            }

            UI.Root.InvalidateCollect();
            InvalidateFull();
        }

        public virtual void InvalidateFull() {
            for (Element? el = this; el != null; el = el.Parent) {
                el.Reflowing = true;
                el.Repainting = true;
            }
        }

        public virtual void InvalidateFullDown() {
            foreach (Element el in Children) {
                el.Reflowing = true;
                el.Repainting = true;
                el.InvalidateFullDown();
            }
        }

        public virtual void InvalidatePaint() {
            for (Element? el = this; el != null; el = el.Parent) {
                el.Repainting = true;
            }
        }

        public virtual void InvalidatePaintDown() {
            foreach (Element el in Children) {
                el.Repainting = true;
                el.InvalidatePaintDown();
            }
        }

        public virtual void InvalidateCachedTexture() {
            for (Element? el = this; el != null; el = el.Parent) {
                el.CachedTexture?.Dispose();
                el.CachedTexture = null;
            }
        }

        public virtual void InvalidateCachedTextureDown() {
            foreach (Element el in Children) {
                el.CachedTexture?.Dispose();
                el.CachedTexture = null;
                el.InvalidateCachedTextureDown();
            }
        }

        #endregion


        #region Main Loop

        public virtual void Awake() {
        }

        public virtual void Update(float dt) {
            Style.Update(dt);
        }

        public virtual void UpdateHidden(float dt) {
            UpdateHiddenTime += dt;
        }

        public virtual void DrawContent() {
            if (ForceDrawAllChildren ?? ((Cached ?? true) || !Clip)) {
                foreach (Element el in Children) {
                    el.Paint();
                }
            } else {
                foreach (Element el in Children) {
                    if (el.Visible && el.OnScreen != null) {
                        el.Paint();
                    }
                }
            }
        }

        #endregion


        #region Paint

        // TODO: Element painting is a general TODO area.

        public virtual void Paint() {
            PaintContent();
            if (UI.GlobalDrawDebug) {
                DrawDebug();
            }
        }

        protected virtual void DrawDebug() {
            Rectangle xywh = ScreenXYWH;
            Color c = new();
            c.PackedValue = (uint) _RandID;
            c.A = 0xff;
            UI.SpriteBatch.DrawDebugRect(c, xywh);
            if ((Cached ?? true) && ConsecutiveCachedPaints <= 2) {
                UI.SpriteBatch.DrawDebugRect(Cached == null ? Color.Green : ConsecutiveUncachedPaints <= 2 ? Color.Yellow : Color.Red, new(xywh.X + 1, xywh.Y + 1, xywh.Width - 2, xywh.Height - 2));
            }
        }

        protected virtual void PaintContent() {
            if (Cached == false /* and not null */ || UI.ForceDisableCache) {
                CachedTexture?.Dispose();
                CachedTexture = null;
                DrawContent();
                return;
            }

            bool repainting = Repainting;
            Repainting = false;
            bool pack = false;

            if (!repainting) {
                ConsecutiveUncachedPaints = 0;
                if (ConsecutiveCachedPaints < 16) {
                    ConsecutiveCachedPaints++;
                    if (ConsecutiveCachedPaints < 8 && Cached == null && !MSAA) {
                        DrawContent();
                        return;
                    }

                } else {
                    pack = true;
                }

            } else {
                ConsecutiveCachedPaints = 0;
                if (ConsecutiveUncachedPaints < 16) {
                    ConsecutiveUncachedPaints++;

                } else if (Cached == null && !MSAA) {
                    CachedTexture?.Dispose();
                    CachedTexture = null;
                    DrawContent();
                    return;
                }
            }

            GraphicsDevice gd = Game.GraphicsDevice;
            Vector2 xy = ScreenXY;
            Point wh = WH;
            Padding padding = CachePadding;
            Point whTexture = new(wh.X + padding.X, wh.Y + padding.Y);

            if (CachedTexture != null && (CachedTexture.RT.IsDisposed || CachedTexture.RT.Width < whTexture.X || CachedTexture.RT.Height < whTexture.Y)) {
                CachedTexture?.Dispose();
                CachedTexture = null;
            }

            if ((repainting || UI.GlobalDrawDebug) && CachedTexture != null && CachedTexture.Page != null) {
                CachedTexture?.Dispose();
                CachedTexture = null;
            }

            if (CachedTexture == null) {
                CachedTexture = (CachePool ?? (MSAA ? UI.MegaCanvas.PoolMSAA : UI.MegaCanvas.Pool)).Get(whTexture.X, whTexture.Y);
                repainting = true;
            }

            if (CachedTexture == null) {
                DrawContent();
                return;
            }

            if (repainting || UI.GlobalDrawDebug) {
                SpriteBatch.End();
                GraphicsStateSnapshot gss = new(gd);
                CachedTexture.RT.SetRenderTargetUsage(RenderTargetUsage.PlatformContents);
                gd.SetRenderTarget(CachedTexture.RT);
                gd.Clear(ClearOptions.Target, new Vector4(0, 0, 0, 0), 0, 0);
                CachedTexture.RT.SetRenderTargetUsage(RenderTargetUsage.PreserveContents);
                Vector2 offsPrev = UI.TransformOffset;
                UI.TransformOffset = -xy + new Vector2(padding.Left, padding.Top);
                SpriteBatch.BeginUI();

                DrawContent();

                SpriteBatch.End();
                gss.Apply();
                UI.TransformOffset = offsPrev;
                SpriteBatch.BeginUI();

            } else if (!repainting && pack && CachedTexture.Page == null) {
                RenderTarget2DRegion? packed = UI.MegaCanvas.GetPackedAndFree(CachedTexture, new(0, 0, whTexture.X, whTexture.Y));
                if (packed != null) {
                    CachedTexture = packed;
                    InvalidateCachedTextureDown();
                }
            }

            DrawCachedTexture(CachedTexture.RT, xy, padding, new(CachedTexture.Region.X, CachedTexture.Region.Y, whTexture.X, whTexture.Y));
        }

        protected virtual void DrawCachedTexture(RenderTarget2D rt, Vector2 xy, Padding padding, Rectangle region) {
            SpriteBatch.Draw(
                rt,
                new Rectangle(
                    (int) xy.X - padding.Left,
                    (int) xy.Y - padding.Top,
                    region.Width,
                    region.Height
                ),
                region,
                Color.White
            );
        }

        #endregion


        #region Finders

        public Element? GetParent(string id) {
            for (Element? el = Parent; el != null; el = el.Parent)
                if (el.ID == id)
                    return el;
            return null;
        }

        public T GetParent<T>() where T : Element {
            for (Element? el = Parent; el != null; el = el.Parent)
                if (el is T found)
                    return found;
            throw new Exception($"Failed to get parent of type {typeof(T)} in hierarchy {Path}");
        }

        public Element? GetChild(string id) {
            foreach (Element el in Children)
                if (el.ID == id)
                    return el;
            return null;
        }

        public T GetChild<T>() where T : Element {
            foreach (Element el in Children)
                if (el is T found)
                    return found;
            throw new Exception($"Failed to get child of type {typeof(T)} in hierarchy {Path}");
        }

        public Element? FindChild(string id) {
            foreach (Element el in Children)
                if (el.ID == id)
                    return el;
            foreach (Element el in Children)
                if (el.FindChild(id) is Element found)
                    return found;
            return null;
        }

        public Element? FindChild<T>() where T : Element {
            foreach (Element el in Children)
                if (el is T found)
                    return found;
            foreach (Element el in Children)
                if (el.FindChild<T>() is Element found)
                    return found;
            return null;
        }

        public Element? Hit(int mx, int my) {
            if (Interactive < InteractiveMode.Pass)
                return null;

            if (!ScreenXYWH.Contains(mx, my))
                return null;

            foreach (Element el in Children)
                if (el.Hit(mx, my) is Element found)
                    return found;

            if (Interactive < InteractiveMode.Process)
                return null;
            return this;
        }

        #endregion


        #region Event Listener Logic

        public T Invoke<T>(T e) where T : Event
            => _Events.Invoke(e);

        public T InvokeDown<T>(T e) where T : Event {
            Invoke(e);
            if (e.Status == EventStatus.Cancelled)
                return e;

            foreach (Element child in Children) {
                child.InvokeDown(e);
                if (e.Status == EventStatus.Cancelled)
                    return e;
            }

            return e;
        }

        public T InvokeUp<T>(T e) where T : Event {
            for (Element? el = this; el != null; el = el.Parent) {
                el.Invoke(e);
                if (e.Status == EventStatus.Cancelled)
                    return e;
            }
            return e;
        }

        #endregion

        #region Layout Handler Logic

        internal void INTERNAL_ReflowLoopReset() => ReflowLoopReset();
        protected void ReflowLoopReset() {
            ReflowLoopCycles = 0;
        }

        internal void INTERNAL_ReflowLoopCount() => ReflowLoopCount();
        protected void ReflowLoopCount() {
            if (ReflowLoopCycles >= ReflowLoopCyclesMax)
                throw new Exception($"Element {this} stuck in reflow loop with unknown cause (was Reflowing ever reset?)");
            ReflowLoopCycles++;
        }

        protected void ReflowLoopProtect() {
            if (ReflowLoopCycles >= ReflowLoopCyclesMax)
                throw new Exception($"Element {this} stuck in reflow loop, detected another reflow");
        }

        public void ForceFullReflow() {
            ReflowLoopReset();
            FirstPass:
            LayoutEvent reflow = LayoutEvent.Instance;
            foreach (LayoutPass pass in UI.Root.AllLayoutPasses) {
                reflow.ForceReflow = LayoutForce.One;
                reflow.Pass = pass;
                reflow.Recursive = false;
                // No need to InvokeDown as reflows follow their own recursion rules.
                Invoke(reflow);
                if (Reflowing) {
                    ReflowLoopCount();
                    goto FirstPass;
                }
            }
        }

        public void ForceFullReflowDown() {
            ReflowLoopReset();
            FirstPass:
            LayoutEvent reflow = LayoutEvent.Instance;
            foreach (LayoutPass pass in UI.Root.AllLayoutPasses) {
                reflow.ForceReflow = LayoutForce.All;
                reflow.Pass = pass;
                reflow.Recursive = true;
                // No need to InvokeDown as reflows follow their own recursion rules.
                Invoke(reflow);
                if (Reflowing) {
                    ReflowLoopCount();
                    goto FirstPass;
                }
            }
        }

        private void OnReflow(LayoutEvent e) {
            if (!(Reflowing || this == UI.Root) && e.ForceReflow == LayoutForce.None)
                return;
            if (e.Pass == (LayoutPass) int.MaxValue)
                Reflowing = false;
            if (e.ForceReflow == LayoutForce.One)
                e.ForceReflow = LayoutForce.None;
            Repainting = true;

            _Layout.Invoke(e);
        }

        #endregion

    }

    public class SiblingCollection : IEnumerable<Element> {

        public readonly Element Owner;

        public SiblingCollection(Element owner) {
            Owner = owner;
        }

        public Element this[int i] {
            get {
                if (i == 0)
                    return Owner;
                ObservableCollection<Element> parentChildren = Owner.Parent?.Children ?? throw new Exception("Can't get siblings of orphaned element!");
                return parentChildren[parentChildren.IndexOf(Owner) + i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
        public IEnumerator<Element> GetEnumerator()
            => Owner.Parent?.Children.GetEnumerator() ?? Enumerable.Empty<Element>().GetEnumerator();

    }

    public enum InteractiveMode {
        Discard = -1,
        Pass = 0,
        Process = 1,
        Block = 2
    }
}
