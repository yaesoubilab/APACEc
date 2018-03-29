using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APACE
{
    public partial class Form1 : Form
    {
        APACE_lib.APACE myAPACE;

        public Form1()
        {
            InitializeComponent();

            // define the epidemic model
            myAPACE = new APACE_lib.APACE();
        }

        // connect button
        private void button1_Click(object sender, EventArgs e)
        {
            // connect to the epidemic model
            myAPACE.ConnectToExcelInteface();

            this.textBox1.Text = "Connected to " + myAPACE.ExcelFileName();
            this.textBox1.ForeColor = System.Drawing.Color.Black;
            
            this.btnRun.Enabled = true;
            this.btnShowExcel.Enabled = true;            

        }

        // Run button
        private void button2_Click(object sender, EventArgs e)
        {
            // display the status
            this.lblStatus.Text = "Status: Running...";            
            this.btnRun.Enabled = false;

            // save the excel file
            //myAPACE.SaveExcelFile();

            // check the visibility of the model
            if (this.chbIfMakeExcelFileVisible.Checked == true)
                myAPACE.MakeModelInvisible();
            else
                myAPACE.MakeModelVisible();

            // run the model
            myAPACE.Run();

            // make the model visible
            myAPACE.MakeModelVisible();

            // reset the epidemic model
            //myAPACE.RestartEpidemicModel();
            
            // save the excel file if needed
            if (this.chbifSaveExcelFile.Checked)
                myAPACE.SaveExcelFile();

            // display the status
            this.lblStatus.Text = "Status: Finished!";
            this.btnRun.Enabled = true;
        }

        private void btnShowExcel_Click(object sender, EventArgs e)
        {
            // make the model visible
            myAPACE.MakeModelVisible();
        }

    }
}
