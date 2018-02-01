using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomNumberGeneratorLib;
using SimulationLib;
using ComputationLib;
using System.Windows.Forms;

namespace APACE_lib
{
    // Class
    public abstract class Class
    {
        // Variables
        #region Variables

        public enum enumType
        {
            Normal = 1,
            Death = 2,
            Splitting = 3,
            ResourceMonitor = 4,
        }

        // Fields
        protected int _ID;
        protected string _name;
        protected enumType _type;

        protected int[] _arrDestinationClasseIDs;
        protected long[] _arrNumOfMembersSendingToEachDestinationClasses;
        protected bool _ifNeedsToBeProcessed = false;
        protected bool _ifMembersWaitingToSendOutBeforeNextDeltaT = false;
        protected int _rowIndexInContactMatrix;
        // show results
        protected bool _showNewMembers;
        protected bool _showMembersInClass;
        protected bool _showAccumulatedNewMembers;
        protected bool _showStatisticsInSimulationResults;
        // statistics 
        protected long _numberOfNewMembersOverPastDeltaT;
        protected long _currentNumberOfMembers;
        protected long _accumulatedNewMembers;
        protected TimePersistentStatistics _timePersistentStat_classSize;
        protected cCounterStatistics _countStatisticsNewMembers = null;
        protected cCounterStatistics _countStatisticsMembersInClass = null;
        #endregion

        // Instantiation
        public Class(int ID, string name)
        {
            _ID = ID;
            _name = name;
            _ifNeedsToBeProcessed = true;
            _ifMembersWaitingToSendOutBeforeNextDeltaT = false;
        }

        // Properties
        #region Properties
        public int ID
        {
            get{return _ID;}
        }
        public string Name
        {
            get {return _name;}
        }
        public enumType Type
        { get { return _type; } }
        public bool ShowNewMembers
        {
            get{return _showNewMembers;}
            set{_showNewMembers = value;}
        }
        public bool ShowMembersInClass
        {
            get{return _showMembersInClass;}
            set{_showMembersInClass = value;}
        }
        public bool ShowAccumulatedNewMembers
        {
            get{return _showAccumulatedNewMembers;}
            set{_showAccumulatedNewMembers = value;}
        }
        public bool CollectNewMembersStatistics
        {
            get
            {
                if (_countStatisticsNewMembers != null)
                    return true;
                else
                    return false;
            }
        }
        public bool CollectMembersInClassStatistics
        {
            get 
            {
                if (_countStatisticsMembersInClass != null)
                    return true;
                else
                    return false;
            }
        }
        public bool ShowStatisticsInSimulationResults
        {
            get { return _showStatisticsInSimulationResults; }
            set { _showStatisticsInSimulationResults = value; }
        }
        public long CurrentNumberOfMembers
        {
            get{ return _currentNumberOfMembers;}
        }
        public bool IfNeedsToBeProcessed
        {
            get { return _ifNeedsToBeProcessed; }
            set { _ifNeedsToBeProcessed = value; }
        }
        public bool IfMembersWaitingToSendOutBeforeNextDeltaT
        {
            get { return _ifMembersWaitingToSendOutBeforeNextDeltaT; }
            set { _ifMembersWaitingToSendOutBeforeNextDeltaT = value; }
        }
        public long NumberOfNewMembersOverPastDeltaT
        {
            get { return _numberOfNewMembersOverPastDeltaT; }
            set { _numberOfNewMembersOverPastDeltaT = value; }
        }
        public long NewMembersOverPastObsPeriod
        {
            get { return _countStatisticsNewMembers.CurrentCountsInThisObsPeriod; }
        }
        public long NewMembersOverPastSimulationOutputInterval
        {
            get { return _countStatisticsNewMembers.CurrentCountsInThisSimulationOutputInterval; }
        }
        public long AccumulatedNewMembers
        {
            get { return _accumulatedNewMembers; }
        }
        public double AverageNewMembersOverPastObsPeriod
        {
            get{ return _countStatisticsNewMembers.Mean();}
        }
        public double AverageClassSize
        {
            get { return _timePersistentStat_classSize.Mean; }
        }
        public cCounterStatistics CountStatisticsNewMembers
        {
            get{ return _countStatisticsNewMembers;}
        }
        public cCounterStatistics CountStatisticsMembersInClass
        {
            get{ return _countStatisticsMembersInClass; }
        }

        public virtual int[] ArrDestinationClasseIDs
        { get { return new int[0]; } }
        public virtual long[] ArrNumOfMembersSendingToEachDestinationClasses
        { get { return new long[0]; } }
        public virtual bool EmptyToEradicate
        {
            get { return false; }
        }
        public virtual int InitialMemebersParID
        {
            get { return 0; }
        }
        public virtual int RowIndexInContactMatrix
        { get { return 0; } }
        public virtual double[] SusceptibilityValues
        {
            get { return new double[0]; }
        }
        public virtual double[] InfectivityValues
        {
            get { return new double[0]; }
        }
        public virtual long[] ArrResourcesConsumed
        {
            get { return new long[0]; }
        }
        public virtual bool IsEpiDependentProcessActive
        {
            get { return false; }
        }
        #endregion

        // set up statistics
        public void SetupNewMembersStatistics(double QALYLossPerNewMember, double costPerNewMember)
        {
            _countStatisticsNewMembers = new cCounterStatistics("Number of new members",QALYLossPerNewMember, 0, costPerNewMember, 0, true);              
        }
        public void SetupNewMembersStatistics(double QALYLossPerNewMember, double costPerNewMember, int numOfPastObsPeriodsToStore, int numOfObsInEachObsPeriod)
        {
            _countStatisticsNewMembers = new cCounterStatistics("Number of new members", QALYLossPerNewMember, 0, costPerNewMember, 0, numOfPastObsPeriodsToStore, numOfObsInEachObsPeriod,0, false);
        }  
        public void SetupMembersInClassStatistics(double healthQualityPerUnitOfTime, double costPerUnitOfTime)
        {
            _countStatisticsMembersInClass = new cCounterStatistics("Number in class",0 , healthQualityPerUnitOfTime, 0, costPerUnitOfTime, false);
            _timePersistentStat_classSize = new TimePersistentStatistics();
        }       

        // add new members
        public void AddNewMembers(long numOfNewMembers)
        {
            // return if number of new members is zero
            if (numOfNewMembers == 0)
                return;

            // increment the current number of members
            _numberOfNewMembersOverPastDeltaT += numOfNewMembers;
            _currentNumberOfMembers += numOfNewMembers;
            _accumulatedNewMembers += numOfNewMembers;            

            // if this is a death class the current number of members should be set to zero
            if (this is Class_Death)
                _currentNumberOfMembers = 0;
            
            if (this is Class_Splitting || this is Class_ResourceMonitor)
                _ifNeedsToBeProcessed = true;
        }     
      
        // update statistics
        public void UpdateStatisticsAtTheEndOfDeltaT(double epidemicTime, double deltaT)
        {
            // gather statistics on the new number of members   
            if (_countStatisticsNewMembers != null)
                _countStatisticsNewMembers.AddAnObservation(_numberOfNewMembersOverPastDeltaT);

            // gather statistics on the number of members in class
            if (_countStatisticsMembersInClass != null)
                _countStatisticsMembersInClass.AddAnObservation(_currentNumberOfMembers, deltaT);

            // update average size
            if (this.Type == enumType.Normal && _timePersistentStat_classSize != null)
                _timePersistentStat_classSize.Record(epidemicTime, _currentNumberOfMembers); 
        }
        // read feature value  
        public double ReadFeatureValue(Feature_DefinedOnNewClassMembers feature)
        {
            double value = 0;            
            switch (feature.FeatureType)
            {
                case Feature.enumFeatureType.Incidence:
                    value = _countStatisticsNewMembers.LastObservedCounts;
                    break;
                case Feature.enumFeatureType.Prediction:
                    value = Math.Max(0, _countStatisticsNewMembers.Prediction(feature.NumOfTimePeriodsForFuturePrediction));
                    break;
                case Feature.enumFeatureType.AccumulatingIncidence:
                    value = _countStatisticsNewMembers.TotalObservedCounts; // _accumulatedNewMembers;
                    break;
                default:
                    value = -1;
                    break;
            }  
            return value;
        }
        // reset new members over past observation period
        public void ResetNewMembersOverPastObsPeriod()
        {
            if (_countStatisticsNewMembers != null)
                _countStatisticsNewMembers.ResetCurrentAggregatedObsInThisObsPeriodervation();
        }

        // update the initial number of members
        public virtual void UpdateInitialNumberOfMembers(long sampledValue)
        {         
        }
        // update rates of epidemic independent processes associated to this class
        public virtual void UpdateRatesOfBirthAndEpidemicIndependentProcesses(double[] updatedParameterValues)
        {
        }
        // update susceptibility values
        public virtual void UpdateSusceptibilityParameterValues(double[] arrSampledParameterValues)
        {
        }
        // update infectivity values
        public virtual void UpdateInfectivityParameterValues(double[] arrSampledParameterValues)
        {
        }
        // update the probability of success
        public virtual void UpdateProbOfSuccess(double[] arrSampledParameters)
        {
        }
        // select this intervention combination 
        public virtual void SelectThisInterventionCombination(int[] interventionCombination)
        {
        }
        // update transmission rates affecting this class
        public virtual void UpdateTransmissionRates(double[] arrTransmissionRatesByPathogens)
        {
        }
        // send out members
        public virtual void SendOutMembers(double deltaT, ThreadSpecificRNG threadSpecificRNG)
        {
        }
        // reset number of members sending to each destination class
        public virtual void ResetNumOfMembersSendingToEachDestinationClasses()
        { }
        // find the members out of active processes
        public virtual void ReturnAndResetNumOfMembersOutOfProcessesOverPastDeltaT(ref long[] arrNumOfMembersOutOfProcessesOverPastDeltaT)
        { }

        //// update transmission rates
        //public virtual void UpdateTransmissionRatesAffectingThisClass(int indexOfIntCombInTransmissionMatrix, double[][][][] arrTransmissionMatrices)
        //{ 
        //}
        //// update proportion of members in this class
        //public virtual void UpdateProportionOfMembers(double[] arrProportionOfMembersInEachClass)
        //{         
        //}
        // read current cost
        public virtual double CurrentCost()
        {
            return 0;
        }
        // read current QALY
        public virtual double CurrentQALY()
        {
            return 0;
        }
        // reset for another simulation run
        public virtual void ResetStatistics(double warmUpPeriodLength, bool ifToResetForAnotherSimulationRun)
        {
        } 
        // update available resources
        public virtual void UpdateAvailableResources(long[] arrResourceAvailability)
        { }
        // Reset statistics for another simulation run
        protected void ResetClassStatisticsForAnotherSimulationRun(double warmUpPeriodLength)
        {
            _ifNeedsToBeProcessed = true;
            _accumulatedNewMembers = 0;
            _numberOfNewMembersOverPastDeltaT = 0;

            if (_countStatisticsNewMembers!=null)
                _countStatisticsNewMembers.ResetForAnotherSimulationRun();
            if (_countStatisticsMembersInClass != null)
                _countStatisticsMembersInClass.ResetForAnotherSimulationRun();
            if (_timePersistentStat_classSize != null)
                _timePersistentStat_classSize.Reset(warmUpPeriodLength);
        }                                 
    }

    // Class_Normal
    public class Class_Normal : Class
    {
        // transmission matrix
        private int _initialMembersParID;
        private long _initialMembers;
        private int[] _susceptibilityParIDs;
        private double[] _susceptibilityValues;
        private int[] _infectivityParIDs;
        private double[] _infectivityValues;
        private bool _isEpiDependentProcessActive = false;

        private bool _emptyToEradicate;
        private List<Process> _processes = new List<Process>();
        private List<Process> _activeProcesses = new List<Process>();
        private int[] _currentInterventionCombination;

        public Class_Normal(int ID, string name)
            : base(ID, name)
        {
            _type = enumType.Normal;
        }

        // Properties
        #region Properties
        public override int InitialMemebersParID
        {
            get { return _initialMembersParID; }
        }
        public long InitialMembers
        {
            get { return _initialMembers; }
            set { _initialMembers = value; }
        }
        public override bool EmptyToEradicate
        {
            get { return _emptyToEradicate; }
        }

        // transmission 
        public int[] SusceptibilityParIDs
        {
            get { return _susceptibilityParIDs; }
        }
        public int[] InfectivityParIDs
        {
            get { return _infectivityParIDs; }
        }
        public override double[] SusceptibilityValues
        {
            get { return _susceptibilityValues; }
        }
        public override double[] InfectivityValues
        {
            get { return _infectivityValues; }
        }
        public override int RowIndexInContactMatrix
        {
            get { return _rowIndexInContactMatrix; }
        }
        public override bool IsEpiDependentProcessActive
        {
            get{return _isEpiDependentProcessActive;}
        }
        public override int[] ArrDestinationClasseIDs
        { get { return _arrDestinationClasseIDs; } }
        public override long[] ArrNumOfMembersSendingToEachDestinationClasses
        { get { return _arrNumOfMembersSendingToEachDestinationClasses; } }

        #endregion

        // add a process
        public void AddAProcess(Process process)
        {
            _processes.Add(process);
        }
        // setup the initial number parID
        public void SetupInitialAndStoppingConditions(int initialMembersParID, bool ifShouldBeEmptyForEradication)
        {
            _initialMembersParID = initialMembersParID;
            _emptyToEradicate = ifShouldBeEmptyForEradication;
        }
        // set up transmission dynamics properties
        public void SetupTransmissionDynamicsProperties(string susceptibilityParameterIDs, string infectivityParameterIDs, int rowIndexInContactMatrix)
        {
            // remove brackets
            susceptibilityParameterIDs = susceptibilityParameterIDs.Replace(" ", "");
            susceptibilityParameterIDs = susceptibilityParameterIDs.Replace("{", "");
            susceptibilityParameterIDs = susceptibilityParameterIDs.Replace("}", "");
            infectivityParameterIDs = infectivityParameterIDs.Replace(" ", "");
            infectivityParameterIDs = infectivityParameterIDs.Replace("{", "");
            infectivityParameterIDs = infectivityParameterIDs.Replace("}", "");
            // convert to array
            string[] strSusceptibilityParameterIDs = susceptibilityParameterIDs.Split(',');
            _susceptibilityParIDs = Array.ConvertAll<string, int>(strSusceptibilityParameterIDs, Convert.ToInt32);
            _susceptibilityValues = new double[_susceptibilityParIDs.Length];

            string[] strInfectivityParameterIDs = infectivityParameterIDs.Split(',');
            _infectivityParIDs = Array.ConvertAll<string, int>(strInfectivityParameterIDs, Convert.ToInt32);
            _infectivityValues = new double[_infectivityParIDs.Length];

            _rowIndexInContactMatrix = rowIndexInContactMatrix;
        }
        // update the initial number of members
        public override void UpdateInitialNumberOfMembers(long sampledValue)
        {
            _initialMembers = sampledValue;
            _currentNumberOfMembers = sampledValue;
        }
        // update susceptibility and infectivity values
        public override void UpdateSusceptibilityParameterValues(double[] arrSampledParameterValues)
        {
            //TODO: revise to avoid passing the entire array of sampled parameters
            for (int i = 0; i < _susceptibilityParIDs.Length; i++)
                _susceptibilityValues[i] = Math.Max(0, arrSampledParameterValues[_susceptibilityParIDs[i]]);
        }
        // update infectivity values
        public override void UpdateInfectivityParameterValues(double[] arrSampledParameterValues)
        {
            //TODO: revise to avoid passing the entire array of sampled parameters
            for (int i = 0; i < _infectivityParIDs.Length; i++)
                _infectivityValues[i] = Math.Max(0, arrSampledParameterValues[_infectivityParIDs[i]]);
        }
        // update rates of epidemic independent processes associated to this class
        public override void UpdateRatesOfBirthAndEpidemicIndependentProcesses(double[] updatedParameterValues)
        {
            foreach (Process thisProcess in _activeProcesses)
            {
                // update the epidemic independent rate
                Process_EpidemicIndependent thisEpidemicIndependentProcess = thisProcess as Process_EpidemicIndependent;
                if (thisEpidemicIndependentProcess != null)
                    thisEpidemicIndependentProcess.UpdateRate(updatedParameterValues[thisEpidemicIndependentProcess.IDOfRateParameter]);

                // update the birth rate
                Process_Birth thisBirthProcess = thisProcess as Process_Birth;
                if (thisBirthProcess!= null)
                    thisBirthProcess.UpdateBirthRate(updatedParameterValues[thisBirthProcess.IDOfRateParameter]);
            }
        }        

        // update transmission rates affecting this class
        public override void UpdateTransmissionRates(double[] arrTransmissionRatesByPathogens)
        {
            // update the transmission rates
            foreach (Process thisProcess in _activeProcesses)
                thisProcess.UpdateTransmissionRate(arrTransmissionRatesByPathogens[thisProcess.IDOfPathogenToGenerate]);
        }

        //// update transmission rates affecting this class for epidemic dependent processes
        //public override void UpdateTransmissionRatesAffectingThisClass(int indexOfIntCombInTransmissionMatrix, double[][][][] arrTransmissionMatrices)
        //{
        //    // update the transmission rates
        //    foreach (Process thisProcess in _processes)
        //    {
        //        if (thisProcess is Process_EpidemicDependent)
        //            thisProcess.UpdateTransmissionRatesToGenerateInfections(arrTransmissionMatrices[indexOfIntCombInTransmissionMatrix][((Process_EpidemicDependent)thisProcess).IDOfPathogenToGenerate][this.ID]);
        //    }
        //}
        //// update proportion of members in this class
        //public override void UpdateProportionOfMembers(double[] arrProportionOfMembersInEachClass)
        //{
        //    foreach (Process process in _processes)
        //        process.UpdateProportionOfMembersInEachClass(arrProportionOfMembersInEachClass);  
        //}

        // select an intervention combination
        public override void SelectThisInterventionCombination(int[] interventionCombination)
        {
            // check if active processes should be updated
            if (_currentInterventionCombination != null && _currentInterventionCombination.SequenceEqual(interventionCombination))
                return;
            // if no process is attached, return
            if (_processes.Count == 0)
                return;

            // update the intervention combination 
            _currentInterventionCombination = (int[])interventionCombination.Clone();
            //_interventionCombinationCode = SupportFunctions.ConvertToBase10FromBase2(_currentInterventionCombination);

            // clear current active processes
            _activeProcesses.Clear();
            // update current active processes
            _isEpiDependentProcessActive = false;
            foreach (Process thisProcess in _processes)
            {
                // always add the processes that are activated                
                if (interventionCombination[thisProcess.IDOfActivatingIntervention] == 1)
                {
                    if ( thisProcess is Process_EpidemicDependent)
                        _isEpiDependentProcessActive = true;
                    _activeProcesses.Add(thisProcess);
                }
            }
            // store the id of the destination classes
            _arrNumOfMembersSendingToEachDestinationClasses = new long[_activeProcesses.Count];
            _arrDestinationClasseIDs = new int[_activeProcesses.Count];
            int i = 0;
            foreach (Process thisProcess in _activeProcesses)
                _arrDestinationClasseIDs[i++] = thisProcess.IDOfDestinationClass;
        }
        // send members of this class out
        public override void SendOutMembers(double deltaT, ThreadSpecificRNG threadSpecificRNG)
        {
            // all departing members will be processed
            //_ifMembersWaitingToSendOutBeforeNextDeltaT = false;

            // if number of members is zero, no member is departing
            if (_currentNumberOfMembers <= 0)
                return;

            int processIndex = 0;
            int numOfActiveProcesses = _activeProcesses.Count;
            double[] arrProcessRates = new double[numOfActiveProcesses];
            double[] arrProcessProbs = new double[numOfActiveProcesses + 1]; // note index 0 denotes not leaving the class
            // _arrNumOfMembersSendingToEachDestinationClasses = new long[numOfActiveProcesses];

            // then calculate the rates of processes
            processIndex = 0;
            double sumOfRates = 0;
            foreach (Process thisProcess in _activeProcesses)
            {
                // birth process does not affect the way members are leaving this class
                if (thisProcess.Type == Process.enumType.Birth)
                    arrProcessRates[processIndex] = 0;
                else
                {
                    arrProcessRates[processIndex] = thisProcess.Rate * deltaT;
                    sumOfRates += arrProcessRates[processIndex];
                }
                ++ processIndex;
            }

            // if the sum of rates is equal to zero, nothing is happening to this class!
            if (sumOfRates <= 0) return;

            // find the probabilities of each process   
            // calculate the probability of not leaving the class
            arrProcessProbs[0] = Math.Exp(-sumOfRates);
            // calculate the probability of other processes 
            double coeff = (1 - arrProcessProbs[0]) / sumOfRates;
            for (int probIndex = 1; probIndex <= numOfActiveProcesses; ++probIndex)
                arrProcessProbs[probIndex] = coeff * arrProcessRates[probIndex-1];

            // define a multinomial distribution for the number of members out of each process (process 0 denotes not leaving the class)
            Multinomial numOutOfProcessDistribution = new Multinomial("temp", _currentNumberOfMembers, arrProcessProbs, 5);
            // get a sample
            long[] arrSampledDepartures = SupportFunctions.ConvertArrayToLong(numOutOfProcessDistribution.arrSample(threadSpecificRNG));

            // handling error
            for (int i = 0; i< arrSampledDepartures.Length; i++)
                if (arrSampledDepartures[i]<0|| arrSampledDepartures[i]>_currentNumberOfMembers)
                {
                    //MessageBox.Show("Number of events cannot be negative (Seed: " + threadSpecificRNG.Seed + ").", "Error in Calculating Number of Events Out of Classes.");
                    arrSampledDepartures[i] = 0;
                }

            // find the number of members out of each process to other classes            
            processIndex = 0; // NOTE: process with index 0 denotes not leaving the class
            foreach (Process thisProcess in _activeProcesses)
            {
                // if this is a birth process
                if (thisProcess.Type == Process.enumType.Birth)
                {
                    // define a Poisson distribution
                    Poisson numOfBirthsDistribution = new Poisson("Birth", _currentNumberOfMembers * thisProcess.Rate * deltaT);
                    // get a sample on the number of births
                    long numOfBirths = (long)numOfBirthsDistribution.sample(threadSpecificRNG);
                    // record the number of members out of this process
                    thisProcess.MembersOutOverPastDeltaT = numOfBirths;
                    // find the number of members to the destination class
                    _arrNumOfMembersSendingToEachDestinationClasses[processIndex] += numOfBirths;
                }
                // if this is not a birth process
                else
                {
                    // update the current number of members
                    _currentNumberOfMembers -= arrSampledDepartures[processIndex + 1];
                    //if (_currentNumberOfMembers < 0)
                    //{
                    //    MessageBox.Show("Size of a class cannot be negative (Seed: " + threadSpecificRNG.Seed + ").", "Error in Calculating Size of a Class");
                    //    _currentNumberOfMembers = 0;
                    //}
                    // record the number of members out of this process
                    thisProcess.MembersOutOverPastDeltaT = arrSampledDepartures[processIndex + 1];
                    // find the number of members to the destination class
                    _arrNumOfMembersSendingToEachDestinationClasses[processIndex] += arrSampledDepartures[processIndex + 1];
                }
                ++processIndex;
            }

            _ifMembersWaitingToSendOutBeforeNextDeltaT = true;
        }
        // reset number of members sending to each destination class
        public override void ResetNumOfMembersSendingToEachDestinationClasses()
        {
            _arrNumOfMembersSendingToEachDestinationClasses = new long[_activeProcesses.Count];
            _ifMembersWaitingToSendOutBeforeNextDeltaT = false;
        }
        // find the members out of active processes
        public override void ReturnAndResetNumOfMembersOutOfProcessesOverPastDeltaT(ref long[] arrNumOfMembersOutOfProcessesOverPastDeltaT)
        {
            foreach (Process activeProcess in _activeProcesses)
            {
                arrNumOfMembersOutOfProcessesOverPastDeltaT[activeProcess.ID] += activeProcess.MembersOutOverPastDeltaT;
                activeProcess.MembersOutOverPastDeltaT = 0;
            }
        }

        // return current Cost
        public override double CurrentCost()
        {
            double currentClassCost = 0;
            // read this class statistics
            if (_countStatisticsNewMembers != null)
                currentClassCost += _countStatisticsNewMembers.CurrentCost;
            if (_countStatisticsMembersInClass != null)
                currentClassCost += _countStatisticsMembersInClass.CurrentCost;
            return currentClassCost;    
        }
        // return current QALY
        public override double CurrentQALY()
        {
            double currentClassQALY = 0;
            // read this class statistics
            if (_countStatisticsNewMembers != null)
                currentClassQALY += _countStatisticsNewMembers.CurrentQALY;
            if (_countStatisticsMembersInClass != null)
                currentClassQALY += _countStatisticsMembersInClass.CurrentQALY;
            return currentClassQALY;
        }
        // Reset statistics for another simulation run
        public override void ResetStatistics(double warmUpPeriodLength, bool ifToResetForAnotherSimulationRun)
        {
            // reset class state    
            if (ifToResetForAnotherSimulationRun)
            {
                _currentNumberOfMembers = _initialMembers;
                // reset class statistics
                ResetClassStatisticsForAnotherSimulationRun(warmUpPeriodLength);
            }
        }
    }

    // Class_Death
    public class Class_Death : Class
    {
        public Class_Death(int ID, string name)
            : base(ID, name)
        {
            _type = enumType.Death;
        }

        // return current Cost
        public override double CurrentCost()
        {
            double currentClassCost = 0;
            // read this class statistics
            if (_countStatisticsNewMembers != null)
                currentClassCost += _countStatisticsNewMembers.CurrentCost;            
            return currentClassCost;  
        }
        // return current QALY
        public override double CurrentQALY()
        {
            double currentClassQALY = 0;
            // read this class statistics
            if (_countStatisticsNewMembers != null)
                currentClassQALY += _countStatisticsNewMembers.CurrentQALY;            
            return currentClassQALY;
        }
        // Reset statistics for another simulation run
        public override void ResetStatistics(double warmUpPeriodLength, bool ifToResetForAnotherSimulationRun)
        {
            // reset class state
            if (ifToResetForAnotherSimulationRun)
                _currentNumberOfMembers = 0;
            // reset class statistics
            ResetClassStatisticsForAnotherSimulationRun(warmUpPeriodLength);
        }
    }    

    // Class_Splitting
    public class Class_Splitting: Class
    {           
        private int _parIDOfProbOfSuccess;
        private double _probOfSuccess;
        
        // Properties
        public override int[] ArrDestinationClasseIDs
        { get { return _arrDestinationClasseIDs; } }
        public override long[] ArrNumOfMembersSendingToEachDestinationClasses
        { get { return _arrNumOfMembersSendingToEachDestinationClasses; } }

        public Class_Splitting(int ID, string name)
            : base(ID, name)
        {
            _type = enumType.Splitting;
        }

        // Properties
        public int ParIDOfProbOfSuccess
        {
            get{return _parIDOfProbOfSuccess;}
        }

        // add the parameter ID for the probability of success
        public void SetUp(int parIDOfProbOfSuccess, int destinationClassIDGivenSuccess, int destinationClassIDGivenFailure)
        {
            _parIDOfProbOfSuccess = parIDOfProbOfSuccess;
            
            // store the id of the destination classes
            _arrNumOfMembersSendingToEachDestinationClasses = new long[2];
            _arrDestinationClasseIDs = new int[2];
            _arrDestinationClasseIDs[0] = destinationClassIDGivenSuccess;
            _arrDestinationClasseIDs[1] = destinationClassIDGivenFailure;
        }        
        // update the probability of success
        public override void UpdateProbOfSuccess(double[] arrSampledParameters)
        {
            _probOfSuccess = arrSampledParameters[_parIDOfProbOfSuccess];
        }
        
        // send members of this class out
        public override void SendOutMembers(double deltaT, ThreadSpecificRNG threadSpecificRNG)
        {
            // departing members will be processed
            //_ifMembersWaitingToSendOutBeforeNextDeltaT = false;

            // if number of members is zero, no member is departing
            if (_currentNumberOfMembers <= 0) return;

            // find the number of members sending to each class
            if (_probOfSuccess == 0)
            {
                _arrNumOfMembersSendingToEachDestinationClasses[0] += 0;
                _arrNumOfMembersSendingToEachDestinationClasses[1] += _currentNumberOfMembers;
            }
            else if (_probOfSuccess == 1)
            {
                _arrNumOfMembersSendingToEachDestinationClasses[0] += _currentNumberOfMembers;
                _arrNumOfMembersSendingToEachDestinationClasses[1] += 0;
            }
            else
            {
                // define a binomial distribution for the number of successes
                Bionomial numOfSuccesses = new Bionomial("temp", _currentNumberOfMembers, _probOfSuccess);
                // sample
                long sampledNumOfSuccesses = (long)numOfSuccesses.sample(threadSpecificRNG);

                _arrNumOfMembersSendingToEachDestinationClasses[0] += sampledNumOfSuccesses;
                _arrNumOfMembersSendingToEachDestinationClasses[1] += _currentNumberOfMembers - sampledNumOfSuccesses;

            }
            // current number of members should be zero now            
            _currentNumberOfMembers = 0;

            _ifMembersWaitingToSendOutBeforeNextDeltaT = true;
        }
        // reset number of members sending to each destination class
        public override void ResetNumOfMembersSendingToEachDestinationClasses()
        {
            _arrNumOfMembersSendingToEachDestinationClasses = new long[2];
            _ifMembersWaitingToSendOutBeforeNextDeltaT = false;
        }
        // return current Cost
        public override double CurrentCost()
        {
            double currentClassCost = 0;
            // read this class statistics
            if (_countStatisticsNewMembers != null)
                currentClassCost += _countStatisticsNewMembers.CurrentCost;
            return currentClassCost;
        }
        // return current QALY
        public override double CurrentQALY()
        {
            double currentClassQALY = 0;
            // read this class statistics
            if (_countStatisticsNewMembers != null)
                currentClassQALY += _countStatisticsNewMembers.CurrentQALY;
            return currentClassQALY;
        }
        // Reset statistics for another simulation run
        public override void ResetStatistics(double warmUpPeriodLength, bool ifToResetForAnotherSimulationRun)
        {
            // reset class state
            if (ifToResetForAnotherSimulationRun)
                _currentNumberOfMembers = 0;
            // reset class statistics
            ResetClassStatisticsForAnotherSimulationRun(warmUpPeriodLength);            
        }
    }   

    // Class_ResourceMonitor
    public class Class_ResourceMonitor : Class
    {
        private int _resourceIDToCheckAvailability;
        private double _resourceUnitsConsumedPerArrival;
        long[] _arrAvailableResources;
        long[] _arrResourcesConsumed;

        // Properties
        public override int[] ArrDestinationClasseIDs
        { get { return _arrDestinationClasseIDs; } }
        public override long[] ArrNumOfMembersSendingToEachDestinationClasses
        { get { return _arrNumOfMembersSendingToEachDestinationClasses; } }
        
        public Class_ResourceMonitor(int ID, string name)
            : base(ID, name)
        {
            _type = enumType.ResourceMonitor;
        }

        // Properties        
        public override long[] ArrResourcesConsumed
        {
            get { return _arrResourcesConsumed; }
        }
       
        // add a resource rule
        public void SetUp(int resourceIDToCheckAvailability, double resourceUnitsConsumedPerArrival, int destinationClassIDGivenSuccess, int destinationClassIDGivenFailure)
        {
            _resourceIDToCheckAvailability = resourceIDToCheckAvailability;
            _resourceUnitsConsumedPerArrival = resourceUnitsConsumedPerArrival;

            // store the id of the destination classes
            _arrNumOfMembersSendingToEachDestinationClasses = new long[2];
            _arrDestinationClasseIDs = new int[2];
            _arrDestinationClasseIDs[0] = destinationClassIDGivenSuccess;
            _arrDestinationClasseIDs[1] = destinationClassIDGivenFailure;
        }
        // update available resources
        public override void UpdateAvailableResources(long[] arrResourceAvailability)
        {
            _arrAvailableResources = (long[])arrResourceAvailability.Clone();
            _arrResourcesConsumed = new long[_arrAvailableResources.Length];
        }
        
        // send members of this class out
        public override void SendOutMembers(double deltaT, ThreadSpecificRNG threadSpecificRNG)
        {
            // departing members will be processed
            //_ifMembersWaitingToSendOutBeforeNextDeltaT = false;

            // if number of members is zero, no member is departing
            if (_currentNumberOfMembers <= 0) return;
            
            // find the number of members that can be served given the current resources
            long membersServed = (long)Math.Min(_currentNumberOfMembers, ((long)_arrAvailableResources[_resourceIDToCheckAvailability] / _resourceUnitsConsumedPerArrival));
            // update the resources consumed
            _arrResourcesConsumed[_resourceIDToCheckAvailability] = (long)(membersServed * _resourceUnitsConsumedPerArrival);

            // find the number of members sent to each class from this class
            _arrNumOfMembersSendingToEachDestinationClasses[0] = membersServed;
            _arrNumOfMembersSendingToEachDestinationClasses[0] = _currentNumberOfMembers - membersServed;            
            // update the current number of members
            _currentNumberOfMembers =0;

            _ifMembersWaitingToSendOutBeforeNextDeltaT = true;
        }
        // reset number of members sending to each destination class
        public override void ResetNumOfMembersSendingToEachDestinationClasses()
        {
            _arrNumOfMembersSendingToEachDestinationClasses = new long[2];
            _ifMembersWaitingToSendOutBeforeNextDeltaT = false;
        }

        // return current Cost
        public override double CurrentCost()
        {
            double currentClassCost = 0;
            // read this class statistics
            if (_countStatisticsNewMembers != null)
                currentClassCost += _countStatisticsNewMembers.CurrentCost;
            return currentClassCost;
        }
        // return current QALY
        public override double CurrentQALY()
        {
            double currentClassQALY = 0;
            // read this class statistics
            if (_countStatisticsNewMembers != null)
                currentClassQALY += _countStatisticsNewMembers.CurrentQALY;
            return currentClassQALY;
        }
        // Reset statistics for another simulation run
        public override void ResetStatistics(double warmUpPeriodLength, bool ifToResetForAnotherSimulationRun)
        {
            // reset class state
            if(ifToResetForAnotherSimulationRun)
                _currentNumberOfMembers = 0;
            // reset class statistics
            ResetClassStatisticsForAnotherSimulationRun(warmUpPeriodLength);
        }
    }
}