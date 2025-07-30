namespace Swipe_Core.Commands
{
public abstract class Command
{
    public enum CommandType
    {
        None,
        CenterAndFullscreen,
        RegisterApplication,
        ForegroundApplication,
    }

    static public Dictionary<string, Command?> CommandInstances = new Dictionary<string, Command?>();
    public int Instance = 0;

    public abstract bool Execute();
}
}
