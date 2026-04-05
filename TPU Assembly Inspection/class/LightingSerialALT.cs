using Lighting_Params;
using System.IO.Ports;
using TPU_Assembly.Class;

namespace Lighting_ALT
{
    public class LightingSerialALT : IDisposable
    {
        public bool isConnected { get; private set; } = false;

        private readonly LightingParams lightingParams;

        int multiChannel;

        public int Channel => int.TryParse(lightingParams.ChannelLight, out int c) ? c : 1;
        public int Brightness => int.TryParse(lightingParams.ValueLight, out int v) ? v : 0;

        public LightingSerialALT()
        {
            lightingParams = new LightingParams();
            lightingParams.ReadData();
            multiChannel = lightingParams.MutilChannel;
            Connect(lightingParams.CommLight, Convert.ToInt32(lightingParams.BaurateLight));
        }


        public string PortName { get; private set; }

        private readonly object LockLightControl = new object();

        private SerialPort Serial;

        public event Action<string> StatusMessage;


        public bool Connect(string port, int baudRate)
        {
            lock (LockLightControl)
            {
                try
                {
                    if (Serial != null)
                    {
                        if (Serial.IsOpen) Serial.Close();
                        Serial = null;
                    }
                    Serial = new SerialPort(port, baudRate, Parity.None, 8, StopBits.One)
                    {
                        Handshake = Handshake.None,
                        ReadTimeout = 500,
                        WriteTimeout = 500,
                        NewLine = "\r\n"
                    };

                    Serial.Open();
                    isConnected = Serial.IsOpen;
                    return Serial.IsOpen;
                }
                catch (Exception ex)
                {
                    StatusMessage?.Invoke($"Error opening port: {ex.Message}");
                    isConnected = false;
                    return false;
                }
            }
        }

        public bool IsConnected() => isConnected;

        public void ClosePort()
        {
            lock (LockLightControl)
            {
                if (Serial != null)
                {
                    if (Serial.IsOpen) Serial.Close();
                    Serial = null;
                }
                isConnected = false;
                StatusMessage?.Invoke("Port closed");
            }
        }

        private string strData = "";
        private void DataReceivePort(object sender, SerialDataReceivedEventArgs e)
        {
            lock (LockLightControl)
            {
                try
                {
                    strData += Serial.ReadExisting();
                    AnalyzeCommData(strData);
                    strData = "";
                }
                catch (Exception ex)
                {
                    StatusMessage?.Invoke($"{PortName} Receive Data Fail: {ex.Message}");
                }
            }
        }

        #region FUNCTION INTERFACE WITH COM PORT
        public void AnalyzeCommData(string strData)
        {
            strData = strData.Trim();
            StatusMessage?.Invoke(strData);

            if (strData.Length == 0 || strData[0] != 0x02) return;
            string[] _strResult = strData.TrimStart('\x02').Split(',');
            // Add parsing logic if needed
        }

        private void Short2Byte(short nValue, ref char[] cData)
        {
            cData[0] = (char)(0xFF & nValue);
            cData[1] = (char)(0xFF & (nValue >> 8));
        }

        private short Byte2Short(char[] cData)
        {
            short nResult = (short)(cData[0] | (cData[1] << 8));
            return nResult;
        }

        public void SetLightOn(int channel, int value)
        {
            lock (LockLightControl)
            {
                byte[] buff = new byte[7];
                byte csum = 0x00;
                buff[0] = 0x4C;
                buff[1] = 0x12;
                csum ^= buff[1]; // command
                buff[2] = (byte)channel;
                csum ^= buff[2]; // nCh
                buff[3] = (byte)value;
                csum ^= buff[3]; // nVal
                buff[4] = csum;
                buff[5] = 0x0D;
                buff[6] = 0x0A;

                if (Serial != null && Serial.IsOpen)
                {
                    Serial.Write(buff, 0, 7);
                    Thread.Sleep(20);
                }
                else
                {
                    StatusMessage?.Invoke("Light Port Not Open");
                    TryReopenPort();
                }
            }
        }

        public bool MutilChannelON(int value)
        {
            lock (LockLightControl)
            {
                byte[] buff = new byte[9];
                byte csum = 0x00;
                buff[0] = 0x4C;
                buff[1] = 0x15;
                csum ^= buff[1]; // command
                buff[2] = (byte)value;
                csum ^= buff[2]; // nCh
                buff[3] = (byte)value;
                csum ^= buff[3]; // nVal
                buff[4] = (byte)value;
                csum ^= buff[4]; // nVal
                buff[5] = (byte)value;
                csum ^= buff[5]; // nVal
                buff[6] = csum;
                buff[7] = 0x0D;
                buff[8] = 0x0A;

                if (Serial != null && Serial.IsOpen)
                {
                    Serial.Write(buff, 0, 9);
                    return true;
                }
                else
                {
                    StatusMessage?.Invoke("Light Port Not Open");
                    TryReopenPort();
                    return false;
                }
            }
        }

        public bool MutilChannelOFF()
        {
            return MutilChannelON(0);
        }

        public void SetLightOff(int channel)
        {
            lock (LockLightControl)
            {
                byte[] buff = new byte[7];
                byte csum = 0x00;
                buff[0] = 0x4C;
                buff[1] = 0x12;
                csum ^= buff[1]; // command
                buff[2] = (byte)channel;
                csum ^= buff[2]; // nCh
                buff[3] = 0x00;
                csum ^= buff[3]; // nVal
                buff[4] = csum;
                buff[5] = 0x0D;
                buff[6] = 0x0A;

                if (Serial != null && Serial.IsOpen)
                {
                    Serial.Write(buff, 0, 7);
                }
                else
                {
                    StatusMessage?.Invoke("Light Port Not Open");
                    TryReopenPort();
                }
            }
        }

        public void ControlLight2chAll(int chnVal1, int chnVal2)
        {
            lock (LockLightControl)
            {
                byte[] tx_arr = new byte[8];
                int check_sum = 0;
                tx_arr[0] = 0xEF;    // Header1
                tx_arr[1] = 0xEF;    // Header2
                tx_arr[2] = 0x00;
                tx_arr[3] = (byte)chnVal1;
                tx_arr[4] = (byte)chnVal2;
                check_sum = tx_arr[2] ^ tx_arr[3] ^ (tx_arr[4] + 0x01);
                tx_arr[5] = (byte)check_sum;
                tx_arr[6] = 0xEE;   // END1
                tx_arr[7] = 0xEE;   // END2

                if (Serial != null && Serial.IsOpen)
                {
                    Serial.Write(tx_arr, 0, 8);
                }
                else
                {
                    StatusMessage?.Invoke("Light Port Not Open");
                    TryReopenPort();
                }
            }
        }

        private void TryReopenPort()
        {
            if (Serial == null || !Serial.IsOpen)
            {
                if (Connect(PortName, 19200))
                    StatusMessage?.Invoke($"{PortName} Open Comport Success ({PortName})");
                else
                    StatusMessage?.Invoke($"{PortName} Open Comport Fail ({PortName})");

                if (Serial != null)
                    Serial.DataReceived += DataReceivePort;
                Thread.Sleep(10);
            }
        }


        public void Dispose()
        {
            ClosePort();
        }
        #endregion
    }
}