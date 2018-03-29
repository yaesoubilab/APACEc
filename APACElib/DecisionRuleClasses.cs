using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APACElib
{
    public enum EnumDecisionRule
    {
        Predetermined = 1,      // always one or off
        Periodic = 2,           // employ at certain frequency
        ThresholdBased = 3,     // employ once a threshold is passed
        IntervalBased = 4,      // employ during a certain time interval
        Dynamic = 5             // guided by a dynamic policy 
    }

    public abstract class DecisionRule
    {
        
        public virtual int GetSwitchStatus(int epiTimeIndex)
        {
            return 0;
        }
    }

    // predetermined decision rule 
    public class DecionRule_Predetermined : DecisionRule
    {
        public int PredeterminedSwitchValue { get; set; } = 0;

        public DecionRule_Predetermined(int predeterminedSwitchValue)
        {
            PredeterminedSwitchValue = predeterminedSwitchValue;
        }

        public override int GetSwitchStatus(int epiTimeIndex)
        {
            return PredeterminedSwitchValue;
        }
    }

    // periodic decision rule 
    public class DecionRule_Periodic : DecisionRule
    {
        private int _frequency_nOfDcisionPeriods = 0;
        private int _duration_nOfDcisionPeriods = 0;

        public DecionRule_Periodic(int frequency_nOfDcisionPeriods, int duration_nOfDcisionPeriods)
        {
            _frequency_nOfDcisionPeriods = frequency_nOfDcisionPeriods;
            _duration_nOfDcisionPeriods = duration_nOfDcisionPeriods;
        }
    }

    // threshold-based decision rule 
    public class DecionRule_ThresholdBased : DecisionRule
    {
        private double _threshold = 0;
        private int _duration_nOfTimeIndices = 0;

        public DecionRule_ThresholdBased(double threshold, int duration_nOfTimeIndices)
        {
            _threshold = threshold;
            _duration_nOfTimeIndices = duration_nOfTimeIndices;
        }
    }

    // interval-based decision rule 
    public class DecionRule_IntervalBased : DecisionRule
    {
        private int _timeIndexToTurnOn;
        private int _timeIndexToTurnOff;

        public DecionRule_IntervalBased(int timeIndexToTurnOn, int timeIndexToTurnOff)
        {
            _timeIndexToTurnOn = timeIndexToTurnOn;
            _timeIndexToTurnOff = timeIndexToTurnOff;
        }
    }

    // dynamic decision rule 
    public class DecionRule_Dynamic : DecisionRule
    {
        public DecionRule_Dynamic()
        {
        }
    }
}
