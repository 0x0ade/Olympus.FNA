#if WINDOWS

using Microsoft.Xna.Framework;
using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Olympus.NativeImpls {
	public unsafe partial class NativeWin32 : NativeImpl {
		private class Win10BackgroundForm : Form {

            NativeWin32 Native;

            RECT RectPrev;

            new bool Shown = true;

            protected override CreateParams CreateParams {
                get {
                    CreateParams args = base.CreateParams;
                    args.ExStyle |= 0x80; // WS_EX_TOOLWINDOW
                    return args;
                }
            }

            public Win10BackgroundForm(NativeWin32 native) {
                Native = native;

                ShowInTaskbar = false;
                ControlBox = false;
                BackColor = System.Drawing.Color.FromArgb(255, 0, 0, 0);

                MARGINS margins = new() {
                    Left = 0,
                    Right = 0,
                    Top = 1,
                    Bottom = 0,
                };
                DwmExtendFrameIntoClientArea(Handle, ref margins);

                int policy = 1;
                DwmSetWindowAttribute(Handle, DwmWindowAttribute.DWMWA_NCRENDERING_POLICY, ref policy, sizeof(int));
            }

            public void Fix(bool force = false) {
                GetWindowRect(Native.HWnd, out RECT rectWindow);
                GetClientRect(Native.HWnd, out RECT rectClient);
                RECT rect = new() {
                    Left = rectWindow.Left + rectClient.Left + Native.OffsetLeft,
                    Top = rectWindow.Top + rectClient.Top,
                    Right = rectWindow.Left + rectClient.Right + Native.OffsetLeft,
                    Bottom = rectWindow.Top + rectClient.Bottom,
                };
                if (!force &&
                    rect.Left == RectPrev.Left && rect.Top == RectPrev.Top &&
                    rect.Right == RectPrev.Right && rect.Bottom == RectPrev.Bottom &&
                    GetWindow(Native.HWnd, /* GW_HWNDNEXT */ 2) == Handle)
                    return;

                if (rectWindow.Left == -32000 && rectWindow.Top == -32000 &&
                    rectWindow.Right == -32000 && rectWindow.Bottom == -32000) {
                    if (Shown) {
                        Shown = false;
                        Hide();
                    }
                } else if (!Shown) {
                    Shown = true;
                    Show();
                }

                RectPrev = rect;
                SetWindowPos(
                    Handle, Native.HWnd,
                    rect.Left, rect.Top,
                    rect.Right - rect.Left, rect.Bottom - rect.Top,
                    /* SWP_NOACTIVATE | SWP_DEFERERASE */ 0x0010 | 0x2000
                );
#if false
                SetWindowPos(
                    Native.HWnd, Handle,
                    0, 0,
                    0, 0,
                    /* SWP_NOSIZE | SWP_NOMOVE | SWP_NOREDRAW | SWP_NOACTIVATE | SWP_DEFERERASE */ 0x0001 | 0x0002 | 0x0008 | 0x0010 | 0x2000
                );
#endif
            }

            protected override void WndProc(ref Message m) {
                switch ((WindowsMessage) m.Msg) {
                    case WindowsMessage.WM_NCCALCSIZE:
                        // On Windows 10, the background blur extends into the non-client area.
                        // We thus MUST return the entire proposed area as the client area.
                        if (m.WParam == ONE) {
                            m.Result = NULL;
                            return;
                        }
                        break;

                    case WindowsMessage.WM_NCHITTEST:
                        m.Result = (IntPtr) HitTestValues.HTNOWHERE;
                        return;

                    case WindowsMessage.WM_NCPAINT:
                        NCPaint(Handle, m.WParam, System.Drawing.Color.Transparent);
                        return;
                }

                base.WndProc(ref m);
            }

        }
	}
}

#endif
