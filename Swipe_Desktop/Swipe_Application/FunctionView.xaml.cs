using Swipe_Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using Windows.UI.Text;
using static Swipe_Core.Function;
using CheckBox = System.Windows.Controls.CheckBox;
using Color = System.Windows.Media.Color;

namespace Swipe_Application
{
public partial class FunctionView : System.Windows.Controls.UserControl
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

        UpdateRecordedCurves();
    }

    public void UpdateRecordedCurves()
    {
        byte activeStrength = 150;
        byte inactiveStrength = 15;

        RecordsPanel.Children.Clear();
        if (_currentFunction != null)
        {
            var count = 0;
            foreach (var recording in _currentFunction.Recordings)
            {
                StackPanel stackPanel =
                    new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal,
                                     HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                                     VerticalAlignment = System.Windows.VerticalAlignment.Stretch };

                var buttonStyle = (Style)System.Windows.Application.Current.Resources["FlatDarkButton"];
                var fontWeight = FontWeights.Regular;
                var delBtn = new System.Windows.Controls.Button { Content = "✖", Style = buttonStyle, FontSize = 14,
                                                                  FontWeight = fontWeight };
                delBtn.Click += (s, e) =>
                {
                    _currentFunction?.RemoveRecording(recording.Key);
                    UpdateEditingStatus();
                };
                stackPanel.Children.Add(delBtn);

                byte strength = _currentFunction.AxisEnabled[">LinAccel_x"] ? activeStrength : inactiveStrength;
                var (graph, _) = App.CreateGraph(">LinAccel_x", 20, -20, Color.FromRgb(strength, 0, 0), true);
                graph.Series.ElementAt(0).Values.Clear();
                graph.Series.ElementAt(0).Values.AddRange(recording.Value[">LinAccel_x"].Cast<object>());
                stackPanel.Children.Add(graph);

                strength = _currentFunction.AxisEnabled[">LinAccel_y"] ? activeStrength : inactiveStrength;
                (graph, _) = App.CreateGraph(">LinAccel_y", 20, -20, Color.FromRgb(0, strength, 0), true);
                graph.Series.ElementAt(0).Values.Clear();
                graph.Series.ElementAt(0).Values.AddRange(recording.Value[">LinAccel_y"].Cast<object>());
                stackPanel.Children.Add(graph);

                strength = _currentFunction.AxisEnabled[">LinAccel_z"] ? activeStrength : inactiveStrength;
                (graph, _) = App.CreateGraph(">LinAccel_z", 20, -20, Color.FromRgb(0, 0, strength), true);
                graph.Series.ElementAt(0).Values.Clear();
                graph.Series.ElementAt(0).Values.AddRange(recording.Value[">LinAccel_z"].Cast<object>());
                stackPanel.Children.Add(graph);

                strength = _currentFunction.AxisEnabled[">Proximity"] ? activeStrength : inactiveStrength;
                (graph, _) = App.CreateGraph(">Proximity", 50, 0, Color.FromRgb(strength, strength, 0), true);
                graph.Series.ElementAt(0).Values.Clear();
                graph.Series.ElementAt(0).Values.AddRange(recording.Value[">Proximity"].Cast<object>());
                stackPanel.Children.Add(graph);

                var numActivationsText =
                    new TextBlock() { Text = "Activations:" + _currentFunction.RecordingActivations[recording.Key],
                                      FontSize = 14,
                                      Height = 24,
                                      FontWeight = FontWeights.Bold,
                                      Foreground = new SolidColorBrush(Colors.Gray),
                                      Margin = new System.Windows.Thickness(20, 0, 0, 0) };
                stackPanel.Children.Add(numActivationsText);

                RecordsPanel.Children.Add(stackPanel);

                count++;
            }
        }
    }

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (_collector != null)
        {
            if (sender is System.Windows.Controls.Button btn)
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

            if (_functionManager.GetFunctions().Count > 0 &&
                _functionManager.GetFunctions().Last().Name == "New Function")
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
                                   if (_record == false)
                                   {
                                       return;
                                   }

                                   if (Visibility != Visibility.Visible)
                                   {
                                       _record = false;
                                       return;
                                   }

                                   if (_functionManager != null && _currentFunction != null)
                                   {
                                       _currentFunction.AddRecording(detectedCurves);
                                       UpdateEditingStatus();
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

                var buttonStyle = (Style)System.Windows.Application.Current.Resources["FlatDarkButton"];

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

                    var delBtn = new System.Windows.Controls.Button { Content = "✖", Style = buttonStyle, FontSize = 10,
                                                                      Height = 24, FontWeight = fontWeight };
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
            _currentFunction.SetName(((System.Windows.Controls.TextBox)sender).Text);
        }
    }

    private void FuncTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_currentFunction != null)
        {
            _currentFunction.SetFunctionType((FunctionType)((System.Windows.Controls.ComboBox)sender).SelectedItem);
        }
    }

    private void FuncCommandTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_currentFunction != null)
        {
            _currentFunction.SetPowershellCommand(((System.Windows.Controls.TextBox)sender).Text);
        }
    }

    private void ScriptRun_Click(object sender, RoutedEventArgs e)
    {
        Swipe_Core.Function f = new Function();
        f.SetName(FuncNameTextBox.Text);
        f.SetFunctionType((FunctionType)FuncTypeCombo.SelectedItem);
        switch (f.FuncType)
        {
        case FunctionType.Powershell: {

            f.SetPowershellCommand(FuncCommandTextBox.Text);
            break;
        }
        case FunctionType.Launch: {

            f.SetExe(FuncCommandTextBox.Text);
            break;
        }
        }

        var result = f.RunFunction();
        FuncScriptStatus.Text = result;
        if (result == "Success")
        {
            FuncScriptStatus.Foreground = new SolidColorBrush(Colors.LightGreen);
        }
        else
        {
            FuncScriptStatus.Foreground = new SolidColorBrush(Colors.IndianRed);
        }
    }

    private void CurveEnabled_Checked(object sender, RoutedEventArgs e)
    {
        if (_currentFunction != null)
        {
            var checkBox = sender as CheckBox;
            switch (checkBox?.Name)
            {
            case "XAxisEnabled": {
                _currentFunction.SetAxisEnabled(">LinAccel_x", true);
                break;
            }
            case "YAxisEnabled": {
                _currentFunction.SetAxisEnabled(">LinAccel_y", true);
                break;
            }
            case "ZAxisEnabled": {
                _currentFunction.SetAxisEnabled(">LinAccel_z", true);
                break;
            }
            case "ProximityEnabled": {
                _currentFunction.SetAxisEnabled(">Proximity", true);
                break;
            }
            }

            UpdateEditingStatus();
        }
    }
    private void CurveEnabled_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_currentFunction != null)
        {
            var checkBox = sender as CheckBox;
            switch (checkBox?.Name)
            {
            case "XAxisEnabled": {
                _currentFunction.SetAxisEnabled(">LinAccel_x", false);
                break;
            }
            case "YAxisEnabled": {
                _currentFunction.SetAxisEnabled(">LinAccel_y", false);
                break;
            }
            case "ZAxisEnabled": {
                _currentFunction.SetAxisEnabled(">LinAccel_z", false);
                break;
            }
            case "ProximityEnabled": {
                _currentFunction.SetAxisEnabled(">Proximity", false);
                break;
            }
            }

            UpdateEditingStatus();
        }
    }
}
}
