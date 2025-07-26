using System.Diagnostics;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Foundation;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace Swipe_Core
{
public static class BluetoothHelper
{
    private static readonly BluetoothLEAdvertisementWatcher _watcher;
    private static TaskCompletionSource<BluetoothLEDevice?>? _taskCompletionSource;
    private static readonly object _lock = new();
    private static string? _name;

    static BluetoothHelper()
    {
        _watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
        _watcher.Received += OnAdvertisementReceived;
    }

    public static async Task<BluetoothLEDevice?> FindDevice(string name, int timeoutMs = 1000)
    {
        while (true)
        {
            lock (_lock)
            {
                if (_taskCompletionSource == null || _taskCompletionSource.Task.IsCompleted)
                {
                    _name = name;
                    _taskCompletionSource = new TaskCompletionSource < BluetoothLEDevice ? > ();
                    break;
                }
            }

            await Task.Delay(timeoutMs);
        }

        _watcher.Start();

        var completedTask = await Task.WhenAny(_taskCompletionSource.Task, Task.Delay(timeoutMs));

        _watcher.Stop();

        var result = completedTask == _taskCompletionSource.Task ? _taskCompletionSource.Task.Result : null;

        _taskCompletionSource.TrySetResult(null);

        return result;
    }

    private static async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender,
                                                      BluetoothLEAdvertisementReceivedEventArgs args)
    {
        var device = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);

        if (device != null && device.Name == _name)
        {
            _taskCompletionSource?.TrySetResult(device);
            return;
        }
    }
}
}
