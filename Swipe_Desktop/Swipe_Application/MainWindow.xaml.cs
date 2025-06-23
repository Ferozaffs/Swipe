using Swipe_Core.Readers;
using Swipe_Core;
using System.Windows;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Forms;
using System.Windows.Input;
using System.IO;
using System.Reflection;

namespace Swipe_Application
{
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public CurveCollector CurveCollector { get; }
    public FunctionManager FunctionManager { get; }
    // private COMReader? _comReader;
    private BluetoothReader? _btReader;
    private NotifyIcon _notifyIcon;

    public MainWindow()
    {
        InitializeComponent();
        Closing += MainWindow_Closing;

        _btReader = new BluetoothReader();
        _btReader.OnConnection += UpdateConnectionStatus;
        _btReader.Initialize();
        // _comReader = new COMReader("COM7", 115200);

        CurveCollector = new CurveCollector(_btReader);
        CurveCollector.AddKeyValueTracker(">LinAccel_x", 4.0f);
        CurveCollector.AddKeyValueTracker(">LinAccel_y", 4.0f);
        CurveCollector.AddKeyValueTracker(">LinAccel_z", 4.0f);
        CurveCollector.AddKeyValueTracker(">Proximity", 5000.0f);

        CurveCollector.OnStatus += UpdateDeviceStatus;

        FunctionManager = new FunctionManager(CurveCollector);

        _notifyIcon = new NotifyIcon();
        string resourceName = "Swipe_Application.Icon1.ico";
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
        {
            _notifyIcon.Icon = new Icon(stream);
        }
        _notifyIcon.Visible = true;
        _notifyIcon.Text = "Swipe";

        _notifyIcon.DoubleClick += _notifyIcon_ShowWindow;
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        _notifyIcon.Dispose();

        CurveCollector.OnStatus -= UpdateDeviceStatus;
        if (_btReader != null)
        {
            _btReader.OnConnection -= UpdateConnectionStatus;
        }

        HomeViewControl.Unload();
        FunctionViewControl.Unload();
        DataViewControl.Unload();

        _btReader?.Stop();
        //_comReader?.StopCom();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
        base.OnStateChanged(e);
    }

    private void _notifyIcon_ShowWindow(object? sender, EventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void Home_Click(object sender, RoutedEventArgs e)
    {
        HomeViewControl.Visibility = Visibility.Visible;
        FunctionViewControl.Visibility = Visibility.Collapsed;
        DataViewControl.Visibility = Visibility.Collapsed;
    }

    private void Functions_Click(object sender, RoutedEventArgs e)
    {
        HomeViewControl.Visibility = Visibility.Collapsed;
        FunctionViewControl.Visibility = Visibility.Visible;
        DataViewControl.Visibility = Visibility.Collapsed;
    }

    private void Data_Click(object sender, RoutedEventArgs e)
    {
        HomeViewControl.Visibility = Visibility.Collapsed;
        FunctionViewControl.Visibility = Visibility.Collapsed;
        DataViewControl.Visibility = Visibility.Visible;
    }

    private void RecalibrateButton_Click(object sender, RoutedEventArgs e)
    {
        if (CurveCollector != null)
        {
            CurveCollector.Recalibrate();
        }
    }

    private void ConnectionBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_btReader != null)
        {
            _btReader.Connect();
        }
    }

    private void UpdateConnectionStatus(bool connectionStatus)
    {
        this.Dispatcher.Invoke(() =>
                               {
                                   if (connectionStatus)
                                   {
                                       ConnectionBtn.Foreground = new SolidColorBrush(Colors.LightGreen);
                                       ConnectionBtn.Content = "✓";
                                   }
                                   else
                                   {
                                       ConnectionBtn.Foreground = new SolidColorBrush(Colors.IndianRed);
                                       ConnectionBtn.Content = "⟳";
                                       DeviceStatusText.Foreground = new SolidColorBrush(Colors.Gray);
                                   }
                               });
    }

    private void UpdateDeviceStatus(CurveCollector.Status status)
    {
        this.Dispatcher.Invoke(() =>
                               {
                                   if (status == CurveCollector.Status.Anomaly)
                                   {
                                       DeviceStatusText.Foreground = new SolidColorBrush(Colors.IndianRed);
                                   }
                                   else if (status == CurveCollector.Status.Calibrating)
                                   {
                                       DeviceStatusText.Foreground = new SolidColorBrush(Colors.LightYellow);
                                   }
                                   else
                                   {
                                       DeviceStatusText.Foreground = new SolidColorBrush(Colors.LightGreen);
                                   }
                               });
    }
}
}
