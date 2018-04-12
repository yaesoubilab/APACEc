using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComputationLib;
using SimulationLib;

namespace APACElib
{
    // Summation statistics
    //public class SummationStatisticsOld
    //{
    //    public enum enumDefinedOn
    //    {
    //        Classes = 0,
    //        Events = 1,
    //    }
    //    public enum enumType
    //    {
    //        Incidence = 0,
    //        AccumulatingIncident = 1,
    //        Prevalence = 2, 
    //    }

    //    int _ID;
    //    string _name;
    //    enumDefinedOn _definedOn = enumDefinedOn.Classes;
    //    enumType _type = enumType.Incidence;
    //    int[] _arrClassOrEventIDs;
    //    bool _ifDisplay = false;

    //    // statistics collectors
    //    private CounterStatistics _countStatisticsNewMembers = null;
    //    private TimePersistentStatistics _averagePrevalence;

    //    private bool _surveillanceDataAvailable;
    //    private bool _firstObservationMarksTheStartOfTheSpread = false;
        
    //    private bool _ifIncludedInCalibration;
    //    private SimulationLib.CalibrationTarget.enumGoodnessOfFitMeasure _goodnessOfFitMeasure;

    //    private double _weight_overalFit;
    //    private double[] _weight_fourierSimilarities = new double[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.SIZE];

    //    private bool _ifCheckWithinFeasibleRange = false;
    //    private double _feasibleRange_min;
    //    private double _feasibleRange_max;

    //    private int _currentMembers;
    //    private int _accumulatedNewMembers;

    //    public SummationStatisticsOld(int ID, string name, enumDefinedOn definedOn, enumType type, string sumFormula, double QALYLossPerNewMember, double costPerNewMember, 
    //                               bool surveillanceDataAvailable, bool firstObservationMarksTheStartOfTheSpread,
    //                               int numOfPastObsPeriodsToStore, int numOfDeltaTInEachObsPeriod, int numOfObsPeriodsDelayed)
    //    {
    //        _ID = ID;
    //        _name = name;
    //        _definedOn = definedOn;
    //        _type = type;
    //        _arrClassOrEventIDs = ConvertSumFormulaToArrayOfClassIDs(sumFormula);
    //        _firstObservationMarksTheStartOfTheSpread = firstObservationMarksTheStartOfTheSpread;
    //        _surveillanceDataAvailable = surveillanceDataAvailable;
    //        _countStatisticsNewMembers = new CounterStatistics(name, QALYLossPerNewMember, 0, costPerNewMember, 0,
    //            numOfPastObsPeriodsToStore, numOfDeltaTInEachObsPeriod, numOfObsPeriodsDelayed, true);

    //        if (type == enumType.Prevalence)
    //            _averagePrevalence = new TimePersistentStatistics();
    //    }

    //    // Properties
    //    public string Name
    //    {
    //        get { return _name; }
    //    }
    //    public int ID
    //    {
    //        get { return _ID; }
    //    }
    //    public enumDefinedOn DefinedOn
    //    {
    //        get { return _definedOn; }
    //    }
    //    public enumType Type
    //    {
    //        get { return _type; }
    //    }
    //    public bool IfDisplay
    //    {
    //        get { return _ifDisplay; }
    //        set { _ifDisplay = value; }
    //    }
    //    public CounterStatistics CountStatisticsNewMembers
    //    {
    //        get { return _countStatisticsNewMembers; }
    //    }
    //    public bool SurveillanceDataAvailable
    //    {
    //        get { return _surveillanceDataAvailable; }
    //    }
    //    public bool FirstObservationMarksTheStartOfTheSpread
    //    {
    //        get { return _firstObservationMarksTheStartOfTheSpread; }
    //    }
    //    // calibration
    //    public bool IfIncludedInCalibration
    //    {
    //        get { return _ifIncludedInCalibration; }
    //        set { _ifIncludedInCalibration = value; }
    //    }
    //    public SimulationLib.CalibrationTarget.enumGoodnessOfFitMeasure GoodnessOfFitMeasure
    //        {
    //            get { return _goodnessOfFitMeasure; }
    //            set { _goodnessOfFitMeasure = value; }
    //        }
    //    public double Weight_overalFit
    //    { 
    //        get { return _weight_overalFit; }
    //        set { _weight_overalFit = value; }
    //    }
    //    public double[] Weight_fourierSimilarities
    //    {
    //        get { return _weight_fourierSimilarities; }
    //        set { _weight_fourierSimilarities = value; }
    //    }

    //    public bool IfCheckWithinFeasibleRange
    //    {
    //        get { return _ifCheckWithinFeasibleRange; }
    //        set { _ifCheckWithinFeasibleRange = value; }
    //    }
    //    public double FeasibleRange_min
    //    { 
    //        get { return _feasibleRange_min; }
    //        set { _feasibleRange_min = value; }
    //    }
    //    public double FeasibleRange_max
    //    { 
    //        get { return _feasibleRange_max; }
    //        set { _feasibleRange_max = value; }
    //    }
    //    //
    //    public double CurrentCost
    //    {
    //        get {
    //           // if (_countStatisticsNewMembers != null)
    //                return _countStatisticsNewMembers.CurrentCost;
    //            //else
    //            //    return 0;
    //        }
    //    }
    //    public double CurrentQALY
    //    {
    //        get {
    //            //if (_countStatisticsNewMembers != null)
    //                return _countStatisticsNewMembers.CurrentQALY;
    //            //else
    //            //    return 0;
    //        }
    //    }
    //    public int CurrentMembers
    //    {
    //        get { return _currentMembers; }
    //    }
    //    public int AccumulatedNewMembers
    //    {            
    //        get { return _accumulatedNewMembers;}
    //    }
    //    public int ObservedAccumulatedNewMembers
    //    {
    //        get { return _countStatisticsNewMembers.TotalObservedCounts; }
    //    }
    //    public int NewMembersOverPastObsPeriod
    //    {
    //        get { return _countStatisticsNewMembers.CurrentCountsInThisObsPeriod;}
    //    }
    //    public int NewMembersOverPastObservableObsPeriod
    //    {
    //        get{ return _countStatisticsNewMembers.LastObservedCounts;}
    //    }
    //    public int NewMemberOverPastSimulationInterval
    //    {
    //        get { return _countStatisticsNewMembers.CurrentCountsInThisSimulationOutputInterval; }
    //    }
    //    public double AveragePrevalence
    //    {
    //        get { return _averagePrevalence.Mean; }
    //    }

    //    // add new members
    //    //public void AddNewMembers(int[] arrNumOfNewMembersOverPastDeltaT, double deltaT)
    //    //{
    //    //    int sumNumMembers = 0;

    //    //    for (int i = 0; i < _arrClassOrEventIDs.Length; ++i)
    //    //        sumNumMembers += arrNumOfNewMembersOverPastDeltaT[_arrClassOrEventIDs[i]];

    //    //    // record the sum of new members
    //    //    if (_type == enumType.Incidence)
    //    //        _accumulatedNewMembers += sumNumMembers;

    //    //    _countStatisticsNewMembers.AddAnObservation(sumNumMembers, deltaT);            
    //    //}
    //    public void AddNewMembers(ref List<Class> classes, double deltaT)
    //    {
    //        int sumNumMembers = 0;

    //        for (int i = 0; i < _arrClassOrEventIDs.Length; ++i)
    //            sumNumMembers += classes[_arrClassOrEventIDs[i]].ClassStat.NumOfNewMembersOverPastPeriod;

    //        // record the sum of new members
    //        if (_type == enumType.Incidence)
    //            _accumulatedNewMembers += sumNumMembers;

    //        _countStatisticsNewMembers.AddAnObservation(sumNumMembers, deltaT);
    //    }
    //    public void AddNewMembers(ref List<Event> events, double deltaT)
    //    {
    //        int sumNumMembers = 0;

    //        for (int i = 0; i < _arrClassOrEventIDs.Length; ++i)
    //            sumNumMembers += events[_arrClassOrEventIDs[i]].MembersOutOverPastDeltaT;

    //        _accumulatedNewMembers += sumNumMembers;

    //        _countStatisticsNewMembers.AddAnObservation(sumNumMembers, deltaT);
    //    }

    //    // add new members
    //    public void AddCurrentMembers(double epidemicTime, int[] arrNumOfCurrentMembers, bool collectSummaryStats)
    //    {
    //        _currentMembers = 0;
    //        for (int i = 0; i < _arrClassOrEventIDs.Length; ++i)
    //            _currentMembers += arrNumOfCurrentMembers[_arrClassOrEventIDs[i]];

    //        if (collectSummaryStats)
    //            _averagePrevalence.Record(epidemicTime, _currentMembers);
    //    }      

    //    // read feature value  
    //    public double ReadFeatureValue(Feature_DefinedOnSummationStatistics feature)
    //    {
    //        double value = 0;
    //        switch (feature.FeatureType)
    //        {       
    //            case Feature.enumFeatureType.Incidence:
    //                value = this.NewMembersOverPastObservableObsPeriod;
    //                break;                
    //            case Feature.enumFeatureType.Prediction:
    //                value = Math.Max( 0, _countStatisticsNewMembers.Prediction(1));
    //                break;
    //            case Feature.enumFeatureType.AccumulatingIncidence:
    //                value = this.ObservedAccumulatedNewMembers;
    //                break;
    //            default:
    //                value = -1;
    //                break;
    //        }
    //        return value;
    //    }

    //    // reset statistics for another simulation run
    //    public void ResetStatistics(double warmUpPeriodLength, bool ifToResetForAnotherSimulationRun)
    //    {
    //        if (ifToResetForAnotherSimulationRun)
    //        {
    //            _currentMembers = 0;
    //        }
    //        _countStatisticsNewMembers.ResetForAnotherSimulationRun();
    //        _accumulatedNewMembers = 0;
    //        if (_averagePrevalence != null)
    //            _averagePrevalence.Reset(warmUpPeriodLength);
    //    }        
    //    // reset new members over past observation period 
    //    public void ResetNewMembersOverPastObsPeriod()
    //    {
    //        _countStatisticsNewMembers.ResetCurrentAggregatedObsInThisObsPeriodervation();
    //    }         

    //    // convert sum formula into the array of class IDs
    //    private int[] ConvertSumFormulaToArrayOfClassIDs(string formula)
    //    {
    //        string[] arrClassIDs = formula.Split('+');
    //        return Array.ConvertAll<string, int>(arrClassIDs, Convert.ToInt32);            
    //    }
    //}

    //// Ratio statistics
    //public class RatioStatistics
    //{
    //    public enum enumType
    //    {
    //        IncidenceOverIncidence = 0,
    //        AccumulatedIncidenceOverAccumulatedIncidence = 1,
    //        PrevalenceOverPrevalence = 2,
    //        IncidenceOverPrevalence = 3,
    //    }

    //    int _ID;
    //    string _name;
    //    enumType _type;        
    //    int _nominatorSpecialStatID;
    //    int _denominatorSpecialStatID;
    //    bool _ifDisplay = false;
    //    private bool _surveillanceDataAvailable;
    //    double _currentValue;

    //    bool _ifIncludedInCalibration;
    //    private SimulationLib.CalibrationTarget.enumGoodnessOfFitMeasure _goodnessOfFitMeasure;
    //    private double _weight_overalFit;
    //    private double[] _weight_fourierSimilarities = new double[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.SIZE];
    //    private bool _ifCheckWithinFeasibleRange = false;
    //    private double _feasibleRange_min;
    //    private double _feasibleRange_max;

    //    ObservationBasedStatistics _obsRatio;

    //    // Instantiation
    //    public RatioStatistics(int ID, string name, enumType type, string ratioFormula, bool ifSurveillanceDataAvailable)
    //    {
    //        _ID = ID;
    //        _name = name;
    //        _type = type;
    //        int[] arrClassIDs = ConvertRatioFormulaToArrayOfClassIDs(ratioFormula);

    //        _nominatorSpecialStatID = arrClassIDs[0];
    //        _denominatorSpecialStatID = arrClassIDs[1];
    //        _obsRatio = new ObservationBasedStatistics(_name);

    //        _surveillanceDataAvailable = ifSurveillanceDataAvailable;
    //    }

    //    // Properties
    //    public int ID
    //    {
    //        get { return _ID; }   
    //    }
    //    public string Name
    //    {
    //        get { return _name; }
    //    }
    //    public enumType Type
    //    {
    //        get { return _type; }
    //    }
    //    public int NominatorSpecialStatID
    //    {
    //        get { return _nominatorSpecialStatID; }
    //    }
    //    public int DenominatorSpecialStatID
    //    {
    //        get { return _denominatorSpecialStatID; }
    //    }
    //    public bool IfDisplay
    //    {
    //        get { return _ifDisplay; }
    //        set { _ifDisplay = value; }
    //    }
    //    public bool SurveillanceDataAvailable
    //    {
    //        get { return _surveillanceDataAvailable; }
    //    }
    //    public double CurrentValue
    //    {
    //        get { return _currentValue; }
    //        set { _currentValue = value; }
    //    }
    //    public double Mean
    //    {
    //        get {return _obsRatio.Mean;}
    //    }
    //    public double StDev
    //    {
    //        get { return _obsRatio.StDev; }
    //    }
    //    public double StError
    //    {
    //        get { return _obsRatio.StErr; }
    //    }

    //    // calibration
    //    public bool IfIncludedInCalibration
    //    {
    //        get { return _ifIncludedInCalibration; }
    //        set { _ifIncludedInCalibration = value; }
    //    }
    //    public SimulationLib.CalibrationTarget.enumGoodnessOfFitMeasure GoodnessOfFitMeasure
    //    {
    //        get { return _goodnessOfFitMeasure; }
    //        set { _goodnessOfFitMeasure = value; }
    //    }
    //    public double Weight_overalFit
    //    {
    //        get { return _weight_overalFit; }
    //        set { _weight_overalFit = value; }
    //    }
    //    public double[] Weight_fourierSimilarities
    //    {
    //        get { return _weight_fourierSimilarities; }
    //        set { _weight_fourierSimilarities = value; }
    //    }
    //    public bool IfCheckWithinFeasibleRange
    //    {
    //        get { return _ifCheckWithinFeasibleRange; }
    //        set { _ifCheckWithinFeasibleRange = value; }
    //    }
    //    public double FeasibleRange_min
    //    {
    //        get { return _feasibleRange_min; }
    //        set { _feasibleRange_min = value; }
    //    }
    //    public double FeasibleRange_max
    //    {
    //        get { return _feasibleRange_max; }
    //        set { _feasibleRange_max = value; }
    //    }

    //    // record
    //    public void Record(double nominatorSpecialStatValue, double denominatorSpecialStatValue)
    //    {
    //        if (denominatorSpecialStatValue != 0)
    //        {
    //            _currentValue = (double)nominatorSpecialStatValue / denominatorSpecialStatValue;
    //            _obsRatio.Record(_currentValue);
    //        }
    //    }
    //    // reset statistics for another simulation run
    //    public void ResetForAnotherSimulationRun()
    //    {
    //        _currentValue = 0;
    //        _obsRatio.Reset();
    //    }
        
    //    // convert ratio formula into the array of class IDs
    //    private int[] ConvertRatioFormulaToArrayOfClassIDs(string formula)
    //    {
    //        string[] arrClassIDs = formula.Split('/');
    //        return Array.ConvertAll<string, int>(arrClassIDs, Convert.ToInt32);
    //    }
    //}
}
