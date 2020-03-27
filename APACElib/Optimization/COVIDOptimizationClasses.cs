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
}
