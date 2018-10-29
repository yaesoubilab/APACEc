using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputationLib;
using MathNet.Numerics.LinearAlgebra;

namespace APACElib
{
    public abstract class Policy
    {
        protected int _nOfParams = 0;
        protected Vector<double> _defaultParamValues;
        protected double Penalty { get; }
        protected double _accumPenalty;

        public int NOfPolicyParameters { get => _nOfParams; }
        public Vector<double> DefaultParamValues { get => _defaultParamValues; }

        public Policy(double penalty) {
            Penalty = penalty;
        }

        public abstract double UpdateParameters(Vector<double> paramValues, double wtp, bool checkFeasibility = true);
        public abstract double GetTau(double wtp);
        public abstract double GetTheta(double wtp);
        public double[] GetTauAndTheta(double wtp)
        {
            double tau = GetTau(wtp);
            double theta = GetTheta(wtp);
            return new double[2] { tau, theta };
        }

        protected double EnsureFeasibility(ref double var, double min, double max)
        {
            double penalty = 0;
            if (var < min)
            {
                penalty += Penalty * Math.Pow(min - var, 2);
                var = min;
            }
            else if (var > max)
            {
                penalty += Penalty * Math.Pow(var - max, 2);
                var = max;
            }
            return penalty;
        }
        protected double EnsureLessThan (ref double var, double upperBound)
        {
            double penalty = 0;
            if (var > upperBound)
            {
                penalty += Penalty * Math.Pow(var - upperBound, 2);
                var = upperBound;
            }
            return penalty;
        }
        protected double EnsureGreaterThan(ref double var, double lowerBound)
        {
            double penalty = 0;
            if (var < lowerBound)
            {
                penalty += Penalty * Math.Pow(var - lowerBound, 2);
                var = lowerBound;
            }
            return penalty;
        }
    }

    public class PolicyPoint : Policy
    {
        /// <summary>
        /// prevalence threshold:            tau 
        /// change in prevalence threshold:  theta 
        /// </summary>
        /// 

        const double MAX_THRESHOLD = 0.5;
        private double _tau;
        private double _theta;
        public double Tau { get => _tau; }
        public double Theta { get => _theta; }

        public PolicyPoint(double penalty) : base(penalty)
        {
            _nOfParams = 2;
            _defaultParamValues = Vector<double>.Build.Dense(new double[2] { 0.05, 0.05 });
        }

        public override double UpdateParameters(Vector<double> paramValues, double wtp, bool checkFeasibility = true)
        {
            _accumPenalty = 0;
            _tau = paramValues[0];
            _theta = paramValues[1];

            if (checkFeasibility)
            {
                _accumPenalty += base.EnsureFeasibility(ref _tau, 0, MAX_THRESHOLD);
                _accumPenalty += base.EnsureFeasibility(ref _theta, 0, MAX_THRESHOLD);
                _accumPenalty += base.EnsureLessThan(ref _theta, _tau);
            }
            return _accumPenalty;
        }

        public override double GetTau(double wtp)
        {
            return _tau;
        }
        public override double GetTheta(double wtp)
        {
            return _theta;
        }        
    }

    public class PolicyExponential : Policy
    {
        /// <summary>
        /// prevalence threshold:            tau(wtp)   = tau0*exp(tau1 * wtp)
        /// multiplier:                      rho(wtp)   = rho0 + rho1 * wtp   
        /// change in prevalence threshold:  theta(wtp) = tau * rho(wtp)
        /// </summary>

        const double MAX_THRESHOLD = 1;
        private double[] _tauParams;
        private double[] _rhoParams;
        public double[] TauParams { get => _tauParams; } // prevalence threshold
        public double[] RhoParams { get => _rhoParams; } // multiplier to get the threshold for change in prevalence

        public PolicyExponential(double penalty) : base(penalty)
        {
            _nOfParams = 4;
            _defaultParamValues = Vector<double>.Build.Dense(new double[4] { 0.05, 0, 1, 0 });
            _tauParams = new double[2];
            _rhoParams = new double[2];
        }

        public override double UpdateParameters(Vector<double> paramValues, double wtp, bool checkFeasibility = true)
        {
            _accumPenalty = 0;
            _tauParams = paramValues.SubVector(0, 2).ToArray();
            _rhoParams = paramValues.SubVector(2, 2).ToArray();

            if (checkFeasibility)
            {
                _accumPenalty += base.EnsureFeasibility(ref _tauParams[0], 0, MAX_THRESHOLD);
                _accumPenalty += base.EnsureFeasibility(ref _tauParams[1], double.MinValue, 0);
                _accumPenalty += base.EnsureFeasibility(ref _rhoParams[0], 0, 1);
                _accumPenalty += base.EnsureLessThan(ref _rhoParams[1], (1 - _rhoParams[0]) / wtp);
                _accumPenalty += base.EnsureGreaterThan(ref _rhoParams[1], -_rhoParams[0] / wtp);
            }

            return _accumPenalty;
        }

        public override double GetTau(double wtp)
        {
            return _tauParams[0] * Math.Exp(wtp * _tauParams[1]);
        }
        private double GetRho(double wtp)
        {
            return Math.Min(Math.Max(_rhoParams[0] + _rhoParams[1] * wtp, 0), 1);
        }
        public override double GetTheta(double wtp)
        {
            return GetRho(wtp) * GetTau(wtp);
        }
    }

    public class PolicyPower : Policy
    {
        /// <summary>
        /// prevalence threshold:            tau(wtp)   = tau0*power(wtp, tau1)
        /// multiplier:                      rho(wtp)   = rho0 + rho1 * wtp   
        /// change in prevalence threshold:  theta(wtp) = tau * rho(wtp)
        /// </summary>

        const double MAX_THRESHOLD = 2;
        private double[] _tauParams;
        private double[] _rhoParams;
        public double[] TauParams { get => _tauParams; }   // prevalence threshold
        public double[] RhoParams { get => _rhoParams; } // multiplier to get the threshold for change in prevalence

        public PolicyPower(double penalty) : base(penalty)
        {
            _nOfParams = 4;
            _defaultParamValues = Vector<double>.Build.Dense(new double[4] { 0.05, 0, 1, 0 });
            _tauParams = new double[2];
            _rhoParams = new double[2];
        }

        public override double UpdateParameters(Vector<double> paramValues, double wtp, bool checkFeasibility = true)
        {
            _accumPenalty = 0;
            _tauParams = paramValues.SubVector(0, 2).ToArray();
            _rhoParams = paramValues.SubVector(2, 2).ToArray();

            if (checkFeasibility)
            {
                _accumPenalty += base.EnsureFeasibility(ref _tauParams[0], 0, MAX_THRESHOLD);
                _accumPenalty += base.EnsureFeasibility(ref _tauParams[1], double.MinValue, 0);
                _accumPenalty += base.EnsureFeasibility(ref _rhoParams[0], 0, 1);
                _accumPenalty += base.EnsureLessThan(ref _rhoParams[1], (1- _rhoParams[0])/wtp);
                _accumPenalty += base.EnsureGreaterThan(ref _rhoParams[1], -_rhoParams[0] / wtp);
            }

            return _accumPenalty;            
        }        

        public override double GetTau(double wtp)
        {
            return Math.Min(_tauParams[0] * Math.Pow(wtp, _tauParams[1]), 1);
        }
        private double GetRho(double wtp)
        {
            return Math.Min(Math.Max(_rhoParams[0] + _rhoParams[1] * wtp, 0), 1);
        }
        public override double GetTheta(double wtp)
        {
            return GetRho(wtp) * GetTau(wtp);
        }
    }

    public class GonorrheaEpiModeller : SimModel
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

        public GonorrheaEpiModeller(int id, ExcelInterface excelInterface, ModelSettings modelSets, double[] wtps, Policy policy)
        {
            Policy = policy;

            _seed = id; // rnd seed used to reset the seed of this epidemic modeller        
            _rng = new RandomVariateLib.RNG(_seed);

            // epi modeller to calcualte f and derivatives
            EpiModeller_Df = new EpidemicModeller(
                id, 
                excelInterface, 
                modelSets,
                numOfEpis: 2 + 2* Policy.NOfPolicyParameters); // 1 for base, 1 for f, and 2*nPar for derivatives

            EpiModeller_Df.BuildEpidemics();

            _wtps = wtps;
            DiscreteUniformDist = new RandomVariateLib.DiscreteUniform("wtp", 0, _wtps.Count() - 1);
        }

        /// <param name="x"> x[0:1]: threshold to switch, x[2:3]: change in prevalence to switch  </param>
        public override void Sample_f_and_Df(Vector<double> x, double derivative_step, Vector<double> xScale, bool ifResampleSeeds = true)
        {
            int i = 0;
            double wtp = 0;

            // derivative of f at x
            _DfValues = Vector<double>.Build.Dense(x.Count());

            // build epsilon matrix
            Matrix<double> epsilonMatrix = Matrix<double>.Build.DenseDiagonal(x.Count(), derivative_step);

            // find x-values to calculate Df
            List<Vector<double>> xValues = new List<Vector<double>>();
            // base scenario
            xValues.Add(Policy.DefaultParamValues);
            // current policy
            xValues.Add(x);
            for (i = 0; i < Policy.NOfPolicyParameters; i++)
            {
                xValues.Add(x - epsilonMatrix.Row(i) * xScale[i]);
                xValues.Add(x + epsilonMatrix.Row(i) * xScale[i]);
            }

            // sample wtp
            wtp = _wtps[DiscreteUniformDist.SampleDiscrete(_rng)];

            // update the thresholds in the epidemic modeller      
            i = 0;
            _fValues = new double[xValues.Count];
            foreach (Epidemic epi in EpiModeller_Df.Epidemics)
            {
                // update the policy parameters
                _fValues[i] += (wtp + 1) * Policy.UpdateParameters(xValues[i], wtp, (i!=0));

                // find thresholds
                double[] t = Policy.GetTauAndTheta(wtp);                
                for (int conditionIndx = 0; conditionIndx < 6; conditionIndx++)
                    ((Condition_OnFeatures)epi.DecisionMaker.Conditions[conditionIndx])
                        .UpdateThresholds(t);
                i++;
            }

            // seeds
            EpiModeller_Df.AssignInitialSeeds();
            foreach (Epidemic epi in EpiModeller_Df.Epidemics)
                epi.InitialSeed = EpiModeller_Df.Epidemics[0].InitialSeed;

            // simulate
            EpiModeller_Df.SimulateEpidemics(ifResampleSeeds: false);

            // update f values
            for (i = 0; i < EpiModeller_Df.Epidemics.Count(); i++)
                _fValues[i] += wtp * EpiModeller_Df.Epidemics[i].EpidemicCostHealth.TotalDiscountedDALY
                    + EpiModeller_Df.Epidemics[i].EpidemicCostHealth.TotalDisountedCost - _fValues[0];

            // calculate derivatives
            for (i = 0; i < x.Count; i++)
            {
                _DfValues[i] = (_fValues[2 * i + 3] - _fValues[2 * i + 2]) / (2 * derivative_step * xScale[i]);
            }
        }

        public override double Get_f()
        {
            return _fValues[1];
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

    public abstract class OptimizeGonohrrea
    {
        protected int NUM_OF_THRESHOLDS { get; } = 2;
        public List<double[]> Summary { get; private set; } = new List<double[]>();

        public double[,] GetSummary()
        {
            double[,] results = new double[Summary.Count, NUM_OF_THRESHOLDS + 5];

            for (int i = 0; i < Summary.Count; i++)
                for (int j = 0; j < NUM_OF_THRESHOLDS + 5; j++)
                    results[i, j] = Summary[i][j];

            return results;
        }
    }

    public class OptimizeGonohrreaRandomizedWTP : OptimizeGonohrrea
    {                
        public void Run(ExcelInterface excelInterface, ModelSettings modelSets)
        {

            // initial policy parameters 
            double[] arrX0 = modelSets.OptmzSets.X0;
            Vector<double> x0 = Vector<double>.Build.DenseOfArray(arrX0);

            // scale of policy parameters
            double[] arrXScale = modelSets.OptmzSets.XScale;
            Vector<double> xScale = Vector<double>.Build.DenseOfArray(arrXScale);

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
                            new GonorrheaEpiModeller(
                                epiID++, 
                                excelInterface, 
                                modelSets, 
                                wtps.ToArray(), 
                                new PolicyExponential(modelSets.OptmzSets.Penalty))
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
                xScale: xScale,
                ifParallel: true,
                modelProvidesDerivatives: true
                );

            // export results
            if (modelSets.OptmzSets.IfExportResults)
                multOptimizer.ExportResultsToCSV("");

            // store results
            PolicyPower policy = new PolicyPower(modelSets.OptmzSets.Penalty);
            ((GonorrheaEpiModeller)epiModels[0]).Policy.UpdateParameters(multOptimizer.xStar, 0);
            
            foreach (double wtp in wtps)
            {
                // 1 for wtp, 1 for fStar, 1 for a0, 1 for b 1 for c0
                double[] result = new double[NUM_OF_THRESHOLDS + 5]; 
                result[0] = wtp;
                result[1] = multOptimizer.a0Star;
                result[2] = multOptimizer.bStar;
                result[3] = multOptimizer.c0Star;
                result[4] = multOptimizer.fStar;                
                double[] t = ((GonorrheaEpiModeller)epiModels[0]).Policy.GetTauAndTheta(wtp);
                result[5] = t[0];
                result[6] = t[1];
                Summary.Add(result);
            }
        }
    }

    public class OptimizeGonohrreaFixedWTPs : OptimizeGonohrrea
    {

        public void Run(ExcelInterface excelInterface, ModelSettings modelSets)
        { 
            // initial thresholds for the initial WTP 
            double[] arrX0 = modelSets.OptmzSets.X0;
            Vector<double> x0 = Vector<double>.Build.DenseOfArray(arrX0);

            // scale of policy parameters
            double[] arrXScale = modelSets.OptmzSets.XScale;
            Vector<double> xScale = Vector<double>.Build.DenseOfArray(arrXScale);

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
                                new GonorrheaEpiModeller(
                                    epiID++, 
                                    excelInterface, 
                                    modelSets, 
                                    new double[1] { wtp },
                                    new PolicyPoint(modelSets.OptmzSets.Penalty))
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
                    xScale: xScale,
                    ifParallel: true,
                    modelProvidesDerivatives: true
                    );

                // export results
                if (modelSets.OptmzSets.IfExportResults)
                    multOptimizer.ExportResultsToCSV("wtp" + wtp + "-");

                // use this xStar as the intial variable for the next wtp
                x0 = multOptimizer.xStar;

                // store results
                double[] result = new double[NUM_OF_THRESHOLDS + 5]; // 1 for wtp, 1 for fStar, 1 for a0, 1 for b 1 for c0
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
        
    }

}
