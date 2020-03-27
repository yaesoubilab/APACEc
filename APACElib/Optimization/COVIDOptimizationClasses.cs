using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputationLib;
using MathNet.Numerics.LinearAlgebra;

namespace APACElib.Optimization
{
    public class COVIDAdaptivePolicy : Policy
    {
        /// <summary>
        /// R_eff threshold under no social distancing:  t_off(wtp) = t_off_0 * exp(t_off_1 * wtp)
        /// R_eff threshold under social distancing:     t_on(wtp)  = t_on_0 * exp(t_on_1 * wtp)  
        /// parameter values = [t_off_0, t_off_1, t_on_0, t_on_1]
        /// </summary>
        /// 

        enum Par { t_off_0, t_off_1, t_on_0, t_on_1 }
        private double[] _paramValues;

        public COVIDAdaptivePolicy(double penalty) : base(penalty)
        {
            NOfPolicyParameters = 4;
            // status quo parameter values: 
            // R_eff threshold under no social distancing = 100 (a large number so that social distancing is never used)
            // R_eff threshold under social distancing = 0 (turn off social distancing immediately)
            _paramValues = new double[4] { 100, 0, 1, 0 };
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
                // t_on_0 should be greater than 0
                accumPenalty += base.EnsureGreaterThan(ref _paramValues[(int)Par.t_on_0], 0);
                // t_on_1 should be less than 0
                accumPenalty += base.EnsureLessThan(ref _paramValues[(int)Par.t_on_1], 0);
            }
            return accumPenalty;
        }
        public double GetThresholdOff(double wtp)
        {
            return _paramValues[(int)Par.t_off_0] * Math.Exp(wtp * _paramValues[(int)Par.t_off_1]);
        }
        public double GetThresholdOn(double wtp)
        {
            return _paramValues[(int)Par.t_on_0] * Math.Exp(wtp * _paramValues[(int)Par.t_on_1]);
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

        public COVIDAdaptivePolicy Policy { get; private set; }
        public EpidemicModeller EpiModeller { get; private set; } // epi modeller to estimate derivatives of f

        public COVIDSimModel(int id, ExcelInterface excelInterface, ModelSettings modelSets,
            List<ModelInstruction> listModelInstr, double[] wtps, COVIDAdaptivePolicy policy)
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
                    double t_off = Policy.GetThresholdOff(wtp);
                    double t_on = Policy.GetThresholdOn(wtp);

                    // update the threshold for the epidemic at index epi_i
                    ((Condition_OnFeatures)EpiModeller.Epidemics[epi_i].DecisionMaker.Conditions[4])
                            .UpdateThresholds(new double[1] { t_on });
                    ((Condition_OnFeatures)EpiModeller.Epidemics[epi_i].DecisionMaker.Conditions[5])
                            .UpdateThresholds(new double[1] { t_off });

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
}
