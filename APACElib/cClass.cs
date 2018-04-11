using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomVariateLib;
using ComputationLib;
using System.Windows.Forms;

namespace APACElib
{
    // Class
    public abstract class Class
    {
        protected int _ID;
        protected string _name;
        protected int _rowIndexInContactMatrix;
        protected int[] _arrDestinationClasseIDs;
        protected int[] _arrNumOfMembersSendingToEachDestinationClasses;        
        public bool IfNeedsToBeProcessed { get; set; }
        public bool IfMembersWaitingToSendOutBeforeNextDeltaT { get; set; }
        // statistics
        public GeneralTrajectory ClassStat { get; set; }

        // show in simulation output 
        public bool ShowIncidence { get; set; }
        public bool ShowPrevalence { get; set; }
        public bool ShowAccumIncidence { get; set; }
        public bool ShowStatisticsInSimulationResults { get; set; }

        // Instantiation
        public Class(int ID, string name)
        {
            _ID = ID;
            _name = name;
            IfNeedsToBeProcessed = true;
            IfMembersWaitingToSendOutBeforeNextDeltaT = false;
        }

        // Properties
        public int ID
        {
            get{return _ID;}
        }
        public string Name
        {
            get {return _name;}
        }

        public virtual int[] ArrDestinationClasseIDs
        { get { return new int[0]; } }
        public virtual int[] ArrNumOfMembersSendingToEachDestinationClasses
        { get { return new int[0]; } }
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
        public virtual int[] ArrResourcesConsumed
        {
            get { return new int[0]; }
        }
        public virtual bool IsEpiDependentEventActive
        {
            get { return false; }
        }
      
        // read feature value  
        public double ReadFeatureValue(Feature_DefinedOnNewClassMembers feature)
        {
            double value = 0;            
            switch (feature.FeatureType)
            {
                case Feature.enumFeatureType.Incidence:
                    value = ClassStat.IncidenceTimeSeries.GetLastObs();
                    break;
                case Feature.enumFeatureType.Prediction:
                    value = 0; // Math.Max(0, _countStatisticsNewMembers.Prediction(feature.NumOfTimePeriodsForFuturePrediction));
                    break;
                case Feature.enumFeatureType.AccumulatingIncidence:
                    value = ClassStat.AccumulatedIncidence;
                    break;
                default:
                    value = -1;
                    break;
            }  
            return value;
        }

        //  add new member
        public virtual void AddNewMembers(int numOfNewMembers) { }
        // update the initial number of members
        public virtual void UpdateInitialNumberOfMembers(int sampledValue)
        {         
        }
        // update rates of epidemic independent processes associated to this class
        public virtual void UpdateRatesOfBirthAndEpiIndpEvents(double[] updatedParameterValues)
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
        public virtual void SendOutMembers(double deltaT, RNG rng)
        {
        }
        // reset number of members sending to each destination class
        public virtual void ResetNumOfMembersSendingToEachDestinationClasses()
        { }
        // find the members out of active processes
        public virtual void ResetNumOfMembersOutOfEventsOverPastDeltaT()//(ref int[] arrNumOfMembersOutOfProcessesOverPastDeltaT)
        { }

        // reset for another simulation run
        public virtual void Reset()
        {} 
        // update available resources
        public virtual void UpdateAvailableResources(int[] arrResourceAvailability)
        { }

    }

    // Class_Normal
    public class Class_Normal : Class
    {
        // transmission matrix
        private int _initialMembersParID;
        private int InitialMembers { get; set; }

        private int[] _susceptibilityParIDs;
        private int[] _infectivityParIDs;
        private double[] _susceptibilityValues;
        private double[] _infectivityValues;

        private bool _isEpiDependentEventActive = false;
        private bool _emptyToEradicate;

        private List<Event> _events = new List<Event>();
        private List<Event> _activeEvents = new List<Event>();
        private int[] _currentInterventionCombination;

        public Class_Normal(int ID, string name)
            : base(ID, name)
        {
        }

        public override int InitialMemebersParID
        {
            get { return _initialMembersParID; }
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
        public override bool IsEpiDependentEventActive
        {
            get{return _isEpiDependentEventActive;}
        }
        public override int[] ArrDestinationClasseIDs
        { get { return _arrDestinationClasseIDs; } }
        public override int[] ArrNumOfMembersSendingToEachDestinationClasses
        { get { return _arrNumOfMembersSendingToEachDestinationClasses; } }

        // add an event
        public void AddAnEvent(Event e)
        {
            _events.Add(e);
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
        public override void UpdateInitialNumberOfMembers(int sampledValue)
        {
            InitialMembers = sampledValue;
            ClassStat.Prevalence= sampledValue;
        }
        // update susceptibility and infectivity values
        public override void UpdateSusceptibilityParameterValues(double[] arrSampledParameterValues)
        {
            for (int i = 0; i < _susceptibilityParIDs.Length; i++)
                _susceptibilityValues[i] = Math.Max(0, arrSampledParameterValues[_susceptibilityParIDs[i]]);
        }
        // update infectivity values
        public override void UpdateInfectivityParameterValues(double[] arrSampledParameterValues)
        {
            for (int i = 0; i < _infectivityParIDs.Length; i++)
                _infectivityValues[i] = Math.Max(0, arrSampledParameterValues[_infectivityParIDs[i]]);
        }
        // update rates of epidemic independent processes associated to this class
        public override void UpdateRatesOfBirthAndEpiIndpEvents(double[] updatedParameterValues)
        {
            foreach (Event thisProcess in _activeEvents)
            {
                // update the epidemic independent rate
                Event_EpidemicIndependent thisEpiIndpEvent = thisProcess as Event_EpidemicIndependent;
                if (thisEpiIndpEvent != null)
                    thisEpiIndpEvent.UpdateRate(updatedParameterValues[thisEpiIndpEvent.IDOfRateParameter]);

                // update the birth rate
                Event_Birth thisBirthEvent = thisProcess as Event_Birth;
                if (thisBirthEvent!= null)
                    thisBirthEvent.UpdateBirthRate(updatedParameterValues[thisBirthEvent.IDOfRateParameter]);
            }
        }        

        // update transmission rates affecting this class
        public override void UpdateTransmissionRates(double[] arrTransmissionRatesByPathogens)
        {
            // update the transmission rates
            foreach (Event thisEvent in _activeEvents)
                thisEvent.UpdateTransmissionRate(arrTransmissionRatesByPathogens[thisEvent.IDOfPathogenToGenerate]);
        }

        // select an intervention combination
        public override void SelectThisInterventionCombination(int[] interventionCombination)
        {
            // check if active processes should be updated
            if (_currentInterventionCombination != null && _currentInterventionCombination.SequenceEqual(interventionCombination))
                return;
            // if no process is attached, return
            if (_events.Count == 0)
                return;

            // update the intervention combination 
            _currentInterventionCombination = (int[])interventionCombination.Clone();

            // clear current active processes
            _activeEvents.Clear();
            // update current active processes
            _isEpiDependentEventActive = false;
            foreach (Event e in _events)
            {
                // always add the processes that are activated                
                if (interventionCombination[e.IDOfActivatingIntervention] == 1)
                {
                    if ( e is Event_EpidemicDependent)
                        _isEpiDependentEventActive = true;
                    _activeEvents.Add(e);
                }
            }
            // store the id of the destination classes
            _arrNumOfMembersSendingToEachDestinationClasses = new int[_activeEvents.Count];
            _arrDestinationClasseIDs = new int[_activeEvents.Count];
            int i = 0;
            foreach (Event thisProcess in _activeEvents)
                _arrDestinationClasseIDs[i++] = thisProcess.IDOfDestinationClass;
        }
        // send members of this class out
        public override void SendOutMembers(double deltaT, RNG rng)
        {
            // if number of members is zero, no member is departing
            if (ClassStat.Prevalence<= 0)
                return;

            int eIndex = 0;
            int numOfActiveEvents = _activeEvents.Count;
            double[] arrEventRates = new double[numOfActiveEvents];
            double[] arrEventProbs = new double[numOfActiveEvents + 1]; // note index 0 denotes not leaving the class

            // then calculate the rates of events
            eIndex = 0;
            double sumOfRates = 0;
            foreach (Event thisEvent in _activeEvents)
            {
                // birth event does not affect the way members are leaving this class
                if (thisEvent is Event_Birth)
                    arrEventRates[eIndex] = 0;
                else
                {
                    arrEventRates[eIndex] = thisEvent.Rate * deltaT;
                    sumOfRates += arrEventRates[eIndex];
                }
                ++ eIndex;
            }

            // if the sum of rates is equal to zero, nothing is happening to this class!
            if (sumOfRates <= 0) return;

            // find the probabilities of each process   
            // calculate the probability of not leaving the class
            arrEventProbs[0] = Math.Exp(-sumOfRates);
            // calculate the probability of other processes 
            double coeff = (1 - arrEventProbs[0]) / sumOfRates;
            for (int probIndex = 1; probIndex <= numOfActiveEvents; ++probIndex)
                arrEventProbs[probIndex] = coeff * arrEventRates[probIndex-1];

            // define a multinomial distribution for the number of members out of each process (process 0 denotes not leaving the class)
            Multinomial numOutOfProcessDistribution = new Multinomial("temp", ClassStat.Prevalence, arrEventProbs);
            // get a sample
            int[] arrSampledDepartures = numOutOfProcessDistribution.ArrSampleDiscrete(rng);

           // find the number of members out of each process to other classes            
            eIndex = 0; // NOTE: process with index 0 denotes not leaving the class
            foreach (Event thisProcess in _activeEvents)
            {
                // if this is a birth process
                if (thisProcess is Event_Birth)
                {
                    int numOfBirths = 0;
                    if (thisProcess.Rate == 0)
                        numOfBirths = 0;
                    else
                    {
                        // define a Poisson distribution
                        Poisson numOfBirthsDistribution = new Poisson("Birth", ClassStat.Prevalence * thisProcess.Rate * deltaT);
                        // get a sample on the number of births
                        numOfBirths = numOfBirthsDistribution.SampleDiscrete(rng);
                    }
                    // record the number of members out of this process
                    thisProcess.MembersOutOverPastDeltaT = numOfBirths;
                    // find the number of members to the destination class
                    _arrNumOfMembersSendingToEachDestinationClasses[eIndex] += numOfBirths;
                }
                // if this is not a birth process
                else
                {
                    // update the current number of members
                    ClassStat.Prevalence -= arrSampledDepartures[eIndex + 1];
                    // record the number of members out of this process
                    thisProcess.MembersOutOverPastDeltaT = arrSampledDepartures[eIndex + 1];
                    // find the number of members to the destination class
                    _arrNumOfMembersSendingToEachDestinationClasses[eIndex] += arrSampledDepartures[eIndex + 1];
                }
                ++eIndex;
            }

            IfMembersWaitingToSendOutBeforeNextDeltaT = true;
        }
        // reset number of members sending to each destination class
        public override void ResetNumOfMembersSendingToEachDestinationClasses()
        {
            _arrNumOfMembersSendingToEachDestinationClasses = new int[_activeEvents.Count];
            IfMembersWaitingToSendOutBeforeNextDeltaT = false;
        }
        // find the members out of active processes
        public override void ResetNumOfMembersOutOfEventsOverPastDeltaT()//(ref int[] arrNumOfMembersOutOfProcessesOverPastDeltaT)
        {
            foreach (Event activeProcess in _activeEvents)
            {
                //arrNumOfMembersOutOfProcessesOverPastDeltaT[activeProcess.ID] += activeProcess.MembersOutOverPastDeltaT;
                activeProcess.MembersOutOverPastDeltaT = 0;
            }
        }

        // add new members
        public override void AddNewMembers(int numOfNewMembers)
        {
            ClassStat.Add(numOfNewMembers);         
        }
        // Reset statistics for another simulation run
        public override void Reset()
        {
            IfNeedsToBeProcessed = true;
            ClassStat.Reset();
            ClassStat.Prevalence = InitialMembers;  
        }
    }

    // Class_Death
    public class Class_Death : Class
    {
        public Class_Death(int ID, string name)
            : base(ID, name)
        {
        }
        // Reset statistics for another simulation run
        public override void Reset()
        {
            IfNeedsToBeProcessed = true;
            ClassStat.Reset();
        }
        // add new members
        public override void AddNewMembers(int numOfNewMembers)
        {
            ClassStat.Add(numOfNewMembers);
            ClassStat.Prevalence = 0;       
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
        public override int[] ArrNumOfMembersSendingToEachDestinationClasses
        { get { return _arrNumOfMembersSendingToEachDestinationClasses; } }

        public Class_Splitting(int ID, string name)
            : base(ID, name)
        {
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
            _arrNumOfMembersSendingToEachDestinationClasses = new int[2];
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
        public override void SendOutMembers(double deltaT, RNG rng)
        {
            // departing members will be processed
            //_ifMembersWaitingToSendOutBeforeNextDeltaT = false;

            // if number of members is zero, no member is departing
            if (ClassStat.Prevalence <= 0) return;

            // find the number of members sending to each class
            if (_probOfSuccess == 0)
            {
                _arrNumOfMembersSendingToEachDestinationClasses[0] += 0;
                _arrNumOfMembersSendingToEachDestinationClasses[1] += ClassStat.Prevalence;
            }
            else if (_probOfSuccess == 1)
            {
                _arrNumOfMembersSendingToEachDestinationClasses[0] += ClassStat.Prevalence;
                _arrNumOfMembersSendingToEachDestinationClasses[1] += 0;
            }
            else
            {
                // define a binomial distribution for the number of successes
                Bionomial numOfSuccesses = new Bionomial("temp", ClassStat.Prevalence, _probOfSuccess);
                // sample
                int sampledNumOfSuccesses = numOfSuccesses.SampleDiscrete(rng);

                _arrNumOfMembersSendingToEachDestinationClasses[0] += sampledNumOfSuccesses;
                _arrNumOfMembersSendingToEachDestinationClasses[1] += ClassStat.Prevalence - sampledNumOfSuccesses;

            }
            // current number of members should be zero now            
            ClassStat.Prevalence = 0;

            IfMembersWaitingToSendOutBeforeNextDeltaT = true;
        }
        // reset number of members sending to each destination class
        public override void ResetNumOfMembersSendingToEachDestinationClasses()
        {
            _arrNumOfMembersSendingToEachDestinationClasses = new int[2];
            ClassStat.Prevalence = 0;
            IfMembersWaitingToSendOutBeforeNextDeltaT = false;
        }
        // Reset statistics for another simulation run
        public override void Reset()
        {
            IfNeedsToBeProcessed = true;
            ClassStat.Reset();
        }

        // add new members
        public override void AddNewMembers(int numOfNewMembers)
        {
            ClassStat.Add(numOfNewMembers);
            IfNeedsToBeProcessed = true;
        }
    }   

    // Class_ResourceMonitor
    public class Class_ResourceMonitor : Class
    {
        private int _resourceIDToCheckAvailability;
        private double _resourceUnitsConsumedPerArrival;
        int[] _arrAvailableResources;
        int[] _arrResourcesConsumed;

        // Properties
        public override int[] ArrDestinationClasseIDs
        { get { return _arrDestinationClasseIDs; } }
        public override int[] ArrNumOfMembersSendingToEachDestinationClasses
        { get { return _arrNumOfMembersSendingToEachDestinationClasses; } }
        
        public Class_ResourceMonitor(int ID, string name)
            : base(ID, name)
        {
        }

        // Properties        
        public override int[] ArrResourcesConsumed
        {
            get { return _arrResourcesConsumed; }
        }
       
        // add a resource rule
        public void SetUp(int resourceIDToCheckAvailability, double resourceUnitsConsumedPerArrival, int destinationClassIDGivenSuccess, int destinationClassIDGivenFailure)
        {
            _resourceIDToCheckAvailability = resourceIDToCheckAvailability;
            _resourceUnitsConsumedPerArrival = resourceUnitsConsumedPerArrival;

            // store the id of the destination classes
            _arrNumOfMembersSendingToEachDestinationClasses = new int[2];
            _arrDestinationClasseIDs = new int[2];
            _arrDestinationClasseIDs[0] = destinationClassIDGivenSuccess;
            _arrDestinationClasseIDs[1] = destinationClassIDGivenFailure;
        }
        // update available resources
        public override void UpdateAvailableResources(int[] arrResourceAvailability)
        {
            _arrAvailableResources = (int[])arrResourceAvailability.Clone();
            _arrResourcesConsumed = new int[_arrAvailableResources.Length];
        }
        
        // send members of this class out
        public override void SendOutMembers(double deltaT, RNG rng)
        {
            // departing members will be processed
            //_ifMembersWaitingToSendOutBeforeNextDeltaT = false;

            // if number of members is zero, no member is departing
            if (ClassStat.Prevalence <= 0) return;
            
            // find the number of members that can be served given the current resources
            int membersServed = (int)Math.Min(ClassStat.Prevalence, (_arrAvailableResources[_resourceIDToCheckAvailability] / _resourceUnitsConsumedPerArrival));
            // update the resources consumed
            _arrResourcesConsumed[_resourceIDToCheckAvailability] = (int)(membersServed * _resourceUnitsConsumedPerArrival);

            // find the number of members sent to each class from this class
            _arrNumOfMembersSendingToEachDestinationClasses[0] = membersServed;
            _arrNumOfMembersSendingToEachDestinationClasses[0] = ClassStat.Prevalence - membersServed;            
            // update the current number of members
            ClassStat.Prevalence =0;

            IfMembersWaitingToSendOutBeforeNextDeltaT = true;
        }
        // reset number of members sending to each destination class
        public override void ResetNumOfMembersSendingToEachDestinationClasses()
        {
            _arrNumOfMembersSendingToEachDestinationClasses = new int[2];
            IfMembersWaitingToSendOutBeforeNextDeltaT = false;
        }

        // Reset for another simulation run
        public override void Reset()
        {
            IfNeedsToBeProcessed = true;
            ClassStat.Reset();
        }

        // add new members
        public override void AddNewMembers(int numOfNewMembers)
        {
            ClassStat.Add(numOfNewMembers);
            IfNeedsToBeProcessed = true;
        }
    }
}