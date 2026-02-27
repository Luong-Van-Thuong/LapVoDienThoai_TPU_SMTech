
namespace Lighting_Params
{
    public class LightingParams : FileRW
    {
        public string VendorLight;
        public string CommLight;
        public string BaurateLight;
        public string ChannelLight;
        public string ValueLight;
        public string NumberChannelLight;
        public int MutilChannel;

        public LightingParams()
        {
        }

        public void ReadData(string _model = "")
        {
            string sVal;
            int iVal;
            var section = "LIGHT";
            sVal = ReadValue(_model, section, "VENDOR", "HIK");
            if(!string.IsNullOrEmpty(sVal)) VendorLight = sVal;

            sVal = ReadValue(_model, section, "COMM", "COM3");
            if(!string.IsNullOrEmpty(sVal)) CommLight = sVal;

            sVal = ReadValue(_model, section, "BAURATE", "115200");
            if (!string.IsNullOrEmpty(sVal)) BaurateLight = sVal;

            sVal = ReadValue(_model, section, "NO_CHANNEL", "4");
            if (!string.IsNullOrEmpty(sVal)) NumberChannelLight = sVal;

            sVal = ReadValue(_model, section, "MUTIL_CHANNEL", "2");
            if (int.TryParse(sVal, out iVal)) MutilChannel = iVal;

            sVal = ReadValue(_model, section, "CHANNEL", "1");
            if(!string.IsNullOrEmpty(sVal)) ChannelLight = sVal;

            sVal = ReadValue(_model, section, "VALUE", "255");
            if (!string.IsNullOrEmpty(sVal)) ValueLight = sVal;
        }
    }
}
