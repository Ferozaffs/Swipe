using LiveCharts.Wpf;
using LiveCharts;
using Swipe_Core;
using Swipe_Core.Readers;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using Windows.UI.ViewManagement;

namespace Swipe_Application
{
public partial class GraphView : UserControl
{
    private List<SeriesCollection> _seriesList = new List<SeriesCollection>();
    private int _downSampleRate = 10;

    public GraphView()
    {
        InitializeComponent();
        Loaded += OnLoaded;

        InputPanel.Children.Add(CreateGraph(">LinAccel_x", 20, -20, Color.FromRgb(100, 0, 0), true, false));
        InputPanel.Children.Add(CreateGraph(">LinAccel_y", 20, -20, Color.FromRgb(0, 100, 0), true, false));
        InputPanel.Children.Add(CreateGraph(">LinAccel_z", 20, -20, Color.FromRgb(0, 0, 100), true, false));
        InputPanel.Children.Add(CreateGraph(">Proximity", 25000, 0, Color.FromRgb(100, 100, 0), true, false));

        DataContext = this;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var mainWindow = FindParent<MainWindow>(this);
        if (mainWindow != null)
        {
            mainWindow.CurveCollector.OnUpdated += UpdateGraphs;
            mainWindow.CurveCollector.OnDetect += UpdateDetectGraphs;
            mainWindow.CurveCollector.OnStatus += UpdateDeviceStatus;
        }
    }

    private T FindParent<T>(DependencyObject child)
        where T : DependencyObject
    {
        DependencyObject parent = VisualTreeHelper.GetParent(child);

        while (parent != null)
        {
            if (parent is T)
            {
                return (T)parent;
            }
            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
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

                graph = CreateGraph(">Proximity_detected", 25000, 0, Color.FromRgb(100, 100, 0), true, true);
                graph.Series.ElementAt(0).Values.Clear();
                graph.Series.ElementAt(0).Values.AddRange(detectedCurves[">Proximity"].Cast<object>());
                stackPanel.Children.Add(graph);

                DetectPanel.Children.Add(stackPanel);
            });
    }

    private void UpdateDeviceStatus(bool status)
    {
        this.Dispatcher.Invoke(() =>
                               {
                                   if (status)
                                   {
                                       DeviceStatus.Text = "Status: Anomaly";
                                       DeviceStatus.Foreground = new SolidColorBrush(Colors.Red);
                                   }
                                   else
                                   {
                                       DeviceStatus.Text = "Status: Idle";
                                       DeviceStatus.Foreground = new SolidColorBrush(Colors.Green);
                                   }
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
