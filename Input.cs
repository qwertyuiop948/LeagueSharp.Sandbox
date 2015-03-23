using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LeagueSharp.Sandbox
{
    internal static class Input
    {
        public delegate int Win32WndProc(IntPtr hWnd, int msg, int wParam, int lParam);

        public const int GWL_WNDPROC = -4;
        public const int WM_KEYUP = 0x0101;
        public static IntPtr OldWndProc = IntPtr.Zero;
        public static Win32WndProc NewWndProc;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(IntPtr zeroOnly, string lpWindowName);

        [DllImport("user32")]
        public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, Win32WndProc newProc);

        [DllImport("user32")]
        public static extern int CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, int msg, int wParam, int lParam);

        public static void SubclassHWnd(IntPtr hWnd)
        {
            NewWndProc = MyWndProc;
            OldWndProc = SetWindowLong(hWnd, GWL_WNDPROC, NewWndProc);
            Logs.InfoFormat("[PID:{0}] Sandbox.Bootstrap: Setup Keyboardhook ({1})", Sandbox.Pid, Process.GetCurrentProcess().MainWindowHandle.ToString());
        }

        public static int MyWndProc(IntPtr hWnd, int msg, int wParam, int lParam)
        {
            if (msg == WM_KEYUP)
            {
                if (wParam == Sandbox.ReloadKey)
                {
                    Sandbox.Reload();
                }

                if (wParam == Sandbox.ReloadAndRecompileKey)
                {
                    Sandbox.Recompile();
                }

                if (wParam == Sandbox.UnloadKey)
                {
                    Sandbox.Unload();
                }
            }

            return CallWindowProc(OldWndProc, hWnd, msg, wParam, lParam);
        }
    }
}