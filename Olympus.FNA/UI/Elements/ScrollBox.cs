using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public class ScrollBox : Group {

        public static readonly new Style DefaultStyle = new() {
            { "BarPadding", 4 }
        };

        public Element Content {
            get => this[0];
            set {
                Children.Clear();
                Children.Add(value);
            }
        }

        public ScrollHandle ScrollHandleX;
        public ScrollHandle ScrollHandleY;

        public int Wiggle = 4;

        public Vector2 ScrollDXY;
        private Vector2 ScrollDXYPrev;
        private Vector2 ScrollDXYMax;
        private float ScrollDXYTime;

        public ScrollBox() {
            Cached = false;
            Interactive = InteractiveMode.Process;
            Content = new Dummy();
            ScrollHandleX = new(ScrollAxis.X);
            ScrollHandleY = new(ScrollAxis.Y);
        }

        public override void Update(float dt) {
            int index;
            if ((index = Children.IndexOf(ScrollHandleX)) != 1) {
                if (index != -1)
                    Children.RemoveAt(index);
                Children.Insert(1, ScrollHandleX);
            }
            if ((index = Children.IndexOf(ScrollHandleY)) != 2) {
                if (index != -1)
                    Children.RemoveAt(index);
                Children.Insert(2, ScrollHandleY);
            }

            if (ScrollDXY != default) {
                if (ScrollDXY != ScrollDXYPrev) {
                    ScrollDXYMax = ScrollDXY * 0.1f;
                    ScrollDXYTime = 0f;
                }
                ScrollDXYTime += dt * 4f;
                if (ScrollDXYTime > 1f)
                    ScrollDXYTime = 1f;
                ScrollDXYPrev = ScrollDXY = ScrollDXYMax * (1f - Ease.CubeOut(ScrollDXYTime));

                if (Math.Abs(ScrollDXY.X) < 1f)
                    ScrollDXY.X = 0f;
                if (Math.Abs(ScrollDXY.Y) < 1f)
                    ScrollDXY.Y = 0f;

                ForceScroll(ScrollDXY.ToPoint());
            }

            Element content = Content;
            Vector2 origXY = content.XY;
            Vector2 xy = origXY;
            Point wh = content.WH;
            Point boxWH = WH;

            xy.X = Math.Min(0, Math.Max(xy.X + wh.X, boxWH.X) - wh.X);
            xy.Y = Math.Min(0, Math.Max(xy.Y + wh.Y, boxWH.Y) - wh.Y);

            if (xy != origXY) {
                content.XY = xy;
                AfterScroll();
            }

            base.Update(dt);
        }

        public void AfterScroll() {
            InvalidateFull();
            Content.InvalidatePaint();
            Content.ForceFullReflow();
            ScrollHandleX.InvalidatePaint();
            ScrollHandleX.ForceFullReflow();
            ScrollHandleY.InvalidatePaint();
            ScrollHandleY.ForceFullReflow();
        }

        public void ForceScroll(Point dxy) {
            if (dxy == default)
                return;

            Element content = Content;
            Vector2 xy = -content.XY;
            Vector2 wh = content.WH.ToVector2();
            Vector2 boxWH = WH.ToVector2();

            xy += dxy.ToVector2();

            if (xy.X < 0) {
                xy.X = 0;
            } else if (wh.X < xy.X + boxWH.X) {
                xy.X = wh.X - boxWH.X;
            }

            if (xy.Y < 0) {
                xy.Y = 0;
            } else if (wh.Y < xy.Y + boxWH.Y) {
                xy.Y = wh.Y - boxWH.Y;
            }

            content.XY = (-xy).Round();

            AfterScroll();
        }

        private void OnScroll(MouseEvent.Scroll e) {
            if (!Contains(e.XY))
                return;

            Point dxy = e.ScrollDXY;
            dxy = new(
                dxy.X * -32,
                dxy.Y * -32
            );
            ScrollDXY += dxy.ToVector2();
            ForceScroll(dxy);
            e.Cancel();
        }

        public override void Resize(Point wh) {
            // TODO: The Lua counterpart entirely no-op'd this. How should this behave?
            if (wh.X != WHFalse) {
                if (wh.Y != WHFalse) {
                    WH = wh;
                } else {
                    WH.X = wh.X;
                }
            } else if (wh.Y != WHFalse) {
                WH.Y = wh.Y;
            }
        }

    }

    public enum ScrollAxis {
        X,
        Y,
    }

    public class ScrollHandle : Element {

        public static readonly new Style DefaultStyle = new() {
            {
                "Normal",
                new Style() {
                    new Color(0x80, 0x80, 0x80, 0xa0),
                    { "Width", 3f },
                    { "Radius", 3f },
                }
            },

            {
                "Disabled",
                new Style() {
                    new Color(0x00, 0x00, 0x00, 0x00),
                    { "Width", 3f },
                    { "Radius", 3f },
                }
            },

            {
                "Hovered",
                new Style() {
                    new Color(0xa0, 0xa0, 0xa0, 0xff),
                    { "Width", 6f },
                    { "Radius", 3f },
                }
            },

            {
                "Pressed",
                new Style() {
                    new Color(0x90, 0x90, 0x90, 0xff),
                    { "Width", 6f },
                    { "Radius", 3f },
                }
            },

            new ColorFader(),
            { "Width", new FloatFader() },
            { "WidthMax", 6 },
            { "Radius", new FloatFader() }
        };

        private BasicMesh Mesh;
        private Color PrevColor;
        private float PrevWidth;
        private float PrevRadius;
        private Point PrevWH;

        public bool? Enabled;
        protected bool IsNeeded;

        public readonly ScrollAxis Axis;

        public ScrollHandle(ScrollAxis axis) {
            Interactive = InteractiveMode.Process;
            Mesh = new BasicMesh(Game.GraphicsDevice) {
                Texture = Assets.GradientQuad
            };

            Axis = axis;
            switch (axis) {
                case ScrollAxis.X:
                    Layout.Add(LayoutPass.Pre, LayoutSubpass.Normal, AxisX_LayoutReset);
                    Layout.Add(LayoutPass.Post, LayoutSubpass.Normal, AxisX_LayoutNormal);
                    Events.Add<MouseEvent.Drag>(AxisX_OnDrag);
                    break;

                case ScrollAxis.Y:
                    Layout.Add(LayoutPass.Pre, LayoutSubpass.Normal, AxisY_LayoutReset);
                    Layout.Add(LayoutPass.Post, LayoutSubpass.Normal, AxisY_LayoutNormal);
                    Events.Add<MouseEvent.Drag>(AxisY_OnDrag);
                    break;

                default:
                    throw new ArgumentException($"Unknown scroll axis: {axis}");
            }
        }

        public override void Update(float dt) {
            bool enabled = Enabled ?? IsNeeded;

            Style.Apply(
                !enabled ? "Disabled" :
                (Pressed || Dragged) ? "Pressed" :
                Hovered ? "Hovered" :
                "Normal"
            );

            base.Update(dt);
        }

        public override void DrawContent() {
            if (!(Enabled ?? IsNeeded))
                return;

            SpriteBatch.End();

            Vector2 xy = ScreenXY;
            Point wh = WH;

            Style.GetCurrent(out Color color);
            Style.GetCurrent("Width", out float width);
            Style.GetCurrent("WidthMax", out int widthMax);
            Style.GetCurrent("Radius", out float radius);

            if (PrevColor != color ||
                PrevWidth != width ||
                PrevRadius != radius ||
                PrevWH != wh) {
                PrevColor = color;
                PrevWidth = width;
                PrevRadius = radius;
                PrevWH = wh;

                MeshShapes shapes = Mesh.Shapes;
                shapes.Clear();

                if (color != default) {
                    shapes.Add(new MeshShapes.Rect() {
                        Color = color,
                        Size = new(wh.X, wh.Y),
                        Radius = radius,
                    });
                }

                // Fix UVs manually as we're using a gradient texture.
                for (int i = 0; i < shapes.VerticesMax; i++) {
                    ref VertexPositionColorTexture vertex = ref shapes.Vertices[i];
                    vertex.TextureCoordinate = new(1f, 1f);
                }

                shapes.AutoApply();
            }

            Mesh.Draw(UI.CreateTransform(xy));

            SpriteBatch.BeginUI();
            base.DrawContent();
        }

        // Mostly ported from the Lua counterpart as-is.

        #region X axis scroll layout

        private void AxisX_LayoutReset(LayoutEvent e) {
            Style.GetReal("WidthMax", out int widthMax);
            XY = RealXY = default;
            WH = new(0, widthMax);
        }
        
        private void AxisX_LayoutNormal(LayoutEvent e) {
            ScrollBox box = Parent as ScrollBox ?? throw new Exception("Scroll handles belong into scroll boxes!");
            Element content = box.Content;

            Style.GetReal("WidthMax", out int widthMax);
            box.Style.GetReal("BarPadding", out int padding);

            int boxSize = box.WH.X;
            int contentSize = content.WH.X;
            if (contentSize == 0)
                contentSize = 1;
            int pos = (int) -content.XY.X;

            pos = boxSize * pos / contentSize;
            int size = boxSize * boxSize / contentSize;
            int tail = pos + size;

            if (pos < 1) {
                pos = 1;
            } else if (tail > boxSize - 1) {
                tail = boxSize - 1;
                if (pos > tail) {
                    pos = tail - 1;
                }
            }

            size = Math.Max(1, tail - pos - padding * 2);

            if (size + 1 + padding * 2 + box.Wiggle < contentSize) {
                IsNeeded = true;
                XY = RealXY = new(
                    pos + padding,
                    box.WH.Y - widthMax - 1 - padding
                );
                WH = new(
                    size,
                    widthMax
                );
            } else {
                IsNeeded = false;
                XY = RealXY = default;
                WH = default;
            }
        }

        private void AxisX_OnDrag(MouseEvent.Drag e) {
            ScrollBox box = Parent as ScrollBox ?? throw new Exception("Scroll handles belong into scroll boxes!");
            box.ForceScroll(new(e.DXY.X * box.Content.WH.X / box.WH.X, 0));
        }

        #endregion

        #region Y axis scroll layout

        private void AxisY_LayoutReset(LayoutEvent e) {
            Style.GetReal("WidthMax", out int widthMax);
            XY = RealXY = default;
            WH = new(widthMax, 0);
        }

        private void AxisY_LayoutNormal(LayoutEvent e) {
            ScrollBox box = Parent as ScrollBox ?? throw new Exception("Scroll handles belong into scroll boxes!");
            Element content = box.Content;

            Style.GetReal("WidthMax", out int widthMax);
            box.Style.GetReal("BarPadding", out int padding);

            int boxSize = box.WH.Y;
            int contentSize = content.WH.Y;
            if (contentSize == 0)
                contentSize = 1;
            int pos = (int) -content.XY.Y;

            pos = boxSize * pos / contentSize;
            int size = boxSize * boxSize / contentSize;
            int tail = pos + size;

            if (pos < 1) {
                pos = 1;
            } else if (tail > boxSize - 1) {
                tail = boxSize - 1;
                if (pos > tail) {
                    pos = tail - 1;
                }
            }

            size = Math.Max(1, tail - pos - padding * 2);

            if (size + 1 + padding * 2 + box.Wiggle < contentSize) {
                IsNeeded = true;
                XY = RealXY = new(
                    box.WH.X - widthMax - 1 - padding,
                    pos + padding
                );
                WH = new(
                    widthMax,
                    size
                );
            } else {
                IsNeeded = false;
                XY = RealXY = default;
                WH = default;
            }
        }

        private void AxisY_OnDrag(MouseEvent.Drag e) {
            ScrollBox box = Parent as ScrollBox ?? throw new Exception("Scroll handles belong into scroll boxes!");
            box.ForceScroll(new(0, e.DXY.Y * box.Content.WH.Y / box.WH.Y));
        }

        #endregion

    }
}
