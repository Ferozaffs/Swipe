using LiveCharts.Wpf;
using LiveCharts;
using System.Windows;
using Swipe_Core;
using Swipe_Core.Readers;
using System.ComponentModel;

namespace Swipe_Application
{
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{

    public SeriesCollection seriesCollectionX { get; set; }
    public SeriesCollection seriesCollectionY { get; set; }
    public SeriesCollection seriesCollectionZ { get; set; }
    public SeriesCollection seriesCollectionProx { get; set; }
    public SeriesCollection seriesCollectionX_Detect { get; set; }
    public SeriesCollection seriesCollectionY_Detect { get; set; }
    public SeriesCollection seriesCollectionZ_Detect { get; set; }
    public SeriesCollection seriesCollectionProx_Detect { get; set; }

    private List<SeriesCollection> _seriesList = new List<SeriesCollection>();
    private CurveCollector _curveCollector;
    private COMReader? _comReader;
    private BluetoothReader? _btReader;

    private int _downSampleRate = 10;

    public MainWindow()
    {
        InitializeComponent();
        Closing += MainWindow_Closing;

        seriesCollectionX = new SeriesCollection { new LineSeries { Title = ">LinAccel_x",
                                                                    Values = new ChartValues<float>(new float[100]),
                                                                    PointGeometrySize = 0.0 } };
        seriesCollectionY = new SeriesCollection { new LineSeries { Title = ">LinAccel_y",
                                                                    Values = new ChartValues<float>(new float[100]),
                                                                    PointGeometrySize = 0.0 } };
        seriesCollectionZ = new SeriesCollection { new LineSeries { Title = ">LinAccel_z",
                                                                    Values = new ChartValues<float>(new float[100]),
                                                                    PointGeometrySize = 0.0 } };
        seriesCollectionProx = new SeriesCollection { new LineSeries { Title = ">Proximity",
                                                                       Values = new ChartValues<float>(new float[100]),
                                                                       PointGeometrySize = 0.0 } };

        seriesCollectionX_Detect =
            new SeriesCollection { new LineSeries { Title = ">LinAccel_x_Detect", Values = new ChartValues<float>(),
                                                    PointGeometrySize = 0.0 } };
        seriesCollectionY_Detect =
            new SeriesCollection { new LineSeries { Title = ">LinAccel_y_Detect", Values = new ChartValues<float>(),
                                                    PointGeometrySize = 0.0 } };
        seriesCollectionZ_Detect =
            new SeriesCollection { new LineSeries { Title = ">LinAccel_z_Detect", Values = new ChartValues<float>(),
                                                    PointGeometrySize = 0.0 } };
        seriesCollectionProx_Detect =
            new SeriesCollection { new LineSeries { Title = ">Proximity_Detect", Values = new ChartValues<float>(),
                                                    PointGeometrySize = 0.0 } };

        _seriesList.Add(seriesCollectionX);
        _seriesList.Add(seriesCollectionY);
        _seriesList.Add(seriesCollectionZ);
        _seriesList.Add(seriesCollectionProx);
        _seriesList.Add(seriesCollectionX_Detect);
        _seriesList.Add(seriesCollectionY_Detect);
        _seriesList.Add(seriesCollectionZ_Detect);
        _seriesList.Add(seriesCollectionProx_Detect);

        _btReader = new BluetoothReader();
        _btReader.Initialize();
        // _comReader = new COMReader("COM7", 115200);

        _curveCollector = new CurveCollector(_btReader);
        _curveCollector.AddKeyValueTracker(">LinAccel_x", 4.0f);
        _curveCollector.AddKeyValueTracker(">LinAccel_y", 4.0f);
        _curveCollector.AddKeyValueTracker(">LinAccel_z", 4.0f);
        _curveCollector.AddKeyValueTracker(">Proximity", 10000.0f);
        _curveCollector.OnUpdated += UpdateGraphs;
        _curveCollector.OnDetect += UpdateDetectGraphs;

        DataContext = this;
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        _comReader?.StopCom();
    }

    private void UpdateGraphs(string key, float value, int index)
    {
        this.Dispatcher.Invoke(() =>
                               {
                                   if (index % _downSampleRate == 0)
                                   {
                                       foreach (var sc in _seriesList)
                                       {
                                           if (sc[0].Title == key)
                                           {
                                               sc[0].Values.RemoveAt(0);
                                               sc[0].Values.Add(value);
                                           }
                                       }
                                   }
                               });
    }

    private void UpdateDetectGraphs(string key, List<float> values)
    {
        this.Dispatcher.Invoke(() =>
                               {
                                   foreach (var sc in _seriesList)
                                   {
                                       if (sc[0].Title == key + "_Detect")
                                       {
                                           sc[0].Values.Clear();
                                           sc[0].Values.AddRange(values.Cast<object>());
                                       }
                                   }
                               });
    }
}
}
