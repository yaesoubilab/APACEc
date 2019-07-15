using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RunAPACE
{
    public partial class Form2 : Form
    {
        APACElib.APACE myAPACE;

        public Form2()
        {
            InitializeComponent();
            // define the epidemic model
            myAPACE = new APACElib.APACE();           
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            // connect to the epidemic model
            myAPACE.ConnectToExcelInteface();

            this.lbConnection.Text = "Connected to " + myAPACE.ExcelIntface.GetFileName();
            this.lbConnection.ForeColor = System.Drawing.Color.Black;

            this.txtStatus.AppendText(DateTime.Now.ToString() + ": Connected to Excel file.");
            this.txtStatus.AppendText(System.Environment.NewLine);

            this.btnRun.Enabled = true;            
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            // display the status
            this.lbStatus.Text = "Status: Running...";
            this.btnRun.Enabled = false;

            // run the model            
            myAPACE.Run(this.cmbModels.SelectedText, this.txtStatus);

            // make the model visible
            myAPACE.ExcelIntface.Visible = true;

            // save the excel file if needed
            if (this.chbIfSave.Checked)
                myAPACE.ExcelIntface.Save();

            // display the status
            this.lbStatus.Text = "Status: Finished!";
            this.btnRun.Enabled = true;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void cmbModels_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
