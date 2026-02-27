using TPU_Assembly_Inspection_Paddle;

namespace TPU_Assembly.Class
{
    public class CameraBasler()
    {
       
        public static bool CheckConnectCam(string IndexCamera)
        {
            return MAINFORM._cameraDict.ContainsKey(IndexCamera)
           && MAINFORM._cameraDict[IndexCamera].CameraInterface.IsOpened();
        }


        public static Bitmap GrabImage(string indexCamera)
        {
            ICameraInterface Camera;
            switch (indexCamera)
            {
                case "CAMERA1": Camera = BaslerCam.CAMERA1; break;
                case "CAMERA2": Camera = BaslerCam.CAMERA2; break;
                case "CAMERA3": Camera = BaslerCam.CAMERA3; break;
                default: return null;
            }

            if (!CheckConnectCam(indexCamera))
            {
                MSystem.InsertAndSaveLogs($"Camera {indexCamera} Is Not Open", Color.Red);
                return null;
            }
            try
            {
                using (Bitmap bitmap_grapIMG = Camera.OneShot_())
                {
                    if (bitmap_grapIMG == null) return null;

                    Bitmap returnImg = (Bitmap)bitmap_grapIMG.Clone();

                    if (MAINFORM.SaveImageOrigin)
                    {
                        CreateFolderFileDefault.SaveOriginalBitmap(returnImg);
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
