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
        public int ID { get; private set; }
        public string Name { get; private set; }
        protected int _rowIndexInContactMatrix;
        protected int[] _destinationClasseIDs;
        public int[] DestinationClasseIDs { get => _destinationClasseIDs; }
        protected int[] _numOfMembersToDestClasses;     // number of members to be sent to other classes   
        public int[] NumOfMembersToDestClasses { get => _numOfMembersToDestClasses; }
        public bool ShouldBeProcessed { get; set; }     // if it should be decided how many members to send to other classes 
        public bool MembersWaitingToDepart { get; set; } // if there are members waiting to be sent to other classes 
        public OneDimTrajectory ClassStat { get; set; }

        // show in simulation output 
        public bool ShowIncidence { get; set; }
        public bool ShowPrevalence { get; set; }
        public bool ShowAccumIncidence { get; set; }

        public Class(int id, string name)
        {
            ID = id;
            Name = name;
            ShouldBeProcessed = true;
            MembersWaitingToDepart = false;
        }

        public virtual bool EmptyToEradicate => false;
        public virtual int RowIndexInContactMatrix => 0;
        public virtual int[] ResourcesConsumed => new int[0];
        public virtual bool IsEpiDependentEventActive => false;
      
        //  add new member
        public virtual void AddNewMembers(int numOfNewMembers) { }
        // update the initial number of members
        public virtual void UpdateInitialNumOfMembers() { }

        // get susceptibility and infectity values 
        public virtual double[] GetSusceptibilityValues() { return null; }
        public virtual double[] GetInfectivityValues() { return null; }

          // add active events
        public virtual void AddActiveEvents(int[] interventionCombination) {}
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
        private Parameter _initialMembersPar;
        private int InitialMembers { get; set; }

        private List<Parameter> _susceptibilityParams;
        private List<Parameter> _infectivityParams;
        private double[] _susceptibilityValues;
        private double[] _infectivityValues;
        private bool _areSusceptibilitiesTimeDep = false;
        private bool _areInfectivitiesTimDep = false;

        private bool _isEpiDependentEventActive = false;
        private bool _emptyToEradicate;

        private List<Event> _events = new List<Event>();
        private List<Event> _activeEvents = new List<Event>();
        private int[] _currentInterventionCombination;

        public Class_Normal(int ID, string name)
            : base(ID, name){}

        public override bool EmptyToEradicate => _emptyToEradicate;
        
        // transmission 
        public override int RowIndexInContactMatrix => _rowIndexInContactMatrix;
        public override bool IsEpiDependentEventActive => _isEpiDependentEventActive;

        // add an event
        public void AddAnEvent(Event e)
        {
            _events.Add(e);
        }
        // setup the initial number parID
        public void SetupInitialAndStoppingConditions(
            Parameter initialMembersPar, 
            bool ifShouldBeEmptyForEradication)
        {
            _initialMembersPar = initialMembersPar;
            _emptyToEradicate = ifShouldBeEmptyForEradication;
        }
        // set up transmission dynamics properties
        public void SetupTransmissionDynamicsProperties(
            List<Parameter> susceptibilityParams, 
            List<Parameter> infectivityParams, 
            int rowIndexInContactMatrix)
        {
            _susceptibilityParams = susceptibilityParams;
            _infectivityParams = infectivityParams;
            
            // if susceptility parameters should be updated by time
            _areSusceptibilitiesTimeDep = (_susceptibilityParams.Where(s => s.ShouldBeUpdatedByTime).Count() > 0);
            // if infectivity parameters should be updated by time
            _areInfectivitiesTimDep = (_infectivityParams.Where(s => s.ShouldBeUpdatedByTime).Count() > 0);

            _rowIndexInContactMatrix = rowIndexInContactMatrix;
        }
        // update the initial number of members
        public override void UpdateInitialNumOfMembers()
        {
            InitialMembers = (int)_initialMembersPar.Value;
            ClassStat.Prevalence= InitialMembers;
        }

        // get susceptibility  values
        public override double[] GetSusceptibilityValues()
        {
            if (_susceptibilityValues is null || _areSusceptibilitiesTimeDep)
            {
                _susceptibilityValues = new double[_susceptibilityParams.Count];
                for (int i = 0; i < _susceptibilityParams.Count; i++)
                    _susceptibilityValues[i] = Math.Max(0, _susceptibilityParams[i].Value);
            }           

            return _susceptibilityValues;
        }

        // update infectivity values
        public override double[] GetInfectivityValues()
        {
            if (_infectivityValues is null || _areInfectivitiesTimDep)
            {
                _infectivityValues = new double[_infectivityParams.Count];
                for (int i = 0; i < _infectivityParams.Count; i++)
                    _infectivityValues[i] = Math.Max(0, _infectivityParams[i].Value);
            }

            return _infectivityValues;
        }
        // update transmission rates affecting this class
        public override void UpdateTransmissionRates(double[] transmissionRatesByPathogen)
        {
            // update the transmission rates
            foreach (Event thisEvent in _events.Where(e => e is Event_EpidemicDependent))
                thisEvent.UpdateRate(transmissionRatesByPathogen[thisEvent.IDOfPathogenToGenerate]);
        }

        // add active events
        public override void AddActiveEvents(int[] interventionCombination)
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
                    if (e is Event_EpidemicDependent)
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
        public override void ResetNumOfMembersOutOfEvents()
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
        private Parameter _parOfProbSucess;
        
        // Properties
        public Class_Splitting(int ID, string name)
            : base(ID, name) {}

        // Properties
        public int ParIDOfProbOfSuccess { get; private set; }

        // add the parameter ID for the probability of success
        public void SetUp(
            Parameter parOfProbSucess, 
            int destinationClassIDIfSuccess, 
            int destinationClassIDIfFailure)
        {
            _parOfProbSucess = parOfProbSucess;
            
            // store the id of the destination classes
            _numOfMembersToDestClasses = new int[2];
            _destinationClasseIDs = new int[2];
            _destinationClasseIDs[0] = destinationClassIDIfSuccess;
            _destinationClasseIDs[1] = destinationClassIDIfFailure;
        }        
        
        // send members of this class out
        public override void SendOutMembers(double deltaT, RNG rng)
        {
            // if number of members is zero, no member is departing
            if (ClassStat.Prevalence <= 0) return;

            // find the number of members sending to each class
            if (_parOfProbSucess.Value == 0)
            {
                _numOfMembersToDestClasses[0] += 0;
                _numOfMembersToDestClasses[1] += ClassStat.Prevalence;
            }
            else if (_parOfProbSucess.Value == 1)
            {
                _numOfMembersToDestClasses[0] += ClassStat.Prevalence;
                _numOfMembersToDestClasses[1] += 0;
            }
            else
            {
                // define a binomial distribution for the number of successes
                int sampledNumOfSuccesses = new Bionomial("temp", ClassStat.Prevalence, _parOfProbSucess.Value).SampleDiscrete(rng);

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
            : base(ID, name){ }

        // Properties        
        public override int[] ResourcesConsumed => _arrAvailableResources;
       
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