using LiveCharts.Wpf;
using LiveCharts;
using Swipe_Core;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using UserControl = System.Windows.Controls.UserControl;
using Color = System.Windows.Media.Color;

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

        var (panel, series) = App.CreateGraph(">LinAccel_x", 20, -20, Color.FromRgb(100, 0, 0), true);
        InputPanel.Children.Add(panel);
        _seriesList.Add(series);

        (panel, series) = App.CreateGraph(">LinAccel_y", 20, -20, Color.FromRgb(0, 100, 0), true);
        InputPanel.Children.Add(panel);
        _seriesList.Add(series);

        (panel, series) = App.CreateGraph(">LinAccel_z", 20, -20, Color.FromRgb(0, 0, 100), true);
        InputPanel.Children.Add(panel);
        _seriesList.Add(series);

        (panel, series) = App.CreateGraph(">Proximity", 50, 0, Color.FromRgb(100, 100, 0), true);
        InputPanel.Children.Add(panel);
        _seriesList.Add(series);

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

                StackPanel stackPanel =
                    new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal,
                                     HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                                     VerticalAlignment = System.Windows.VerticalAlignment.Stretch };

                var (graph, _) = App.CreateGraph(">LinAccel_x_detected", 20, -20, Color.FromRgb(100, 0, 0), true);
                graph.Series.ElementAt(0).Values.Clear();
                graph.Series.ElementAt(0).Values.AddRange(detectedCurves[">LinAccel_x"].Cast<object>());
                stackPanel.Children.Add(graph);

                (graph, _) = App.CreateGraph(">LinAccel_y_detected", 20, -20, Color.FromRgb(0, 100, 0), true);
                graph.Series.ElementAt(0).Values.Clear();
                graph.Series.ElementAt(0).Values.AddRange(detectedCurves[">LinAccel_y"].Cast<object>());
                stackPanel.Children.Add(graph);

                (graph, _) = App.CreateGraph(">LinAccel_z_detected", 20, -20, Color.FromRgb(0, 0, 100), true);
                graph.Series.ElementAt(0).Values.Clear();
                graph.Series.ElementAt(0).Values.AddRange(detectedCurves[">LinAccel_z"].Cast<object>());
                stackPanel.Children.Add(graph);

                (graph, _) = App.CreateGraph(">Proximity_detected", 50, 0, Color.FromRgb(100, 100, 0), true);
                graph.Series.ElementAt(0).Values.Clear();
                graph.Series.ElementAt(0).Values.AddRange(detectedCurves[">Proximity"].Cast<object>());
                stackPanel.Children.Add(graph);

                DetectPanel.Children.Add(stackPanel);
            });
    }
}
}
