namespace TPU_Assembly.Class
{
    public abstract class ICameraInterface
    {
        public delegate void CaptureCompleteEventArgs(object sender, CaptureCompleteEventArgs args);

        public CameraParams cameraParams = new CameraParams();

        public abstract Bitmap OneShot_();
        public abstract bool IsOpened();
        public abstract void Stop();
        public abstract bool ReOpenCamera();
        public abstract void DestroyCamera();
        public abstract void SetPictureBox(PictureBox contorl);
        public abstract bool isContinuous();
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
