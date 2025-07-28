using System.Diagnostics;
using static Swipe_Core.Devices.PadDevice;

namespace Swipe_Core.Devices
{
public class KeyboardDevice : BaseDevice
{
    public event Action<SortedSet<int>>? OnKeyPressed;
    public event Action<SortedSet<int>>? OnKeyReleased;
    public event Action<SortedSet<int>>? OnAllKeyReleased;
    public SortedSet<int> Keys { get; private set; } = new SortedSet<int>();
    public SortedSet<int> LastHeldKeys { get; private set; } = new SortedSet<int>();

    public KeyboardDevice()
    {
        Name = "Keyboard";
        Connected = true;
    }
    public void AddKey(int key)
    {
        LastHeldKeys.Add(key);
        if (Keys.Add(key))
        {
            var keyCopy = new SortedSet<int>(Keys);
            Task.Run(() => OnKeyPressed?.Invoke(keyCopy));
        }
    }

    public void RemoveKey(int key)
    {
        if (Keys.Contains(key))
        {
            var keyCopy = new SortedSet<int>(Keys);
            Task.Run(() => OnKeyReleased?.Invoke(keyCopy));
            Keys.Remove(key);
        }

        if (Keys.Count == 0)
        {
            var keyCopy = new SortedSet<int>(LastHeldKeys);
            Task.Run(() => OnAllKeyReleased?.Invoke(keyCopy));
            LastHeldKeys.Clear();
        }
    }

    override protected void OnUpdated_Internal(string obj)
    {
    }
}
}
