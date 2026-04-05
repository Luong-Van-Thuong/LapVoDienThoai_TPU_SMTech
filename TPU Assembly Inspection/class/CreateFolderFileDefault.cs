
using System;
using System.Drawing;
using System.IO;

namespace TPU_Assembly.Class
{
    public class CreateFolderFileDefault
    {
        public static string Today => DateTime.Now.ToString("ddMMyyyy");
        public static string BasePath = @"C:\FA\TPU_Assembly_Inspection_Paddle\Images";
        private static readonly object _lock = new object();

        public static void CreateSaveFolders()
        {
            string dayFolder = Path.Combine(BasePath, Today);

            if (!Directory.Exists(dayFolder))
                Directory.CreateDirectory(dayFolder);

            string Origin = Path.Combine(dayFolder, "Origin");
            string OK = Path.Combine(dayFolder, "OK");
            string NG = Path.Combine(dayFolder, "NG");

            if (!Directory.Exists(Origin))
                Directory.CreateDirectory(Origin);
            if (!Directory.Exists(OK))
                Directory.CreateDirectory(OK);
            if (!Directory.Exists(NG))
                Directory.CreateDirectory(NG);

        }
        public static void SaveOriginalBitmap(Bitmap image)
        {
            string saveFolder = Path.Combine(BasePath, Today, "Origin");

            if (!Directory.Exists(saveFolder))
                Directory.CreateDirectory(saveFolder);

            string filename = DateTime.Now.ToString("HHmmssfff") + ".bmp";
            string fullPath = Path.Combine(saveFolder, filename);

            lock (_lock)
            {
                image.Save(fullPath, System.Drawing.Imaging.ImageFormat.Bmp);
            }

            //lock (image)
            //{
            //    image.Save(fullPath, System.Drawing.Imaging.ImageFormat.Bmp);
            //}
        }

        

    }
}
