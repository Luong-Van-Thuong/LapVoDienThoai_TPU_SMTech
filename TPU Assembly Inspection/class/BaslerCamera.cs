//using Basler.Pylon;
//using Caliburn.Micro;
//using IMGProcess.Interfaces;
//using IMGProcess.Models;
//using MvCameraControl;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Forms;
//using System.Windows.Media.Converters;

//namespace IMGProcess.Services.CameraServices
//{
//    public class BaslerCamera : ICamera
//    {
//        public Camera camera = null;
//        private PixelDataConverter converter = new PixelDataConverter();
//        public Stopwatch sw = new Stopwatch();
//        public List<ICameraInfo> _cameras = new List<ICameraInfo>();
//        public List<string> LstBaslerCamera = new List<string>();
//        private ICameraInfo selectedCamera;


//        private CameraParam cameraParam;
//        public BaslerCamera(CameraParam _cameraParam)
//        {
//            cameraParam = _cameraParam;
//            //DeviceListAcq();
//        }

//        public void DestroyCamera()
//        {
//            try
//            {
//                if (camera != null)
//                {
//                    //camera.Close();

//                    camera.CameraOpened -= Configuration.AcquireContinuous;
//                    camera.ConnectionLost -= OnConnectionLost;
//                    camera.CameraOpened -= OnCameraOpened;
//                    camera.CameraClosed -= OnCameraClosed;
//                    camera.StreamGrabber.GrabStarted -= OnGrabStarted;
//                    camera.StreamGrabber.ImageGrabbed -= OnImageGrabbed;
//                    camera.StreamGrabber.GrabStopped -= OnGrabStopped;
//                    camera.Close();
//                    camera.Dispose();
//                    camera = null;
//                }
//            }
//            catch
//            {
//                //System.Windows.MessageBox.Show(exception.ToString());
//            }
//        }

//        public List<string> DeviceListAcq()
//        {
//            try
//            {
//                LstBaslerCamera.Clear();
//                _cameras = CameraFinder.Enumerate();
                    
//                foreach (var item in _cameras)
//                {
//                    //string _cameraName = item[CameraInfoKey.FriendlyName];
//                    //LstBaslerCamera.Add(_cameraName);
//                    string cameraSN = item[CameraInfoKey.SerialNumber];
//                    if(cameraSN == cameraParam.sSerialNumber)
//                    {
//                        string _cameraName = item[CameraInfoKey.FriendlyName];
//                        LstBaslerCamera.Add(_cameraName);
//                    }
//                }
//                return LstBaslerCamera;
//            }
//            catch (Exception)
//            {
//               return null;
//            }
//        }
        
//        public void OpenDevice(int idx)
//        {
//            try
//            {
//                if (camera == null)
//                {
                    
//                    foreach (var cam in _cameras)
//                    {
//                        string cameraSN = cam[CameraInfoKey.SerialNumber];
//                        if (cameraSN == cameraParam.sSerialNumber)
//                        {
//                            selectedCamera = _cameras[idx] as ICameraInfo;
//                            if (selectedCamera != null)
//                            {
//                                camera = new Camera(selectedCamera);
//                                camera.CameraOpened += Configuration.AcquireContinuous;
//                                camera.ConnectionLost += OnConnectionLost;
//                                camera.CameraOpened += OnCameraOpened;
//                                camera.CameraClosed += OnCameraClosed;
//                                camera.StreamGrabber.GrabStarted += OnGrabStarted;
//                                camera.StreamGrabber.ImageGrabbed += OnImageGrabbed;
//                                camera.StreamGrabber.GrabStopped += OnGrabStopped;
//                                camera.Open();
//                            }
//                        }
//                        idx++;
//                    }
                    
//                    //camera.Parameters[PLCamera.PixelFormat].SetValue(PLCamera.PixelFormat.Mono8);
//                }
//                else
//                {

//                }    
//            }
//            catch (Exception)
//            {
//                System.Windows.MessageBox.Show("Open Camera Fail.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        public void RunOnce()
//        {
//            try
//            {
//                if (camera != null && camera.IsOpen)
//                {
//                    Configuration.AcquireSingleFrame(camera, null);
//                    camera.StreamGrabber.Start(1, GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
//                }
//                else
//                {
//                    CollectionLogRuntime.LogRuntimeSW.Enqueue(new SequenceLog
//                    {
//                        _SequenceLogList = $"[{DateTime.Now.ToString("yyyy MM dd - HH mm ss fff")}] : GRAB IMAGE FAIL: CAMERA NOT CONNECTED!!!",
//                        _SequenceForeLog = System.Windows.Media.Brushes.Red
//                    });
//                    //System.Windows.MessageBox.Show($"Grab Image Fail: No open cameras found!!!", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
//                }    
//            }
//            catch (Exception ex)
//            {
//                CollectionLogRuntime.LogRuntimeSW.Enqueue(new SequenceLog
//                {
//                    _SequenceLogList = $"[{DateTime.Now.ToString("yyyy MM dd - HH mm ss fff")}] : GRAB IMAGE FAIL: {ex.ToString()}",
//                    _SequenceForeLog = System.Windows.Media.Brushes.Red
//                });
//                //DestroyCamera();
//                //DeviceListAcq();
//                //OpenDevice(0);
//                //RunOnce();
//            }
//        }

//        public void RunContinuous()
//        {
//            try
//            {
//                if(camera != null && camera.IsOpen)
//                {
//                    Configuration.AcquireContinuous(camera, null);
//                    camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
//                }    
//            }
//            catch (Exception exception)
//            {
//                //CollectionLogRuntime.LogRuntimeSW.Enqueue($"RunContinuous Fail: {exception.ToString()}");
//                ShowException(exception);
//            }
//        }

//        public void Stop()
//        {
//            // Stop the grabbing.
//            try
//            {
//                if (camera != null && camera.IsOpen)
//                {
//                    camera.StreamGrabber.Stop();
//                }
//            }
//            catch (Exception exception)
//            {
//                //CollectionLogRuntime.LogRuntimeSW.Enqueue($"Stop Camera Fail: {exception.ToString()}");
//                ShowException(exception);
//            }
//        }

//        private void OnImageGrabbed(Object sender, ImageGrabbedEventArgs e)
//        {
//            try
//            {
//                IGrabResult grabResult = e.GrabResult;
//                if (grabResult.IsValid)
//                {
//                    //sw.Restart();
//                    Bitmap bitmap = new Bitmap(grabResult.Width, grabResult.Height, PixelFormat.Format32bppRgb);
//                    BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
//                    converter.OutputPixelFormat = PixelType.BGRA8packed;
//                    IntPtr ptrBmp = bmpData.Scan0;
//                    converter.Convert(ptrBmp, bmpData.Stride * bitmap.Height, grabResult);
//                    bitmap.UnlockBits(bmpData);
//                    var imageName = "IMG_" + DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss_ff") + ".bmp";
//                    ImageCallBack.Raise(bitmap, 0, sw.ElapsedMilliseconds.ToString(), "Basler", imageName);
//                }
//            }

//            catch (Exception ex)
//            {
//                CollectionLogRuntime.LogRuntimeSW.Enqueue(new SequenceLog
//                {
//                    _SequenceLogList = $"[{DateTime.Now.ToString("yyyy MM dd - HH mm ss fff")}] : ERROR ON IMAGE GRAB: {ex.ToString()}",
//                    _SequenceForeLog = System.Windows.Media.Brushes.Red
//                });
//                //ShowException(ex);
//            }
//            finally
//            {
//                e.DisposeGrabResultIfClone();
//            }
//        }

//        private void OnGrabStarted(object sender, EventArgs e)
//        {
//            if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
//            {
//                System.Windows.Application.Current.Dispatcher.Invoke(() => OnGrabStarted(sender, e));
//                return;
//            }

//            sw.Reset();
//            sw.Start();
//        }

//        private void OnCameraClosed(object sender, EventArgs e)
//        {
//            if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
//            {
//                System.Windows.Application.Current.Dispatcher.Invoke(() => OnCameraClosed(sender, e));
//                return;
//            }
//        }

//        private void OnCameraOpened(object sender, EventArgs e)
//        {
//            if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
//            {
//                System.Windows.Application.Current.Dispatcher.Invoke(() => OnCameraOpened(sender, e));
//                return;
//            }
//        }

//        private void OnConnectionLost(object sender, EventArgs e)
//        {
//            if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
//            {

//                System.Windows.Application.Current.Dispatcher.Invoke(() => OnConnectionLost(sender, e));
//                return;
//            }
//            DestroyCamera();
//            DeviceListAcq();
//            //OpenDevice(0);
//        }

//        private void OnGrabStopped(object sender, GrabStopEventArgs e)
//        {
//            sw.Stop();
//        }

//        private void ShowException(Exception exception)
//        {
//            System.Windows.MessageBox.Show("Exception caught:\n" + exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//        }
//        public void SetParams(Dictionary<string, double> _params)
//        {
//            try
//            {
//                if (camera == null || !camera.IsConnected) return;
//                camera.Parameters[PLCamera.ExposureTime].SetValue(_params["ExposureTime"]);
//                camera.Parameters[PLCamera.AcquisitionFrameRate].SetValue(_params["FrameRate"]);
//                camera.Parameters[PLCamera.Gain].SetValue(_params["Gain"]);
//                camera.Parameters[PLCamera.Gamma].SetValue(_params["Gamma"]);

//            }
//            catch (Exception ex)
//            {
//                System.Windows.MessageBox.Show("Set Camera Parameters Fail!." + ex.ToString(), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        public Dictionary<string, double> GetParams()
//        {
//            try
//            {
//                if (!camera.IsConnected) return null;
//                return new Dictionary<string, double>()
//                {
//                    ["ExposureTime"] = camera.Parameters[PLCamera.ExposureTime].GetValue(),
//                    ["FrameRate"] = camera.Parameters[PLCamera.AcquisitionFrameRate].GetValue(),
//                    ["Gain"] = camera.Parameters[PLCamera.Gain].GetValue(),
//                    ["Gamma"] = camera.Parameters[PLCamera.Gamma].GetValue()
//                };
//            }
//            catch (Exception)
//            {
//                return null;
//                //System.Windows.MessageBox.Show("Get Camera Parameters Fail!." + ex.ToString(), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        public bool IsConnect()
//        {
//            if (camera == null) return false;
//            return camera.IsConnected;
//        }

//        public void SetPixelFormat(string pixelFormat)
//        {
//            try
//            {
//                if (camera == null || !camera.IsConnected) return;
//                camera.Parameters[PLCamera.PixelFormat].SetValue(pixelFormat);
//            }
//            catch (Exception ex)
//            {
//                System.Windows.MessageBox.Show("Set Pixel Format Fail!." + ex.ToString(), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }
//        public void SetPixelFormat(uint pixelFormat)
//        {

//        }

//        public void SetDefaultParams()
//        {
            
//        }

//        public Dictionary<string, string> GetPixelTypes()
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
