using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimulationLib;
using ComputationLib;

namespace APACElib
{
    // Counter Statistics
    public class cCounterStatistics
    {
        // Variables
        #region Variables               

        // Fields
        string _name;
        TimeSeries _timeSeries;
        double _QALYLossPerCount;
        double _healthQualityPerUnitOfTime;
        double _costPerCount;
        double _costPerUnitOfTime;     
        int _numOfPastObsPeriodsToStore;
        int _numOfObsPeriodsDelayed;
        int _numOfDeltaTInEachObsPeriod;
        // statistics        
        int _currentCount;
        double _currentCost;
        double _currentQALY;
        double _totalCost;
        double _totalQALY;
        int _totalCounts;
        bool _collectTotalCount = false;        
        #endregion

        /// <summary>
        /// Counter statistics with no time-series
        /// </summary>
        /// <param name="name"></param>
        /// <param name="QALYLossPerCount"></param>
        /// <param name="healthQualityPerUnitOfTime"></param>
        /// <param name="costPerCount"></param>
        /// <param name="costPerUnitOfTime"></param>
        /// <param name="collectTotalCount"></param>
        public cCounterStatistics(string name, double QALYLossPerCount, double healthQualityPerUnitOfTime, 
            double costPerCount, double costPerUnitOfTime, bool collectTotalCount)
        {
            _name = name;
            _QALYLossPerCount = QALYLossPerCount;
            _healthQualityPerUnitOfTime = healthQualityPerUnitOfTime;
            _costPerCount = costPerCount;
            _costPerUnitOfTime = costPerUnitOfTime;
            _collectTotalCount = collectTotalCount;
        }
        public cCounterStatistics(string name, double QALYLossPerCount, double healthQualityPerUnitOfTime, double costPerCount, double costPerUnitOfTime,
                                int numOfPastObsPeriodsToStore, int numOfDeltaTInEachObsPeriod, int numOfObsPeriodsDelayed, bool collectTotalCount)
        {
            _name = name;
            _QALYLossPerCount = QALYLossPerCount;
            _healthQualityPerUnitOfTime = healthQualityPerUnitOfTime;
            _costPerCount = costPerCount;
            _costPerUnitOfTime = costPerUnitOfTime;

            _numOfPastObsPeriodsToStore = numOfPastObsPeriodsToStore;
            _numOfDeltaTInEachObsPeriod = numOfDeltaTInEachObsPeriod;
            _numOfObsPeriodsDelayed = numOfObsPeriodsDelayed;
            _collectTotalCount = collectTotalCount;

            // setup prediction
            if (numOfPastObsPeriodsToStore > 0)
                _timeSeries = new TimeSeries(name, numOfPastObsPeriodsToStore + numOfObsPeriodsDelayed, numOfDeltaTInEachObsPeriod, TimeSeries.enumPredictionModel.Nothing);
        }  
     
        // create a clone of this class
        public cCounterStatistics Clone()
        {
            cCounterStatistics clone;
            if (_timeSeries == null)
                clone = new cCounterStatistics(_name, _QALYLossPerCount, _healthQualityPerUnitOfTime, _costPerCount, _costPerUnitOfTime, _collectTotalCount);            
            else
                clone = new cCounterStatistics(_name, _QALYLossPerCount, _healthQualityPerUnitOfTime, _costPerCount, _costPerUnitOfTime,
                     _numOfPastObsPeriodsToStore, _numOfDeltaTInEachObsPeriod, _numOfObsPeriodsDelayed, _collectTotalCount);
                
            return clone;
        }

        // Properties
        public int CurrentCountsInThisObsPeriod
        {
            get { return (int)_timeSeries.CurrentAggregatedObsInLastObsPeriod; }
        }
        public int CurrentCountsInThisSimulationOutputInterval
        {
            get { return (int)_timeSeries.CurrentAggregatedObsInLastObsPeriod; }
        }
        public int TotalCounts
        {
            get { return _totalCounts; }
        }
        public int LastObservedCounts
        {
            get { return (int)_timeSeries.Sum(_numOfPastObsPeriodsToStore - 1, _numOfPastObsPeriodsToStore - 1); }
        }
        public int TotalObservedCounts
        {
            get
            {
                if (_timeSeries == null)
                    return _totalCounts;
                else
                    return _totalCounts - (int)_timeSeries.Sum(_numOfPastObsPeriodsToStore, _numOfPastObsPeriodsToStore + _numOfObsPeriodsDelayed - 1);
            }
        }

        public double CurrentQALY
        {
            get{ return _currentQALY;}
        }
        public double CurrentCost
        {
            get{ return _currentCost;}
        }
        public double TotalQALY
        {
            get{ return _totalQALY; }
        }
        public double TotalCost
        {
            get{return _totalCost;}
        }
        
        // add one row of data
        public void AddAnObservation(int count, double duration)
        {
            // record the new count
            _currentCount = count;
            if (_collectTotalCount) _totalCounts += count;

            // if a time series object is attached
            if (_timeSeries != null)
                _timeSeries.AddAnObs(count);

            _currentQALY = (- _QALYLossPerCount + duration * _healthQualityPerUnitOfTime) * count ;
            _currentCost = (_costPerCount + duration * _costPerUnitOfTime) * count;
            
            _totalQALY += _currentQALY;
            _totalCost += _currentCost;
        }        
        public void AddAnObservation(int count)
        {
            AddAnObservation(count, 0);        
        }        
        // prediction
        public double Prediction(int numOfObsPeriodsInFuture)
        {
            double prediction = 0;
            if (_timeSeries != null)
                prediction = _timeSeries.Prediction(numOfObsPeriodsInFuture, false);

            return prediction;
        }
        // trend
        public double Trend()
        {
            double trend = 0;
            if (_timeSeries != null)
                trend = _timeSeries.Trend();

            return trend;
        }
        // Mean
        public double Mean()
        {
            double mean  = 0;
            if (_timeSeries != null)
                mean = _timeSeries.Mean();

            return mean;
        }
        public double DelayedMean()
        {
            double mean = 0;
            if (_timeSeries != null)
                mean = _timeSeries.Mean(0, _numOfPastObsPeriodsToStore - 1);

            return mean;
        }

        // reset current aggregated counts
        public void ResetCurrentAggregatedObsInThisObsPeriodervation()
        {
            _timeSeries.ResetCurrentAggregatedObsInThisObsPeriodervation();
        }

        // reset the statistics for another simulation run
        public void ResetForAnotherSimulationRun()
        {
            _currentQALY = 0;
            _currentCost = 0;
            _totalCounts = 0;
            _totalQALY = 0;
            _totalCost = 0;

            if (_timeSeries != null)
                _timeSeries.Reset();
        }        
    }
}
