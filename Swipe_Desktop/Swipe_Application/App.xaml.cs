using LiveCharts.Wpf;
using LiveCharts;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace Swipe_Application
{
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{

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
}

}
