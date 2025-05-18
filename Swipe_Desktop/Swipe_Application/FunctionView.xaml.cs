using Swipe_Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Windows.UI.Text;

namespace Swipe_Application
{
public partial class FunctionView : UserControl
{
    private bool _record = false;

    private CurveCollector? _collector = null;
    private FunctionManager? _functionManager = null;
    private Function? _currentFunction = null;

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

            _functionManager = mainWindow.FunctionManager;
            _functionManager.OnFunctionChange += UpdateFunctionList;
        }
    }

    public void Unload()
    {
        if (_collector != null)
        {
            _collector.OnDetect -= HandleDetectCurves;
        }
    }

    public void UpdateEditingStatus()
    {
        FuncEnabledCheck.IsEnabled = _currentFunction != null ? true : false;
        FuncNameTextBox.IsEnabled = _currentFunction != null ? true : false;
        FuncTypeCombo.IsEnabled = _currentFunction != null ? true : false;
        FuncCommandTextBox.IsEnabled = _currentFunction != null ? true : false;
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

    private void CreateFunctBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_functionManager != null)
        {
            _currentFunction = _functionManager.CreateFunction();
            UpdateEditingStatus();
        }
    }

    private void HandleDetectCurves(Dictionary<string, List<float>> detectedCurves)
    {
        this.Dispatcher.Invoke(() =>
                               {
                                   if (_functionManager != null && _currentFunction != null)
                                   {
                                       var prevState = _functionManager.IsExecutionEnabled;
                                       _functionManager.IsExecutionEnabled = false;

                                       _currentFunction.Recordings.Add(detectedCurves);

                                       _functionManager.IsExecutionEnabled = prevState;
                                   }
                               });
    }

    private void UpdateFunctionList(List<Function> functions)
    {
        this.Dispatcher.Invoke(
            () =>
            {
                FunctionDeleteColumn.Children.Clear();
                FunctionNameColumn.Children.Clear();
                FunctionTypeColumn.Children.Clear();
                FunctionDetailsColumn.Children.Clear();

                if (functions.Count == 0)
                {
                    _currentFunction = null;
                    UpdateEditingStatus();
                    return;
                }

                var buttonStyle = (Style)Application.Current.Resources["FlatDarkButton"];

                if (_currentFunction == null)
                {
                    _currentFunction = functions.Last();
                    UpdateEditingStatus();
                }

                for (int i = 0; i < functions.Count; i++)
                {
                    var fontWeight = FontWeights.Regular;
                    var color = Colors.Gray;
                    if (functions[i] == _currentFunction)
                    {
                        fontWeight = FontWeights.Bold;
                        color = Colors.LightGray;
                    }
                    int index = i;

                    var delBtn = new Button { Content = "✖", Style = buttonStyle, FontSize = 10, Height = 24,
                                              FontWeight = fontWeight };
                    delBtn.Click += (s, e) => _functionManager.RemoveFunction(index);
                    FunctionDeleteColumn.Children.Add(delBtn);

                    var funcName = new TextBlock { Text = functions[i].Name, Foreground = new SolidColorBrush(color),
                                                   FontSize = 14, Height = 24, FontWeight = fontWeight };
                    FunctionNameColumn.Children.Add(funcName);

                    var funcType = new TextBlock { Text = functions[i].FuncType.ToString(),
                                                   Foreground = new SolidColorBrush(color), FontSize = 14, Height = 24,
                                                   FontWeight = fontWeight };
                    FunctionTypeColumn.Children.Add(funcType);

                    var funcDetails =
                        new TextBlock { Text = functions[i].GetDetails(), Foreground = new SolidColorBrush(color),
                                        FontSize = 12, Height = 24, FontWeight = fontWeight };
                    FunctionDetailsColumn.Children.Add(funcDetails);
                }
            });
    }
}
}
