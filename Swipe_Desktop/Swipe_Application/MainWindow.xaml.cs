using Swipe_Core.Readers;
using Swipe_Core;
using System.Windows;
using System.ComponentModel;

namespace Swipe_Application
{
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public CurveCollector CurveCollector { get; }
    private COMReader? _comReader;
    private BluetoothReader? _btReader;

    public MainWindow()
    {
        InitializeComponent();
        Closing += MainWindow_Closing;

        _btReader = new BluetoothReader();
        _btReader.Initialize();
        // _comReader = new COMReader("COM7", 115200);

        CurveCollector = new CurveCollector(_btReader);
        CurveCollector.AddKeyValueTracker(">LinAccel_x", 4.0f);
        CurveCollector.AddKeyValueTracker(">LinAccel_y", 4.0f);
        CurveCollector.AddKeyValueTracker(">LinAccel_z", 4.0f);
        CurveCollector.AddKeyValueTracker(">Proximity", 5000.0f);
    }
    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        GraphViewControl.Unload();

        _btReader?.Stop();
        _comReader?.StopCom();
    }
}
}
