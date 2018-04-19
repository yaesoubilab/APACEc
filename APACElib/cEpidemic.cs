using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RandomVariateLib;
using SimulationLib;
using ComputationLib;

namespace APACElib
{
    public class Epidemic
    {
        ModelSettings _modelSets;
        public int ID { get; set; }
        public EnumModelUse ModelUse { get; set; } = EnumModelUse.Simulation;
        public bool StoreEpidemicTrajectories { get; set; } = true;
        
        // public model entities
        public DecisionMaker DecisionMaker { get => _decisionMaker; }
        public ParameterManager ParamManager { get => _paramManager; } 
        public ForceOfInfectionModel FOIModel { get; set; }
        public List<Class> Classes { get => _classes; }
        private List<Class> _classes = new List<Class>();
        private List<Event> _events = new List<Event>();    
        public EpidemicHistory EpiHist { get; private set; }
        public EpidemicCostHealth EpidemicCostHealth { get; set; }

        public Timer Timer { get; private set; } = new Timer();
        public Calibration Calibration { get; private set; } = new Calibration();
        
        RNG _rng;
        private DecisionMaker _decisionMaker;
        private ParameterManager _paramManager;
        private MonitorOfInterventionsInEffect _monitorOfIntrvsInEffect;
        private int[] _pathogenIDs;
        private int _numOfClasses;        
        private int _simTimeIndex;  // simulation time index
        private int _epiTimeIndex;  // time indeces since the detection of epidemic
        private bool _firstObservationObtained;
        public int SimTimeIndexOfFirstObs { get; private set; }
        private bool _thereAreClassesWithEradicationCondition = false;
        public bool StoppedDueToEradication { get; private set; }
        public int SeedProducedAcceptibleTraj { get; private set; }

        // Instantiation
        public Epidemic(int id)
        {
            ID = id;
        }

        // clean except the results
        public void CleanExceptResults()
        {
            _modelSets = null;
            _paramManager = new ParameterManager();
            _classes = new List<Class>();
            _events = new List<Event>();
            EpiHist.Clean();
            FOIModel.Clean();            
    }
        
        // Simulate one trajectory (parameters will be sampled)
        public int SimulateTrajectoriesUntilOneAcceptibleFound(int beginSeed, int stopSeed, int imRepIndex, int timeIndexToStop)
        {
            bool acceptableTrajFound = false;
            int nTrajDiscarded = 0;

            while (!acceptableTrajFound && beginSeed <= stopSeed)
            {
                // reset for another simulation
                ResetForAnotherSimulation(beginSeed);
                // simulate
                if (Simulate(imRepIndex, timeIndexToStop))
                {
                    acceptableTrajFound = true;
                    SeedProducedAcceptibleTraj = beginSeed;
                }
                else
                {
                    ++beginSeed;
                    // if the model is used for calibration, record the number of discarded trajectories due to violating the feasible ranges
                    if (_modelSets.ModelUse == EnumModelUse.Calibration)
                        ++nTrajDiscarded;
                }
            }
            return nTrajDiscarded;
        }
        // Simulate one trajectory (parameters will be sampled)
        public void SimulateOneTrajectory(int seed, int simRepIndex, int timeIndexToStop)
        {
            Timer.Start();       // reset the timer     
            SeedProducedAcceptibleTraj = -1;    // reset the seed
            
            // reset for another simulation
            ResetForAnotherSimulation(seed);
            // simulate
            if (Simulate(simRepIndex, timeIndexToStop))
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
        private bool Simulate(int simReplication, int timeIndexToStop)
        {
            bool toStop = false;
            bool acceptableTrajectory = false;
            bool ifThisIsAFeasibleCalibrationTrajectory = true;        

            // simulate the epidemic
            while (!toStop)
            {
                // make decisions if decision is not predetermined and announce the new decisions (may not necessarily go into effect)
                _monitorOfIntrvsInEffect.Update(_epiTimeIndex, false, ref _classes);

                // update the effect of chance in time dependent parameter value
                _paramManager.UpdateTimeDepParams(ref _rng, _simTimeIndex * _modelSets.DeltaT, ref _classes);

                // update recorded trajectories 
                EpiHist.TrajsForSimOutput.Record(_simTimeIndex, false);

                // check if this is has been a feasible trajectory for calibration
                if (ModelUse == EnumModelUse.Calibration && !ifThisIsAFeasibleCalibrationTrajectory)
                {
                    acceptableTrajectory = false;
                    return acceptableTrajectory;
                }

                // update transmission rates
                FOIModel.UpdateTransmissionRates(_simTimeIndex, _monitorOfIntrvsInEffect.InterventionsInEffect, ref _classes);

                // send transfer class members                    
                TransferClassMembers();

                // check if eradicated
                CheckIfEradicated();

                // advance time  
                _simTimeIndex += 1;
                UpdateEpiTimeIndex();

                // check if stopping rules are satisfied 
                if (_epiTimeIndex >= timeIndexToStop || StoppedDueToEradication == true)
                {
                    toStop = true;
                    // update recorded trajectories 
                    EpiHist.TrajsForSimOutput.Record(_simTimeIndex, true);

                    // find if it is an acceptable trajectory
                    acceptableTrajectory = true;
                    if (_epiTimeIndex < _modelSets.EpidemicConditionTimeIndex)
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

            // update class statistics                      
            foreach (Class thisClass in Classes)
            {
                thisClass.ClassStat.CollectEndOfDeltaTStats(_simTimeIndex);
                if (ModelUse != EnumModelUse.Calibration)
                {
                    EpidemicCostHealth.Add(
                        _simTimeIndex,
                        thisClass.ClassStat.DeltaCostHealthCollector.DeltaTCost,
                        thisClass.ClassStat.DeltaCostHealthCollector.DeltaTDALY);
                }
            }

            // update summation and ratio trajectories
            EpiHist.UpdateSumAndRatioTrajs(_simTimeIndex, ref _classes, ref _events);

            // reset number of members out of active events for all classes
            foreach (Class thisClass in Classes)
                thisClass.ResetNumOfMembersOutOfEvents();

            // update decision costs
            if (ModelUse != EnumModelUse.Calibration)
            {
                EpidemicCostHealth.Add(_simTimeIndex, DecisionMaker.CostOverThisDecisionPeriod, 0);
                DecisionMaker.CostOverThisDecisionPeriod = 0;
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

            // reset time
            _simTimeIndex = 0;
            SimTimeIndexOfFirstObs = 0;
            _firstObservationObtained = false;
            UpdateEpiTimeIndex();

            // resample parameters 
            _paramManager.SampleAllParameters(ref _rng, 0);

            // update contact matrices
            FOIModel.AddContactInfo(
                _modelSets.GetBaseContactMatrices(),
                _modelSets.GetPercentChangeInContactMatricesParIDs()
                );
            // reset force of infection manager 
            FOIModel.Reset();

            // update intervention information 
            DecisionMaker.UpdateParameters(_paramManager, _modelSets.DeltaT);

            // reset the number of people in each compartment
            foreach (Class thisClass in Classes)
                thisClass.UpdateInitialNumOfMembers((int)Math.Round(_paramManager.ParameterValues[thisClass.InitialMemebersParID]));

            // health and cost outcomes
            EpidemicCostHealth = new EpidemicCostHealth(_modelSets.DeltaTDiscountRate, _modelSets.WarmUpPeriodTimeIndex);
            EpidemicCostHealth.Reset();

            // reset decisions
            DecisionMaker.Reset();

            // update decisions
            _monitorOfIntrvsInEffect.Update(0, true, ref _classes);

            // calculate contact matrices
            FOIModel.CalculateContactMatrices();

            // update rates associated with each class and their initial size
            foreach (Class thisClass in Classes)
            {
                thisClass.UpdateRatesOfBirthAndEpiIndpEvents(_paramManager.ParameterValues);
                thisClass.UpdateProbOfSuccess(_paramManager.ParameterValues);
                thisClass.Reset();
            }

            // reset epidemic history 
            EpiHist.Reset(_simTimeIndex, ref _classes, ref _events);           
        }         

        // update current epidemic time
        private void UpdateEpiTimeIndex()
        {
            switch (_modelSets.MarkOfEpidemicStartTime)
            {
                case EnumMarkOfEpidemicStartTime.TimeZero:
                    {
                        _epiTimeIndex = _simTimeIndex;
                    }
                    break;

                case EnumMarkOfEpidemicStartTime.TimeOfFirstObservation:
                    {
                        if (_firstObservationObtained)
                            _epiTimeIndex = _simTimeIndex - SimTimeIndexOfFirstObs + _modelSets.NumOfDeltaT_inObservationPeriod;
                        else
                            _epiTimeIndex = int.MinValue;
                    }
                    break;
            }            
        }

        // subs to create model
        #region subs to create model  
        // create the model
        public void BuildModel(ref ModelSettings modelSettings, bool extractOutputHeaders = false)
        {
            // model settings
            _modelSets = modelSettings;
            // decision maker
            _decisionMaker = new DecisionMaker(
                _modelSets.EpidemicTimeIndexToStartDecisionMaking,
                _modelSets.NumOfDeltaT_inDecisionInterval);
            // add parameters
            AddParameters(modelSettings.ParametersSheet);
            // add pathogens
            AddPathogens(modelSettings.PathogenSheet);
            // add classes
            AddClasses(modelSettings.ClassesSheet);
            // force of infection model
            FOIModel = new ForceOfInfectionModel(
                _pathogenIDs.Length,
                ref _paramManager);
            // add interventions
            AddInterventions(modelSettings.InterventionSheet);
            // add resources
            AddResources(modelSettings.ResourcesSheet);
            // add events
            AddEvents(modelSettings.EventSheet);
            // epidemic history
            EpiHist = new EpidemicHistory();
            // add summation statistics
            AddSummationStatistics(modelSettings.SummationStatisticsSheet);
            // add ratio statistics
            AddRatioStatistics(modelSettings.RatioStatisticsSheet);
            // add trajectories for simulation output
            EpiHist.SetupTrajsForSimOutput(
                ID,
                _modelSets.DeltaT,
                _modelSets.NumOfDeltaT_inSimOutputInterval,
                ref _decisionMaker,
                ref _classes,
                extractOutputHeaders);
            // add connections
            AddConnections(modelSettings.ConnectionsMatrix);
            // monitor of interventions in effect
            _monitorOfIntrvsInEffect = new MonitorOfInterventionsInEffect(ref _decisionMaker);
            
            // find if there are classes with eradiation condition
            _thereAreClassesWithEradicationCondition = Classes.Where(s => s.EmptyToEradicate).Count() > 0;
        }
        // read parameters
        private void AddParameters(Array parametersSheet)
        {
            // parameter manager
            _paramManager = new ParameterManager();

            int lastRowIndex = parametersSheet.GetLength(0);
            for (int rowIndex = 1; rowIndex <= lastRowIndex; ++rowIndex)
            {
                // ID and Name
                int parameterID = Convert.ToInt32(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.ID));
                string name = Convert.ToString(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.Name));
                bool updateAtEachTimeStep = SupportFunctions.ConvertYesNoToBool(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.UpdateAtEachTimeStep).ToString());
                string distribution = Convert.ToString(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.Distribution));
                EnumRandomVariates enumRVG = RandomVariateLib.SupportProcedures.ConvertToEnumRVG(distribution);
                bool includedInCalibration = SupportFunctions.ConvertYesNoToBool(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.IncludedInCalibration).ToString());

                Parameter thisParameter = null;
                double par1 = 0, par2 = 0, par3 = 0, par4 = 0;

                if (enumRVG == EnumRandomVariates.LinearCombination)
                {
                    string strPar1 = Convert.ToString(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.Par1));
                    string strPar2 = Convert.ToString(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.Par2));

                    // remove spaces and parenthesis
                    strPar1 = strPar1.Replace(" ", "");
                    strPar1 = strPar1.Replace("(", "");
                    strPar1 = strPar1.Replace(")", "");
                    strPar2 = strPar2.Replace(" ", "");
                    strPar2 = strPar2.Replace("(", "");
                    strPar2 = strPar2.Replace(")", "");
                    // convert to array
                    string[] strParIDs = strPar1.Split(',');
                    string[] strCoefficients = strPar2.Split(',');
                    // convert to numbers
                    int[] arrParIDs = Array.ConvertAll<string, int>(strParIDs, Convert.ToInt32);
                    double[] arrCoefficients = Array.ConvertAll<string, double>(strCoefficients, Convert.ToDouble);

                    thisParameter = new LinearCombination(parameterID, name, arrParIDs, arrCoefficients);
                }
                else if (enumRVG == EnumRandomVariates.MultipleCombination)
                {
                    string strPar1 = Convert.ToString(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.Par1));

                    // remove spaces and parenthesis
                    strPar1 = strPar1.Replace(" ", "");
                    strPar1 = strPar1.Replace("(", "");
                    strPar1 = strPar1.Replace(")", "");
                    // convert to array
                    string[] strParIDs = strPar1.Split(',');
                    // convert to numbers
                    int[] arrParIDs = Array.ConvertAll<string, int>(strParIDs, Convert.ToInt32);

                    thisParameter = new ProductParameter(parameterID, name, arrParIDs);
                }
                else
                {
                    par1 = Convert.ToDouble(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.Par1));
                    par2 = Convert.ToDouble(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.Par2));
                    par3 = Convert.ToDouble(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.Par3));
                    par4 = Convert.ToDouble(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.Par4));
                }           

                switch (enumRVG)
                {
                    case (EnumRandomVariates.LinearCombination):
                    case (EnumRandomVariates.MultipleCombination):
                        // created above
                        break;
                    case (EnumRandomVariates.Correlated):
                        thisParameter = new CorrelatedParameter(parameterID, name, (int)par1, par2, par3);
                        break;
                    case (EnumRandomVariates.Multiplicative):
                        thisParameter = new MultiplicativeParameter(parameterID, name, (int)par1, (int)par2, (bool)(par3==1));
                        break;
                    case (EnumRandomVariates.TimeDependetLinear):
                        {
                            thisParameter = new TimeDependetLinear(parameterID, name, (int)par1, (int)par2, par3, par4);
                        }
                        break;
                    case (EnumRandomVariates.TimeDependetOscillating):
                        {
                            thisParameter = new TimeDependetOscillating(parameterID, name, (int)par1, (int)par2, (int)par3, (int)par4);
                        }
                        break;
                    default:
                        thisParameter = new IndependetParameter(parameterID, name, enumRVG, par1, par2, par3, par4);
                        break;
                }

                thisParameter.ShouldBeUpdatedByTime = updateAtEachTimeStep;
                thisParameter.IncludedInCalibration = includedInCalibration;
                
                // add the parameter
                _paramManager.Add(thisParameter);
            }
        }
        // add pathogens
        private void AddPathogens(Array pathogenSheet)
        {
            _pathogenIDs = new int[pathogenSheet.GetLength(0)];
            for (int rowIndex = 1; rowIndex <= pathogenSheet.GetLength(0); ++rowIndex)
            {
                _pathogenIDs[rowIndex - 1] = Convert.ToInt32(pathogenSheet.GetValue(rowIndex, 1));
            }
        }
        // add classes
        private void AddClasses(Array classesSheet)
        {
            for (int rowIndex = 1; rowIndex <= classesSheet.GetLength(0); ++rowIndex)
            {
                // ID and Name
                int classID = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ID));
                string name = Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.Name));
                // class type
                string strClassType = Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ClassType));

                // DALY loss and cost outcomes
                double DALYPerNewMember = Convert.ToDouble(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.DALYPerNewMember));
                double costPerNewMember = Convert.ToDouble(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.CostPerNewMember));
                double healthQualityPerUnitOfTime = Convert.ToDouble(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.DisabilityWeightPerUnitOfTime));
                double costPerUnitOfTime = Convert.ToDouble(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.CostPerUnitOfTime));

                // statistics                
                bool collectPrevalenceStats = SupportFunctions.ConvertYesNoToBool(Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.CollectPrevalenceStats)));
                bool collectAccumIncidenceStats = SupportFunctions.ConvertYesNoToBool(Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.CollectAccumIncidenceStats)));

                // simulation output
                bool showIncidence = SupportFunctions.ConvertYesNoToBool(Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ShowIncidence)));
                bool showPrevalence = SupportFunctions.ConvertYesNoToBool(Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ShowPrevalence)));
                bool showAccumIncidence = SupportFunctions.ConvertYesNoToBool(Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ShowAccumIncidence)));

                // build and add the class
                #region Build class
                switch (strClassType)
                {
                    case "Class: Normal":
                        {
                            // initial number parameter ID
                            int initialMembersParID = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.InitialMembers));
                            // empty to eradicate
                            string strEmptyToEradicate = Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.EmptyToEradicate));
                            bool emptyToEradicate = SupportFunctions.ConvertYesNoToBool(strEmptyToEradicate);

                            // susceptibility parameter ID
                            string strSusceptibilityIDs = Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.SusceptibilityParID));
                            // infectivity parameter ID
                            string strInfectivityIDs = Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.InfectivityParID));
                            // row in contact matrix
                            int rowInContactMatrix = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.RowInContactMatrix));

                            // build the class
                            Class_Normal thisNormalClass = new Class_Normal(classID, name);
                            // set up initial member and eradication rules
                            thisNormalClass.SetupInitialAndStoppingConditions(initialMembersParID, emptyToEradicate);
                            // set up transmission dynamics properties                            
                            thisNormalClass.SetupTransmissionDynamicsProperties(strSusceptibilityIDs, strInfectivityIDs, rowInContactMatrix);

                            // add class
                            Classes.Add(thisNormalClass);

                            // check if infectivity and susceptibility parameters are time dependent
                            if (_paramManager.ThereAreTimeDepParms)
                            {
                                for (int i = 0; i < thisNormalClass.InfectivityParIDs.Length; i++)
                                    if (_paramManager.Parameters[thisNormalClass.InfectivityParIDs[i]].ShouldBeUpdatedByTime)
                                    {
                                        _paramManager.ThereAreTimeDepParms_infectivities = true;
                                        _paramManager.ThereAreTimeDepParms_tranmission = true;
                                    }
                                for (int i = 0; i < thisNormalClass.SusceptibilityParIDs.Length; i++)
                                    if (_paramManager.Parameters[thisNormalClass.SusceptibilityParIDs[i]].ShouldBeUpdatedByTime)
                                    {
                                        _paramManager.ThereAreTimeDepParms_susceptibilities = true;
                                        _paramManager.ThereAreTimeDepParms_tranmission = true;
                                    }
                            }
                        }
                        break;
                    case "Class: Death":
                        {
                            // build the class
                            Class_Death thisDealthClass = new Class_Death(classID, name);

                            // add class
                            Classes.Add(thisDealthClass);
                        }
                        break;
                    case "Class: Splitting":
                        {
                            // read settings
                            int parIDForProbOfSuccess = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ParIDForProbOfSuccess));
                            int destinationClassIDGivenSuccess = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.DestinationClassIDIfSuccess));
                            int destinationClassIDGivenFailure = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.DestinationClassIDIfFailure));

                            // build the class
                            Class_Splitting thisSplittingClass = new Class_Splitting(classID, name);
                            thisSplittingClass.SetUp(parIDForProbOfSuccess, destinationClassIDGivenSuccess, destinationClassIDGivenFailure);

                            // add class
                            Classes.Add(thisSplittingClass);

                            // check if the rate parameter is time dependent
                            if (_paramManager.ThereAreTimeDepParms && _paramManager.Parameters[parIDForProbOfSuccess].ShouldBeUpdatedByTime)
                                _paramManager.ThereAreTimeDepParms_splittingClasses = true;
                        }
                        break;
                    case "Class: Resource Monitor":
                        {
                            // read settings
                            int resourceIDToCheckAvailability = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ResourceIDToCheckAvailability));
                            double resourceUnitsConsumedPerArrival = (double)Convert.ToDouble(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ResourceUnitsConsumedPerArrival));
                            int destinationClassIDGivenSuccess = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.DestinationClassIDIfSuccess));
                            int destinationClassIDGivenFailure = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.DestinationClassIDIfFailure));                            

                            // build the class
                            Class_ResourceMonitor thisResourceMonitorClass = new Class_ResourceMonitor(classID, name);                            
                            thisResourceMonitorClass.SetUp(resourceIDToCheckAvailability, resourceUnitsConsumedPerArrival, destinationClassIDGivenSuccess, destinationClassIDGivenFailure);

                            // add class
                            Classes.Add(thisResourceMonitorClass);
                        }
                        break;
                }
                #endregion

                // class statistics 
                Classes.Last().ClassStat = new OneDimTrajectory(classID, name, _modelSets.WarmUpPeriodTimeIndex);
                Classes.Last().ClassStat.SetupStatisticsCollectors(
                    collectAccumIncidenceStats,
                    collectPrevalenceStats
                    );
                // adding time series
                Classes.Last().ClassStat.AddTimeSeries(
                    collectIncidence: showIncidence,
                    collectPrevalence: false,
                    collectAccumIncidence: false,
                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval
                    );
                
                // adding cost and health outcomes
                Classes.Last().ClassStat.AddCostHealthOutcomes(
                    DALYPerNewMember, costPerNewMember, healthQualityPerUnitOfTime * _modelSets.DeltaT, costPerUnitOfTime * _modelSets.DeltaT);

                // set up which statistics to show
                Classes.Last().ShowIncidence = showIncidence;
                Classes.Last().ShowPrevalence = showPrevalence;
                Classes.Last().ShowAccumIncidence = showAccumIncidence;

            }// end of for
            _numOfClasses = Classes.Count;
        }
        // add interventions
        private void AddInterventions(Array interventionsSheet)
        {
            //_useSameContactMatrixForAllDecisions = true;

            for (int rowIndex = 1; rowIndex <= interventionsSheet.GetLength(0); ++rowIndex)
            {
                Intervention thisIntervention;
                double timeBecomeAvailable = 0;
                double timeBecomeUnavailable = 0;
                int resourceID = 0;
                int delayParID = 0;
                bool affectingContactPattern;
                string strDecisionRule;
                EnumDecisionRule enumDecisionRule;
                int switchStatus = 0;

                // read intervention information
                int ID = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.ID));
                // name
                string name = Convert.ToString(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.Name));
                // action type
                string strType = Convert.ToString(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.Type));
                EnumInterventionType type = Intervention.ConvertToActionType(strType);
                // mutually exclusive group
                int mutuallyExclusiveGroup = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.MutuallyExclusiveGroup));

                // availability
                timeBecomeAvailable = Convert.ToDouble(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.TimeBecomesAvailable));
                timeBecomeUnavailable = Convert.ToDouble(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.TimeBecomesUnavailableTo));

                // costs
                double fixedCost = Convert.ToDouble(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.FixedCost));
                double costPerUnitOfTime = Convert.ToDouble(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.CostPerUnitOfTime));
                double penaltyForSwitchingFromOnToOff = Convert.ToDouble(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.PenaltyOfSwitchingFromOnToOff));

                // if type is default
                if (type == EnumInterventionType.Default)
                {
                    affectingContactPattern = true;
                    enumDecisionRule = EnumDecisionRule.Predetermined;
                    switchStatus = 1;
                }
                else // if type is not default
                {
                    affectingContactPattern = SupportFunctions.ConvertYesNoToBool(
                        Convert.ToString(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.AffectingContactPattern)));
                    strDecisionRule = Convert.ToString(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.OnOffSwitchSetting));
                    enumDecisionRule = SupportProcedures.ConvertToDecisionRule(strDecisionRule);
                    
                    delayParID = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.DelayParID));
                    resourceID = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.ResourceID));

                    // if this intervention is affecting contacts
                    if (affectingContactPattern)
                        FOIModel.AddIntrvnAffectingContacts(rowIndex - 1);

                    // switch value for the pre-determined employment
                    switchStatus = SupportProcedures.ConvertToSwitchValue(
                        Convert.ToString(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.PreDeterminedEmployment_SwitchValue)));
                }

                // set up resource requirement
                //if (Convert.ToString(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.ResourceID)) != "")
                //  thisIntervention.SetupResourceRequirement(Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.ResourceID)));

                // define decision rule
                DecisionRule simDecisionRule = null;
                switch (enumDecisionRule)
                {
                    case EnumDecisionRule.Predetermined:
                        {
                            simDecisionRule = new DecionRule_Predetermined(switchStatus);
                        }
                        break;
                    case EnumDecisionRule.Periodic:
                        {
                            int frequency = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.PeriodicEmployment_Periodicity));
                            int duration = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.PeriodicEmployment_Length));
                            //thisIntervention.AddPeriodicEmploymentSetting(frequency, duration);
                        }
                        break;
                    case EnumDecisionRule.ThresholdBased:
                        {
                            //int IDOfSpecialStatistics = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.ThresholdBased_IDOfSpecialStatisticsToObserveAccumulation));
                            //string strObservation = Convert.ToString(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.ThresholdBased_Observation));
                            //APACE_lib.Intervention.EnumEpidemiologicalObservation observation = Intervention.EnumEpidemiologicalObservation.OverPastObservationPeriod;
                            //if (strObservation == "Accumulating")
                            //    observation = Intervention.EnumEpidemiologicalObservation.Accumulating;

                            //double threshold = Convert.ToDouble(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.ThresholdBased_ThresholdToTriggerThisDecision));
                            //int duration = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.ThresholdBased_NumOfDecisionPeriodsToUseThisDecision));
                            //thisIntervention.AddThresholdBasedEmploymentSetting(IDOfSpecialStatistics, observation, threshold, (int)(duration * _numOfDeltaTsInADecisionInterval));
                        }
                        break;
                    case EnumDecisionRule.IntervalBased:
                        {
                            //double availableUntilThisTime = Convert.ToDouble(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.IntervalBasedOptimizationSettings_AvailableUpToTime));
                            //int minNumOfDecisionPeriodsToUse = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.IntervalBasedOptimizationSettings_MinNumOfDecisionPeriodsToUse));
                            //thisIntervention.AddIntervalBaseEmploymentSetting(availableUntilThisTime, minNumOfDecisionPeriodsToUse);
                        }
                        break;                   
                    case EnumDecisionRule.Dynamic:
                        {
                            //bool selectOnOffStatusAsFeature = SupportFunctions.ConvertYesNoToBool(Convert.ToString(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.SelectOnOffStatusAsFeature)));

                            //int previousObservationPeriodToObserveOnOffValue=0;
                            //if (selectOnOffStatusAsFeature)
                            //    previousObservationPeriodToObserveOnOffValue = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.PreviousObservationPeriodToObserveValue));
                            
                            //bool useNumOfDecisionPeriodEmployedAsFeature = SupportFunctions.ConvertYesNoToBool(Convert.ToString(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.UseNumOfDecisionPeriodEmployedAsFeature)));
                            //bool remainOnOnceSwitchedOn = SupportFunctions.ConvertYesNoToBool(Convert.ToString(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.RemainsOnOnceSwitchedOn)));
                            //thisIntervention.AddDynamicPolicySettings(remainOnOnceSwitchedOn); //selectOnOffStatusAsFeature, previousObservationPeriodToObserveOnOffValue, useNumOfDecisionPeriodEmployedAsFeature,

                            //// add features related to this intervention
                            //// on/off status features (note that default interventions can not have on/off status feature)
                            //if (selectOnOffStatusAsFeature && thisIntervention.Type != Intervention.enumActionType.Default)
                            //    _features.Add(new Feature_InterventionOnOffStatus("On/Off status of " + thisIntervention.Name, _numOfFeatures++, thisIntervention.ID, previousObservationPeriodToObserveOnOffValue));
                            //// feature on the number of decision periods over which this intervention is used
                            //if (useNumOfDecisionPeriodEmployedAsFeature)
                            //    _features.Add(new Feature_NumOfDecisoinPeriodsOverWhichThisInterventionWasUsed("Number of decision periods " + thisIntervention.Name + " is used", _numOfFeatures++, thisIntervention.ID));
                        }
                        break;
                }

                // create the intervention
                thisIntervention = new Intervention(ID, name, type, affectingContactPattern,
                    (int)(timeBecomeAvailable / _modelSets.DeltaT), (int)(timeBecomeUnavailable / _modelSets.DeltaT), delayParID, ref simDecisionRule);

                // set up cost
                thisIntervention.SetUpCost(fixedCost, costPerUnitOfTime, penaltyForSwitchingFromOnToOff);

                // add the intervention
                DecisionMaker.AddAnIntervention(thisIntervention);
            }

            // gather info
            DecisionMaker.UpdateAfterAllInterventionsAdded();

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
        // add events
        private void AddEvents(Array eventSheet)
        {
            for (int rowIndex = 1; rowIndex <= eventSheet.GetLength(0); ++rowIndex)
            {
                // general settings
                int ID = Convert.ToInt32(eventSheet.GetValue(rowIndex, (int)ExcelInterface.enumEventColumns.ID));
                string name = Convert.ToString(eventSheet.GetValue(rowIndex, (int)ExcelInterface.enumEventColumns.Name));
                string strEventType = Convert.ToString(eventSheet.GetValue(rowIndex, (int)ExcelInterface.enumEventColumns.EventType));
                int IDOfActivatingIntervention = Convert.ToInt32(eventSheet.GetValue(rowIndex, (int)ExcelInterface.enumEventColumns.IDOfActiviatingIntervention));
                int IDOfDestinationClass = Convert.ToInt32(eventSheet.GetValue(rowIndex, (int)ExcelInterface.enumEventColumns.IDOfDestinationClass));

                // build the event
                #region Build event
                switch (strEventType)
                {
                    case "Event: Birth":
                        {
                            int IDOfRateParameter = Convert.ToInt32(eventSheet.GetValue(rowIndex, (int)ExcelInterface.enumEventColumns.IDOfRateParameter));
                            // create the event
                            Event_Birth thisEvent_Birth = new Event_Birth(name, ID, IDOfActivatingIntervention, IDOfRateParameter, IDOfDestinationClass);
                            _events.Add(thisEvent_Birth);
                        }
                        break;
                    case "Event: Epidemic-Independent":
                        {
                            int IDOfRateParameter = Convert.ToInt32(eventSheet.GetValue(rowIndex, (int)ExcelInterface.enumEventColumns.IDOfRateParameter));
                            // create the process
                            Event_EpidemicIndependent thisEvent_EpidemicIndependent = new Event_EpidemicIndependent(name, ID, IDOfActivatingIntervention, IDOfRateParameter, IDOfDestinationClass);
                            _events.Add(thisEvent_EpidemicIndependent);

                            // check if the rate parameter is time dependent
                            if (_paramManager.ThereAreTimeDepParms && _paramManager.Parameters[IDOfRateParameter].ShouldBeUpdatedByTime)
                                _paramManager.ThereAreTimeDepParms_diseaseProgression = true;
                        }
                        break;
                    case "Event: Epidemic-Dependent":
                        {
                            int IDOfPathogenToGenerate = Convert.ToInt32(eventSheet.GetValue(rowIndex, (int)ExcelInterface.enumEventColumns.IDOfGeneratingPathogen));
                            // create the process
                            Event_EpidemicDependent thisEvent_EpidemicDependent = new Event_EpidemicDependent(name, ID, IDOfActivatingIntervention, IDOfPathogenToGenerate, IDOfDestinationClass);
                            _events.Add(thisEvent_EpidemicDependent);
                        }
                        break;
                }
                #endregion

            } // end of for
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
        // add summation statistics
        private void AddSummationStatistics(Array summationStatisticsSheet)
        {
            if (summationStatisticsSheet == null) return;
            for (int rowIndex = 1; rowIndex <= summationStatisticsSheet.GetLength(0); ++rowIndex)
            {
                // ID and Name
                int statID = Convert.ToInt32(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.ID));
                string name = Convert.ToString(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Name));
                string strDefinedOn = Convert.ToString(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.DefinedOn));
                string strType = Convert.ToString(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Type));
                string sumFormula = Convert.ToString(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Formula));

                // defined on 
                SumTrajectory.EnumDefinedOn definedOn = SumTrajectory.EnumDefinedOn.Classes;
                if (strDefinedOn == "Events") definedOn = SumTrajectory.EnumDefinedOn.Events;

                // type
                SumTrajectory.EnumType type = SumTrajectory.EnumType.Incidence;
                if (strType == "Prevalence") type = SumTrajectory.EnumType.Prevalence;
                else if (strType == "Accumulating Incidence") type = SumTrajectory.EnumType.AccumulatingIncident;

                // if display
                bool ifDispay = SupportFunctions.ConvertYesNoToBool(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.IfDisplay).ToString());

                // DALY and cost outcomes
                double DALYPerNewMember = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.DALYPerNewMember));
                double costPerNewMember = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.CostPerNewMember));
                // real-time monitoring
                bool surveillanceDataAvailable = SupportFunctions.ConvertYesNoToBool(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.SurveillanceDataAvailable).ToString());
                int numOfObservationPeriodsDelayBeforeObservating = Convert.ToInt32(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.NumOfObservationPeriodsDelayBeforeObservating));
                bool firstObservationMarksTheStartOfTheSpread = SupportFunctions.ConvertYesNoToBool(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.FirstObservationMarksTheStartOfTheSpread).ToString());

                // calibration
                bool ifIncludedInCalibration = SupportFunctions.ConvertYesNoToBool(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.IfIncludedInCalibration).ToString());
                
                // goodness of fit measure
                // default values
                CalibrationTarget.enumGoodnessOfFitMeasure goodnessOfFitMeasure = CalibrationTarget.enumGoodnessOfFitMeasure.SumSqurError_timeSeries;
                double[] fourierWeights = new double[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.SIZE];
                bool ifCheckWithinFeasibleRange = false;
                double feasibleMin = 0, feasibleMax = 0;
                
                // check if included in calibration 
                double overalWeight = 0;
                if (ifIncludedInCalibration)
                {
                    // measure of fit
                    string strGoodnessOfFitMeasure = Convert.ToString(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.MeasureOfFit));
                    if (strGoodnessOfFitMeasure == "Fourier") goodnessOfFitMeasure = CalibrationTarget.enumGoodnessOfFitMeasure.Fourier;

                    // overall weight
                    overalWeight = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_OveralFit));
                    // Fourier weights
                    if (goodnessOfFitMeasure == CalibrationTarget.enumGoodnessOfFitMeasure.Fourier)
                    {
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.Cosine]
                        = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierCosine));
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.Norm2]
                            = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierEuclidean));
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.Average]
                            = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierAverage));
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.StDev]
                            = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierStDev));
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.Min]
                            = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierMin));
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.Max]
                            = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierMax));
                    }
                    // if to check if within a feasible range
                    ifCheckWithinFeasibleRange = SupportFunctions.ConvertYesNoToBool(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.IfCheckWithinFeasibleRange).ToString());
                    if (ifCheckWithinFeasibleRange)
                    {
                        feasibleMin = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.FeasibleRange_minimum));
                        feasibleMax = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.FeasibleRange_maximum));
                    }
                }

                // build and add the summation statistics
                if (definedOn == SumTrajectory.EnumDefinedOn.Classes)
                {
                    SumClassesTrajectory thisSumClassTraj = new SumClassesTrajectory
                        (statID, name, type, sumFormula, ifDispay, _modelSets.WarmUpPeriodTimeIndex, _modelSets.NumOfDeltaT_inSimOutputInterval);
                    // add the summation statistics
                    EpiHist.SumTrajs.Add(thisSumClassTraj);
                    
                    // update class time-series
                    foreach (int i in thisSumClassTraj.ClassIDs)
                    {
                        if (thisSumClassTraj.Type == SumTrajectory.EnumType.Incidence)
                            _classes[i].ClassStat.AddTimeSeries(
                                collectIncidence: true,
                                collectAccumIncidence: false,
                                collectPrevalence: false, 
                                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval
                                );
                        if (thisSumClassTraj.Type == SumTrajectory.EnumType.AccumulatingIncident)
                            _classes[i].ClassStat.CollectAccumIncidenceStats = true;
                    }
                }
                else // if defined on events
                {
                    SumEventTrajectory thisSumEventTraj = new SumEventTrajectory
                        (statID, name, type, sumFormula, ifDispay, _modelSets.WarmUpPeriodTimeIndex, _modelSets.NumOfDeltaT_inSimOutputInterval);
                    // add the summation statistics
                    EpiHist.SumTrajs.Add(thisSumEventTraj);
                }

                // adding cost and health outcomes
                EpiHist.SumTrajs.Last().AddCostHealthOutcomes(DALYPerNewMember, costPerNewMember, 0, 0);

                // update calibraton infor
                EpiHist.SumTrajs.Last().CalibInfo = new TrajectoryCalibrationInfo(ifIncludedInCalibration, ifCheckWithinFeasibleRange, feasibleMin, feasibleMax);
                
            }
        }
        // add ratio statistics
        private void AddRatioStatistics(Array ratioStatisticsSheet)
        {
            if (ratioStatisticsSheet == null) return;

            for (int rowIndex = 1; rowIndex <= ratioStatisticsSheet.GetLength(0); ++rowIndex)
            {
                // ID and Name
                int statID = Convert.ToInt32(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.ID));
                string name = Convert.ToString(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Name));
                string strType = Convert.ToString(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Type));
                string ratioFormula = Convert.ToString(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Formula));

                // if display
                bool ifDispay = SupportFunctions.ConvertYesNoToBool(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.IfDisplay).ToString());

                // calibration
                bool ifIncludedInCalibration = SupportFunctions.ConvertYesNoToBool(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.IfIncludedInCalibration).ToString());

                // default values
                CalibrationTarget.enumGoodnessOfFitMeasure goodnessOfFitMeasure = CalibrationTarget.enumGoodnessOfFitMeasure.SumSqurError_timeSeries;
                double[] fourierWeights = new double[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.SIZE];
                bool ifCheckWithinFeasibleRange = false;
                double feasibleMin = 0, feasibleMax = 0;

                // check if included in calibration 
                double overalWeight = 0;
                if (ifIncludedInCalibration)
                {
                    // measure of fit
                    string strGoodnessOfFitMeasure = Convert.ToString(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.MeasureOfFit));
                    switch (strGoodnessOfFitMeasure)
                    {
                        case "Sum Sqr Err (time-series)":
                            goodnessOfFitMeasure = CalibrationTarget.enumGoodnessOfFitMeasure.SumSqurError_timeSeries;
                            break;
                        case "Sum Sqr Err (average time-series)":
                            goodnessOfFitMeasure = CalibrationTarget.enumGoodnessOfFitMeasure.SumSqurError_average;
                            break;
                        case "Fourier":
                            goodnessOfFitMeasure = CalibrationTarget.enumGoodnessOfFitMeasure.Fourier;
                            break;
                    }
                    // overall weight
                    overalWeight = Convert.ToDouble(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_OveralFit));
                    // Fourier weights
                    if (goodnessOfFitMeasure == CalibrationTarget.enumGoodnessOfFitMeasure.Fourier)
                    {
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.Cosine]
                        = Convert.ToDouble(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierCosine));
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.Norm2]
                            = Convert.ToDouble(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierEuclidean));
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.Average]
                            = Convert.ToDouble(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierAverage));
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.StDev]
                            = Convert.ToDouble(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierStDev));
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.Min]
                            = Convert.ToDouble(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierMin));
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.Max]
                            = Convert.ToDouble(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierMax));
                    }
                    // if to check if within a feasible range
                    ifCheckWithinFeasibleRange = SupportFunctions.ConvertYesNoToBool(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.IfCheckWithinFeasibleRange).ToString());
                    if (ifCheckWithinFeasibleRange)
                    {
                        feasibleMin = Convert.ToDouble(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.FeasibleRange_minimum));
                        feasibleMax = Convert.ToDouble(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.FeasibleRange_maximum));
                    }
                }

                // find the type
                RatioTrajectory.EnumType type = RatioTrajectory.EnumType.AccumulatedIncidenceOverAccumulatedIncidence;
                switch (strType)
                {
                    case "Incidence/Incidence":
                        type = RatioTrajectory.EnumType.IncidenceOverIncidence;
                        break;
                    case "Accumulated Incidence/Accumulated Incidence":
                        type = RatioTrajectory.EnumType.AccumulatedIncidenceOverAccumulatedIncidence;
                        break;
                    case "Prevalence/Prevalence":
                        type = RatioTrajectory.EnumType.PrevalenceOverPrevalence;
                        break;
                    case "Incidence/Prevalence":
                        type = RatioTrajectory.EnumType.IncidenceOverPrevalence;
                        break;
                }

                // build a ratio stat
                RatioTrajectory thisRatioTraj = new RatioTrajectory(
                    statID, 
                    name, 
                    type, 
                    ratioFormula, 
                    ifDispay, 
                    _modelSets.WarmUpPeriodTimeIndex, 
                    _modelSets.NumOfDeltaT_inSimOutputInterval);

                // set up calibration
                thisRatioTraj.CalibInfo = new TrajectoryCalibrationInfo(ifIncludedInCalibration, ifCheckWithinFeasibleRange, feasibleMin, feasibleMax);

                // add the summation statistics
                EpiHist.RatioTrajs.Add(thisRatioTraj);
            }
        }
        // add connections
        private void AddConnections(int[,] connectionsMatrix)
        {
            int i = 0;
            int classID, processID;
            while (i < connectionsMatrix.GetLength(0))
            {
                classID = connectionsMatrix[i, 0];
                processID = connectionsMatrix[i, 1];
                ((Class_Normal)Classes[classID]).AddAnEvent((Event)_events[processID]);
                
                ++i;
            }
        }        
        #endregion

    }
}
