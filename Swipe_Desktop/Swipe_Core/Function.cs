using System.Diagnostics;
using System.Management.Automation;
using System.Text.Json;

namespace Swipe_Core
{

public class Function
{
    public enum FunctionType
    {
        Powershell,
        Launch,
    }

    public bool IsEnabled = true;
    public string Name = "New Function";
    public FunctionType FuncType = FunctionType.Powershell;
    public List<Dictionary<string, List<float>>> Recordings = new List<Dictionary<string, List<float>>>();

    private string _command = "";

    public void SetPowershellCommand(string command)
    {
        _command = command;
    }

    public void SetExe(string exe)
    {
        _command = exe;
    }

    public string GetDetails()
    {
        return _command;
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
                    PowerShellInstance.AddScript(_command);
                    IAsyncResult result = PowerShellInstance.BeginInvoke();
                }
                break;
            case FunctionType.Launch:
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = _command;
                Process.Start(start);
                break;
            }
        }
    }
    public bool Save()
    {
        if (Name.Length == 0 || Name == "New Function")
        {
            return false;
        }

        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

        var filename = @"Functions\" + Name.Replace(" ", "_") + ".json";
        File.WriteAllText(filename, json);

        return true;
    }

    static public Function? Load(FileInfo fileInfo)
    {
        return JsonSerializer.Deserialize<Function>(fileInfo.FullName);
    }
}
}
