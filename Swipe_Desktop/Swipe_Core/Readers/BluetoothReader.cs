using System.Diagnostics;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace Swipe_Core.Readers
{
public class BluetoothReader : IDataReader
{
    public event Action<string>? OnUpdated;

    private bool _isMonitoring;
    private bool _stopped = false;

    public async Task<bool> Initialize()
    {
        var device = await BluetoothHelper.FindDevice();
        if (device != null)
        {
            _isMonitoring = true;
            MonitorDevice(device);
            return true;
        }

        return false;
    }

    public void Stop()
    {
        _stopped = true;
    }

    private async Task MonitorDevice(BluetoothLEDevice device)
    {
        var connectionTries = 0;
        while (_isMonitoring)
        {
            if (device.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                await device.GetGattServicesAsync();
                Debug.WriteLine("Reconnecting...");
                connectionTries++;
            }

            while (device.ConnectionStatus == BluetoothConnectionStatus.Connected && _stopped == false)
            {
                connectionTries = 0;
                await ReadDeviceData(device);
            }

            Thread.Sleep(5000);
            if (connectionTries >= 5)
            {
                Debug.WriteLine("Exceeded connection attempts...");
                _isMonitoring = false;
            }
        }
    }

    async Task ReadDeviceData(BluetoothLEDevice device)
    {
        foreach (var service in device.GattServices)
        {
            var result = await service.GetCharacteristicsAsync();
            if (result.Status == GattCommunicationStatus.Success)
            {
                foreach (var characteristic in result.Characteristics)
                {
                    if (characteristic.Uuid.ToString().Contains("2c19"))
                    {
                        if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                        {
                            DateTime lastUpdateTime = DateTime.Now;

                            characteristic.ValueChanged += (sender, args) =>
                            {
                                var data = ReadBuffer(args.CharacteristicValue);
                                OnUpdated?.Invoke(System.Text.Encoding.UTF8.GetString(data));

                                lastUpdateTime = DateTime.Now;
                            };

                            await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                                GattClientCharacteristicConfigurationDescriptorValue.Notify);

                            while ((DateTime.Now - lastUpdateTime).TotalSeconds < 30)
                            {
                                await Task.Delay(1000);
                            }

                            Debug.WriteLine("No data received for 30 seconds. Exiting...");
                            return;
                        }
                    }
                }
            }
        }
    }

    static byte[] ReadBuffer(IBuffer buffer)
    {
        var data = new byte[buffer.Length];
        DataReader.FromBuffer(buffer).ReadBytes(data);
        return data;
    }
}
}
