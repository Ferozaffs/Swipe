using Swipe_Core.Readers;
using Swipe_Core;
using System.Windows;
using System.ComponentModel;
using System.Windows.Media;
using System.IO;
using System.Reflection;
using Swipe_Core.Devices;
using Swipe_Core.Functions;

namespace Swipe_Application
{
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public FunctionManager FunctionManager { get; private set; }
    public Logger Logger { get; private set; }
    public KeyboardDevice? KeyboardDevice { get; private set; }
    public BandDevice? BandDevice { get; private set; }
    public PadDevice? PadDevice { get; private set; }

    private NotifyIcon _notifyIcon;

    public MainWindow()
    {
        InitializeComponent();
        Closing += MainWindow_Closing;

        Logger = new Logger();

        Directory.CreateDirectory(@"Functions");

        KeyboardDevice = new KeyboardDevice();
        App.SetKeyboardDevice(KeyboardDevice);
        BandDevice = new BandDevice(IDataReader.ReaderType.Bluetooth);
        PadDevice = new PadDevice(IDataReader.ReaderType.Bluetooth);

        BandDevice.OnConnection += UpdateBandConnectionStatus;
        BandDevice.OnStatus += UpdateBandDataStatus;
        PadDevice.OnConnection += UpdatePadConnectionStatus;
        PadDevice.OnStatus += UpdatePadDataStatus;

        _ = BandDevice.Start();
        _ = PadDevice.Start();

        FunctionManager = new FunctionManager(KeyboardDevice, BandDevice, PadDevice);
        FunctionManager.AddLogger(Logger);
        FunctionManager.OnActivation += FunctionActivatied;

        _notifyIcon = new NotifyIcon();
        _notifyIcon.Visible = true;
        _notifyIcon.Text = "Swipe";
        UpdateIcon(System.Drawing.Color.LightYellow);

        _notifyIcon.DoubleClick += ShowWindow;
    }

    public CurveCollector? GetCurveCollector()
    {
        if (BandDevice != null && BandDevice.CurveCollector != null)
        {
            return BandDevice.CurveCollector;
        }

        return null;
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        FunctionManager.OnActivation -= FunctionActivatied;

        _notifyIcon.Dispose();

        if (BandDevice != null)
        {
            BandDevice.OnConnection -= UpdateBandConnectionStatus;
            BandDevice.OnStatus -= UpdateBandDataStatus;
            BandDevice.Stop();
        }
        if (PadDevice != null)
        {
            PadDevice.OnConnection -= UpdatePadConnectionStatus;
            PadDevice.OnStatus -= UpdatePadDataStatus;
            PadDevice.Stop();
        }

        FunctionManager.Unload();
        HomeViewControl.Unload();
        FunctionViewControl.Unload();
        CurveDebuggerViewControl.Unload();
        SettingsViewControl.Unload();
    }

    void UpdateIcon(System.Drawing.Color color)
    {
        int size = 16;
        using (Bitmap bmp = new Bitmap(size, size))
        {
            using (Graphics gfx = Graphics.FromImage(bmp))
            {
                gfx.Clear(color);
            }

            IntPtr hIcon = bmp.GetHicon();
            Icon icon = System.Drawing.Icon.FromHandle(hIcon);
            _notifyIcon.Icon = icon;
        }
    }

    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            FunctionManager.IsExecutionEnabled = true;
        }
        base.OnStateChanged(e);
    }

    private void ShowWindow(object? sender, EventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        HomeViewControl.Visibility = Visibility.Visible;
        FunctionViewControl.Visibility = Visibility.Collapsed;
        CurveDebuggerViewControl.Visibility = Visibility.Collapsed;
        SettingsViewControl.Visibility = Visibility.Collapsed;
        FunctionManager.IsExecutionEnabled = true;
    }

    private void Home_Click(object sender, RoutedEventArgs e)
    {
        HomeViewControl.Visibility = Visibility.Visible;
        FunctionViewControl.Visibility = Visibility.Collapsed;
        CurveDebuggerViewControl.Visibility = Visibility.Collapsed;
        SettingsViewControl.Visibility = Visibility.Collapsed;
        FunctionManager.IsExecutionEnabled = true;
    }

    private void Functions_Click(object sender, RoutedEventArgs e)
    {
        HomeViewControl.Visibility = Visibility.Collapsed;
        FunctionViewControl.Visibility = Visibility.Visible;
        CurveDebuggerViewControl.Visibility = Visibility.Collapsed;
        SettingsViewControl.Visibility = Visibility.Collapsed;
        FunctionManager.IsExecutionEnabled = false;
    }

    private void CurveDebugging_Click(object sender, RoutedEventArgs e)
    {
        HomeViewControl.Visibility = Visibility.Collapsed;
        FunctionViewControl.Visibility = Visibility.Collapsed;
        CurveDebuggerViewControl.Visibility = Visibility.Visible;
        SettingsViewControl.Visibility = Visibility.Collapsed;
        FunctionManager.IsExecutionEnabled = true;
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        HomeViewControl.Visibility = Visibility.Collapsed;
        FunctionViewControl.Visibility = Visibility.Collapsed;
        CurveDebuggerViewControl.Visibility = Visibility.Collapsed;
        SettingsViewControl.Visibility = Visibility.Visible;
        FunctionManager.IsExecutionEnabled = true;
    }

    private void RecalibrateBandButton_Click(object sender, RoutedEventArgs e)
    {
        if (BandDevice != null && BandDevice.CurveCollector != null)
        {
            BandDevice.CurveCollector.Recalibrate();
        }
    }

    private void ConnectionBandButton_Click(object sender, RoutedEventArgs e)
    {
        if (BandDevice != null)
        {
            BandDevice.Connect();
        }
    }

    private void ConnectionPadButton_Click(object sender, RoutedEventArgs e)
    {
        if (PadDevice != null)
        {
            PadDevice.Connect();
        }
    }

    private void UpdateBandConnectionStatus(bool connectionStatus)
    {
        this.Dispatcher.Invoke(() =>
                               {
                                   if (connectionStatus)
                                   {
                                       BandConnectionBtn.Foreground = new SolidColorBrush(Colors.LightGreen);
                                       BandConnectionBtn.Content = "✓";
                                   }
                                   else
                                   {
                                       BandConnectionBtn.Foreground = new SolidColorBrush(Colors.IndianRed);
                                       BandConnectionBtn.Content = "⟳";
                                       BandDataStatusText.Foreground = new SolidColorBrush(Colors.Gray);
                                   }
                               });
    }

    private void UpdatePadConnectionStatus(bool connectionStatus)
    {
        this.Dispatcher.Invoke(() =>
                               {
                                   if (connectionStatus)
                                   {
                                       PadConnectionBtn.Foreground = new SolidColorBrush(Colors.LightGreen);
                                       PadConnectionBtn.Content = "✓";
                                   }
                                   else
                                   {
                                       PadConnectionBtn.Foreground = new SolidColorBrush(Colors.IndianRed);
                                       PadConnectionBtn.Content = "⟳";
                                   }
                               });
    }

    private void UpdateBandDataStatus(CurveCollector.Status status)
    {
        this.Dispatcher.Invoke(() =>
                               {
                                   if (status == CurveCollector.Status.Anomaly)
                                   {
                                       BandDataStatusText.Foreground = new SolidColorBrush(Colors.IndianRed);
                                   }
                                   else if (status == CurveCollector.Status.Calibrating)
                                   {
                                       BandDataStatusText.Foreground = new SolidColorBrush(Colors.LightYellow);
                                   }
                                   else
                                   {
                                       BandDataStatusText.Foreground = new SolidColorBrush(Colors.LightGreen);
                                   }
                               });
    }

    private void UpdatePadDataStatus(bool status)
    {
        this.Dispatcher.Invoke(() =>
                               {
                                   if (status)
                                   {
                                       PadStatus.Foreground = new SolidColorBrush(Colors.LightGreen);
                                   }
                                   else
                                   {
                                       PadStatus.Foreground = new SolidColorBrush(Colors.Gray);
                                   }
                               });
    }

    private async void FunctionActivatied(bool obj)
    {
        UpdateIcon(System.Drawing.Color.LightGreen);
        await Task.Delay(3000);

        UpdateIcon(System.Drawing.Color.LightYellow);
    }
}
}
