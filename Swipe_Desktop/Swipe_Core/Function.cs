using System.Diagnostics;
using System.Management.Automation;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    public List<Dictionary<string, List<float>>> Recordings { get; private set; } =
        new List<Dictionary<string, List<float>>>();

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

    public void AddRecording(Dictionary<string, List<float>> recording)
    {
        Recordings.Add(recording);
        Save();
    }

    public void RunFunction()
    {
        if (IsEnabled)
        {
            Debug.WriteLine("Invoke function " + Name);

            switch (FuncType)
            {
            case FunctionType.Powershell:
                using (PowerShell PowerShellInstance = PowerShell.Create())
                {
                    PowerShellInstance.AddScript(Command);
                    IAsyncResult result = PowerShellInstance.BeginInvoke();
                }
                break;
            case FunctionType.Launch:
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = Command;
                Process.Start(start);
                break;
            }
        }
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
}
}
