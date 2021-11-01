#if WINDOWS

using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OlympUI;
using Olympus.External;
using SDL2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Olympus.NativeImpls {
    public unsafe partial class NativeWin32 : NativeImpl {

        static readonly IntPtr NULL = (IntPtr) 0;
        static readonly IntPtr ONE = (IntPtr) 1;

        static readonly IntPtr InvisibleRegion = CreateRectRgn(0, 0, -1, -1);

        // Windows 10 1809 introduced acrylic blur.
        static readonly bool SystemHasAcrylic = Environment.OSVersion.Version >= new Version(10, 0, 17763, 0);
        // Windows 11 (22000) is (going to be) the first reliable non-insider build with (almost) fixed acrylic.
        // Apparently self-acrylic is still better than having an acrylic background window though.
        static readonly bool SetAcrylicOnSelf = true; // Environment.GetEnvironmentVariable("OLYMPUS_WIN32_SELFACRYLIC") == "1" || Environment.OSVersion.Version >= new Version(10, 0, 22000, 0);
        static readonly bool SystemHasAcrylicFixes = Environment.OSVersion.Version >= new Version(10, 0, 22000, 0);
        // Windows 11 clips the window for us, Windows 10 doesn't. It's as simple as that... right? No!
        // There once was ClipSelf for testing but it just misbehaved greatly.
        static readonly bool ClipBackgroundAcrylicChild = false; // Could be explored further.
        static readonly bool ExtendedBorderedWindow = SetAcrylicOnSelf && !SystemHasAcrylicFixes;
        static readonly bool ExtendedBorderedWindowResizeOutside = false; // TODO
        static readonly bool LegacyBorderedWindow = ExtendedBorderedWindow;
        static readonly bool SetAcrylicOnSelfMaximized = false; // TODO

        static bool IsOpenGL => FNAHooks.FNA3DDriver?.ToLowerInvariant() == "opengl";
        static bool IsVulkan => FNAHooks.FNA3DDriver?.ToLowerInvariant() == "vulkan";

        private IntPtr HWnd;
        private IntPtr HDc;

        private IntPtr WndProcPrevPtr;
        private WndProcDelegate WndProcCurrent;
        private IntPtr WndProcCurrentPtr;
        private IntPtr DefWindowProcW;

        private RECT MaximizedOffset;

        internal bool WorkaroundDWMMaximizedTitleBarSize;

        internal int OffsetLeft = 0;
        internal int OffsetTop = 0;
        // TODO: DWMWA_CAPTION_BUTTON_BOUNDS returns something close-ish but not fully correct.
        internal int WindowControlsWidth => _IsMaximized ? WorkaroundDWMMaximizedTitleBarSize ? 141 : 142 : 138;
        internal int WindowControlsHeight => _IsMaximized ? WorkaroundDWMMaximizedTitleBarSize ? OffsetTop + 1: OffsetTop + 8 : OffsetTop;

        private bool? _IsTransparentPreferred;
        private bool IsTransparentPreferred => _IsTransparentPreferred ??= (SystemHasAcrylic && Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", null) as int? != 0);
        private bool IsTransparent;
        private float LastBackgroundBlur;

        private bool WindowIdleForceRedraw;

        private bool Ready = false;
        private Bitmap Splash;
        private Win10BackgroundForm? BackgroundAcrylicChild;
        private Win10BackgroundForm? BackgroundResizeChild;
        private Thread? BackgroundResizeChildThread;

        private RECT LastWindowRect;
        private RECT LastClientRect;
        private TimeSpan LastTickStart;
        private TimeSpan LastTickEnd;
        private volatile bool ManuallyBlinking;

        private Thread? BGThread;
        private bool BGThreadRedraw;
        private bool BGThreadRedrawSkip;

        private Display[]? _Displays;
        private Display[] Displays {
            get {
                if (_Displays != null)
                    return _Displays;

                List<Display> displays = new();

                // FIXME: Figure out what adapter is being used by FNA!
                // FIXME: Figure out what adapter is being used by Windows!
                
                // The first adapter in the list is what Windows wants us to use (as per Windows settings overrides).
                // If Windows forces us to use a non-main adapter, it also fakes all outputs to belong to that adapter (luckily no flicker).
                bool isMain = true;

                using SharpDX.DXGI.Factory1 factory = new();
                foreach (SharpDX.DXGI.Adapter1 adapterRaw in factory.Adapters1) {
                    SharpDX.DXGI.AdapterDescription1 adapterInfo = adapterRaw.Description1;
                    Adapter adapter = new(isMain);
                    isMain = false;
                    foreach (SharpDX.DXGI.Output displayRaw in adapterRaw.Outputs) {
                        SharpDX.DXGI.OutputDescription displayInfo = displayRaw.Description;
                        SharpDX.Mathematics.Interop.RawRectangle bounds = displayInfo.DesktopBounds;
                        displays.Add(new(adapter, new(
                            bounds.Left, bounds.Top,
                            bounds.Right - bounds.Left, bounds.Bottom - bounds.Top
                        )));
                    }
                }

                return _Displays = displays.ToArray();
            }
        }

        private Display? CurrentDisplay {
            get {
                RECT rect = LastWindowRect;
                Microsoft.Xna.Framework.Point center = new(
                    rect.Left + (rect.Right - rect.Left) / 2,
                    rect.Top + (rect.Bottom - rect.Top) / 2
                );

                foreach (Display display in Displays)
                    if (display.Bounds.Contains(center))
                        return display;

                return null;
            }
        }

        public override bool CanRenderTransparentBackground => SystemHasAcrylic && (ClientSideDecoration == ClientSideDecorationMode.Title || SetAcrylicOnSelfMaximized || !_IsMaximized);

        public override bool IsActive {
            get {
                IntPtr active = GetActiveWindow();
                IntPtr foreground = GetForegroundWindow();
                if (active == HWnd && foreground == HWnd)
                    return true;
                // Resize child can't ever be active, but it can be in the foreground.
                IntPtr child = BackgroundResizeChild?.FriendlyHandle ?? NULL;
                if (child != NULL && foreground == child)
                    return true;
                return false;
            }
        }

        private bool _IsMaximized;
        public override bool IsMaximized => _IsMaximized;
        public override Microsoft.Xna.Framework.Point WindowPosition {
            get {
                Microsoft.Xna.Framework.Point pos = new();
                SDL.SDL_GetWindowPosition(App.Window.Handle, out pos.X, out pos.Y);
                return pos;
            }
            set => SDL.SDL_SetWindowPosition(App.Window.Handle, value.X, value.Y);
        }

        public Microsoft.Xna.Framework.Point WindowSize => new Microsoft.Xna.Framework.Point(
            App.Graphics.PreferredBackBufferWidth + (LastWindowRect.Right - LastWindowRect.Left) - (LastClientRect.Right - LastClientRect.Left),
            App.Graphics.PreferredBackBufferHeight + (LastWindowRect.Bottom - LastWindowRect.Top) - (LastClientRect.Bottom - LastClientRect.Top)
        );

        private bool? _DarkModePreferred;
        public override bool? DarkModePreferred => _DarkModePreferred ??= (Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", null) as int? == 0);
        private bool _DarkMode;
        public override bool DarkMode {
            get => _DarkMode;
            set {
                if (_DarkMode == value)
                    return;
                _DarkMode = value;

                if (Ready)
                    SetBackgroundBlur(null, true);

                // Doesn't seem to do anything??
                // SetWindowTheme(HWnd, value ? "DarkMode_Explorer" : "Explorer", null);

                // Works but breaks opaque background...
                int dark = value ? 1 : 0;
                WindowCompositionAttributeData data = new() {
                    Attribute = WindowCompositionAttribute.WCA_USEDARKMODECOLORS,
                    Data = (IntPtr) (&dark),
                    SizeOfData = sizeof(int),
                };
                SetWindowCompositionAttribute(HWnd, ref data);
            }
        }

        public override Microsoft.Xna.Framework.Point SplashSize => new(
            Splash.Width / 2,
            Splash.Height / 2
        );

        public Microsoft.Xna.Framework.Color SplashMain = new(0x3b, 0x2d, 0x4a, 0xff);
        public Microsoft.Xna.Framework.Color SplashNeutral = new(0xff, 0xff, 0xff, 0xff);

        public override Microsoft.Xna.Framework.Color SplashBG => DarkMode ? SplashMain : SplashNeutral;
        public override Microsoft.Xna.Framework.Color SplashFG => DarkMode ? SplashNeutral : SplashMain;


        private bool _BackgroundBlur;
        public override bool BackgroundBlur {
            get => _BackgroundBlur;
            set => _BackgroundBlur = SetBackgroundBlur(value ? 0.6f : -1f);
        }

        public override bool ReduceBackBufferResizes => !IsOpenGL && !IsVulkan && !(CurrentDisplay?.Adapter.IsMain ?? true);

        public override OlympUI.Padding Padding =>
            ExtendedBorderedWindow ? new() {
                Left = 8,
                Top = 8, // Could be 0 but let's have equal padding.
                Right = 8,
                Bottom = 8,
            } :
            default;
        public override ClientSideDecorationMode ClientSideDecoration =>
            ExtendedBorderedWindow ? ClientSideDecorationMode.Full :
            ClientSideDecorationMode.Title;

        public BasicMesh? WindowBackgroundMesh;
        public float WindowBackgroundOpacity;


        private bool _IsMouseFocus;
        public override bool IsMouseFocus => _IsMouseFocus;

        private bool _CaptureMouse;
        public override bool CaptureMouse {
            get => _CaptureMouse;
            set {
                if (_CaptureMouse == value)
                    return;
                _CaptureMouse = value;
                SDL.SDL_CaptureMouse(value ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE);
            }
        }

        public override Microsoft.Xna.Framework.Point MouseOffset => (IsMaximized && WorkaroundDWMMaximizedTitleBarSize) ? new(0, -8) : new(0, 0);


        public NativeWin32(App app)
            : base(app) {

            WndProcCurrentPtr = Marshal.GetFunctionPointerForDelegate(WndProcCurrent = WndProc);
            DefWindowProcW = NativeLibrary.GetExport(NativeLibrary.Load("user32.dll"), nameof(DefWindowProcW));

            SDL.SDL_SetWindowMinimumSize(app.Window.Handle, 800, 600);

            SDL.SDL_SysWMinfo info = new();
            SDL.SDL_GetVersion(out info.version);
            SDL.SDL_GetWindowWMInfo(app.Window.Handle, ref info);
            IntPtr hwnd = HWnd = info.info.win.window;
            HDc = info.info.win.hdc;

            // Dark mode and the extended frame area + "blur" don't play along together.
            DarkMode = true;
            _DarkMode = DarkModePreferred ?? false;

            // GDI doesn't seem to have any tinted drawing, so let's just tint the image before drawing...
            Splash = new(OlympUI.Assets.OpenStream("splash_win32.png") ?? throw new Exception("Win32 splash not found"));
            if (System.Drawing.Image.GetPixelFormatSize(Splash.PixelFormat) != 32) {
                using Bitmap splashOld = Splash;
                Splash = splashOld.Clone(new System.Drawing.Rectangle(0, 0, splashOld.Width, splashOld.Height), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }
            System.Drawing.Imaging.BitmapData srcData = Splash.LockBits(
                new System.Drawing.Rectangle(0, 0, Splash.Width, Splash.Height),
                System.Drawing.Imaging.ImageLockMode.ReadWrite,
                Splash.PixelFormat
            );
            byte* splashData = (byte*) srcData.Scan0;
            Microsoft.Xna.Framework.Color splashFG = SplashFG;
            for (int i = (Splash.Width * Splash.Height * 4) - 1 - 3; i > -1; i -= 4) {
                splashData[i + 0] = (byte) ((splashData[i + 0] * splashFG.B) / 255);
                splashData[i + 1] = (byte) ((splashData[i + 1] * splashFG.G) / 255);
                splashData[i + 2] = (byte) ((splashData[i + 2] * splashFG.R) / 255);
                splashData[i + 3] = (byte) ((splashData[i + 3] * splashFG.A) / 255);
            }
            Splash.UnlockBits(srcData);

            // We should now be ready to set our own WndProc. Setting it earlier could possibly access Splash too soon?
            WndProcPrevPtr = SetWindowLongPtr(hwnd, /* GWLP_WNDPROC */ -4, WndProcCurrentPtr);

            // We could prepare the background blur styling here, but doing so turns the splash into additive-ish(?) mode.
            // SetBackgroundBlur(-1f, true);

            // Force-repaint and move the window for the splash screen.
            Microsoft.Xna.Framework.Point size = WindowSize;
            // FIXME: WindowSize seems to be weird. The size is perfect for SetWindowPos but the centering doesn't seem to match.
            SetWindowPos(
                hwnd, NULL,
                GetSystemMetrics(/* SM_CXSCREEN */ 0) / 2 - size.X / 2,
                GetSystemMetrics(/* SM_CYSCREEN */ 1) / 2 - size.Y / 2,
                size.X, size.Y,
                /* SWP_NOZORDER | SWP_FRAMECHANGED */ 0x0004 | 0x0020
            );
            InvalidateRect(hwnd, NULL, true);
            RedrawWindow(hwnd, NULL, NULL, RDW_INVALIDATE | RDW_FRAME | RDW_ERASE | RDW_INTERNALPAINT | RDW_UPDATENOW | RDW_ERASENOW);

            // Let's show the window early.
            // Style value grabbed at runtime, this is what the visible window's style is.
            SetWindowLongPtr(hwnd, /* GWL_STYLE */ -16, (IntPtr) (0x16cf0000 & (long) ~WindowStyles.WS_VISIBLE));
            // Required, otherwise it won't always show up in the task bar or at the top.
            ShowWindow(hwnd, /* SW_SHOWNORMAL */ 1);
        }

        public override void PrepareLate() {
            Ready = true;

            // Enable background blur if possible without risking a semi-transparent splash.
            BackgroundBlur = true;

            // Set from force-dark mode to preferred mode after window creation.
            _DarkMode = !_DarkMode;
            DarkMode = !_DarkMode;

            // Do other late init stuff.

            BGThread = new(BGThreadLoop) {
                Name = "Olympus Win32 Helper Background Thread",
                IsBackground = true,
            };
            BGThread.Start();

            WindowBackgroundMesh = new(App.GraphicsDevice) {
                Shapes = {
                    // Will be updated in BeginDrawBB.
                    new MeshShapes.Quad() {
                        XY1 = new(0, 0),
                        XY2 = new(1, 0),
                        XY3 = new(0, 2),
                        XY4 = new(1, 2),
                    },
                    new MeshShapes.Quad() {
                        XY1 = new(1, 1),
                        XY2 = new(2, 1),
                        XY3 = new(1, 2),
                        XY4 = new(2, 2),
                    },
                },
                MSAA = false,
                Texture = OlympUI.Assets.White,
                BlendState = BlendState.Opaque,
                SamplerState = SamplerState.PointClamp,
            };
            WindowBackgroundMesh.Reload();
        }

        public override Microsoft.Xna.Framework.Point FixWindowPositionDisplayDrag(Microsoft.Xna.Framework.Point pos) {
            pos.Y += OffsetTop;
            return pos;
        }

        public override void Update(float dt) {
            TimeSpan time = App.GlobalWatch.Elapsed;
            LastTickStart = time;
        }

        public override void BeginDrawRT(float dt) {
            Viewport vp = new(0, 0, App.Width, App.Height) {
                MinDepth = 0f,
                MaxDepth = 1f,
            };
            if (_IsMaximized) {
                vp.X += MaximizedOffset.Left;
                vp.Y += MaximizedOffset.Top;
                vp.Width -= MaximizedOffset.Left;
                vp.Height -= MaximizedOffset.Top;
            } else {
                // See WM_NCCALCSIZE handler.
                if (!ExtendedBorderedWindow) {
                    vp.Y += 1;
                    // ... weirdly enough there seems to be yet another special pixel row rule when painting and updating manually?!
                    if (!App.ManualUpdate) {
                        vp.Height -= 1;
                    }
                }
            }
            App.GraphicsDevice.Viewport = vp;
        }

        public override void EndDrawRT(float dt) {
        }

        public override void BeginDrawBB(float dt) {
            // One background color for light mode, three for dark mode (focused black vs focused gray vs unfocused), whyyyy
            if (!SetAcrylicOnSelfMaximized && _IsMaximized && ClientSideDecoration == ClientSideDecorationMode.Title) {
                WindowBackgroundOpacity = 1.5f;
            } else {
                WindowBackgroundOpacity -= dt * 2f;
            }
            if (WindowBackgroundOpacity > 0f && WindowBackgroundMesh != null) {
                // The "ideal" maximized dark bg is 0x2e2e2e BUT it's too bright for the overlay.
                // Light mode is too dark to be called light mode.
                float a = Math.Min(WindowBackgroundOpacity, 1f);
                a = a * a * a * a * a;
                WindowBackgroundMesh.Color =
                    DarkMode ?
                    (new Microsoft.Xna.Framework.Color(0x1e, 0x1e, 0x1e, 0xff) * a) :
                    (new Microsoft.Xna.Framework.Color(0xe0, 0xe0, 0xe0, 0xff) * a);
                fixed (VertexPositionColorTexture* vertices = &WindowBackgroundMesh.Vertices[0]) {
                    vertices[1].Position = new(App.Width - WindowControlsWidth, 0, 0);
                    vertices[2].Position = new(0, App.Height, 0);
                    vertices[3].Position = new(App.Width - WindowControlsWidth, App.Height, 0);
                    vertices[4].Position = new(App.Width - WindowControlsWidth, WindowControlsHeight, 0);
                    vertices[5].Position = new(App.Width, WindowControlsHeight, 0);
                    vertices[6].Position = new(App.Width - WindowControlsWidth, App.Height, 0);
                    vertices[7].Position = new(App.Width, App.Height, 0);
                }
                WindowBackgroundMesh.QueueNext();
                WindowBackgroundMesh.Draw();
            }
        }

        public override void EndDrawBB(float dt) {
            LastTickEnd = App.GlobalWatch.Elapsed;
        }

        public override void BeginDrawDirect(float dt) {
            BeginDrawBB(dt);
            BeginDrawRT(dt);
        }

        public override void EndDrawDirect(float dt) {
            EndDrawRT(dt);
            EndDrawBB(dt);
        }


        private void BGThreadLoop() {
            IntPtr hwnd = HWnd;

            const int sleepDefault = 80;
            int sleep = sleepDefault;

            uint guiThread = GetWindowThreadProcessId(hwnd, out _);
            GUITHREADINFO guiInfo = new() {
                cbSize = Marshal.SizeOf<GUITHREADINFO>()
            };

            while (BGThread != null) {
                Thread.Yield();
                Thread.Sleep(sleep);
                sleep = sleepDefault;

                bool redraw = false;

                if (!redraw) {
                    redraw = WindowIdleForceRedraw;
                    if (!redraw) {
                        GetGUIThreadInfo(guiThread, ref guiInfo);
                        redraw = (guiInfo.flags & GuiThreadInfoFlags.GUI_INMOVESIZE) == GuiThreadInfoFlags.GUI_INMOVESIZE;
                    }
                }

                if (redraw) {
                    BGThreadRedraw = true;
                    sleep = 10;
                    if (!ManuallyBlinking && !BGThreadRedrawSkip) {
                        ManuallyBlinking = true;
                        RedrawWindow(hwnd, NULL, NULL, RDW_INVALIDATE | RDW_FRAME | RDW_ERASE | RDW_INTERNALPAINT | RDW_UPDATENOW | RDW_ERASENOW);
                    }
                    BGThreadRedrawSkip = false;
                } else {
                    BGThreadRedraw = false;
                }
            }
        }


        public bool SetBackgroundBlur(float? alpha = null, bool forceUpdate = false) {
            if (alpha == null)
                alpha = LastBackgroundBlur;
            else
                LastBackgroundBlur = alpha.Value;

            // Use the system theme color, otherwise the titlebar controls won't match.
            Microsoft.Xna.Framework.Color color =
                // FIXME: Maximized background blur leaves an unblurred area at the right edge with self-acrylic.
                (SetAcrylicOnSelf && !SetAcrylicOnSelfMaximized && _IsMaximized) ? default :
                // Force-disable blur.
                alpha < 0f ? default :
                // System-wide transparency is disabled by the user.
                !IsTransparentPreferred ? default :
                // Dark mode transparency is very noticeable.
                DarkMode ? new(0f, 0f, 0f, alpha.Value) :
                // Light mode transparency is barely noticeable. Multiply by 0.5f also matches maximized better.
                // Sadly making it too transparent also makes it too dark.
                new(1f, 1f, 1f, 0.9f * alpha.Value);

            // FIXME: DWM composite checks whatever!

            // Windows 10 1809 introduced acrylic blur but it has become unreliable with further updates.
            if (SystemHasAcrylic) {
                bool wasTransparent = IsTransparent;
                IsTransparent = color != default;
                forceUpdate |= wasTransparent != IsTransparent;

                if (SetAcrylicOnSelf) {
                    if (forceUpdate) {
                        if (ExtendedBorderedWindow) {
                            // DwmExtendFrameIntoClientArea loves to misbehave sometimes...

                        } else {
                            // Setting ONLY the top margin and not using invisible blurbehind works as well,
                            // but -1 everywhere allows for the system to show title bar buttons.
                            MARGINS margins = new() {
                                Left = -1,
                                Right = -1,
                                Top = -1,
                                Bottom = -1,
                            };
                            if (!IsTransparent && !SetAcrylicOnSelfMaximized && _IsMaximized) {
                                // Blame DWM for showing the supposedly invisible "window outside of screen" area with ^^^.
                                WorkaroundDWMMaximizedTitleBarSize = true;
                                margins = new() {
                                    Left = 0,
                                    Right = 0,
                                    // Top = rect.Bottom - rect.Top,
                                    // Bottom = 0,
                                    Top = 32,
                                    Bottom = LastWindowRect.Bottom - LastWindowRect.Top,
                                };
                            }
                            DwmExtendFrameIntoClientArea(HWnd, ref margins);
                            DWM_BLURBEHIND blurBehind =
                                IsTransparent ?
                                new() {
                                    dwFlags = /* DWM_BB_ENABLE | DWM_BB_BLUREGION */ 0x1 | 0x2,
                                    fEnable = true,
                                    hRgnBlur = InvisibleRegion,
                                    fTransitionOnMaximized = false
                                } :
                                new() {
                                    dwFlags = /* DWM_BB_ENABLE | DWM_BB_BLUREGION */ 0x1 | 0x2,
                                    // Dark mode breaks background blur, thus disable it fully.
                                    // Setting this to false turns the BG black / white, but true turns it light / dark gray.
                                    // fEnable = DarkMode,
                                    // Sadly trying to flip this on demand turns the light mode background black at all times.
                                    fEnable = true,
                                    hRgnBlur = NULL,
                                    fTransitionOnMaximized = false
                                };
                            DwmEnableBlurBehindWindow(HWnd, ref blurBehind);
                        }
                    }

                    SetAcrylic(HWnd, color);

                    if (ExtendedBorderedWindow && ExtendedBorderedWindowResizeOutside) {
                        if (BackgroundResizeChildThread == null) {
                            uint threadMain = GetCurrentThreadId();
                            BackgroundResizeChildThread = new(() => {
                                Win10BackgroundForm child = new(this, "Olympus Olympus Win32 Helper Background Resize Window", false);
                                child.Fix(false, -16, 0, 0, 8);
                                child.Show();
                                BackgroundResizeChild = child;
                                // AttachThreadInput(threadChild, threadMain, true);
                                Application.Run(child);
                            }) {
                                Name = "Olympus Win32 Helper Background Resize Window Thread",
                                IsBackground = true,
                            };
                            BackgroundResizeChildThread.SetApartmentState(ApartmentState.STA);
                            BackgroundResizeChildThread.Start();
                            while (BackgroundResizeChild == null)
                                ;
                            BackgroundResizeChild.FriendlyHandle = FindWindowEx(NULL, NULL, null, "Olympus Olympus Win32 Helper Background Resize Window");
                        }
                    } else {
                        BackgroundResizeChild?.Invoke(child => {
                            Application.Exit();
                            child.Close();
                            child.Dispose();
                            BackgroundResizeChild = null;
                        });
                        BackgroundResizeChildThread?.Join();
                        BackgroundResizeChildThread = null;
                    }

                } else if (IsTransparent) {
                    BackgroundResizeChild?.Invoke(child => {
                        Application.Exit();
                        child.Close();
                        child.Dispose();
                        BackgroundResizeChild = null;
                    });
                    BackgroundResizeChildThread?.Join();
                    BackgroundResizeChildThread = null;

                    Win10BackgroundForm? child = BackgroundAcrylicChild;
                    if (child == null) {
                        child = new Win10BackgroundForm(this, "Olympus Olympus Win32 Helper Background Acrylic Window", true);
                        child.Fix(false, 0, 0, 0, 0);
                    }
                    SetAcrylic(child.Handle, color);
                    if (BackgroundAcrylicChild == null) {
                        BackgroundAcrylicChild = child;
                        child.Show();
                    }

                    if (forceUpdate) {
                        // Apparently a surprisingly legitimate method to enforce a fully transparent window.
                        // Layered windows themselves are horrible, blurbehind isn't functional since Win8, but hey, this works!
                        // WS_EX_NOREDIRECTIONBITMAP would be ideal but sadly it'd require too many changes to SDL2 and FNA3D.
                        SetWindowLongPtr(HWnd, /* GWL_EXSTYLE */ -20, (IntPtr) ((long) GetWindowLongPtr(HWnd, -20) | /* WS_EX_LAYERED */ 0x80000));
                        SetLayeredWindowAttributes(HWnd, 0x00000000, 0xff, /* LWA_ALPHA */ 0x2);

                        MARGINS margins = new() {
                            Left = -1,
                            Right = -1,
                            Top = -1,
                            Bottom = -1,
                        };
                        DwmExtendFrameIntoClientArea(HWnd, ref margins);
                        DWM_BLURBEHIND blurBehind = new() {
                            dwFlags = /* DWM_BB_ENABLE | DWM_BB_BLUREGION */ 0x1 | 0x2,
                            fEnable = true,
                            hRgnBlur = InvisibleRegion,
                            fTransitionOnMaximized = false
                        };
                        DwmEnableBlurBehindWindow(HWnd, ref blurBehind);
                    }

                } else if (forceUpdate) {
                    BackgroundResizeChild?.Invoke(child => {
                        Application.Exit();
                        child.Close();
                        child.Dispose();
                        BackgroundResizeChild = null;
                    });
                    BackgroundResizeChildThread?.Join();
                    BackgroundResizeChildThread = null;

                    BackgroundAcrylicChild?.Close();
                    BackgroundAcrylicChild?.Dispose();
                    BackgroundAcrylicChild = null;

                    MARGINS margins = new() {
                        Left = -1,
                        Right = -1,
                        Top = -1,
                        Bottom = -1,
                    };
                    DwmExtendFrameIntoClientArea(HWnd, ref margins);
                    DWM_BLURBEHIND blurBehind = new() {
                        dwFlags = /* DWM_BB_ENABLE | DWM_BB_BLUREGION */ 0x1 | 0x2,
                        fEnable = false,
                        hRgnBlur = NULL,
                        fTransitionOnMaximized = false
                    };
                    DwmEnableBlurBehindWindow(HWnd, ref blurBehind);

                    SetWindowLongPtr(HWnd, /* GWL_EXSTYLE */ -20, (IntPtr) ((long) GetWindowLongPtr(HWnd, -20) & /* WS_EX_LAYERED */ ~0x80000));
                }

            }

            return false;
        }

        private void SetAcrylic(IntPtr hwnd, Microsoft.Xna.Framework.Color color) {
            /* Acrylic accent flags:
             * 0b00000000000000000000000000000001: ???????? Used by Windows Explorer for the task bar
             * 0b00000000000000000000000000000010: [WIN10 ] Force accent color? (At least according to info online)
             * 0b00000000000000000000000000000010: [WIN11+] Opaque(-ish?) but NOT Mica
             * 0b00000000000000000000000000000100: [WIN10+] Backdrop is as big as entire display area (unless clipped by Win11 round corners)
             * 0b00000000000000000000000000000100: [WIN11+] FIX RESIZING LAG BUT fullscreen spills onto all screens when maximized
             * 0b00000000000000000000000000001000: ???????? Not used by anything?
             * 0b00000000000000000000000000010000: [WIN11+] PERMANENT: Clip backdrop. Seems to fix the aforementioned spilling, only on 22000? But also clips too hard in maximized mode.
             * 0b00000000000000000000000000100000: [WIN10+] Left border
             * 0b00000000000000000000000001000000: [WIN10+] Top border
             * 0b00000000000000000000000010000000: [WIN10+] Right border
             * 0b00000000000000000000000100000000: [WIN10+] Bottom border
             */
            AccentPolicy accent =
                color == default ?
                new() {
                    AccentState = AccentState.ACCENT_DISABLED
                } :
                new() {
                    AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                    AccentFlags =
                        (
                        SystemHasAcrylicFixes && !_IsMaximized ?
                        0b00000000000000000000000000010100U :
                        0b00000000000000000000000000000000U
                        ) |
                        (
                        LegacyBorderedWindow ?
                        0b00000000000000000000000111100000U :
                        0b00000000000000000000000000000000U
                        ) |
                        0b00000000000000000000000000000000U,
                    GradientColor = color.PackedValue, // Kinda lucky about alignment here.
            };
            
            WindowCompositionAttributeData data = new() {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                Data = (IntPtr) (&accent),
                SizeOfData = Marshal.SizeOf(accent),
            };
            SetWindowCompositionAttribute(hwnd, ref data);
        }

        private static bool GetIsMaximized(IntPtr hwnd) {
            WINDOWPLACEMENT placement = new() {
                Length = Marshal.SizeOf<WINDOWPLACEMENT>(),
            };
            return GetWindowPlacement(hwnd, ref placement) && placement.ShowCmd == /* SW_MAXIMIZE */ 3;
        }

        private IntPtr WndProc(IntPtr hwnd, WindowsMessage msg, IntPtr wParam, IntPtr lParam) {
            if (hwnd != HWnd)
                return CallWindowProc(WndProcPrevPtr, hwnd, msg, wParam, lParam);

            // Console.WriteLine($"{hwnd}, {msg}: {wParam}, {lParam}");

            TimeSpan time = App.GlobalWatch.Elapsed;
            IntPtr rv;
            HitTestValues hit;
            POINT point, pointReal;
            RECT rect;
            MONITORINFO monitorInfo;
            IntPtr monitor;
            IntPtr hwndResize;

            switch (msg) {
                case WindowsMessage.WM_MOVE:
                    WindowIdleForceRedraw = false;
                    GetWindowRect(HWnd, out LastWindowRect);
                    GetClientRect(HWnd, out LastClientRect);
                    if (Ready) {
                        // Just doing lots of SetBackgroundBlur seems to help with moving lag?!
                        if (SetAcrylicOnSelf) {
                            SetBackgroundBlur(null, true);
                        }
                        // Force-redraw ourselves as on some systems, MOVE isn't followed up by PAINT...
                        if (!BGThreadRedraw) {
                            BGThreadRedrawSkip = true;
                            RedrawWindow(hwnd, NULL, NULL, RDW_INVALIDATE | RDW_FRAME | RDW_ERASE | RDW_INTERNALPAINT | RDW_UPDATENOW | RDW_ERASENOW);
                        }
                        BGThreadRedraw = false;
                    }
                    break;

                case WindowsMessage.WM_SIZE:
                    WindowIdleForceRedraw = false;
                    _IsMaximized = GetIsMaximized(hwnd);
                    GetWindowRect(hwnd, out LastWindowRect);
                    GetClientRect(hwnd, out LastClientRect);
                    if (Ready) {
                        if (_IsMaximized && SetAcrylicOnSelf) {
                            // SOMEHOW flashing the window is needed to update both the background and button colors.
                            // Luckily it also calls SetBackgroundBlur.
                            DarkMode = !DarkMode;
                            DarkMode = !DarkMode;
                        } else {
                            SetBackgroundBlur(null, true);
                        }
                    }
                    if (ClipBackgroundAcrylicChild && BackgroundAcrylicChild != null) {
                        // Note that this won't be round, but given how this should only affect Win10, who cares?
                        // FIXME: Delete the old region? According to MSDN, Windows does that for us.
                        SetWindowRgn(BackgroundAcrylicChild.Handle, CreateRectRgn(LastClientRect.Left, LastClientRect.Top, LastClientRect.Right, LastClientRect.Bottom), false);
                    }
                    break;

                case WindowsMessage.WM_SYSCOMMAND when ((ulong) wParam & 0xFFF0) == /* SC_MOVE */ 0xF010:
                case WindowsMessage.WM_ENTERSIZEMOVE:
                case WindowsMessage.WM_ENTERIDLE:
                    WindowIdleForceRedraw = true;
                    break;
                case WindowsMessage.WM_EXITSIZEMOVE:
                case WindowsMessage.WM_CAPTURECHANGED when ((ulong) wParam) == 0:
                    WindowIdleForceRedraw = false;
                    break;

                case WindowsMessage.WM_ACTIVATE:
                    WindowIdleForceRedraw = false;
                    _IsMaximized = GetIsMaximized(HWnd);
                    GetWindowRect(HWnd, out LastWindowRect);
                    GetClientRect(HWnd, out LastClientRect);
                    break;

                case WindowsMessage.WM_SETTINGCHANGE:
                case WindowsMessage.WM_THEMECHANGED:
                case WindowsMessage.WM_DWMCOMPOSITIONCHANGED:
                    _IsMaximized = GetIsMaximized(HWnd);
                    GetWindowRect(HWnd, out LastWindowRect);
                    GetClientRect(HWnd, out LastClientRect);
                    _DarkModePreferred = null;
                    _IsTransparentPreferred = null;
                    _DarkMode = !(DarkModePreferred ?? _DarkMode);
                    DarkMode = DarkModePreferred ?? !_DarkMode;
                    break;

                case WindowsMessage.WM_DISPLAYCHANGE:
                    _IsMaximized = GetIsMaximized(HWnd);
                    GetWindowRect(HWnd, out LastWindowRect);
                    GetClientRect(HWnd, out LastClientRect);
                    _Displays = null;
                    break;

                case WindowsMessage.WM_NCCALCSIZE:
                    _IsMaximized = GetIsMaximized(HWnd);
                    GetWindowRect(HWnd, out LastWindowRect);
                    GetClientRect(HWnd, out LastClientRect);
                    if (wParam == ONE) {
                        // Call the original WndProc for easier resizing,
                        // BUT undo the title bar at the top.
                        // Fun fact: There's no extra resize region at the top!
                        NCCALCSIZE_PARAMS* ncsize = (NCCALCSIZE_PARAMS*) lParam;
                        RECT prev = ncsize->RgRc0;
                        CallWindowProc(WndProcPrevPtr, hwnd, msg, wParam, lParam);
                        RECT* next = &ncsize->RgRc0;
                        OffsetLeft = next->Left - prev.Left;
                        OffsetTop = next->Top - prev.Top;
                        next->Top = prev.Top;
                        // Make sure that maximized windows line up with the workspace.
                        monitorInfo = new() {
                            Size = Marshal.SizeOf<MONITORINFO>(),
                        };
                        if (_IsMaximized &&
                            (monitor = MonitorFromWindow(hwnd, 0)) != NULL &&
                            GetMonitorInfo(monitor, ref monitorInfo)) {
                            // Setting param->rgrc0 = monitorInfo.WorkArea sadly breaks DwmDefWindowProc.
                            // Some DWM flags are doing a funny though AND we can revert to the pre-DefWindowProc rect just fine?!
                            *next = prev;
                            // SOMEHOW IT KEEPS GOING PAST THE SCREEN AREA TO THE LEFT (AND RIGHT ON WIN10?!) (AND BOTTOM ON IDK ANYMORE).
                            // TOP SEEMS TO BE FINE AFTER MUCH TESTING. I DO NOT KNOW WHAT IS GOING ON ANYMORE.
                            next->Left = monitorInfo.WorkArea.Left;
                            next->Right = monitorInfo.WorkArea.Right;
                            next->Bottom = monitorInfo.WorkArea.Bottom;
                            // The maximized window might extend a bit past the monitor area.
                            MaximizedOffset.Left = monitorInfo.WorkArea.Left - next->Left;
                            MaximizedOffset.Top = monitorInfo.WorkArea.Top - next->Top;
                            MaximizedOffset.Right = monitorInfo.WorkArea.Right - next->Right;
                            MaximizedOffset.Bottom = monitorInfo.WorkArea.Bottom - next->Bottom;
                        } else if (ExtendedBorderedWindow && !IsIconic(hwnd)) {
                            // Windows 10 loves to pretend that the non-client area is acrylic'd.
                            next->Left -= 8;
                            next->Right += 8;
                            next->Bottom += 8;
                        } else {
                            // We SHOULDN'T + 1 this but not doing this will cost us one row of pixels in windowed mode.
                            // param->rgrc0.Top += 1;
                            // Nevermind, turns out that row is also interactive...
                        }
                        return NULL;
                    }
                    break;

                case WindowsMessage.WM_NCACTIVATE:
                case WindowsMessage.WM_NCPAINT:
                    // Calling the default is a must, otherwise Windows 10 (and especially 11) won't add the special border.
                    // Weirdly enough Windows handles / forces a lot of painting outside of WM_NCPAINT as well...
                    // Likewise, handling the PAINT event enables the standard hit test handling... welp.
                    CallWindowProc(WndProcPrevPtr, hwnd, msg, wParam, lParam);
                    // In case this ever becomes necessary again...
                    // NCPaint(hwnd, wParam, System.Drawing.Color.Transparent);
                    return NULL;

                case WindowsMessage.WM_PAINT:
                    if (!Ready) {
                        IntPtr hdc = BeginPaint(hwnd, out PAINTSTRUCT ps);
                        if (hdc != NULL) {
                            try {
                                PaintSplash(hdc);
                            } finally {
                                EndPaint(hwnd, ref ps);
                            }
                        }
                        return NULL;
                    }
                    // NCPaint(hwnd, wParam, System.Drawing.Color.Transparent);
                    break;

                case WindowsMessage.WM_ERASEBKGND:
                    if (!Ready) {
                        PaintSplash(wParam);
                        return NULL;
                    }
                    break;

                case WindowsMessage.WM_MOUSEMOVE:
                    // High poll rate mouses cause SDL2's event pump to lag!
                    // Luckily there's a fix for this, hopefully it'll get merged:
                    // https://github.com/0x0ade/SDL/tree/windows-high-frequency-mouse
                    // Let's continue using our own mouse focus tracking tho.
                    if (!_IsMouseFocus) {
                        _IsMouseFocus = true;
                        TRACKMOUSEEVENT track = new() {
                            cbSize = Marshal.SizeOf<TRACKMOUSEEVENT>(),
                            // dwFlags = /* TME_LEAVE | TME_NONCLIENT */ 0x00000012,
                            dwFlags = /* TME_LEAVE */ 0x00000002,
                            hwndTrack = hwnd,
                            dwHoverTime = 0,
                        };
                        TrackMouseEvent(ref track);
                    }

                    return CallWindowProc(DefWindowProcW, hwnd, msg, wParam, lParam);
                    // return CallWindowProc(WndProcPrevPtr, hwnd, msg, wParam, lParam);

                case WindowsMessage.WM_MOUSELEAVE:
                    _IsMouseFocus = false;
                    break;

                case WindowsMessage.WM_NCMOUSELEAVE:
                    _IsMouseFocus = false;
                    DwmDefWindowProc(hwnd, msg, wParam, lParam, out _);
                    break;

                case WindowsMessage.WM_NCMOUSEHOVER:
                case WindowsMessage.WM_NCMOUSEMOVE:
                // case WindowsMessage.WM_NCMOUSELEAVE:
                    DwmDefWindowProc(hwnd, msg, wParam, lParam, out _);
                    break;

                case WindowsMessage.WM_NCHITTEST:
                    // Don't use SDL's hittest helper as it won't pass it on if we return normal from the SDL side of things.
                    hit = (HitTestValues) CallWindowProc(WndProcPrevPtr, hwnd, msg, wParam, lParam);
                    // Call DwmDefWindowProc to let the system handle window controls.
                    bool hitDWM = DwmDefWindowProc(hwnd, msg, wParam, lParam, out rv);

                    pointReal = new() {
                        X = (short) (((ulong) lParam >> 0) & 0xFFFF),
                        Y = (short) (((ulong) lParam >> 16) & 0xFFFF),
                    };
                    point = pointReal;
                    rect = LastClientRect;
                    const int border = 8;
                    // FIXME: GET THIS THE HELL OUT OF HERE.
                    const int windowButtonWidth = border + 48 * 3 + 2 * 2 + 8;
                    if (hit == HitTestValues.HTCLIENT && ScreenToClient(hwnd, ref point)) {
                        // TODO: Ensure that this doesn't overlap with any UI elements!
                        if (point.Y < OffsetTop) {
                            if (hitDWM && point.Y > 0 && ((HitTestValues) rv) > HitTestValues.HTSYSMENU)
                                return rv;

                            if (point.Y < border && ExtendedBorderedWindow && ExtendedBorderedWindowResizeOutside) {
                                return (IntPtr) HitTestValues.HTNOWHERE;
                            }

                            if (ClientSideDecoration >= ClientSideDecorationMode.Full && point.Y >= border && point.X >= rect.Right - windowButtonWidth && point.X <= rect.Right - border) {
                                return (IntPtr) HitTestValues.HTCLIENT;
                            }

                            // Windows 11 (maybe 10 too) allows the window to be moved in a weird way from the corners.
                            if (_IsMaximized && (point.X <= 32 || point.X > rect.Right - 32)) {
                                return (IntPtr) HitTestValues.HTCLIENT;
                            }

                            if (!(ExtendedBorderedWindow && ExtendedBorderedWindowResizeOutside)) {
                                if (point.Y < border) {
                                    if (point.X < border) {
                                        return (IntPtr) (_IsMaximized ? HitTestValues.HTNOWHERE : HitTestValues.HTTOPLEFT);
                                    }
                                    if (point.X >= rect.Right - border) {
                                        return (IntPtr) (_IsMaximized ? HitTestValues.HTNOWHERE : HitTestValues.HTTOPRIGHT);
                                    }
                                    return (IntPtr) (_IsMaximized ? HitTestValues.HTNOWHERE : HitTestValues.HTTOP);
                                }
                                if (_IsMaximized || (ExtendedBorderedWindow && !ExtendedBorderedWindowResizeOutside && ClientSideDecoration < ClientSideDecorationMode.Title)) {
                                    return (IntPtr) HitTestValues.HTCAPTION;
                                }
                                if (point.X < border) {
                                    return (IntPtr) (_IsMaximized ? HitTestValues.HTNOWHERE : HitTestValues.HTLEFT);
                                }
                                if (point.X >= rect.Right - border) {
                                    return (IntPtr) (_IsMaximized ? HitTestValues.HTNOWHERE : HitTestValues.HTRIGHT);
                                }
                            }

                            return (IntPtr) HitTestValues.HTCAPTION;
                        }

                        if (_IsMaximized && (
                                point.X < border || point.X >= rect.Right - border ||
                                point.Y < border || point.Y >= rect.Bottom - border
                            ))
                            return (IntPtr) HitTestValues.HTNOWHERE;

                        if (!_IsMaximized && ExtendedBorderedWindow && !ExtendedBorderedWindowResizeOutside) {
                            if (point.Y >= rect.Bottom - border) {
                                if (point.X < border) {
                                    return (IntPtr) (_IsMaximized ? HitTestValues.HTNOWHERE : HitTestValues.HTBOTTOMLEFT);
                                }
                                if (point.X >= rect.Right - border) {
                                    return (IntPtr) (_IsMaximized ? HitTestValues.HTNOWHERE : HitTestValues.HTBOTTOMRIGHT);
                                }
                                return (IntPtr) (_IsMaximized ? HitTestValues.HTNOWHERE : HitTestValues.HTBOTTOM);
                            }
                            if (point.X < border) {
                                return (IntPtr) (_IsMaximized ? HitTestValues.HTNOWHERE : HitTestValues.HTLEFT);
                            }
                            if (point.X >= rect.Right - border) {
                                return (IntPtr) (_IsMaximized ? HitTestValues.HTNOWHERE : HitTestValues.HTRIGHT);
                            }
                        }

                        // Splash screens on Windows behave like full-sized titlebars.
                        if (!Ready)
                            return (IntPtr) HitTestValues.HTCAPTION;
                    }
                    return (IntPtr) hit;

                case WindowsMessage.WM_NCRBUTTONUP:
                    // The system menu won't show up outside of the original title bar.
                    hit = (HitTestValues) wParam;
                    pointReal = new() {
                        X = (short) (((ulong) lParam >> 0) & 0xFFFF),
                        Y = (short) (((ulong) lParam >> 16) & 0xFFFF),
                    };
                    point = pointReal;
                    rect = LastClientRect;
                    if (ScreenToClient(hwnd, ref point)) {
                        switch (hit) {
                            case HitTestValues.HTCAPTION:
                                int cmd = TrackPopupMenu(GetSystemMenu(hwnd, false), /* TPM_RETURNCMD */ 0x0100, pointReal.X, pointReal.Y, 0, hwnd, IntPtr.Zero);
                                if (cmd > 0)
                                    SendMessage(hwnd, /* WM_SYSCOMMAND */ 0x0112, (IntPtr) cmd, IntPtr.Zero);
                                break;
                        }
                    }
                    break;

                case WindowsMessage.WM_USER_ADE_MOVE_AFTER:
                    SetWindowPos(hwnd, wParam, 0, 0, 0, 0, (uint) lParam);
                    return NULL;

                case WindowsMessage.WM_USER_ADE_MOVE:
                    SetWindowPos(
                        hwnd, NULL,
                        unchecked((short) (ushort) (((ulong) wParam) >> 0)),
                        unchecked((short) (ushort) (((ulong) wParam) >> 16)),
                        unchecked((short) (ushort) (((ulong) lParam) >> 0)),
                        unchecked((short) (ushort) (((ulong) lParam) >> 16)),
                        /* SWP_NOACTIVATE | SWP_NOOWNERZORDER */ 0x0010 | 0x0200
                    );
                    return NULL;

            }

            rv = CallWindowProc(WndProcPrevPtr, hwnd, msg, wParam, lParam);

            switch (msg) {
                case WindowsMessage.WM_MOVE:
                case WindowsMessage.WM_SIZE:
                case WindowsMessage.WM_ACTIVATE:
                case WindowsMessage.WM_WININICHANGE:
                case WindowsMessage.WM_THEMECHANGED:
                case WindowsMessage.WM_DWMCOMPOSITIONCHANGED:
                case WindowsMessage.WM_DISPLAYCHANGE:
                    _IsMaximized = GetIsMaximized(HWnd);
                    GetWindowRect(HWnd, out LastWindowRect);
                    GetClientRect(HWnd, out LastClientRect);
                    // Excessive but eh whatever.
                    BackgroundAcrylicChild?.Fix(false, 0, 0, 0, 0);
                    hwndResize = BackgroundResizeChild?.FriendlyHandle ?? NULL;
                    if (hwndResize != NULL) {
                        PostMessage(
                            hwndResize, (int) WindowsMessage.WM_USER_ADE_CALL_FIX, NULL,
                            (IntPtr) (
                                ((unchecked((ulong) (byte) (sbyte) -16)) << 0) | 
                                ((unchecked((ulong) (byte) (sbyte) 0)) << 8) |
                                ((unchecked((ulong) (byte) (sbyte) 0)) << 16) |
                                ((unchecked((ulong) (byte) (sbyte) 8)) << 24)
                            )
                        );
                    }
                    break;

                case WindowsMessage.WM_WINDOWPOSCHANGED:
                    // TODO: This likes to fire quite often.
                    WINDOWPOS* posInfo = (WINDOWPOS*) lParam;
                    Win10BackgroundForm? resizer = BackgroundResizeChild;
                    hwndResize = resizer?.FriendlyHandle ?? NULL;
                    GUITHREADINFO guiInfo = new() {
                        cbSize = Marshal.SizeOf<GUITHREADINFO>()
                    };
                    if (resizer != null && hwndResize != NULL && posInfo->HWndInsertAfter != hwndResize && GetGUIThreadInfo(resizer.ThreadID, ref guiInfo) && (guiInfo.flags & GuiThreadInfoFlags.GUI_INMOVESIZE) == 0) {
                        SetWindowPos(
                            hwndResize, posInfo->HWndInsertAfter,
                            0, 0,
                            0, 0,
                            /* SWP_NOSIZE | SWP_NOMOVE | SWP_NOREDRAW | SWP_NOACTIVATE | SWP_DEFERERASE | SWP_NOSENDCHANGING */ 0x0001 | 0x0002 | 0x0008 | 0x0010 | 0x2000 | 0x0400
                        );
                    }
                    break;

                case WindowsMessage.WM_PAINT:
                    ManuallyBlinking = false;
                    break;
            }

            return rv;
        }

        internal static void NCPaint(IntPtr hwnd, IntPtr wParam, System.Drawing.Color color) {
            IntPtr hdc =
                wParam == NULL ?
                GetDCEx(hwnd, wParam, DCX_WINDOW | DCX_USESTYLE | DCX_LOCKWINDOWUPDATE) :
                GetDCEx(hwnd, wParam, DCX_WINDOW | DCX_USESTYLE | DCX_LOCKWINDOWUPDATE | DCX_CACHE | DCX_INTERSECTRGN | DCX_NODELETERGN);

            if (hdc == NULL)
                return;

            try {
                using (Graphics g = Graphics.FromHdc(hdc)) {
                    g.Clear(color);
                }
            } finally {
                ReleaseDC(hwnd, hdc);
            }
        }

        private void PaintSplash(IntPtr hdc) {
            if (hdc == NULL)
                return;

            int width = LastClientRect.Right - LastClientRect.Left;
            int height = LastClientRect.Bottom - LastClientRect.Top;
            using Bitmap tmp = new(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(tmp)) {
                // Fun fact: white is best to hide the fact that there is still a very short white flash.
                // g.Clear(System.Drawing.Color.White);
                Microsoft.Xna.Framework.Color bgXNA = SplashBG;
                System.Drawing.Color bg = System.Drawing.Color.FromArgb(bgXNA.A, bgXNA.R, bgXNA.G, bgXNA.B);
                g.Clear(bg);

                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                g.DrawImage(
                    Splash,
                    width / 2 - Splash.Width / 4,
                    height / 2 - Splash.Height / 4,
                    Splash.Width / 2,
                    Splash.Height / 2
                );
            }

            IntPtr tmphdc = CreateCompatibleDC(hdc);
            SelectObject(tmphdc, tmp.GetHbitmap());
            BitBlt(hdc, 0, 0, width, height, tmphdc, 0, 0, TernaryRasterOperations.SRCCOPY);
            DeleteDC(tmphdc);
        }


#region Misc utils

        [DllImport("user32.dll")]
        static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int smIndex);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [DllImport("user32.dll")]
        static extern int GetWindowRgn(IntPtr hWnd, IntPtr hRgn);

        [DllImport("user32.dll")]
        static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr hWndChildAfter, string? className, string? windowTitle);

#endregion


#region Window accent policy and composition attributes

        enum AccentState {
            ACCENT_DISABLED,
            ACCENT_ENABLE_GRADIENT,
            ACCENT_ENABLE_TRANSPARENTGRADIENT,
            ACCENT_ENABLE_BLURBEHIND,
            ACCENT_ENABLE_ACRYLICBLURBEHIND,
            ACCENT_ENABLE_HOSTBACKDROP,
            ACCENT_TRANSPARENT,
            ACCENT_INVALID_STATE,
        }

        enum CornerStyle {
            CORNER_DEFAULT,
            CORNER_SQUARE,
            CORNER_ROUND,
            CORNER_LIGHTSHADOW_ROUND_SMALL,
            CORNER_LIGHTSHADOW_ROUND,
            CORNER_INVALID,
        }

        enum BackdropStyle {
            BACKDROP_DEFAULT,
            BACKDROP_MICA,
            // Anything past Mica is transparent
            BACKDROP_TRANSPARENT,
        }

        enum PartColorTarget {
            PART_BORDER,
            PART_NC_BACKGROUND,
            PART_NC_TEXT,
            PART_NC_INVALID,
        }

        [StructLayout(LayoutKind.Sequential)]
        struct AccentPolicy {
            public AccentState AccentState;
            public uint AccentFlags;
            public uint GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PartColorPolicy {
            public PartColorTarget Part;
            public uint Color;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WindowCompositionAttributeData {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        enum WindowCompositionAttribute {
            WCA_UNDEFINED,
            WCA_NCRENDERING_ENABLED,
            WCA_NCRENDERING_POLICY,
            WCA_TRANSITIONS_FORCEDISABLED,
            WCA_ALLOW_NCPAINT,
            WCA_CAPTION_BUTTON_BOUNDS,
            WCA_NONCLIENT_RTL_LAYOUT,
            WCA_FORCE_ICONIC_REPRESENTATION,
            WCA_EXTENDED_FRAME_BOUNDS,
            WCA_HAS_ICONIC_BITMAP,
            WCA_THEME_ATTRIBUTES,
            WCA_NCRENDERING_EXILED,
            WCA_NCADORNMENTINFO,
            WCA_EXCLUDED_FROM_LIVEPREVIEW,
            WCA_VIDEO_OVERLAY_ACTIVE,
            WCA_FORCE_ACTIVEWINDOW_APPEARANCE,
            WCA_DISALLOW_PEEK,
            WCA_CLOAK,
            WCA_CLOAKED,
            WCA_ACCENT_POLICY,
            WCA_FREEZE_REPRESENTATION,
            WCA_EVER_UNCLOAKED,
            WCA_VISUAL_OWNER,
            WCA_HOLOGRAPHIC,
            WCA_EXCLUDED_FROM_DDA,
            WCA_PASSIVEUPDATEMODE,
            WCA_USEDARKMODECOLORS,
            WCA_CORNER_STYLE,
            WCA_PART_COLOR,
            WCA_DISABLE_MOVESIZE_FEEDBACK,
            WCA_BACKDROP_STYLE,
            WCA_LAST,
        }

        enum DwmWindowAttribute {
            DWMWA_NCRENDERING_ENABLED = 1,
            DWMWA_NCRENDERING_POLICY,
            DWMWA_TRANSITIONS_FORCEDISABLED,
            DWMWA_ALLOW_NCPAINT,
            DWMWA_CAPTION_BUTTON_BOUNDS,
            DWMWA_NONCLIENT_RTL_LAYOUT,
            DWMWA_FORCE_ICONIC_REPRESENTATION,
            DWMWA_FLIP3D_POLICY,
            DWMWA_EXTENDED_FRAME_BOUNDS,
            DWMWA_HAS_ICONIC_BITMAP,
            DWMWA_DISALLOW_PEEK,
            DWMWA_EXCLUDED_FROM_PEEK,
            DWMWA_CLOAK,
            DWMWA_CLOAKED,
            DWMWA_FREEZE_REPRESENTATION,
            DWMWA_LAST
        }

        [DllImport("user32.dll")]
        static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [DllImport("dwmapi.dll")]
        static extern int DwmGetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, out bool pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        static extern int DwmGetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, out int pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        static extern int DwmGetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, out RECT pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, ref bool pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, ref int pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, ref RECT pvAttribute, int cbAttribute);



#endregion


#region Window margin

        [StructLayout(LayoutKind.Sequential)]
        struct MARGINS {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        };

        [DllImport("dwmapi.dll")]
        static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

#endregion


#region Window handler

        [StructLayout(LayoutKind.Sequential)]
        struct WINDOWPOS {
            public IntPtr HWnd;
            public IntPtr HWndInsertAfter;
            public int X;
            public int Y;
            public int CX;
            public int CY;
            public uint Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct NCCALCSIZE_PARAMS {
            public RECT RgRc0;
            public RECT RgRc1;
            public RECT RgRc2;
            public WINDOWPOS* Pos;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WINDOWPLACEMENT {
            public int Length;
            public int Flags;
            public int ShowCmd;
            public POINT MinPosition;
            public POINT MaxPosition;
            public RECT NormalPosition;
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct MONITORINFO {
            public int Size;
            public RECT Monitor;
            public RECT WorkArea;
            public uint Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MINMAXINFO {
            public POINT Reserved;
            public POINT MaxSize;
            public POINT MaxPosition;
            public POINT MinTrackSize;
            public POINT MaxTrackSize;
        }

        enum HitTestValues {
            HTERROR = -2,
            HTTRANSPARENT = -1,
            HTNOWHERE = 0,
            HTCLIENT = 1,
            HTCAPTION = 2,
            HTSYSMENU = 3,
            HTGROWBOX = 4,
            HTMENU = 5,
            HTHSCROLL = 6,
            HTVSCROLL = 7,
            HTMINBUTTON = 8,
            HTMAXBUTTON = 9,
            HTLEFT = 10,
            HTRIGHT = 11,
            HTTOP = 12,
            HTTOPLEFT = 13,
            HTTOPRIGHT = 14,
            HTBOTTOM = 15,
            HTBOTTOMLEFT = 16,
            HTBOTTOMRIGHT = 17,
            HTBORDER = 18,
            HTOBJECT = 19,
            HTCLOSE = 20,
            HTHELP = 21
        }

        [Flags]
        enum WVR {
            WVR_ALIGNTOP    = 0x0010,
            WVR_ALIGNLEFT   = 0x0020,
            WVR_ALIGNBOTTOM = 0x0040,
            WVR_ALIGNRIGHT  = 0x0080,
            WVR_HREDRAW     = 0x0100,
            WVR_VREDRAW     = 0x0200,
            WVR_REDRAW      = WVR_HREDRAW | WVR_VREDRAW,
            WVR_VALIDRECTS  = 0x0400,
        }

        delegate IntPtr WndProcDelegate(IntPtr hWnd, WindowsMessage msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        struct TRACKMOUSEEVENT {
            public int cbSize;
            public uint dwFlags;
            public IntPtr hwndTrack;
            public int dwHoverTime;
        }

        [DllImport("user32.dll")]
        static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, WindowsMessage uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        [DllImport("user32.dll")]
        static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        static extern bool DestroyMenu(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

        [DllImport("user32.dll")]
        static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

        [DllImport("user32.dll")]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("dwmapi.dll")]
        static extern bool DwmDefWindowProc(IntPtr hWnd, WindowsMessage msg, IntPtr wParam, IntPtr lParam, out IntPtr plResult);

        [DllImport("user32.dll")]
        static extern bool TrackMouseEvent(ref TRACKMOUSEEVENT lpEventTrack);

#endregion


#region Window non-client area repaint

        [StructLayout(LayoutKind.Sequential)]
        struct RECT {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        const int DCX_WINDOW = 0x1;
        const int DCX_CACHE = 0x2;
        const int DCX_INTERSECTRGN = 0x80;
        const int DCX_LOCKWINDOWUPDATE = 400;
        const int DCX_USESTYLE = 0x10000; // Supposedly undocumented for decades.
        const int DCX_NODELETERGN = 0x40000; // Supposedly undocumented for decades.

        const int RDW_INVALIDATE = 0x1;
        const int RDW_INTERNALPAINT = 0x2;
        const int RDW_ERASE = 0x4;
        const int RDW_NOERASE = 0x20;
        const int RDW_NOCHILDREN = 0x40;
        const int RDW_UPDATENOW = 0x100;
        const int RDW_ERASENOW = 0x200;
        const int RDW_FRAME = 0x400;

        [Flags]
        enum WindowStyles : uint {
            WS_BORDER = 0x800000,
            WS_CAPTION = 0xc00000,
            WS_CHILD = 0x40000000,
            WS_CLIPCHILDREN = 0x2000000,
            WS_CLIPSIBLINGS = 0x4000000,
            WS_DISABLED = 0x8000000,
            WS_DLGFRAME = 0x400000,
            WS_GROUP = 0x20000,
            WS_HSCROLL = 0x100000,
            WS_MAXIMIZE = 0x1000000,
            WS_MAXIMIZEBOX = 0x10000,
            WS_MINIMIZE = 0x20000000,
            WS_MINIMIZEBOX = 0x20000,
            WS_OVERLAPPED = 0x0,
            WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_SIZEFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
            WS_POPUP = 0x80000000u,
            WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,
            WS_SIZEFRAME = 0x40000,
            WS_SYSMENU = 0x80000,
            WS_TABSTOP = 0x10000,
            WS_VISIBLE = 0x10000000,
            WS_VSCROLL = 0x200000,
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetDCEx(IntPtr hWnd, IntPtr hRgn, int flags);

        [DllImport("user32.dll")]
        static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hRgnUpdate, int flags);

        [DllImport("user32.dll")]
        static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

#endregion


#region Window client area repaint

        enum TernaryRasterOperations : uint {
            SRCCOPY = 0x00CC0020,
            SRCPAINT = 0x00EE0086,
            SRCAND = 0x008800C6,
            SRCINVERT = 0x00660046,
            SRCERASE = 0x00440328,
            NOTSRCCOPY = 0x00330008,
            NOTSRCERASE = 0x001100A6,
            MERGECOPY = 0x00C000CA,
            MERGEPAINT = 0x00BB0226,
            PATCOPY = 0x00F00021,
            PATPAINT = 0x00FB0A09,
            PATINVERT = 0x005A0049,
            DSTINVERT = 0x00550009,
            BLACKNESS = 0x00000042,
            WHITENESS = 0x00FF0062,
            CAPTUREBLT = 0x40000000,
        }


        [StructLayout(LayoutKind.Sequential)]
        struct PAINTSTRUCT {
            public IntPtr hdc;
            public bool fErase;
            public RECT rcPaint;
            public bool fRestore;
            public bool fIncUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] rgbReserved;
        }

        [DllImport("user32.dll")]
        static extern IntPtr BeginPaint(IntPtr hwnd, out PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        static extern bool EndPaint(IntPtr hWnd, [In] ref PAINTSTRUCT lpPaint);

        [DllImport("gdi32.dll")]
        static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hObjSource, int nXSrc, int nYSrc, TernaryRasterOperations dwRop);

#endregion


#region Child window

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int nWidth, int nHeight, uint uFlags);

#endregion


#region Transparent window

        enum CombineRgnStyles {
            RGN_AND = 1,
            RGN_OR,
            RGN_XOR,
            RGN_DIFF,
            RGN_COPY,
            RGN_MAX,
            RGN_MIN = RGN_AND,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BLENDFUNCTION {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DWM_BLURBEHIND {
            public int dwFlags;
            public bool fEnable;
            public IntPtr hRgnBlur;
            public bool fTransitionOnMaximized;
        }

        [DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool UpdateLayeredWindow(
            IntPtr hwnd,
            IntPtr hdcDst, IntPtr pptDst, IntPtr psize,
            IntPtr hdcSrc, ref POINT pptSrc,
            uint crKey, [In] ref BLENDFUNCTION pblend,
            uint dwFlags
        );

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);

        [DllImport("gdi32.dll")]
        static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll")]
        static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);

        [DllImport("dwmapi.dll")]
        static extern void DwmEnableBlurBehindWindow(IntPtr hwnd, ref DWM_BLURBEHIND blurBehind);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateRectRgn(int x1, int y1, int x2, int y2);

        [DllImport("gdi32.dll")]
        static extern int CombineRgn(IntPtr hrgnDest, IntPtr hrgnSrc1, IntPtr hrgnSrc2, CombineRgnStyles fnCombineMode);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

#endregion


#region Dark Mode

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string? pszSubIdList);

#endregion


#region GUI Thread

        [Flags]
        enum GuiThreadInfoFlags {
            GUI_CARETBLINKING = 0x00000001,
            GUI_INMENUMODE = 0x00000004,
            GUI_INMOVESIZE = 0x00000002,
            GUI_POPUPMENUMODE = 0x00000010,
            GUI_SYSTEMMENUMODE = 0x00000008
        }

        [StructLayout(LayoutKind.Sequential)]
        struct GUITHREADINFO {
            public int cbSize;
            public GuiThreadInfoFlags flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public System.Drawing.Rectangle rcCaret;
        }

        [DllImport("user32.dll")]
        static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

#endregion


#region Custom display info

        private class Adapter {

            public bool IsMain;

            public Adapter(bool isMain) {
                IsMain = isMain;
            }

        }

        private class Display {

            public Adapter Adapter;
            public Microsoft.Xna.Framework.Rectangle Bounds;

            public Display(Adapter adapter, Microsoft.Xna.Framework.Rectangle bounds) {
                Adapter = adapter;
                Bounds = bounds;
            }

        }

#endregion

    }
}

#endif
