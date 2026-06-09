namespace TPU_Assembly_Inspection.form
{
    partial class CreateProductModelForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            tbProductModelName = new TextBox();
            btnOK = new Button();
            label2 = new Label();
            nupObjectNumber = new NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)nupObjectNumber).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 19.8000011F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.Location = new Point(12, 41);
            label1.Name = "label1";
            label1.Size = new Size(109, 46);
            label1.TabIndex = 0;
            label1.Text = "Name";
            // 
            // tbProductModelName
            // 
            tbProductModelName.Font = new Font("Segoe UI", 19.8000011F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tbProductModelName.Location = new Point(127, 36);
            tbProductModelName.Name = "tbProductModelName";
            tbProductModelName.Size = new Size(379, 51);
            tbProductModelName.TabIndex = 1;
            tbProductModelName.TextAlign = HorizontalAlignment.Center;
            // 
            // btnOK
            // 
            btnOK.Location = new Point(753, 100);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(121, 61);
            btnOK.TabIndex = 2;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 19.8000011F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.Location = new Point(525, 39);
            label2.Name = "label2";
            label2.Size = new Size(252, 46);
            label2.TabIndex = 0;
            label2.Text = "Object Number";
            // 
            // nupObjectNumber
            // 
            nupObjectNumber.Font = new Font("Segoe UI", 19.8000011F, FontStyle.Regular, GraphicsUnit.Point, 0);
            nupObjectNumber.Location = new Point(793, 34);
            nupObjectNumber.Name = "nupObjectNumber";
            nupObjectNumber.Size = new Size(81, 51);
            nupObjectNumber.TabIndex = 3;
            // 
            // CreateProductModelForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(897, 173);
            Controls.Add(nupObjectNumber);
            Controls.Add(btnOK);
            Controls.Add(tbProductModelName);
            Controls.Add(label2);
            Controls.Add(label1);
            Name = "CreateProductModelForm";
            Text = "Create Product Model";
            ((System.ComponentModel.ISupportInitialize)nupObjectNumber).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox tbProductModelName;
        private Button btnOK;
        private Label label2;
        private NumericUpDown nupObjectNumber;
    }
}