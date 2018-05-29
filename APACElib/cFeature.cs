using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ComputationLib;

namespace APACElib
{
    public abstract class Feature
    {
        public string Name { get; private set; }
        public int Index { get; private set; }
        public double Value { get; protected set; }
        public double Min { get; private set; }
        public double Max { get; private set; }
        
        // Instantiation
        public Feature(string name, int index)
        {
            Name = name;
            Index = index;

            Min = double.MaxValue;
            Max = double.MinValue;
        }

        public abstract void Update(int epiTimeIndex);
        protected void UpdateMinMax()
        {
            if (Value > Max)
                Max = Value;
            if (Value < Min)
                Min = Value;
        }
    }

    public class Feature_EpidemicTime : Feature
    {
        public Feature_EpidemicTime(string name, int featureID) 
            : base(name, featureID)
        {            
        }

        public override void Update(int epiTimeIndex)
        {
            Value = epiTimeIndex;
            UpdateMinMax();
        }
    }

    public class Feature_SpecialStats: Feature
    {
        private enum EnumFeatureType
        {
            CurrentObservedValue = 0,
            Slope = 1
        }

        private EnumFeatureType _featureType;
        private SurveyedTrajectory _surveyedTraj; // pointer 
        private double _par;

        public Feature_SpecialStats(string name, int featureID, string strFeatureType, SurveyedTrajectory surveyedTraj, double par) 
            : base(name, featureID)
        {
            switch (strFeatureType)
            {
                case "Current Observed Value":
                    _featureType = EnumFeatureType.CurrentObservedValue;
                    break;
                case "Slope":
                    _featureType = EnumFeatureType.Slope;
                    break;
            }
            _surveyedTraj = surveyedTraj;
        }

        public override void Update(int epiTimeIndex)
        {
            switch (_featureType)
            {
                case EnumFeatureType.CurrentObservedValue:
                    Value = _surveyedTraj.GetLastObs(epiTimeIndex);
                    break;

                case EnumFeatureType.Slope:
                    // TODO: implement slope
                    Value = 0;
                    break;
            }

            UpdateMinMax();
        }
    }

    public class Feature_Intervention: Feature
    {
        private enum EnumFeatureType
        {
            IfEventSwitchedOff = 0,
        }

        private Intervention _intervention; // pointer
        private EnumFeatureType _featureType;

        public Feature_Intervention(string name, int featureID, string strFeatureType, Intervention intervention): base(name, featureID)
        {
            switch (strFeatureType)
            {
                case "If Ever Switched Off":
                    _featureType = EnumFeatureType.IfEventSwitchedOff;
                    break;
            }
            _intervention = intervention;
        }

        public override void Update(int epiTimeIndex)
        {
            switch (_featureType)
            {
                case EnumFeatureType.IfEventSwitchedOff:
                    Value = Convert.ToDouble(_intervention.IfEverTurnedOffBefore);
                    break;
            }
        }
    }




    //public class Feature_DefinedOnNewClassMembers : Feature
    //{       
    //    int _classID;        
        
    //    // feature definition for incidence
    //    public Feature_DefinedOnNewClassMembers(string name, int featureID, EnumFeatureType featureType, int classID)
    //        : base(name, featureID, featureType)
    //    {   
    //        _classID = classID;
    //    }
    //    // feature definition for incidence and prediction
    //    public Feature_DefinedOnNewClassMembers(string name, int featureID, EnumFeatureType featureType, int classID, int numOfObsPeriodsForFuturePrediction)
    //        : base(name, featureID, featureType)
    //    {            
    //        _classID = classID;
    //        _numOfObsPeriodsForFuturePrediction = numOfObsPeriodsForFuturePrediction;
    //    }

    //    // Properties
    //    public int ClassID
    //    {
    //        get{return _classID;}
    //    }                
    //}
     
    //public class Feature_DefinedOnSummationStatistics : Feature
    //{
    //    int _sumStatisticsID;

    //    // feature definition for incidence
    //    public Feature_DefinedOnSummationStatistics(string name, int featureID, EnumFeatureType featureType, int sumStatisticsID)
    //        : base(name, featureID, featureType)
    //    {
    //        _sumStatisticsID = sumStatisticsID;
    //    }
    //    // feature definition for incidence and prediction
    //    public Feature_DefinedOnSummationStatistics(string name, int featureID, EnumFeatureType featureType, int sumStatisticsID, int numOfObsPeriodsForFuturePrediction)
    //        : base(name, featureID, featureType)
    //    {
    //        _sumStatisticsID = sumStatisticsID;
    //        _numOfObsPeriodsForFuturePrediction = numOfObsPeriodsForFuturePrediction;
    //    }

    //    // Properties
    //    public int SumStatisticsID
    //    {
    //        get{return _sumStatisticsID;}
    //    }
    //}

    //public class Feature_DefinedOnResources : Feature
    //{
    //    // ***** resource features only return the current availability of each resource

    //    int _resourceID;

    //    // Instantiation
    //    public Feature_DefinedOnResources(string name, int featureID, int resourceID) 
    //        : base(name, featureID)
    //    {
    //        _resourceID = resourceID;
    //    }

    //    // Properties
    //    public int ResourceID
    //    {
    //        get { return _resourceID; }
    //    }
    //}

    //public class Feature_NumOfDecisoinPeriodsOverWhichThisInterventionWasUsed : Feature
    //{
    //    int _interventionID;

    //    // Instantiaion
    //    public Feature_NumOfDecisoinPeriodsOverWhichThisInterventionWasUsed(string name, int featureID, int interventionID)
    //        : base(name, featureID)
    //    {
    //        _interventionID = interventionID;
    //    }

    //    // Properties
    //    public int InterventionID
    //    {
    //        get { return _interventionID; }
    //    }
    //}

    //public class Feature_InterventionOnOffStatus : Feature
    //{
    //    int _interventionID;
    //    int _previousObservationPeriodToObserveOnOffValue;

    //    // Instantiaion
    //    public Feature_InterventionOnOffStatus(string name, int featureID, int interventionID, int previousObservationPeriodToObserveOnOffValue)
    //        : base(name, featureID)
    //    {
    //        _interventionID = interventionID;
    //        _previousObservationPeriodToObserveOnOffValue = previousObservationPeriodToObserveOnOffValue;
    //    }

    //    // Properties
    //    public int InterventionID
    //    {
    //        get { return _interventionID; }
    //    }
    //    public int PreviousObservationPeriodToObserveOnOffValue
    //    {
    //        get { return _previousObservationPeriodToObserveOnOffValue; }
    //    }
    //}

    
    public abstract class Condition
    {
        public Condition()
        {
        }

        public abstract bool GetValue(int epiTimeIndex);
    }

    public class Condition_AlwaysTrue : Condition
    {
        public override bool GetValue(int epiTimeIndex)
        {
            return true;
        }
    }

    public class Condition_AlwaysFalse : Condition
    {
        public override bool GetValue(int epiTimeIndex)
        {
            return false;
        }
    }

    public class Condition_OnFeatures : Condition
    {
        private List<Feature> _features;
        private int[] _featureIDs = new int[0];
        private EnumSign[] _signs = new EnumSign[0];
        private double[] _thresholds = new double[0];
        private EnumAndOr _andOr = EnumAndOr.And;

        public Condition_OnFeatures(
            List<Feature> features,
            string strFeatureIDs,
            string strSigns,
            string strTheresholds,
            string strConclusions): base()
        {
            _features = features;
            _featureIDs = SupportProcedures.ConvertStringToIntArray(strFeatureIDs);
            _signs = SupportProcedures.ConvertToEnumSigns(strSigns);
            _thresholds = SupportProcedures.ConvertStringToDoubleArrray(strTheresholds);
            if (strConclusions == "And")
                _andOr = EnumAndOr.And;
            else
                _andOr = EnumAndOr.Or;
        }

        public override bool GetValue(int epiTimeIndex)
        {
            bool result = false;

            switch (_andOr)
            {
                case EnumAndOr.And:
                    {
                        result = true;  // all features hit thresholds
                        for (int i = 0; i < _featureIDs.Length; i++)
                        {
                            // if one does not 
                            if (!(SupportProcedures.ValueOfComparison(
                                _features[_featureIDs[i]].Value, _signs[i], _thresholds[i]))
                                )
                            {
                                result = false;
                                break;
                            }
                        }
                    }
                    break;
                case EnumAndOr.Or:
                    {
                        result = false; // no feature hits its threshold
                        for (int i = 0; i < _featureIDs.Length; i++)
                        {
                            // if one is within
                            if (SupportProcedures.ValueOfComparison(
                                _features[_featureIDs[i]].Value, _signs[i], _thresholds[i]))
                            {
                                result = true;
                                break;
                            }
                        }
                    }
                    break;
            }

            return result;
        }
    }

    public class Condition_OnConditions: Condition
    {
        private List<Condition> _conditions;
        private int[] _conditionIDs;
        private EnumAndOr _andOr = EnumAndOr.And;

        public Condition_OnConditions(
            List<Condition> conditions,
            string strConditions,
            string strConclusions): base()
        {
            _conditions = conditions;
            _conditionIDs = SupportProcedures.ConvertStringToIntArray(strConditions);
            if (strConclusions == "And")
                _andOr = EnumAndOr.And;
            else
                _andOr = EnumAndOr.Or;
        }

        public override bool GetValue(int epiTimeIndex)
        {
            bool results = false; 

            switch (_andOr)
            {
                case EnumAndOr.And:
                    {
                        results = true;  // all conditions are satisifed
                        for (int i = 0; i < _conditionIDs.Length; i++)
                        {
                            // if one conditions is not satisfied
                            if (_conditions[_conditionIDs[i]].GetValue(epiTimeIndex) == false)
                            {
                                results = false;
                                break;
                            }
                        }
                    }
                    break;
                case EnumAndOr.Or:
                    {
                        results = false;  // no conditions is satisifed
                        for (int i = 0; i < _conditionIDs.Length; i++)
                        {
                            // if one is satisifed
                            if (_conditions[_conditionIDs[i]].GetValue(epiTimeIndex) == true)
                            {
                                results = true;
                                break;
                            }
                        }
                    }
                    break;
            }
            return results;
        }
    }
}
