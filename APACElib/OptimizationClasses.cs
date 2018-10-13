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
        const double PENALTY = 10e10;
        private double _wtp = 0;

        public EpidemicModeller EpiModeller { get; private set; }

        public GonorrheaEpiModeller(int id, ExcelInterface excelInterface, ModelSettings modelSets, double wtp)
        {
            EpiModeller = new EpidemicModeller(id, excelInterface, modelSets, 
                numOfEpis: (int)Math.Pow(2, OptimizeGonohrrea.NUM_OF_VARIABLES));
            EpiModeller.BuildEpidemics();
            _wtp = wtp;
        }

        /// <param name="x"> x[0]: threshold to switch, x[1]: change in prevalence to switch  </param>
        /// <returns></returns>
        public override double GetAReplication(Vector<double> x, bool ifResampleSeeds)
        {
            double objValue = 0;

            // make sure variables are in feasible range; if not, add the penalty to the objective function
            objValue += MakeXFeasible(x);

            // update the thresholds in the epidemic modeller
            foreach (Epidemic epi in EpiModeller.Epidemics)
            {
                for (int conditionIndx = 0; conditionIndx < 6; conditionIndx ++)
                    ((Condition_OnFeatures)epi.DecisionMaker.Conditions[conditionIndx]).UpdateThresholds(x.ToArray());
            }

            // simulate
            EpiModeller.SimulateEpidemics(ifResampleSeeds);

            // calcualte net monetary benefit
            objValue += _wtp* EpiModeller.SimSummary.DALYStat.Mean + EpiModeller.SimSummary.CostStat.Mean;
                       
            return objValue;
        }

        public override Vector<double> GetDerivativeEstimate(Vector<double> x, double derivative_step)
        {
            // estimate the derivative of f at x
            Vector<double> Df = Vector<double>.Build.Dense(x.Count());

            // build epsilon matrix
            Matrix<double> epsilonMatrix = Matrix<double>.Build.DenseDiagonal(x.Count(), derivative_step);

            List<Vector<double>> xValues = new List<Vector<double>>();
            xValues.Add(x - epsilonMatrix.Row(0));
            xValues.Add(x + epsilonMatrix.Row(0));
            xValues.Add(x - epsilonMatrix.Row(1));
            xValues.Add(x + epsilonMatrix.Row(1));

            // update the thresholds in the epidemic modeller
            int i = 0;
            foreach (Epidemic epi in EpiModeller.Epidemics)
            {
                for (int conditionIndx = 0; conditionIndx < 6; conditionIndx++)
                    ((Condition_OnFeatures)epi.DecisionMaker.Conditions[conditionIndx])
                        .UpdateThresholds(xValues[i].ToArray());
            }

            // simulate
            EpiModeller.SimulateEpidemics(ifResampleSeeds:false);

            double[] fValues = new double[4];
            for (i = 0; i < 4; i++)
                fValues[i] = _wtp * EpiModeller.Epidemics[i].EpidemicCostHealth.TotalDiscountedDALY
                    + EpiModeller.Epidemics[i].EpidemicCostHealth.TotalDisountedCost;

            Df[0] = (fValues[1] - fValues[0]) / (2 * derivative_step);
            Df[1] = (fValues[3] - fValues[2]) / (2 * derivative_step);

            return Df;
        }

        private double MakeXFeasible(Vector<double> x)
        {
            double penalty = 0;
            // make sure variables are in feasible range; if not, add the penalty to the objective function
            for (int i = 0; i < x.Count; i++)
            {
                if (x[i] < 0)
                {
                    penalty += PENALTY * Math.Pow(x[i], 2);
                    x[i] = 0;
                }
                else if (x[i] > 0.5)
                {
                    penalty += PENALTY * Math.Pow(x[i] - 0.5, 2);
                    x[i] = 0.5;
                }
            }
            if (x[0] < x[1])
            {
                penalty += PENALTY * Math.Pow(x[1] - x[0], 2);
                x[1] = x[0];
            }

            return penalty;
        }


        public override void ResetSeedAtItr0()
        {
            EpiModeller.ResetRNG();
        }
    }

    public class OptimizeGonohrrea
    {
        public const int NUM_OF_VARIABLES = 2;

        public List<double[]> Summary = new List<double[]>();

        public void Run(ExcelInterface excelInterface, ModelSettings modelSets)
        {          

            // initial thresholds for the initial WTP 
            double[] arrX0 = modelSets.OptmzSets.X0;
            Vector<double> x0 = Vector<double>.Build.DenseOfArray(arrX0);

            // for all wtp values
            for (double wtp = modelSets.OptmzSets.WTP_min; 
                wtp <= modelSets.OptmzSets.WTP_max; 
                wtp += modelSets.OptmzSets.WTP_step)
            {

                // build epidemic models
                int epiID = 0;
                List<SimModel> epiModels = new List<SimModel>();
                foreach (double a in modelSets.OptmzSets.StepSize_as)
                    foreach (double c in modelSets.OptmzSets.DerivativeStep_cs)
                        epiModels.Add(
                            new GonorrheaEpiModeller(epiID++, excelInterface, modelSets, wtp)
                            );

                // create a stochastic approximation object
                ParallelStochasticApproximation multOptimizer = new ParallelStochasticApproximation(
                    simModels: epiModels,
                    stepSize_as: modelSets.OptmzSets.StepSize_as,
                    stepSize_cs: modelSets.OptmzSets.DerivativeStep_cs
                    );

                // minimize 
                multOptimizer.Minimize(
                    maxItrs: modelSets.OptmzSets.NOfItrs,
                    nLastItrsToAve: modelSets.OptmzSets.NOfLastItrsToAverage,
                    x0: x0,
                    ifParallel: true,
                    modelProvidesDerivatives: true
                    );

                // export results
                if (modelSets.OptmzSets.IfExportResults)
                    multOptimizer.ExportResultsToCSV("wtp" + wtp + "-");

                // use this xStar as the intial variable for the next wtp
                x0 = multOptimizer.xStar;

                // store results
                double[] result = new double[NUM_OF_VARIABLES + 3]; // 1 for wtp, 1 for fStar, 1 for a0
                result[0] = wtp;
                result[1] = multOptimizer.aStar;
                result[2] = multOptimizer.fStar;
                result[3] = multOptimizer.xStar[0];
                result[4] = multOptimizer.xStar[1];
                Summary.Add(result);
            }
        }

        public double[,] GetSummary()
        {
            double[,] results = new double[Summary.Count, NUM_OF_VARIABLES + 3];

            for (int i = 0; i < Summary.Count; i++)
                for (int j = 0; j < NUM_OF_VARIABLES + 3; j++)
                    results[i, j] = Summary[i][j];

            return results;
        }
    }

}
