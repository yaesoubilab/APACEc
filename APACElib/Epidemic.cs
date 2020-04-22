using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RandomVariateLib;
using ComputationLib;

namespace APACElib
{
    public class Epidemic
    {
        ModelSettings _modelSets;
        public int ID { get; set; }
        public EnumModelUse ModelUse { get; set; } = EnumModelUse.Simulation;
        public bool StoreEpiTrajsForExcelOutput { get; set; } = true;
        
        // public model entities
        public DecisionMaker DecisionMaker { get => _decisionMaker; }
        public ParameterManager ParamManager { get => _paramManager; }  
        public ForceOfInfectionModel FOIModel { get => _FOIModel; }
        public List<Class> Classes { get => _classes; }
        private List<Class> _classes = new List<Class>();

        private List<Event> _events = new List<Event>();
        public EpidemicHistory EpiHist { get; private set; }
        public EpidemicCostHealth EpidemicCostHealth { get; set; }
        public double LnL { get; set; } // likelihood
        public Timer Timer { get; private set; } = new Timer();

        public int InitialSeed { get; set; }
        RNG _rng;        
        RNG _rngNoise;
        int _newSeed;
        public List<int> TestedSeeds { get; set; } = new List<int>();
        private DecisionMaker _decisionMaker;
        private ParameterManager _paramManager;
        private MonitorOfInterventionsInEffect _monitorOfIntrvsInEffect;
        private int[] _pathogenIDs;
        private ForceOfInfectionModel _FOIModel;

        private int _numOfClasses;        
        private int _simTimeIndex;  // simulation time index
        private int _epiTimeIndex;  // time indeces since the detection of epidemic
        
        private bool _thereAreClassesWithEradicationCondition = false;
        public bool StoppedDueToEradication { get; private set; }
        public int SeedProducedAcceptibleTraj { get; private set; }
        public int SeedsDiscarded { get; private set; }

        public Epidemic(int id)
        {
            ID = id;
        }

        public void CleanMemory()
        {
            
            _decisionMaker = null;
            _paramManager.ClearMemory();
            _paramManager = null;

            _FOIModel = null;
            _classes.Clear();
            _classes = null;
            _events.Clear();
            _events = null;
            EpiHist.Clean();
            EpiHist = null;
            EpidemicCostHealth = null;
        }
        
        // Simulate one trajectory (parameters will be sampled)
        public void SimulateUntilOneAcceptibleTrajFound(int timeIndexToStop, int maxTries = 100000)
        {
            int tries = 0;
            SeedProducedAcceptibleTraj = -1;
            SeedsDiscarded = 0;
            bool acceptableTrajFound = false;

            // identify the first seed to try
            int seed = 0;
            RNG seedGenerator = new RNG(InitialSeed);
            seed = InitialSeed;           
            
            Timer.Start();       // reset the timer 
            while (!acceptableTrajFound && tries <= maxTries)// && seed <= stopSeed)
            {
                // record the tested seeds
                TestedSeeds.Add(seed);

                // reset for another simulation
                ResetForAnotherSimulation(seed);
                // simulate
                if (Simulate(timeIndexToStop))
                {
                    acceptableTrajFound = true;
                    SeedProducedAcceptibleTraj = seed;
                }
                else
                {
                    seed = seedGenerator.Next(); // next seed
                    ++tries;
                    ++SeedsDiscarded;
                }
            }
            Timer.Stop(); // stop timer
        }
        // Simulate one trajectory (parameters will be sampled)
        public void SimulateOneTrajectory(int seed, int timeIndexToStop)
        {
            Timer.Start();       // reset the timer     
            SeedProducedAcceptibleTraj = -1;    // reset the seed
            
            // reset for another simulation
            ResetForAnotherSimulation(seed);
            // simulate
            if (Simulate(timeIndexToStop))
                // if this is an acceptable trajectory
                SeedProducedAcceptibleTraj = seed;

            Timer.Stop(); // stop timer
        }

        // return accumulated net benfit (health or monetary) 
        public double NetBenefit(EnumObjectiveFunction enumObjFunc, double wtp)
        {
            double reward = 0;
            switch (enumObjFunc)
            {
                case EnumObjectiveFunction.MaximizeNMB:
                    reward = EpidemicCostHealth.GetDiscountedNMB(wtp);
                    break;
                case EnumObjectiveFunction.MaximizeNHB:
                    reward = EpidemicCostHealth.GetDiscountedNHB(wtp);
                    break;
            }
            return reward;
        }

        // simulate the trajectory assuming that parameter values are already assigned
        private bool Simulate(int timeIndexToStop)
        {
            bool toStop = false;
            bool acceptableTrajectory = false;
            bool ifFeasibleRangesViolated = true;        

            // simulate the epidemic
            while (!toStop)
            {
                // update epidemic history
                ifFeasibleRangesViolated = EpiHist.Update(_simTimeIndex, _epiTimeIndex, false, _rngNoise);
                UpdateEpiTimeIndex();

                // update features
                foreach (Feature f in EpiHist.Features)
                    f.Update(_epiTimeIndex, _modelSets.DeltaT);

                // update cost and health outcomes
                UpdateCostAndHealthOutcomes(false);

                // check if this is has been a feasible trajectory for calibration
                if (ModelUse == EnumModelUse.Calibration && ifFeasibleRangesViolated)
                {
                    acceptableTrajectory = false;
                    return acceptableTrajectory;
                }

                // if warm-up period has ended
                if (_epiTimeIndex == _modelSets.EpiTimeIndexToChangeSeed)
                    _rng = new RNG(_newSeed);

                // make decisions
                if (_simTimeIndex == 0)
                {
                    _monitorOfIntrvsInEffect.MakeADecision(0, _rng, true, ref _classes);
                    // calculate contact matrices
                    FOIModel.CalculateContactMatrices();
                }
                else
                    // make decisions if decision is not predetermined and announce the new decisions (may not necessarily go into effect)
                    _monitorOfIntrvsInEffect.MakeADecision(_epiTimeIndex, _rng, false, ref _classes);

                // update the effect of chance in time dependent parameter value
                _paramManager.UpdateTimeDepParams(_rng, _simTimeIndex * _modelSets.DeltaT, _classes);

                // update transmission rates
                FOIModel.UpdateTransmissionRates(_simTimeIndex, _monitorOfIntrvsInEffect.InterventionsInEffect, ref _classes);

                // Update recorded trajectories to report to the Excel file
                if (StoreEpiTrajsForExcelOutput)
                    EpiHist.Record(_simTimeIndex, _epiTimeIndex, false);                

                // send transfer class members                    
                TransferClassMembers();

                // check if eradicated
                CheckIfEradicated();

                // advance time  
                _simTimeIndex += 1;                

                // check if stopping rules are satisfied 
                if (_epiTimeIndex >= timeIndexToStop || StoppedDueToEradication == true)
                {
                    toStop = true;
                    
                    // update history
                    ifFeasibleRangesViolated =  EpiHist.Update(_simTimeIndex, _epiTimeIndex, true, _rng);
                    // find if required minimum thresholds are hit
                    if (_modelSets.ModelUse == EnumModelUse.Calibration && EpiHist.FindIfMinThresholdsHit() == false)
                        ifFeasibleRangesViolated = true;

                    // update cost and health outcomes
                    UpdateCostAndHealthOutcomes(true);

                    if (StoreEpiTrajsForExcelOutput)
                        EpiHist.Record(_simTimeIndex, _epiTimeIndex, true);

                    // find if it is an acceptable trajectory
                    acceptableTrajectory = true;
                    if (ifFeasibleRangesViolated)
                        acceptableTrajectory = false;
                    else if (_epiTimeIndex < _modelSets.EpidemicConditionTimeIndex)
                        acceptableTrajectory = false;
                }
            } 
            return acceptableTrajectory;
        }
        
        // transfer class members        
        private void TransferClassMembers()
        {
            // reset number of new members over past period for all classes
            foreach (Class thisClass in Classes)
                thisClass.ClassStat.NumOfNewMembersOverPastPeriod = 0;

            // reset number of members out of active events for all classes
            foreach (Class thisClass in Classes)
                thisClass.ResetNumOfMembersOutOfEvents();

            // do the transfer on all members
            foreach (Class thisClass in Classes.Where(c => c.ClassStat.Prevalence>0))
                thisClass.ShouldBeProcessed = true;

            bool thereAreClassesToBeProcessed= true;
            while (thereAreClassesToBeProcessed)
            {
                // if members are waiting
                foreach (Class thisClass in Classes.Where(c => c.ShouldBeProcessed))
                {                    
                    // calculate the number of members to be sent out from each class
                    thisClass.SendOutMembers(_modelSets.DeltaT, _rng);
                    // all departing members are processed
                    thisClass.ShouldBeProcessed = false;
                }

                // receive members
                foreach (Class thisSendingOutClass in Classes.Where(c => c.MembersWaitingToDepart))
                {
                    for (int j = 0; j < thisSendingOutClass.DestinationClasseIDs.Length; j++)
                        Classes[thisSendingOutClass.DestinationClasseIDs[j]].AddNewMembers(thisSendingOutClass.NumOfMembersToDestClasses[j]);
                    // reset number of members sending out to each destination class
                    thisSendingOutClass.ResetNumOfMembersToDestClasses();
                }

                // check if there are members waiting to be sent out
                thereAreClassesToBeProcessed = false;
                for (int i = _numOfClasses - 1; i >= 0; i--)
                    if (Classes[i].ShouldBeProcessed)
                    {
                        thereAreClassesToBeProcessed = true;
                        break;
                    }
            } // end of while (membersWaitingToBeTransferred)
        }

        // update cost and health outcomes
        private void UpdateCostAndHealthOutcomes(bool ifEndOfSim)
        {
            // update costs and health outcomes
            if (ModelUse != EnumModelUse.Calibration)
            {
                // collect cost and health outcomes of classes
                foreach (Class thisClass in Classes.Where(s => !(s.ClassStat.DeltaCostHealthCollector is null)))
                {
                    EpidemicCostHealth.Add(
                        _simTimeIndex,
                        thisClass.ClassStat.DeltaCostHealthCollector.DeltaTCost,
                        thisClass.ClassStat.DeltaCostHealthCollector.DeltaTDALY);
                }
                // collect cost and health outcomes of summation statistics 
                foreach (SumClassesTrajectory thisSumTraj in EpiHist.SumTrajs.Where(s => !(s.DeltaCostHealthCollector is null)))
                    EpidemicCostHealth.Add(
                        _simTimeIndex,
                        thisSumTraj.DeltaCostHealthCollector.DeltaTCost,
                        thisSumTraj.DeltaCostHealthCollector.DeltaTDALY);
                // collect cost and health outcomes of decisions
                EpidemicCostHealth.Add(_simTimeIndex, DecisionMaker.CostOverThisDecisionPeriod, 0);
                DecisionMaker.CostOverThisDecisionPeriod = 0;

                // if simulation has ended, calcualte the discounted outcomes from the current deltaT
                if (ifEndOfSim)
                    EpidemicCostHealth.UpdateDiscountedOutcomes(_simTimeIndex);
            }
        }
        
        // check if stopping condition is satisfied
        private bool CheckIfEradicated()
        {
            // check if any class has eradication condition
            bool eradicated = true;
            if (_thereAreClassesWithEradicationCondition)
                foreach (Class thisClass in Classes)
                {
                    // if a class should be empty while it is not then return false
                    if (thisClass.EmptyToEradicate == true && thisClass.ClassStat.Prevalence > 0)
                    {
                        eradicated = false;
                        break;
                    }
                }            
            else
            {
                eradicated = false;
            }
            StoppedDueToEradication = eradicated;
            return eradicated;
        }
        
        // reset for another simulation
        private void ResetForAnotherSimulation(int seed)
        {
            // reset the rnd object
            _rng = new RNG(seed);
            _rngNoise = new RNG(_rng.Next());
            int[] seeds = (new RNG(_rng.Next())).NextInt32s(_modelSets.ScenarioSeed+1);
            _newSeed = seeds.Last();

            // reset time
            _simTimeIndex = 0;
            UpdateEpiTimeIndex();

            // resample parameters 
            _paramManager.SampleAllParameters(_rng, 0);
            
            // reset force of infection manager 
            FOIModel.Reset();

            // update intervention information 
            DecisionMaker.UpdateParameters(_paramManager, _modelSets.DeltaT);
            DecisionMaker.Reset();
            _monitorOfIntrvsInEffect.Reset();

            // reset the number of people in each compartment
            foreach (Class thisClass in Classes)
                thisClass.UpdateInitialNumOfMembers();

            // health and cost outcomes            
            EpidemicCostHealth.Reset();

            // update rates associated with each class and their initial size
            foreach (Class thisClass in Classes)
                thisClass.Reset();

            // reset epidemic history 
            EpiHist.Reset();       
        }         

        // update current epidemic time
        private void UpdateEpiTimeIndex()
        {
            int baseIndex;
            switch (_modelSets.MarkOfEpidemicStartTime)
            {
                case EnumMarkOfEpidemicStartTime.TimeZero:
                    {
                        _epiTimeIndex = _simTimeIndex;
                    }
                    break;

                case EnumMarkOfEpidemicStartTime.TimeOfFirstObservation:
                    {
                        if (EpiHist.IfSpreadDetected)
                        {
                            baseIndex = (EpiHist.SimTimeIndexOfSpreadDetection / _modelSets.NumOfDeltaT_inObservationPeriod) 
                                * _modelSets.NumOfDeltaT_inObservationPeriod;
                            _epiTimeIndex = _simTimeIndex - baseIndex;

                            // //_epiTimeIndex = _simTimeIndex - EpiHist.SimTimeIndexOfFirstObs
                            // //    + _modelSets.NumOfDeltaT_inObservationPeriod;
                            // _epiTimeIndex = _simTimeIndex - EpiHist.SimTimeIndexOfSpreadDetection;
                        }
                        else
                            _epiTimeIndex = int.MinValue;
                    }
                    break;
            }            
        }

        // subs to create model
        #region functions to create model  
        // create the model
        public void BuildModel(ModelSettings modelSettings, ModelInstruction modelInstr, bool extractOutputHeaders = false)
        {
            // model settings
            _modelSets = modelSettings;

            bool defaultIfSpreadDetected = true;
            if (_modelSets.MarkOfEpidemicStartTime == EnumMarkOfEpidemicStartTime.TimeOfFirstObservation)
                defaultIfSpreadDetected = false;

            // epidemic history
            EpiHist = new EpidemicHistory(_classes, _events, defaultIfSpreadDetected, _modelSets.DeltaT);
            // decision maker
            _decisionMaker = new DecisionMaker(
                _modelSets.EpidemicTimeIndexToStartDecisionMaking,
                _modelSets.NumOfDeltaT_inDecisionInterval, 
                EpiHist.Conditions);

            _paramManager = new ParameterManager();

            // add pathogens
            AddPathogens();

            // force of infection model
            _FOIModel = new ForceOfInfectionModel(
                _pathogenIDs.Length,
                ref _paramManager);

            // add contact matrices
            _FOIModel.AddContactInfo(
                _modelSets.GetBaseContactMatrices(),
                _modelSets.GetPercentChangeInContactMatricesParIDs()
                );

            // build the model accoding to the provided instruction 
            modelInstr.AssignElements(
                pathogenIDs: ref _pathogenIDs,
                modelSets: modelSettings,
                paramManager: _paramManager,
                classes: _classes,
                events: _events,
                epiHist: EpiHist,
                FOIModel: _FOIModel,
                decisionMaker: _decisionMaker);
            modelInstr.BuildModel();
            
            _numOfClasses = _classes.Count;            
            
            // add resources
            AddResources(modelSettings.Sheets.ResourcesSheet);            
            
            EpiHist.SetupSimOutputTrajs(
                ID,
                _modelSets.DeltaT,
                _modelSets.NumOfDeltaT_inSimOutputInterval,
                _modelSets.NumOfDeltaT_inObservationPeriod,
                ref _decisionMaker,
                ref _classes,
                extractOutputHeaders);
                        
            // monitor of interventions in effect
            _monitorOfIntrvsInEffect = new MonitorOfInterventionsInEffect(ref _decisionMaker);
            // economic cost and health
            EpidemicCostHealth = new EpidemicCostHealth(_modelSets.DeltaTDiscountRate, _modelSets.WarmUpPeriodSimTIndex);
            // find if there are classes with eradiation condition
            _thereAreClassesWithEradicationCondition = Classes.Where(s => s.EmptyToEradicate).Count() > 0;

            // prespecified decisions
            if (_modelSets.DecisionRule == EnumEpiDecisions.PredeterminedSequence)
                _decisionMaker.AddPrespecifiedDecisionsOverDecisionsPeriods(_modelSets.PrespecifiedSequenceOfInterventions);
        }

        // add pathogens
        private void AddPathogens()
        {
            _pathogenIDs = new int[_modelSets.Sheets.PathogenSheet.GetLength(0)];
            for (int rowIndex = 1; rowIndex <= _modelSets.Sheets.PathogenSheet.GetLength(0); ++rowIndex)
            {
                _pathogenIDs[rowIndex - 1] = Convert.ToInt32(_modelSets.Sheets.PathogenSheet.GetValue(rowIndex, 1));
            }
        }
        // add resources
        private void AddResources(Array resourcesSheet)
        {
            if (resourcesSheet == null) return;

            for (int rowIndex = 1; rowIndex <= resourcesSheet.GetLength(0); ++rowIndex)
            {
                // read resource information
                int ID = Convert.ToInt32(resourcesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceColumns.ID));
                string name = Convert.ToString(resourcesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceColumns.Name));
                double pricePerUnit = Convert.ToDouble(resourcesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceColumns.PricePerUnit));
                string replenishmentType = Convert.ToString(resourcesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceColumns.ReplenishmentType));
                int parID_firstTimeAvailability = Convert.ToInt32(resourcesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceColumns.FirstTimeAvailable_parID));
                int parID_replenishmentQuantity = Convert.ToInt32(resourcesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceColumns.ReplenishmentQuantity_parID));

                // create the resource
                Resource thisResource = new Resource(ID, name, pricePerUnit);

                // setup the availability time and quantity
                switch (replenishmentType)
                {
                    case "One-Time":
                            thisResource.SetupAvailability(parID_firstTimeAvailability, parID_replenishmentQuantity);
                        break;
                    case "Periodic":
                        {
                            int parID_replenishmentInterval = Convert.ToInt32(resourcesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceColumns.ReplenishmentInterval_parID));
                            thisResource.SetupAvailability(parID_firstTimeAvailability, parID_replenishmentQuantity, parID_replenishmentInterval);
                        }
                        break;
                }

                // if its availability should be reported
                thisResource.ShowAvailability = SupportFunctions.ConvertYesNoToBool(Convert.ToString(resourcesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceColumns.ShowAvailableUnits)));
            } 
        }
       
        // add resource rules
        private void AddResourceRules(Array resourceRulesSheet)
        {
            if (resourceRulesSheet == null) return;

            // return if there is no resource
            //if (Resources.Count == 0) return;

            for (int rowIndex = 1; rowIndex <= resourceRulesSheet.GetLength(0); ++rowIndex)
            {
                // read resource rule information
                int ID = Convert.ToInt32(resourceRulesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceRuleColumns.ID));
                string name = Convert.ToString(resourceRulesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceRuleColumns.Name));
                int associatedResourceID = Convert.ToInt32(resourceRulesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceRuleColumns.ResourceID));
                string resourceRuleType = Convert.ToString(resourceRulesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceRuleColumns.ResourceRuleType));
                int consumptionPerArrival = Convert.ToInt32(resourceRulesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceRuleColumns.UnitsConsumedPerArrival));
                int consumptionPerUnitOfTime = Convert.ToInt32(resourceRulesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceRuleColumns.UnitsConsumedPerUnitOfTime));

                // create the resource
                ResourceRule thisResourceRule = new ResourceRule(ID, name, associatedResourceID, consumptionPerArrival, consumptionPerUnitOfTime);

                // setup the unavailability rule
                switch (resourceRuleType)
                {
                    case "Rule: Send to Another Class":
                        {
                            int classIDIfSatisfied = Convert.ToInt32(resourceRulesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceRuleColumns.ClassIDIfSatisfied));
                            int classIDIfNotSatisfied = Convert.ToInt32(resourceRulesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceRuleColumns.ClassIDIfNotSatisfied));
                            thisResourceRule.SetupUnavailabilityRuleSendToAnotherClass(classIDIfSatisfied, classIDIfNotSatisfied);
                        }
                        break;
                    case "Rule: Send to Another Event":
                        {
                            int processIDIfSatisfied = Convert.ToInt32(resourceRulesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceRuleColumns.EventIDIfSatisfied));
                            int processIDIfNotSatisfied = Convert.ToInt32(resourceRulesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceRuleColumns.EventIDIfNotSatisfied));
                            thisResourceRule.SetupUnavailabilityRuleSendToAnotherClass(processIDIfSatisfied, processIDIfNotSatisfied);
                        }
                        break;
                }

                // add the resource
                //_resourceRules.Add(thisResourceRule);
            }
        }
        
        
        
        #endregion

    }
}
