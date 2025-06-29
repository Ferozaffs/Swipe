using Newtonsoft.Json;

namespace Swipe_Core
{

public class SwipeBandFunction : Function
{
    [JsonProperty]
    public Dictionary<Guid, Dictionary<string, List<float>>> Recordings {
        get; private set;
    } = new Dictionary<Guid, Dictionary<string, List<float>>>();
    [JsonProperty]
    public Dictionary<Guid, int> RecordingActivations {
        get; private set;
    } = new Dictionary<Guid, int>();
    [JsonProperty]
    public Dictionary<string, bool> AxisEnabled {
        get; private set;
    } = new Dictionary<string, bool> { { ">LinAccel_x", true },
                                       { ">LinAccel_y", true },
                                       { ">LinAccel_z", true },
                                       { ">Proximity",
                                         true } };

    public Guid AddRecording(Dictionary<string, List<float>> recording)
    {
        var deepCopy = new Dictionary<string, List<float>>();

        foreach (var graph in recording)
        {
            deepCopy[graph.Key] = new List<float>(graph.Value);
        }

        var guid = Guid.NewGuid();
        Recordings.Add(guid, deepCopy);
        RecordingActivations.Add(guid, 0);
        Save();

        return guid;
    }

    public void RemoveRecording(Guid guid)
    {
        if (Recordings.ContainsKey(guid))
        {
            Recordings.Remove(guid);
            RecordingActivations.Remove(guid);
            Save();
        }
    }

    public void SetAxisEnabled(string axis, bool enabled)
    {
        if (AxisEnabled.ContainsKey(axis))
        {
            AxisEnabled[axis] = enabled;
            Save();
        }
    }
}
}
