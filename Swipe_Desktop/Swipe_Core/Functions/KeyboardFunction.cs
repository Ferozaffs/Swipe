using Newtonsoft.Json;
using System.Collections.Generic;

namespace Swipe_Core.Functions
{

    public class KeyboardFunction : Function
    {
        private bool _fired = false;
        [JsonProperty]
        public Dictionary<Guid, SortedSet<int>> KeySets { get; private set; } = new Dictionary<Guid, SortedSet<int>>();

        public bool EvaluateAndRun(SortedSet<int> keys)
        {
            if (_fired == false && EvaluateKeys(keys))
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

        public Guid AddKeySet(SortedSet<int> keys)
        {
            if (KeySets.Values.Any(s => s.SetEquals(keys)))
            {
                return Guid.Empty;
            }

            var guid = Guid.NewGuid();
            KeySets.Add(guid, new SortedSet<int>(keys));
            Save();
            return guid;
        }

        public void RemoveKeySet(Guid guid)
        {
            KeySets.Remove(guid);
            Save();
        }

        private bool EvaluateKeys(SortedSet<int> keys)
        {
            foreach (var set in KeySets)
            {
                if (set.Value.SetEquals(keys))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
