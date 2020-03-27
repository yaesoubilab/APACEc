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
}
