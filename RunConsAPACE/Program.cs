using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunConsAPACE
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {

            APACElib.APACE myAPACE;
            // define the epidemic model
            myAPACE = new APACElib.APACE();

            // connect to the epidemic model
            myAPACE.ConnectToExcelInteface();
            
            // run apace
            myAPACE.Run();

            if (args.Length > 0 && args[0] == "true")
            {
                Console.WriteLine("Saving...");
                myAPACE.ExcelIntface.Save();
            }

            Console.WriteLine("Completed.");
        }
    }
}
