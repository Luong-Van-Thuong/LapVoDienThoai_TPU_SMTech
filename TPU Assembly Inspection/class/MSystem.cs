namespace TPU_Assembly.Class
{
    public class MSystem
    {
      
        private static void MessageColor(RichTextBox rtBox, string msg, Color color)
        {
            if (rtBox == null) return;

            lock (rtBox)
            {
                int maxLines = 100;
                if (rtBox.Lines.Length > maxLines)
                {
                    rtBox.Select(0, rtBox.GetFirstCharIndexFromLine(rtBox.Lines.Length - maxLines));
                    rtBox.SelectedText = string.Empty;
                }

                rtBox.SelectionStart = rtBox.TextLength;
                rtBox.SelectionLength = 0;

                rtBox.SelectionColor = color;

                string fullMessage = DateTime.Now.ToString(" => yyyy-MM-dd | HH:mm:ss.fff | ") + msg;
                rtBox.AppendText(fullMessage + Environment.NewLine);

                rtBox.ScrollToCaret();
                rtBox.SelectionColor = Color.Black;
            }
        }



        private static RichTextBox LogsVision;
        public static void SetRichTextLogs(RichTextBox RichText1)
        {
            LogsVision = RichText1;
        }


        public static void InsertAndSaveLogs(string messager1, Color color)
        {
            string fullMessage =  messager1;

            if (LogsVision == null) return;

            if (LogsVision.InvokeRequired)
            {
                LogsVision.Invoke(new Action(() => MessageColor(LogsVision, fullMessage, color)));
            }
            else
            {
                MessageColor(LogsVision, fullMessage, color);
            }

            SaveLogToFile(fullMessage);

        }


        public static string FileName => DateTime.Now.ToString("yyyy-MM-dd") + ".txt";

        private static readonly object _logLock = new object();

        public static void SaveLogToFile(string message)
        {
            string fullMessage = DateTime.Now.ToString(" => HH:mm:ss.fff | ") + message;
            lock (_logLock)
            {
                try
                {
                    string folderPath = @"C:\FA\TPU_Assembly_Inspection_Paddle\LOGFILE\MAIN_LOG";
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    string filePath = Path.Combine(folderPath, FileName);

                    File.AppendAllText(filePath, fullMessage + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Log Error: " + ex.Message);
                }
            }
        }

        //public static string timestamp => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        //public static void SaveVisionResultToLog(string processingTime, string resultsendrobot, string count)
        //{
        //    try
        //    {
        //        string folderPath = @"C:\FA\TPU_Assembly\Logfile\ResultVision";
        //        if (!Directory.Exists(folderPath))
        //        {
        //            Directory.CreateDirectory(folderPath);
        //        }
        //        string filePath = Path.Combine(folderPath, FileName);

        //        string logLine = $"{timestamp} | Vistiontime: {processingTime}| resultsendrobot: {resultsendrobot} | Count: {count}";

        //        File.AppendAllText(filePath, logLine + Environment.NewLine);
        //    }
        //    catch (Exception ex)
        //    {
        //        MSystem.InsertAndSaveLogs("Lỗi ghi file log resultvision: " + ex.Message, Color.Red);
        //    }
        //}
    }
}


