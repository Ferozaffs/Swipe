using System.Runtime.InteropServices;

namespace Swipe_Core.Commands
{
public class ForegroundApplicationCommand : Command
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public override bool Execute()
    {
        var identifier = "RegisterApplication_" + Instance;
        Command ? value;
        CommandInstances.TryGetValue(identifier, out value);

        if (value != null)
        {
            var func = (value as RegisterApplicationCommand);
            if (func != null)
            {
                ShowWindow(func.SavedWindowHandle, 9);
                return SetForegroundWindow(func.SavedWindowHandle);
            }
        }

        return false;
    }
}
}
