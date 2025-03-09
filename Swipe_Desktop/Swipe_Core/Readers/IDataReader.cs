namespace Swipe_Core.Readers
{
public interface IDataReader
{
    event Action<string>? OnUpdated;
}
}
