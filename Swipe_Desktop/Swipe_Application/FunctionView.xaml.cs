using LiveCharts.Wpf;
using LiveCharts;
using Swipe_Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Swipe_Application
{
public partial class FunctionView : UserControl
{
    private bool _record = false;

    private CurveCollector? _collector = null;

    public FunctionView()
    {
        InitializeComponent();
        Loaded += OnLoaded;

        DataContext = this;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var mainWindow = Utils.FindParent<MainWindow>(this);
        if (mainWindow != null)
        {
            _collector = mainWindow.CurveCollector;
            _collector.OnDetect += HandleDetectCurves;
        }
    }

    public void Unload()
    {
        if (_collector != null)
        {
            _collector.OnDetect -= HandleDetectCurves;
        }
    }

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (_collector != null)
        {
            if (sender is Button btn)
            {
                _record = !_record;
                btn.Content = _record ? "◼" : "⬤";
            }
        }
    }

    private void HandleDetectCurves(Dictionary<string, List<float>> detectedCurves)
    {
        this.Dispatcher.Invoke(() =>
                               {

                               });
    }

    private void CreateFunctBtn_Click(object sender, RoutedEventArgs e)
    {
    }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
