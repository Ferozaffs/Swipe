using Swipe_Core;
using Swipe_Core.Functions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Swipe_Core.Functions.Function;
using CheckBox = System.Windows.Controls.CheckBox;
using Color = System.Windows.Media.Color;

namespace Swipe_Application
{
public partial class FunctionView : System.Windows.Controls.UserControl
{
    private bool _record = false;

    private CurveCollector? _curveCollector = null;
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
            _curveCollector = mainWindow.GetCurveCollector();
            if (_curveCollector != null)
            {
                _curveCollector.OnDetect += HandleDetectCurves;
            }

            _functionManager = mainWindow.FunctionManager;
            _functionManager.OnFunctionChange += UpdateFunctionList;
            UpdateFunctionList(_functionManager.GetFunctions());

            KeyboardManager.OnAllKeyReleased += KeyboardManager_OnKeyReleased;
        }

        FuncTypeCombo.ItemsSource = Enum.GetValues(typeof(FunctionType));
        FuncInterfaceCombo.ItemsSource = Enum.GetValues(typeof(InterfaceType));
    }

    public void Unload()
    {
        if (_curveCollector != null)
        {
            _curveCollector.OnDetect -= HandleDetectCurves;
        }

        KeyboardManager.OnAllKeyReleased -= KeyboardManager_OnKeyReleased;
    }

    public void UpdateEditingStatus()
    {
        if (_currentFunction == null)
        {
            FuncEnabledCheck.IsChecked = true;
            FuncNameTextBox.Text = "";
            FuncTypeCombo.SelectedItem = FunctionType.Powershell;
            FuncInterfaceCombo.SelectedItem = InterfaceType.Keyboard;
            FuncCommandTextBox.Text = "";

            FuncEnabledCheck.IsEnabled = false;
            FuncNameTextBox.IsEnabled = false;
            FuncTypeCombo.IsEnabled = false;
            FuncInterfaceCombo.IsEnabled = false;
            FuncCommandTextBox.IsEnabled = false;
        }
        else
        {
            FuncEnabledCheck.IsChecked = _currentFunction.IsEnabled;
            FuncNameTextBox.Text = _currentFunction.Name;
            FuncTypeCombo.SelectedItem = _currentFunction.FuncType;
            FuncInterfaceCombo.SelectedItem = _currentFunction.Interface;
            FuncCommandTextBox.Text = _currentFunction.Command;

            FuncEnabledCheck.IsEnabled = true;
            FuncNameTextBox.IsEnabled = true;
            FuncTypeCombo.IsEnabled = true;
            FuncInterfaceCombo.IsEnabled = true;
            FuncCommandTextBox.IsEnabled = true;
        }

        if (_currentFunction is BandFunction)
        {
            SwipeBandPanel.Visibility = Visibility.Visible;
            RecordsPanel.Visibility = Visibility.Visible;
            KeyboardPanel.Visibility = Visibility.Collapsed;
            KeyboardBindsPanel.Visibility = Visibility.Collapsed;
            UpdateRecordedCurves();
        }
        else if (_currentFunction is KeyboardFunction)
        {
            SwipeBandPanel.Visibility = Visibility.Collapsed;
            RecordsPanel.Visibility = Visibility.Collapsed;
            KeyboardPanel.Visibility = Visibility.Visible;
            KeyboardBindsPanel.Visibility = Visibility.Visible;
            UpdateKeySets();
        }
    }

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn)
        {
            _record = !_record;
            btn.Content = _record ? "◼" : "⬤";
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

            var nonConfiguredFunction =
                _functionManager.GetFunctions().Values.FirstOrDefault(f => f.Name.Contains("New Function"));

            if (nonConfiguredFunction != null)
            {
                _currentFunction = nonConfiguredFunction;
                UpdateFunctionList(_functionManager.GetFunctions());
                UpdateEditingStatus();
                return;
            }

            _currentFunction = null;
            _functionManager.CreateFunction();
            UpdateEditingStatus();
        }
    }

    private void UpdateFunctionList(Dictionary<Guid, Function> functions)
    {
        this.Dispatcher.Invoke(
            () =>
            {
                if (_functionManager == null)
                {
                    return;
                }

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
                    _currentFunction = functions.Values.Last();
                    UpdateEditingStatus();
                }

                bool foundMatchingFunction = false;
                foreach (var function in functions)
                {
                    var fontWeight = FontWeights.Regular;
                    var color = Colors.Gray;
                    if (function.Key == _currentFunction.Guid)
                    {
                        foundMatchingFunction = true;
                        fontWeight = FontWeights.Bold;
                        color = Colors.LightGray;
                    }

                    var delBtn = new System.Windows.Controls.Button { Content = "✖", Style = buttonStyle, FontSize = 10,
                                                                      Height = 24, FontWeight = fontWeight };
                    delBtn.Click += (s, e) => _functionManager.RemoveFunction(function.Key);
                    FunctionDeleteColumn.Children.Add(delBtn);

                    var funcName = new TextBlock { Text = function.Value.Name, Foreground = new SolidColorBrush(color),
                                                   FontSize = 14, Height = 24, FontWeight = fontWeight };
                    funcName.MouseLeftButtonDown += (s, e) =>
                    {
                        _currentFunction = _functionManager.GetFunction(function.Key);
                        UpdateFunctionList(_functionManager.GetFunctions());
                        UpdateEditingStatus();
                    };

                    FunctionNameColumn.Children.Add(funcName);

                    var funcType = new TextBlock { Text = function.Value.FuncType.ToString(),
                                                   Foreground = new SolidColorBrush(color), FontSize = 14, Height = 24,
                                                   FontWeight = fontWeight };
                    funcType.MouseLeftButtonDown += (s, e) =>
                    {
                        _currentFunction = _functionManager.GetFunction(function.Key);
                        UpdateFunctionList(_functionManager.GetFunctions());
                        UpdateEditingStatus();
                    };
                    FunctionTypeColumn.Children.Add(funcType);

                    var funcDetails =
                        new TextBlock { Text = function.Value.Command, Foreground = new SolidColorBrush(color),
                                        FontSize = 8, Height = 24, FontWeight = fontWeight };
                    funcDetails.MouseLeftButtonDown += (s, e) =>
                    {
                        _currentFunction = _functionManager.GetFunction(function.Key);
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

    private void FuncInterfaceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_functionManager != null)
        {
            var interfaceType = (InterfaceType)((System.Windows.Controls.ComboBox)sender).SelectedItem;

            if (_currentFunction is BandFunction swipeBandFunction)
            {
                if (interfaceType == InterfaceType.Keyboard)
                {
                    _currentFunction = _functionManager.ConvertBandToKeyboard(swipeBandFunction);
                    UpdateEditingStatus();
                }
            }
            else if (_currentFunction is KeyboardFunction keyboardFunction)
            {
                if (interfaceType == InterfaceType.Band)
                {
                    _currentFunction = _functionManager.ConvertKeyboardToBand(keyboardFunction);
                    UpdateEditingStatus();
                }
            }
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
        Function f = new Function();
        f.SetName(FuncNameTextBox.Text);
        f.SetFunctionType((FunctionType)FuncTypeCombo.SelectedItem);
        switch (f.FuncType)
        {
        case FunctionType.Powershell: {

            f.SetPowershellCommand(FuncCommandTextBox.Text);
            break;
        }
        case FunctionType.PowershellScript: {

            f.SetPowershellScriptPath(FuncCommandTextBox.Text);
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
    private void KeyboardManager_OnKeyReleased(SortedSet<int> keys)
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

                                   if (_currentFunction is KeyboardFunction keyboardFunction && keys.Count > 0)
                                   {
                                       keyboardFunction.AddKeySet(keys);
                                       UpdateEditingStatus();
                                   }
                               });
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

                                   if (_functionManager != null && _currentFunction is BandFunction swipeBandFunction)
                                   {
                                       swipeBandFunction.AddRecording(detectedCurves);
                                       UpdateEditingStatus();
                                   }
                               });
    }

    private void CurveEnabled_Checked(object sender, RoutedEventArgs e)
    {
        if (_currentFunction is BandFunction swipeBandFunction)
        {
            var checkBox = sender as CheckBox;
            switch (checkBox?.Name)
            {
            case "XAxisEnabled": {
                swipeBandFunction.SetAxisEnabled(">LinAccel_x", true);
                break;
            }
            case "YAxisEnabled": {
                swipeBandFunction.SetAxisEnabled(">LinAccel_y", true);
                break;
            }
            case "ZAxisEnabled": {
                swipeBandFunction.SetAxisEnabled(">LinAccel_z", true);
                break;
            }
            case "ProximityEnabled": {
                swipeBandFunction.SetAxisEnabled(">Proximity", true);
                break;
            }
            }

            UpdateEditingStatus();
        }
    }
    private void CurveEnabled_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_currentFunction is BandFunction swipeBandFunction)
        {
            var checkBox = sender as CheckBox;
            switch (checkBox?.Name)
            {
            case "XAxisEnabled": {
                swipeBandFunction.SetAxisEnabled(">LinAccel_x", false);
                break;
            }
            case "YAxisEnabled": {
                swipeBandFunction.SetAxisEnabled(">LinAccel_y", false);
                break;
            }
            case "ZAxisEnabled": {
                swipeBandFunction.SetAxisEnabled(">LinAccel_z", false);
                break;
            }
            case "ProximityEnabled": {
                swipeBandFunction.SetAxisEnabled(">Proximity", false);
                break;
            }
            }

            UpdateEditingStatus();
        }
    }

    public void UpdateKeySets()
    {
        KeyboardBindsPanel.Children.Clear();
        if (_currentFunction is KeyboardFunction keyboardFunction)
        {
            foreach (var keySet in keyboardFunction.KeySets)
            {
                WrapPanel wrapPanel = new WrapPanel();

                var buttonStyle = (Style)System.Windows.Application.Current.Resources["FlatDarkButton"];
                var fontWeight = FontWeights.Regular;
                var delBtn = new System.Windows.Controls.Button { Content = "✖", Style = buttonStyle, FontSize = 14,
                                                                  FontWeight = fontWeight };
                delBtn.Click += (s, e) =>
                {
                    keyboardFunction?.RemoveKeySet(keySet.Key);
                    UpdateEditingStatus();
                };
                wrapPanel.Children.Add(delBtn);

                var keysText = new TextBlock() { Text = App.GetKeysAsText(keySet.Value),
                                                 FontSize = 14,
                                                 Height = 24,
                                                 FontWeight = FontWeights.Bold,
                                                 Foreground = new SolidColorBrush(Colors.Gray),
                                                 Margin = new System.Windows.Thickness(20, 0, 0, 0) };
                wrapPanel.Children.Add(keysText);

                KeyboardBindsPanel.Children.Add(wrapPanel);
            }
        }
    }

    public void UpdateRecordedCurves()
    {
        byte activeStrength = 150;
        byte inactiveStrength = 15;

        RecordsPanel.Children.Clear();
        if (_currentFunction is BandFunction swipeBandFunction)
        {
            foreach (var recording in swipeBandFunction.Recordings)
            {
                WrapPanel wrapPanel = new WrapPanel();

                var buttonStyle = (Style)System.Windows.Application.Current.Resources["FlatDarkButton"];
                var fontWeight = FontWeights.Regular;
                var delBtn = new System.Windows.Controls.Button { Content = "✖", Style = buttonStyle, FontSize = 14,
                                                                  FontWeight = fontWeight };
                delBtn.Click += (s, e) =>
                {
                    swipeBandFunction?.RemoveRecording(recording.Key);
                    UpdateEditingStatus();
                };
                wrapPanel.Children.Add(delBtn);

                byte strength = swipeBandFunction.AxisEnabled[">LinAccel_x"] ? activeStrength : inactiveStrength;
                var (graph, _) = App.CreateGraph(">LinAccel_x", 20, -20, Color.FromRgb(strength, 0, 0), true);
                graph.Series.ElementAt(0).Values.Clear();
                graph.Series.ElementAt(0).Values.AddRange(recording.Value[">LinAccel_x"].Cast<object>());
                wrapPanel.Children.Add(graph);

                strength = swipeBandFunction.AxisEnabled[">LinAccel_y"] ? activeStrength : inactiveStrength;
                (graph, _) = App.CreateGraph(">LinAccel_y", 20, -20, Color.FromRgb(0, strength, 0), true);
                graph.Series.ElementAt(0).Values.Clear();
                graph.Series.ElementAt(0).Values.AddRange(recording.Value[">LinAccel_y"].Cast<object>());
                wrapPanel.Children.Add(graph);

                strength = swipeBandFunction.AxisEnabled[">LinAccel_z"] ? activeStrength : inactiveStrength;
                (graph, _) = App.CreateGraph(">LinAccel_z", 20, -20, Color.FromRgb(0, 0, strength), true);
                graph.Series.ElementAt(0).Values.Clear();
                graph.Series.ElementAt(0).Values.AddRange(recording.Value[">LinAccel_z"].Cast<object>());
                wrapPanel.Children.Add(graph);

                strength = swipeBandFunction.AxisEnabled[">Proximity"] ? activeStrength : inactiveStrength;
                (graph, _) = App.CreateGraph(">Proximity", 50, 0, Color.FromRgb(strength, strength, 0), true);
                graph.Series.ElementAt(0).Values.Clear();
                graph.Series.ElementAt(0).Values.AddRange(recording.Value[">Proximity"].Cast<object>());
                wrapPanel.Children.Add(graph);

                var numActivationsText =
                    new TextBlock() { Text = "Activations:" + swipeBandFunction.RecordingActivations[recording.Key],
                                      FontSize = 14,
                                      Height = 24,
                                      FontWeight = FontWeights.Bold,
                                      Foreground = new SolidColorBrush(Colors.Gray),
                                      Margin = new System.Windows.Thickness(20, 0, 0, 0) };
                wrapPanel.Children.Add(numActivationsText);

                RecordsPanel.Children.Add(wrapPanel);
            }
        }
    }
}
}
