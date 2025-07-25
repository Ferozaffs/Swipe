namespace Swipe_Core.Readers
{
public interface IDataReader
{
    enum ReaderType
    {
        Bluetooth,
        Serial
    }

    event Action<string>? OnUpdated;
    event Action<bool>? OnConnection;

    Task<bool> Start();
    bool Stop();
}
}
