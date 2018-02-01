using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APACE_lib
{
    // Simulation and Optimization
    public enum enumMarkOfEpidemicStartTime : int
    {
        TimeZero = 1,
        TimeOfFirstObservation = 2,
    }
    public enum enumSimulationRNDSeedsSource : int
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
    public enum enumModelUse : int
    {
        Simulation = 0,
        Calibration = 1,
        Optimization = 2,
    }
    public enum enumObjectiveFunction : int
    {
        MaximizeNMB = 0,
        MaximizeNHB = 1,
    }
    public enum enumDecisionRule : int
    {
        SpecifiedByPolicy = 1,
        //Greedy = 3,
        //EpsilonGreedy = 4,
        PredeterminedSequence = 2,
    }
    public enum enumStaticPolicyOptimizationMethod : int
    {
        FullFactorialEvaluation = 0,
        StochasticOptimization = 1,
    }
    public enum enumWhatToTransfer : int
    {
        Nothing = 0,
        AllClasses = 1,
        NonNormalClasses = 2,
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

