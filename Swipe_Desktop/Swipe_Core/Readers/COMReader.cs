using System.Diagnostics;
using System.IO.Ports;
using System.Text;

namespace Swipe_Core.Readers
{
public class COMReader : IDataReader
{
    public event Action<string>? OnUpdated;
    public event Action<bool>? OnConnection;

    private SerialPort? _serialPort;
    private Thread? _serialThread;
    private bool _serialThreadActive;
    private StringBuilder _serialbuffer = new StringBuilder();

    public COMReader(string COMPort, int Baud = 115200)
    {
        _serialPort = new SerialPort(COMPort, Baud);
    }

    public async Task<bool> Start()
    {
        await Task.CompletedTask;

        if (_serialPort != null)
        {
            try
            {
                _serialPort.Handshake = Handshake.None;
                _serialPort.DtrEnable = true;
                _serialPort.ReceivedBytesThreshold = 20;
                _serialPort.ReadBufferSize = 8192;
                _serialPort.Open();

                Debug.WriteLine($"COM opened");

                _serialThreadActive = true;
                _serialThread = new Thread(SerialRead);
                _serialThread.Start();
                OnConnection?.Invoke(true);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                OnConnection?.Invoke(false);
                return false;
            }
        }

        return false;
    }

    public bool Stop()
    {
        if (_serialPort != null)
        {
            _serialThreadActive = false;
            _serialThread?.Join();
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
            Debug.WriteLine("COM closed");
            OnConnection?.Invoke(false);
        }

        return true;
    }

    private void SerialRead()
    {
        while (_serialThreadActive && _serialPort != null)
        {
            while (_serialPort.BytesToRead > 0 && _serialThreadActive)
            {
                int data = _serialPort.ReadByte();
                if (data == '\n' || data == '\r') // End of line
                {
                    string line = _serialbuffer.ToString();
                    if (!string.IsNullOrEmpty(line))
                    {
                        OnUpdated?.Invoke(line);
                        _serialbuffer.Clear();
                    }
                }
                else
                {
                    _serialbuffer.Append((char)data);
                }
            }
        }
    }
}
}
