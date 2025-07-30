using System;
using System.Runtime.InteropServices;

namespace Swipe_Core.Commands
{
public class CenterAndFullscreenCommand : Command
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public override bool Execute()
    {
        IntPtr hWnd = GetForegroundWindow();
        RECT windowRect = new RECT();
        if (hWnd == IntPtr.Zero && !GetWindowRect(hWnd, out windowRect))
        {
            return false;
        }

        ShowWindow(hWnd, 2);
        int width = windowRect.Right - windowRect.Left;
        int height = windowRect.Bottom - windowRect.Top;

        IntPtr primaryMonitor = MonitorFromWindow(IntPtr.Zero, 1);
        MONITORINFO mi = new MONITORINFO();
        mi.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
        if (GetMonitorInfo(primaryMonitor, ref mi))
        {
            int left = mi.rcMonitor.Right / 2 - width / 2;
            int top = mi.rcMonitor.Bottom / 2 - height / 2;
            MoveWindow(hWnd, left, top, width, height, true);
            ShowWindow(hWnd, 3);
            return true;
        }

        return false;
    }
}
}
