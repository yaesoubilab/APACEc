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
        }

        public abstract void Record(double value);

        /// <returns> the last period observation </returns>
        public double GetLastObs()
        {
            return ObsList.Last();
        }
        public double GetLastObs(int nPeriodDelay)
        {
            return ObsList[ObsList.Count - nPeriodDelay];
        }

        public abstract void Reset();
    }

    public class IncidenceTimeSeries: TimeSeries
    {
        public IncidenceTimeSeries(int nOfRecodingsInEachPeriod):base(nOfRecodingsInEachPeriod)
        {
            ObsList.Add(0); // 0 added for the interval 0
        }

        public override void Record(double value)
        {
            // find if a new element should be added to the list
            if (_nRecordingsInThisPeriod % _nRecodingsInEachPeriod == 0)
            {
                ObsList.Add(value);
                _nRecordingsInThisPeriod = 1;
            }
            else
            {
                // increment the observation in the current period
                ObsList[ObsList.Count-1] += value;
                ++_nRecordingsInThisPeriod;
            }
        }

        public override void Reset()
        {
            ObsList.Clear();
            ObsList.Add(0); // 0 added for the interval 0
            _nRecordingsInThisPeriod = 0;
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
                _nRecordingsInThisPeriod = 1;
            }
            else
                ++_nRecordingsInThisPeriod;
        }

        public override void Reset()
        {
            ObsList.Clear();
            _nRecordingsInThisPeriod = 0;
        }
    }

    public class OneDimTrajectory
    {
        protected int _warmUpSimIndex;

        // statistics 
        public int ID { get; set; }
        public string Name { get; set; }

        public bool CollectAccumIncidenceStats { get; set; } = false;
        public bool CollectPrevalenceStats { get; set; } = false;

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
        // average incidence prevalence
        public ObsBasedStat AveragePrevalenceStat { get; set; }

        public OneDimTrajectory(int id, string name, int warmUpSimIndex)
        {
            ID = id;
            Name = name;
            _warmUpSimIndex = warmUpSimIndex;
        }

        public void SetupStatisticsCollectors(bool accumIncidence, bool prevalence)
        {
            CollectAccumIncidenceStats = accumIncidence;
            CollectPrevalenceStats = prevalence;
            if (CollectPrevalenceStats)
                AveragePrevalenceStat = new ObsBasedStat("Average prevalence");
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
            if (CollectAccumIncidenceStats)
            {
                AccumulatedIncidence += NumOfNewMembersOverPastPeriod;
                if (simIndex >= _warmUpSimIndex)
                    AccumulatedIncidenceAfterWarmUp += NumOfNewMembersOverPastPeriod;
            }

            // average prevalence
            if (CollectPrevalenceStats)
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

    public abstract class SumTrajectory : OneDimTrajectory
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
                    SetupStatisticsCollectors(
                        accumIncidence: true,
                        prevalence: false
                        );
                    AddTimeSeries(
                        collectIncidence: true,
                        collectPrevalence: false,
                        collectAccumIncidence: false,
                        nDeltaTInAPeriod: nDeltaTInAPeriod
                        );
                    break;

                case EnumType.AccumulatingIncident:
                    SetupStatisticsCollectors(
                        accumIncidence: true,
                        prevalence: false
                        );
                    AddTimeSeries(
                        collectIncidence: false,
                        collectPrevalence: false,
                        collectAccumIncidence: false,
                        nDeltaTInAPeriod: nDeltaTInAPeriod
                        );
                    break;

                case EnumType.Prevalence:
                    SetupStatisticsCollectors(
                        accumIncidence: false,
                        prevalence: true
                        );
                    AddTimeSeries(
                        collectIncidence: false,
                        collectPrevalence: false,
                        collectAccumIncidence: false,
                        nDeltaTInAPeriod: nDeltaTInAPeriod
                        );
                    break;
            }
        }

        public virtual void Add(int simIndex, ref List<Class> classes, ref List<Event> events) { return; }

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
        public int[] ClassIDs { get; private set; }

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
            ClassIDs = ConvertSumFormulaToArrayOfIDs(sumFormula);
        }

        public override void Add(int simIndex, ref List<Class> classes, ref List<Event> events)
        {
            switch (Type)
            {
                case EnumType.Incidence:
                case EnumType.AccumulatingIncident:
                    {
                        NumOfNewMembersOverPastPeriod = 0;
                        for (int i = 0; i < ClassIDs.Length; ++i)
                            NumOfNewMembersOverPastPeriod += classes[ClassIDs[i]].ClassStat.NumOfNewMembersOverPastPeriod;
                    }
                    break;
                case EnumType.Prevalence:
                    {
                        Prevalence = 0;
                        for (int i = 0; i < ClassIDs.Length; ++i)
                            Prevalence += classes[ClassIDs[i]].ClassStat.Prevalence;
                    }
                    break;
            }

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

        public override void Add(int simIndex, ref List<Class> classes, ref List<Event> events)
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
        public ObsBasedStat AveragePrevalenceStat { get; set; }

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
                AveragePrevalenceStat = new ObsBasedStat("Average prevalence");
        }

        public void Add(int simIndex, ref List<SumTrajectory> sumTrajectories)
        {
            double ratio = double.NaN;
            switch (Type)
            {
                case EnumType.PrevalenceOverPrevalence:
                    {
                        ratio = (double)sumTrajectories[_nominatorSpecialStatID].Prevalence
                            / (double)sumTrajectories[_denominatorSpecialStatID].Prevalence;
                        TimeSeries.Record(ratio);

                        if (simIndex >= _warmUpSimIndex)
                            AveragePrevalenceStat.Record(ratio);
                    }
                    break;
            }          
                                             
        }
        // convert ratio formula into the array of class IDs
        private int[] ConvertRatioFormulaToArrayOfClassIDs(string formula)
        {
            string[] arrClassIDs = formula.Split('/');
            return Array.ConvertAll<string, int>(arrClassIDs, Convert.ToInt32);
        }

        public void Reset()
        {
            TimeSeries.Reset();
            if (Type == EnumType.PrevalenceOverPrevalence)
                AveragePrevalenceStat.Reset();
        }
    }

    public class SurveyedIncidenceTrajectory
    {
        int _nObsPeriodsDelay;
        public bool FirstObsMarksStartOfEpidemic { get; private set; }
        public IncidenceTimeSeries IncidenceTimeSeries { get; set; }

        public SurveyedIncidenceTrajectory(
            int IDofSpecialStats,
            string name,
            int nDeltaTsObsPeriod,
            int nDeltaTsDelayed)
        {
            _nObsPeriodsDelay = nDeltaTsDelayed / nDeltaTsObsPeriod;
        }

        public int GetLastObs()
        {
            return (int)IncidenceTimeSeries.GetLastObs(_nObsPeriodsDelay);
        }
    }
    
    public class SurveyedRatioTrajectory : RatioTrajectory
    {
        int _nObsPeriodsDelay;
        bool _firstObsMarksStartOfEpidemic;

        public SurveyedRatioTrajectory(
            int ID,
            string name,
            EnumType type,
            string ratioFormula,
            bool displayInSimOutput,
            int warmUpSimIndex,
            int nDeltaTsObsPeriod,
            int nDeltaTsDelayed) 
            : base(ID, name, type, ratioFormula, displayInSimOutput, warmUpSimIndex, nDeltaTsObsPeriod)
        {
            _nObsPeriodsDelay = nDeltaTsDelayed / nDeltaTsObsPeriod;
        }

        public double GetLastObs()
        {
            return TimeSeries.GetLastObs(_nObsPeriodsDelay);
        }
    }

    public class SimOutputTrajs
    {
        private int _simReplication;
        private double _deltaT;
        private int _nDeltaTInSimOutputInterval;
        private int _nextSimTimeIndexToStore; // for visualization
        private DecisionMaker _decisionMaker;
        private List<Class> _classes;
        private List<SumTrajectory> _sumTrajectories;
        private List<RatioTrajectory> _ratioTrajectories;

        public String[] PrevalenceOutputsHeader = new string[0];
        public String[] IncidenceOutputsHeader = new string[0];

        public int NumOfPrevalenceOutputsToReport { get; set; }
        public int NumOfIncidenceOutputsToReport { get; set; }

        public int[][] SimRepIndeces { get; private set; }
        public double[][] SimIncidenceOutputs { get; private set; }
        public double[][] SimPrevalenceOutputs { get; private set; }
        public int[][] InterventionCombinations { get; private set; }

        public SimOutputTrajs(
            int simReplication, 
            double deltaT,
            int nDeltaTInSimOutputInterval,
            ref DecisionMaker decisionMaker,
            ref List<Class> classes,
            ref List<SumTrajectory> sumTrajectories,
            ref List<RatioTrajectory> ratioTrajectories, 
            bool findHeader = false)
        {
            _simReplication = simReplication;
            _deltaT = deltaT;
            _nDeltaTInSimOutputInterval = nDeltaTInSimOutputInterval;
            _decisionMaker = decisionMaker;
            _classes = classes;
            _sumTrajectories = sumTrajectories;
            _ratioTrajectories = ratioTrajectories;        

            _nextSimTimeIndexToStore = 0;

            // find number of incidence and prevalence outputs to report 
            FindNumOfOutputsAndHeaders(findHeader); 
        }

        // store selected outputs while simulating
        public void Record(int simTimeIndex, bool endOfSim)
        {
            // check if it is time to store output
            if (simTimeIndex < _nextSimTimeIndexToStore && !endOfSim)
                return;

            // define the jagged array to store current observation
            int[][] thisSimRepIndeces = new int[1][];
            thisSimRepIndeces[0] = new int[1];
            double[][] thisIncidenceOutputs = new double[1][];
            double[][] thisPrevalenceOutputs = new double[1][];
            int[][] thisActionCombination = new int[1][];

            int colIndexPrevalenceOutputs = 0;
            int colIndexIncidenceOutputs = 0;            
            thisPrevalenceOutputs[0] = new double[NumOfPrevalenceOutputsToReport];
            thisIncidenceOutputs[0] = new double[NumOfIncidenceOutputsToReport];

            // store the current time and the current interval
            thisSimRepIndeces[0][0] = _simReplication;
            thisIncidenceOutputs[0][colIndexIncidenceOutputs++] 
                = Math.Floor((double)(simTimeIndex-1) / _nDeltaTInSimOutputInterval) + 1;
            thisPrevalenceOutputs[0][colIndexPrevalenceOutputs++] = simTimeIndex * _deltaT;

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
                thisPrevalenceOutputs[0][colIndexPrevalenceOutputs++] = thisRatioTraj.TimeSeries.GetLastObs();
            }

            // action combination
            thisActionCombination[0] = (int[])_decisionMaker.CurrentDecision.Clone();

            // concatenate this row 
            SimRepIndeces = SupportFunctions.ConcatJaggedArray(SimRepIndeces, thisSimRepIndeces);
            SimIncidenceOutputs = SupportFunctions.ConcatJaggedArray(SimIncidenceOutputs, thisIncidenceOutputs);
            SimPrevalenceOutputs = SupportFunctions.ConcatJaggedArray(SimPrevalenceOutputs, thisPrevalenceOutputs);
            InterventionCombinations = SupportFunctions.ConcatJaggedArray(InterventionCombinations, thisActionCombination);

            // find next time index to store trajectories
            _nextSimTimeIndexToStore += _nDeltaTInSimOutputInterval;
        }

        // get header
        private void FindNumOfOutputsAndHeaders(bool storeHeaders)
        {
            // define the header
            PrevalenceOutputsHeader = new string[0];
            IncidenceOutputsHeader = new string[0];

            // create headers
            NumOfIncidenceOutputsToReport = 1;
            if (storeHeaders) SupportFunctions.AddToEndOfArray(ref IncidenceOutputsHeader, "Simulation Period");
            NumOfPrevalenceOutputsToReport = 1;
            if (storeHeaders) SupportFunctions.AddToEndOfArray(ref PrevalenceOutputsHeader, "Simulation Time");

            // class headers
            foreach (Class thisClass in _classes)
            {
                if (thisClass.ShowIncidence)
                {
                    if (storeHeaders) SupportFunctions.AddToEndOfArray(ref IncidenceOutputsHeader, "To: " + thisClass.Name);
                    ++NumOfIncidenceOutputsToReport;
                }
                if (thisClass.ShowPrevalence)
                {
                    if (storeHeaders) SupportFunctions.AddToEndOfArray(ref PrevalenceOutputsHeader, "In: " + thisClass.Name);
                    ++NumOfPrevalenceOutputsToReport;
                }
                if (thisClass.ShowAccumIncidence)
                {
                    if (storeHeaders) SupportFunctions.AddToEndOfArray(ref PrevalenceOutputsHeader, "Sum To: " + thisClass.Name);
                    ++NumOfPrevalenceOutputsToReport;
                }
            }
            // summation statistics header
            foreach (SumTrajectory thisSumTraj in _sumTrajectories.Where(s => s.DisplayInSimOutput))
            {
                switch (thisSumTraj.Type)
                {
                    case SumTrajectory.EnumType.Incidence:
                        {
                            if (storeHeaders) SupportFunctions.AddToEndOfArray(ref IncidenceOutputsHeader, thisSumTraj.Name);
                            ++NumOfIncidenceOutputsToReport;
                        }
                        break;
                    case SumTrajectory.EnumType.AccumulatingIncident:
                    case SumTrajectory.EnumType.Prevalence:
                        {
                            if (storeHeaders) SupportFunctions.AddToEndOfArray(ref PrevalenceOutputsHeader, thisSumTraj.Name);
                            ++NumOfPrevalenceOutputsToReport;
                        }
                        break;
                }
            }
            // ratio statistics
            foreach (RatioTrajectory thisRatioTaj in _ratioTrajectories.Where(s => s.DisplayInSimOutput))
            {
                if (storeHeaders) SupportFunctions.AddToEndOfArray(ref PrevalenceOutputsHeader, thisRatioTaj.Name);
                ++NumOfPrevalenceOutputsToReport;
            }

        }

        public void Reset()
        {            
            _nextSimTimeIndexToStore = 0;
            SimRepIndeces = new int[0][];
            InterventionCombinations = new int[0][];
            SimPrevalenceOutputs = new double[0][];
            SimIncidenceOutputs = new double[0][];
        }
    }

    public class EpidemicHistory
    {
        // summation and ratio trajectories
        public List<SumTrajectory> _sumTrajs = new List<SumTrajectory>();
        public List<RatioTrajectory> _ratioTraj = new List<RatioTrajectory>();
        public List<SumTrajectory> SumTrajs { get => _sumTrajs; set => _sumTrajs = value; }
        public List<RatioTrajectory> RatioTrajs { get => _ratioTraj; set => _ratioTraj = value; }
        // surveyed summation and ratio trajectories
        public List<IncidenceTimeSeries> _survSumClassTimeSeries = new List<IncidenceTimeSeries>();

        public List<SurveyedIncidenceTrajectory> _survSumClassTrajs = new List<SurveyedIncidenceTrajectory>();
        public List<SurveyedRatioTrajectory> _survRatioTrajs = new List<SurveyedRatioTrajectory>();
        public List<SurveyedIncidenceTrajectory> SurveyedSumTrajs { get => _survSumClassTrajs; set => _survSumClassTrajs = value; }
        public List<SurveyedRatioTrajectory> SurveyedRatioTrajs { get => _survRatioTrajs; set => _survRatioTrajs = value; }
        // all trajectories prepared for simulation output 
        public SimOutputTrajs TrajsForSimOutput { get; private set; }
        public SimOutputTrajs SurveyedTrajsForSimOutput { get; private set; }

        public EpidemicHistory()
        {
        }

        public void SetupTrajsForSimOutput(
            int ID,
            double deltaT,
            int numOfDeltaT_inSimOutputInterval,
            ref DecisionMaker decisionMaker,
            ref List<Class> classes,
            bool extractOutputHeaders)
        {
            TrajsForSimOutput = new SimOutputTrajs(
               ID,
               deltaT,
               numOfDeltaT_inSimOutputInterval,
               ref decisionMaker,
               ref classes,
               ref _sumTrajs,
               ref _ratioTraj,
               extractOutputHeaders);
        }

        public void UpdateSumAndRatioTrajs(int simTimeIndex, ref List<Class> classes, ref List<Event> events)
        {
            // update summation statistics
            foreach (SumTrajectory thisSumTaj in SumTrajs)
                thisSumTaj.Add(simTimeIndex, ref classes, ref events);
            // update ratio statistics
            foreach (RatioTrajectory ratioTraj in RatioTrajs)
                ratioTraj.Add(simTimeIndex, ref _sumTrajs);
        }

        public void Reset(int simTimeIndex, ref List<Class> classes, ref List<Event> events)
        {
            TrajsForSimOutput.Reset();

            // update summation statistics
            foreach (SumTrajectory sumTraj in SumTrajs)
            {
                sumTraj.Reset();
                if (sumTraj.Type == SumTrajectory.EnumType.Prevalence)
                    sumTraj.Add(simTimeIndex, ref classes, ref events);
            }
            // update ratio statistics
            foreach (RatioTrajectory ratioTraj in RatioTrajs)
            {
                ratioTraj.Reset();
                ratioTraj.Add(simTimeIndex, ref _sumTrajs);
            }
        }

        public void Clean()
        {
            SumTrajs = new List<SumTrajectory>();
            RatioTrajs = new List<RatioTrajectory>();
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
