using Swipe_Core.Readers;

namespace Swipe_Core.Devices
{
public abstract class BaseDevice
{
    public event Action<bool>? OnConnection;

    protected string? _name;
    protected string? _uuid;
    protected IDataReader.ReaderType _readerType;
    protected IDataReader? _reader;

    public async Task<bool> Start()
    {
        if (_reader != null)
        {
            return await _reader.Start();
        }
        return false;
    }

    public bool Stop()
    {
        if (_reader != null)
        {
            return _reader.Stop();
        }
        return false;
    }

    protected void SetupReader()
    {
        switch (_readerType)
        {
        case IDataReader.ReaderType.Bluetooth:
            if (_name == null || _uuid == null)
            {
                throw new InvalidOperationException("Name and UUID not setup");
            }
            _reader = new BluetoothReader(_name, _uuid);
            break;
        case IDataReader.ReaderType.Serial:
            _reader = new COMReader("COM7", 115200);
            break;
        }

        if (_reader != null)
        {
            _reader.OnConnection += OnConnection_Internal;
            _reader.OnUpdated += OnUpdated_Internal;
        }
    }

    abstract protected void OnUpdated_Internal(string obj);

    private void OnConnection_Internal(bool obj)
    {
        OnConnection?.Invoke(obj);
    }
}
}
