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
        public double? Value { get; protected set; }
        public double Min { get; private set; }
        public double Max { get; private set; }
        
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
                Max = Value.GetValueOrDefault();
            if (Value < Min)
                Min = Value.GetValueOrDefault();
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
        public enum EnumFeatureType
        {
            CurrentObservedValue = 0,
            Slope = 1
        }

        private EnumFeatureType _featureType;
        private SurveyedTrajectory _surveyedTraj; // pointer 
        //private double _par;

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
        public Feature_SpecialStats(string name, int featureID, EnumFeatureType featureType, SurveyedTrajectory surveyedTraj, double par)
            : base(name, featureID)
        {
            _featureType = featureType;
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
                    Value = _surveyedTraj.GetIncrementalChange(epiTimeIndex);
                    break;
            }

            UpdateMinMax();
        }
    }

    public class Feature_Intervention: Feature
    {
        public enum EnumFeatureType
        {
            IfEverSwitchedOff = 0,
            IfEverSwitchedOn = 1
        }

        private Intervention _intervention; // pointer
        private EnumFeatureType _featureType;

        public Feature_Intervention(string name, int featureID, string strFeatureType, Intervention intervention): base(name, featureID)
        {
            switch (strFeatureType)
            {
                case "If Ever Switched Off":
                    _featureType = EnumFeatureType.IfEverSwitchedOff;
                    break;
                case "If Ever Switched On":
                    _featureType = EnumFeatureType.IfEverSwitchedOn;
                    break;
                default:
                    throw new Exception("Invalid value for feature type defined on intervention.");
            }
            _intervention = intervention;
        }
        public Feature_Intervention(string name, int featureID, EnumFeatureType featureType, Intervention intervention) : base(name, featureID)
        {
            _featureType = featureType;
            _intervention = intervention;
        }

        public override void Update(int epiTimeIndex)
        {
            switch (_featureType)
            {
                case EnumFeatureType.IfEverSwitchedOff:
                    Value = Convert.ToDouble(_intervention.IfEverTurnedOffBefore);
                    break;
                case EnumFeatureType.IfEverSwitchedOn:
                    Value = Convert.ToDouble(_intervention.IfEverTurnedOnBefore);
                    break;
            }
        }
    }
    
    public abstract class Condition
    {
        public int ID { get; }
        public bool Value { get; protected set; }
        public Condition(int id)
        {
            ID = id;
        }

        public abstract void Update(int epiTimeIndex);
    }

    public class Condition_AlwaysTrue : Condition
    {
        public Condition_AlwaysTrue(int id) : base(id) { }

        public override void Update(int epiTimeIndex)
        {
            Value = true;
        }
    }

    public class Condition_AlwaysFalse : Condition
    {
        public Condition_AlwaysFalse(int id) : base(id) { }

        public override void Update(int epiTimeIndex)
        {
            Value = false;
        }
    }

    public class Condition_OnFeatures : Condition
    {
        private List<Feature> _features;
        private int[] _featureIDs = new int[0];
        private EnumSign[] _signs = new EnumSign[0];
        private List<Parameter> _thresholdPars = new List<Parameter>();
        private double[] _thresholdValues = new double[0];
        private EnumAndOr _andOr = EnumAndOr.And;

        public Condition_OnFeatures(
            int id,
            List<Feature> features,
            string strFeatureIDs,
            string strSigns,
            string strTheresholds,
            string strConclusions): base(id)
        {
            _features = features;
            _featureIDs = SupportProcedures.ConvertStringToIntArray(strFeatureIDs);
            _signs = SupportProcedures.ConvertToEnumSigns(strSigns);
            _thresholdValues = SupportProcedures.ConvertStringToDoubleArrray(strTheresholds);
            if (strConclusions == "And")
                _andOr = EnumAndOr.And;
            else
                _andOr = EnumAndOr.Or;
        }
        public Condition_OnFeatures(
            int id,
            List<Feature> features,
            int[] featureIDs,
            EnumSign[] signs,
            double[] theresholds,
            EnumAndOr conclusion) : base(id)
        {
            _features = features;
            _featureIDs = featureIDs;
            _signs = signs;
            _thresholdValues = theresholds;
            _andOr = conclusion;
        }

        public void UpdateThresholds(double[] values)
        {
            _thresholdValues = (double[])values.Clone();
        }

        public override void Update(int epiTimeIndex)
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
                            if (_features[_featureIDs[i]].Value.HasValue &&
                                !SupportProcedures.ValueOfComparison(
                                    _features[_featureIDs[i]].Value.Value, _signs[i], _thresholdValues[i]))                                
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
                            if (_features[_featureIDs[i]].Value.HasValue && 
                                SupportProcedures.ValueOfComparison(
                                    _features[_featureIDs[i]].Value.Value, _signs[i], _thresholdValues[i]))
                            {
                                result = true;
                                break;
                            }
                        }
                    }
                    break;
            }

            Value = result;
        }
    }

    public class Condition_OnConditions: Condition
    {
        private List<Condition> _conditions;
        private int[] _conditionIDs;
        private EnumAndOr _andOr = EnumAndOr.And;

        public Condition_OnConditions(
            int id,
            List<Condition> conditions,
            string strConditions,
            string strConclusions): base(id)
        {
            _conditions = conditions;
            _conditionIDs = SupportProcedures.ConvertStringToIntArray(strConditions);
            if (strConclusions == "And")
                _andOr = EnumAndOr.And;
            else
                _andOr = EnumAndOr.Or;
        }
        public Condition_OnConditions(
            int id,
            List<Condition> conditions,
            int[] conditionIDs,
            EnumAndOr conclusion) : base(id)
        {
            _conditions = conditions;
            _conditionIDs = conditionIDs;
            _andOr = conclusion;
        }

        public override void Update(int epiTimeIndex)
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
                            if (_conditions[_conditionIDs[i]].Value == false)
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
                            if (_conditions[_conditionIDs[i]].Value == true)
                            {
                                results = true;
                                break;
                            }
                        }
                    }
                    break;
            }
            Value = results;
        }
    }
}
