using System.Diagnostics;

namespace Swipe_Core
{
static public class KeyboardManager
{
    static public event Action<SortedSet<int>>? OnKeyPressed;
    static public event Action<SortedSet<int>>? OnKeyReleased;
    static public event Action<SortedSet<int>>? OnAllKeyReleased;
    static public SortedSet<int> Keys { get; private set; } = new SortedSet<int>();
    static public SortedSet<int> LastHeldKeys { get; private set; } = new SortedSet<int>();
    static public void AddKey(int key)
    {
        LastHeldKeys.Add(key);
        if (Keys.Add(key))
        {
            var keyCopy = new SortedSet<int>(Keys);
            Task.Run(() => OnKeyPressed?.Invoke(keyCopy));
        }
    }

    static public void RemoveKey(int key)
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
}
}
