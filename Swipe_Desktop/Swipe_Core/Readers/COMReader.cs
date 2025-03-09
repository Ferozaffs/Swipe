using System.Diagnostics;
using System.IO.Ports;
using System.Text;

namespace Swipe_Core.Readers
{
public class COMReader : IDataReader
{
    public event Action<string>? OnUpdated;

    private string cachedData = "";
    private SerialPort? serialPort;
    private Thread? serialThread;
    private bool serialThreadActive;
    private StringBuilder serialbuffer = new StringBuilder();

    public COMReader(string COMPort, int Baud = 115200)
    {
        serialPort = new SerialPort(COMPort, Baud);

        try
        {
            serialPort.Handshake = Handshake.None;
            serialPort.DtrEnable = true;
            serialPort.ReceivedBytesThreshold = 20;
            serialPort.ReadBufferSize = 8192;
            serialPort.Open();

            Debug.WriteLine($"COM opened");

            serialThreadActive = true;
            serialThread = new Thread(SerialRead);
            serialThread.Start();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error: {ex.Message}");
        }
    }

    public void StopCom()
    {
        if (serialPort != null)
        {
            serialThreadActive = false;
            serialThread?.Join();
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
            Debug.WriteLine("COM closed");
        }
    }

    private void SerialRead()
    {
        while (serialThreadActive && serialPort != null)
        {
            while (serialPort.BytesToRead > 0 && serialThreadActive)
            {
                int data = serialPort.ReadByte();
                if (data == '\n' || data == '\r') // End of line
                {
                    string line = serialbuffer.ToString();
                    if (!string.IsNullOrEmpty(line))
                    {
                        OnUpdated?.Invoke(line);
                        serialbuffer.Clear();
                    }
                }
                else
                {
                    serialbuffer.Append((char)data);
                }
            }
        }
    }
}
}
