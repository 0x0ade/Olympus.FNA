using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OlympUI.MegaCanvas;
using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public static class UI {

        public static readonly Root Root = new();

        public static int MultiSampleCount = 8;
        public static Vector2 TransformOffset = default;

        public static uint GlobalReflowID = 1;
        public static uint GlobalRepaintID = 1;
        public static uint GlobalUpdateID = 1;
        public static uint GlobalDrawID = 1;

        public static Element? Hovering;
        public static Element? Dragging;
        public static Element? Focusing;

#pragma warning disable CS8618 // Initialize and LoadContent always run first.
        public static Game Game;
        public static UINativeImpl Native;
        public static SpriteBatch SpriteBatch;
        public static CanvasManager MegaCanvas;
#pragma warning restore CS8618

        private static readonly AutoRotatingPool<List<Action>> RunLateListPool = new(8);
        private static List<Action> RunLateList = RunLateListPool.Next();
        private static HashSet<Action> RunOnceList = new();

        private static Point WHPrev;

        private static bool ReflowingPrev;

        public static void Initialize(Game game, UINativeImpl native) {
            Game = game;
            Native = native;
        }

        public static void LoadContent() {
            SpriteBatch?.Dispose();
            SpriteBatch = new SpriteBatch(Game.GraphicsDevice);
            MegaCanvas?.Dispose();
            MegaCanvas = new(Game.GraphicsDevice) {
                MultiSampleCount = MultiSampleCount
            };
        }

        public static void Update(float dt) {
            GlobalUpdateID++;

            bool forceReflow = WHPrev != Root.WH;
            WHPrev = Root.WH;
            Root.Reflowing = forceReflow;

            RunOnceList.Clear();

            #region Input and other event handling
            {

                UIInput.Update();

                if (UIInput.MouseDXY != default || ReflowingPrev) {
                    Element? hoveringPrev = Hovering;
                    Element? hoveringNext = Hovering = UIInput.MouseFocus ? Root.GetInteractiveChildAt(UIInput.Mouse.ToPoint()) : null;

                    if (hoveringPrev != hoveringNext) {
                        hoveringPrev?.InvokeUp(new MouseEvent.Leave());
                        hoveringNext?.InvokeUp(new MouseEvent.Enter());
                    }

                    if (UIInput.MouseDXY != default) {
                        Dragging?.InvokeUp(new MouseEvent.Drag());
                    }
                }

                for (MouseButtons btn = MouseButtons.First; btn <= MouseButtons.Last; btn = (MouseButtons) ((int) btn << 1)) {
                    if (UIInput.Pressed(btn)) {
                        if (Dragging == null || Dragging == Hovering) {
                            Dragging = Hovering;
                            // Focusing?.InvokeUp(TODO: UNFOCUS);
                            Hovering?.InvokeUp(new MouseEvent.Press() {
                                Button = btn,
                                Dragging = true
                            });

                        } else {
                            Hovering?.InvokeUp(new MouseEvent.Press() {
                                Button = btn,
                                Dragging = false
                            });
                        }

                    } else if (UIInput.Released(btn)) {
                        if (UIInput.MousePresses == 0) {
                            Element? dragging = Dragging;
                            Dragging = null;
                            dragging?.InvokeUp(new MouseEvent.Release() {
                                Button = btn,
                                Dragging = false
                            });
                            if (dragging == Hovering) {
                                dragging?.InvokeUp(new MouseEvent.Click() {
                                    Button = btn,
                                    Dragging = false
                                });
                            }

                        } else {
                            Dragging?.InvokeUp(new MouseEvent.Release() {
                                Button = btn,
                                Dragging = true
                            });
                        }
                    }
                }

                if (UIInput.MouseScrollDXY != default)
                    Hovering?.InvokeUp(new MouseEvent.Scroll());

            }
            #endregion

            #region Main loop
            {
                ReflowingPrev = false;

                do {
                    List<Element> allOffScreen = Root.AllOffScreen;
                    List<Element> allOnScreen = Root.AllOnScreen;
                    foreach (Element el in allOffScreen) {
                        if (el.UpdateID == GlobalUpdateID)
                            continue;
                        el.UpdateID = GlobalUpdateID;

                        if (!el.Awakened) {
                            el.Awakened = true;
                            el.Awake();
                            el.UpdateHiddenTime = 0f;
                            el.Update(dt);

                        } else {
                            el.UpdateHiddenTime += dt;
                            el.UpdateHidden(dt);
                        }
                    }

                    foreach (Element el in allOnScreen) {
                        if (el.UpdateID == GlobalUpdateID)
                            continue;
                        el.UpdateID = GlobalUpdateID;

                        if (!el.Awakened) {
                            el.Awakened = true;
                            el.Awake();
                        }

                        float udt = dt + el.UpdateHiddenTime;
                        el.UpdateHiddenTime = 0f;
                        el.Update(udt);
                    }

                    bool reflowing = Root.Reflowing;
                    if (Root.Recollecting) {
                        reflowing = true;
                        Root.CollectLayoutPasses();
                    }

                    if (reflowing || forceReflow) {
                        ReflowingPrev = true;
                        FirstPass:
                        Root.Reflowing = false; // Set to false early so that it can be set to true only when necessary.
                        LayoutEvent reflow = LayoutEvent.Instance;
                        foreach (LayoutPass pass in Root.AllLayoutPasses) {
                            reflow.ForceReflow = forceReflow ? LayoutForce.All : LayoutForce.None;
                            reflow.Pass = pass;
                            reflow.Recursive = true;
                            // No need to InvokeDown as reflows follow their own recursion rules.
                            Root.Invoke(reflow);
                            if (Root.Reflowing)
                                goto FirstPass;
                        }
                    }

                    if (!Root.Recollecting && !reflowing)
                        break;

                    Root.Collect();
                    Root.Recollecting = false;

                } while (true);

            }
            #endregion

            #region RunLate
            {

                List<Action> runLateList = RunLateList;
                RunLateList = RunLateListPool.Next();
                foreach (Action runLate in runLateList)
                    runLate();
                runLateList.Clear();

            }
            #endregion


        }

        public static void Paint() {
            Game.GraphicsDevice.Textures[0] = null;
            MegaCanvas.Update();
            GlobalDrawID++;
            Game.GraphicsDevice.Textures[0] = null;
            SpriteBatch.BeginUI();
            Root.Paint();
            SpriteBatch.End();
        }

        public static void RunLate(Action runLate) {
            RunLateList.Add(runLate);
        }

        public static void RunOnce(Action runLate) {
            if (RunOnceList.Add(runLate))
                runLate();
        }

        public static Matrix CreateTransform()
            => CreateTransform(TransformOffset);
        public static Matrix CreateTransform(Vector2 offset) {
            offset += TransformOffset;
            Viewport view = Game.GraphicsDevice.Viewport;
            return Matrix.CreateOrthographicOffCenter(
                -offset.X, -offset.X + view.Width,
                -offset.Y + view.Height, -offset.Y,
                view.MinDepth, view.MaxDepth
            );
        }

        public static void BeginUI(this SpriteBatch batch)
            => batch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullCounterClockwise,
                null,
                Matrix.CreateTranslation(new(TransformOffset, 0f))
            );


        public static void DrawDebugRect(this SpriteBatch batch, Color color, Rectangle rect) {
            batch.Draw(Assets.White, new Rectangle(rect.Left, rect.Top, 1, rect.Height), color);
            batch.Draw(Assets.White, new Rectangle(rect.Right - 1, rect.Top, 1, rect.Height), color);
            batch.Draw(Assets.White, new Rectangle(rect.Left + 1, rect.Top, rect.Width - 2, 1), color);
            batch.Draw(Assets.White, new Rectangle(rect.Left + 1, rect.Bottom - 1, rect.Width - 2, 1), color);
        }

    }
}
