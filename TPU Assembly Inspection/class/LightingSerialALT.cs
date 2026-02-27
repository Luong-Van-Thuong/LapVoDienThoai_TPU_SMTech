using ILignting;
using Lighting_Params;
using System.IO.Ports;
using TPU_Assembly.Class;

namespace Lighting_ALT
{
    public class LightingSerialALT : ILighting, IDisposable
    {
        public SerialPort serialPort;
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

        public void Connect(string port, int baudRate)
        {
            try
            {
                Disconnect();

                serialPort = new SerialPort(port, baudRate)
                {
                    ReadTimeout = 500,
                    WriteTimeout = 500,
                    NewLine = "\r\n"
                };

                serialPort.Open();
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs($"[Lighting Error] Không thể mở cổng COM: {ex.Message}", Color.Red);
            }
        }

        public bool IsConnected()
        {
            return serialPort != null && serialPort.IsOpen;
        }

        public void Disconnect()
        {
            if (serialPort != null)
            {
                if (serialPort.IsOpen) serialPort.Close();
                serialPort.Dispose();
                serialPort = null;
            }
        }

        public bool LightON(int channel, int val)
        {
            if (!IsConnected()) return false;

            try
            {
                serialPort.Write(StringFormat(channel, val));
                return true;
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs($"[Lighting Error] Lỗi khi bật đèn: {ex.Message}", Color.Red);
                return false;
            }
        }

        public bool LightOFF(int channel)
        {
            return LightON(channel, 0);
        }

        public static string StringFormat(int channel, int val)
        {
            int ch = channel - 1;
            return string.Format("L{0}{1:000}\r\n", ch, val);
        }

        public List<int> FormatMultiChannel()
        {
            List<int> channelList = new List<int>();
            for (int i = 0; i < multiChannel; i++)
            {
                channelList.Add(i + 1);
            }
            return channelList;
        }

        public void MutilChannelON(int val)
        {
            foreach (int i in FormatMultiChannel())
            {
                LightON(i, val);
                Thread.Sleep(30);
            }
        }

        public void MutilChannelOFF()
        {
            foreach (int i in FormatMultiChannel())
            {
                LightOFF(i);
                Thread.Sleep(30);
            }
        }

        public List<(string channel, string value)> GetLights()
        {
            return new List<(string ch, string val)>
            {
                (lightingParams.ChannelLight, lightingParams.ValueLight)
            };
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}