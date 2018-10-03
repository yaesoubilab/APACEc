using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputationLib;
using MathNet.Numerics.LinearAlgebra;

namespace APACElib
{
    public class GonorrheaEpiModeller : SimModel
    {
        const double PENALTY = 100000;
        private double _wtp = 0;

        public EpidemicModeller EpiModeller { get; private set; }

        public GonorrheaEpiModeller(EpidemicModeller epiModeller, double wtp)
        {
            EpiModeller = epiModeller;
            _wtp = wtp;
        }

        /// <param name="x"> x[0]: threshold to switch, x[1]: change in prevalence to switch  </param>
        /// <returns></returns>
        public override double GetAReplication(Vector<double> x)
        {
            double objValue = 0;

            // make sure variables are in feasible range; if not, add the penalty to the objective function
            for (int i = 0; i < x.Count; i++)
            {
                if (x[i] < 0)
                {
                    objValue += PENALTY * Math.Pow(x[i], 2);
                    x[i] = 0;
                }
                else if (x[i] > 1)
                {
                    objValue += PENALTY * Math.Pow(x[i]-1, 2);
                    x[i] = 1;
                }
            }

            // update the thresholds in the epidemic modeller
            foreach (Epidemic epi in EpiModeller.Epidemics)
            {
                for (int conditionIndx = 0; conditionIndx < 6; conditionIndx ++)
                    ((Condition_OnFeatures)epi.DecisionMaker.Conditions[conditionIndx]).UpdateThresholds(x.ToArray());
            }

            // simulate
            EpiModeller.SimulateEpidemics();

            // calcualte net monetary benefit
            objValue += _wtp* EpiModeller.SimSummary.DALYStat.Mean + EpiModeller.SimSummary.CostStat.Mean;
                       
            return objValue;
        }
    }

    public class OptimizeGonohrrea
    {
        const int NUM_OF_VARIABLES = 2;

        public List<double[]> Summary = new List<double[]>();

        public void Run(EpidemicModeller epiModeller, ModelSettings modelSets)
        {
            // initial thresholds for the initial WTP 
            double[] arrX0 = modelSets.OptmzSets.X0;
            Vector<double> x0 = Vector<double>.Build.DenseOfArray(arrX0);

            for (double wtp = modelSets.OptmzSets.WTP_min; 
                wtp < modelSets.OptmzSets.WTP_max; 
                wtp += modelSets.OptmzSets.WTP_step)
            {

                // create a stochastic approximation object
                StochasticApproximation optimizer = new StochasticApproximation(
                    simModel: new GonorrheaEpiModeller(epiModeller, wtp),
                    derivativeStep: modelSets.OptmzSets.DerivativeStep,
                    stepSize: new StepSize(a: modelSets.OptmzSets.StepSize_a)
                    );
                
                // minimize
                optimizer.Minimize(
                    maxItrs: modelSets.OptmzSets.NOfIterations, 
                    x0: x0);

                // export results
                if (modelSets.OptmzSets.IfExportResults)
                    optimizer.ExportResultsToCSV("wtp-" + wtp + ".csv");

                // use this xStar as the intial variable for the next wtp
                x0 = optimizer.XStar;

                // store results
                double[] result = new double[NUM_OF_VARIABLES+ 1];
                result[0] = wtp;
                result[1] = optimizer.XStar[0];
                result[2] = optimizer.XStar[1];
                Summary.Add(result);
            }
        }

        public double[,] GetSummary()
        {
            double[,] results = new double[Summary.Count, NUM_OF_VARIABLES + 1];

            for (int i = 0; i < Summary.Count; i++)
                for (int j = 0; j < NUM_OF_VARIABLES + 1; j++)
                    results[i, j] = Summary[i][j];

            return results;
        }
    }

}
