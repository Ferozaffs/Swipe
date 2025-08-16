using System.Diagnostics;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Swipe_Core.Readers
{
public class BluetoothReader : IDataReader
{
    public event Action<string>? OnUpdated;
    public event Action<bool>? OnConnection;

    public bool IsMonitoring;
    private bool _stopped = false;
    private string _name;
    private string _uuid;

    public BluetoothReader(string name, string uuid)
    {
        _name = name;
        _uuid = uuid;
    }

    public async Task<bool> Start()
    {
        await Task.CompletedTask;

        _ = Connect();

        return true;
    }

    public async Task<bool> Connect()
    {
        if (IsMonitoring == false)
        {
            var device = await BluetoothHelper.FindDevice(_name);
            if (device != null)
            {
                IsMonitoring = true;
                _ = MonitorDevice(device);
                return true;
            }
        }

        return false;
    }

    public bool Stop()
    {
        _stopped = true;
        return true;
    }

    private async Task MonitorDevice(BluetoothLEDevice device)
    {
        var connectionInterval = 1000;
        var connectionTries = 0;
        while (IsMonitoring)
        {
            if (device.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                try
                {
                    await device.GetGattServicesAsync();
                    Debug.WriteLine("Reconnecting...");
                    connectionTries++;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Reconnection failed: {ex.Message}");
                }
            }

            while (device.ConnectionStatus == BluetoothConnectionStatus.Connected && _stopped == false)
            {
                connectionInterval = 1000;
                connectionTries = 0;
                OnConnection?.Invoke(true);
                await ReadDeviceData(device);
                await Task.Delay(100);
            }

            if (connectionTries >= 3)
            {
                Debug.WriteLine("Exceeded connection attempts...reducing reconnection interval");
                connectionInterval = 10000;
                OnConnection?.Invoke(false);
            }
            await Task.Delay(connectionInterval);
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
                    if (characteristic.Uuid.ToString().Contains(_uuid))
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

                            try
                            {
                                await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                                    GattClientCharacteristicConfigurationDescriptorValue.Notify);
                            }
                            catch
                            {
                                return;
                            }

                            while ((DateTime.Now - lastUpdateTime).TotalSeconds < 5)
                            {
                                await Task.Delay(1000);
                            }

                            Debug.WriteLine("No data received for 5 seconds. Exiting...");
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
