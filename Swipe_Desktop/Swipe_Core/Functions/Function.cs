using System.Data.SqlClient;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Swipe_Core.Commands;

namespace Swipe_Core.Functions
{

public class Function
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public enum FunctionType
    {
        Powershell,
        PowershellScript,
        Launch,
        Command,
    }
    public enum InterfaceType
    {
        Keyboard,
        Band,
        Pad,
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
    public string CommandString { get; private set; } = "";
    [JsonProperty]
    public Commands.Command.CommandType CommandType { get; private set; } = Commands.Command.CommandType.None;
    [JsonProperty]
    public int CommandInstance { get; private set; } = 0;
    [JsonProperty]
    public string Arguments { get; private set; } = "";
    [JsonProperty]
    public int NumActivations { get; set; } = 0;

    private Commands.Command? Command = null;

    public void PostLoad()
    {
        if (FuncType == FunctionType.Command && CommandType != Commands.Command.CommandType.None)
        {
            SetCommand(CommandType, false);
        }
    }

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
        CommandString = command;
        Save();
    }
    public void SetFilepath(string filepath)
    {
        CommandString = filepath;
        Save();
    }

    public void SetArgs(string args)
    {
        Arguments = args;
        Save();
    }

    public void SetCommand(Commands.Command.CommandType commandType, bool save = true)
    {
        CommandType = commandType;
        var identifier = commandType.ToString() + "_" + CommandInstance;
        Commands.Command ? value;
        Commands.Command.CommandInstances.TryGetValue(identifier, out value);

        if (value != null)
        {
            Command = value;
        }
        else
        {
            switch (commandType)
            {
            case Commands.Command.CommandType.CenterAndFullscreen: {
                Command = new CenterAndFullscreenCommand();
                break;
            }
            case Commands.Command.CommandType.RegisterApplication: {
                Command = new RegisterApplicationCommand();
                break;
            }
            case Commands.Command.CommandType.ForegroundApplication: {
                Command = new ForegroundApplicationCommand();
                break;
            }
            }

            if (Command != null)
            {
                Command.Instance = CommandInstance;
                Commands.Command.CommandInstances.Add(identifier, Command);
            }
        }

        if (save)
        {
            Save();
        }
    }

    public void SetCommandInstance(int instance)
    {
        CommandInstance = instance;
        if (Command != null)
        {
            SetCommand(CommandType);
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

                        ps.AddScript(CommandString).Invoke();

                        if (ps.Streams.Error.Count > 0)
                        {
                            string errorString = "Errors detected:";
                            foreach (var error in ps.Streams.Error)
                            {
                                errorString += $"- {error}";
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
                ProcessStartInfo startInfo = new ProcessStartInfo() {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{CommandString}\"  {Arguments}",
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    CreateNoWindow = true
                };
                Process.Start(startInfo);
                break;
            }
            case FunctionType.Launch: {
                Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(CommandString));

                if (processes.Length > 0 && Arguments.Contains("NewProcess") == false)
                {
                    IntPtr hWnd = processes[0].MainWindowHandle;
                    if (hWnd != IntPtr.Zero)
                    {
                        ShowWindow(hWnd, 9);
                        SetForegroundWindow(hWnd);
                    }
                }
                else
                {
                    ProcessStartInfo start = new ProcessStartInfo();
                    start.FileName = CommandString;
                    start.Arguments = Arguments;
                    Process.Start(start);
                }
                break;
            }
            case FunctionType.Command: {
                if (Command != null)
                {
                    if (Command.Execute())
                    {
                        return "Failed";
                    }
                }
                break;
            }
            }

            NumActivations++;
            return "Success";
        }
        return "Disabled";
    }
    public bool Save()
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
            var func = JsonConvert.DeserializeObject<Function>(
                json, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            func?.PostLoad();
            return func;
        }
        catch
        {
            return null;
        }
    }
}
}
