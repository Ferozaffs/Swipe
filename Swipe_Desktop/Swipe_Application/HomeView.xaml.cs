using Swipe_Core.Devices;
using Swipe_Core.Functions;
using Swipe_Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
namespace Swipe_Application
{
/// <summary>
/// Interaction logic for HomeView.xaml
/// </summary>
public partial class HomeView : System.Windows.Controls.UserControl
{
    private Swipe_Core.Devices.KeyboardDevice? _keyboardDevice;
    private BandDevice? _bandDevice;
    private PadDevice? _padDevice;
    private FunctionManager? _functionManager;

    public HomeView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var mainWindow = Utils.FindParent<MainWindow>(this);
        if (mainWindow != null)
        {
            if (mainWindow.FunctionManager != null)
            {
                _functionManager = mainWindow.FunctionManager;
                _functionManager.OnActivation += FunctionActivated;
            }

            if (mainWindow.KeyboardDevice != null)
            {
                _keyboardDevice = mainWindow.KeyboardDevice;
                _keyboardDevice.OnConnection += UpdateKeyboardConnection;
            }
            if (mainWindow.BandDevice != null)
            {
                _bandDevice = mainWindow.BandDevice;
                _bandDevice.OnConnection += UpdateBandConnection;
            }
            if (mainWindow.PadDevice != null)
            {
                _padDevice = mainWindow.PadDevice;
                _padDevice.OnConnection += UpdatePadConnection;
            }
        }

        UpdateDeviceGUI(KeyboardPanel, _keyboardDevice);
        UpdateDeviceGUI(BandPanel, _bandDevice);
        UpdateDeviceGUI(PadPanel, _padDevice);
        FunctionActivated(true);
    }

    public void Unload()
    {
        if (_functionManager != null)
        {
            _functionManager.OnActivation -= FunctionActivated;
        }
        if (_keyboardDevice != null)
        {
            _keyboardDevice.OnConnection -= UpdateKeyboardConnection;
        }
        if (_bandDevice != null)
        {
            _bandDevice.OnConnection -= UpdateBandConnection;
        }
        if (_padDevice != null)
        {
            _padDevice.OnConnection -= UpdatePadConnection;
        }
    }

    private void UpdateDeviceGUI(StackPanel panel, BaseDevice? device)
    {
        panel.Children.Clear();

        if (device == null)
        {
            return;
        }

        TextBlock name = new TextBlock { FontSize = 14, Foreground = new SolidColorBrush(Colors.Gray),
                                         Text = "Name: " + device.Name };
        TextBlock status =
            new TextBlock { Margin = new Thickness(0, 5, 0, 0), FontSize = 14,
                            Foreground = new SolidColorBrush(device.Connected ? Colors.LightGreen : Colors.IndianRed),
                            Text = "Connected: " + (device.Connected ? "✓" : "✖") };

        panel.Children.Add(name);
        panel.Children.Add(status);
    }

    private void UpdateKeyboardConnection(bool obj)
    {
        UpdateDeviceGUI(KeyboardPanel, _keyboardDevice);
    }
    private void UpdateBandConnection(bool obj)
    {
        UpdateDeviceGUI(BandPanel, _bandDevice);
    }
    private void UpdatePadConnection(bool obj)
    {
        UpdateDeviceGUI(PadPanel, _padDevice);
    }

    private void FunctionActivated(bool obj)
    {
        this.Dispatcher.Invoke(
            () =>
            {
                TopActivations.Children.Clear();
                LatestActivations.Children.Clear();
                if (_functionManager == null)
                {
                    return;
                }

                foreach (var func in _functionManager.Functions)
                {
                    TextBlock funcElement =
                        new TextBlock { FontSize = 12, Foreground = new SolidColorBrush(Colors.LightGray),
                                        Text = func.Value.Name + ": " + func.Value.NumActivations.ToString() };

                    TopActivations.Children.Add(funcElement);
                };

                foreach (var func in _functionManager.ActivatedFunctions)
                {
                    TextBlock funcElement =
                        new TextBlock { FontSize = 12, Foreground = new SolidColorBrush(Colors.LightGray),
                                        Text = func.Name };

                    LatestActivations.Children.Add(funcElement);
                };
            });
    }
}
}
