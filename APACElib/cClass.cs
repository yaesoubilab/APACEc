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
        protected int[] _destinationClasseIDs;
        protected int[] _numOfMembersToDestClasses;     // number of members to be sent to other classes        
        public bool ShouldBeProcessed { get; set; }     // if it should be decided how many members to send to other classes 
        public bool MembersWaitingToDepart { get; set; } // if there are members waiting to be sent to other classes 
        public GeneralTrajectory ClassStat { get; set; }

        // show in simulation output 
        public bool ShowIncidence { get; set; }
        public bool ShowPrevalence { get; set; }
        public bool ShowAccumIncidence { get; set; }
        public bool ShowStatsInSimResults { get; set; } // show this class statistics in the simulation results

        public Class(int ID, string name)
        {
            _ID = ID;
            _name = name;
            ShouldBeProcessed = true;
            MembersWaitingToDepart = false;
        }

        // Properties
        public int ID
        {
            get { return _ID; } }
        public string Name
        {
            get {return _name;}
        }
        public int[] DestinationClasseIDs
        {
            get { return _destinationClasseIDs; }
        }
        public int[] NumOfMembersToDestClasses
        {
            get { return _numOfMembersToDestClasses; }
        }
        public virtual bool EmptyToEradicate
        {
            get { return false; }
        }
        public virtual int InitialMemebersParID
        {
            get { return 0; }
        }
        public virtual int RowIndexInContactMatrix
        {
            get { return 0; }
        }
        public virtual double[] SusceptibilityValues
        {
            get { return new double[0]; }
        }
        public virtual double[] InfectivityValues
        {
            get { return new double[0]; }
        }
        public virtual int[] ResourcesConsumed
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
        public virtual void UpdateInitialNumOfMembers(int sampledValue) { }
        // update rates of epidemic independent processes associated to this class
        public virtual void UpdateRatesOfBirthAndEpiIndpEvents(double[] updatedParameterValues) { }
        // update susceptibility values
        public virtual void UpdateSusceptibilityParams(double[] arrSampledParameterValues) { }
        // update infectivity values
        public virtual void UpdateInfectivityParams(double[] arrSampledParameterValues) { }
        // update the probability of success
        public virtual void UpdateProbOfSuccess(double[] arrSampledParameters) { }
        // select this intervention combination 
        public virtual void UpdateIntrvnCombination(int[] interventionCombination) {}
        // update transmission rates affecting this class
        public virtual void UpdateTransmissionRates(double[] transmissionRatesByPathogens) { }
        // send out members
        public virtual void SendOutMembers(double deltaT, RNG rng) { }
        // reset number of members sending to each destination class
        public virtual void ResetNumOfMembersToDestClasses() { }
        // find the members out of active processes
        public virtual void ResetNumOfMembersOutOfEvents() { } //(ref int[] arrNumOfMembersOutOfProcessesOverPastDeltaT)
        // reset for another simulation run
        public virtual void Reset() { } 
        // update available resources
        public virtual void UpdateAvailableResources(int[] resourceAvailability) { }

    }

    // Class_Normal
    public class Class_Normal : Class
    {
        // transmission matrix
        private int _initialMembersParID;
        private int InitialMembers { get; set; }

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
        public int[] SusceptibilityParIDs { get; private set; }
        public int[] InfectivityParIDs { get; private set; }
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

        // add an event
        public void AddAnEvent(Event e)
        {
            _events.Add(e);
        }
        // setup the initial number parID
        public void SetupInitialAndStoppingConditions(
            int initialMembersParID, 
            bool ifShouldBeEmptyForEradication)
        {
            _initialMembersParID = initialMembersParID;
            _emptyToEradicate = ifShouldBeEmptyForEradication;
        }
        // set up transmission dynamics properties
        public void SetupTransmissionDynamicsProperties(
            string susceptibilityParamIDs, 
            string infectivityParamIDs, int 
            rowIndexInContactMatrix)
        {
            // remove brackets
            susceptibilityParamIDs = susceptibilityParamIDs.Replace(" ", "");
            susceptibilityParamIDs = susceptibilityParamIDs.Replace("{", "");
            susceptibilityParamIDs = susceptibilityParamIDs.Replace("}", "");
            infectivityParamIDs = infectivityParamIDs.Replace(" ", "");
            infectivityParamIDs = infectivityParamIDs.Replace("{", "");
            infectivityParamIDs = infectivityParamIDs.Replace("}", "");
            // convert to array
            SusceptibilityParIDs = Array.ConvertAll(susceptibilityParamIDs.Split(','), Convert.ToInt32);
            _susceptibilityValues = new double[SusceptibilityParIDs.Length];

            InfectivityParIDs = Array.ConvertAll(infectivityParamIDs.Split(','), Convert.ToInt32);
            _infectivityValues = new double[InfectivityParIDs.Length];

            _rowIndexInContactMatrix = rowIndexInContactMatrix;
        }
        // update the initial number of members
        public override void UpdateInitialNumOfMembers(int value)
        {
            InitialMembers = value;
            ClassStat.Prevalence= value;
        }
        // update susceptibility and infectivity values
        public override void UpdateSusceptibilityParams(double[] values)
        {
            for (int i = 0; i < SusceptibilityParIDs.Length; i++)
                _susceptibilityValues[i] = Math.Max(0, values[SusceptibilityParIDs[i]]);
        }

        // update infectivity values
        public override void UpdateInfectivityParams(double[] values)
        {
            for (int i = 0; i < InfectivityParIDs.Length; i++)
                _infectivityValues[i] = Math.Max(0, values[InfectivityParIDs[i]]);
        }
        // update rates of epidemic independent processes associated to this class
        public override void UpdateRatesOfBirthAndEpiIndpEvents(double[] values)
        {
            foreach (Event thisEvent in _activeEvents.Where(e => e.IDOfRateParameter > 0))
                thisEvent.UpdateRate(values[thisEvent.IDOfRateParameter]);
        }        

        // update transmission rates affecting this class
        public override void UpdateTransmissionRates(double[] transmissionRatesByPathogen)
        {
            // update the transmission rates
            foreach (Event thisEvent in _activeEvents.Where(e => e is Event_EpidemicDependent))
                thisEvent.UpdateRate(transmissionRatesByPathogen[thisEvent.IDOfPathogenToGenerate]);
        }

        // select an intervention combination
        public override void UpdateIntrvnCombination(int[] interventionCombination)
        {
            // check if active processes should be updated
            if (_currentInterventionCombination != null && _currentInterventionCombination.SequenceEqual(interventionCombination))
                return;
            // if no event is attached, return
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
                // add the events that are activated                
                if (interventionCombination[e.IDOfActivatingIntervention] == 1)
                {
                    if ( e is Event_EpidemicDependent)
                        _isEpiDependentEventActive = true;
                    _activeEvents.Add(e);
                }
            }
            // store the id of the destination classes
            _numOfMembersToDestClasses = new int[_activeEvents.Count];
            _destinationClasseIDs = new int[_activeEvents.Count];
            int i = 0;
            foreach (Event thisProcess in _activeEvents)
                _destinationClasseIDs[i++] = thisProcess.IDOfDestinationClass;
        }
        // send members of this class out
        public override void SendOutMembers(double deltaT, RNG rng)
        {
            // if number of members is zero, no member is departing
            if (ClassStat.Prevalence<= 0)
                return;

            int eIndex = 0;
            int numOfActiveEvents = _activeEvents.Count;
            double[] eventRates = new double[numOfActiveEvents];
            double[] eventProbs = new double[numOfActiveEvents + 1]; // note index 0 denotes not leaving the class

            // calculate the rates of events
            eIndex = 0;
            double sumOfRates = 0;
            foreach (Event thisEvent in _activeEvents)
            {
                // birth event does not affect the way members are leaving this class
                if (thisEvent is Event_Birth)
                    eventRates[eIndex] = 0;
                else
                {
                    eventRates[eIndex] = thisEvent.Rate * deltaT;
                    sumOfRates += eventRates[eIndex];
                }
                ++ eIndex;
            }

            // if the sum of rates is equal to zero, nothing is happening to this class!
            if (sumOfRates <= 0) return;

            // find the probabilities of each process   
            // calculate the probability of not leaving the class
            eventProbs[0] = Math.Exp(-sumOfRates);
            // calculate the probability of other processes 
            double coeff = (1 - eventProbs[0]) / sumOfRates;
            for (int probIndex = 1; probIndex <= numOfActiveEvents; ++probIndex)
                eventProbs[probIndex] = coeff * eventRates[probIndex-1];

            // define a multinomial distribution for the number of members out of each process (process 0 denotes not leaving the class)
            int[] arrSampledDepartures = new Multinomial("temp", ClassStat.Prevalence, eventProbs).ArrSampleDiscrete(rng);

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
                        // get a sample on the number of births
                        numOfBirths = new Poisson("Birth", ClassStat.Prevalence * thisProcess.Rate * deltaT).SampleDiscrete(rng);
                    }
                    // record the number of members out of this process
                    thisProcess.MembersOutOverPastDeltaT = numOfBirths;
                    // find the number of members to the destination class
                    _numOfMembersToDestClasses[eIndex] += numOfBirths;
                }
                // if this is not a birth process
                else
                {
                    // update the current number of members
                    ClassStat.Prevalence -= arrSampledDepartures[eIndex + 1];
                    // record the number of members out of this process
                    thisProcess.MembersOutOverPastDeltaT = arrSampledDepartures[eIndex + 1];
                    // find the number of members to the destination class
                    _numOfMembersToDestClasses[eIndex] += arrSampledDepartures[eIndex + 1];
                }
                ++eIndex;
            }

            MembersWaitingToDepart = true;
        }
        // reset number of members sending to each destination class
        public override void ResetNumOfMembersToDestClasses()
        {
            _numOfMembersToDestClasses = new int[_activeEvents.Count];
            MembersWaitingToDepart = false;
        }
        // find the members out of active processes
        public override void ResetNumOfMembersOutOfEvents()//(ref int[] arrNumOfMembersOutOfProcessesOverPastDeltaT)
        {
            foreach (Event activeProcess in _activeEvents)
                activeProcess.MembersOutOverPastDeltaT = 0;
        }

        // add new members
        public override void AddNewMembers(int numOfNewMembers)
        {
            ClassStat.Add(numOfNewMembers);         
        }
        // Reset statistics for another simulation run
        public override void Reset()
        {
            ShouldBeProcessed = true;
            MembersWaitingToDepart = false;
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
            ShouldBeProcessed = true;
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
        private double _probOfSuccess;
        
        // Properties
        public Class_Splitting(int ID, string name)
            : base(ID, name)
        {
        }

        // Properties
        public int ParIDOfProbOfSuccess { get; private set; }

        // add the parameter ID for the probability of success
        public void SetUp(int parIDOfProbOfSuccess, int destinationClassIDGivenSuccess, int destinationClassIDGivenFailure)
        {
            ParIDOfProbOfSuccess = parIDOfProbOfSuccess;
            
            // store the id of the destination classes
            _numOfMembersToDestClasses = new int[2];
            _destinationClasseIDs = new int[2];
            _destinationClasseIDs[0] = destinationClassIDGivenSuccess;
            _destinationClasseIDs[1] = destinationClassIDGivenFailure;
        }        
        // update the probability of success
        public override void UpdateProbOfSuccess(double[] arrSampledParameters)
        {
            _probOfSuccess = arrSampledParameters[ParIDOfProbOfSuccess];
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
                _numOfMembersToDestClasses[0] += 0;
                _numOfMembersToDestClasses[1] += ClassStat.Prevalence;
            }
            else if (_probOfSuccess == 1)
            {
                _numOfMembersToDestClasses[0] += ClassStat.Prevalence;
                _numOfMembersToDestClasses[1] += 0;
            }
            else
            {
                // define a binomial distribution for the number of successes
                int sampledNumOfSuccesses = new Bionomial("temp", ClassStat.Prevalence, _probOfSuccess).SampleDiscrete(rng);

                _numOfMembersToDestClasses[0] += sampledNumOfSuccesses;
                _numOfMembersToDestClasses[1] += ClassStat.Prevalence - sampledNumOfSuccesses;

            }
            // current number of members should be zero now            
            ClassStat.Prevalence = 0;

            MembersWaitingToDepart = true;
        }
        // reset number of members sending to each destination class
        public override void ResetNumOfMembersToDestClasses()
        {
            _numOfMembersToDestClasses = new int[2];
            ClassStat.Prevalence = 0;
            MembersWaitingToDepart = false;
        }
        // Reset statistics for another simulation run
        public override void Reset()
        {
            ShouldBeProcessed = true;
            ClassStat.Reset();
        }

        // add new members
        public override void AddNewMembers(int numOfNewMembers)
        {
            ClassStat.Add(numOfNewMembers);
            ShouldBeProcessed = true;
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
        public Class_ResourceMonitor(int ID, string name)
            : base(ID, name)
        {
        }

        // Properties        
        public override int[] ResourcesConsumed
        {
            get { return _arrResourcesConsumed; }
        }
       
        // add a resource rule
        public void SetUp(int resourceIDToCheckAvailability, double resourceUnitsConsumedPerArrival, int destinationClassIDGivenSuccess, int destinationClassIDGivenFailure)
        {
            _resourceIDToCheckAvailability = resourceIDToCheckAvailability;
            _resourceUnitsConsumedPerArrival = resourceUnitsConsumedPerArrival;

            // store the id of the destination classes
            _numOfMembersToDestClasses = new int[2];
            _destinationClasseIDs = new int[2];
            _destinationClasseIDs[0] = destinationClassIDGivenSuccess;
            _destinationClasseIDs[1] = destinationClassIDGivenFailure;
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
            _numOfMembersToDestClasses[0] = membersServed;
            _numOfMembersToDestClasses[0] = ClassStat.Prevalence - membersServed;            
            // update the current number of members
            ClassStat.Prevalence =0;

            MembersWaitingToDepart = true;
        }
        // reset number of members sending to each destination class
        public override void ResetNumOfMembersToDestClasses()
        {
            _numOfMembersToDestClasses = new int[2];
            MembersWaitingToDepart = false;
        }

        // Reset for another simulation run
        public override void Reset()
        {
            ShouldBeProcessed = true;
            ClassStat.Reset();
        }

        // add new members
        public override void AddNewMembers(int numOfNewMembers)
        {
            ClassStat.Add(numOfNewMembers);
            ShouldBeProcessed = true;
        }
    }
}