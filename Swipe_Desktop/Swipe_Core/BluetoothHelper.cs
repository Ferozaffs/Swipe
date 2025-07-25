using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Foundation;

namespace Swipe_Core
{
public static class BluetoothHelper
{
    public static async Task<BluetoothLEDevice?> FindDevice(string name)
    {
        var taskCompletionSource = new TaskCompletionSource<BluetoothLEDevice>();

        var watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };

        TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs>? handler = null;

        handler = async (sender, args) =>
        {
            var device = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);

            if (device != null && device.Name == name)
            {
                sender.Received -= handler;
                sender.Stop();
                taskCompletionSource.TrySetResult(device);
            }
        };

        watcher.Received += handler;
        watcher.Start();

        var completedTask = await Task.WhenAny(taskCompletionSource.Task, Task.Delay(1000));

        if (taskCompletionSource.Task.IsCompleted == false)
        {
            watcher.Received -= handler;
            watcher.Stop();
        }

        return completedTask == taskCompletionSource.Task ? taskCompletionSource.Task.Result : null;
    }
}
}
