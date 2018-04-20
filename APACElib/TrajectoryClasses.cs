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
            return ObsList[ObsList.Count - nPeriodDelay - 1];
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
        public double LastRecordedRatio { get; private set; }

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
            LastRecordedRatio = double.NaN;
            switch (Type)
            {
                case EnumType.PrevalenceOverPrevalence:
                    LastRecordedRatio = (double)sumTrajectories[_nominatorSpecialStatID].Prevalence
                        / (double)sumTrajectories[_denominatorSpecialStatID].Prevalence;
                    break;
                case EnumType.AccumulatedIncidenceOverAccumulatedIncidence:
                    LastRecordedRatio = (double)sumTrajectories[_nominatorSpecialStatID].AccumulatedIncidenceAfterWarmUp
                        / (double)sumTrajectories[_denominatorSpecialStatID].AccumulatedIncidenceAfterWarmUp;
                    break;
            }

            TimeSeries.Record(LastRecordedRatio);
            if (simIndex >= _warmUpSimIndex)
                AveragePrevalenceStat.Record(LastRecordedRatio);

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

    public abstract class SurveyedTrajectory
    {
        public string Name { get; private set; }
        public bool DisplayInSimOut { get; private set; }
        protected int _nObsPeriodsDelay;
        protected int _nDeltaTsObsPeriod;
        public bool FirstObsMarksStartOfEpidemic { get; private set; }

        public SurveyedTrajectory(
            string name,
            bool displayInSimOutput,
            bool firstObsMarksStartOfEpidemic,
            int nDeltaTsObsPeriod,
            int nDeltaTsDelayed)
        {
            Name = name;
            DisplayInSimOut = displayInSimOutput;
            FirstObsMarksStartOfEpidemic = firstObsMarksStartOfEpidemic;
            _nDeltaTsObsPeriod = nDeltaTsObsPeriod;
            _nObsPeriodsDelay = nDeltaTsDelayed / nDeltaTsObsPeriod;
        }

        public abstract void Update();
        public abstract double GetLastObs(int epiTimeIndex);

    }

    public class SurveyedIncidenceTrajectory : SurveyedTrajectory
    {
        private IncidenceTimeSeries _timeSeries;
        private SumClassesTrajectory _sumClassesTraj;
        private SumEventTrajectory _sumEventsTraj;

        public SurveyedIncidenceTrajectory(
            string name,
            bool displayInSimOutput,
            bool firstObsMarksStartOfEpidemic,
            SumClassesTrajectory sumClassesTrajectory,
            SumEventTrajectory sumEventTrajectory,
            int nDeltaTsObsPeriod,
            int nDeltaTsDelayed) 
            : base(name, displayInSimOutput, firstObsMarksStartOfEpidemic, nDeltaTsObsPeriod, nDeltaTsDelayed)
        {
            _sumClassesTraj = sumClassesTrajectory;
            _sumEventsTraj = sumEventTrajectory;
            _timeSeries = new IncidenceTimeSeries(nDeltaTsObsPeriod);
        }

        public override void Update()
        {
            double value = 0;
            if (!(_sumClassesTraj is null))
                value = _sumClassesTraj.NumOfNewMembersOverPastPeriod;
            else if (!(_sumEventsTraj is null))
                value = _sumEventsTraj.NumOfNewMembersOverPastPeriod;

            _timeSeries.Record(value);
        }
        public override double GetLastObs(int epiTimeIndex)
        {
            double value = 0;
            if (epiTimeIndex > _nDeltaTsObsPeriod * _nObsPeriodsDelay)
                value = _timeSeries.GetLastObs(_nObsPeriodsDelay);

            return value;
        }
    }

    public class SurveyedPrevalenceTrajectory : SurveyedTrajectory
    {
        private PrevalenceTimeSeries _timeSeries;
        private SumClassesTrajectory _sumClassesTraj;
        private RatioTrajectory _ratioTraj;

        public SurveyedPrevalenceTrajectory(
            string name,
            bool displayInSimOutput,
            bool firstObsMarksStartOfEpidemic,
            SumClassesTrajectory sumClassesTrajectory,
            RatioTrajectory ratioTrajectory,
            int nDeltaTsObsPeriod,
            int nDeltaTsDelayed)
            : base(name, displayInSimOutput, firstObsMarksStartOfEpidemic, nDeltaTsObsPeriod, nDeltaTsDelayed)
        {
            _sumClassesTraj = sumClassesTrajectory;
            _ratioTraj = ratioTrajectory;
            _timeSeries = new PrevalenceTimeSeries(nDeltaTsObsPeriod);
        }

        public override void Update()
        {
            double value = 0;
            if (!(_sumClassesTraj is null))
                value = _sumClassesTraj.Prevalence;
            else if (!(_ratioTraj is null))
                value = _ratioTraj.LastRecordedRatio;

            _timeSeries.Record(value);
        }
        public override double GetLastObs(int epiTimeIndex)
        {
            double value = 0;
            if (epiTimeIndex > _nDeltaTsObsPeriod * _nObsPeriodsDelay)
                value = _timeSeries.GetLastObs(_nObsPeriodsDelay);
            return value;
        }
    }

    public abstract class OutputTrajs
    {
        protected int _simReplication;
        protected double _deltaT;
        protected int _nextTimeIndexToStore;
        protected DecisionMaker _decisionMaker;

        public List<string> PrevalenceOutputsHeader { get; protected set; } = new List<string>();
        public List<string> IncidenceOutputsHeader { get; protected set; } = new List<string>();
        public int NumOfPrevalenceOutputsToReport { get; protected set; }
        public int NumOfIncidenceOutputsToReport { get; protected set; }

        public int[][] SimRepIndeces { get; protected set; }
        public double[][] SimIncidenceOutputs { get; protected set; }
        public double[][] SimPrevalenceOutputs { get; protected set; }
        public int[][] InterventionCombinations { get; protected set; }

        public OutputTrajs(
            int simReplication,
            double deltaT,
            ref DecisionMaker decisionMaker,
            bool findHeader = false)
        {
            _simReplication = simReplication;
            _deltaT = deltaT;
            _decisionMaker = decisionMaker;
            _nextTimeIndexToStore = 0;
        }

        public void Reset()
        {
            _nextTimeIndexToStore = 0;
            SimRepIndeces = new int[0][];
            InterventionCombinations = new int[0][];
            SimPrevalenceOutputs = new double[0][];
            SimIncidenceOutputs = new double[0][];
        }

        // store selected outputs while simulating
        public void Record(int timeIndex, bool endOfSim)
        {
            // check if it is time to store output
            if (timeIndex < _nextTimeIndexToStore && !endOfSim)
                return;

            // define the jagged array to store current observation
            int[][] thisSimRepIndeces = new int[1][];
            thisSimRepIndeces[0] = new int[1];
            double[][] thisIncidenceOutputs = new double[1][];
            double[][] thisPrevalenceOutputs = new double[1][];
            int[][] thisActionCombination = new int[1][];

            // simulation replication index
            thisSimRepIndeces[0][0] = _simReplication;
            // action combination
            thisActionCombination[0] = (int[])_decisionMaker.CurrentDecision.Clone();

            // fill in the rest
            FillIn(timeIndex, ref thisIncidenceOutputs, ref thisPrevalenceOutputs, ref thisActionCombination);

            // concatenate this row 
            SimRepIndeces = SupportFunctions.ConcatJaggedArray(SimRepIndeces, thisSimRepIndeces);
            SimIncidenceOutputs = SupportFunctions.ConcatJaggedArray(SimIncidenceOutputs, thisIncidenceOutputs);
            SimPrevalenceOutputs = SupportFunctions.ConcatJaggedArray(SimPrevalenceOutputs, thisPrevalenceOutputs);
            InterventionCombinations = SupportFunctions.ConcatJaggedArray(InterventionCombinations, thisActionCombination);
        }

        protected abstract void FillIn(int timeIndex, ref double[][] thisIncidenceOutputs, ref double[][] thisPrevalenceOutputs, ref int[][] thisActionCombination);
    }

    public class SimOutputTrajs : OutputTrajs
    {
        private int _nDeltaTInSimOutputInterval;
        private List<Class> _classes;
        private List<SumTrajectory> _sumTrajectories;
        private List<RatioTrajectory> _ratioTrajectories;

        public SimOutputTrajs(
            int simReplication, 
            double deltaT,
            int nDeltaTInSimOutputInterval,
            ref DecisionMaker decisionMaker,
            ref List<Class> classes,
            ref List<SumTrajectory> sumTrajectories,
            ref List<RatioTrajectory> ratioTrajectories, 
            bool findHeader = false) : base(simReplication, deltaT, ref decisionMaker, findHeader)
        {
            _nDeltaTInSimOutputInterval = nDeltaTInSimOutputInterval;
            
            _classes = classes;
            _sumTrajectories = sumTrajectories;
            _ratioTrajectories = ratioTrajectories;

            FindNumOfOutputsAndHeaders(findHeader);
        }

        protected override void FillIn(int timeIndex, ref double[][] thisIncidenceOutputs, ref double[][] thisPrevalenceOutputs, ref int[][] thisActionCombination)
        {
            int colIndexPrevalenceOutputs = 0;
            int colIndexIncidenceOutputs = 0;
            thisPrevalenceOutputs[0] = new double[NumOfPrevalenceOutputsToReport];
            thisIncidenceOutputs[0] = new double[NumOfIncidenceOutputsToReport];

            // store the current time and the current interval            
            thisIncidenceOutputs[0][colIndexIncidenceOutputs++]
                = Math.Floor((double)(timeIndex - 1) / _nDeltaTInSimOutputInterval) + 1;
            thisPrevalenceOutputs[0][colIndexPrevalenceOutputs++] = timeIndex * _deltaT;

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

            // find next time index to store trajectories
            _nextTimeIndexToStore += _nDeltaTInSimOutputInterval;
        }

        // get header
        private void FindNumOfOutputsAndHeaders(bool storeHeaders)
        {
            // create headers
            NumOfIncidenceOutputsToReport = 1;
            if (storeHeaders) IncidenceOutputsHeader.Add("Simulation Period");
            NumOfPrevalenceOutputsToReport = 1;
            if (storeHeaders) PrevalenceOutputsHeader.Add("Simulation Time");

            // class headers
            foreach (Class thisClass in _classes)
            {
                if (thisClass.ShowIncidence)
                {
                    if (storeHeaders) IncidenceOutputsHeader.Add("To: " + thisClass.Name);
                    ++NumOfIncidenceOutputsToReport;
                }
                if (thisClass.ShowPrevalence)
                {
                    if (storeHeaders) PrevalenceOutputsHeader.Add("In: " + thisClass.Name);
                    ++NumOfPrevalenceOutputsToReport;
                }
                if (thisClass.ShowAccumIncidence)
                {
                    if (storeHeaders) PrevalenceOutputsHeader.Add("Sum To: " + thisClass.Name);
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
                            if (storeHeaders) IncidenceOutputsHeader.Add(thisSumTraj.Name);
                            ++NumOfIncidenceOutputsToReport;
                        }
                        break;
                    case SumTrajectory.EnumType.AccumulatingIncident:
                    case SumTrajectory.EnumType.Prevalence:
                        {
                            if (storeHeaders) PrevalenceOutputsHeader.Add(thisSumTraj.Name);
                            ++NumOfPrevalenceOutputsToReport;
                        }
                        break;
                }
            }
            // ratio statistics
            foreach (RatioTrajectory thisRatioTaj in _ratioTrajectories.Where(s => s.DisplayInSimOutput))
            {
                if (storeHeaders) PrevalenceOutputsHeader.Add(thisRatioTaj.Name);
                ++NumOfPrevalenceOutputsToReport;
            }
        }
    }

    public class ObsOutputTrajs : OutputTrajs
    {
        private int _nDeltaTInObsInterval;
        List<SurveyedIncidenceTrajectory> _surveyIncidenceTrajs;
        List<SurveyedPrevalenceTrajectory> _surveyPrevalenceTrajs;

        public ObsOutputTrajs(
            int simReplication,
            double deltaT,
            int nDeltaTInObsInterval,
            ref DecisionMaker decisionMaker,
            ref List<SurveyedIncidenceTrajectory> surveyedIncidenceTrajectories,
            ref List<SurveyedPrevalenceTrajectory> surveyedPrevalenceTrajectories,
            bool findHeader = false) : base(simReplication, deltaT, ref decisionMaker, findHeader)
        {
            _nDeltaTInObsInterval = nDeltaTInObsInterval;
            _surveyIncidenceTrajs = surveyedIncidenceTrajectories;
            _surveyPrevalenceTrajs = surveyedPrevalenceTrajectories;

            FindNumOfOutputsAndHeaders(findHeader);
        }

        protected override void FillIn(int epiTimeIndex, ref double[][] thisIncidenceOutputs, ref double[][] thisPrevalenceOutputs, ref int[][] thisActionCombination)
        {

            // return if epidemic has not started yet
            if (epiTimeIndex < 0)
                return;

            int colIndexPrevalenceOutputs = 0;
            int colIndexIncidenceOutputs = 0;
            thisPrevalenceOutputs[0] = new double[NumOfPrevalenceOutputsToReport];
            thisIncidenceOutputs[0] = new double[NumOfIncidenceOutputsToReport];

            // store the current time and the current interval            
            thisIncidenceOutputs[0][colIndexIncidenceOutputs++]
                = Math.Floor((double)(epiTimeIndex - 1) / _nDeltaTInObsInterval) + 1;
            thisPrevalenceOutputs[0][colIndexPrevalenceOutputs++] = epiTimeIndex * _deltaT;

            foreach (SurveyedIncidenceTrajectory incdTraj in _surveyIncidenceTrajs.Where(i => i.DisplayInSimOut))
            {
                thisIncidenceOutputs[0][colIndexIncidenceOutputs++] = incdTraj.GetLastObs(epiTimeIndex);
            }
            foreach (SurveyedPrevalenceTrajectory prevTraj in _surveyPrevalenceTrajs.Where(i => i.DisplayInSimOut))
            {
                thisPrevalenceOutputs[0][colIndexPrevalenceOutputs++] = prevTraj.GetLastObs(epiTimeIndex);
            }

            // find next time index to store trajectories
            _nextTimeIndexToStore += _nDeltaTInObsInterval;
        }

        private void FindNumOfOutputsAndHeaders(bool storeHeaders)
        {
            // create headers
            NumOfIncidenceOutputsToReport = 1;
            if (storeHeaders) IncidenceOutputsHeader.Add("Observation Period");
            NumOfPrevalenceOutputsToReport = 1;
            if (storeHeaders) PrevalenceOutputsHeader.Add("Epidemic Time");

            foreach (SurveyedIncidenceTrajectory incdTraj in _surveyIncidenceTrajs.Where(i => i.DisplayInSimOut))
            {
                if (storeHeaders) IncidenceOutputsHeader.Add("Obs: " + incdTraj.Name);
                ++NumOfIncidenceOutputsToReport;
            }
            foreach (SurveyedPrevalenceTrajectory prevTraj in _surveyPrevalenceTrajs.Where(i => i.DisplayInSimOut))
            {
                if (storeHeaders) IncidenceOutputsHeader.Add("Obs: " + prevTraj.Name);
                ++NumOfIncidenceOutputsToReport;
            }
        }
    }

    public class EpidemicHistory
    {
        // summation and ratio trajectories
        public List<SumTrajectory> _sumTrajs = new List<SumTrajectory>();
        public List<RatioTrajectory> _ratioTraj = new List<RatioTrajectory>();
        public List<SumTrajectory> SumTrajs { get => _sumTrajs; set => _sumTrajs = value; }
        public List<RatioTrajectory> RatioTrajs { get => _ratioTraj; set => _ratioTraj = value; }
        
        // surveyed trajectories
        public List<SurveyedIncidenceTrajectory> _survIncidence = new List<SurveyedIncidenceTrajectory>();
        public List<SurveyedPrevalenceTrajectory> _survPrevalence = new List<SurveyedPrevalenceTrajectory>();
        public List<SurveyedIncidenceTrajectory> SurveyedIncidenceTrajs { get => _survIncidence; set => _survIncidence = value; }
        public List<SurveyedPrevalenceTrajectory> SurveyedPrevalenceTrajs { get => _survPrevalence; set => _survPrevalence = value; }
        
        // all trajectories prepared for simulation output 
        public SimOutputTrajs SimOutputTrajs { get; private set; }
        public ObsOutputTrajs ObsTrajs { get; private set; }

        public EpidemicHistory()
        {
        }

        public void SetupSimOutputTrajs(
            int ID,
            double deltaT,
            int nDeltaTinSimOutputInterval,
            int nDeltaTInObsInterval,
            ref DecisionMaker decisionMaker,
            ref List<Class> classes,
            bool extractOutputHeaders)
        {
            SimOutputTrajs = new SimOutputTrajs(
               ID,
               deltaT,
               nDeltaTinSimOutputInterval,
               ref decisionMaker,
               ref classes,
               ref _sumTrajs,
               ref _ratioTraj,
               extractOutputHeaders);
            ObsTrajs = new ObsOutputTrajs(
                ID,
                deltaT,
                nDeltaTInObsInterval,
                ref decisionMaker,
                ref _survIncidence,
                ref _survPrevalence,
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
            // update surveyed incidence 
            foreach (SurveyedIncidenceTrajectory survIncdTraj in SurveyedIncidenceTrajs)
                survIncdTraj.Update();
            // update surveyed prevalence 
            foreach (SurveyedPrevalenceTrajectory survPrevTraj in SurveyedPrevalenceTrajs)
                survPrevTraj.Update();
        }

        public void Reset(int simTimeIndex, ref List<Class> classes, ref List<Event> events)
        {
            SimOutputTrajs.Reset();
            ObsTrajs.Reset();

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
