using System;

namespace APACE
{
    partial class Form1
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
            this.btnRun = new System.Windows.Forms.Button();
            this.chbIfMakeExcelFileInVisible = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.btnShowExcel = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.chbifSaveExcelFile = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(10, 29);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(75, 23);
            this.btnConnect.TabIndex = 0;
            this.btnConnect.Text = "Connect...";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnRun
            // 
            this.btnRun.Enabled = false;
            this.btnRun.Location = new System.Drawing.Point(224, 17);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(75, 23);
            this.btnRun.TabIndex = 2;
            this.btnRun.Text = "Run...";
            this.btnRun.UseVisualStyleBackColor = true;
            this.btnRun.Click += new System.EventHandler(this.button2_Click);
            // 
            // chbIfMakeExcelFileVisible
            // 
            this.chbIfMakeExcelFileInVisible.AutoSize = true;
            this.chbIfMakeExcelFileInVisible.Location = new System.Drawing.Point(23, 172);
            this.chbIfMakeExcelFileInVisible.Name = "chbIfMakeExcelFileVisible";
            this.chbIfMakeExcelFileInVisible.Size = new System.Drawing.Size(154, 17);
            this.chbIfMakeExcelFileInVisible.TabIndex = 3;
            this.chbIfMakeExcelFileInVisible.Text = "Make spreadsheet invisible";
            this.chbIfMakeExcelFileInVisible.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.textBox1);
            this.groupBox1.Controls.Add(this.btnShowExcel);
            this.groupBox1.Controls.Add(this.btnConnect);
            this.groupBox1.Location = new System.Drawing.Point(12, 16);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(310, 102);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Connection to Excel Model";
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.SystemColors.Menu;
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox1.ForeColor = System.Drawing.Color.Red;
            this.textBox1.Location = new System.Drawing.Point(10, 58);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(290, 30);
            this.textBox1.TabIndex = 3;
            this.textBox1.Text = "Connection not established!";
            // 
            // btnShowExcel
            // 
            this.btnShowExcel.Enabled = false;
            this.btnShowExcel.Location = new System.Drawing.Point(91, 29);
            this.btnShowExcel.Name = "btnShowExcel";
            this.btnShowExcel.Size = new System.Drawing.Size(75, 23);
            this.btnShowExcel.TabIndex = 2;
            this.btnShowExcel.Text = "Show";
            this.btnShowExcel.UseVisualStyleBackColor = true;
            this.btnShowExcel.Click += new System.EventHandler(this.btnShowExcel_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.chbifSaveExcelFile);
            this.groupBox2.Controls.Add(this.lblStatus);
            this.groupBox2.Controls.Add(this.btnRun);
            this.groupBox2.Location = new System.Drawing.Point(13, 130);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(309, 96);
            this.groupBox2.TabIndex = 5;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Run";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.ForeColor = System.Drawing.Color.Black;
            this.lblStatus.Location = new System.Drawing.Point(7, 67);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(40, 13);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "Status:";
            // 
            // chbifSaveExcelFile
            // 
            this.chbifSaveExcelFile.AutoSize = true;
            this.chbifSaveExcelFile.Location = new System.Drawing.Point(10, 20);
            this.chbifSaveExcelFile.Name = "chbifSaveExcelFile";
            this.chbifSaveExcelFile.Size = new System.Drawing.Size(182, 17);
            this.chbifSaveExcelFile.TabIndex = 4;
            this.chbifSaveExcelFile.Text = "Save the Excel file when finished";
            this.chbifSaveExcelFile.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(333, 240);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.chbIfMakeExcelFileInVisible);
            this.Controls.Add(this.groupBox2);
            this.Name = "Form1";
            this.Text = "APACE";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.CheckBox chbIfMakeExcelFileInVisible;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnShowExcel;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.CheckBox chbifSaveExcelFile;
    }
}

