using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APACElib
{
    // Simulation and Optimization
    public enum EnumMarkOfEpidemicStartTime : int
    {
        TimeZero = 1,
        TimeOfFirstObservation = 2,
    }
    public enum EnumSimulationRNDSeedsSource : int
    {
        StartFrom0 = 1,
        PrespecifiedSquence = 2,
        WeightedPrespecifiedSquence = 3,
    }
    public enum enumFeatureCombinations
    {
        None = 0,
        IncidenceOnly = 1, 
        PredictionOnly = 2, 
        AccumulatingIncidenceOnly = 3,
        AccumulatingIncidenceANDPrediction = 4,
        IncidenceANDAccumulatingIncidence = 5,         
    }
    public enum EnumModelUse : int
    {
        Simulation = 0,
        Calibration = 1,
        Optimization = 2,
    }
    public enum EnumObjectiveFunction : int
    {
        MaximizeNMB = 0,
        MaximizeNHB = 1,
    }
    public enum EnumEpiDecisions : int
    {
        SpecifiedByPolicy = 1,
        PredeterminedSequence = 2,
    }
    public enum EnumStaticPolicyOptimizationMethod : int
    {
        FullFactorialEvaluation = 0,
        StochasticOptimization = 1,
    }
    public enum enumADPParameter : int
    {
        WTPForHealth = 0,
        HarmonicStepSize_a = 1,
        EpsilonGreedy_beta = 2,
    }
    
    public enum enumIntervalBasedStaticPolicySetting : int
    {
        StartTime = 0,
        NumOfDecisionPeriodsToUse = 1,
    }
    public enum enumThresholdBasedStaticPolicySetting : int
    {
        FirstRNDSeed = 0,
        DecisionID = 1,
        Threshold = 2,
        NumOfDecisionPeriodsToUse = 3,
    }
    // performance 
    public enum enumTimes : int
    {
        StartSimulationAPolicy = 0,
        EndOfBuildModelFromSpreadsheet = 1,
        End = 2,
    }    

 
    // Public procedures
    public static class SupportProcedures
    {
        public const double minimumWTPforHealth = 0.01; // 1 cent

        public static EnumDecisionRule ConvertToDecisionRule(string strOnOffSwitchSetting)
        {
            EnumDecisionRule onOffSwitchSetting = EnumDecisionRule.Predetermined;
            switch (strOnOffSwitchSetting)
            {
                case "Predetermined":
                    onOffSwitchSetting = EnumDecisionRule.Predetermined;
                    break;
                case "Periodic":
                    onOffSwitchSetting = EnumDecisionRule.Periodic;
                    break;
                case "Threshold-Based":
                    onOffSwitchSetting = EnumDecisionRule.ThresholdBased;
                    break;
                case "Interval-Based":
                    onOffSwitchSetting = EnumDecisionRule.IntervalBased;
                    break;
                case "Dynamic":
                    onOffSwitchSetting = EnumDecisionRule.Dynamic;
                    break;
            }
            return onOffSwitchSetting;
        }

        public static int ConvertToSwitchValue(string value)
        {
            int switchValue = 0;

            if (value == "On")
                switchValue = 1;

            return switchValue;
        }

        public static enumFeatureCombinations ConvertToFeatureCombination(string input)
        {
            enumFeatureCombinations result = enumFeatureCombinations.None;

            switch (input)
            {
                case "None":
                    result = enumFeatureCombinations.None;
                    break;
                case "Incidence":
                    result = enumFeatureCombinations.IncidenceOnly;
                    break;
                case "Prediction":
                    result = enumFeatureCombinations.PredictionOnly;
                    break;
                case "Accumulating Incidence":
                    result = enumFeatureCombinations.AccumulatingIncidenceOnly;
                    break;
                case "Incidence and Accumulating Incidence":
                    result = enumFeatureCombinations.IncidenceANDAccumulatingIncidence;
                    break;      
                case "Accumulating Incidence and Prediction":
                    result = enumFeatureCombinations.AccumulatingIncidenceANDPrediction;
                    break;
            }
            return result;
        }

    }   
    
}

