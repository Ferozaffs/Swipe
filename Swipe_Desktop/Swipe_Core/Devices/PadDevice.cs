using Swipe_Core.Readers;
using System.Diagnostics;

namespace Swipe_Core.Devices
{
public class PadDevice : BaseDevice
{
    public enum PadKey
    {
        None = 0,
        Key1 = 1,
        Key2 = 2,
        Key3 = 3,
        Key4 = 4,
        Key5 = 5,
        Key6 = 6,
        Key7 = 7,
        Key8 = 8,
        Key9 = 9,
        Key10 = 10,
        Key11 = 11,
        Key12 = 12,
        Key13 = 13,
        Key14 = 14,
        Key15 = 15,
        Key16 = 16
    }

    public event Action<PadKey>? OnKeyPressed;
    public event Action<bool>? OnStatus;

    private bool[] previousStates = new bool[17];

    public PadDevice(IDataReader.ReaderType type)
    {
        Name = "Swipe Pad v.1.0";
        _uuid = "67391cf3-acc4-4a8c-af95-0131221895f2";
        _readerType = type;
        SetupReader();
    }

    override protected void OnUpdated_Internal(string obj)
    {
        bool hasInput = false;

        string dataPart = obj.Split(':') [1].Trim();
        string[] numbers = dataPart.Split(',');
        for (int i = 0; i < numbers.Length; i++)
        {
            bool pressed = numbers[i].Trim() == "1";
            if (pressed)
            {
                hasInput = true;
            }

            if (pressed && previousStates[i + 1] == false)
            {
                OnKeyPressed?.Invoke((PadKey)(i + 1));
            }

            previousStates[i + 1] = pressed;
        }

        OnStatus?.Invoke(hasInput);
    }

    public void Connect()
    {
        var btReader = _reader as BluetoothReader;
        if (btReader != null)
        {
            _ = btReader.Connect();
        }
    }
}
}
