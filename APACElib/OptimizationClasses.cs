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
        const double PENALTY = 10e8;
        const double MAX_THRESHOLD = 0.25;
        private int _seed;
        private double _wtp = 0;

        public EpidemicModeller EpiModeller_f { get; private set; } // epi modeller to estimate f
        public EpidemicModeller EpiModeller_Df { get; private set; } // epi modeller to estimate derivatives of f

        public GonorrheaEpiModeller(int id, ExcelInterface excelInterface, ModelSettings modelSets, double wtp)
        {
            _seed = id; // rnd seed used to reset the seed of this epidemic modeller

            // epi modeller with 1 epidemic to calcualte f(x)
            EpiModeller_f = new EpidemicModeller(id, excelInterface, modelSets, numOfEpis: 1);
            EpiModeller_f.BuildEpidemics();

            // epi modeller to calcualte derivatives
            EpiModeller_Df = new EpidemicModeller(id, excelInterface, modelSets, 
                numOfEpis: (int)Math.Pow(2, OptimizeGonohrrea.NUM_OF_VARIABLES));
            EpiModeller_Df.BuildEpidemics();

            _wtp = wtp;
        }

        /// <param name="x"> x[0]: threshold to switch, x[1]: change in prevalence to switch  </param>
        public override double GetAReplication(Vector<double> x, bool ifResampleSeeds)
        {
            double objValue = 0;

            // make sure variables are in feasible range; if not, add the penalty to the objective function
            objValue += MakeXFeasible(x);

            // update the thresholds in the epidemic modeller
            foreach (Epidemic epi in EpiModeller_f.Epidemics)
            {
                for (int conditionIndx = 0; conditionIndx < 6; conditionIndx ++)
                    ((Condition_OnFeatures)epi.DecisionMaker.Conditions[conditionIndx]).UpdateThresholds(x.ToArray());
            }

            // simulate
            EpiModeller_f.SimulateEpidemics(ifResampleSeeds);

            // calcualte net monetary benefit
            objValue += _wtp* EpiModeller_f.SimSummary.DALYStat.Mean + EpiModeller_f.SimSummary.CostStat.Mean;
                       
            return objValue;
        }

        /// <param name="x"> x[0]: threshold to switch, x[1]: change in prevalence to switch  </param>
        public override Vector<double> GetDerivativeEstimate(Vector<double> x, double derivative_step)
        {
            // estimate the derivative of f at x
            Vector<double> Df = Vector<double>.Build.Dense(x.Count());

            // build epsilon matrix
            Matrix<double> epsilonMatrix = Matrix<double>.Build.DenseDiagonal(x.Count(), derivative_step);

            // find x-values to calculate Df
            List<Vector<double>> xValues = new List<Vector<double>>();
            xValues.Add(x - epsilonMatrix.Row(0));
            xValues.Add(x + epsilonMatrix.Row(0));
            xValues.Add(x - epsilonMatrix.Row(1));
            xValues.Add(x + epsilonMatrix.Row(1));

            // penalize f when x is outside the feasible readon
            double[] fValues = new double[xValues.Count];
            int i = 0;
            for (i = 0; i < xValues.Count; i++)
            {
                fValues[i] = MakeXFeasible(xValues[i]);
            }

            // update the thresholds in the epidemic modeller      
            i = 0;
            foreach (Epidemic epi in EpiModeller_Df.Epidemics)
            {
                epi.InitialSeed = EpiModeller_f.Epidemics[0].InitialSeed;
                for (int conditionIndx = 0; conditionIndx < 6; conditionIndx++)
                    ((Condition_OnFeatures)epi.DecisionMaker.Conditions[conditionIndx])
                        .UpdateThresholds(xValues[i].ToArray());
                i++;
            }

            // simulate
            EpiModeller_Df.SimulateEpidemics(ifResampleSeeds:false);
        
            // update f values
            for (i = 0; i < 4; i++)
                fValues[i] += _wtp * EpiModeller_Df.Epidemics[i].EpidemicCostHealth.TotalDiscountedDALY
                    + EpiModeller_Df.Epidemics[i].EpidemicCostHealth.TotalDisountedCost;

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
                    penalty += _wtp*PENALTY * Math.Pow(x[i], 2);
                    x[i] = 0;
                }
                else if (x[i] > MAX_THRESHOLD)
                {
                    penalty += _wtp*PENALTY * Math.Pow(x[i] - MAX_THRESHOLD, 2);
                    x[i] = MAX_THRESHOLD;
                }
            }
            // change in prevalence should be smaller than the prevalence threshold
            if (x[0] < x[1])
            {
                penalty += _wtp*PENALTY * Math.Pow(x[1] - x[0], 2);
                x[1] = x[0];
            }

            return penalty;
        }


        public override void ResetSeedAtItr0()
        {
            EpiModeller_f.ResetRNG(seed: _seed);
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
            int epiID = 0;
            for (double wtp = modelSets.OptmzSets.WTP_min; 
                wtp <= modelSets.OptmzSets.WTP_max; 
                wtp += modelSets.OptmzSets.WTP_step)
            {

                // build epidemic models                
                List<SimModel> epiModels = new List<SimModel>();
                foreach (double a in modelSets.OptmzSets.StepSize_GH_a0s)
                    foreach (double c in modelSets.OptmzSets.DerivativeStep_cs)
                        epiModels.Add(
                            new GonorrheaEpiModeller(epiID++, excelInterface, modelSets, wtp)
                            );

                // create a stochastic approximation object
                ParallelStochasticApproximation multOptimizer = new ParallelStochasticApproximation(
                    simModels: epiModels,
                    stepSizeGH_a0s: modelSets.OptmzSets.StepSize_GH_a0s,
                    stepSizeGH_bs: modelSets.OptmzSets.StepSize_GH_bs,
                    stepSizeDf_cs: modelSets.OptmzSets.DerivativeStep_cs
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
                double[] result = new double[NUM_OF_VARIABLES + 4]; // 1 for wtp, 1 for fStar, 1 for a0, 1 for c0
                result[0] = wtp;
                result[1] = multOptimizer.a0Star;
                result[1] = multOptimizer.bStar;
                result[1] = multOptimizer.cStar;
                result[2] = multOptimizer.fStar;
                result[3] = multOptimizer.xStar[0];
                result[4] = multOptimizer.xStar[1];
                Summary.Add(result);
            }
        }

        public double[,] GetSummary()
        {
            double[,] results = new double[Summary.Count, NUM_OF_VARIABLES + 4];

            for (int i = 0; i < Summary.Count; i++)
                for (int j = 0; j < NUM_OF_VARIABLES + 4; j++)
                    results[i, j] = Summary[i][j];

            return results;
        }
    }

}
