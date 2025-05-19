using Swipe_Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Windows.Networking.Proximity;
using Windows.UI.Text;
using static Swipe_Core.Function;

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
            UpdateFunctionList(_functionManager.GetFunctions());
        }

        FuncTypeCombo.ItemsSource = Enum.GetValues(typeof(FunctionType));
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
        if (_currentFunction == null)
        {
            FuncEnabledCheck.IsChecked = true;
            FuncNameTextBox.Text = "";
            FuncTypeCombo.SelectedItem = FunctionType.Powershell;
            FuncCommandTextBox.Text = "";

            FuncEnabledCheck.IsEnabled = false;
            FuncNameTextBox.IsEnabled = false;
            FuncTypeCombo.IsEnabled = false;
            FuncCommandTextBox.IsEnabled = false;
        }
        else
        {
            FuncEnabledCheck.IsChecked = _currentFunction.IsEnabled;
            FuncNameTextBox.Text = _currentFunction.Name;
            FuncTypeCombo.SelectedItem = _currentFunction.FuncType;
            FuncCommandTextBox.Text = _currentFunction.Command;

            FuncEnabledCheck.IsEnabled = true;
            FuncNameTextBox.IsEnabled = true;
            FuncTypeCombo.IsEnabled = true;
            FuncCommandTextBox.IsEnabled = true;
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

    private void CreateFunctBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_functionManager != null)
        {
            if (_currentFunction != null && _currentFunction.Name == "New Function")
            {
                return;
            }

            if (_functionManager.GetFunctions().Last().Name == "New Function")
            {
                _currentFunction = _functionManager.GetFunctions().Last();
                UpdateFunctionList(_functionManager.GetFunctions());
                UpdateEditingStatus();
                return;
            }

            _currentFunction = null;
            _functionManager.CreateFunction();
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

                                       _currentFunction.AddRecording(detectedCurves);

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

                bool foundMatchingFunction = false;
                for (int i = 0; i < functions.Count; i++)
                {
                    var fontWeight = FontWeights.Regular;
                    var color = Colors.Gray;
                    if (functions[i] == _currentFunction)
                    {
                        foundMatchingFunction = true;
                        fontWeight = FontWeights.Bold;
                        color = Colors.LightGray;
                    }
                    int index = i;

                    var delBtn = new Button { Content = "✖", Style = buttonStyle, FontSize = 10, Height = 24,
                                              FontWeight = fontWeight };
                    delBtn.Click += (s, e) => _functionManager?.RemoveFunction(index);
                    FunctionDeleteColumn.Children.Add(delBtn);

                    var funcName = new TextBlock { Text = functions[i].Name, Foreground = new SolidColorBrush(color),
                                                   FontSize = 14, Height = 24, FontWeight = fontWeight };
                    funcName.MouseLeftButtonDown += (s, e) =>
                    {
                        _currentFunction = _functionManager.GetFunction(index);
                        UpdateFunctionList(_functionManager.GetFunctions());
                        UpdateEditingStatus();
                    };

                    FunctionNameColumn.Children.Add(funcName);

                    var funcType = new TextBlock { Text = functions[i].FuncType.ToString(),
                                                   Foreground = new SolidColorBrush(color), FontSize = 14, Height = 24,
                                                   FontWeight = fontWeight };
                    funcType.MouseLeftButtonDown += (s, e) =>
                    {
                        _currentFunction = _functionManager.GetFunction(index);
                        UpdateFunctionList(_functionManager.GetFunctions());
                        UpdateEditingStatus();
                    };
                    FunctionTypeColumn.Children.Add(funcType);

                    var funcDetails =
                        new TextBlock { Text = functions[i].Command, Foreground = new SolidColorBrush(color),
                                        FontSize = 8, Height = 24, FontWeight = fontWeight };
                    funcDetails.MouseLeftButtonDown += (s, e) =>
                    {
                        _currentFunction = _functionManager.GetFunction(index);
                        UpdateFunctionList(_functionManager.GetFunctions());
                        UpdateEditingStatus();
                    };
                    FunctionDetailsColumn.Children.Add(funcDetails);
                }

                if (foundMatchingFunction == false)
                {
                    _currentFunction = null;
                    UpdateEditingStatus();
                }
            });
    }

    private void FuncEnabledCheck_Checked(object sender, RoutedEventArgs e)
    {
        if (_currentFunction != null)
        {
            _currentFunction.Enable();
        }
    }

    private void FuncEnabledCheck_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_currentFunction != null)
        {
            _currentFunction.Disable();
        }
    }

    private void FuncNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_currentFunction != null)
        {
            _currentFunction.SetName(((TextBox)sender).Text);
        }
    }

    private void FuncTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_currentFunction != null)
        {
            _currentFunction.SetFunctionType((FunctionType)((ComboBox)sender).SelectedItem);
        }
    }

    private void FuncCommandTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_currentFunction != null)
        {
            _currentFunction.SetPowershellCommand(((TextBox)sender).Text);
        }
    }
}
}
