using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputationLib;
using MathNet.Numerics.LinearAlgebra;

namespace APACElib.Optimization
{
    public abstract class Policy
    {
        public double Penalty { get; protected set; }  // penalty factor when parameter values are out of range
        public static int NOfPolicyParameters { get; set; } // number of parameters of this policy
        public Vector<double> StatusQuoParamValues { get; protected set; } // parameter values under the status quo

        public Policy(double penalty)
        {
            Penalty = penalty;
        }

        public abstract double UpdateParameters(Vector<double> paramValues, double wtp, bool checkFeasibility = true);
        
        /// <returns> text of policy params </returns>
        public virtual string GetParams() { return ""; }

        /// <summary>
        /// change the value to make sure it is between min and max, 
        /// and returns (penalty factor) * (value - max)^2 if value > max 
        /// or (penalty factor) * (value - min)^2 if value < min
        /// </summary>
        /// <param name="value"> current parameter value </param>
        /// <param name="min"> acceptable min </param>
        /// <param name="max"> acceptable max </param>
        /// <returns> penalty </returns>
        protected double EnsureFeasibility(ref double value, double min, double max)
        {
            double penalty = 0;
            if (value < min)
            {
                penalty += Penalty * Math.Pow(min - value, 2);
                value = min;
            }
            else if (value > max)
            {
                penalty += Penalty * Math.Pow(value - max, 2);
                value = max;
            }
            return penalty;
        }
        // see EnsureFeasibility
        protected double EnsureLessThan(ref double value, double upperBound)
        {
            double penalty = 0;
            if (value > upperBound)
            {
                penalty += Penalty * Math.Pow(value - upperBound, 2);
                value = upperBound;
            }
            return penalty;
        }
        // see EnsureFeasibility
        protected double EnsureGreaterThan(ref double value, double lowerBound)
        {
            double penalty = 0;
            if (value < lowerBound)
            {
                penalty += Penalty * Math.Pow(value - lowerBound, 2);
                value = lowerBound;
            }
            return penalty;
        }
    }

    public abstract class Optimizer
    {
        public string OptimalParamValues { get; set; }
        public string[,] Summary { get; protected set; }
        public ExcelInterface EexcelInterface { get; protected set; }
        public ModelSettings ModelSets { get; protected set; }
        public List<ModelInstruction> ListModelInstr { get; protected set; }
        public Vector<double> x0 { get; protected set; }
        public Vector<double> xScale { get; protected set; }

        public Optimizer(ExcelInterface excelInterface, ModelSettings modelSets, List<ModelInstruction> listModelInstr)
        {
            EexcelInterface = excelInterface;
            ModelSets = modelSets;
            ListModelInstr = listModelInstr;

            // initial values of policy parameters 
            // (this could be different from or the same as the status quo parameters)
            double[] arrX0 = ModelSets.SimOptmzSets.X0;
            x0 = Vector<double>.Build.DenseOfArray(arrX0);

            // scale of policy parameters
            double[] arrXScale = ModelSets.SimOptmzSets.XScale;
            xScale = Vector<double>.Build.DenseOfArray(arrXScale);
        }

        protected abstract SimModel GetASimModel(int epiID);

        public void Minimize(int digits)
        {
            // build epidemic models to evaluate structured policies 
            // build as many as a0's * b's * c0's
            int epiID = 0;
            List<SimModel> epiModels = new List<SimModel>();
            foreach (double a0 in ModelSets.SimOptmzSets.StepSize_GH_a0s)
                foreach (double b in ModelSets.SimOptmzSets.StepSize_GH_bs)
                    foreach (double c0 in ModelSets.SimOptmzSets.DerivativeStep_cs)
                        epiModels.Add(GetASimModel(epiID++));

            // create a multi stochastic approximation object            
            // it runs the optimization algorithm for all combinations of (a0's, b's, c0's)
            MultipleStochasticApproximation multOptimizer = new MultipleStochasticApproximation(
                simModels: epiModels,
                stepSizeGH_a0s: ModelSets.SimOptmzSets.StepSize_GH_a0s,
                stepSizeGH_bs: ModelSets.SimOptmzSets.StepSize_GH_bs,
                stepSizeDf_cs: ModelSets.SimOptmzSets.DerivativeStep_cs
                );

            // minimize 
            multOptimizer.Minimize(
                nItrs: ModelSets.SimOptmzSets.NOfItrs,
                nLastItrsToAve: ModelSets.SimOptmzSets.NOfLastItrsToAverage,
                x0: x0,
                xScale: xScale,
                ifParallel: false, // combinations of (a0's, b's, c0's) will run sequentially 
                modelProvidesDerivatives: true
                );

            // export results
            if (ModelSets.SimOptmzSets.IfExportResults)
                multOptimizer.ExportResultsToCSV("");

            // store the summary of the optimization
            Summary = multOptimizer.GetSummary(f_digits: 1, x_digits: digits);
        }
        
    }
}
