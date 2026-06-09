using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TPU_Assembly_Inspection.form
{
    public partial class CreateProductModelForm : Form
    {
        public string productModelName;
        public int objectNumber;

        public CreateProductModelForm()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            productModelName = tbProductModelName.Text;
            objectNumber = (int) nupObjectNumber.Value;
            if (string.IsNullOrEmpty(productModelName)) 
            {
                MessageBox.Show("Hãy Nhập Tên Model", "Error");
                return;
            }

            this.DialogResult = DialogResult.OK; // 👈 quan trọng
            this.Close();
        }
    }
}
