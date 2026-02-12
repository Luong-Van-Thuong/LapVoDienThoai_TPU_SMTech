using System;
using System.Drawing;
using System.Windows.Forms;

namespace TPU_Assembly.Class
{
    public class CaputreCompleteEventArgs : EventArgs
    {
        public string CameraName { get; set; }
        public Bitmap Image { get; set; }

        public CaputreCompleteEventArgs(string cameraName, Bitmap image)
        {
            CameraName = cameraName;
            Image = image;
        }
    }

    public abstract class ICameraInterface
    {
        public delegate void CaptureCompleteEventArgs(object sender, CaptureCompleteEventArgs args);

        public CameraParams cameraParams = new CameraParams();

        public abstract Bitmap OneShot_();
        public abstract void Init();
        public abstract bool IsOpened();
        public abstract void Stop();
        public abstract bool ReOpenCamera();
        public abstract void DestroyCamera();
        public abstract void SetLivePlay(bool bLiveMode);
        public abstract CameraParams GetParameter();
        public abstract bool SetParameter(CameraParams cameraParams);
        public abstract int GetExposure();
        public abstract bool SetExposure(int exposure);
        public abstract void SetPictureBox(PictureBox contorl);
        public abstract bool isContinuous();
        public abstract bool GetGammaStatus();
        public abstract void SetGammaMode();
        public abstract void SetGammaValue(double value);
        public abstract bool SetTriggerMode(IntPtr _hDisplayWnd, IntPtr _Handle);
        public abstract bool SetPreviewMode(IntPtr _hDisplayWnd, IntPtr _Handle);
        public abstract void DisablePreviewMode(IntPtr _hDisplayWnd, IntPtr _Handle);
        public abstract void StartAutoExposure();
        public abstract void StopAutoExposure();

        public abstract bool GetLivePlay();
    }

    public class CameraParams
    {
        public long ExposureValue;
        public long MinExposure;
        public long MaxExposure;

        public long Width;
        public long MinWidth;
        public long MaxWidth;

        public long Height;
        public long MinHeight;
        public long MaxHeight;

        public long Xoffset;
        public long MinXoffset;
        public long MaxXoffset;

        public long Yoffset;
        public long MinYoffset;
        public long MaxYoffset;

        public long EndX;
        public long EndY;
    }
}
