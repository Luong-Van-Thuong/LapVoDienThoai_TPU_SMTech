using Basler.Pylon;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

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


            public string CAM1 = "CAMERA1";
            public string CAM2 = "CAMERA2";
            public string CAM3 = "CAMERA3";

            public List<AoiParam> AoiParams = new List<AoiParam>();


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
                isOpened = false;
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

        public override Bitmap OneShot_()
        {
            if (camera == null || !camera.IsOpen) return null;
            int maxRetries = 4;

            for (int i = 0; i < maxRetries; i++)
            {
                if (i > 0) ReOpenCamera();
                _waitForImageEvent.Reset();

                if (!OneShot())
                {
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