using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Management.Automation;

namespace Swipe_Core
{

public class Function
{
    public enum FunctionType
    {
        Powershell,
        Launch,
    }

    public event Action<Function>? OnFunctionChange;

    [JsonInclude]
    public bool IsEnabled { get; private set; } = true;
    [JsonInclude]
    public string Name { get; private set; } = "New Function";
    [JsonInclude]
    public FunctionType FuncType { get; private set; } = FunctionType.Powershell;
    [JsonInclude]
    public string Command { get; private set; } = "";
    [JsonInclude]
    public int NumActivations { get; set; } = 0;
    [JsonInclude]
    public Dictionary<Guid, Dictionary<string, List<float>>> Recordings { get; private set; } =
        new Dictionary<Guid, Dictionary<string, List<float>>>();
    [JsonInclude]
    public Dictionary<Guid, int> RecordingActivations { get; private set; } = new Dictionary<Guid, int>();
    [JsonInclude]
    public Dictionary<string, bool> AxisEnabled { get; private set; } = new Dictionary<string, bool> {
        { ">LinAccel_x", true }, { ">LinAccel_y", true }, { ">LinAccel_z", true }, { ">Proximity", true } };

    public void Enable()
    {
        IsEnabled = true;
        Save();
    }

    public void Disable()
    {
        IsEnabled = false;
        Save();
    }

    public void SetName(string name)
    {
        var filename = @"Functions\" + Name.Replace(" ", "_") + ".json";
        File.Delete(filename);

        Name = name;
        Save();
    }

    public void SetFunctionType(FunctionType funcType)
    {
        FuncType = funcType;
        Save();
    }
    public void SetPowershellCommand(string command)
    {
        Command = command;
        Save();
    }

    public void SetExe(string exe)
    {
        Command = exe;
        Save();
    }

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

    public string RunFunction()
    {
        if (IsEnabled)
        {
            Debug.WriteLine("Invoke function " + Name);

            switch (FuncType)
            {
            case FunctionType.Powershell:
                using (PowerShell ps = PowerShell.Create())
                {
                    try
                    {
                        ps.AddCommand("Set-ExecutionPolicy")
                            .AddParameter("ExecutionPolicy", "RemoteSigned")
                            .AddParameter("Scope", "Process")
                            .Invoke();

                        ps.AddScript(Command).Invoke();

                        if (ps.Streams.Error.Count > 0)
                        {
                            string errorString = "Errors detected:";
                            foreach (var error in ps.Streams.Error)
                            {
                                errorString += $"- {error.ToString()}";
                            }
                            return errorString;
                        }
                    }
                    catch (Exception ex)
                    {
                        return ex.Message;
                    }
                }
                break;
            case FunctionType.Launch:
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = Command;
                Process.Start(start);
                break;
            }

            return "Success";
        }
        return "Disabled";
    }
    private bool Save()
    {
        if (Name.Length == 0 || Name == "New Function")
        {
            return false;
        }

        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

        var filename = @"Functions\" + Name.Replace(" ", "_") + ".json";
        File.WriteAllText(filename, json);

        OnFunctionChange?.Invoke(this);

        return true;
    }

    public void Remove()
    {
        var filename = @"Functions\" + Name.Replace(" ", "_") + ".json";
        File.Delete(filename);
    }

    static public Function? Load(FileInfo fileInfo)
    {
        try
        {
            string json = File.ReadAllText(fileInfo.FullName);
            return JsonSerializer.Deserialize<Function>(json);
        }
        catch
        {
            return null;
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
