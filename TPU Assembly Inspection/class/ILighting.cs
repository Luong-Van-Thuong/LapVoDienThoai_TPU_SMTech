namespace ILignting
{
    public interface ILighting
    {
        void Connect(string port, int baurate);
        bool IsConnected();
        void Disconnect();
        bool LightON(int channel, int val);
        bool LightOFF(int channel);
        bool MutilChannelON(int val);
        bool MutilChannelOFF();
        List<(string channel, string value)> GetLights();
    }
}
