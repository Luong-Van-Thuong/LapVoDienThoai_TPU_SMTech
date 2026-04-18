
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace TPU_Assembly.Class
{
    public class CreateFolderFileDefault
    {
        public static string Today => DateTime.Now.ToString("ddMMyyyy");
        public static string BasePath = @"C:\FA\TPU_Assembly_Inspection_Paddle\Images";
        private static readonly object _lock = new object();
        // Tạo encoder JPEG 1 lần duy nhất, tái sử dụng
        private static readonly ImageCodecInfo _jpegCodec = GetEncoder(ImageFormat.Jpeg);
        private static readonly EncoderParameters _jpegParams = CreateJpegParams(90L); // quality 90%

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
        public static void SaveOriginalBitmap(Bitmap image, string indexCamera)
        {
            string saveFolder = Path.Combine(BasePath, Today, "Origin");

            if (!Directory.Exists(saveFolder))
                Directory.CreateDirectory(saveFolder);

            string filename = indexCamera + "_" + DateTime.Now.ToString("HHmmssfff") + ".bmp";
            string fullPath = Path.Combine(saveFolder, filename);

            lock (_lock)
            {
                //image.Save(fullPath, System.Drawing.Imaging.ImageFormat.Bmp);
                image.Save(fullPath, _jpegCodec, _jpegParams);
            }

            //lock (image)
            //{
            //    image.Save(fullPath, System.Drawing.Imaging.ImageFormat.Bmp);
            //}
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            foreach (var codec in ImageCodecInfo.GetImageEncoders())
            {
                if (codec.FormatID == format.Guid) return codec;
            }
            return null;
        }

        private static EncoderParameters CreateJpegParams(long quality)
        {
            var param = new EncoderParameters(1);
            param.Param[0] = new EncoderParameter(Encoder.Quality, quality);
            return param;
        }

    }
}
