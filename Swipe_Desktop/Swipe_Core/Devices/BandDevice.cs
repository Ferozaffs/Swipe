using Swipe_Core.Readers;

namespace Swipe_Core.Devices
{
public class BandDevice : BaseDevice
{
    public CurveCollector? CurveCollector { get; }
    public event Action<CurveCollector.Status>? OnStatus;

    public BandDevice(IDataReader.ReaderType type)
    {
        _name = "Swipe v.1.0";
        _uuid = "b6fe5a0e-ff8d-494a-8885-143cda0100ea";
        _readerType = type;
        SetupReader();

        if (_reader != null)
        {
            CurveCollector = new CurveCollector(_reader);
            CurveCollector.AddKeyValueTracker(">LinAccel_x", 4.0f);
            CurveCollector.AddKeyValueTracker(">LinAccel_y", 4.0f);
            CurveCollector.AddKeyValueTracker(">LinAccel_z", 4.0f);
            CurveCollector.AddKeyValueTracker(">Proximity", 5000.0f);

            CurveCollector.OnStatus += UpdateDeviceStatus_Internal;
        }
    }

    public void Connect()
    {
        var btReader = _reader as BluetoothReader;
        if (btReader != null)
        {
            _ = btReader.Connect();
        }
    }

    override protected void OnUpdated_Internal(string obj)
    {
    }

    private void UpdateDeviceStatus_Internal(CurveCollector.Status status)
    {
        OnStatus?.Invoke(status);
    }
}
}
