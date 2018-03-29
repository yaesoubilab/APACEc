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
        public enum enumFeatureType
        {   
            EpidemicTime = 0, 
            Incidence = 1, // for new members
            Prediction= 2, // for new members
            AccumulatingIncidence= 3, // for new members
            NumOfDecisoinPeriodsOverWhichThisInterventionWasEmployed = 4,
        }

        // Variables
        protected string _name;
        protected int _index;
        protected enumFeatureType _featureType;
        protected int _numOfObsPeriodsForFuturePrediction;
        protected double _max, _min;
        // Instantiation
        public Feature(string name, int index, enumFeatureType featureType)
        {
            _name = name;
            _index = index;
            _featureType = featureType;

            _min = double.MaxValue;
            _max = double.MinValue;
        }
        public Feature(string name, int index)
        {
            _name = name;
            _index = index;

            _min = double.MaxValue;
            _max = double.MinValue;
        }

        // Properties
        public int Index
        {
            get { return _index; }
        }
        public string Name
        {
            get{ return _name; }
        }
        public double Max
        {
            get{ return _max; }
        }
        public double Min
        {
            get{ return _min; }
        }
        public enumFeatureType FeatureType
        {
            get{ return _featureType; }
        }
        public int NumOfTimePeriodsForFuturePrediction
        {
            get{ return _numOfObsPeriodsForFuturePrediction;}
        }
        public void UpdateMinMax(double value)
        {
            if (value > _max)
                _max = value;
            if (value < _min)
                _min = value;
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

}
