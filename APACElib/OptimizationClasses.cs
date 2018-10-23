using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputationLib;
using MathNet.Numerics.LinearAlgebra;

namespace APACElib
{

    public class Policy
    {
        /// <summary>
        /// prevalence threshold:            tau = x1*power(wtp, x2)
        /// change in prevalence threshold:  theta = y1*power(wtp, y2)
        /// </summary>

        const double PENALTY = 10e9;
        const double MAX_THRESHOLD = 0.5;

        public Vector<double> TauParams { get; private set; }   // prevalence threshold
        public Vector<double> ThetaParams { get; private set; } // change in prevalence threshold

        public Policy()
        {
            TauParams = Vector<double>.Build.Dense(2);
            ThetaParams = Vector<double>.Build.Dense(2);
        }

        public double UpdateParameters(Vector<double> paramValues)
        {
            return UpdateParameters(
                tauParams: paramValues.SubVector(0, 2),
                thetaParams: paramValues.SubVector(2, 2)
                );
        }
        public double UpdateParameters(Vector<double> tauParams, Vector<double> thetaParams)
        {
            double penalty = 0;

            if (tauParams[0] < 0)
            {
                penalty += PENALTY * Math.Pow(tauParams[0], 2);
                tauParams[0] = 0;
            }
            else if (tauParams[0] > MAX_THRESHOLD)
            {
                penalty += PENALTY * Math.Pow(tauParams[0] - MAX_THRESHOLD, 2);
                tauParams[0] = MAX_THRESHOLD;
            }

            if (thetaParams[0] < 0)
            {
                penalty += PENALTY * Math.Pow(thetaParams[0], 2);
                thetaParams[0] = 0;
            }
            else if (thetaParams[0] > tauParams[0])
            {
                penalty += PENALTY * Math.Pow(thetaParams[0] - tauParams[0], 2);
                thetaParams[0] = tauParams[0];
            }

            if (tauParams[1]>0)
            {
                penalty += PENALTY * Math.Pow(tauParams[0], 2);
                tauParams[1] = 0;
            }
            if (thetaParams[1] > 0)
            {
                penalty += PENALTY * Math.Pow(thetaParams[0], 2);
                thetaParams[1] = 0;
            }

            TauParams = tauParams.Clone();
            ThetaParams = thetaParams.Clone();

            return penalty;
        }

        public double GetPrevThreshold(double wtp)
        {
            return TauParams[0] * Math.Pow(wtp, TauParams[1]);
        }

        public double GetDeltaPrevThreshold(double wtp)
        {
            return ThetaParams[0] * Math.Pow(wtp, ThetaParams[1]);
        }

        public double[] FindTauAndTheta(double wtp)
        {
            double tau = GetPrevThreshold(wtp);
            double theta = GetDeltaPrevThreshold(wtp);
            double[] t = new double[2] { tau, theta };
            return t;
        }
    }

    public class GonorrheaEpiModellerV2 : SimModel
    {
        private int _seed;
        private RandomVariateLib.DiscreteUniform DiscreteUniformDist;
        private RandomVariateLib.RNG _rng;
        private double[] _wtps;
        private double[] _fValues;
        private Vector<double> _DfValues;

        public Policy Policy { get; private set; }
        public EpidemicModeller EpiModeller_f { get; private set; } // epi modeller to estimate f
        public EpidemicModeller EpiModeller_Df { get; private set; } // epi modeller to estimate derivatives of f

        public GonorrheaEpiModellerV2(int id, ExcelInterface excelInterface, ModelSettings modelSets, double[] wtps)
        {
            Policy = new Policy();

            _seed = id; // rnd seed used to reset the seed of this epidemic modeller        
            _rng = new RandomVariateLib.RNG(_seed);

            // epi modeller to calcualte f and derivatives
            EpiModeller_Df = new EpidemicModeller(id, excelInterface, modelSets,
                numOfEpis: 1 + 2*OptimizeGonohrreaV2.NUM_OF_VARIABLES);
            EpiModeller_Df.BuildEpidemics();

            _wtps = wtps;
            DiscreteUniformDist = new RandomVariateLib.DiscreteUniform("wtp", 0, _wtps.Count() - 1);
        }

        /// <param name="x"> x[0:1]: threshold to switch, x[2:3]: change in prevalence to switch  </param>
        public override void Sample_f_and_Df(Vector<double> x, double derivative_step, bool ifResampleSeeds = true)
        {
            int i = 0;
            double wtp = 0;

            // derivative of f at x
            _DfValues = Vector<double>.Build.Dense(x.Count());

            // build epsilon matrix
            Matrix<double> epsilonMatrix = Matrix<double>.Build.DenseDiagonal(x.Count(), derivative_step);

            // find x-values to calculate Df
            List<Vector<double>> xValues = new List<Vector<double>>();
            xValues.Add(x);
            for (i = 0; i < OptimizeGonohrreaV2.NUM_OF_VARIABLES; i++)
            {
                xValues.Add(x - epsilonMatrix.Row(i));
                xValues.Add(x + epsilonMatrix.Row(i));
            }

            // sample wtp
            wtp = _wtps[DiscreteUniformDist.SampleDiscrete(_rng)];

            // update the thresholds in the epidemic modeller      
            i = 0;
            _fValues = new double[xValues.Count];
            foreach (Epidemic epi in EpiModeller_Df.Epidemics)
            {
                // update the policy parameters
                _fValues[i] += Policy.UpdateParameters(xValues[i]);

                // find thresholds
                double[] t = Policy.FindTauAndTheta(wtp);                
                for (int conditionIndx = 0; conditionIndx < 6; conditionIndx++)
                    ((Condition_OnFeatures)epi.DecisionMaker.Conditions[conditionIndx])
                        .UpdateThresholds(t);
                i++;
            }

            // seeds
            EpiModeller_Df.AssignInitialSeeds();
            foreach (Epidemic epi in EpiModeller_Df.Epidemics)
            {
                epi.InitialSeed = EpiModeller_Df.Epidemics[0].InitialSeed;
            }

            // simulate
            EpiModeller_Df.SimulateEpidemics(ifResampleSeeds: false);

            // update f values
            for (i = 0; i < EpiModeller_Df.Epidemics.Count(); i++)
                _fValues[i] += wtp * EpiModeller_Df.Epidemics[i].EpidemicCostHealth.TotalDiscountedDALY
                    + EpiModeller_Df.Epidemics[i].EpidemicCostHealth.TotalDisountedCost;

            // calculate derivatives
            for (i = 0; i < x.Count; i++)
            {
                _DfValues[i] = (_fValues[2 * i + 2] - _fValues[2 * i + 1]) / (2 * derivative_step);
            }
        }

        public override double Get_f()
        {
            return _fValues[0];
        }
        public override Vector<double> Get_Df()
        {
            return _DfValues;
        }

        public override void ResetSeedAtItr0()
        {
            if (!(EpiModeller_f is null))
                EpiModeller_f.ResetRNG(seed: _seed);
            if (!(EpiModeller_Df is null))
                EpiModeller_Df.ResetRNG(seed: _seed);
        }
    }

    public class OptimizeGonohrreaV2
    {
        public const int NUM_OF_VARIABLES = 4;
        public const int NUM_OF_THRESHOLDS = 2;

        public List<double[]> Summary = new List<double[]>();

        public void Run(ExcelInterface excelInterface, ModelSettings modelSets)
        {

            // initial policy parameters 
            double[] arrX0 = modelSets.OptmzSets.X0;
            Vector<double> x0 = Vector<double>.Build.DenseOfArray(arrX0);

            // find wtp
            List<double> wtps = new List<double>();
            for (double wtp = modelSets.OptmzSets.WTP_min;
                wtp <= modelSets.OptmzSets.WTP_max;
                wtp += modelSets.OptmzSets.WTP_step)
            {
                wtps.Add(wtp);
            }

            // build epidemic models  
            int epiID = 0;
            List<SimModel> epiModels = new List<SimModel>();
            foreach (double a0 in modelSets.OptmzSets.StepSize_GH_a0s)
                foreach (double b in modelSets.OptmzSets.StepSize_GH_bs)
                    foreach (double c0 in modelSets.OptmzSets.DerivativeStep_cs)
                        epiModels.Add(
                            new GonorrheaEpiModellerV2(epiID++, excelInterface, modelSets, wtps.ToArray())
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
                ifParallel: false,
                modelProvidesDerivatives: true
                );

            // export results
            if (modelSets.OptmzSets.IfExportResults)
                multOptimizer.ExportResultsToCSV("");

            // store results
            Policy policy = new Policy();
            policy.UpdateParameters(multOptimizer.xStar);
            
            foreach (double wtp in wtps)
            {
                // 1 for wtp, 1 for fStar, 1 for a0, 1 for b 1 for c0
                double[] result = new double[NUM_OF_THRESHOLDS + 5]; 
                result[0] = wtp;
                result[1] = multOptimizer.a0Star;
                result[2] = multOptimizer.bStar;
                result[3] = multOptimizer.c0Star;
                result[4] = multOptimizer.fStar;                
                double[] t = policy.FindTauAndTheta(wtp);
                result[5] = t[0];
                result[6] = t[1];
                Summary.Add(result);
            }
        }

        public double[,] GetSummary()
        {
            double[,] results = new double[Summary.Count, NUM_OF_THRESHOLDS + 5];

            for (int i = 0; i < Summary.Count; i++)
                for (int j = 0; j < NUM_OF_THRESHOLDS + 5; j++)
                    results[i, j] = Summary[i][j];

            return results;
        }
    }

    public class GonorrheaEpiModeller : SimModel
    {
        const double PENALTY = 10e9;
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
                foreach (double a0 in modelSets.OptmzSets.StepSize_GH_a0s)
                    foreach (double b in modelSets.OptmzSets.StepSize_GH_bs)
                        foreach (double c0 in modelSets.OptmzSets.DerivativeStep_cs)
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
                double[] result = new double[NUM_OF_VARIABLES + 5]; // 1 for wtp, 1 for fStar, 1 for a0, 1 for b 1 for c0
                result[0] = wtp;
                result[1] = multOptimizer.a0Star;
                result[2] = multOptimizer.bStar;
                result[3] = multOptimizer.c0Star;
                result[4] = multOptimizer.fStar;
                result[5] = multOptimizer.xStar[0];
                result[6] = multOptimizer.xStar[1];
                Summary.Add(result);
            }
        }

        public double[,] GetSummary()
        {
            double[,] results = new double[Summary.Count, NUM_OF_VARIABLES + 5];

            for (int i = 0; i < Summary.Count; i++)
                for (int j = 0; j < NUM_OF_VARIABLES + 5; j++)
                    results[i, j] = Summary[i][j];

            return results;
        }
    }

}
