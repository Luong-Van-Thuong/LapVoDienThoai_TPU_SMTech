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

        public readonly object _LockImage = new();

        private readonly AutoResetEvent _waitForImageEvent = new(false);

        public override bool isContinuous()
        {
            if (isContinue == true)
                return true;
            return false;
        }

        public override void SetPictureBox(PictureBox control)
        {
            this.thisControl = control;
        }

        public static ICameraInterface CAMERA1 = new BaslerCam(AoiParam.Instance.CAM1);
        public static ICameraInterface CAMERA2 = new BaslerCam(AoiParam.Instance.CAM2);
        public static ICameraInterface CAMERA3 = new BaslerCam(AoiParam.Instance.CAM3);

        public BaslerCam(string userDefinedName)
        {
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

                            camera.Parameters[PLCamera.UserSetLoad].Execute();
                        }
                    }
                }
                catch
                {
                    Thread.Sleep(300);
                    try
                    {
                        // Retry to open camera
                        camera.Open();
                        isOpened = true;

                        camera.Parameters[PLCamera.UserSetLoad].Execute();
                    }
                    catch (Exception)
                    {
                        MSystem.InsertAndSaveLogs($"Failed to open camera: {userDefinedName}", Color.Red);
                    }
                }
            }
        }
        public override bool ReOpenCamera()
        {
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

                            camera.Parameters[PLCamera.UserSetLoad].Execute();

                        }
                        MSystem.InsertAndSaveLogs($"ReOpened camera: {cameraName}", Color.Red);
                        return true;
                    }
                }
                catch
                {
                    Thread.Sleep(300);
                    try
                    {
                        camera.Open();
                        MSystem.InsertAndSaveLogs($"ReOpened camera: {cameraName}", Color.Red);
                        return true;
                    }
                    catch (Exception)
                    {
                        MSystem.InsertAndSaveLogs($"Failed to ReOpen camera: {cameraName}", Color.Red);
                        return false;
                    }
                }
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
                    if (instance == null)
                    {
                        instance = new AoiParam();
                    }
                    return instance;
                }
            }

            public string CameraName { get; set; }
            public long Exposure { get; set; }
            public long SizeW { get; set; }
            public long SizeH { get; set; }
            public long OffsetX { get; set; }
            public long OffsetY { get; set; }

            public string CAM1 = "CAMERA1";
            public string CAM2 = "CAMERA2";
            public string CAM3 = "CAMERA3";

            public List<AoiParam> AoiParams = new List<AoiParam>();
            public long NumberOfCamera = 4;

        }
        public override void Init()
        {
            for (int i = 0; i < AoiParam.Instance.NumberOfCamera; i++)
            {
                if (AoiParam.Instance.AoiParams[i].CameraName == cameraName)
                {
                    CameraParams readJsonParam = new CameraParams
                    {
                        ExposureValue = AoiParam.Instance.AoiParams[i].Exposure,
                        Width = AoiParam.Instance.AoiParams[i].SizeW,
                        Height = AoiParam.Instance.AoiParams[i].SizeH,
                        Xoffset = AoiParam.Instance.AoiParams[i].OffsetX,
                        Yoffset = AoiParam.Instance.AoiParams[i].OffsetY
                    };

                    SetParameter(readJsonParam);
                }
            }
        }
        public override bool IsOpened()
        {
            return isOpened;
        }
        
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
                ReOpenCamera();
            }
        }

        // Occurs when the connection to a camera device is opened.
        private void OnCameraOpened(Object sender, EventArgs e)
        {
            if (isContinue)
            {

                if (thisControl.InvokeRequired)
                {
                    // If called from a different thread, we must use the Invoke method to marshal the call to the proper thread.
                    thisControl.BeginInvoke(new EventHandler<EventArgs>(OnCameraOpened), sender, e);
                    return;
                }
            }
        }

        // Occurs when the connection to a camera device is closed.
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

        // Occurs when a camera starts grabbing.
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

        public override Bitmap OneShot_()
        {
            if (camera == null || !camera.IsOpen) return null;
            int maxRetries = 4;

            for (int i = 0; i < maxRetries; i++)
            {
                if (i > 0) ReOpenCamera();
                _waitForImageEvent.Reset();

                lock (_LockImage) 
                {
                    Image_BASLER?.Dispose();
                    Image_BASLER = null;
                }
                if (!OneShot())
                {
                    Thread.Sleep(100);
                    continue;
                }

                if (_waitForImageEvent.WaitOne(2500))
                {
                    if (Image_BASLER != null)
                    {
                        return Image_BASLER;
                    }
                }
                MSystem.InsertAndSaveLogs($"Retry shot: {i}", Color.Red);
            }
            return null;
        }
        private void OnImageGrabbed(Object sender, ImageGrabbedEventArgs e)
        {
            if (isContinue)
            {
                if (thisControl.InvokeRequired)
                {
                    thisControl.BeginInvoke(new EventHandler<ImageGrabbedEventArgs>(OnImageGrabbed), sender, e.Clone());
                    return;
                }
            }
            try
            {
                IGrabResult grabResult = e.GrabResult;

                if (grabResult.IsValid)
                {
                    if (!stopWatch.IsRunning || stopWatch.ElapsedMilliseconds > 120 || stopWatch.ElapsedMilliseconds > 33)
                    {
                        try
                        {
                            stopWatch.Restart();
                            Bitmap bitmap = new Bitmap(grabResult.Width, grabResult.Height, PixelFormat.Format8bppIndexed);
                            ColorPalette palette = bitmap.Palette;
                            for (int i = 0; i < 256; i++)
                                palette.Entries[i] = Color.FromArgb(i, i, i);
                            bitmap.Palette = palette;

                            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

                            converter.OutputPixelFormat = PixelType.Mono8;

                            IntPtr ptrBmp = bmpData.Scan0;
                            converter.Convert(ptrBmp, bmpData.Stride * bitmap.Height, grabResult);
                            bitmap.UnlockBits(bmpData);
                            

                            if (isContinue)
                            {
                                Bitmap bitmapOld = ((PictureBox)thisControl).Image as Bitmap;
                                ((PictureBox)thisControl).Image = bitmap;
                                bitmapOld?.Dispose();
                            }
                            else
                            {
                                if (bitmap != null)
                                {
                                    Image_BASLER?.Dispose();
                                    Image_BASLER = bitmap;
                                    _waitForImageEvent.Set();
                                    isgrabed = true;
                                }
                            }
                        }
                        catch(Exception ex) 
                        {
                            MSystem.InsertAndSaveLogs($"OnImageGrabbed error: {ex.Message}", Color.Red);
                            Thread.Sleep(100);
                        }
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
            if (isContinue)
            {
                if (thisControl.InvokeRequired)
                {
                    thisControl.BeginInvoke(new EventHandler<GrabStopEventArgs>(OnGrabStopped), sender, e);
                    return;
                }
            }

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
                if (camera != null)
                {
                    if (!camera.IsOpen)
                    {
                        camera.Open();
                        Thread.Sleep(500);
                    }

                    Image_BASLER?.Dispose();
                    Image_BASLER = null;
                    isgrabed = false;

                    camera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.SingleFrame); // SingleFrame
                    camera.StreamGrabber.Start(1, GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
                    GC.Collect();
                    return true;
                }
                else
                {
                    if (!camera.IsOpen)
                    {
                        camera.Close();
                        Thread.Sleep(1000);
                        camera.Open();
                    }
                   
                    isgrabed = false;
                    Image_BASLER?.Dispose();
                    Image_BASLER = null;

                    camera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.SingleFrame); // SingleFrame
                    camera.StreamGrabber.Start(1, GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
                    GC.Collect();
                    return true;
                }
            }
            catch (Exception)
            {
                Thread.Sleep(1000);
                GC.Collect();
                return false;
            }
        }

        public void ContinuousShot()
        {
            try
            {
                camera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.Continuous);
                camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
                isgrabed = false;
                Image_BASLER = null;
            }
            catch (Exception)
            {
            }
        }

        public override CameraParams GetParameter()
        {
            cameraParams.ExposureValue = (int)camera.Parameters[PLCamera.ExposureTimeRaw].GetValue();
            cameraParams.MinExposure = (int)camera.Parameters[PLCamera.ExposureTimeRaw].GetMinimum();
            cameraParams.MaxExposure = (int)camera.Parameters[PLCamera.ExposureTimeRaw].GetMaximum();
            cameraParams.Width = (int)camera.Parameters[PLCamera.Width].GetValue();
            cameraParams.MinWidth = (int)camera.Parameters[PLCamera.Width].GetMinimum();
            cameraParams.MaxWidth = (int)camera.Parameters[PLCamera.Width].GetMaximum();
            cameraParams.Height = (int)camera.Parameters[PLCamera.Height].GetValue();
            cameraParams.MinHeight = (int)camera.Parameters[PLCamera.Height].GetMinimum();
            cameraParams.MaxHeight = (int)camera.Parameters[PLCamera.Height].GetMaximum();
            cameraParams.Xoffset = (int)camera.Parameters[PLCamera.OffsetX].GetValue();
            cameraParams.Yoffset = (int)camera.Parameters[PLCamera.OffsetY].GetValue();
            return cameraParams;
        }

        public override bool GetGammaStatus()
        {
            return camera.Parameters[PLCamera.GammaEnable].GetValue();
        }
        public override void SetGammaValue(double value)
        {
            camera?.Parameters[PLCamera.Gamma].SetValue(value);
        }
        public override void SetGammaMode()
        {
            camera?.Parameters[PLCamera.GammaEnable].SetValue(true);
        }
        public override void StartAutoExposure()
        {
            camera?.Parameters[PLCamera.ExposureAuto].SetValue("Once");
        }

        public override void StopAutoExposure()
        {
            return;
        }

        public override int GetExposure()
        {
            return (int)camera.Parameters[PLCamera.ExposureTimeRaw].GetValue();
        }
        public override void SetLivePlay(bool isLiveMode)
        {
            if (isLiveMode)
                isContinue = true;
            else
                isContinue = false;
        }

        public override bool GetLivePlay()
        {
            return isContinue;
        }

        public override bool SetParameter(CameraParams cameraParams)
        {
            try
            {
                camera.Parameters[PLCamera.Width].SetValue(cameraParams.Width, IntegerValueCorrection.Nearest);
                Thread.Sleep(50);
                camera.Parameters[PLCamera.Height].SetValue(cameraParams.Height, IntegerValueCorrection.Nearest);
                Thread.Sleep(50);
                camera.Parameters[PLCamera.OffsetX].SetValue(cameraParams.Xoffset, IntegerValueCorrection.Nearest);
                Thread.Sleep(50);
                camera.Parameters[PLCamera.OffsetY].SetValue(cameraParams.Yoffset, IntegerValueCorrection.Nearest);
                Thread.Sleep(50);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public override bool SetExposure(int exposure)
        {
            return camera.Parameters[PLCamera.ExposureTimeRaw].TrySetValue(exposure, IntegerValueCorrection.Nearest);
        }

        public override bool SetTriggerMode(IntPtr _hDisplayWnd, IntPtr _Handle)
        {
            return true;
        }

        public override bool SetPreviewMode(IntPtr _hDisplayWnd, IntPtr _Handle)
        {
            SetLivePlay(true);
            ContinuousShot();
            return true;
        }

        public override void DisablePreviewMode(IntPtr _hDisplayWnd, IntPtr _Handle)
        {
            SetLivePlay(false);
            Stop();
        }
    }
}