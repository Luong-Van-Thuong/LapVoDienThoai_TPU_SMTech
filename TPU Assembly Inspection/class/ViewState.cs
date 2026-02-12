using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TPU_Assembly.Class
{
    public class ViewState
    {
        public float ZoomScale { get; set; } = 1.0f;
        public PointF ImagePos { get; set; } = new PointF(0, 0);
        public Point LastMousePos { get; set; }
        public bool IsDragging { get; set; }

    }

}
