using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputationLib;
using MathNet.Numerics.LinearAlgebra;

namespace APACElib.Optimization
{
    public abstract class COVIDPolicyI : Policy
    {
        protected double[] _paramValues;

        public COVIDPolicyI(double penalty) : base(penalty)
        {
        }
        public abstract double[] GetThresholdToTurnOn(double wtp);
        public abstract double[] GetThresholdToTurnOff(double wtp);
    }

    public class COVIDPolicyISingleWTP : COVIDPolicyI
    {
        /// <summary>
        /// prevalence threshold to turn on: tau0
        /// prevalence threshod to turn off: tau1
        /// parameter values = [tau0, tau1]
        /// </summary>

        enum Par { tau0, tau1 }        
        private double _maxI;

        public COVIDPolicyISingleWTP(double penalty, double maxI) : base(penalty)
        {
            NOfPolicyParameters = 2;
            _maxI = maxI;
            // status quo parameter values: 
            _paramValues = new double[2] { 100000, 0, }; // large number so that social distancing is never triggered 
            StatusQuoParamValues = Vector<double>.Build.Dense(_paramValues);
        }

        public override double UpdateParameters(Vector<double> paramValues, double wtp, bool checkFeasibility = true)
        {
            _paramValues = paramValues.ToArray();

            double accumPenalty = 0;
            if (checkFeasibility)
            {
                // tau0 should be greater than 0
                accumPenalty += base.EnsureFeasibility(ref _paramValues[(int)Par.tau0], 0, _maxI);
                // tau1 should be greater than 0
                accumPenalty += base.EnsureFeasibility(ref _paramValues[(int)Par.tau1], 0, _maxI);
            }
            return accumPenalty;
        }
        public override double[] GetThresholdToTurnOn(double wtp)
        {
            return new double[1] { _paramValues[(int)Par.tau0] / 100000 };
        }
        public override double[] GetThresholdToTurnOff(double wtp)
        {
            return new double[1] { _paramValues[(int)Par.tau1] / 100000 };
        }

    }

    public class COVIDPolicyIRangeOfWTP : COVIDPolicyI
    {
        /// <summary>
        /// prevalence threshold to turn on: tau(wtp)   = tau0*exp(tau1 * wtp)
        /// multiplier:                     rho   
        /// prevalence threshod to turn off: theta(wtp) = tau(wtp) * rho
        /// parameter values = [tau0, tau1, rho]
        /// </summary>

        enum Par { tau0, tau1, rho }
        private double _scale;

        public COVIDPolicyIRangeOfWTP(double penalty, double wtpScale) : base(penalty)
        {
            NOfPolicyParameters = 3;
            _scale = wtpScale;
            // status quo parameter values: 
            _paramValues = new double[3] { 100000, 0, 1 }; // large number so that social distancing is never triggered 
            StatusQuoParamValues = Vector<double>.Build.Dense(_paramValues);
        }

        public override double UpdateParameters(Vector<double> paramValues, double wtp, bool checkFeasibility = true)
        {
            _paramValues = paramValues.ToArray();

            double accumPenalty = 0;
            if (checkFeasibility)
            {
                // tau0 should be greater than 0
                accumPenalty += base.EnsureGreaterThan(ref _paramValues[(int)Par.tau0], 0);
                // tau1 should be less than 0
                accumPenalty += base.EnsureLessThan(ref _paramValues[(int)Par.tau1], 0);
                // r_on_0 should be between 0 and 1
                accumPenalty += base.EnsureFeasibility(ref _paramValues[(int)Par.rho], 0, 1);
            }
            return accumPenalty;
        }
        public override double[] GetThresholdToTurnOn(double wtp)
        {
            return new double[1] { _paramValues[(int)Par.tau0] * Math.Exp(wtp * _paramValues[(int)Par.tau1] / _scale) / 100000};
        }
        public  override double[] GetThresholdToTurnOff(double wtp)
        {
            return new double[1] { GetThresholdToTurnOn(wtp)[0] * _paramValues[(int)Par.rho] };
        }
    }

    public abstract class COVIDPolicyRt : Policy
    {
        
        protected double[] _paramValues;        

        public COVIDPolicyRt(double penalty) : base(penalty)
        {
        }

        public abstract double GetThresholdToTurnOff(double wtp);
        public abstract double GetThresholdToTurnOn(double wtp);
    }

    public class COVIDPolicyRtSingleWTP: COVIDPolicyRt
    {
        /// <summary>
        /// R_eff threshold under no social distancing:  t_0
        /// R_eff threshold under social distancing:     t_1
        /// parameter values = []
        /// </summary>
        ///
        private enum Par { t_0, t_1}
        private double _maxR;

        public COVIDPolicyRtSingleWTP(double penalty, double maxR): base(penalty)
        {
            NOfPolicyParameters = 2;
            _maxR = maxR;
            // status quo parameter values: 
            // R_eff threshold under no social distancing = 100 (a large number so that social distancing is never used)
            // R_eff threshold under social distancing = 0 (turn off social distancing immediately)
            _paramValues = new double[2] { 100, 0};
            StatusQuoParamValues = Vector<double>.Build.Dense(_paramValues);
        }

        public override double UpdateParameters(Vector<double> paramValues, double wtp, bool checkFeasibility = true)
        {
            _paramValues = paramValues.ToArray();

            double accumPenalty = 0;
            if (checkFeasibility)
            {
                // t_0 should be greater than 0
                accumPenalty += base.EnsureFeasibility(ref _paramValues[(int)Par.t_0], 0, _maxR);
                // t_1 should be greater than 0
                accumPenalty += base.EnsureFeasibility(ref _paramValues[(int)Par.t_1], 0, _maxR);
            }
            return accumPenalty;
        }

        public override double GetThresholdToTurnOn(double wtp)
        {
            return _paramValues[(int)Par.t_0];
        }
        public override double GetThresholdToTurnOff(double wtp)
        {
            return _paramValues[(int)Par.t_1];
        }
    }

    public class COVIDPolicyRtRangeOfWTP : COVIDPolicyRt
    {
        /// <summary>
        /// R_eff threshold under no social distancing:  t_off(wtp) = t_off_0 * exp(t_off_1 * wtp)
        /// R_eff threshold under social distancing:     t_on(wtp)  = r_on_0 * t_off(wtp)
        /// parameter values = [t_off_0, t_off_1, t_on_0]
        /// </summary>
        /// 
        private enum Par { t_off_0, t_off_1, r_on_0 }
        private double _scale;
        private double _maxRatioOfROnToROff;

        public COVIDPolicyRtRangeOfWTP(double penalty, double wtpScale, double maxRatioOfROnToROff) : base(penalty)
        {
            NOfPolicyParameters = 3;
            _scale = wtpScale;
            _maxRatioOfROnToROff = maxRatioOfROnToROff;
            // status quo parameter values: 
            // R_eff threshold under no social distancing = 100 (a large number so that social distancing is never used)
            // R_eff threshold under social distancing = 0 (turn off social distancing immediately)
            _paramValues = new double[3] { 100, 0, 1 };
            StatusQuoParamValues = Vector<double>.Build.Dense(_paramValues);
        }
        public override double UpdateParameters(Vector<double> paramValues, double wtp, bool checkFeasibility = true)
        {
            _paramValues = paramValues.ToArray();

            double accumPenalty = 0;
            if (checkFeasibility)
            {
                // t_off_0 should be greater than 0
                accumPenalty += base.EnsureGreaterThan(ref _paramValues[(int)Par.t_off_0], 0);
                // t_off_1 should be less than 0
                accumPenalty += base.EnsureLessThan(ref _paramValues[(int)Par.t_off_1], 0);
                // r_on_0 should be between 0 and 1
                accumPenalty += base.EnsureFeasibility(ref _paramValues[(int)Par.r_on_0], 0, _maxRatioOfROnToROff);


                //// t_on should be less than 5 at any wtp value
                //accumPenalty += base.EnsureLessThan(ref _paramValues[(int)Par.t_off_0], 5 / Math.Exp(wtp * _paramValues[(int)Par.t_off_1]));

                //// t_off should be greateer than t_on 
                //accumPenalty += base.EnsureLessThan(ref _paramValues[(int)Par.t_on_0], _paramValues[(int)Par.t_off_0]);

                //accumPenalty += base.EnsureLessThan(
                //    ref _paramValues[(int)Par.t_on_1],
                //    _paramValues[(int)Par.t_off_1] + (1/wtp) * Math.Log(_paramValues[(int)Par.t_off_0]/ _paramValues[(int)Par.t_on_0])
                //    );

                //if (_paramValues[(int)Par.t_on_1] > 0 )
                //    throw new System.ArgumentException("Parameter cannot be null", "original");
            }
            return accumPenalty;
        }

        public override double GetThresholdToTurnOn(double wtp)
        {
            return _paramValues[(int)Par.t_off_0] * Math.Exp(wtp * _paramValues[(int)Par.t_off_1] / _scale);
        }
        public override double GetThresholdToTurnOff(double wtp)
        {
            return GetThresholdToTurnOn(wtp) * _paramValues[(int)Par.r_on_0];
        }
    }

    public class COVIDPolicyRtISingleWTP : Policy
    {
        /// <summary>
        /// prevalence threshold to turn on: tau0
        /// prevalence threshod to turn off: tau1
        /// parameter values = [tau0, tau1]
        /// </summary>

        enum Par { R, I }
        private double[] _paramValues;
        private double _maxR;
        private double _maxI;        

        public COVIDPolicyRtISingleWTP(double penalty, double maxR, double maxI) : base(penalty)
        {
            NOfPolicyParameters = 2;
            _maxR = maxR;
            _maxI = maxI;
            // status quo parameter values: 
            _paramValues = new double[2] { 10, 100000 }; // large number so that social distancing is never triggered 
            StatusQuoParamValues = Vector<double>.Build.Dense(_paramValues);
        }

        public override double UpdateParameters(Vector<double> paramValues, double wtp, bool checkFeasibility = true)
        {
            _paramValues = paramValues.ToArray();

            double accumPenalty = 0;
            if (checkFeasibility)
            {
                // R threshold
                accumPenalty += base.EnsureFeasibility(ref _paramValues[(int)Par.R], 0, _maxR);
                // I threshold
                accumPenalty += base.EnsureFeasibility(ref _paramValues[(int)Par.I], 0, _maxI);
            }
            return accumPenalty;
        }
        public double[] GetThresholdToTurnOn(double wtp)
        {
            return new double[2] { _paramValues[(int)Par.R], _paramValues[(int)Par.I] / 100000 };
        }
        public double[] GetThresholdToTurnOff(double wtp)
        {
            return new double[2] { _paramValues[(int)Par.R], _paramValues[(int)Par.I] / 100000 };
        }
    }


    public class COVIDPolicyIncAndDInc : Policy
    {
        /// <summary>
        /// incidence threshold:            tau(wtp)   = tau0*exp(tau1 * wtp)
        /// multiplier:                     rho   
        /// change in incidence threshold:  theta(wtp) = tau(wtp) * rho
        /// parameter values = [tau0, tau1, rho]
        /// </summary>

        enum Par { tau0, tau1, rho}
        private double[] _paramValues;
        private double _scale;

        public COVIDPolicyIncAndDInc(double penalty, double wtpScale) : base(penalty)
        {
            NOfPolicyParameters = 3;
            _scale = wtpScale;
            // status quo parameter values: 
            _paramValues = new double[3] { 100000, 0, 1 }; // large number so that social distancing is never triggered 
            StatusQuoParamValues = Vector<double>.Build.Dense(_paramValues);
        }

        public override double UpdateParameters(Vector<double> paramValues, double wtp, bool checkFeasibility = true)
        {
            _paramValues = paramValues.ToArray();

            double accumPenalty = 0;
            if (checkFeasibility)
            {
                // tau0 should be greater than 0
                accumPenalty += base.EnsureGreaterThan(ref _paramValues[(int)Par.tau0], 0);
                // tau1 should be less than 0
                accumPenalty += base.EnsureLessThan(ref _paramValues[(int)Par.tau1], 0);
                // r_on_0 should be between 0 and 1
                accumPenalty += base.EnsureFeasibility(ref _paramValues[(int)Par.rho], 0, 1);
            }
            return accumPenalty;
        }
        public double GetThresholdForIncidence(double wtp)
        {
            return _paramValues[(int)Par.tau0] * Math.Exp(wtp * _paramValues[(int)Par.tau1] / _scale);
        }
        public double GetThresholdForChangeInIncidence(double wtp)
        {
            return GetThresholdForIncidence(wtp) * _paramValues[(int)Par.rho];
        }
        /// <returns> [tau, theta] </returns>
        public double[] GetThresholdForIncidenceAndChangeInIncidence(double wtp)
        {
            double tau = GetThresholdForIncidence(wtp);
            double theta = GetThresholdForChangeInIncidence(wtp);
            return new double[2] { tau, theta };
        }
    }

    public class COVIDPolicyRtAndPrevRangeOfWTP : Policy
    {
        /// <summary>
        /// 
        /// R_t threshold under no social distancing:  r_off(wtp) = r_off_0 * exp(r_off_1 * wtp)
        /// R_t threshold under social distancing:     r_on(wtp)  = r_ratio * r_off(wtp),    r_ratio in [0, 1]
        /// Incidence threshold under no social distancing:  i_off(wtp) = i_off_0 * exp(i_off_1 * wtp)
        /// Incidence threshold under social distancing:     i_on(wtp)  = i_ratio * i_off(wtp),  i_ratio in [0, inf]
        /// 
        /// parameter values = [r_off_0, r_off_1, r_ratio, i_off_0, i_off_1, i_ratio]
        /// </summary>

        enum Par { r_off_0, r_off_1, r_ratio, i_off_0, i_off_1, i_ratio }
        private double[] _paramValues;
        private double _scale;
        private double _maxRatioOfROnToROff;

        public COVIDPolicyRtAndPrevRangeOfWTP(double penalty, double wtpScale, double maxRatioOfROnToROff) : base(penalty)
        {
            NOfPolicyParameters = 6;
            _scale = wtpScale;
            _maxRatioOfROnToROff = maxRatioOfROnToROff;

            // status quo parameter values: 
            _paramValues = new double[6] { 100000, 0, 1, 100000, 0, 1}; // large number so that social distancing is never triggered 
            StatusQuoParamValues = Vector<double>.Build.Dense(_paramValues);
        }

        public override double UpdateParameters(Vector<double> paramValues, double wtp, bool checkFeasibility = true)
        {
            _paramValues = paramValues.ToArray();

            double accumPenalty = 0;
            if (checkFeasibility)
            {
                // check parameters of Rt
                // r_off_0 should be greater than 1
                accumPenalty += base.EnsureGreaterThan(ref _paramValues[(int)Par.r_off_0], 1);
                // r_off_1 should be less than 0
                accumPenalty += base.EnsureLessThan(ref _paramValues[(int)Par.r_off_1], 0);
                // r_off_1 should be small enough so that r_off >= 1
                accumPenalty += base.EnsureGreaterThan(ref _paramValues[(int)Par.r_off_1], _scale * Math.Log(1/ _paramValues[(int)Par.r_off_0])/wtp);
                // r_ratio should be between 0 and 1
                accumPenalty += base.EnsureFeasibility(ref _paramValues[(int)Par.r_ratio], 0, _maxRatioOfROnToROff);

                // check parameters of %I
                // i_off_0 should be greater than 0
                accumPenalty += base.EnsureGreaterThan(ref _paramValues[(int)Par.i_off_0], 0);
                // i_off_1 should be less than 0
                accumPenalty += base.EnsureLessThan(ref _paramValues[(int)Par.i_off_1], 0);
                // i_ratio should be greater than 0
                accumPenalty += base.EnsureGreaterThan(ref _paramValues[(int)Par.i_ratio], 0);
            }
            return accumPenalty;
        }

        public double[] GetThresholdsToTurnOn(double wtp)
        {
            double r_off = _paramValues[(int)Par.r_off_0] * Math.Exp(wtp * _paramValues[(int)Par.r_off_1] / _scale);
            double i_off = _paramValues[(int)Par.i_off_0] * Math.Exp(wtp * _paramValues[(int)Par.i_off_1] / _scale)/100000;

            return new double[2] { r_off, i_off };
        }
        public double[] GetThresholdsToTurnOff(double wtp)
        {
            double[] r_off_i_off = GetThresholdsToTurnOn(wtp);
            double r_on = r_off_i_off[0] * _paramValues[(int)Par.r_ratio];
            double i_on = r_off_i_off[1] * _paramValues[(int)Par.i_ratio];

            return new double[2] { r_on, i_on };
        }
    }

    public class COVIDSimModel : SimModel
    {
        /// <summary>
        /// this will be used by the stochatic approximation algorithm to evalaute structured policies
        /// </summary>

        private int _seed; // seed will be used to reset the seed of this epidemic modeller 
                           // at iteration 0 of the stochastic approximation algorithm
        private double[] _wtps; // wtp values at which the assigned structured policy should be evaluated
        private double[] _fValues; // sampled simulation outcomes: 
                                   // index = 0 -> f(current value of policy parameters)
                                   // index > 0 -> Df()
        private Vector<double> _DfValues; // Df at the current value of policy parameters 
        private int _nSimsPerOptItr; 

        public Policy Policy { get; private set; }
        public EpidemicModeller EpiModeller { get; private set; } // epi modeller to estimate derivatives of f

        public COVIDSimModel(int id, ExcelInterface excelInterface, ModelSettings modelSets,
            List<ModelInstruction> listModelInstr, double[] wtps, Policy policy)
        {
            _seed = id;
            _nSimsPerOptItr = modelSets.OptmzSets.NOfSimsPerOptItr;
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

        /// <param name="x"> [t_off_0, t_off_1, t_on_0, t_on_1]  </param>
        public override void Sample_f_and_Df(
            Vector<double> x, double derivative_step, Vector<double> xScale, bool ifResampleSeeds = true)
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
            for (int x_i = 0; x_i < ThresholdPolicy.NOfPolicyParameters; x_i++)
            {
                xValues.Add(x - epsilonMatrix.Row(x_i) * xScale[x_i]);
                xValues.Add(x + epsilonMatrix.Row(x_i) * xScale[x_i]);
            }

            // update the policy of all epidemics and 
            // record the penalty if a policy parameter is out of the feasible range
            epi_i = 0;
            _fValues = new double[xValues.Count()];
            for (int x_index = 0; x_index < xValues.Count(); x_index++)
            {
                foreach (int wtp in _wtps)
                {
                    // update the policy parameters and record the penalty if any
                    // it multiplies the penalty by (wtp + 1) to account for the level of wtp in calcualting the penalty
                    _fValues[x_index] += Policy.UpdateParameters(  // (wtp + 1) * 
                        paramValues: xValues[x_index],
                        wtp: wtp,
                        checkFeasibility: x_index != 0);

                    // find thresholds for this wtp
                    if (Policy is COVIDPolicyRtAndPrevRangeOfWTP)
                    {
                        double[] thresholdsToOn = ((COVIDPolicyRtAndPrevRangeOfWTP)Policy).GetThresholdsToTurnOn(wtp);
                        double[] thresholdsToOff = ((COVIDPolicyRtAndPrevRangeOfWTP)Policy).GetThresholdsToTurnOff(wtp);
                        // update the threshold for the epidemic at index epi_i
                        for (int sim_i = 0; sim_i < _nSimsPerOptItr; sim_i++)
                        {
                            ((Condition_OnFeatures)EpiModeller.Epidemics[epi_i * _nSimsPerOptItr + sim_i].DecisionMaker.Conditions[6])
                                .UpdateThresholds(thresholdsToOn); 
                            ((Condition_OnFeatures)EpiModeller.Epidemics[epi_i * _nSimsPerOptItr + sim_i].DecisionMaker.Conditions[7])
                                    .UpdateThresholds(thresholdsToOff);
                        }
                    }
                    else if (Policy is COVIDPolicyRt)
                    {
                        double rToOn = ((COVIDPolicyRt)Policy).GetThresholdToTurnOn(wtp);
                        double rToOff = ((COVIDPolicyRt)Policy).GetThresholdToTurnOff(wtp);
                        // update the threshold for the epidemic at index epi_i
                        for (int sim_i = 0; sim_i < _nSimsPerOptItr; sim_i++)
                        {
                            ((Condition_OnFeatures)EpiModeller.Epidemics[epi_i * _nSimsPerOptItr + sim_i].DecisionMaker.Conditions[0])
                                .UpdateThresholds(new double[1] { rToOn }); 
                            ((Condition_OnFeatures)EpiModeller.Epidemics[epi_i * _nSimsPerOptItr + sim_i].DecisionMaker.Conditions[1])
                                    .UpdateThresholds(new double[1] { rToOff });
                        }
                    }
                    else if (Policy is COVIDPolicyI)
                    {
                        for (int sim_i = 0; sim_i < _nSimsPerOptItr; sim_i++)
                        {
                            double[] thresholdToOn = ((COVIDPolicyI)Policy).GetThresholdToTurnOn(wtp);
                            double[] thresholdToOff = ((COVIDPolicyI)Policy).GetThresholdToTurnOff(wtp);
                            ((Condition_OnFeatures)EpiModeller.Epidemics[epi_i * _nSimsPerOptItr + sim_i].DecisionMaker.Conditions[2])
                                .UpdateThresholds(thresholdToOn);
                            ((Condition_OnFeatures)EpiModeller.Epidemics[epi_i * _nSimsPerOptItr + sim_i].DecisionMaker.Conditions[3])
                                    .UpdateThresholds(thresholdToOff);
                        }
                    }   
                    else if (Policy is COVIDPolicyRtISingleWTP)
                    {
                        for (int sim_i = 0; sim_i < _nSimsPerOptItr; sim_i++)
                        {
                            double[] thresholdToOn = ((COVIDPolicyRtISingleWTP)Policy).GetThresholdToTurnOn(wtp);
                            double[] thresholdToOff = ((COVIDPolicyRtISingleWTP)Policy).GetThresholdToTurnOff(wtp);
                            ((Condition_OnFeatures)EpiModeller.Epidemics[epi_i * _nSimsPerOptItr + sim_i].DecisionMaker.Conditions[6])
                                .UpdateThresholds(thresholdToOn);
                            ((Condition_OnFeatures)EpiModeller.Epidemics[epi_i * _nSimsPerOptItr + sim_i].DecisionMaker.Conditions[7])
                                    .UpdateThresholds(thresholdToOff);
                        }
                    }

                   
                    epi_i++;
                }
            }

            // resample the seeds 
            EpiModeller.AssignInitialSeeds();
            // make sure all epidemics have the same seed
            foreach (Epidemic epi in EpiModeller.Epidemics)
                epi.InitialSeed = EpiModeller.Epidemics[epi.ID % _nSimsPerOptItr].InitialSeed;

            // simulate all epidemics 
            // without resampling seeds (seeds are already assigned above, assumed to be the same for all epidemics
            EpiModeller.SimulateEpidemics(ifResampleSeeds: false);

            // update f values
            epi_i = 0;
            for (int x_index = 0; x_index < xValues.Count(); x_index++)
            {
                // store net monetary values 
                foreach (int wtp in _wtps)
                {
                    for (int sim_i = 0; sim_i < _nSimsPerOptItr; sim_i++)
                    {
                        _fValues[x_index] +=
                        wtp * EpiModeller.Epidemics[epi_i * _nSimsPerOptItr + sim_i].EpidemicCostHealth.TotalDiscountedDALY
                        + EpiModeller.Epidemics[epi_i * _nSimsPerOptItr + sim_i].EpidemicCostHealth.TotalDisountedCost;
                    }
                    epi_i++;
                }

                // store f values (average over wpt values)
                // index = 0 is the status quoe
                if (x_index == 0)
                    _fValues[x_index] = _fValues[x_index] / _wtps.Count() / _nSimsPerOptItr;
                else
                    _fValues[x_index] = _fValues[x_index] / _wtps.Count() /_nSimsPerOptItr  - _fValues[0];
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

    public class COVIDOptimizer : Optimizer
    {
        public enum EnumPolicyType
        {
            ISingleWTP = 0,
            RtSingleWTP = 1,
            RtISingleWTP = 2,
                
        }
        public EnumPolicyType PolicyType { get; }

        public COVIDOptimizer(ExcelInterface excelInterface,
            ModelSettings modelSets, List<ModelInstruction> listModelInstr, EnumPolicyType policyType)
            : base(excelInterface, modelSets, listModelInstr) {

            PolicyType = policyType;
        }

        protected override SimModel GetASimModel(int epiID)
        {

            double scale = (ModelSets.OptmzSets.WTPs[0] + ModelSets.OptmzSets.WTPs.Last()) / 2;

            Policy policy;
            switch (PolicyType)
            {
                case EnumPolicyType.ISingleWTP:
                    policy = new COVIDPolicyISingleWTP(
                        penalty: ModelSets.OptmzSets.Penalty,
                        maxI: 25000);
                    break;

                case EnumPolicyType.RtSingleWTP:
                    policy = new COVIDPolicyRtSingleWTP(
                        penalty: ModelSets.OptmzSets.Penalty,
                        maxR: 4);
                    break;
                case EnumPolicyType.RtISingleWTP:
                    policy = new COVIDPolicyRtISingleWTP(
                        penalty: ModelSets.OptmzSets.Penalty,
                        maxR: 4, maxI: 25000);
                    break;
                default:
                    throw new System.ArgumentException("Invalid value for policy type.");
            }         

            return new COVIDSimModel(
                id: epiID,
                excelInterface: EexcelInterface,
                modelSets: ModelSets,
                listModelInstr: ListModelInstr,
                wtps: ModelSets.OptmzSets.WTPs,
                policy: policy);
                    //wtpScale: scale )
                    
        }
    }
}
