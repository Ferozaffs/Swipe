using System.Runtime.InteropServices;

namespace Swipe_Core.Commands
{
public class RegisterApplicationCommand : Command
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    public IntPtr SavedWindowHandle = IntPtr.Zero;

    public override bool Execute()
    {

        SavedWindowHandle = GetForegroundWindow();
        return true;
    }
}
}
