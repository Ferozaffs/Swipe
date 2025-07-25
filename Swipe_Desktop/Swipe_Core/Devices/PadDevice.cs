using Swipe_Core.Readers;

namespace Swipe_Core.Devices
{
public class PadDevice : BaseDevice
{
    public enum PadKey
    {
        None,
        Key1,
        Key2,
        Key3,
        Key4,
        Key5,
        Key6,
        Key7,
        Key8,
        Key9,
        Key10,
        Key11,
        Key12,
        Key13,
        Key14,
        Key15,
        Key16
    }

    public event Action<PadKey>? OnKeyPressed;

    public PadDevice(IDataReader.ReaderType type)
    {
        _name = "Swipe Pad v.1.0";
        _uuid = "67391cf3-acc4-4a8c-af95-0131221895f2";
        _readerType = type;
        SetupReader();
    }

    override protected void OnUpdated_Internal(string obj)
    {
        OnKeyPressed?.Invoke(PadKey.Key1);
    }
}
}
