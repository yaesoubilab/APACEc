using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APACElib;

namespace RunSIR
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

            List<ModelInstruction> SIRModels = new List<ModelInstruction>();
            for (int i = 0; i < myAPACE.ModelSetting.GetNumModelsToBuild(); i++)
                SIRModels.Add(new SIRModel());
            
            // run apace
            myAPACE.Run(SIRModels);

            if (args.Length > 0 && args[0] == "true")
            {
                Console.WriteLine("Saving...");
                myAPACE.ExcelIntface.Save();
            }

            Console.WriteLine("Completed.");

        }
    }
}
