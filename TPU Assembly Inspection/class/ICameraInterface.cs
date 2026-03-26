namespace TPU_Assembly.Class
{
    public abstract class ICameraInterface
    {
        public delegate void CaptureCompleteEventArgs(object sender, CaptureCompleteEventArgs args);

        public abstract Bitmap OneShot_();
        public abstract bool IsOpened();
        public abstract void Stop();
        public abstract bool ReOpenCamera();
        public abstract void DestroyCamera();
        public abstract void SetPictureBox(PictureBox contorl);
        public abstract bool isContinuous();

        #region Settings
        public abstract double GetGain();
        public abstract double GetGamma();
        public abstract double GetExposureTime();
        public abstract bool SetGain(double gain);
        public abstract bool SetGamma(double gamma);
        public abstract bool SetExposureTime(double exposuretime);
        public abstract bool UserSetSave();

        #endregion


    }
}
