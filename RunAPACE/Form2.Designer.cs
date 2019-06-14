namespace RunAPACE
{
    partial class Form2
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
            this.btnConnect = new System.Windows.Forms.Button();
            this.cmbModels = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lbConnection = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lbStatus = new System.Windows.Forms.Label();
            this.btnRun = new System.Windows.Forms.Button();
            this.chbIfSave = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtStatus = new System.Windows.Forms.RichTextBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(10, 24);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(75, 23);
            this.btnConnect.TabIndex = 0;
            this.btnConnect.Text = "Connect ...";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // cmbModels
            // 
            this.cmbModels.FormattingEnabled = true;
            this.cmbModels.Items.AddRange(new object[] {
            "Gonorrhea"});
            this.cmbModels.Location = new System.Drawing.Point(161, 23);
            this.cmbModels.Name = "cmbModels";
            this.cmbModels.Size = new System.Drawing.Size(117, 21);
            this.cmbModels.TabIndex = 1;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lbConnection);
            this.groupBox1.Controls.Add(this.btnConnect);
            this.groupBox1.Location = new System.Drawing.Point(13, 21);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(297, 103);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Connection to Excel Model";
            // 
            // lbConnection
            // 
            this.lbConnection.AutoSize = true;
            this.lbConnection.ForeColor = System.Drawing.Color.Red;
            this.lbConnection.Location = new System.Drawing.Point(14, 62);
            this.lbConnection.Name = "lbConnection";
            this.lbConnection.Size = new System.Drawing.Size(138, 13);
            this.lbConnection.TabIndex = 1;
            this.lbConnection.Text = "Connection not established!";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.txtStatus);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.chbIfSave);
            this.groupBox2.Controls.Add(this.lbStatus);
            this.groupBox2.Controls.Add(this.btnRun);
            this.groupBox2.Controls.Add(this.cmbModels);
            this.groupBox2.Location = new System.Drawing.Point(13, 140);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(297, 203);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Run";
            // 
            // lbStatus
            // 
            this.lbStatus.AutoSize = true;
            this.lbStatus.Location = new System.Drawing.Point(10, 91);
            this.lbStatus.Name = "lbStatus";
            this.lbStatus.Size = new System.Drawing.Size(40, 13);
            this.lbStatus.TabIndex = 1;
            this.lbStatus.Text = "Status:";
            this.lbStatus.Click += new System.EventHandler(this.label2_Click);
            // 
            // btnRun
            // 
            this.btnRun.Enabled = false;
            this.btnRun.Location = new System.Drawing.Point(203, 53);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(75, 23);
            this.btnRun.TabIndex = 0;
            this.btnRun.Text = "Run...";
            this.btnRun.UseVisualStyleBackColor = true;
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
            // 
            // chbIfSave
            // 
            this.chbIfSave.AutoSize = true;
            this.chbIfSave.Location = new System.Drawing.Point(10, 57);
            this.chbIfSave.Name = "chbIfSave";
            this.chbIfSave.Size = new System.Drawing.Size(185, 17);
            this.chbIfSave.TabIndex = 2;
            this.chbIfSave.Text = "Save the Excel file when finished.";
            this.chbIfSave.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(131, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Select a pre-coded model:";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // txtStatus
            // 
            this.txtStatus.Location = new System.Drawing.Point(10, 107);
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.Size = new System.Drawing.Size(268, 83);
            this.txtStatus.TabIndex = 4;
            this.txtStatus.Text = "";
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(330, 355);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "Form2";
            this.Text = "Form2";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.ComboBox cmbModels;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lbConnection;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label lbStatus;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.CheckBox chbIfSave;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox txtStatus;
    }
}