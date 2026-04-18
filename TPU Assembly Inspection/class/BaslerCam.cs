using Basler.Pylon;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace TPU_Assembly.Class
{
    public class BaslerCam : ICameraInterface
    {
        public Camera camera = null;

        private readonly Stopwatch stopWatch = new();

        private bool isOpened = false;

        private PictureBox thisControl;

        public Bitmap Image_BASLER;

        public bool isgrabed = false;

        public bool isContinue = false;

        public string cameraName;

        public string deviceType;

        private readonly PixelDataConverter converter = new();

        private readonly AutoResetEvent _waitForImageEvent = new(false);

        public delegate void ConnectionStatusChangedHandler(string cameraName, bool isConnected);
        public event ConnectionStatusChangedHandler ConnectionStatusChangedEvent;

        public override bool isContinuous() => isContinue;

        public override void SetPictureBox(PictureBox control)
        {
            this.thisControl = control;
        }

        public static ICameraInterface CAMERA1 = new BaslerCam(AoiParam.Instance.CAM1);
        public static ICameraInterface CAMERA2 = new BaslerCam(AoiParam.Instance.CAM2);
        public static ICameraInterface CAMERA3 = new BaslerCam(AoiParam.Instance.CAM3);

        public BaslerCam(string userDefinedName)
        {

            List<ICameraInfo> allCameras = CameraFinder.Enumerate();
            try
            {
                foreach (ICameraInfo cameraInfo in allCameras)
                {

                    if (cameraInfo[CameraInfoKey.UserDefinedName] == userDefinedName)
                    {
                        string cameraSerial = cameraInfo[CameraInfoKey.SerialNumber];
                        camera = new Camera(cameraSerial);
                        cameraName = userDefinedName;
                        deviceType = cameraInfo[CameraInfoKey.DeviceType];

                        camera.CameraOpened += Configuration.AcquireContinuous;
                        camera.ConnectionLost += OnConnectionLost;
                        camera.CameraOpened += OnCameraOpened;
                        camera.CameraClosed += OnCameraClosed;
                        camera.StreamGrabber.GrabStarted += OnGrabStarted;
                        camera.StreamGrabber.ImageGrabbed += OnImageGrabbed;
                        camera.StreamGrabber.GrabStopped += OnGrabStopped;

                        camera.Open();
                        isOpened = true;
                        break;
                    }
                }
            }
            catch
            {
                isOpened = false;
                MSystem.InsertAndSaveLogs($"Failed to open camera: {userDefinedName}", Color.Red);
                ConnectionStatusChangedEvent?.Invoke(cameraName, isOpened);

            }
            finally
            {
                ConnectionStatusChangedEvent?.Invoke(cameraName, isOpened);
            }
        }
        public override bool ReOpenCamera()
        {
            try
            {
                Stop();
                Thread.Sleep(200);
                DestroyCamera();
                Thread.Sleep(200);
            }
            catch (Exception)
            {
            }

            List<ICameraInfo> allCameras = CameraFinder.Enumerate();
            try
            {
                foreach (ICameraInfo cameraInfo in allCameras)
                {
                    if (cameraInfo[CameraInfoKey.UserDefinedName] == cameraName)
                    {
                        string cameraSerial = cameraInfo[CameraInfoKey.SerialNumber];
                        camera = new Camera(cameraSerial);
                        deviceType = cameraInfo[CameraInfoKey.DeviceType];

                        camera.CameraOpened += Configuration.AcquireContinuous;
                        camera.ConnectionLost += OnConnectionLost;
                        camera.CameraOpened += OnCameraOpened;
                        camera.CameraClosed += OnCameraClosed;
                        camera.StreamGrabber.GrabStarted += OnGrabStarted;
                        camera.StreamGrabber.ImageGrabbed += OnImageGrabbed;
                        camera.StreamGrabber.GrabStopped += OnGrabStopped;

                        camera.Open();

                        isOpened = true;
                        break;
                    }
                    MSystem.InsertAndSaveLogs($"ReOpened camera: {cameraName}", Color.Red);
                    return true;
                }
            }
            catch
            {
                MSystem.InsertAndSaveLogs($"Failed to ReOpen camera: {cameraName}", Color.Red);
                return false;
            }
            return false;
        }
        public class AoiParam
        {
            private static AoiParam instance;

            public static AoiParam Instance
            {
                get
                {
                    instance ??= new AoiParam();
                    return instance;
                }
            }


            public string CAM1 = "FRONT";
            public string CAM2 = "REAR";
            public string CAM3 = "LEFT";

            public List<AoiParam> AoiParams = [];
        }


        #region SET /GET Camera Parameters
        //public override bool SetExposureTime(double exposuretime)
        //{
        //    try
        //    {
        //        if (camera == null || !camera.IsOpen) return false;

        //        if (camera.Parameters[PLCamera.ExposureTimeRaw].IsWritable)
        //        {
        //            camera.Parameters[PLCamera.ExposureTimeRaw].SetValue((long)exposuretime);
        //            return true;
        //        }
        //        else
        //        {
        //            MSystem.InsertAndSaveLogs($"Exposure time parameter is not writable for camera: {cameraName}", Color.Red);
        //            return false;
        //        }
        //    }
        //    catch (Exception) { return false; }
        //}

        public override bool SetExposureTime(double exposuretime)
        {
            try
            {
                if (camera == null || !camera.IsOpen) return false;

                if (camera.Parameters[PLCamera.ExposureTimeAbs].IsWritable)
                {
                    camera.Parameters[PLCamera.ExposureTimeAbs].SetValue((long)exposuretime);
                    return true;
                }
                else
                {
                    MSystem.InsertAndSaveLogs($"Exposure time parameter is not writable for camera: {cameraName}", Color.Red);
                    return false;
                }
            }
            catch (Exception) { return false; }
        }

        public override double GetGain()
        {
            try
            {
                if (camera == null || !camera.IsOpen) return 0;
                double gain = camera.Parameters[PLCamera.GainRaw].GetValue();
                return gain;
            }
            catch (Exception)
            {
                MSystem.InsertAndSaveLogs($"Failed to get gain for camera: {cameraName}", Color.Red);
                return 0;
            }
        }

        public override bool SetGain(double gain)
        {
            try
            {
                if (camera == null || !camera.IsOpen) return false;
                if (camera.Parameters[PLCamera.GainRaw].IsWritable)
                {
                    camera.Parameters[PLCamera.GainRaw].SetValue((long)gain);
                    return true;
                }
                else
                {
                    MSystem.InsertAndSaveLogs($"Gain parameter is not writable for camera: {cameraName}", Color.Red);
                    return false;
                }
            }
            catch (Exception) { return false; }
        }

        public override double GetExposureTime()
        {
            try
            {
                if (camera == null || !camera.IsOpen) return 0;
                double exposureTime = camera.Parameters[PLCamera.ExposureTimeRaw].GetValue();
                return exposureTime;
            }
            catch (Exception)
            {
                MSystem.InsertAndSaveLogs($"Failed to get exposure time for camera: {cameraName}", Color.Red);
                return 0;
            }
        }

        public override double GetGamma()
        {
            try
            {
                if (camera == null || !camera.IsOpen) return 0;
                double gamma = camera.Parameters[PLCamera.Gamma].GetValue();
                return gamma;

            }
            catch(Exception) { return 0; }
        }

        public override bool SetGamma(double gamma)
        {
            try
            {
                if (camera == null || !camera.IsOpen) return false;
                if (camera.Parameters[PLCamera.Gamma].IsWritable)
                {
                    camera.Parameters[PLCamera.Gamma].SetValue(gamma);
                    return true;
                }
                else
                {
                    MSystem.InsertAndSaveLogs($"Gamma parameter is not writable for camera: {cameraName}", Color.Red);
                    return false;
                }
            }
            catch (Exception) { return false; }
        }
        public override bool UserSetSave()
        {
            try
            {
                if (camera == null || !camera.IsOpen) return false;
                camera.Parameters[PLCamera.UserSetSelector].SetValue(PLCamera.UserSetSelector.UserSet1);
                camera.Parameters[PLCamera.UserSetDefault].SetValue(PLCamera.UserSetDefault.UserSet1);
                camera.Parameters[PLCamera.UserSetSave].Execute();
                return true;
            } catch (Exception) { return false; }
        }

        #endregion

        #region Event 
        private void OnConnectionLost(Object sender, EventArgs e)
        {
            if (isContinue)
            {
                if (thisControl.InvokeRequired)
                {
                    thisControl.BeginInvoke(new EventHandler<EventArgs>(OnConnectionLost), sender, e);
                    Image_BASLER?.Dispose();
                    Image_BASLER = null;
                    return;
                }
            }
            Thread.Sleep(200);
            try
            {
                camera.Close();
                isOpened = false;
                MSystem.InsertAndSaveLogs($"Camera connection lost: {cameraName}", Color.Red);
            }
            catch (Exception)
            {
                isOpened = false;
                MSystem.InsertAndSaveLogs($"Camera connection lost: {cameraName}", Color.Red);
            }
            finally
            {

                isOpened = false;
                ConnectionStatusChangedEvent?.Invoke(cameraName, isOpened);
                ReOpenCamera();
            }
        }

        private void OnCameraOpened(Object sender, EventArgs e)
        {
            if (isContinue)
            {
                if (thisControl.InvokeRequired)
                {
                    thisControl.BeginInvoke(new EventHandler<EventArgs>(OnCameraOpened), sender, e);
                    return;
                }
            }
            isOpened = true;
            ConnectionStatusChangedEvent?.Invoke(cameraName, isOpened);
        }

        private void OnCameraClosed(Object sender, EventArgs e)
        {
            if (isContinue)
            {
                if (thisControl.InvokeRequired)
                {
                    thisControl.BeginInvoke(new EventHandler<EventArgs>(OnCameraClosed), sender, e);
                    return;
                }
            }
            isOpened = false;
        }

        private void OnGrabStarted(Object sender, EventArgs e)
        {
            if (isContinue)
            {
                if (thisControl.InvokeRequired)
                {

                    thisControl.BeginInvoke(new EventHandler<EventArgs>(OnGrabStarted), sender, e);
                    return;
                }
            }

            stopWatch.Reset();

        }

        #endregion

        public override bool IsOpened()
        {
            return isOpened;
        }

        public override Bitmap OneShot_()
        {
            if (camera == null || !camera.IsOpen) return null;

            _waitForImageEvent.Reset();

            if (!OneShot())
            {
                return null;
            }

            if (_waitForImageEvent.WaitOne(2500))
            {
                if (Image_BASLER != null)
                {
                    return Image_BASLER;
                }
            }
            return null;
        }

        private void OnImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            try
            {
                IGrabResult grabResult = e.GrabResult;

                if (grabResult.IsValid)
                {
                    try
                    {
                        stopWatch.Restart();

                        Bitmap bitmap = new(grabResult.Width, grabResult.Height, PixelFormat.Format8bppIndexed);
                        //Bitmap bitmap = new(grabResult.Width, grabResult.Height, PixelFormat.Format32bppRgb);

                        ColorPalette palette = bitmap.Palette;
                        for (int i = 0; i < 256; i++)
                            palette.Entries[i] = Color.FromArgb(i, i, i);
                        bitmap.Palette = palette;

                        BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
                        //converter.OutputPixelFormat = PixelType.BGRA8packed;
                        converter.OutputPixelFormat = PixelType.Mono8;

                        IntPtr ptrBmp = bmpData.Scan0;
                        converter.Convert(ptrBmp, bmpData.Stride * bitmap.Height, grabResult);

                        bitmap.UnlockBits(bmpData);

                        Image_BASLER?.Dispose();
                        Image_BASLER = bitmap;
                        _waitForImageEvent.Set();
                        isgrabed = true;

                    }
                    catch (Exception ex)
                    {
                        MSystem.InsertAndSaveLogs($"OnImageGrabbed error: {ex.Message}", Color.Red);
                        Thread.Sleep(100);
                    }
                }
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs($"OnImageGrabbed error: {ex.Message}", Color.Red);
            }
            finally
            {
                e.DisposeGrabResultIfClone();
            }
        }

        private void OnGrabStopped(Object sender, GrabStopEventArgs e)
        {
            stopWatch.Reset();
            if (e.Reason != GrabStopReason.UserRequest)
            {
                MessageBox.Show("A grab error occured:\n" + e.ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public override void Stop()
        {
            try
            {
                camera?.StreamGrabber.Stop();
            }
            catch (Exception)
            {
                MSystem.InsertAndSaveLogs($"Failed to stop camera: {cameraName}", Color.Red);
            }
        }

        public override void DestroyCamera()
        {
            try
            {
                if (camera != null)
                {
                    camera.CameraOpened -= Configuration.AcquireContinuous;
                    camera.ConnectionLost -= OnConnectionLost;
                    camera.CameraOpened -= OnCameraOpened;
                    camera.CameraClosed -= OnCameraClosed;
                    camera.StreamGrabber.GrabStarted -= OnGrabStarted;
                    camera.StreamGrabber.ImageGrabbed -= OnImageGrabbed;
                    camera.StreamGrabber.GrabStopped -= OnGrabStopped;

                    camera.Close();
                    camera.Dispose();
                    camera = null;
                }
            }
            catch (Exception)
            {
                MSystem.InsertAndSaveLogs($"Failed to destroy camera: {cameraName}", Color.Red);
            }
        }

        public bool OneShot()
        {
            try
            {
                if (camera == null || !camera.IsOpen) { return false; }

                Image_BASLER?.Dispose();
                Image_BASLER = null;
                isgrabed = false;
                camera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.SingleFrame);
                camera.StreamGrabber.Start(1, GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
                return true;
            }
            catch (Exception)
            {
                Thread.Sleep(1000);
                GC.Collect();
                return false;
            }
        }

        
    }
}