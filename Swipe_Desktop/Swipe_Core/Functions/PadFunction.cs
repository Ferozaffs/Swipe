using Newtonsoft.Json;
using Swipe_Core.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swipe_Core.Functions
{
public class PadFunction : Function
{
    private bool _fired = false;
    [JsonProperty]
    public PadDevice.PadKey Key { get; private set; } = PadDevice.PadKey.None;

    public bool EvaluateAndRun(PadDevice.PadKey key)
    {
        if (_fired == false && key == Key)
        {
            if (RunFunction() == "Success")
            {
                return true;
            }
        }

        return false;
    }
    public void Reset()
    {
        _fired = false;
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
}
}
