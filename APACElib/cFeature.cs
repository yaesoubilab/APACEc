﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ComputationLib;

namespace APACElib
{
    public abstract class Feature
    {
        public enum enumFeatureType
        {   
            EpidemicTime = 0, 
            Incidence = 1, // for new members
            Prediction= 2, // for new members
            AccumulatingIncidence= 3, // for new members
            NumOfDecisoinPeriodsOverWhichThisInterventionWasEmployed = 4,
        }

        public string Name { get; private set; }
        public int Index { get; private set; }
        public double Value { get; protected set; }
        public enumFeatureType FeatureType { get; private set; }
        public double Min { get; private set; }
        public double Max { get; private set; }
        protected int _numOfObsPeriodsForFuturePrediction;
        // Instantiation
        public Feature(string name, int index, enumFeatureType featureType = enumFeatureType.Incidence)
        {
            Name = name;
            Index = index;
            FeatureType = featureType;

            Min = double.MaxValue;
            Max = double.MinValue;
        }

        // Properties        
        public int NumOfTimePeriodsForFuturePrediction
        {
            get{ return _numOfObsPeriodsForFuturePrediction;}
        }
        public void UpdateValue(double value)
        {
            Value = value;
            if (value > Max)
                Max = value;
            if (value < Min)
                Min = value;
        }
    }

    public class Feature_EpidemicTime : Feature
    {
        public Feature_EpidemicTime(string name, int featureID) : base(name, featureID, enumFeatureType.EpidemicTime)
        {            
        }
    }

    public class Feature_DefinedOnNewClassMembers : Feature
    {       
        int _classID;        
        
        // feature definition for incidence
        public Feature_DefinedOnNewClassMembers(string name, int featureID, enumFeatureType featureType, int classID)
            : base(name, featureID, featureType)
        {   
            _classID = classID;
        }
        // feature definition for incidence and prediction
        public Feature_DefinedOnNewClassMembers(string name, int featureID, enumFeatureType featureType, int classID, int numOfObsPeriodsForFuturePrediction)
            : base(name, featureID, featureType)
        {            
            _classID = classID;
            _numOfObsPeriodsForFuturePrediction = numOfObsPeriodsForFuturePrediction;
        }

        // Properties
        public int ClassID
        {
            get{return _classID;}
        }                
    }

    public class Feature_DefinedOnSummationStatistics : Feature
    {
        int _sumStatisticsID;

        // feature definition for incidence
        public Feature_DefinedOnSummationStatistics(string name, int featureID, enumFeatureType featureType, int sumStatisticsID)
            : base(name, featureID, featureType)
        {
            _sumStatisticsID = sumStatisticsID;
        }
        // feature definition for incidence and prediction
        public Feature_DefinedOnSummationStatistics(string name, int featureID, enumFeatureType featureType, int sumStatisticsID, int numOfObsPeriodsForFuturePrediction)
            : base(name, featureID, featureType)
        {
            _sumStatisticsID = sumStatisticsID;
            _numOfObsPeriodsForFuturePrediction = numOfObsPeriodsForFuturePrediction;
        }

        // Properties
        public int SumStatisticsID
        {
            get{return _sumStatisticsID;}
        }
    }

    public class Feature_DefinedOnResources : Feature
    {
        // ***** resource features only return the current availability of each resource

        int _resourceID;

        // Instantiation
        public Feature_DefinedOnResources(string name, int featureID, int resourceID) 
            : base(name, featureID)
        {
            _resourceID = resourceID;
        }

        // Properties
        public int ResourceID
        {
            get { return _resourceID; }
        }
    }

    public class Feature_NumOfDecisoinPeriodsOverWhichThisInterventionWasUsed : Feature
    {
        int _interventionID;

        // Instantiaion
        public Feature_NumOfDecisoinPeriodsOverWhichThisInterventionWasUsed(string name, int featureID, int interventionID)
            : base(name, featureID)
        {
            _interventionID = interventionID;
        }

        // Properties
        public int InterventionID
        {
            get { return _interventionID; }
        }
    }

    public class Feature_InterventionOnOffStatus : Feature
    {
        int _interventionID;
        int _previousObservationPeriodToObserveOnOffValue;

        // Instantiaion
        public Feature_InterventionOnOffStatus(string name, int featureID, int interventionID, int previousObservationPeriodToObserveOnOffValue)
            : base(name, featureID)
        {
            _interventionID = interventionID;
            _previousObservationPeriodToObserveOnOffValue = previousObservationPeriodToObserveOnOffValue;
        }

        // Properties
        public int InterventionID
        {
            get { return _interventionID; }
        }
        public int PreviousObservationPeriodToObserveOnOffValue
        {
            get { return _previousObservationPeriodToObserveOnOffValue; }
        }
    }

    

    public class Condition
    {
        private List<Feature> _features;
        private int[] _featureIDs = new int[0];
        private double[] _t_low = new double[0];
        private double[] _t_high = new double[0];
        private EnumAndOr _andOr = EnumAndOr.And;

        public Condition(
            List<Feature> features,
            int[] featureIDs,
            double[] lowTheresholds,
            double[] highThresholds,
            EnumAndOr andOr = EnumAndOr.And)
        {
            _features = features;
            _featureIDs = featureIDs;
            _t_low = lowTheresholds;
            _t_high = highThresholds;
            _andOr = andOr;
        }

        public bool GetValue(int epiTimeIndex)
        {
            bool result = false;

            switch (_andOr)
            {
                case EnumAndOr.And:
                    {
                        result = true;  // all features within thresholds
                        for (int i = 0; i < _featureIDs.Length; i++)
                        {
                            // if one is outside
                            if (!(_features[_featureIDs[i]].Value >= _t_low[i] 
                                && _features[_featureIDs[i]].Value <= _t_high[i]))
                            {
                                result = false;
                                break;
                            }
                        }
                    }
                    break;
                case EnumAndOr.Or:
                    {
                        result = false; // no feature is within the thresholds
                        for (int i = 0; i < _featureIDs.Length; i++)
                        {
                            // if one is within
                            if (_features[_featureIDs[i]].Value >= _t_low[i]
                                && _features[_featureIDs[i]].Value <= _t_high[i])
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
}
