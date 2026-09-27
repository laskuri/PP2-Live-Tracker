using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

namespace PP2_Live_Tracker.Helpers
{
    public static class WindowFinder
    {
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(
            IntPtr hWnd,
            StringBuilder lpString,
            int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(
            IntPtr hWnd,
            out RECT lpRect);


        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }


        public static IntPtr FindProPilkkiWindow()
        {
            IntPtr result = IntPtr.Zero;


            // 1. Yritetään löytää normaalilla ikkunanimellä
            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                StringBuilder title = new StringBuilder(256);
                GetWindowText(hWnd, title, title.Capacity);

                if (title.ToString().Contains("Pro Pilkki 2"))
                {
                    result = hWnd;
                    return false;
                }

                return true;

            }, IntPtr.Zero);


            // 2. Jos ei löytynyt, kokeillaan aktiivista ikkunaa
            // (fullscreen-tuki)
            if (result == IntPtr.Zero)
            {
                IntPtr foreground = GetForegroundWindow();

                if (foreground != IntPtr.Zero)
                {
                    StringBuilder title = new StringBuilder(256);
                    GetWindowText(
                        foreground,
                        title,
                        title.Capacity);

                    result = foreground;
                }
            }


            // 3. Viimeinen yritys prosessien kautta
            if (result == IntPtr.Zero)
            {
                var processes = Process.GetProcesses();

                foreach (var process in processes)
                {
                    try
                    {
                        if (process.MainWindowHandle != IntPtr.Zero &&
                            process.MainWindowTitle.Contains("Pro Pilkki"))
                        {
                            result = process.MainWindowHandle;
                            break;
                        }
                    }
                    catch
                    {
                        // Ohitetaan prosessit joihin ei ole pääsyä
                    }
                }
            }


            return result;
        }


        public static RECT? GetGameWindowRect()
        {
            IntPtr hwnd = FindProPilkkiWindow();

            if (hwnd == IntPtr.Zero)
                return null;

            if (GetWindowRect(hwnd, out RECT rect))
                return rect;

            return null;
        }
    }
}