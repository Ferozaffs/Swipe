using System;
using System.IO;

namespace Swipe_Core
{
using System;
using System.IO;

public class Logger
{
    private readonly string _logFilePath;
    public event EventHandler<string>? OnLogEntry;

    public Logger()
    {
        string rootPath = AppDomain.CurrentDomain.BaseDirectory;
        _logFilePath = Path.Combine(rootPath, "Log.txt");
    }

    public void Log(string message)
    {
        string timestampedMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";

        File.AppendAllText(_logFilePath, timestampedMessage + Environment.NewLine);

        OnLogEntry?.Invoke(this, timestampedMessage);
    }
}

}
