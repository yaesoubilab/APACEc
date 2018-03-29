using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APACElib
{
    public class IntervalBasedStaticPolicy
    {
        int _id;
        int _interventionCombinationCode;
        double[] _timeToUseInterventions;
        int[] _numOfDecisionPointsToUseInterventions;

        public IntervalBasedStaticPolicy(int id, int interventionCombinationCode, double[] timeToUseInterventions, int[] numOfDecisionPointsToUseInterventions)
        {
            _id = id;
            _interventionCombinationCode = interventionCombinationCode;
            _timeToUseInterventions = (double[])timeToUseInterventions.Clone();
            _numOfDecisionPointsToUseInterventions = (int[])numOfDecisionPointsToUseInterventions.Clone();
        }
        public int ID
        {
            get { return _id; }
        }
        public int InterventionCombinationCode
        {
            get { return _interventionCombinationCode; }
        }
        public double[] TimeToUseInterventions
        {
            get { return _timeToUseInterventions; }
        }
        public int[] NumOfDecisionPointsToUseInterventions
        {
            get { return _numOfDecisionPointsToUseInterventions; }
        }
    }
}
