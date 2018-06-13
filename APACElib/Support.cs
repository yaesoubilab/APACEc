﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComputationLib;

namespace APACElib
{
    // Simulation and Optimization
    public enum EnumMarkOfEpidemicStartTime : int
    {
        TimeZero = 1,
        TimeOfFirstObservation = 2,
    }
    public enum EnumSimRNDSeedsSource : int
    {
        StartFrom0 = 1,
        Prespecified = 2,
        Weighted = 3,
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

    public enum EnumAndOr
    {
        And = 0,
        Or = 1,
    }

    public enum EnumSign
    {
        e = 0,
        l = 1,
        q = 2, 
        le = 3, 
        qe = 4
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
                case "Condition-Based":
                    onOffSwitchSetting = EnumDecisionRule.ConditionBased;
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

        public static int[] ConvertStringToIntArray(string str)
        {
            // remove spaces and brackets
            str = str.Replace(" ", "");
            str = str.Replace("{", "");
            str = str.Replace("}", "");
            // convert to array
            return Array.ConvertAll(str.Split(','), Convert.ToInt32);
        }
        public static double[] ConvertStringToDoubleArrray(string str)
        {
            // remove spaces and brackets
            str = str.Replace(" ", "");
            str = str.Replace("{", "");
            str = str.Replace("}", "");
            // convert to array
            return Array.ConvertAll(str.Split(','), Convert.ToDouble);
        }

        public static EnumSign[] ConvertToEnumSigns(string strSigns)
        {
            EnumSign[] signs;

            // remove spaces
            strSigns = strSigns.Replace(" ", "");
            // convert to array
            string[] arrSigns = Array.ConvertAll(strSigns.Split(','), Convert.ToString);
            signs = new EnumSign[arrSigns.Length];
            for (int i = 0; i < signs.Length; i++)
            {
                switch (arrSigns[i])
                {
                    case "e":
                        signs[i] = EnumSign.e;
                        break;
                    case "l":
                        signs[i] = EnumSign.l;
                        break;
                    case "q":
                        signs[i] = EnumSign.q;
                        break;
                    case "le":
                        signs[i] = EnumSign.le;
                        break;
                    case "qe":
                        signs[i] = EnumSign.qe;
                        break;
                }
            }
            return signs;
        }

        public static bool ValueOfComparison(double x, EnumSign sign, double y)
        {
            bool result = false;

            switch (sign)
            {
                case EnumSign.e:
                    result = (Math.Abs(x - y) < 0.0001);
                    break;
                case EnumSign.l:
                    result = (x < y);
                    break;
                case EnumSign.q:
                    result = (x > y);
                    break;
                case EnumSign.le:
                    result = (x <= y);
                    break;
                case EnumSign.qe:
                    result = (x >= y);
                    break;
            }
            return result;
        }

        public static double ReplaceNaNWith(double value, double replaceWith)
        {
            double result = 0;

            if (value is double.NaN)
                result = replaceWith;
            else
                result = value;
            
            return result;
        }
    }   
    

    public class Timer
    {
        // simulation run time
        private int _startTime;
        public double TimePassed { get; private set; }

        public void Start()
        {
            _startTime = Environment.TickCount;
        }
        public void Stop()
        {
            int endTime = Environment.TickCount;
            TimePassed = (double)(endTime - _startTime) / 1000;
        }
    }

    public class CommonSumRatioStatistics
    {
        public int ID { get; }
        public string Name { get; }
        public string StrType { get; }
        public string Formula { get; }
        public bool IfDisplay { get; }
        public bool SurveillanceDataAvailable { get; }
        public int NDeltaTDelayed { get; }
        public double SurveillanceNoise { get; }
        public bool FirstObsMarksEpiStart { get; }

        public bool IfIncludedInCalibration { get; }
        public string StrMeasureOfFit { get; }
        public string StrLikelihood { get; }
        public bool IfCheckWithinFeasibleRange { get; }
        public double FeasibleMin { get; }
        public double FeasibleMax { get; }
        
        public CommonSumRatioStatistics(Array sheet, int rowIndex)
        {
            ID = Convert.ToInt32(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.ID));
            Name = Convert.ToString(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Name));
            StrType = Convert.ToString(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Type));
            Formula = Convert.ToString(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Formula));

            // if display
            IfDisplay = SupportFunctions.ConvertYesNoToBool(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.IfDisplay).ToString());

            // real-time monitoring
            SurveillanceDataAvailable = SupportFunctions.ConvertYesNoToBool(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.SurveillanceDataAvailable).ToString());
            if (SurveillanceDataAvailable)
            {
                NDeltaTDelayed = Convert.ToInt32(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.NumOfDeltaTsDelayed));
                SurveillanceNoise = Convert.ToDouble(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Noise));
                FirstObsMarksEpiStart = SupportFunctions.ConvertYesNoToBool(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.FirstObservationMarksTheStartOfTheSpread).ToString());
            }
            // basic settings
            IfIncludedInCalibration = SupportFunctions.ConvertYesNoToBool(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.IfIncludedInCalibration).ToString());
            StrMeasureOfFit = Convert.ToString(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.MeasureOfFit));
            StrLikelihood = Convert.ToString(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Likelihood));
            IfCheckWithinFeasibleRange = SupportFunctions.ConvertYesNoToBool(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.IfCheckWithinFeasibleRange).ToString());
            if (IfCheckWithinFeasibleRange)
            {
                FeasibleMin = Convert.ToDouble(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.FeasibleRange_minimum));
                FeasibleMax = Convert.ToDouble(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.FeasibleRange_maximum));
            }

            // fourier
            double[] fourierWeights = new double[(int)CalibrationTarget.enumFourierSimilarityMeasures.SIZE];
            if (IfIncludedInCalibration && StrMeasureOfFit == "Fourier")
            {
                fourierWeights[(int)CalibrationTarget.enumFourierSimilarityMeasures.Cosine]
                    = Convert.ToDouble(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierCosine));
                fourierWeights[(int)CalibrationTarget.enumFourierSimilarityMeasures.Norm2]
                    = Convert.ToDouble(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierEuclidean));
                fourierWeights[(int)CalibrationTarget.enumFourierSimilarityMeasures.Average]
                    = Convert.ToDouble(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierAverage));
                fourierWeights[(int)CalibrationTarget.enumFourierSimilarityMeasures.StDev]
                    = Convert.ToDouble(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierStDev));
                fourierWeights[(int)CalibrationTarget.enumFourierSimilarityMeasures.Min]
                    = Convert.ToDouble(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierMin));
                fourierWeights[(int)CalibrationTarget.enumFourierSimilarityMeasures.Max]
                    = Convert.ToDouble(sheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierMax));
            }
        }
    }
}

