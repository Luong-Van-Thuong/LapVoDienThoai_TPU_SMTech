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
                if (!config.CameraInterface.ReOpenCamera())
                {
                    MSystem.InsertAndSaveLogs($"Camera {indexCamera} Is Not Open", Color.Red);
                    return null;
                }
            }
            try
            {
                using (Bitmap bitmap_grapIMG = config.CameraInterface.OneShot_())
                {

                    if (bitmap_grapIMG == null) return null;

                    Bitmap returnImg = new(bitmap_grapIMG);

                    if (MAINFORM.SaveImageOrigin)
                    {
                        //CreateFolderFileDefault.SaveOriginalBitmap(bitmap_grapIMG);
                        //using (var clone = new Bitmap(bitmap_grapIMG))
                        //{
                        //    CreateFolderFileDefault.SaveOriginalBitmap(clone);
                        //}

                        var clone = new Bitmap(bitmap_grapIMG);

                        _ = Task.Run(() =>
                        {
                            try
                            {
                                CreateFolderFileDefault.SaveOriginalBitmap(clone);
                            }
                            catch (Exception ex)
                            {
                                MSystem.InsertAndSaveLogs(ex.ToString(), Color.Red);
                            }
                            finally
                            {
                                clone?.Dispose(); 
                            }
                        });
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

        public static bool UserSetSave(string indexCamera)
        {
            try
            {
                if (MAINFORM._cameraDict.TryGetValue(indexCamera, out var config))
                {
                    config.CameraInterface.UserSetSave();
                    return true;
                }
                return false;
            }
            catch (Exception ex) { 
            
                MSystem.InsertAndSaveLogs(ex.ToString(), Color.Red);
                return false;
            }
        }

        public static bool ReOpenCamera(string indexCamera)
        {
            try
            {
                if (MAINFORM._cameraDict.TryGetValue(indexCamera, out var config))
                {
                    return config.CameraInterface.ReOpenCamera();
                }
                return false;
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs(ex.ToString(), Color.Red);
                return false;
            }
        }

        public static bool SetExposureTime(string indexCamera, double exposuretime)
        {
            try
            {
                if (MAINFORM._cameraDict.TryGetValue(indexCamera, out var config))
                {
                    config.CameraInterface.SetExposureTime(exposuretime);

                    return true;
                }
                return false;
            }
            catch (Exception ex) 
            { 
                MSystem.InsertAndSaveLogs(ex.ToString(), Color.Red);
                return false;
            }
        }

        public static bool SetGain(string indexCamera, double gain)
        {
            try
            {
                if (MAINFORM._cameraDict.TryGetValue(indexCamera, out var config))
                {
                    config.CameraInterface.SetGain(gain);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs(ex.ToString(), Color.Red);
                return false;
            }
        }

        public static bool SetGamma(string indexCamera, double gamma)
        {
            try
            {
                if (MAINFORM._cameraDict.TryGetValue(indexCamera, out var config))
                {
                    config.CameraInterface.SetGamma(gamma);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs(ex.ToString(), Color.Red);
                return false;
            }
        }

        public static double GetExposureTime(string v)
        {
            try
            {
                if (MAINFORM._cameraDict.TryGetValue(v, out var config))
                {
                    return config.CameraInterface.GetExposureTime();
                }
                return 0;
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs(ex.ToString(), Color.Red);
                return 0;
            }
        }

        public static double GetGain(string selectedCam)
        {
            try
            {
                if (MAINFORM._cameraDict.TryGetValue(selectedCam, out var config))
                {
                    return config.CameraInterface.GetGain();
                }
                return 0;
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs(ex.ToString(), Color.Red);
                return 0;
            }
        }

        public static double GetGamma(string selectedCam)
        {
            try
            {
                if (MAINFORM._cameraDict.TryGetValue(selectedCam, out var config))
                {
                    return config.CameraInterface.GetGamma();
                }
                return 0;
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs(ex.ToString(), Color.Red);
                return 0;
            }
        }
    }
}