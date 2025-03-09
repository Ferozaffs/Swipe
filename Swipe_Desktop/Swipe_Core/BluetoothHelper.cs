using System.Diagnostics;
using System.Threading;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace Swipe_Core
{
public static class BluetoothHelper
{
    private static BluetoothLEAdvertisementWatcher _watcher;
    private static TaskCompletionSource<BluetoothLEDevice>? _taskCompletionSource;

    static BluetoothHelper()
    {
        _watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
        _watcher.Received += OnAdvertisementReceived;
    }

    public static async Task<BluetoothLEDevice> FindDevice()
    {
        _taskCompletionSource = new TaskCompletionSource<BluetoothLEDevice>();

        _watcher.Start();

        var completedTask = await Task.WhenAny(_taskCompletionSource.Task, Task.Delay(1000));

        _watcher.Stop();

        return completedTask == _taskCompletionSource.Task ? _taskCompletionSource.Task.Result : null;
    }

    private static async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender,
                                                      BluetoothLEAdvertisementReceivedEventArgs args)
    {
        var device = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);

        if (device != null && device.Name == "Swipe v.1.0")
        {
            _taskCompletionSource.TrySetResult(device);
            return;
        }
    }
}
}
