using System.Configuration;
using TPU_Assembly_Inspection_Paddle;
namespace TPU_Assembly.Class
{
    public class ConfigurationSystem
    {

        public static void SaveSystemSetting()
        {
            try
            {
                SaveSetting("SaveImageOrigin", MAINFORM.SaveImageOrigin.ToString());
                SaveSetting("SaveImageOK", MAINFORM.SaveImageOK.ToString());
                SaveSetting("SaveImageNG", MAINFORM.SaveImageNG.ToString());
                SaveSetting("SaveLogDays", MAINFORM.SaveLogDays.ToString());
                SaveSetting("ConfidenceThreshold", MAINFORM.ConfidenceThreshold.ToString());
                SaveSetting("IPAddress", MAINFORM.IPAddress.ToString());
                SaveSetting("Port", MAINFORM.Port.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Saving: " + ex.Message);
            }
        }

        private static void SaveSetting(string key, string value)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[key] == null)
            {
                config.AppSettings.Settings.Add(key, value);
            }
            else
            {
                config.AppSettings.Settings[key].Value = value;
            }

            if (key == "ConfidenceThreshold")
            {
                if (!float.TryParse(value, out float parsedValue) || parsedValue < 0 || parsedValue > 1)
                {
                    ReloadSystemSettings();
                    throw new ArgumentException("ConfidenceThreshold must be a float between 0 and 1.");
                }
            }
            if (key == "SaveLogDays")
            {
                if (!int.TryParse(value, out int parsedValue) || parsedValue < 0)
                {
                    ReloadSystemSettings();
                    throw new ArgumentException("SaveLogDays must be a non-negative integer.");
                }
            }

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        public static void ReloadSystemSettings()
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                MAINFORM.SaveImageOrigin = bool.Parse(appSettings["SaveImageOrigin"] ?? "true");
                MAINFORM.SaveImageOK = bool.Parse(appSettings["SaveImageOK"] ?? "true");
                MAINFORM.SaveImageNG = bool.Parse(appSettings["SaveImageNG"] ?? "true");
                MAINFORM.SaveLogDays = int.Parse(appSettings["SaveLogDays"] ?? "15");
                MAINFORM.ConfidenceThreshold = float.Parse(appSettings["ConfidenceThreshold"] ?? "0.65");
                MAINFORM.IPAddress = appSettings["IPAddress"] ?? "127.0.0.1";
                MAINFORM.Port = int.Parse(appSettings["Port"] ?? "9900");
            }
            catch { }
        }
    }
}
