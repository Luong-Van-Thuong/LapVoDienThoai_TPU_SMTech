using TPU_Assembly_Inspection_Paddle;

namespace TPU_Assembly.Class
{
    public class CameraBasler()
    {
       
        public static bool CheckConnectCam(string IndexCamera)
        {
            if (IndexCamera == "CAMERA1")
                return BaslerCam.CAMERA1.IsOpened();
            else if (IndexCamera == "CAMERA2")
                return BaslerCam.CAMERA2.IsOpened();
            else if (IndexCamera == "CAMERA3")
                return BaslerCam.CAMERA3.IsOpened();
            else
                return false;
        }


        public static Bitmap GrabImage(bool ManualGrab, string indexCamera)
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
                Bitmap bitmap_grapIMG = Camera.OneShot_();

                if (bitmap_grapIMG == null)
                {
                    MSystem.InsertAndSaveLogs("Grab Image Failed", Color.Red);
                    return null;
                }

                Bitmap returnImage = (Bitmap)bitmap_grapIMG.Clone();

                if (MAINFORM.SaveImageOrigin || ManualGrab)
                {
                    CreateFolderFileDefault.SaveOriginalBitmap(returnImage);
                }
                return returnImage;

            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs(ex.ToString(), Color.Red);
                return null;
            }
        }


    }
}
