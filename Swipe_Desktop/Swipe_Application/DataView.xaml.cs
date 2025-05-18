using LiveCharts.Wpf;
using LiveCharts;
using Swipe_Core;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace Swipe_Application
{
public partial class DataView : UserControl
{
    private List<SeriesCollection> _seriesList = new List<SeriesCollection>();
    private int _downSampleRate = 10;

    private CurveCollector? _collector = null;

    public DataView()
    {
        InitializeComponent();
        Loaded += OnLoaded;

        InputPanel.Children.Add(CreateGraph(">LinAccel_x", 20, -20, Color.FromRgb(100, 0, 0), true, false));
        InputPanel.Children.Add(CreateGraph(">LinAccel_y", 20, -20, Color.FromRgb(0, 100, 0), true, false));
        InputPanel.Children.Add(CreateGraph(">LinAccel_z", 20, -20, Color.FromRgb(0, 0, 100), true, false));
        InputPanel.Children.Add(CreateGraph(">Proximity", 50, 0, Color.FromRgb(100, 100, 0), true, false));

        DataContext = this;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var mainWindow = Utils.FindParent<MainWindow>(this);
        if (mainWindow != null)
        {
            _collector = mainWindow.CurveCollector;
            _collector.OnUpdated += UpdateGraphs;
            _collector.OnDetect += UpdateDetectGraphs;
        }
    }

    public void Unload()
    {
        if (_collector != null)
        {
            _collector.OnUpdated -= UpdateGraphs;
            _collector.OnDetect -= UpdateDetectGraphs;
        }
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

    private void UpdateDetectGraphs(Dictionary<string, List<float>> detectedCurves)
    {
        this.Dispatcher.Invoke(
            () =>
            {
                if (DetectPanel.Children.Count > 1)
                {
                    DetectPanel.Children.RemoveAt(1);
                }

                StackPanel stackPanel = new StackPanel { Orientation = Orientation.Horizontal,
                                                         HorizontalAlignment = HorizontalAlignment.Center,
                                                         VerticalAlignment = VerticalAlignment.Stretch };

                var graph = CreateGraph(">LinAccel_x_detected", 20, -20, Color.FromRgb(100, 0, 0), true, true);
                graph.Series.ElementAt(0).Values.Clear();
                graph.Series.ElementAt(0).Values.AddRange(detectedCurves[">LinAccel_x"].Cast<object>());
                stackPanel.Children.Add(graph);

                graph = CreateGraph(">LinAccel_y_detected", 20, -20, Color.FromRgb(0, 100, 0), true, true);
                graph.Series.ElementAt(0).Values.Clear();
                graph.Series.ElementAt(0).Values.AddRange(detectedCurves[">LinAccel_y"].Cast<object>());
                stackPanel.Children.Add(graph);

                graph = CreateGraph(">LinAccel_z_detected", 20, -20, Color.FromRgb(0, 0, 100), true, true);
                graph.Series.ElementAt(0).Values.Clear();
                graph.Series.ElementAt(0).Values.AddRange(detectedCurves[">LinAccel_z"].Cast<object>());
                stackPanel.Children.Add(graph);

                graph = CreateGraph(">Proximity_detected", 50, 0, Color.FromRgb(100, 100, 0), true, true);
                graph.Series.ElementAt(0).Values.Clear();
                graph.Series.ElementAt(0).Values.AddRange(detectedCurves[">Proximity"].Cast<object>());
                stackPanel.Children.Add(graph);

                DetectPanel.Children.Add(stackPanel);
            });
    }

    private CartesianChart CreateGraph(string title, float max, float min, Color color, bool showLabels, bool temporary)
    {
        var sc = new SeriesCollection { new LineSeries { Title = title, Values = new ChartValues<float>(new float[100]),
                                                         PointGeometrySize = 0.0, Stroke = new SolidColorBrush(color),
                                                         Fill = new SolidColorBrush(
                                                             Color.FromArgb(50, color.R, color.G, color.B)) } };

        if (temporary == false)
        {
            _seriesList.Add(sc);
        }

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

        return chart;
    }
}
}
