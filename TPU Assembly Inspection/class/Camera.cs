using TPU_Assembly_Inspection_Paddle;

namespace TPU_Assembly.Class
{
    public static class CameraBasler
    {
        public static bool CheckConnectCam(string indexCamera)
        {
            return MAINFORM._cameraDict.TryGetValue(indexCamera, out var config)
                   && config.CameraInterface.IsOpened();
        }

        public static Bitmap GrabImage(string indexCamera)
        {
            if (!MAINFORM._cameraDict.TryGetValue(indexCamera, out var config))
            {
                MSystem.InsertAndSaveLogs($"Camera {indexCamera} Not Found in Dict", Color.Red);
                return null;
            }

            if (!config.CameraInterface.IsOpened())
            {
                MSystem.InsertAndSaveLogs($"Camera {indexCamera} Is Not Open", Color.Red);
                return null;
            }

            try
            {
                using (Bitmap bitmap_grapIMG = config.CameraInterface.OneShot_())
                {

                    if (bitmap_grapIMG == null) return null;

                    Bitmap returnImg = new(bitmap_grapIMG);

                    if (MAINFORM.SaveImageOrigin)
                    {
                        CreateFolderFileDefault.SaveOriginalBitmap(bitmap_grapIMG);
                    }

                    return returnImg;
                }
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs(ex.ToString(), Color.Red);
                return null;
            }
        }
    }
}