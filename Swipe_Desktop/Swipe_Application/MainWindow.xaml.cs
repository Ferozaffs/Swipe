using Swipe_Core.Readers;
using Swipe_Core;
using System.Windows;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Forms;
using System.Windows.Input;
using System.IO;
using System.Reflection;
using Windows.Devices.PointOfService;
using Windows.Devices.HumanInterfaceDevice;
using Swipe_Core.Devices;
using Swipe_Core.Functions;

namespace Swipe_Application
{
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public FunctionManager FunctionManager { get; }
    // private COMReader? _comReader;
    private BandDevice? _bandDevice;
    private PadDevice? _padDevice;
    private NotifyIcon _notifyIcon;

    public MainWindow()
    {
        InitializeComponent();
        Closing += MainWindow_Closing;

        Directory.CreateDirectory(@"Functions");

        _bandDevice = new BandDevice(IDataReader.ReaderType.Bluetooth);
        _padDevice = new PadDevice(IDataReader.ReaderType.Bluetooth);

        _bandDevice.OnConnection += UpdateBandConnectionStatus;
        _bandDevice.OnStatus += UpdateBandDataStatus;
        _padDevice.OnConnection += UpdatePadConnectionStatus;

        _bandDevice.Start();
        _padDevice.Start();

        FunctionManager = new FunctionManager(_bandDevice, _padDevice);

        _notifyIcon = new NotifyIcon();
        string resourceName = "Swipe_Application.Icon1.ico";

        Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            _notifyIcon.Icon = new Icon(stream);
        }
        _notifyIcon.Visible = true;
        _notifyIcon.Text = "Swipe";

        _notifyIcon.DoubleClick += ShowWindow;
    }

    public CurveCollector? GetCurveCollector()
    {
        if (_bandDevice != null && _bandDevice.CurveCollector != null)
        {
            return _bandDevice.CurveCollector;
        }

        return null;
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        _notifyIcon.Dispose();

        if (_bandDevice != null)
        {
            _bandDevice.OnConnection -= UpdateBandConnectionStatus;
            _bandDevice.OnStatus -= UpdateBandDataStatus;
            _bandDevice.Stop();
        }
        if (_padDevice != null)
        {
            _padDevice.OnConnection -= UpdatePadConnectionStatus;
            _padDevice.Stop();
        }

        HomeViewControl.Unload();
        FunctionViewControl.Unload();
        DataViewControl.Unload();
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
        DataViewControl.Visibility = Visibility.Collapsed;
        FunctionManager.IsExecutionEnabled = true;
    }

    private void Home_Click(object sender, RoutedEventArgs e)
    {
        HomeViewControl.Visibility = Visibility.Visible;
        FunctionViewControl.Visibility = Visibility.Collapsed;
        DataViewControl.Visibility = Visibility.Collapsed;
        FunctionManager.IsExecutionEnabled = true;
    }

    private void Functions_Click(object sender, RoutedEventArgs e)
    {
        HomeViewControl.Visibility = Visibility.Collapsed;
        FunctionViewControl.Visibility = Visibility.Visible;
        DataViewControl.Visibility = Visibility.Collapsed;
        FunctionManager.IsExecutionEnabled = false;
    }

    private void Data_Click(object sender, RoutedEventArgs e)
    {
        HomeViewControl.Visibility = Visibility.Collapsed;
        FunctionViewControl.Visibility = Visibility.Collapsed;
        DataViewControl.Visibility = Visibility.Visible;
        FunctionManager.IsExecutionEnabled = true;
    }

    private void RecalibrateBandButton_Click(object sender, RoutedEventArgs e)
    {
        if (_bandDevice != null && _bandDevice.CurveCollector != null)
        {
            _bandDevice.CurveCollector.Recalibrate();
        }
    }

    private void ConnectionBandButton_Click(object sender, RoutedEventArgs e)
    {
        if (_bandDevice != null)
        {
            _bandDevice.Connect();
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
}
}
