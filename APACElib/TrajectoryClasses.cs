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
        protected int _nRecordingsInThisPeriod = 0;
        protected int _nRecodingsInEachPeriod;

        public TimeSeries(int nRecodingsInEachPeriod)
        {
            _nRecodingsInEachPeriod = nRecodingsInEachPeriod;
            ObsList.Add(0); // 0 added for the interval 0
        }

        public abstract void Record(double value);

        /// <returns> the last period observation </returns>
        public double GetLastObs()
        {
            return ObsList.Last();
        }

        public void Reset()
        {
            ObsList.Clear();
            ObsList.Add(0); // 0 added for the interval 0
            _nRecordingsInThisPeriod = 0;
        }
    }

    public class IncidenceTimeSeries: TimeSeries
    {
        public IncidenceTimeSeries(int nOfRecodingsInEachPeriod):base(nOfRecodingsInEachPeriod)
        {
        }

        public override void Record(double value)
        {
            // find if a new element should be added to the list
            if (_nRecordingsInThisPeriod % _nRecodingsInEachPeriod == 0)
            {
                ObsList.Add(value);
                ++_nRecordingsInThisPeriod;
            }
            else
            {
                // increment the observation in the current period
                ObsList[ObsList.Count-1] += value;
            }
        }
    }
     
    public class PrevalenceTimeSeries : TimeSeries
    {
        public PrevalenceTimeSeries(int nRecodingsInEachObsPeriod) : base(nRecodingsInEachObsPeriod)
        {
        }

        public override void Record(double value)
        {
            // find if a new element should be added to the list
            if (_nRecordingsInThisPeriod % _nRecodingsInEachPeriod == 0)
            {
                ObsList.Add(value);
                ++_nRecordingsInThisPeriod;
            }
        }
    }

    public class GeneralTrajectory
    {
        protected int _warmUpSimIndex;

        // statistics 
        public int ID { get; set; }
        public string Name { get; set; }

        public bool IfCollectAccumulatedIncidence { get; set; } = false;
        public bool IfCalculateAvePrevalence { get; set; } = false;

        public int NumOfNewMembersOverPastPeriod { get; set; }
        public int Prevalence { get; set; }
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

        public GeneralTrajectory(int id, string name, int warmUpSimIndex)
        {
            ID = id;
            Name = name;
            _warmUpSimIndex = warmUpSimIndex;
        }

        public void SetupAvePrevalenceAndAccumIncidence(bool collectAccumulatedIncidence, bool calculateAvePrevalence)
        {
            IfCollectAccumulatedIncidence = collectAccumulatedIncidence;
            IfCalculateAvePrevalence = calculateAvePrevalence;
            if (IfCalculateAvePrevalence)
                AveragePrevalenceStat = new ObservationBasedStatistics("Average prevalence");
        }

        public void AddTimeSeries(bool collectIncidence, bool collectPrevalence, bool collectAccumIncidence, int nDeltaTInAPeriod)
        {
            if (collectIncidence)
                IncidenceTimeSeries = new IncidenceTimeSeries(nDeltaTInAPeriod);
            if (collectPrevalence)
                PrevalenceTimeSeries = new PrevalenceTimeSeries(nDeltaTInAPeriod);
            if (collectAccumIncidence)
                AccumIncidenceTimeSeries = new PrevalenceTimeSeries(nDeltaTInAPeriod);
        }

        public void AddCostHealthOutcomes(
            double DALYPerNewMember,
            double costPerNewMember,
            double disabilityWeightPerDeltaT,
            double costPerDeltaT)
        {
            DeltaCostHealthCollector = new DeltaTCostHealth(_warmUpSimIndex, DALYPerNewMember, costPerNewMember, disabilityWeightPerDeltaT, costPerDeltaT);
        }

        public void Add(int value)
        {
            NumOfNewMembersOverPastPeriod += value;
            Prevalence += value;                       
        }

        public void CollectEndOfDeltaTStats(int simIndex)
        {
            // accumulated incidence
            if (IfCollectAccumulatedIncidence)
            {
                AccumulatedIncidence += NumOfNewMembersOverPastPeriod;
                if (simIndex >= _warmUpSimIndex)
                    AccumulatedIncidenceAfterWarmUp += NumOfNewMembersOverPastPeriod;
            }

            // average prevalence
            if (IfCalculateAvePrevalence)
                    AveragePrevalenceStat.Record(Prevalence);

            // time series
            if (!(IncidenceTimeSeries is null))
                IncidenceTimeSeries.Record(NumOfNewMembersOverPastPeriod);
            if ((!(PrevalenceTimeSeries is null)))
                PrevalenceTimeSeries.Record(Prevalence);
            if ((!(AccumIncidenceTimeSeries is null)))
                AccumIncidenceTimeSeries.Record(AccumulatedIncidence);

            // cost and health outcomes
            DeltaCostHealthCollector.Update(simIndex, Prevalence, NumOfNewMembersOverPastPeriod);

        }

        public void Reset()
        {
            NumOfNewMembersOverPastPeriod = 0;
            Prevalence = 0;
            AccumulatedIncidence = 0;
            AccumulatedIncidenceAfterWarmUp = 0;
            if (IncidenceTimeSeries != null)
                IncidenceTimeSeries.Reset();
            if (PrevalenceTimeSeries != null)
                PrevalenceTimeSeries.Reset();
            if (AccumIncidenceTimeSeries != null)
                AccumIncidenceTimeSeries.Reset();
            DeltaCostHealthCollector.Reset();
        }
        
    }

    public abstract class SumTrajectory : GeneralTrajectory
    {
        public enum EnumType
        {
            Incidence = 0,
            AccumulatingIncident = 1,
            Prevalence = 2,
        }
        public enum EnumDefinedOn
        {
            Classes = 0,
            Events = 1,
        }

        public Boolean DisplayInSimOutput { get; set; }
        public EnumType Type { get; set; }
        public TrajectoryCalibrationInfo CalibInfo { get; set; }

        public SumTrajectory(
            int ID,
            string name,
            EnumType type,
            bool displayInSimOutput,
            int warmUpSimIndex,
            int nDeltaTInAPeriod)
            : base(ID, name, warmUpSimIndex)
        {
            Type = type;
            DisplayInSimOutput = displayInSimOutput;
            switch (Type)
            {
                case EnumType.Incidence:
                    SetupAvePrevalenceAndAccumIncidence(
                        collectAccumulatedIncidence: false,
                        calculateAvePrevalence: false);
                    AddTimeSeries(
                        collectIncidence: true,
                        collectPrevalence: false,
                        collectAccumIncidence: false,
                        nDeltaTInAPeriod: nDeltaTInAPeriod);
                    break;
                case EnumType.AccumulatingIncident:
                    SetupAvePrevalenceAndAccumIncidence(
                        collectAccumulatedIncidence: true,
                        calculateAvePrevalence: false);
                    AddTimeSeries(
                        collectIncidence: false,
                        collectPrevalence: false,
                        collectAccumIncidence: true,
                        nDeltaTInAPeriod: nDeltaTInAPeriod);
                    break;
                case EnumType.Prevalence:
                    SetupAvePrevalenceAndAccumIncidence(
                        collectAccumulatedIncidence: false, 
                        calculateAvePrevalence: true);
                    AddTimeSeries(
                        collectIncidence: false,
                        collectPrevalence: true,
                        collectAccumIncidence: false,
                        nDeltaTInAPeriod: nDeltaTInAPeriod);
                    break;
            }
        }

        public int GetLastObs()
        {
            int value = 0;
            switch (Type)
            {
                case EnumType.Incidence:
                    value = (int)IncidenceTimeSeries.GetLastObs();
                    break;
                case EnumType.AccumulatingIncident:
                    value = AccumulatedIncidenceAfterWarmUp;
                    break;
                case EnumType.Prevalence:
                    value = (int)PrevalenceTimeSeries.GetLastObs();
                    break;
            }
            return value;
        }

        // convert sum formula into the array of class IDs or event IDs
        protected int[] ConvertSumFormulaToArrayOfIDs(string formula)
        {
            string[] arrClassIDs = formula.Split('+');
            return Array.ConvertAll<string, int>(arrClassIDs, Convert.ToInt32);
        }

    }

    public class SumClassesTrajectory: SumTrajectory
    {
        int[] _arrClassIDs;

        public SumClassesTrajectory(
            int ID,
            string name,
            EnumType type,
            string sumFormula,
            bool displayInSimOutput,
            int warmUpSimIndex,
            int nDeltaTInAPeriod) 
            :base(ID, name, type, displayInSimOutput, warmUpSimIndex, nDeltaTInAPeriod)
        {           
            _arrClassIDs = ConvertSumFormulaToArrayOfIDs(sumFormula);
        }

        public void Add(int simIndex, ref List<Class> classes)
        {
            NumOfNewMembersOverPastPeriod = 0;
            for (int i = 0; i < _arrClassIDs.Length; ++i)
                NumOfNewMembersOverPastPeriod += classes[_arrClassIDs[i]].ClassStat.NumOfNewMembersOverPastPeriod;

            CollectEndOfDeltaTStats(simIndex);
        }        
    }

    public class SumEventTrajectory: SumTrajectory
    {
        int[] _arrEventIDs;

        public SumEventTrajectory(
            int ID,
            string name,
             EnumType type,
            string sumFormula,
            bool displayInSimOutput,
            int warmUpSimIndex,
            int nDeltaTInAPeriod) 
            :base(ID, name, type, displayInSimOutput, warmUpSimIndex, nDeltaTInAPeriod)
        {
            _arrEventIDs = ConvertSumFormulaToArrayOfIDs(sumFormula);
        }

        public void Add(int simIndex, ref List<Event> events)
        {
            NumOfNewMembersOverPastPeriod = 0;
            for (int i = 0; i < _arrEventIDs.Length; ++i)
                NumOfNewMembersOverPastPeriod += events[_arrEventIDs[i]].MembersOutOverPastDeltaT;

            CollectEndOfDeltaTStats(simIndex);
        }
    }    

    public class RatioTrajectory
    {
        public enum EnumType
        {
            IncidenceOverIncidence = 0,
            AccumulatedIncidenceOverAccumulatedIncidence = 1,
            PrevalenceOverPrevalence = 2,
            IncidenceOverPrevalence = 3,
        }
        public TrajectoryCalibrationInfo CalibInfo { get; set; }

        public int ID { get; }
        public string Name { get; set; }
        public Boolean DisplayInSimOutput { get; set; }
        public PrevalenceTimeSeries TimeSeries { get; set; }
        public ObservationBasedStatistics AveragePrevalenceStat { get; set; }

        public EnumType Type { get; set; }
        int _nominatorSpecialStatID;
        int _denominatorSpecialStatID;
        int _warmUpSimIndex;

        public RatioTrajectory(
            int id,
            string name,
            EnumType type,
            string ratioFormula,
            bool displayInSimOutput,
            int warmUpSimIndex,
            int nDeltaTInAPeriod)            
        {
            ID = id;
            Name = name;
            Type = type;
            DisplayInSimOutput = displayInSimOutput;
            int[] arrRatio = ConvertRatioFormulaToArrayOfClassIDs(ratioFormula);
            _nominatorSpecialStatID = arrRatio[0];
            _denominatorSpecialStatID = arrRatio[1];
            _warmUpSimIndex = warmUpSimIndex;

            TimeSeries = new PrevalenceTimeSeries(nDeltaTInAPeriod);
            
            if (Type == EnumType.PrevalenceOverPrevalence)
                AveragePrevalenceStat = new ObservationBasedStatistics("Average prevalence");
        }

        public void Add(int simIndex, ref List<SumTrajectory> sumTrajectories)
        {
            double ratio = sumTrajectories[_nominatorSpecialStatID].GetLastObs()
                    / sumTrajectories[_denominatorSpecialStatID].GetLastObs();
            TimeSeries.Record(ratio);

            if (simIndex >= _warmUpSimIndex)
            {
                if (Type == EnumType.PrevalenceOverPrevalence)
                    AveragePrevalenceStat.Record(ratio);
            }                        
        }
        // convert ratio formula into the array of class IDs
        private int[] ConvertRatioFormulaToArrayOfClassIDs(string formula)
        {
            string[] arrClassIDs = formula.Split('/');
            return Array.ConvertAll<string, int>(arrClassIDs, Convert.ToInt32);
        }
    }


    public class ObservedTrajectories
    {

        public void Record (int simTimeIndex)
        {
            double[][] thisCalibrationObservation = new double[1][];
            double[][] thisTimeOfObservableOutputs = new double[1][];
            //int colIndexObservableOutputs = 0;
            //int colIndexCalibrationObservation = 0;
        }
    }

    public class SimulationTrajectories
    {
        private int _simReplication;
        private double _deltaT;
        private int _nDeltaTInSimOutputInterval;
        private int _nextSimTimeIndexToStore; // for visualization
        private DecisionMaker _decisionMaker;
        private List<Class> _classes;
        private List<SumTrajectory> _sumTrajectories;
        private List<RatioTrajectory> _ratioTrajectories;

        public int NumOfPrevalenceOutputsToReport { get; set; }
        public int NumOfIncidenceOutputsToReport { get; set; }

        private int[][] _interventionCombinations;
        private double[][] _simPrevalenceOutputs;
        private double[][] _simIncidenceOutputs;

        public SimulationTrajectories(
            int simReplication, 
            double deltaT,
            int nDeltaTInSimOutputInterval,
            ref DecisionMaker decisionMaker,
            ref List<Class> classes,
            ref List<SumTrajectory> sumTrajectories,
            ref List<RatioTrajectory> ratioTrajectories)
        {
            _simReplication = simReplication;
            _deltaT = deltaT;
            _nDeltaTInSimOutputInterval = nDeltaTInSimOutputInterval;
            _decisionMaker = decisionMaker;
            _classes = classes;
            _sumTrajectories = sumTrajectories;
            _ratioTrajectories = ratioTrajectories;        

            _nextSimTimeIndexToStore = 0;
        }

        // store selected outputs while simulating
        public void Record(int simTimeIndex)
        {
            // check if it is time to store output
            if (simTimeIndex < _nextSimTimeIndexToStore)
                return;

            // define the jagged array to store current observation
            int[][] thisActionCombination = new int[1][];
            double[][] thisPrevalenceOutputs = new double[1][];
            double[][] thisIncidenceOutputs = new double[1][];                        
            int colIndexPrevalenceOutputs = 0;
            int colIndexIncidenceOutputs = 0;
            
            thisPrevalenceOutputs[0] = new double[NumOfPrevalenceOutputsToReport];
            thisIncidenceOutputs[0] = new double[NumOfIncidenceOutputsToReport];

            // store the current time and the current interval
            thisPrevalenceOutputs[0][colIndexPrevalenceOutputs++] = _simReplication;
            thisPrevalenceOutputs[0][colIndexPrevalenceOutputs++] = simTimeIndex * _deltaT;
            thisIncidenceOutputs[0][colIndexIncidenceOutputs++] = simTimeIndex / _nDeltaTInSimOutputInterval;

            // action combination
            thisActionCombination[0] = (int[])_decisionMaker.CurrentDecision.Clone();

            // classes
            foreach (Class thisClass in _classes)
            {
                if (thisClass.ShowIncidence)
                    thisIncidenceOutputs[0][colIndexIncidenceOutputs++] = thisClass.ClassStat.IncidenceTimeSeries.GetLastObs();
                if (thisClass.ShowPrevalence)
                    thisPrevalenceOutputs[0][colIndexPrevalenceOutputs++] = thisClass.ClassStat.Prevalence;
                if (thisClass.ShowAccumIncidence)
                    thisPrevalenceOutputs[0][colIndexPrevalenceOutputs++] = thisClass.ClassStat.AccumulatedIncidence;
            }

            // summation statistics
            foreach (SumTrajectory thisSumTraj in _sumTrajectories.Where(s => s.DisplayInSimOutput))
            {
                switch (thisSumTraj.Type)
                {
                    case SumTrajectory.EnumType.Incidence:
                        thisIncidenceOutputs[0][colIndexIncidenceOutputs++] = thisSumTraj.IncidenceTimeSeries.GetLastObs();
                        break;
                    case SumTrajectory.EnumType.AccumulatingIncident:
                        thisPrevalenceOutputs[0][colIndexPrevalenceOutputs++] = thisSumTraj.AccumulatedIncidence;
                        break;
                    case SumTrajectory.EnumType.Prevalence:
                        thisPrevalenceOutputs[0][colIndexPrevalenceOutputs++] = thisSumTraj.Prevalence;
                        break;
                }
            }
            // ratio statistics
            foreach (RatioTrajectory thisRatioTraj in _ratioTrajectories.Where(s => s.DisplayInSimOutput))
            {
                thisIncidenceOutputs[0][colIndexIncidenceOutputs++] = thisRatioTraj.TimeSeries.GetLastObs();
            }

            // concatenate this row 
            _simPrevalenceOutputs = SupportFunctions.ConcatJaggedArray(_simPrevalenceOutputs, thisPrevalenceOutputs);
            _simIncidenceOutputs = SupportFunctions.ConcatJaggedArray(_simIncidenceOutputs, thisIncidenceOutputs);
            // record the action combination
            _interventionCombinations = SupportFunctions.ConcatJaggedArray(_interventionCombinations, thisActionCombination);
        }


        // get header
        public void CreateOutputHeaders(ref string[] arrPrevalenceOutputsHeader, ref string[] arrIncidenceOutputsHeader)
        {
            // define the header
            arrPrevalenceOutputsHeader = new string[0];
            arrIncidenceOutputsHeader = new string[0];

            // create headers
            SupportFunctions.AddToEndOfArray(ref arrPrevalenceOutputsHeader, "Simulation Replication");
            SupportFunctions.AddToEndOfArray(ref arrPrevalenceOutputsHeader, "Simulation Time");
            SupportFunctions.AddToEndOfArray(ref arrIncidenceOutputsHeader, "Decision Code");
            SupportFunctions.AddToEndOfArray(ref arrIncidenceOutputsHeader, "Observation Period");

            // class headers
            foreach (Class thisClass in _classes)
            {
                if (thisClass.ShowIncidence)
                    SupportFunctions.AddToEndOfArray(ref arrIncidenceOutputsHeader, "To: " + thisClass.Name);
                if (thisClass.ShowPrevalence)
                    SupportFunctions.AddToEndOfArray(ref arrPrevalenceOutputsHeader, "In: " + thisClass.Name);
                if (thisClass.ShowAccumIncidence)
                    SupportFunctions.AddToEndOfArray(ref arrPrevalenceOutputsHeader, "Sum To: " + thisClass.Name);
            }
            // summation statistics header
            foreach (SumTrajectory thisSumTraj in _sumTrajectories.Where(s => s.DisplayInSimOutput))
            {
                switch (thisSumTraj.Type)
                {
                    case SumTrajectory.EnumType.Incidence:
                        SupportFunctions.AddToEndOfArray(ref arrIncidenceOutputsHeader, thisSumTraj.Name);
                        break;
                    case SumTrajectory.EnumType.AccumulatingIncident:
                    case SumTrajectory.EnumType.Prevalence:
                        SupportFunctions.AddToEndOfArray(ref arrPrevalenceOutputsHeader, thisSumTraj.Name);
                        break;
                }
            }
            // ratio statistics
            foreach (RatioTrajectory thisRatioTaj in _ratioTrajectories.Where(s => s.DisplayInSimOutput))
            {
                // find the type of this ratio statistics
                switch (thisRatioTaj.Type)
                {
                    case RatioTrajectory.EnumType.IncidenceOverIncidence:
                        SupportFunctions.AddToEndOfArray(ref arrIncidenceOutputsHeader, thisRatioTaj.Name);
                        break;
                    case RatioTrajectory.EnumType.AccumulatedIncidenceOverAccumulatedIncidence:
                        SupportFunctions.AddToEndOfArray(ref arrPrevalenceOutputsHeader, thisRatioTaj.Name);
                        break;
                    case RatioTrajectory.EnumType.PrevalenceOverPrevalence:
                        SupportFunctions.AddToEndOfArray(ref arrPrevalenceOutputsHeader, thisRatioTaj.Name);
                        break;
                    case RatioTrajectory.EnumType.IncidenceOverPrevalence:
                        SupportFunctions.AddToEndOfArray(ref arrIncidenceOutputsHeader, thisRatioTaj.Name);
                        break;
                }
            }

            // find the number of outputs to report
            NumOfPrevalenceOutputsToReport = arrPrevalenceOutputsHeader.Length - 2;
            NumOfIncidenceOutputsToReport = arrIncidenceOutputsHeader.Length - 2;
        }

        public void Reset()
        {            
            _nextSimTimeIndexToStore = 0;
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
