using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimulationLib;
using ComputationLib;

namespace APACElib
{
    public abstract class TimeSeries
    {
        public List<double> ObsList { get; set; } = new List<double>();
        protected int _counter = 0;
        protected int _nOfRecodingsInEachObsPeriod;

        public TimeSeries(int nOfRecodingsInEachObsPeriod)
        {
            _nOfRecodingsInEachObsPeriod = nOfRecodingsInEachObsPeriod;
            ObsList.Add(0); // 0 added for the interval 0
        }

        public abstract void Record(double value);

        public double GetLastObs()
        {
            return ObsList.Last();
        }

        public void Reset()
        {
            ObsList.Clear();
            ObsList.Add(0); // 0 added for the interval 0
            _counter = 0;
        }
    }

    public class IncidenceTimeSeries: TimeSeries
    {
        public IncidenceTimeSeries(int nOfRecodingsInEachObsPeriod):base(nOfRecodingsInEachObsPeriod)
        {
        }

        public override void Record(double value)
        {
            // find if a new element should be added to the list
            if (_counter % _nOfRecodingsInEachObsPeriod == 0)
            {
                ObsList.Add(value);
                ++_counter;
            }
            else
            {
                ObsList[ObsList.Count-1] += value;
            }
        }
    }
     
    public class PrevalenceTimeSeries : TimeSeries
    {
        public PrevalenceTimeSeries(int nOfRecodingsInEachObsPeriod) : base(nOfRecodingsInEachObsPeriod)
        {
        }

        public override void Record(double value)
        {
            // find if a new element should be added to the list
            if (_counter % _nOfRecodingsInEachObsPeriod == 0)
            {
                ObsList.Add(value);
                ++_counter;
            }
        }
    }

    public class DeltaTCostHealth
    {
        public double DeltaTCost{ get; set; }
        public double DeltaTDALY { get; set; }

        bool _ifCollecting;
        double _warmUpSimIndex;
        double _DALYPerNewMember;
        double _costPerNewMember;
        double _disabilityWeightPerDeltaT;
        double _costPerDeltaT;

        public DeltaTCostHealth(
            int warmUpSimIndex,
            double DALYPerNewMember,
            double costPerNewMember,
            double disabilityWeightPerDeltaT,
            double costPerDeltaT)
        {
            // find if cost and health outcomes should be collected
            double m = new[] { DALYPerNewMember, costPerNewMember, disabilityWeightPerDeltaT, costPerDeltaT}.Max();
            if (m > 0) _ifCollecting = true;

            _warmUpSimIndex = warmUpSimIndex;
            _DALYPerNewMember = DALYPerNewMember;
            _costPerNewMember = costPerNewMember;
            _disabilityWeightPerDeltaT = disabilityWeightPerDeltaT;
            _costPerDeltaT = costPerDeltaT;
        }

        public void Update(int simIndex, double prevalence, double incidence)
        {
            if (_ifCollecting && simIndex >= _warmUpSimIndex)
            {
                DeltaTCost = _costPerNewMember * incidence + _costPerDeltaT * prevalence;
                DeltaTDALY = _DALYPerNewMember * incidence + _disabilityWeightPerDeltaT * prevalence;
            }
        }

        public void Reset()
        {
            DeltaTCost = 0;
            DeltaTDALY = 0;
        }
    }

    public class EpidemicCostHealth
    {
        public double TotalDisountedCost { get; set; }
        public double TotalDiscountedDALY { get; set; }
        private double _deltaTCost;
        private double _deltaTDALY;
        private int _currentSimIndex;

        double _deltaTDiscountRate;
        double _warmUpSimIndex;

        public EpidemicCostHealth(double deltaTDiscountRate, int warmUpSimIndex)
        {
            _deltaTDiscountRate = deltaTDiscountRate;
            _warmUpSimIndex = warmUpSimIndex;
            _currentSimIndex = 0;
        }

        public void Add(int simIndex, double deltaTCost, double deltaTDALY)
        {
            if (simIndex >= _warmUpSimIndex)
            {
                if (simIndex>_currentSimIndex)
                {
                    TotalDisountedCost = deltaTCost / Math.Pow(1+_deltaTDiscountRate, simIndex-_warmUpSimIndex );
                    TotalDiscountedDALY = deltaTDALY / Math.Pow(1+_deltaTDiscountRate, simIndex-_warmUpSimIndex );

                    _currentSimIndex = simIndex;
                    _deltaTCost = deltaTCost;
                    _deltaTDALY = deltaTDALY;
                }
                else
                {
                    _deltaTCost += deltaTCost;
                    _deltaTDALY += deltaTDALY;
                }                 
            }
        }

        public double GetEquivalentAnnualCost(double annualDiscountRate, int currentYear, int warmUpYear)
        {
            if (annualDiscountRate == 0)
                return TotalDisountedCost/ (currentYear - warmUpYear);
            else
                return annualDiscountRate * TotalDisountedCost / (1 - Math.Pow(1 + annualDiscountRate, -(currentYear-warmUpYear)));
        }
        public double GetEquivalentAnnualDALY(double annualDiscountRate, int currentYear, int warmUpYear)
        {
            if (annualDiscountRate == 0)
                return TotalDiscountedDALY / (currentYear - warmUpYear);
            else
                return annualDiscountRate * TotalDiscountedDALY / (1 - Math.Pow(1 + annualDiscountRate, -(currentYear-warmUpYear)));
        }

        public double GetDiscountedNMB(double wtp)
        {
            return -TotalDisountedCost - wtp * TotalDiscountedDALY;
        }
        public double GetDiscountedNHB(double wtp)
        {
            return -TotalDisountedCost/ wtp - TotalDiscountedDALY;
        }

        public void Reset()
        {
            _currentSimIndex = 0;
            _deltaTDALY = 0;
            _deltaTCost = 0;
            TotalDisountedCost = 0;
            TotalDiscountedDALY = 0;
        }
    }

    public class ClassStatistics
    {
        private int _warmUpSimIndex;
        // statistics 
        public int NumOfNewMembersOverPastDeltaT { get; set; }
        public int Prevalence { get; set; }
        public bool IfCollectAccumulatedIncidence { get; set; } = false;
        public int AccumulatedIncidence { get; set; }
        public int AccumulatedIncidenceAfterWarmUp { get; set; }
        
        // health and cost outcomes
        public DeltaTCostHealth DeltaCostHealthCollector { get; set; }        

        // time-series
        public IncidenceTimeSeries IncidenceTimeSeries { get; set; }
        public PrevalenceTimeSeries PrevalenceTimeSeries { get; set; }
        public PrevalenceTimeSeries AccumIncidenceTimeSeries { get; set; }
        // average prevalence
        public ObservationBasedStatistics AveragePrevalenceStat { get; set; }


        public ClassStatistics(bool showStatsInSummaryResults, int warmUpSimIndex, bool collectAccumulatedIncidence)
        {
            _warmUpSimIndex = warmUpSimIndex;

            IfCollectAccumulatedIncidence = collectAccumulatedIncidence;

            if (showStatsInSummaryResults)
            {
                AveragePrevalenceStat = new ObservationBasedStatistics("Average prevalence");
            }
        }

        public void AddTimeSeries(bool collectIncidence, bool collectPrevalence, bool collectAccumIncidence, int nDeltaTInSimPeriod)
        {
            if (collectIncidence)
                IncidenceTimeSeries = new IncidenceTimeSeries(nDeltaTInSimPeriod);
            if (collectPrevalence)
                PrevalenceTimeSeries = new PrevalenceTimeSeries(nDeltaTInSimPeriod);
            if (collectAccumIncidence)
                AccumIncidenceTimeSeries = new PrevalenceTimeSeries(nDeltaTInSimPeriod);
        }

        public void AddCostHealthOutcomes(
            int warmUpSimIndex,
            double DALYPerNewMember,
            double costPerNewMember,
            double disabilityWeightPerDeltaT,
            double costPerDeltaT)
        {
            DeltaCostHealthCollector = new DeltaTCostHealth(warmUpSimIndex, DALYPerNewMember, costPerNewMember, disabilityWeightPerDeltaT, costPerDeltaT);
        }

        public void Add(int value)
        {
            NumOfNewMembersOverPastDeltaT += value;
            Prevalence += value;                       
        }

        public void CollectEndOfDeltaTStats(int simIndex)
        {
            // accumulated incidence
            if (IfCollectAccumulatedIncidence)
            {
                AccumulatedIncidence += NumOfNewMembersOverPastDeltaT;
            }

            if (simIndex >= _warmUpSimIndex)
                AccumulatedIncidenceAfterWarmUp += NumOfNewMembersOverPastDeltaT;

            // average prevalence
            if (!(AveragePrevalenceStat is null))
                    AveragePrevalenceStat.Record(Prevalence);

            // time series
            if (!(IncidenceTimeSeries is null))
                IncidenceTimeSeries.Record(NumOfNewMembersOverPastDeltaT);
            if ((!(PrevalenceTimeSeries is null)))
                PrevalenceTimeSeries.Record(Prevalence);
            if ((!(AccumIncidenceTimeSeries is null)))
                AccumIncidenceTimeSeries.Record(AccumulatedIncidence);

            // cost and health outcomes
            DeltaCostHealthCollector.Update(simIndex, Prevalence, NumOfNewMembersOverPastDeltaT);

        }

        public void Reset()
        {
            NumOfNewMembersOverPastDeltaT = 0;
            Prevalence = 0;
            AccumulatedIncidence=0;
            if (IncidenceTimeSeries != null)
                IncidenceTimeSeries.Reset();
            if (PrevalenceTimeSeries != null)
                PrevalenceTimeSeries.Reset();
            if (AccumIncidenceTimeSeries != null)
                AccumIncidenceTimeSeries.Reset();
            DeltaCostHealthCollector.Reset();
        }
    }


    // Counter Statistics
    public class CounterStatistics
    {
        // Variables
        #region Variables               

        // Fields
        string _name;
        OldTimeSeries _timeSeries;
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
        public CounterStatistics(string name, double QALYLossPerCount, double healthQualityPerUnitOfTime, 
            double costPerCount, double costPerUnitOfTime, bool collectTotalCount)
        {
            _name = name;
            _QALYLossPerCount = QALYLossPerCount;
            _healthQualityPerUnitOfTime = healthQualityPerUnitOfTime;
            _costPerCount = costPerCount;
            _costPerUnitOfTime = costPerUnitOfTime;
            _collectTotalCount = collectTotalCount;
        }
        public CounterStatistics(string name, double QALYLossPerCount, double healthQualityPerUnitOfTime, double costPerCount, double costPerUnitOfTime,
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
                _timeSeries = new OldTimeSeries(name, numOfPastObsPeriodsToStore + numOfObsPeriodsDelayed, numOfDeltaTInEachObsPeriod, OldTimeSeries.enumPredictionModel.Nothing);
        }  
     
        // create a clone of this class
        public CounterStatistics Clone()
        {
            CounterStatistics clone;
            if (_timeSeries == null)
                clone = new CounterStatistics(_name, _QALYLossPerCount, _healthQualityPerUnitOfTime, _costPerCount, _costPerUnitOfTime, _collectTotalCount);            
            else
                clone = new CounterStatistics(_name, _QALYLossPerCount, _healthQualityPerUnitOfTime, _costPerCount, _costPerUnitOfTime,
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
