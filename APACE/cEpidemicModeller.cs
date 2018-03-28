using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SimulationLib;
using RandomVariateLib;
using ComputationLib;

namespace APACE_lib
{
    public class EpidemicModeller
    {
        // Variable Definition 
        #region Variable Definition
        private int _ID;
        private ModelSettings _modelSettings;
        // parent epidemic and collection of epidemics
        private Epidemic _parentEpidemic;
        //private ArrayList _epidemics = new ArrayList();
        private List<Epidemic> _epidemics = new List<Epidemic>();
        // model information
        int _numOfInterventions;
        int _numOfInterventionsAffectingContactPattern;
        int _numOfInterventionsControlledDynamically;
        int _numOfFeatures;
        bool _storeEpidemicTrajectories;
        string[] _interventionNames;
        string[] _namesOfDefaultInterventionsAndThoseSpecifiedByDynamicRule;
        string[] _featureNames;
        string[] _namesOfParameters;
        // simulation setting
        RNG _rng = new RNG(0);  
        //private enumModelUse _modelUse = enumModelUse.Simulation;
        //private enumSimulationRNDSeedsSource _simulationRNDSeedsSource = enumSimulationRNDSeedsSource.StartFrom0;
        //private int _firstRNGSeed;
        //private int _distanceBtwRNGSeeds;
        //private int[] _rndSeeds;
        //private double[] _rndSeedsGoodnessOfFit;
        RandomVariateLib.Discrete _discreteDistributionOverSeeds;
        private int[] _sampledRNDSeeds;

        // simulation outputs
        private int _numOfMonitoredSimulationOutputs;
        private int _numOfTimeBasedOutputsToReport;
        private int _numOfIntervalBasedOutputsToReport;
        private int _numOfResourcesToReport;
        private int[][] _pastActionCombinations;
        private double[][] _simulationTimeBasedOutputs;
        private double[][] _simulationIntervalBasedOutputs;
        private double[][] _simulationObservableOutputs;
        private double[][] _simulationResourcesOutputs;
        private double[][] _timeOfSimulationObservableOutputs;        
        // simulation statistics collection
        private ArrayList _incidenceStats = new ArrayList();
        private ArrayList _prevalenceStats = new ArrayList();
        private ArrayList _ratioStatistics = new ArrayList();
        private ArrayList _computationStatistics = new ArrayList();
        private int[] _arrSimItrs;
        private int[] _arrRNGSeeds;
        private double[] _arrNHB;
        private double[] _arrNMB;
        private double[] _arrSimulationQALY;
        private double[] _arrSimulationCost;
        private double[] _arrSimulationAnnualCost;
        private ObservationBasedStatistics _obsTotalCost = new ObservationBasedStatistics("Total cost");
        private ObservationBasedStatistics _obsAnnualCost = new ObservationBasedStatistics("Annual cost");
        private ObservationBasedStatistics _obsTotalQALY = new ObservationBasedStatistics("Total QALY");
        private ObservationBasedStatistics _obsTotalNMB = new ObservationBasedStatistics("Total NMB");
        private ObservationBasedStatistics _obsTotalNHB = new ObservationBasedStatistics("Total NHB");
        private ObservationBasedStatistics _obsNumOfSwitchesBtwDecisions = new ObservationBasedStatistics("Total switches");
        private double[][] _sampledParameterValues;
        // calibration
        private Calibration _calibration;
        private int _numOfCalibratoinTargets;
        //private int _numOfSpecialStatisticsIncludedInCalibration;
        //private double _calibratoinWarmUpPeriodLength;
        //private int _numOfWarmUpObservationPeriodsForCalibration;
        //private int[][] _prespecifiedDecisionsOverObservationPeriods;
        private int _numOfParametersToCalibrate;

        // computation statistics
        private double _actualTimeUsedToSimulateAllTrajectories; // seconds        
        private ObservationBasedStatistics _obsTimeUsedToSimulateATrajectory = new ObservationBasedStatistics("Time used to simulate a trajectory");
        private double _actualTimeUsedByCalibration;
        private double _totalSimulationTimeUsedByCalibration;
        private int _numOfTrajectoriesDiscardedByCalibration;

        #endregion
        #region Properties
        public int ID
        { get { return _ID; } }
        public string[] ParameterNames
        { get { return _namesOfParameters; } }
        public int CurrentRNDSeed
        {
            get { return _parentEpidemic.RNDSeedResultedInAnAcceptibleTrajectory; }
        }
        public int CurrentEpidemicTimeIndex
        {
            get {return _parentEpidemic.CurrentEpidemicTimeIndex; }
        }
        public double CurrentEpidemicTime
        {
            get { return _parentEpidemic.CurrentEpidemicTime; }
        }
        public int NumOfInterventions
        {
            get { return _numOfInterventions; }
        }
        public int NumOfInterventionsAffectingContactPattern
        {
            get { return _numOfInterventionsAffectingContactPattern; }
        }
        public int NumOfInterventionsControlledDynamically
        {
            get { return _numOfInterventionsControlledDynamically; }
        }
        public string[] InterventionNames
        {
            get { return _interventionNames; }
        }
        public string[] NamesOfDefaultInterventionsAndThoseSpecifiedByDynamicRule
        {
            get { return _namesOfDefaultInterventionsAndThoseSpecifiedByDynamicRule; }
        }
        public ArrayList Features
        {
            get {return _parentEpidemic.Features;}
        }
        public int NumOfFeatures
        {
            get { return _numOfFeatures; }
        }
        public string[] FeatureNames
        {
            get { return _featureNames; }
        }
        public bool StoreEpidemicTrajectories
        {
            get { return _storeEpidemicTrajectories; }
        }
        public List<Class> Classes
        {
            get { return _parentEpidemic.Classes;}
        }
        public List<SummationStatistics> SummationStatistics
        {
            get {return _parentEpidemic.SummationStatistics; }
        }
        public List<RatioStatistics> RatioStatistics
        {
            get {return _parentEpidemic.RatioStatistics;}
        }
        public ArrayList Resources
        {
            get { return _parentEpidemic.Resources;}
        }
        public int[][] PastActionCombinations
        {
            get { return _pastActionCombinations; }
        }
        public double[,] SimulationTimeBasedOutputs
        {
            get { return SupportFunctions.ConvertFromJaggedArrayToRegularArray(_simulationTimeBasedOutputs, _numOfTimeBasedOutputsToReport); }
        }
        public double[,] SimulationIntervalBasedOutputs
        {
            get { return SupportFunctions.ConvertFromJaggedArrayToRegularArray(_simulationIntervalBasedOutputs, _numOfIntervalBasedOutputsToReport); }
        }
        public double[,] TimeOfSimulationObservableOutputs
        {
            get { return SupportFunctions.ConvertFromJaggedArrayToRegularArray(_timeOfSimulationObservableOutputs, 1); }
        }
        public double[,] SimulationMonitoredOutputs
        {
            get { return SupportFunctions.ConvertFromJaggedArrayToRegularArray(_simulationObservableOutputs, _numOfMonitoredSimulationOutputs); }
        }
        public double[,] SimulationResourcesOutputs
        {
            get { return SupportFunctions.ConvertFromJaggedArrayToRegularArray(_simulationResourcesOutputs, _numOfResourcesToReport); }
        }
        public int NumOfMonitoredSimulationOutputs
        {
            get { return _numOfMonitoredSimulationOutputs; }
        }
        // calibration
        public Calibration Calibration
        {
            get { return _calibration; }
        }
        public int NumOfCalibratoinTargets
        {
            get { return _numOfCalibratoinTargets; }
        }
        // simulation outcomes
        public double TotalNHB
        {
            get { return _parentEpidemic.TotalNHB; }
        }
        public double TotalNMB
        {
            get { return _parentEpidemic.TotalNMB; }
        }
        // simulation summary
        public ObservationBasedStatistics obsTotalCost
        {
            get { return _obsTotalCost; }
        }
        public ObservationBasedStatistics obsAnnualCost
        {
            get { return _obsAnnualCost; }
        }
        public ObservationBasedStatistics obsTotalQALY
        {
            get { return _obsTotalQALY; }
        }
        public ObservationBasedStatistics obsTotalNMB
        {
            get { return _obsTotalNMB; }
        }
        public ObservationBasedStatistics obsTotalNHB
        {
            get { return _obsTotalNHB; }
        }
        public ObservationBasedStatistics obsNumberOfSwitchesBtwDecisions
        {
            get { return _obsNumOfSwitchesBtwDecisions; }
        }        
        // simulation iterations
        public int[] SimulationIterations_itrs
        {
            get { return _arrSimItrs; }
        }
        public int[] SimulationIterations_RNGSeeds
        {
            get { return _arrRNGSeeds; }
        }
        public double[][] SimulationIterations_ParameterValues
        {
            get { return _sampledParameterValues; }
        }
        public double[] SimulationIterations_NHB
        {
            get { return _arrNHB; }
        }
        public double[] SimulationIterations_NMB
        {
            get { return _arrNMB; }
        }
        public double[] SimulationIterations_QALY
        {
            get { return _arrSimulationQALY; }
        }
        public double[] SimulationIterations_Cost
        {
            get { return _arrSimulationCost; }
        }        
        public double[] SimulationIterations_AnnualCost
        {
            get { return _arrSimulationAnnualCost; }
        }
        // computation performance
        public ObservationBasedStatistics ObsTimeUsedToSimulateATrajectory
        {
            get{return _obsTimeUsedToSimulateATrajectory;}
        }
        public double ActualTimeUsedToSimulateAllTrajectories
        {
            get { return _actualTimeUsedToSimulateAllTrajectories; }
        }
        public double ActualTimeUsedByCalibration
        {
            get { return _actualTimeUsedByCalibration; }
        }
        public double TotalSimulationTimeUsedByCalibration
        {
            get { return _totalSimulationTimeUsedByCalibration; }
        }
        public int NumOfTrajectoriesDiscardedByCalibration
        { get { return _numOfTrajectoriesDiscardedByCalibration; } }

        #endregion

        // Instantiation
        public EpidemicModeller(int ID, ref ExcelInterface excelInterface, ref ModelSettings modelSettings)
        {
            _ID = ID;
            _modelSettings = modelSettings;

            _parentEpidemic = _modelSettings.TempEpidemic.Clone(0);
            _parentEpidemic.BuildModel(ref _modelSettings);

            // gather model information
            CollectInformationAvailableAfterModelIsBuilt(_parentEpidemic);

            // read contact matrices
            _modelSettings.ReadContactMatrices(ref excelInterface, _numOfInterventionsAffectingContactPattern);
            _parentEpidemic.UpdateContactMatrices();

            // find the names of parameters
            FindNamesOfParameters();

            // if use parallel computing, create a collection of epidemics
            if (_modelSettings.UseParallelComputing)
            {
                // find how many epi model to create
                int numOfEpis = 0;
                switch (_modelSettings.ModelUse)
                {
                    case enumModelUse.Simulation:
                        numOfEpis = _modelSettings.NumOfSimulationIterations;
                        break;
                    case enumModelUse.Calibration:
                        numOfEpis = _modelSettings.NumOfSimulationsRunInParallelForCalibration;
                        break;
                }

                // create the epi models
                _epidemics.Clear();
                Object thisLock = new Object();
                Parallel.For(0, numOfEpis, simItr =>
                {
                    // create an epidemic
                    Epidemic thisEpidemic = _parentEpidemic.Clone(simItr);
                    // add this epidemic
                    lock (thisLock)
                        {
                            _epidemics.Add(thisEpidemic);
                        }
                });
            }

            // add contact matrices
            //AddContactMatrices(_modelSettings.GetBaseContactMatrices(), _modelSettings.GetPercentChangeInContactMatricesParIDs());

            // set up adaptive policy related settings if necessary
            if (_numOfInterventionsControlledDynamically> 0)
                AddDynamicPolicySettings(ref excelInterface);

            //// read prespecified interventions
            //if (_modelSettings.TempEpidemic.DecisionRule == enumDecisionRule.PredeterminedSequence ||
            //    _modelSettings.ModelUse == enumModelUse.Calibration)
            //    _prespecifiedDecisionsOverObservationPeriods = (int[][])_modelSettings.PrespecifiedSequenceOfInterventions.Clone();

            // find the weighted RND seeds
            if (_modelSettings.SimulationRNDSeedsSource == enumSimulationRNDSeedsSource.WeightedPrespecifiedSquence)
            {
                int n = _modelSettings.RndSeeds.Length;
                double[] arrProb = new double[n];
                for (int i = 0; i < n; i++)
                    arrProb[i] = _modelSettings.RndSeedsGoodnessOfFit[i];

                // form a probability function over rnd seeds
                double sum = arrProb.Sum();
                for (int i = 0; i < n; i++)
                    arrProb[i] = arrProb[i] / sum;

                // define the sampling object
                _discreteDistributionOverSeeds = new Discrete("Discrete distribution over RND seeds", arrProb); //SupportFunctions.ConvertArrayToDouble(_modelSettings.RndSeeds)
            }            
        }

        // destruction
        public void MyDestruct()
        {
            _epidemics.Clear();
            _epidemics = null;
            GC.Collect();
        }

        // simulate several epidemics
        public void SimulateEpidemics()
        {
            // initialize simulation            
            InitializeSimulation();

            // simulation time
            int startTime, endTime;
            startTime = Environment.TickCount;

            // use parallel computing? 
            if (_modelSettings.UseParallelComputing == false)
            {
                #region Use sequential computing
                // build the parent epidemic model
                //_parentEpidemic.BuildModel(ref _modelSettings);

                int rndSeedToGetAnAcceptibleEpidemic = 0;
                for (int simItr = 0; simItr < _modelSettings.NumOfSimulationIterations; ++simItr)
                {
                    // find the RND seed for this iteration
                    rndSeedToGetAnAcceptibleEpidemic = FindRNDSeed(simItr);

                    // simulate one epidemic trajectory
                    _parentEpidemic.SimulateTrajectoriesUntilOneAcceptibleFound(
                        rndSeedToGetAnAcceptibleEpidemic, 
                        rndSeedToGetAnAcceptibleEpidemic + Math.Max(_modelSettings.DistanceBtwRNGSeeds, 1), 
                        simItr,
                        _modelSettings.TempEpidemic.SimulationHorizonTimeIndex);

                    // report this epidemic trajectory
                    if (_storeEpidemicTrajectories)
                    {
                        _pastActionCombinations = SupportFunctions.ConcatJaggedArray(_pastActionCombinations, _parentEpidemic.PastActionCombinations);
                        _simulationTimeBasedOutputs = SupportFunctions.ConcatJaggedArray(_simulationTimeBasedOutputs, _parentEpidemic.SimulationTimeBasedOutputs);
                        _simulationIntervalBasedOutputs = SupportFunctions.ConcatJaggedArray(_simulationIntervalBasedOutputs, _parentEpidemic.SimulationIntervalBasedOutputs);
                        _simulationObservableOutputs = SupportFunctions.ConcatJaggedArray(_simulationObservableOutputs, _parentEpidemic.SimulationObservableOutputs);
                        _simulationResourcesOutputs = SupportFunctions.ConcatJaggedArray(_simulationResourcesOutputs, _parentEpidemic.SimulationResourceOutpus);
                        _timeOfSimulationObservableOutputs = SupportFunctions.ConcatJaggedArray(_timeOfSimulationObservableOutputs, _parentEpidemic.TimesOfEpidemicObservationsOverPastObservationPeriods);

                    }
                    // store outcomes of this epidemic 
                    if (_parentEpidemic.ModelUse == enumModelUse.Simulation)
                        StoreOutcomesOfThisEpidemic(_parentEpidemic, simItr);

                    // store sampled parameter values
                    _sampledParameterValues[simItr] = _parentEpidemic.arrSampledParameterValues;
                }
                #endregion
            }
            else // (_modelSettings.UseParallelComputing == true)
            {
                #region Use parallel computing

                // simulate and store outcomes
                int rndSeedToGetAnAcceptibleEpidemic = 0;

                Parallel.ForEach(_epidemics, thisEpidemic =>
                {
                    // build the parent epidemic model
                    thisEpidemic.BuildModel(ref _modelSettings);

                    // find the RND seed for this iteration
                    rndSeedToGetAnAcceptibleEpidemic = FindRNDSeed(thisEpidemic.ID);

                    // simulate
                    thisEpidemic.SimulateTrajectoriesUntilOneAcceptibleFound(
                        rndSeedToGetAnAcceptibleEpidemic, 
                        rndSeedToGetAnAcceptibleEpidemic + Math.Max(_modelSettings.DistanceBtwRNGSeeds, 1), 
                        ((Epidemic)thisEpidemic).ID, 
                        _modelSettings.TempEpidemic.SimulationHorizonTimeIndex);
                });

                // store epidemic trajectories and outcomes
                foreach (Epidemic thisEpidemic in _epidemics)
                {
                    if (_storeEpidemicTrajectories)
                    {
                        _pastActionCombinations = SupportFunctions.ConcatJaggedArray(_pastActionCombinations, thisEpidemic.PastActionCombinations);
                        _simulationTimeBasedOutputs = SupportFunctions.ConcatJaggedArray(_simulationTimeBasedOutputs, thisEpidemic.SimulationTimeBasedOutputs);
                        _simulationIntervalBasedOutputs = SupportFunctions.ConcatJaggedArray(_simulationIntervalBasedOutputs, thisEpidemic.SimulationIntervalBasedOutputs);
                        _simulationObservableOutputs = SupportFunctions.ConcatJaggedArray(_simulationObservableOutputs, thisEpidemic.SimulationObservableOutputs);
                        _simulationResourcesOutputs = SupportFunctions.ConcatJaggedArray(_simulationResourcesOutputs, thisEpidemic.SimulationResourceOutpus);
                        _timeOfSimulationObservableOutputs = SupportFunctions.ConcatJaggedArray(_timeOfSimulationObservableOutputs, thisEpidemic.TimesOfEpidemicObservationsOverPastObservationPeriods);
                    }
                    // store outcomes of this epidemic 
                    if (thisEpidemic.ModelUse == enumModelUse.Simulation)
                        StoreOutcomesOfThisEpidemic(thisEpidemic, thisEpidemic.ID);

                    // store sampled parameter values
                    _sampledParameterValues[thisEpidemic.ID] = thisEpidemic.arrSampledParameterValues;
                }
                #endregion
            }

            // simulation run time
            endTime = Environment.TickCount;
            _actualTimeUsedToSimulateAllTrajectories = (double)(endTime - startTime) / 1000;
        }
        // calibrate
        public void Calibrate(int numOfInitialSimulationRuns, int numOfFittestRunsToReturn)
        {   
            int calibrationTimeHorizonIndex = _modelSettings.TempEpidemic.SimulationHorizonTimeIndex;
            int numOfSimulationsRunInParallelForCalibration = _modelSettings.NumOfSimulationsRunInParallelForCalibration;

            // reset calibration
            _calibration.Reset();
            // toggle to calibration
            ToggleModellerTo(enumModelUse.Calibration, enumDecisionRule.PredeterminedSequence, false, _modelSettings.TempEpidemic.WarmUpPeriodIndex); 

            // keep obtaining trajectories until enough
            int simItr = -1;
            int simItrParallel = -1;
            int parallelLoopIndex = 0;

            // computation time
            int startTime, endTime;
            startTime = Environment.TickCount;
            _totalSimulationTimeUsedByCalibration = 0;
            _numOfTrajectoriesDiscardedByCalibration = 0;

            // build the epidemic models if using parallel computing
            if (_modelSettings.UseParallelComputing)
            {
                Parallel.ForEach(_epidemics, thisEpidemic =>
                {
                    // build the parent epidemic model
                    thisEpidemic.BuildModel(ref _modelSettings);
                });
            }

            while (simItr <  numOfInitialSimulationRuns - 1)
                //|| !_calibration.IfAcceptedSimulationRunsAreSymmetricAroundObservations)
            {
                // use parallel computing? 
                if (_modelSettings.UseParallelComputing == false)
                {
                    #region Use sequential computing
                    // increment the simulation iteration
                    ++simItr;
                    // find the RND seed for this iteration
                    int rndSeedToGetAnAcceptibleEpidemic = FindRNDSeed(simItr);                    

                    // simulate one epidemic trajectory
                    //_parentEpidemic.SimulateTrajectoriesUntilOneAcceptibleFound(rndSeedToGetAnAcceptibleEpidemic, int.MaxValue, simItr, calibrationTimeHorizonIndex);
                    _parentEpidemic.SimulateOneTrajectory(rndSeedToGetAnAcceptibleEpidemic, simItr, calibrationTimeHorizonIndex);

                    // calibration time
                    _totalSimulationTimeUsedByCalibration += _parentEpidemic.TimeUsedToSimulateOneTrajectory;

                    // find the number of discarded trajectories    
                    if (_parentEpidemic.RNDSeedResultedInAnAcceptibleTrajectory == -1)
                        _numOfTrajectoriesDiscardedByCalibration += 1;
                    else
                    {
                        // add this simulation observations
                        //_calibration.AddResultOfASimulationRun(simItr, _parentEpidemic.RNDSeedResultedInAnAcceptibleTrajectory, _parentEpidemic.GetValuesOfParametersToCalibrate(),
                        //SupportFunctions.ConvertFromJaggedArrayToRegularArray(_parentEpidemic.CalibrationObservation, _parentEpidemic.NumOfCalibratoinTargets));
                        double[,] mOfObs = SupportFunctions.ConvertFromJaggedArrayToRegularArray(_parentEpidemic.CalibrationObservation, _parentEpidemic.NumOfCalibratoinTargets);
                        double[] par = new double[0];
                        _calibration.AddResultOfASimulationRun(simItr, _parentEpidemic.RNDSeedResultedInAnAcceptibleTrajectory, ref par, ref mOfObs);

                        // find the fit of the stored simulation results
                        _calibration.FindTheFitOfRecordedSimulationResults(_modelSettings.UseParallelComputing);
                    }
                    #endregion
                }
                else // (_modelSettings.UseParallelComputing == true)
                {
                    #region Use parallel computing
                    
                    // simulate and store outcomes
                    int rndSeedToGetAnAcceptibleEpidemic = 0;
                    //Object thisLock = new Object();
                    //Parallel.ForEach(_epidemics.Cast<object>(), thisEpidemic =>
                    Parallel.ForEach(_epidemics, thisEpidemic =>
                    {
                        // build the parent epidemic model
                        //thisEpidemic.BuildModel(ref _modelSettings);

                        // find sim iteration 
                        simItr = numOfSimulationsRunInParallelForCalibration * parallelLoopIndex + thisEpidemic.ID;

                        // find the RND seed for this iteration
                        rndSeedToGetAnAcceptibleEpidemic = FindRNDSeed(simItr);

                        // simulate            
                        //((Epidemic)thisEpidemic).SimulateTrajectoriesUntilOneAcceptibleFound(rndSeedToGetAnAcceptibleEpidemic, int.MaxValue, simItr, calibrationTimeHorizonIndex);
                        thisEpidemic.SimulateOneTrajectory(rndSeedToGetAnAcceptibleEpidemic, simItr, calibrationTimeHorizonIndex);

                        // clean the memory
                        //thisEpidemic.CleanExceptResults();
                    });

                    // run the calibration
                    foreach (Epidemic thisEpidemic in _epidemics)
                    {
                        ++ simItrParallel;
                        // simulation time
                        _totalSimulationTimeUsedByCalibration += thisEpidemic.TimeUsedToSimulateOneTrajectory;
                        // sim itr
                        simItr = numOfSimulationsRunInParallelForCalibration * parallelLoopIndex + thisEpidemic.ID;

                        // find the number of discarded trajectories    
                        //_numOfTrajectoriesDiscardedByCalibration += thisEpidemic.NumOfDiscardedTrajectoriesAmongCalibrationRuns;
                        if (thisEpidemic.RNDSeedResultedInAnAcceptibleTrajectory == -1)
                            _numOfTrajectoriesDiscardedByCalibration += 1;
                        else
                        {
                            double[,] mOfObs = SupportFunctions.ConvertFromJaggedArrayToRegularArray(thisEpidemic.CalibrationObservation, thisEpidemic.NumOfCalibratoinTargets);
                            double[] par = new double[0];
                            // add this simulation observations
                            _calibration.AddResultOfASimulationRun(simItr, thisEpidemic.RNDSeedResultedInAnAcceptibleTrajectory, ref par, ref mOfObs);
                        }
                    }
                    
                    // find the fit of the stored simulation results
                    _calibration.FindTheFitOfRecordedSimulationResults(_modelSettings.UseParallelComputing);

                    // increment the loop id
                    ++parallelLoopIndex;
                    simItr = simItrParallel;
                    #endregion
                }                
            }

            // find the fittest runs
            _calibration.FindTheFittestSimulationRuns(numOfFittestRunsToReturn);

            // computation time
            endTime = Environment.TickCount;
            _actualTimeUsedByCalibration = (double)(endTime - startTime) / 1000;

        }
        // find the optimal dynamic policy
        public void FindOptimalDynamicPolicy()
        {
            // find the optimal dynamic policy
            _parentEpidemic.FindOptimalDynamicPolicy();
        }
        // simulate the optimal dynamic policy
        public void SimulateTheOptimalDynamicPolicy(int numOfSimulationIterations, int timeIndexToStop, int warmUpPeriodIndex, bool storeEpidemicTrajectories)
        {
            // toggle to simulation
            ToggleModellerTo(enumModelUse.Simulation, enumDecisionRule.SpecifiedByPolicy, storeEpidemicTrajectories, warmUpPeriodIndex);            
            // simulate epidemic (sequential)
            SimulateEpidemics();
        }
        // simulate one trajectory until the first time decisions should be made
        public void SimulateOneTrajectoryUntilTheFirstDecisionPoint(int threadSpecificSeedNumber)
        {
            _parentEpidemic.SimulateOneTrajectoryUntilTheFirstDecisionPoint(threadSpecificSeedNumber);
        }
        // continue simulating current trajectory 
        public void ContinueSimulatingThisTrajectory(int additionalDeltaTs)
        {
            _parentEpidemic.ContinueSimulatingThisTrajectory(additionalDeltaTs);
        }
        // get if stop condition of this epidemic satisfied
        public bool GetIfEitherEradicatedOrSimulationHorizonHasReached()
        {
            bool value = false;
            if (_parentEpidemic.IfStoppedDueToEradication ||
                (_parentEpidemic.CurrentEpidemicTimeIndex >= _parentEpidemic.SimulationHorizonTimeIndex))
                value = true;
            return value;
        }
        // change the status of storing epidemic trajectories
        public void ShouldStoreEpidemicTrajectories(bool yesOrNo)
        {
            _storeEpidemicTrajectories = yesOrNo;
            if (_modelSettings.UseParallelComputing)
            {
                foreach (Epidemic thisEpidemic in _epidemics)
                    thisEpidemic.StoreEpidemicTrajectories = yesOrNo;
            }
            else
                _parentEpidemic.StoreEpidemicTrajectories = yesOrNo;
        }
        // update the time to start decision making and collecting epidemic outcomes
        public void UpdateTheTimeToStartDecisionMakingAndWarmUpPeriod(double timeToStartDecisionMaking, double warmUpPeriod)
        {
            if (_modelSettings.UseParallelComputing)
            {
                foreach (Epidemic thisEpidemic in _epidemics)
                    thisEpidemic.UpdateTheTimeToStartDecisionMakingAndWarmUpPeriod(timeToStartDecisionMaking, warmUpPeriod);
            }
            else
                _parentEpidemic.UpdateTheTimeToStartDecisionMakingAndWarmUpPeriod(timeToStartDecisionMaking, warmUpPeriod);
        }


        //// build one epidemic model - sequantial processing
        //public void BuildAnEpidemicModelForSequentialSimulation(Epidemic parentEpidemic, Array parametersSheet, Array pathogensSheet,
        //    Array classesSheet, Array interventionsSheet, Array resourcesSheet, Array processesSheet,
        //    Array summationStatisticsSheet, Array ratioStatisticsSheet,
        //    int[,] connectionsMatrix)
        //{
        //    _modelSettings.UseParallelComputing = false;
        //    // build the parent epidemic model
        //    _parentEpidemic = parentEpidemic.Clone(0);
        //    //_parentEpidemic.BuildModel(parametersSheet, pathogensSheet, classesSheet, interventionsSheet, resourcesSheet, processesSheet,
        //    //    summationStatisticsSheet, ratioStatisticsSheet, connectionsMatrix);//, baseContactMatrices, percentChangeInContactMatricesParIDs);

        //    //// setup storing simulation trajectories
        //    //_parentEpidemic.SetupStoringSimulationTrajectory();

        //    // gather model information
        //    CollectInformationAvailableAfterModelIsBuilt();
        //}
        
        //// build a collection of epidemic model - parallel processing
        //public void BuildCollectionOfEpidemicModelForParralelSimulation(int numOfParallelEpidemics, Epidemic parentEpidemic,
        //    Array parametersSheet, Array pathogensSheet, Array classesSheet, Array interventionsSheet, Array resourcesSheet, Array processesSheet,
        //    Array summationStatisticsSheet, Array ratioStatisticsSheet,
        //    int[,] connectionsMatrix)
        //{
        //    _modelSettings.UseParallelComputing = true;
        //    // build collection of epidemics
        //    _epidemics.Clear();
        //    Object thisLock = new Object();
        //    Parallel.For(0, numOfParallelEpidemics, simItr =>
        //    {
        //        // create an epidemic
        //        Epidemic thisEpidemic = parentEpidemic.Clone(simItr);
        //        //// build the parent epidemic model
        //        //thisEpidemic.BuildModel(parametersSheet, pathogensSheet, classesSheet, interventionsSheet, resourcesSheet, processesSheet,
        //        //    summationStatisticsSheet, ratioStatisticsSheet, 
        //        //    connectionsMatrix);//, baseContactMatrices, percentChangeInContactMatricesParIDs);

        //        //// setup storing simulation trajectories
        //        //thisEpidemic.SetupStoringSimulationTrajectory();

        //        // add this epidemic
        //        lock (thisLock)
        //        {
        //            _epidemics.Add(thisEpidemic);
        //        }
        //    });
        //    // gather model information
        //    CollectInformationAvailableAfterModelIsBuilt();
        //}

        //// add contact matrices
        //public void AddContactMatrices(double[][,] baseContactMatrices, int[][][,] percentChangeInContactMatricesParIDs)
        //{
        //    if (_modelSettings.UseParallelComputing)
        //    {
        //        foreach (Epidemic thisEpidemic in _epidemics)
        //            thisEpidemic.AddContactMatrices(ref baseContactMatrices, ref percentChangeInContactMatricesParIDs);
        //    }
        //    else _parentEpidemic.AddContactMatrices(ref baseContactMatrices, ref percentChangeInContactMatricesParIDs);
        //}
        
        //// specify RNG source, sequential
        //public void SpecifyRNGSource(int firstRNGSeed, int distanceBtwRNGSeeds)
        //{
        //    _simulationRNDSeedsSource = enumSimulationRNDSeedsSource.StartFrom0;
        //    _firstRNGSeed = firstRNGSeed;            
        //    _distanceBtwRNGSeeds = distanceBtwRNGSeeds;
        //}
        //// specify RNG source, prespecified 
        //public void SpecifyRNGSource(int[] rndSeeds)
        //{
        //    _simulationRNDSeedsSource = enumSimulationRNDSeedsSource.PrespecifiedSquence;
        //    _rndSeeds = rndSeeds;
        //}
        //// specify RNG source, weighted
        //public void SpecifyRNGSource(int[] rndSeeds, double[] rndSeedsGoodnessOfFit)
        //{
        //    _simulationRNDSeedsSource = enumSimulationRNDSeedsSource.WeightedPrespecifiedSquence;
        //    _rndSeeds = (int[])rndSeeds.Clone();
        //    _rndSeedsGoodnessOfFit = (double[])rndSeedsGoodnessOfFit.Clone();

        //    double[] arrProb = new double[rndSeeds.Length];
        //    for (int i = 0; i < rndSeeds.Length; i++)
        //        //arrProb[i] = 1 / _rndSeedsGoodnessOfFit[i];
        //        arrProb[i] = _rndSeedsGoodnessOfFit[i];// Math.Exp(-Math.Pow(_rndSeedsGoodnessOfFit[i],2));

        //    // form a probability function over rnd seeds
        //    double sum = arrProb.Sum();
        //    for (int i = 0; i < rndSeeds.Length; i++)
        //        arrProb[i] = arrProb[i] / sum;

        //    // define the sampling object
        //    _discreteDistributionOverSeeds = new Discrete("Discrete distribution over RND seeds", SupportFunctions.ConvertArrayToDouble(rndSeeds), arrProb);
        //}

        //// switch off all interventions controled by decision rule
        //public void SwitchOffAllInterventionsControlledByDecisionRule()
        //{
        //    if (_modelSettings.UseParallelComputing)
        //        foreach (Epidemic thisEpidemic in _epidemics)
        //        {
        //            // update initial decision
        //            thisEpidemic.SwitchOffAllInterventionsControlledByDecisionRule();
        //        }
        //    else
        //        // update initial decision
        //        _parentEpidemic.SwitchOffAllInterventionsControlledByDecisionRule();
        //}
        // get possible intervention combinations for on/off static policies
        public ArrayList GetIntervalBasedStaticPoliciesDesigns()
        {
            if (_modelSettings.UseParallelComputing)
                return ((Epidemic)_epidemics[0]).GetIntervalBasedStaticPoliciesDesigns();
            else
                return _parentEpidemic.GetIntervalBasedStaticPoliciesDesigns();
        }        

        // add policy related settings
        public void AddDynamicPolicySettings(ref ExcelInterface excelInterface)
        {
            if (_modelSettings.UseParallelComputing)
            {                
                Parallel.ForEach(_epidemics.Cast<object>(), thisEpidemic =>
                {
                    // setup policy-related settings
                    ((Epidemic)thisEpidemic).SetupDynamicPolicySettings(
                        _modelSettings.QFunApxMethod, _modelSettings.IfEpidemicTimeIsUsedAsFeature,
                        _modelSettings.DegreeOfPolynomialQFunction, _modelSettings.L2RegularizationPenalty);                    
                });
                // find the number of features
                _numOfFeatures = ((Epidemic)_epidemics[0]).NumOfFeatures;
                // read feature names                
                _featureNames = new string[((Epidemic)_epidemics[0]).NumOfFeatures];
                foreach (Feature thisFeature in ((Epidemic)_epidemics[0]).Features)
                    _featureNames[thisFeature.Index] = thisFeature.Name;
            }
            else
            {
                // setup policy-related settings
                _parentEpidemic.SetupDynamicPolicySettings(
                    _modelSettings.QFunApxMethod, _modelSettings.IfEpidemicTimeIsUsedAsFeature,
                    _modelSettings.DegreeOfPolynomialQFunction, _modelSettings.L2RegularizationPenalty);
                // find the number of features
                _numOfFeatures = _parentEpidemic.NumOfFeatures;
                // read feature names                
                _featureNames = new string[_parentEpidemic.NumOfFeatures];
                foreach (Feature thisFeature in _parentEpidemic.Features)
                    _featureNames[thisFeature.Index] = thisFeature.Name;
            }

            // update the q-function coefficients
            double[] qFunCoefficients = excelInterface.GetQFunctionCoefficients(_numOfFeatures);
            if (_modelSettings.UseParallelComputing)
            {
                Parallel.ForEach(_epidemics.Cast<object>(), thisEpidemic =>
                {
                    // q-function coefficients
                    ((Epidemic)thisEpidemic).UpdateQFunctionCoefficients(qFunCoefficients);
                });
            }
            else
            {
                // q-function coefficients
                _parentEpidemic.UpdateQFunctionCoefficients(qFunCoefficients);
            }
        }
        // add always on/off and interval-based static policy settings
        public void AddAlwaysOnOffAndIntervalBasedStaticPolicySettings(int[] interventionsOnOffSwitchStatus, double[] startTime, int[] numOfDecisionPeriodsToUse)
        {
            if (_modelSettings.UseParallelComputing)
            {
                foreach (Epidemic thisEpidemic in _epidemics)
                    // setup policy-related settings
                    thisEpidemic.SetupAlwaysOnOffAndIntervalBasedStaticPolicySettings(interventionsOnOffSwitchStatus, startTime, numOfDecisionPeriodsToUse);
            }
            else
                // setup policy-related settings
                _parentEpidemic.SetupAlwaysOnOffAndIntervalBasedStaticPolicySettings(interventionsOnOffSwitchStatus, startTime, numOfDecisionPeriodsToUse);
        }
        // add threshold-based static policy settings
        public void AddThresholdBasedStaticPolicySettings
            (int[] decisionIDs, int[] specialStatisticsIDs, double[] thresholds, int[] numOfDecisionPeriodsToUseInterventions)
        {
            if (_modelSettings.UseParallelComputing)
            {
                foreach (Epidemic thisEpidemic in _epidemics)
                    // setup policy-related settings
                    thisEpidemic.SetupThresholdBasedStaticPolicySettings(decisionIDs, specialStatisticsIDs, thresholds, numOfDecisionPeriodsToUseInterventions);
            }
            else
                // setup policy-related settings
                _parentEpidemic.SetupThresholdBasedStaticPolicySettings(decisionIDs, specialStatisticsIDs, thresholds, numOfDecisionPeriodsToUseInterventions);
        }
        public void AddThresholdBasedStaticPolicySettings
           (int[] decisionIDs, double[] thresholds, int[] numOfDecisionPeriodsToUseInterventions)
        {
            if (_modelSettings.UseParallelComputing)
            {
                foreach (Epidemic thisEpidemic in _epidemics)
                    // setup policy-related settings
                    thisEpidemic.SetupThresholdBasedStaticPolicySettings(decisionIDs, thresholds, numOfDecisionPeriodsToUseInterventions);
            }
            else
                // setup policy-related settings
                _parentEpidemic.SetupThresholdBasedStaticPolicySettings(decisionIDs, thresholds, numOfDecisionPeriodsToUseInterventions);
        }
        
        
        public void UpdateQFunctionCoefficients(double[] qFunctionCoefficients)
        {
            if (_modelSettings.UseParallelComputing)
            {
                Parallel.ForEach(_epidemics.Cast<object>(), thisEpidemic =>
                {
                    // q-function coefficients
                    ((Epidemic)thisEpidemic).UpdateQFunctionCoefficients(qFunctionCoefficients);
                });
            }
            else
            {
                // q-function coefficients
                _parentEpidemic.UpdateQFunctionCoefficients(qFunctionCoefficients);
            }
        }   
        // set up calibration
        public void SetUpCalibration()
        {
            _calibration = new Calibration(0);
            _storeEpidemicTrajectories = false;                        

            // find the names of the parameters
            FindNamesOfParametersToCalibrate();

            // add observations
            AddObservationsToSetUpCalibration(ref _parentEpidemic, _modelSettings.MatrixOfObservationsAndWeights);
        }
   
        // add observations to set up calibration
        private void AddObservationsToSetUpCalibration(ref Epidemic thisEpiModel, double[,] matrixOfObservationsAndWeights)
        {
            int j = 0;
            int numOfRows = matrixOfObservationsAndWeights.GetLength(0);

            // go over summation statistics
            foreach (SummationStatistics sumStat in thisEpiModel.SummationStatistics.Where(s => s.IfIncludedInCalibration))
            {
                double[] arrObservations = new double[numOfRows];
                double[] arrWeights = new double[numOfRows];
                // read observations for this target
                for (int i = 0; i < numOfRows; i++)
                {
                    arrObservations[i] = matrixOfObservationsAndWeights[i, j];
                    arrWeights[i] = matrixOfObservationsAndWeights[i, j + 1];
                }
                j += 2;
                // enter observations for this target
                switch (sumStat.GoodnessOfFitMeasure)
                {
                    case CalibrationTarget.enumGoodnessOfFitMeasure.SumSqurError_timeSeries:
                        _calibration.AddACalibrationTarget_timeSeries(sumStat.Name, sumStat.Weight_overalFit, arrObservations, arrWeights);
                        break;
                    case CalibrationTarget.enumGoodnessOfFitMeasure.SumSqurError_average:
                        _calibration.AddACalibrationTarget_aveTimeSeries(sumStat.Name, sumStat.Weight_overalFit, arrObservations, arrWeights);
                        break;
                    case CalibrationTarget.enumGoodnessOfFitMeasure.Fourier:
                        _calibration.AddACalibrationTarget_fourier(sumStat.Name, sumStat.Weight_overalFit, arrObservations, sumStat.Weight_fourierSimilarities);
                        break;
                }
            }

            // go over ratio statistics
            foreach (RatioStatistics ratioStat in thisEpiModel.RatioStatistics.Where(r => r.IfIncludedInCalibration))
            {
                double[] arrObservations = new double[numOfRows];
                double[] arrWeights = new double[numOfRows];
                // read observations for this target
                for (int i = 0; i < numOfRows; i++)
                {
                    arrObservations[i] = matrixOfObservationsAndWeights[i, j];
                    arrWeights[i] = matrixOfObservationsAndWeights[i, j + 1];
                }
                j += 2;
                // enter observations for this target
                switch (ratioStat.GoodnessOfFitMeasure)
                {
                    case CalibrationTarget.enumGoodnessOfFitMeasure.SumSqurError_timeSeries:
                        _calibration.AddACalibrationTarget_timeSeries(ratioStat.Name, ratioStat.Weight_overalFit, arrObservations, arrWeights);
                        break;
                    case CalibrationTarget.enumGoodnessOfFitMeasure.SumSqurError_average:
                        _calibration.AddACalibrationTarget_aveTimeSeries(ratioStat.Name, ratioStat.Weight_overalFit, arrObservations, arrWeights);
                        break;
                    case CalibrationTarget.enumGoodnessOfFitMeasure.Fourier:
                        _calibration.AddACalibrationTarget_fourier(ratioStat.Name, ratioStat.Weight_overalFit, arrObservations, ratioStat.Weight_fourierSimilarities);
                        break;
                }
            }
        }
        // set up optimization 
        public void SetUpOptimization(
            double wtpForHealth, double harmonicRule_a, double epsilonGreedy_beta, double epsilonGreedy_delta)
        {
            // setup ADP algorithm
            _parentEpidemic.SetUpADPAlgorithm(_modelSettings.ObjectiveFunction, _modelSettings.NumOfADPIterations, _modelSettings.NumOfSimRunsToBackPropogate,
                wtpForHealth, harmonicRule_a, epsilonGreedy_beta, epsilonGreedy_delta, true);
            // update initial coefficients of the Q-function
            _parentEpidemic.UpdateQFunctionCoefficients(_modelSettings.QFunctionCoefficientsInitialValues);
            // don't store trajectories while simulating
            _parentEpidemic.StoreEpidemicTrajectories = false;

            // specify rnd seeds for ADP algorithm
            switch (_modelSettings.SimulationRNDSeedsSource)
            {
                case enumSimulationRNDSeedsSource.StartFrom0:
                    break;
                case enumSimulationRNDSeedsSource.PrespecifiedSquence:
                    _parentEpidemic.SetUpADPRandomNumberSource(_modelSettings.RndSeeds);
                    break;
                case enumSimulationRNDSeedsSource.WeightedPrespecifiedSquence:
                    _parentEpidemic.SetUpADPRandomNumberSource(_modelSettings.RndSeeds, _modelSettings.RndSeedsGoodnessOfFit);
                    break;
            }
        }

        // ******** Subs to return some information about the model *******
        #region Subs to return some information about the model
        // get name of special statistics included in calibratoin 
        public string[] GetNamesOfSpecialStatisticsIncludedInCalibratoin()
        {
            // find the names of the parameters
            string[] names = new string[0];

            //if (_modelSettings.UseParallelComputing)
            //{
            //    // summation statistics
            //    foreach (SummationStatistics thisSumStat in _epidemics[0].SummationStatistics.Where(s => s.IfIncludedInCalibration))
            //        SupportFunctions.AddToEndOfArray(ref names, thisSumStat.Name);
            //    // ratio statistics
            //    foreach (RatioStatistics thisRatioStat in _epidemics[0].RatioStatistics.Where(s => s.IfIncludedInCalibration))
            //        SupportFunctions.AddToEndOfArray(ref names, thisRatioStat.Name);
            //}
            //else //(if using sequential runs
            //{
            // summation statistics
            foreach (SummationStatistics thisSumStat in _parentEpidemic.SummationStatistics.Where(s => s.IfIncludedInCalibration))
                SupportFunctions.AddToEndOfArray(ref names, thisSumStat.Name);
            // ratio statistics
            foreach (RatioStatistics thisRatioStat in _parentEpidemic.RatioStatistics.Where(s => s.IfIncludedInCalibration))
                SupportFunctions.AddToEndOfArray(ref names, thisRatioStat.Name);
            //}

            return names;
        }
        // get q-function polynomial terms
        public int[,] GetQFunctionPolynomialTerms()
        {
            return new int[0, 0];// _parentEpidemic.SimDecisionMaker.GetQFunctionPolynomialTerms();
        }
        #endregion

        // ******** Simulation result subs **********
        #region Simulation result subs
        // store the outcomes of this epidemic
        private void StoreOutcomesOfThisEpidemic(Epidemic thisEpidemic, int epidemicIndex)
        {
            // summary statistics
            _obsTotalQALY.Record(thisEpidemic.TotalQALY);
            _obsTotalCost.Record(thisEpidemic.TotalCost);
            _obsAnnualCost.Record(thisEpidemic.AnnualCost);
            _obsTotalNHB.Record(thisEpidemic.TotalNHB);
            _obsTotalNMB.Record(thisEpidemic.TotalNMB);
            _obsNumOfSwitchesBtwDecisions.Record(thisEpidemic.NumOfSwitchesBtwDecisions);
            _obsTimeUsedToSimulateATrajectory.Record(thisEpidemic.TimeUsedToSimulateOneTrajectory);

            int incidentStatIndex = 0, prevalenceStatIndex = 0, ratioStatIndex = 0;
            foreach (Class thisClass in thisEpidemic.Classes.Where(c => c.ShowStatisticsInSimulationResults))
            {
                if (thisClass.CollectNewMembersStatistics)
                    ((ObservationBasedStatistics)_incidenceStats[incidentStatIndex++]).Record(thisClass.AccumulatedNewMembers, epidemicIndex);
                if (thisClass.CollectMembersInClassStatistics)
                    ((ObservationBasedStatistics)_prevalenceStats[prevalenceStatIndex++]).Record(thisClass.AverageClassSize, epidemicIndex);
            }
            foreach (SummationStatistics thisSummationStatistics in thisEpidemic.SummationStatistics)
            {
                if (thisSummationStatistics.Type == APACE_lib.SummationStatistics.enumType.Incidence)
                    ((ObservationBasedStatistics)_incidenceStats[incidentStatIndex++]).Record(thisSummationStatistics.AccumulatedNewMembers, epidemicIndex);
                if (thisSummationStatistics.Type ==  APACE_lib.SummationStatistics.enumType.Prevalence)
                    ((ObservationBasedStatistics)_prevalenceStats[prevalenceStatIndex++]).Record(thisSummationStatistics.AveragePrevalence, epidemicIndex);
            }
            foreach (RatioStatistics thisRatioStatistics in thisEpidemic.RatioStatistics)
            {
                switch (thisRatioStatistics.Type)
                {
                    case APACE_lib.RatioStatistics.enumType.AccumulatedIncidenceOverAccumulatedIncidence:
                        ((ObservationBasedStatistics)_ratioStatistics[ratioStatIndex]).Record(thisRatioStatistics.CurrentValue, epidemicIndex);
                        break;
                    case APACE_lib.RatioStatistics.enumType.PrevalenceOverPrevalence:
                        ((ObservationBasedStatistics)_ratioStatistics[ratioStatIndex]).Record(thisRatioStatistics.Mean, epidemicIndex);
                        break;
                    case APACE_lib.RatioStatistics.enumType.IncidenceOverIncidence:
                        ((ObservationBasedStatistics)_ratioStatistics[ratioStatIndex]).Record(thisRatioStatistics.Mean, epidemicIndex);
                        break;
                }
                ++ratioStatIndex;
            }

            // sampled summary statistics  
            _arrSimItrs[epidemicIndex] = epidemicIndex;
            _arrRNGSeeds[epidemicIndex] = thisEpidemic.RNDSeedResultedInAnAcceptibleTrajectory;
            _arrNHB[epidemicIndex] = thisEpidemic.TotalNHB;
            _arrNMB[epidemicIndex] = thisEpidemic.TotalNMB;
            _arrSimulationQALY[epidemicIndex] = thisEpidemic.TotalQALY;
            _arrSimulationCost[epidemicIndex] = thisEpidemic.TotalCost;
            _arrSimulationAnnualCost[epidemicIndex] = thisEpidemic.AnnualCost;
        }  
        // get simulation iteration outcomes
        public void GetSimulationIterationOutcomes(ref string[] strIterationOutcomes, ref double[][] arrIterationOutcomes)
        {
            // header
            strIterationOutcomes = new string[4];
            strIterationOutcomes[0] = "RNG Seed";
            strIterationOutcomes[1] = "Health Measure";
            strIterationOutcomes[2] = "Total Cost";
            strIterationOutcomes[3] = "Annual Cost";

            foreach (ObservationBasedStatistics thisObs in _incidenceStats)
                SupportFunctions.AddToEndOfArray(ref strIterationOutcomes, thisObs.Name);

            foreach (ObservationBasedStatistics thisObs in _prevalenceStats)
                SupportFunctions.AddToEndOfArray(ref strIterationOutcomes, thisObs.Name);

            foreach (ObservationBasedStatistics thisObs in _ratioStatistics)
                SupportFunctions.AddToEndOfArray(ref strIterationOutcomes, thisObs.Name);

            //// headers
            //for (int j = 0; j < strClassAndSumStatistics.Length; j++)
            //    strIterationOutcomes[4 + j] = strClassAndSumStatistics[j];
            //for (int j = 0; j < strRatioStatistics.Length; j++)
            //    strIterationOutcomes[4 + strClassAndSumStatistics.Length + j] = strRatioStatistics[j];

            // observations
            arrIterationOutcomes = new double[_arrSimulationQALY.Length][];
            for (int i = 0; i < _arrSimulationQALY.Length; i++)
            {
                arrIterationOutcomes[i] = new double[strIterationOutcomes.Length];
                arrIterationOutcomes[i][0] = _arrRNGSeeds[i];
                arrIterationOutcomes[i][1] = _arrSimulationQALY[i];
                arrIterationOutcomes[i][2] = _arrSimulationCost[i];
                arrIterationOutcomes[i][3] = _arrSimulationAnnualCost[i];
            }
            int colIndex = 0;
            foreach (ObservationBasedStatistics thisObs in _incidenceStats)
            {
                for (int i = 0; i < _arrSimulationQALY.Length; i++)
                    arrIterationOutcomes[i][4 + colIndex] = thisObs.Observations[i];
                ++colIndex;
            }
            foreach (ObservationBasedStatistics thisObs in _prevalenceStats)
            {
                for (int i = 0; i < _arrSimulationQALY.Length; i++)
                    arrIterationOutcomes[i][4 + colIndex] = thisObs.Observations[i];
                ++colIndex;
            }
            foreach (ObservationBasedStatistics thisObs in _ratioStatistics)
            {
                for (int i = 0; i < _arrSimulationQALY.Length; i++)
                    arrIterationOutcomes[i][4 + colIndex] = thisObs.Observations[i];
                ++colIndex;
            }
        }
        // get simulation statistics
        public void GetSimulationStatistics(
            ref string[] strSummaryStatistics, ref  string[] strClassAndSumStatistics, 
            ref string[] strRatioStatistics, ref string[] strComputationStatistics, ref string[] strIterationOutcomes,
            ref double[,] arrSummaryStatistics, ref double[][] arrClassAndSumStatistics, 
            ref double[][] arrRatioStatistics, ref double[,] arrComputationStatistics, ref double[][] arrIterationOutcomes)
        {
            strSummaryStatistics = new string[6];
            strClassAndSumStatistics = new string[0];
            strRatioStatistics = new string[0];
            strComputationStatistics = new string[2];

            arrSummaryStatistics = new double[6, 3];
            arrClassAndSumStatistics = new double[0][];
            arrRatioStatistics = new double[0][];
            arrComputationStatistics = new double[2, 3];

            #region summary statistics
            strSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.TotalQALY - 1] = "Total discounted health measure";
            strSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.TotalCost - 1] = "Total discounted cost";
            strSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.AnnualCost - 1] = "Total annual cost";
            strSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.NHB - 1] = "Total discounted net health benefit";
            strSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.NMB - 1] = "Total discounted net monetary benefit";
            strSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.NumOfSwitches - 1] = "Number of switches between decisions";
            // Total QALY
            arrSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.TotalQALY - 1,
                (int)ExcelInterface.enumSimulationStatisticsColumns.Mean - 2] = _obsTotalQALY.Mean;
            arrSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.TotalQALY - 1,
                (int)ExcelInterface.enumSimulationStatisticsColumns.StDev - 2] = _obsTotalQALY.StDev;
            arrSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.TotalQALY - 1,
                (int)ExcelInterface.enumSimulationStatisticsColumns.StError - 2] = _obsTotalQALY.StErr;
            // Total cost
            arrSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.TotalCost - 1,
                (int)ExcelInterface.enumSimulationStatisticsColumns.Mean - 2] = _obsTotalCost.Mean;
            arrSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.TotalCost - 1,
                (int)ExcelInterface.enumSimulationStatisticsColumns.StDev - 2] = _obsTotalCost.StDev;
            arrSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.TotalCost - 1,
                (int)ExcelInterface.enumSimulationStatisticsColumns.StError - 2] = _obsTotalCost.StErr;
            // Annual cost
            arrSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.AnnualCost - 1,
                (int)ExcelInterface.enumSimulationStatisticsColumns.Mean - 2] = _obsAnnualCost.Mean;
            arrSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.AnnualCost - 1,
                (int)ExcelInterface.enumSimulationStatisticsColumns.StDev - 2] = _obsAnnualCost.StDev;
            arrSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.AnnualCost - 1,
                (int)ExcelInterface.enumSimulationStatisticsColumns.StError - 2] = _obsAnnualCost.StErr;
            // NHB
            arrSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.NHB - 1,
                (int)ExcelInterface.enumSimulationStatisticsColumns.Mean - 2] = _obsTotalNHB.Mean;
            arrSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.NHB - 1,
                (int)ExcelInterface.enumSimulationStatisticsColumns.StDev - 2] = _obsTotalNHB.StDev;
            arrSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.NHB - 1,
                (int)ExcelInterface.enumSimulationStatisticsColumns.StError - 2] = _obsTotalNHB.StErr;
            // NMB
            arrSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.NMB - 1,
                (int)ExcelInterface.enumSimulationStatisticsColumns.Mean - 2] = _obsTotalNMB.Mean;
            arrSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.NMB - 1,
                (int)ExcelInterface.enumSimulationStatisticsColumns.StDev - 2] = _obsTotalNMB.StDev;
            arrSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.NMB - 1,
                (int)ExcelInterface.enumSimulationStatisticsColumns.StError - 2] = _obsTotalNMB.StErr;
            // Number of switches
            arrSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.NumOfSwitches - 1,
                (int)ExcelInterface.enumSimulationStatisticsColumns.Mean - 2] = _obsNumOfSwitchesBtwDecisions.Mean;
            arrSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.NumOfSwitches - 1,
                (int)ExcelInterface.enumSimulationStatisticsColumns.StDev - 2] = _obsNumOfSwitchesBtwDecisions.StDev;
            arrSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.NumOfSwitches - 1,
                (int)ExcelInterface.enumSimulationStatisticsColumns.StError - 2] = _obsNumOfSwitchesBtwDecisions.StErr;
            #endregion

            #region class and summation statistics
            foreach (ObservationBasedStatistics thisObs in _incidenceStats)
            {                
                // name of this statistics
                SupportFunctions.AddToEndOfArray(ref strClassAndSumStatistics, thisObs.Name);
                double[][] thisStatValues = new double[1][];
                thisStatValues[0] = new double[3];
                // values of this statistics
                thisStatValues[0][0] = thisObs.Mean;
                thisStatValues[0][1] = thisObs.StDev;
                thisStatValues[0][2] = thisObs.StErr;
                // concatinate 
                arrClassAndSumStatistics = SupportFunctions.ConcatJaggedArray(arrClassAndSumStatistics, thisStatValues);
            }

            foreach (ObservationBasedStatistics thisObs in _prevalenceStats)
            {
                // name of this statistics
                SupportFunctions.AddToEndOfArray(ref strClassAndSumStatistics, thisObs.Name);
                double[][] thisStatValues = new double[1][];
                thisStatValues[0] = new double[3];
                // values of this statistics
                thisStatValues[0][0] = thisObs.Mean;
                thisStatValues[0][1] = thisObs.StDev;
                thisStatValues[0][2] = thisObs.StErr;
                // concatinate 
                arrClassAndSumStatistics = SupportFunctions.ConcatJaggedArray(arrClassAndSumStatistics, thisStatValues);
            }
            #endregion

            #region ratio statistics
            foreach (ObservationBasedStatistics thisObs in _ratioStatistics)
            {
                // name of this statistics
                SupportFunctions.AddToEndOfArray(ref strRatioStatistics, thisObs.Name);
                double[][] thisStatValues = new double[1][];
                thisStatValues[0] = new double[3];
                // values of this statistics
                thisStatValues[0][0] = thisObs.Mean;
                thisStatValues[0][1] = thisObs.StDev;
                thisStatValues[0][2] = thisObs.StErr;
                // concatinate 
                arrRatioStatistics = SupportFunctions.ConcatJaggedArray(arrRatioStatistics, thisStatValues);
            }
            #endregion

            #region simulation iteration outcomes
            GetSimulationIterationOutcomes(ref strIterationOutcomes, ref arrIterationOutcomes);
            #endregion

            #region computation statistics
            strComputationStatistics[0] = "Total simulation time (seconds)";
            strComputationStatistics[1] = "Simulation time of one trajectory (seconds)";
            arrComputationStatistics[0, 0] = _actualTimeUsedToSimulateAllTrajectories;
            arrComputationStatistics[1, 0] = _obsTimeUsedToSimulateATrajectory.Mean;
            arrComputationStatistics[1, 1] = _obsTimeUsedToSimulateATrajectory.StDev;
            arrComputationStatistics[1, 2] = _obsTimeUsedToSimulateATrajectory.StErr;

            #endregion            
        }
        // get objective function mean
        public double GetObjectiveFunction_Mean(enumObjectiveFunction objectiveFunction)
        {
            double mean = 0;
            switch (objectiveFunction)
            {
                case enumObjectiveFunction.MaximizeNHB:
                    mean = _obsTotalNHB.Mean;
                    break;
                case enumObjectiveFunction.MaximizeNMB:
                    mean = _obsTotalNMB.Mean;
                    break;
            }
            return mean;
        }
        public double GetObjectiveFunction_Mean(enumObjectiveFunction objectiveFunction, double wtpForHealth)
        {
            double mean = 0;
            switch (objectiveFunction)
            {
                case enumObjectiveFunction.MaximizeNHB:
                    mean = _obsTotalQALY.Mean - _obsTotalCost.Mean / wtpForHealth;
                    break;
                case enumObjectiveFunction.MaximizeNMB:
                    mean = wtpForHealth * _obsTotalQALY.Mean - _obsTotalCost.Mean;
                    break;
            }
            return mean;
        }
        // get objective function StDev
        public double GetObjectiveFunction_StDev(enumObjectiveFunction objectiveFunction)
        {
            double mean = 0;
            switch (objectiveFunction)
            {
                case enumObjectiveFunction.MaximizeNHB:
                    mean = _obsTotalNHB.StDev;
                    break;
                case enumObjectiveFunction.MaximizeNMB:
                    mean = _obsTotalNMB.StDev;
                    break;
            }
            return mean;
        }
        public double GetObjectiveFunction_StDev(enumObjectiveFunction objectiveFunction, double wtpForHealth)
        {
            double mean = 0;
            switch (objectiveFunction)
            {
                case enumObjectiveFunction.MaximizeNHB:
                    mean = _obsTotalQALY.StDev + _obsTotalCost.StDev / wtpForHealth;
                    break;
                case enumObjectiveFunction.MaximizeNMB:
                    mean = wtpForHealth * _obsTotalQALY.StDev + _obsTotalCost.StDev;
                    break;
            }
            return mean;
        }
        // get lower bound of objective function
        public double GetObjectiveFunction_LowerBound(enumObjectiveFunction objectiveFunction, double significanceLevel)
        {
            double lowerBound = 0;
            switch (objectiveFunction)
            {
                case enumObjectiveFunction.MaximizeNHB:
                    lowerBound = _obsTotalNHB.LBoundConfidenceInterval(significanceLevel);
                    break;
                case enumObjectiveFunction.MaximizeNMB:
                    lowerBound = _obsTotalNMB.LBoundConfidenceInterval(significanceLevel);
                    break;
            }
            return lowerBound;
        }
        public double GetObjectiveFunction_LowerBound(enumObjectiveFunction objectiveFunction, double wtpForHealth, double significanceLevel)
        {
            double lowerBound = 0;
            switch (objectiveFunction)
            {
                case enumObjectiveFunction.MaximizeNHB:
                    lowerBound = _obsTotalQALY.Mean - _obsTotalCost.Mean / wtpForHealth
                        - (_obsTotalQALY.HalfWidth(significanceLevel) + _obsTotalCost.HalfWidth(significanceLevel) / wtpForHealth);
                    break;
                case enumObjectiveFunction.MaximizeNMB:
                    lowerBound = wtpForHealth*_obsTotalQALY.Mean - _obsTotalCost.Mean
                        - (wtpForHealth * _obsTotalQALY.HalfWidth(significanceLevel) + _obsTotalCost.HalfWidth(significanceLevel));
                    break;
            }
            return lowerBound;
        }
        // get upper bound of objective function
        public double GetObjectiveFunction_UpperBound(enumObjectiveFunction objectiveFunction, double significanceLevel)
        {
            double upperBound = 0;
            switch (objectiveFunction)
            {
                case enumObjectiveFunction.MaximizeNHB:
                    upperBound = _obsTotalNHB.UBoundConfidenceInterval(significanceLevel);
                    break;
                case enumObjectiveFunction.MaximizeNMB:
                    upperBound = _obsTotalNMB.UBoundConfidenceInterval(significanceLevel);
                    break;
            }
            return upperBound;
        }
        public double GetObjectiveFunction_UpperBound(enumObjectiveFunction objectiveFunction, double wtpForHealth, double significanceLevel)
        {
            double upperBound = 0;
            switch (objectiveFunction)
            {
                case enumObjectiveFunction.MaximizeNHB:
                    upperBound = _obsTotalQALY.Mean - _obsTotalCost.Mean / wtpForHealth
                        + (_obsTotalQALY.HalfWidth(significanceLevel) + _obsTotalCost.HalfWidth(significanceLevel) / wtpForHealth);
                    break;
                case enumObjectiveFunction.MaximizeNMB:
                    upperBound = wtpForHealth * _obsTotalQALY.Mean - _obsTotalCost.Mean
                        + (wtpForHealth * _obsTotalQALY.HalfWidth(significanceLevel) + _obsTotalCost.HalfWidth(significanceLevel));
                    break;
            }
            return upperBound;
        }
        // get observations
        public double[,] GetEpidemicObservations()
        {
            return SupportFunctions.ConvertFromJaggedArrayToRegularArray(_parentEpidemic.SimulationObservableOutputs, _numOfMonitoredSimulationOutputs);
        }
        // get past actions
        //public int[] GetDecisionsOverPastObservationPeriods()
        //{
        //    return _parentEpidemic.DecisionsOverPastObservationPeriods;
        //}
        #endregion

        // ******* optimization result subs *********
        #region Optimization result subs
        // get  q-function coefficient estimates
        public double[] GetQFunctionCoefficientEstiamtes()
        {
            return _parentEpidemic.GetQFunctionCoefficientEstiamtes();
        }
        // get ADP iterations
        public double[,] GetADPIterations()
        {
            return _parentEpidemic.ArrADPIterationResults;
        }
        // get dynamic policy // only one dimensional
        public void GetOptimalDynamicPolicy(ref string featureName, ref double[] headers, ref int[] optimalDecisions,
            int numOfIntervalsToDescretizeFeatures)
        {
            _parentEpidemic.GetOptimalDynamicPolicy(ref featureName, ref headers, ref optimalDecisions, numOfIntervalsToDescretizeFeatures);
        }
        // get dynamic policy // only two dimensional
        public void GetOptimalDynamicPolicy(ref string[] strFeatureNames, ref double[][] headers, ref int[,] optimalDecisions,
            int numOfIntervalsToDescretizeFeatures)
        {
            _parentEpidemic.GetOptimalDynamicPolicy(ref strFeatureNames, ref headers, ref optimalDecisions, numOfIntervalsToDescretizeFeatures);
        }
        // get adaptive policy // two epidemilogical feature + 1 resource feature
        public void GetOptimalDynamicPolicy(ref string[] strFeatureNames, ref double[][] headers, ref int[][,] optimalDecisions,
            int numOfIntervalsToDescretizeFeatures)
        {
            _parentEpidemic.GetOptimalDynamicPolicy(ref strFeatureNames, ref headers, ref optimalDecisions, numOfIntervalsToDescretizeFeatures);
        }
        // get total simulation time for optimization
        public double GetTotalSimulationTimeToFindOneDynamicPolicy()
        {
            return _parentEpidemic.TotalSimulationTimeToFindOneDynamicPolicy;
        }
        // get optimization time
        public double GetTimeUsedToFindOneDynamicPolicy()
        {
            return _parentEpidemic.TimeUsedToFindOneDynamicPolicy;
        }
        #endregion

        // ********  Private Subs ******** 
        #region Private Subs
        // collect the information which becomes available after building the model
        //private void CollectInformationAvailableAfterModelIsBuilt()
        //{            
        //    if (_modelSettings.UseParallelComputing)
        //        CollectInformationAvailableAfterModelIsBuilt((Epidemic)_epidemics[0]);
        //    else
        //        CollectInformationAvailableAfterModelIsBuilt(_parentEpidemic);

        //    // find the names of parameters
        //    FindNamesOfParameters();
        //}
        // initialize simulation
        private void InitializeSimulation()
        {    
            // reset the rnd object
            
            if (_modelSettings.SimulationRNDSeedsSource == enumSimulationRNDSeedsSource.WeightedPrespecifiedSquence)
            {
                _sampledRNDSeeds = new int[_modelSettings.NumOfSimulationIterations];
                for (int i = 0; i< _modelSettings.NumOfSimulationIterations; i++)
                    _sampledRNDSeeds[i] = _arrRNGSeeds[_discreteDistributionOverSeeds.SampleDiscrete(_rng)];
            }

            // setup statistics collector
            SetupStatisticsCollectors();
            // reset simulation statistics
            ResetSimulationStatistics();            
        }
        // set up statistics collectors
        private void SetupStatisticsCollectors()
        {
            int numOfSimulationIterations = _modelSettings.NumOfSimulationIterations;
            _incidenceStats.Clear();
            _prevalenceStats.Clear();
            _ratioStatistics.Clear();
            _computationStatistics.Clear();

            string name;
            foreach (Class thisClass in GetClasses())
            {
                if (thisClass.CollectNewMembersStatistics)
                {
                    if (thisClass.ShowStatisticsInSimulationResults)
                    {
                        name = "Total: " + thisClass.Name;
                        _incidenceStats.Add(new ObservationBasedStatistics(name, numOfSimulationIterations));
                    }
                }
                if (thisClass.CollectMembersInClassStatistics)
                {
                    if (thisClass.ShowStatisticsInSimulationResults)
                    {
                         name = "Average size: " + thisClass.Name;
                        _prevalenceStats.Add(new ObservationBasedStatistics(name, numOfSimulationIterations));
                    }
                }
            }
            foreach (SummationStatistics thisSummationStatistics in GetSummationStatistics())
            {
                if (thisSummationStatistics.Type == APACE_lib.SummationStatistics.enumType.Incidence)
                {
                    name = "Total: " + thisSummationStatistics.Name;
                    _incidenceStats.Add(new ObservationBasedStatistics(name, numOfSimulationIterations));
                }
                else if (thisSummationStatistics.Type == APACE_lib.SummationStatistics.enumType.Prevalence)
                {
                    name = "Averge size: " + thisSummationStatistics.Name;
                    _prevalenceStats.Add(new ObservationBasedStatistics(name, numOfSimulationIterations));
                }
            }
            foreach (RatioStatistics thisRatioStatistics in GetRatioStatistics())
            {
                name = "Ratio Statistics: " + thisRatioStatistics.Name;
                _ratioStatistics.Add(new ObservationBasedStatistics(name, numOfSimulationIterations));
            }
            
            // reset the jagged array containing trajectories
            _pastActionCombinations = new int[0][];
            _simulationTimeBasedOutputs = new double[0][];
            _simulationIntervalBasedOutputs = new double[0][];
            _simulationObservableOutputs = new double[0][];
            _simulationResourcesOutputs = new double[0][];
            _timeOfSimulationObservableOutputs = new double[0][];
        }
        // reset simulation 
        private void ResetSimulationStatistics()
        {
            int numOfSimulationIterations = _modelSettings.NumOfSimulationIterations;

            // reset statistics
            _obsTotalCost.Reset();
            _obsTotalQALY.Reset();
            _obsTotalNMB.Reset();
            _obsTotalNHB.Reset();
            _obsNumOfSwitchesBtwDecisions.Reset();
            _obsTimeUsedToSimulateATrajectory.Reset();
            _actualTimeUsedToSimulateAllTrajectories = 0;

            // set up cost and QALY arrays
            _arrSimItrs = new int[numOfSimulationIterations];
            _arrRNGSeeds = new int[numOfSimulationIterations];
            _sampledParameterValues = new double[numOfSimulationIterations][];
            _arrSimulationQALY = new double[numOfSimulationIterations];
            _arrSimulationCost = new double[numOfSimulationIterations];
            _arrSimulationAnnualCost = new double[numOfSimulationIterations];
            _arrNHB= new double[numOfSimulationIterations];
            _arrNMB = new double[numOfSimulationIterations];

            // reset simulation statistics
            foreach (ObservationBasedStatistics thisObsStat in _incidenceStats)
                thisObsStat.Reset();
            foreach (ObservationBasedStatistics thisObsStat in _prevalenceStats)
                thisObsStat.Reset();
            foreach (ObservationBasedStatistics thisObsStat in _ratioStatistics)
                thisObsStat.Reset();
            foreach (ObservationBasedStatistics thisObsStat in _computationStatistics)
                thisObsStat.Reset();
        }
        // collect information available after model is built
        private void CollectInformationAvailableAfterModelIsBuilt(Epidemic thisEpidemic)
        {
            _numOfInterventions = thisEpidemic.NumOfInterventions;
            _numOfInterventionsAffectingContactPattern = thisEpidemic.NumOfInterventionsAffectingContactPattern;
            _numOfInterventionsControlledDynamically = thisEpidemic.NumOfInterventionsControlledDynamically;
            _storeEpidemicTrajectories = thisEpidemic.StoreEpidemicTrajectories;
            _numOfTimeBasedOutputsToReport = thisEpidemic.NumOfTimeBasedOutputsToReport;
            _numOfIntervalBasedOutputsToReport = thisEpidemic.NumOfIntervalBasedOutputsToReport;
            _numOfResourcesToReport = thisEpidemic.NumOfResourcesToReport;
            _numOfParametersToCalibrate = thisEpidemic.NumOfParametersToCalibrate;
            _numOfMonitoredSimulationOutputs = thisEpidemic.NumOfMonitoredSimulationOutputs;
            // calibration information            
            _numOfCalibratoinTargets= thisEpidemic.NumOfCalibratoinTargets;
            // decision names
            _interventionNames = new string[_numOfInterventions];
            _namesOfDefaultInterventionsAndThoseSpecifiedByDynamicRule = new string[0];
            foreach (Intervention thisIntervention in thisEpidemic.Interventions)
            {
                _interventionNames[thisIntervention.Index] = thisIntervention.Name;
                if (thisIntervention.DecisionRule is DecionRule_Dynamic
                    || thisIntervention.ActionType == EnumActionType.Default)
                    SupportFunctions.AddToEndOfArray(ref _namesOfDefaultInterventionsAndThoseSpecifiedByDynamicRule, thisIntervention.Name);
            }
        }
        // find the names of parameters
        private void FindNamesOfParameters()
        {
            // find the names of the parameters
            int i = 0;
            _namesOfParameters = new string[_parentEpidemic.Parameters.Count];
            foreach (Parameter thisParameter in _parentEpidemic.Parameters)
                _namesOfParameters[i++] = thisParameter.Name;
        }
        // find the names of parameters to calibrate
        private void FindNamesOfParametersToCalibrate()
        {
            // find the names of the parameters
            string[] namesOfParameters;

            int i = 0;
            namesOfParameters = new string[_parentEpidemic.Parameters.Where(p => p.IncludedInCalibration).Count()];
            foreach (Parameter thisParameter in _parentEpidemic.Parameters.Where(p => p.IncludedInCalibration))
                namesOfParameters[i++] = thisParameter.Name;

            _calibration.NamesOfParameters = (string[])namesOfParameters.Clone();
        }
        // get classes
        private List<Class> GetClasses()
        {
            return _parentEpidemic.Classes;
        }
        // get summation statistics
        private List<SummationStatistics> GetSummationStatistics()
        {
            return _parentEpidemic.SummationStatistics;
        }
        // get ratio statistics
        private List<RatioStatistics> GetRatioStatistics()
        {
            return _parentEpidemic.RatioStatistics;
        }

        // toggle modeller to different operation
        public void ToggleModellerTo(enumModelUse modelUse, enumDecisionRule decisionRule, bool reportEpidemicTrajectories, int warmUpPeriodIndex)
        {
            _storeEpidemicTrajectories = reportEpidemicTrajectories;            
            // toggle each epidemic
            if (_modelSettings.UseParallelComputing)
                foreach (Epidemic thisEpidemic in _epidemics)
                    ToggleAnEpidemicTo(thisEpidemic, modelUse, decisionRule, reportEpidemicTrajectories, warmUpPeriodIndex);
            else
                ToggleAnEpidemicTo(_parentEpidemic, modelUse, decisionRule, reportEpidemicTrajectories, warmUpPeriodIndex);
        }
        // toggle one epidemic
        private void ToggleAnEpidemicTo(Epidemic thisEpidemic, enumModelUse modelUse, enumDecisionRule decisionRule, bool storeEpidemicTrajectories, int warmUpPeriodIndex)
        {   
            thisEpidemic.ModelUse = modelUse;
            thisEpidemic.DecisionRule = decisionRule;
            thisEpidemic.StoreEpidemicTrajectories = storeEpidemicTrajectories;

            switch (modelUse)
            {
                case enumModelUse.Simulation:
                    {                        
                        if (decisionRule == enumDecisionRule.PredeterminedSequence)
                            thisEpidemic.AddInterventionHistory(_modelSettings.PrespecifiedSequenceOfInterventions);
                    }
                    break;
                case enumModelUse.Calibration:
                    #region Calibration
                    {
                        thisEpidemic.AddInterventionHistory(_modelSettings.PrespecifiedSequenceOfInterventions);
                        thisEpidemic.WarmUpPeriodIndex = warmUpPeriodIndex;
                    }
                    break;
                    #endregion
                case enumModelUse.Optimization:
                    {
                        thisEpidemic.StoreEpidemicTrajectories = false;
                    }
                    break;
            }
        }

        // find the RND seed for this iteration
        private int FindRNDSeed(int simItr)
        {
            int r = 0;
            switch (_modelSettings.SimulationRNDSeedsSource)
            {
                case enumSimulationRNDSeedsSource.StartFrom0:
                    r = _modelSettings.DistanceBtwRNGSeeds* simItr + _modelSettings.FirstRNGSeed;
                    break;
                case enumSimulationRNDSeedsSource.PrespecifiedSquence:
                    r = _modelSettings.RndSeeds[simItr];
                    break;
                case enumSimulationRNDSeedsSource.WeightedPrespecifiedSquence:
                    r = _sampledRNDSeeds[simItr];
                    break;
            }
            return r;
        }

    #endregion

}
}
