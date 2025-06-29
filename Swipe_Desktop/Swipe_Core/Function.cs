using System.Diagnostics;
using System.Management.Automation;
using Newtonsoft.Json;

namespace Swipe_Core
{

public class Function
{
    public enum FunctionType
    {
        Powershell,
        PowershellScript,
        Launch,
    }
    public enum InterfaceType
    {
        Keyboard,
        SwipeBand,
    }

    public event Action<Function>? OnFunctionChange;

    [JsonProperty]
    public Guid Guid { get; private set; } = Guid.NewGuid();
    [JsonProperty]
    public bool IsEnabled { get; private set; } = true;
    [JsonProperty]
    public string Name { get; private set; } = "New Function";
    [JsonProperty]
    public FunctionType FuncType { get; private set; } = FunctionType.Powershell;
    [JsonProperty]
    public InterfaceType Interface { get; private set; } = InterfaceType.Keyboard;
    [JsonProperty]
    public string Command { get; private set; } = "";
    [JsonProperty]
    public int NumActivations { get; set; } = 0;

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
    public void SetInterfaceType(InterfaceType interfaceType)
    {
        Interface = interfaceType;
        Save();
    }
    public void SetPowershellCommand(string command)
    {
        Command = command;
        Save();
    }
    public void SetPowershellScriptPath(string exe)
    {
        Command = exe;
        Save();
    }

    public void SetExe(string exe)
    {
        Command = exe;
        Save();
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
            case FunctionType.PowershellScript: {
                ProcessStartInfo startInfo =
                    new ProcessStartInfo() { FileName = "powershell.exe",
                                             Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{Command}\"",
                                             UseShellExecute = false,
                                             RedirectStandardOutput = false,
                                             RedirectStandardError = false,
                                             CreateNoWindow = true };
                Process.Start(startInfo);
                break;
            }
            case FunctionType.Launch: {
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = Command;
                Process.Start(start);
                break;
            }
            }

            return "Success";
        }
        return "Disabled";
    }
    protected bool Save()
    {
        if (Name.Length == 0 || Name == "New Function")
        {
            return false;
        }
        string json =
            JsonConvert.SerializeObject(this, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All,
                                                                           Formatting = Formatting.Indented });

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
            return JsonConvert.DeserializeObject<Function>(
                json, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
        }
        catch
        {
            return null;
        }
    }
}
}
