using LiveCharts.Wpf;
using LiveCharts;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows;

namespace Swipe_Application
{
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private static LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;

    private static Swipe_Core.Devices.KeyboardDevice? _keyboardDevice;

#region WinAPI Imports
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
#endregion

    static public (CartesianChart, SeriesCollection)
        CreateGraph(string title, float max, float min, Color color, bool showLabels)
    {
        var sc = new SeriesCollection { new LineSeries { Title = title, Values = new ChartValues<float>(new float[100]),
                                                         PointGeometrySize = 0.0, Stroke = new SolidColorBrush(color),
                                                         Fill = new SolidColorBrush(
                                                             Color.FromArgb(50, color.R, color.G, color.B)) } };

        LiveCharts.Wpf.Separator separatorX =
            new LiveCharts.Wpf.Separator { Stroke = System.Windows.Media.Brushes.Gray };
        LiveCharts.Wpf.Separator separatorY =
            new LiveCharts.Wpf.Separator { Stroke = System.Windows.Media.Brushes.Gray };

        var xAxis = new Axis { Title = title, ShowLabels = false, Separator = separatorX };
        var yAxis = new Axis { MaxValue = max, MinValue = min, ShowLabels = showLabels, Separator = separatorY };

        var chart = new CartesianChart { DataTooltip = null, DisableAnimations = true, Hoverable = false, Series = sc };
        chart.Width = 200;
        chart.Height = 100;
        chart.Margin = new System.Windows.Thickness(5);
        chart.AxisX.Add(xAxis);
        chart.AxisY.Add(yAxis);

        return (chart, sc);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _hookID = SetHook(_proc);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        UnhookWindowsHookEx(_hookID);
        base.OnExit(e);
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        Process? curProcess = Process.GetCurrentProcess();
        if (curProcess != null)
        {
            ProcessModule? curModule = curProcess.MainModule;
            if (curModule != null)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }

            return IntPtr.Zero;
        }

        return IntPtr.Zero;
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            int vkCode = Marshal.ReadInt32(lParam);
            _keyboardDevice?.AddKey((int)KeyInterop.KeyFromVirtualKey(vkCode));
        }
        else if (nCode >= 0 && (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP))
        {
            int vkCode = Marshal.ReadInt32(lParam);
            _keyboardDevice?.RemoveKey((int)KeyInterop.KeyFromVirtualKey(vkCode));
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    public static string GetKeysAsText(SortedSet<int> keySet)
    {
        string text = "";
        foreach (int key in keySet)
        {
            if (text.Length > 0)
            {
                text += " + ";
            }

            Key vk = (Key)key;
            text += vk.ToString();
        }

        return text;
    }

    public static void SetKeyboardDevice(Swipe_Core.Devices.KeyboardDevice keyboard)
    {
        _keyboardDevice = keyboard;
    }
}
}
