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
        protected double Penalty { get; }  // penalty factor when parameter values are out of range
        public static int NOfPolicyParameters { get;  set; } // number of parameters of this policy
        public Vector<double> StatusQuoParamValues { get; protected set; } // parameter values under the status quo

        public Policy(double penalty) {
            Penalty = penalty;
        }

        public abstract double UpdateParameters(Vector<double> paramValues, double wtp, bool checkFeasibility = true);
        /// <returns> tau: prevalence threshold to switch </returns>
        public abstract double GetTau(double wtp);
        /// <returns> theta: threshold of change in prevalence to switch </returns>
        public abstract double GetTheta(double wtp);
        /// <returns> text of policy params </returns>
        public virtual string GetParams() { return ""; }
        /// <returns> [tau, theta] </returns>
        public double[] GetTauAndTheta(double wtp)
        {
            double tau = GetTau(wtp);
            double theta = GetTheta(wtp);
            return new double[2] { tau, theta };
        }

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
        protected double EnsureLessThan (ref double value, double upperBound)
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

    public class PolicyPoint : Policy
    {
        /// <summary>
        /// prevalence threshold:            tau 
        /// change in prevalence threshold:  theta 
        /// </summary>

        const double MAX_THRESHOLD = 0.5;
        private double _tau;
        private double _theta;
        public double Tau { get => _tau; }
        public double Theta { get => _theta; }

        public PolicyPoint(double penalty) : base(penalty)
        {
            NOfPolicyParameters = 2;
            StatusQuoParamValues = Vector<double>.Build.Dense(new double[2] { 0.05, 0.05 });
        }

        public override double UpdateParameters(Vector<double> paramValues, double wtp, bool checkFeasibility = true)
        {
            double accumPenalty = 0;
            _tau = paramValues[0];
            _theta = paramValues[1];

            if (checkFeasibility)
            {
                accumPenalty += base.EnsureFeasibility(ref _tau, 0, MAX_THRESHOLD);
                accumPenalty += base.EnsureFeasibility(ref _theta, 0, MAX_THRESHOLD);
                accumPenalty += base.EnsureLessThan(ref _theta, _tau);
            }
            return accumPenalty;
        }

        public override string GetParams()
        {
            return _tau.ToString() + ',' + _theta.ToString();
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
        /// change in prevalence threshold:  theta(wtp) = tau(wtp) * rho(wtp)
        /// parameter values = [tau0, tau1, rho0, rho 1]
        /// </summary>

        enum Par { tau0, tau1, rho0, rho1}

        private double[] _paramValues;
        //public double[] TauParams { get => _paramValues.SubVector(0, 2).ToArray(); } // prevalence threshold
        //public double[] RhoParams { get => _paramValues.SubVector(2, 2).ToArray(); } // multiplier to get the threshold for change in prevalence

        public PolicyExponential(double penalty) : base(penalty)
        {
            NOfPolicyParameters = 3;
            // status quo parameter values 
            _paramValues = new double[3] { 0.05, 0, 1};
            StatusQuoParamValues = Vector<double>.Build.Dense(_paramValues);
        }

        public override double UpdateParameters(Vector<double> paramValues, double wtp, bool checkFeasibility = true)
        {
            _paramValues = paramValues.ToArray();

            double accumPenalty = 0;
            if (checkFeasibility)
            {
                // accumPenalty += base.EnsureFeasibility(ref _tauParams[0], 0, MAX_THRESHOLD);

                // tau0 should be greater than 0
                accumPenalty += base.EnsureGreaterThan(ref _paramValues[(int)Par.tau0], 0);
                // tau1 should be less than 0
                accumPenalty += base.EnsureLessThan(ref _paramValues[(int)Par.tau1], 0);
                // rho0 should be between 0 and 1
                accumPenalty += base.EnsureFeasibility(ref _paramValues[(int)Par.rho0], 0, 1);
                //// ensure 0 <= rho(w) <= 1  =>   0 <= rho0 + rho1 * wtp <= 1
                //accumPenalty += base.EnsureFeasibility(
                //    value: ref _paramValues[(int)Par.rho1], 
                //    min: -_paramValues[(int)Par.rho0] / wtp, 
                //    max: (1 - _paramValues[(int)Par.rho0]) / wtp);
            }

            return accumPenalty;
        }

        public override double GetTau(double wtp)
        {
            return _paramValues[(int)Par.tau0] * Math.Exp(wtp * _paramValues[(int)Par.tau1]);
        }
        private double GetRho(double wtp)
        {
            return Math.Min(Math.Max(_paramValues[(int)Par.rho0] + _paramValues[(int)Par.rho1] * wtp, 0), 1);
        }
        public override double GetTheta(double wtp)
        {
            return  GetTau(wtp) * _paramValues[(int)Par.rho0]; // GetRho(wtp) *
        }
    }

    public class PolicyPower : Policy
    {
        // *** should be updated ***
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
            NOfPolicyParameters = 4;
            StatusQuoParamValues = Vector<double>.Build.Dense(new double[4] { 0.05, 0, 1, 0 });
            _tauParams = new double[2];
            _rhoParams = new double[2];
        }

        public override double UpdateParameters(Vector<double> paramValues, double wtp, bool checkFeasibility = true)
        {
            double accumPenalty = 0;
            _tauParams = paramValues.SubVector(0, 2).ToArray();
            _rhoParams = paramValues.SubVector(2, 2).ToArray();

            if (checkFeasibility)
            {
                accumPenalty += base.EnsureFeasibility(ref _tauParams[0], 0, MAX_THRESHOLD);
                accumPenalty += base.EnsureFeasibility(ref _tauParams[1], double.MinValue, 0);
                accumPenalty += base.EnsureFeasibility(ref _rhoParams[0], 0, 1);
                accumPenalty += base.EnsureLessThan(ref _rhoParams[1], (1- _rhoParams[0])/wtp);
                accumPenalty += base.EnsureGreaterThan(ref _rhoParams[1], -_rhoParams[0] / wtp);
            }

            return accumPenalty;            
        }

        public override string GetParams()
        {
            return _tauParams.ToString() + ',' + _rhoParams.ToString();
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

    public class GonorrheaSimModel : SimModel
    {
        /// <summary>
        /// this will be used by the stochatic approximation algorithm to evalaute structured policies
        /// </summary>
        
        private int _seed; // seed will be used to reset the seed of this epidemic modeller 
                           // at iteration 0 of the stochastic approximation algorithm
        private double[] _wtps; // wtp values at which the assigned structured policy should be evaluated
        private double[] _fValues; // sampled simulation outcomes: 
                                   // index = 0 -> fstatus quo, 
                                   // index = 1 -> f(current value of policy parameters)
                                   // index > 1 -> Df()
        private Vector<double> _DfValues; // Df at the current value of policy parameters 

        public Policy Policy { get; private set; }
        public EpidemicModeller EpiModeller { get; private set; } // epi modeller to estimate derivatives of f

        public GonorrheaSimModel(int id, ExcelInterface excelInterface, ModelSettings modelSets, 
            List<ModelInstruction> listModelInstr, double[] wtps, Policy policy)
        {
            _seed = id;
            Policy = policy;
            _wtps = wtps;

            // epi modeller to calcualte f and derivatives
            // number of epidemics = (number of wtp values) * M 
            // M = 1 + 1 + 2*(number of policy parameters), 1 for status quo, 1 for f(x), and 2*(n of policy params) for derivatives
            EpiModeller = new EpidemicModeller(
                ID: id, 
                excelInterface: excelInterface, 
                modelSettings: modelSets,
                listModelInstr: listModelInstr
                ); 
            EpiModeller.BuildEpidemics();
        }

        /// <param name="x"> x[0:1]: threshold to switch, x[2:3]: change in prevalence to switch  </param>
        public override void Sample_f_and_Df(Vector<double> x, double derivative_step, Vector<double> xScale, bool ifResampleSeeds = true)
        {
            int epi_i = 0;

            // derivative of f at x
            _DfValues = Vector<double>.Build.Dense(x.Count());

            // build epsilon matrix
            Matrix<double> epsilonMatrix = Matrix<double>.Build.DenseDiagonal(x.Count(), derivative_step);

            // find x-values to calculate f(status quo), f(current policy), and Df(current policy)
            List<Vector<double>> xValues = new List<Vector<double>>();
            // x for status quo
            xValues.Add(Policy.StatusQuoParamValues);
            // x for current policy
            xValues.Add(x);
            // x for derivatives at current policy
            for (int x_i = 0; x_i < Policy.NOfPolicyParameters; x_i++)
            {
                xValues.Add(x - epsilonMatrix.Row(x_i) * xScale[x_i]);
                xValues.Add(x + epsilonMatrix.Row(x_i) * xScale[x_i]);
            }

            // update the policy of all epidemics and 
            // record the penalty if a policy parameter is out of the feasible range
            epi_i = 0;
            _fValues = new double[xValues.Count()];           
            for (int i = 0; i < xValues.Count(); i++)
            {
                foreach (int wtp in _wtps)
                {
                    // update the policy parameters and record the penalty if any
                    // it multiplies the penalty by (wtp + 1) to account for the level of wtp in calcualting the penalty
                    _fValues[i] += (wtp + 1) * Policy.UpdateParameters(
                        paramValues: xValues[i],
                        wtp: wtp, 
                        checkFeasibility: i != 0);

                    // find thresholds (tau and theta) for this wtp
                    double[] t = Policy.GetTauAndTheta(wtp);

                    // update the threshold for the epidemic at index epi_i
                    for (int conditionIndx = 0; conditionIndx < 6; conditionIndx++)
                        ((Condition_OnFeatures)EpiModeller.Epidemics[epi_i].DecisionMaker.Conditions[conditionIndx])
                            .UpdateThresholds(t);

                    epi_i++;
                }
            }

            // resample the seeds 
            EpiModeller.AssignInitialSeeds();
            // make sure all epidemics have the same seed
            foreach (Epidemic epi in EpiModeller.Epidemics)
                epi.InitialSeed = EpiModeller.Epidemics[0].InitialSeed;

            // simulate all epidemics 
            // without resampling seeds (seeds are already assigned above, assumed to be the same for all epidemics
            EpiModeller.SimulateEpidemics(ifResampleSeeds: false);

            // update f values
            epi_i = 0;
            for (int i = 0; i < xValues.Count(); i++)
            {
                // store net monetary values 
                foreach (int wtp in _wtps)
                {
                    _fValues[i] += 
                        wtp * EpiModeller.Epidemics[epi_i].EpidemicCostHealth.TotalDiscountedDALY
                        + EpiModeller.Epidemics[epi_i].EpidemicCostHealth.TotalDisountedCost;
                    epi_i++;
                }

                // store f values (average over wpt values)
                // index = 0 is the status quoe
                if (i == 0)
                    _fValues[i] = _fValues[i] / _wtps.Count();
                else
                    _fValues[i] = _fValues[i] / _wtps.Count() - _fValues[0];
            }

            // calculate derivatives
            for (int x_i = 0; x_i < x.Count; x_i++)
            {
                _DfValues[x_i] = (_fValues[2 * x_i + 3] - _fValues[2 * x_i + 2]) / (2 * derivative_step * xScale[x_i]);
            }
        }

        public override double Get_f()
        {
            // f(current x) is stored at index 1
            return _fValues[1];
        }
        public override Vector<double> Get_Df()
        {
            return _DfValues;
        }

        public override void ResetSeedAtItr0()
        {
            if (!(EpiModeller is null))
                EpiModeller.ResetRNG(seed: _seed);
        }
    }

    public abstract class OptimizeGonohrrea
    {
        public string OptimalParamValues { get; set; }
        protected int NUM_OF_THRESHOLDS { get; } = 2;
        public string[,] Summary { get; protected set; }
    }

    public class OptimizeGonohrrea_StructuredPolicy : OptimizeGonohrrea
    {                
        public void Run(ExcelInterface excelInterface, ModelSettings modelSets, List<ModelInstruction> listModelInstr)
        {
            //if (listModelInstr == null)
            //{
            //    listModelInstr = new List<ModelInstruction>();
            //    for (int i = 0; i < modelSets.GetNumModelsToBuild(); i++)
            //        listModelInstr.Add(new ModelInstruction());
            //}

            // initial values of policy parameters 
            // (this could be different from or the same as the status quo parameters)
            double[] arrX0 = modelSets.OptmzSets.X0;
            Vector<double> x0 = Vector<double>.Build.DenseOfArray(arrX0);

            // scale of policy parameters
            double[] arrXScale = modelSets.OptmzSets.XScale;
            Vector<double> xScale = Vector<double>.Build.DenseOfArray(arrXScale);

            // find wtps 
            List<double> wtps = new List<double>();
            foreach (double wtp in modelSets.OptmzSets.WTPs)
                wtps.Add(wtp);
                
            // build epidemic models to evaluate structured policies 
            // build as many as a0's * b's * c0's
            int epiID = 0;
            List<SimModel> epiModels = new List<SimModel>();
            foreach (double a0 in modelSets.OptmzSets.StepSize_GH_a0s)
                foreach (double b in modelSets.OptmzSets.StepSize_GH_bs)
                    foreach (double c0 in modelSets.OptmzSets.DerivativeStep_cs)
                        epiModels.Add(
                            new GonorrheaSimModel(
                                id: epiID++, 
                                excelInterface: excelInterface, 
                                modelSets: modelSets, 
                                listModelInstr: listModelInstr,
                                wtps: wtps.ToArray(), 
                                policy: new PolicyExponential(modelSets.OptmzSets.Penalty))
                            );

            // create a multi stochastic approximation object            // it runs the optimization algorithm for all combinations of (a0's, b's, c0's)
            MultipleStochasticApproximation multOptimizer = new MultipleStochasticApproximation(
                simModels: epiModels,
                stepSizeGH_a0s: modelSets.OptmzSets.StepSize_GH_a0s,
                stepSizeGH_bs: modelSets.OptmzSets.StepSize_GH_bs,
                stepSizeDf_cs: modelSets.OptmzSets.DerivativeStep_cs
                );

            // minimize 
            multOptimizer.Minimize(
                nItrs: modelSets.OptmzSets.NOfItrs,
                nLastItrsToAve: modelSets.OptmzSets.NOfLastItrsToAverage,
                x0: x0,
                xScale: xScale,
                ifParallel: false, // combinations of (a0's, b's, c0's) will run sequentially 
                modelProvidesDerivatives: true 
                );

            // export results
            if (modelSets.OptmzSets.IfExportResults)
                multOptimizer.ExportResultsToCSV("");

            // store the summary of the optimization
            Summary = multOptimizer.GetSummary(f_digits:1, x_digits:4);

            //// store results
            //PolicyExponential policy = new PolicyExponential(modelSets.OptmzSets.Penalty);
            //((GonorrheaEpiModeller)epiModels[0]).Policy.UpdateParameters(multOptimizer.xStar, 0);

            //// optimal policy parameters
            //double[] optParam = multOptimizer.xStar.ToArray();
            //foreach (double v in optParam)
            //    OptimalParamValues = OptimalParamValues + v + ',';
            //OptimalParamValues = OptimalParamValues.Substring(0, OptimalParamValues.Length - 1);

            //foreach (double wtp in wtps)
            //{
            //    // 1 for wtp, 1 for fStar, 1 for a0, 1 for b 1 for c0
            //    double[] result = new double[NUM_OF_THRESHOLDS + 5]; 
            //    result[0] = wtp;
            //    result[1] = multOptimizer.a0Star;
            //    result[2] = multOptimizer.bStar;
            //    result[3] = multOptimizer.c0Star;
            //    result[4] = multOptimizer.fStar;                
            //    double[] t = ((GonorrheaEpiModeller)epiModels[0]).Policy.GetTauAndTheta(wtp);
            //    result[5] = t[0];
            //    result[6] = t[1];
            //    Summary.Add(result);
            //}
        }
    }

    public class OptimizeGonohrrea_FixedWTPs : OptimizeGonohrrea
    {

        public void Run(ExcelInterface excelInterface, ModelSettings modelSets, List<ModelInstruction> listModelInstr)
        { 
            // initial thresholds for the initial WTP 
            double[] arrX0 = modelSets.OptmzSets.X0;
            Vector<double> x0 = Vector<double>.Build.DenseOfArray(arrX0);

            // scale of policy parameters
            double[] arrXScale = modelSets.OptmzSets.XScale;
            Vector<double> xScale = Vector<double>.Build.DenseOfArray(arrXScale);

            // for all wtp values
            int epiID = 0;
            foreach (double wtp in modelSets.OptmzSets.WTPs)
            {
                // build epidemic models                
                List<SimModel> epiModels = new List<SimModel>();
                foreach (double a0 in modelSets.OptmzSets.StepSize_GH_a0s)
                    foreach (double b in modelSets.OptmzSets.StepSize_GH_bs)
                        foreach (double c0 in modelSets.OptmzSets.DerivativeStep_cs)
                            epiModels.Add(
                                new GonorrheaSimModel(
                                    epiID++, 
                                    excelInterface, 
                                    modelSets,
                                    listModelInstr,
                                    new double[1] { wtp },
                                    new PolicyPoint(modelSets.OptmzSets.Penalty))
                                );

                // create a stochastic approximation object
                MultipleStochasticApproximation multOptimizer = new MultipleStochasticApproximation(
                    simModels: epiModels,
                    stepSizeGH_a0s: modelSets.OptmzSets.StepSize_GH_a0s,
                    stepSizeGH_bs: modelSets.OptmzSets.StepSize_GH_bs,
                    stepSizeDf_cs: modelSets.OptmzSets.DerivativeStep_cs
                    );

                // minimize 
                multOptimizer.Minimize(
                    nItrs: modelSets.OptmzSets.NOfItrs,
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

                //// store results
                //double[] result = new double[NUM_OF_THRESHOLDS + 5]; // 1 for wtp, 1 for fStar, 1 for a0, 1 for b 1 for c0
                //result[0] = wtp;
                //result[1] = multOptimizer.a0Star;
                //result[2] = multOptimizer.bStar;
                //result[3] = multOptimizer.c0Star;
                //result[4] = multOptimizer.fStar;
                //result[5] = multOptimizer.xStar[0];
                //result[6] = multOptimizer.xStar[1];
                //Summary.Add(result);
            }
        }
        
    }

}
