using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TPU_Assembly_Inspection_Paddle;

namespace TPU_Assembly.Class
{
    public class VisionSettingsWrapper
    {
        [Category("1. Cấu hình Lưu Ảnh")]
        [DisplayName("Lưu ảnh Gốc")]
        [Description("Có lưu ảnh gốc (Original) khi chụp xong không?")]
        public bool SaveOriginal
        {
            get { return MAINFORM.SaveImageOrigin; }
            set { MAINFORM.SaveImageOrigin = value; }
        }

        [Category("1. Cấu hình Lưu Ảnh")]
        [DisplayName("Lưu ảnh OK")]
        [Description("Có lưu ảnh khi kết quả là OK không?")]
        public bool SaveOK
        {
            get { return MAINFORM.SaveImageOK; }
            set { MAINFORM.SaveImageOK = value; }
        }

        [Category("1. Cấu hình Lưu Ảnh")]
        [DisplayName("Lưu ảnh NG")]
        [Description("Có lưu ảnh khi kết quả là NG không?")]
        public bool SaveNG
        {
            get { return MAINFORM.SaveImageNG; }
            set { MAINFORM.SaveImageNG = value; }
        }

        [Category("2. Cấu hình Log")]
        [DisplayName("Số ngày giữ Log")]
        [Description("Log cũ hơn số ngày này sẽ bị xóa.")]
        public int LogDays
        {
            get { return MAINFORM.SaveLogDays; }
            set { MAINFORM.SaveLogDays = value; }
        }


        [Category("3. Settings Model AI")]
        [DisplayName("Confidence")]
        [Description("Xác định ngưỡng chính xác của đối tượng")]
        public float ConfidenceThreshold
        {
            get { return MAINFORM.ConfidenceThreshold; }
            set { MAINFORM.ConfidenceThreshold = value; }
        }


        [Category("3. Settings Model AI")]
        [DisplayName("AutoLoadModel")]
        [Description("Tự động load model khi khởi động ứng dụng")]
        public bool AutoLoadModel
        {
            get { return MAINFORM.AutoLoadModel; }
            set { MAINFORM.AutoLoadModel = value; }
        }

        [Category("4. TCP/IP")]
        [DisplayName("IPAddress")]
        [Description("Địa chỉ IP để kết nối")]
        public string IPAddress
        {
            get { return MAINFORM.IPAddress; }
            set { MAINFORM.IPAddress = value; }
        }

        [Category("4. TCP/IP")]
        [DisplayName("Port")]
        [Description("Cổng Port để kết nối")]
        public int Port
        {
            get { return MAINFORM.Port; }
            set { MAINFORM.Port = value; }
        }
    }
}
