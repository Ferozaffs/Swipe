using Newtonsoft.Json;
using Swipe_Core.Devices;

namespace Swipe_Core.Functions
{
public class PadFunction : Function
{
    public enum KeyModifier
    {
        None,
        Ctrl,
        Shift,
        Alt,
        CtrlShift,
        CtrlAlt,
        AltShift,
        CtrlAltShift,
    }

    [JsonProperty]
    public PadDevice.PadKey Key { get; private set; } = PadDevice.PadKey.None;
    [JsonProperty]
    public KeyModifier Modifier {
        get; private set;
    } = KeyModifier.None;

    public bool EvaluateAndRun(PadDevice.PadKey key, KeyModifier modifier)
    {
        if (key == Key && Modifier == modifier)
        {
            if (RunFunction() == "Success")
            {
                return true;
            }
        }

        return false;
    }

    public void SetKey(PadDevice.PadKey key)
    {
        if (Key == key)
        {
            return;
        }

        Key = key;
        Save();
    }

    public void SetModifier(KeyModifier modifier)
    {
        if (Modifier == modifier)
        {
            return;
        }

        Modifier = modifier;
        Save();
    }
}
}
