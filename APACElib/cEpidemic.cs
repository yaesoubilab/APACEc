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
        // Variable Definition 
        #region Variable Definition

        ModelSettings _set;
        public int ID { get; set; }
        private DecisionMaker _decisionMaker;
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();
        private List<Class> _classes = new List<Class>();
        private List<Event> _events = new List<Event>();
        private ArrayList _resources = new ArrayList();
        private ArrayList _resourceRules = new ArrayList();
        public List<SumTrajectory> SumTrajectories { get; set; } = new List<SumTrajectory>();
        public List<RatioTrajectory> RatioTrajectories { get; set; } = new List<RatioTrajectory>();
        public SimulationTrajectories EpidemicHistory { get; set; }

        //private List<SummationStatisticsOld> _summationStatistics = new List<SummationStatisticsOld>();
        //private List<RatioStatistics> _ratioStatistics = new List<RatioStatistics>();
        private ArrayList _features = new ArrayList();
        
        private MonitorOfInterventionsInEffect _monitorofInterventionsInEffect;
        private int[] _pathogenIDs;

        private int _numOfClasses;
        private int _numOfPathogens;

        // simulation setting
        RNG _rng;        
        private int _rndSeedResultedInAnAcceptibleTrajectory;
        private double[] _arrSampledParameterValues;
        private EnumModelUse _modelUse = EnumModelUse.Simulation;

        // simulation output settings      
        public bool StoreEpidemicTrajectories { get; set; } = true;
              
        private int _timeIndexOfTheFirstObservation;
        private bool _firstObservationObtained;

        private int[][] _pastActionCombinations;
        private double[][] _simulationTimeBasedOutputs;
        private double[][] _simulationIntervalBasedOutputs;
        private double[][] _simulationObservableOutputs;
        private double[][] _simulationResourceAvailabilityOutput;
        private double[][] _timesOfEpidemicObservationsOverPastObservationPeriods;
        private double[][] _calibrationObservation;

        // contact and transmission matrices
        private double[][,] _baseContactMatrices = null; //[pathogen ID][group i, group j] 
        private int[][][,] _percentChangeInContactMatricesParIDs = null; //[intervention ID][pathogen ID][group i, group j]
        private double[][][,] _contactMatrices = null; //[intervention ID][pathogen ID][group i, group j]
        private double[][][][] _tranmissionMatrices = null; // [intervention ID][pathogen ID][group i][group j]
        private int _numOfInterventionsAffectingContactPattern = 0;
        private int[] _indecesOfInterventionsAffectingContactPattern;
        private int[] _onOffStatusOfInterventionsAffectingContactPattern;
        // simulation
        private int _simTimeIndex;
        private int _epiTimeIndex;
        private bool _stoppedDueToEradication;
        private int[] _arrNumOfMembersInEachClass;
        // decision
        
        // dynamic policy
        private int _numOfFeatures;
        private bool _useEpidemicTimeAsFeature;
        private double[] _arrCurrentValuesOfFeatures = null;        
        // outcomes                
        double[] _arrSimulationObjectiveFunction;
        public EpidemicCostHealth EpidemicCostHealth { get; set; }

        // optimization        
        private int _adpSimItr; // the index of simulation runs that should be done before doing back-propagation
        private int[] _rndSeeds;
        Discrete _discreteDistOverSeeds;        
        private bool _storeADPIterationResults;
        private double[][] _arrADPIterationResults;
        // calibration
        private int _numOfCalibratoinTargets;
        private int _numOfDiscardedTrajectoriesAmongCalibrationRuns;
        private int _numOfParametersToCalibrate;        
        // computation time
        private double _timeUsedToSimulateOneTrajectory;
        // parameters
        private bool _thereAreTimeDependentParameters = false;
        private bool _thereAreTimeDependentParameters_affectingSusceptibilities = false;
        private bool _thereAreTimeDependentParameters_affectingInfectivities = false;
        private bool _thereAreTimeDependentParameters_affectingTranmissionDynamics = false;
        private bool _thereAreTimeDependentParameters_affectingNaturalHistoryRates = false;
        private bool _thereAreTimeDependentParameters_affectingSplittingClasses = false;

        #endregion

        // Instantiation
        public Epidemic(int id)
        {
            ID = id;
        }

        #region Properties

        public int CurrentEpidemicTimeIndex
        {
            get {return _epiTimeIndex;}
        }
        public double CurrentEpidemicTime
        {
            get { return _epiTimeIndex * _set.DeltaT; }
        }
       
        public int TimeIndexOfTheFirstObservation
        {
            get { return _timeIndexOfTheFirstObservation; }
        }       
   
        public bool IfStoppedDueToEradication
        {
            get { return _stoppedDueToEradication; }
        }
        public int NumOfInterventionsAffectingContactPattern
        {
            get { return _numOfInterventionsAffectingContactPattern; }
        }

        public EnumModelUse ModelUse
        {
            get { return _modelUse; }
            set { _modelUse = value; }
        }

        public int NumOfResourcesToReport
        {
            get { return _resources.Count; }
        }            
        public int[][] PastActionCombinations
        {
            get { return _pastActionCombinations; }
        }
        public double[][] SimulationTimeBasedOutputs
        {
            get { return _simulationTimeBasedOutputs; }
        }
        public double[][] SimulationIntervalBasedOutputs
        {
            get { return _simulationIntervalBasedOutputs; }
        }
        public double[][] SimulationObservableOutputs
        {
            get {return _simulationObservableOutputs; }
        }
        public double[][] CalibrationObservation
        {
            get { return _calibrationObservation; }
        }
        public double[][] SimulationResourceOutpus
        {
            get { return _simulationResourceAvailabilityOutput; }
        }
        public double[][] TimesOfEpidemicObservationsOverPastObservationPeriods
        {
            get { return _timesOfEpidemicObservationsOverPastObservationPeriods; }
        }
        
        public double[,] ArrADPIterationResults
        {
            get { return SupportFunctions.ConvertFromJaggedArrayToRegularArray(_arrADPIterationResults, 5); }
        }
            
        public int NumOfFeatures
        {
            get { return _features.Count; }
        }
        public int NumOfDiscardedTrajectoriesAmongCalibrationRuns
        {
            get { return _numOfDiscardedTrajectoriesAmongCalibrationRuns; }
        }
        public int NumOfParametersToCalibrate
        {
            get { return _numOfParametersToCalibrate; }
        }
        public int NumOfCalibratoinTargets
        { 
            get { return _numOfCalibratoinTargets; } 
        }
        public bool UseEpidemicTimeAsFeature
        {
            get { return _useEpidemicTimeAsFeature; }
        }
        public List<Class> Classes
        {
            get { return _classes; }
        }        
        public ArrayList Resources
        {
            get { return _resources; }
        }
        public ArrayList Features
        {
            get { return _features; }
        }
        public DecisionMaker DecisionMaker
        {
            get { return _decisionMaker; }
        }
        public int RNDSeedResultedInAnAcceptibleTrajectory
        {
            get { return _rndSeedResultedInAnAcceptibleTrajectory; }
        }
        public double[] arrSampledParameterValues
        { get { return _arrSampledParameterValues; } }


        // simulation run time
        public double TimeUsedToSimulateOneTrajectory
        {
            get { return _timeUsedToSimulateOneTrajectory; }
        }
        #endregion

        // clone
        //public Epidemic Clone(int ID)
        //{
        //    Epidemic clone = new Epidemic(ID);

        //    clone.ModelUse = _modelUse;
        //    clone.DecisionRule = _decisionRule;

        //    if (_modelUse == EnumModelUse.Calibration)
        //    {
        //        clone._observationMatrix = _observationMatrix;
        //    }

        //    return clone;
        //}

        // reset 
        private void Reset()
        {
            Parameters = new List<Parameter>();
            _classes = new List<Class>();
            _events = new List<Event>();
            _resources = new ArrayList();
            _resourceRules = new ArrayList();
            SumTrajectories = new List<SumTrajectory>();
            RatioTrajectories = new List<RatioTrajectory>();

            _features = new ArrayList();
            _decisionMaker.Reset();
    }
        // clean except the results
        public void CleanExceptResults()
        {
            _set = null;

            Reset();

            _arrSampledParameterValues = null;
            _baseContactMatrices = null; 
            _percentChangeInContactMatricesParIDs = null; 
            _contactMatrices = null; 
            _tranmissionMatrices = null; 
            _indecesOfInterventionsAffectingContactPattern = null;
            _onOffStatusOfInterventionsAffectingContactPattern = null;
             _arrCurrentValuesOfFeatures = null;
            _rndSeeds = null;
    }
        
        // Simulate one trajectory (parameters will be sampled)
        public void SimulateTrajectoriesUntilOneAcceptibleFound(int threadSpecificSeedNumber, int seedNumberToStopTryingFindingATrajectory, int simReplication, int timeIndexToStop)
        {
            // time keeping
            int startTime, endTime;
            startTime = Environment.TickCount;

            bool acceptableTrajectoryFound = false;
            _numOfDiscardedTrajectoriesAmongCalibrationRuns = 0;

            while (!acceptableTrajectoryFound && threadSpecificSeedNumber < seedNumberToStopTryingFindingATrajectory)
            {
                // reset for another simulation
                ResetForAnotherSimulation(threadSpecificSeedNumber);
                // simulate
                if (Simulate(simReplication, timeIndexToStop) == true)
                {
                    acceptableTrajectoryFound = true;
                    _rndSeedResultedInAnAcceptibleTrajectory = threadSpecificSeedNumber;
                }
                else
                {
                    ++threadSpecificSeedNumber;
                    // if the model is used for calibration, record the number of discarded trajectories due to violating the feasible ranges
                    if (_set.ModelUse == EnumModelUse.Calibration)
                        ++_numOfDiscardedTrajectoriesAmongCalibrationRuns;
                }
            }

            // simulation time
            endTime = Environment.TickCount;
            _timeUsedToSimulateOneTrajectory = (double)(endTime - startTime) / 1000;    
        }
        // Simulate one trajectory (parameters will be sampled)
        public void SimulateOneTrajectory(int threadSpecificSeedNumber, int simReplication, int timeIndexToStop)
        {
            int startTime, endTime;
            startTime = Environment.TickCount;
            _rndSeedResultedInAnAcceptibleTrajectory = -1;
            
            // reset for another simulation
            ResetForAnotherSimulation(threadSpecificSeedNumber);
            // simulate
            if (Simulate(simReplication, timeIndexToStop) == true)
                _rndSeedResultedInAnAcceptibleTrajectory = threadSpecificSeedNumber;

            // simulation time
            endTime = Environment.TickCount;
            _timeUsedToSimulateOneTrajectory = (double)(endTime - startTime) / 1000;
        }
        /// <summary>
        /// Simulate one trajectory (parameters should be assigned)
        /// </summary>
        /// <param name="threadSpecificSeedNumber"></param>
        /// <param name="parameterValues"></param>
        //public void SimulateOneTrajectory(int threadSpecificSeedNumber, int simReplication, int timeIndexToStop, double[] parameterValues)
        //{
        //    bool acceptableTrajectoryFound = false;
        //    while (!acceptableTrajectoryFound)
        //    {
        //        // reset for another simulation
        //        ResetForAnotherSimulation(threadSpecificSeedNumber, false, parameterValues);
        //        // simulate
        //        if (Simulate(simReplication, timeIndexToStop) == true)
        //        {
        //            acceptableTrajectoryFound = true;
        //            _rndSeedResultedInAnAcceptibleTrajectory = threadSpecificSeedNumber;
        //        }
        //        else
        //            ++threadSpecificSeedNumber;
        //    }
        //}
        // simulate one trajectory until the first time decisions should be made
        public void SimulateOneTrajectoryUntilTheFirstDecisionPoint(int threadSpecificSeedNumber)
        {
            // stop simulating this trajectory when it is time to make decisions
            bool acceptableTrajectoryFound = false;
            while (!acceptableTrajectoryFound)
            {
                // reset for another simulation
                ResetForAnotherSimulation(threadSpecificSeedNumber);
                // simulate until the first decision point
                if (Simulate(0, _set.EpidemicTimeIndexToStartDecisionMaking) == true)
                {
                    acceptableTrajectoryFound = true;
                    _rndSeedResultedInAnAcceptibleTrajectory = threadSpecificSeedNumber;
                }
                else
                    ++threadSpecificSeedNumber;
            }
        }
        // continue simulating current trajectory 
        public void ContinueSimulatingThisTrajectory(int additionalDeltaTs)
        {
            // find the stop time
            int timeIndexToStopSimulation = _simTimeIndex + additionalDeltaTs;
            // continue the simulation
            Simulate(0, timeIndexToStopSimulation);
        }
        // simulate N trajectories - this sub is exclusively for the ADP algorithm
        private void SimulateNTrajectories(int numOfSimulationIterations, int [] rndSeeds)
        {
            _arrSimulationObjectiveFunction = new double[numOfSimulationIterations];
            for (_adpSimItr = 0; _adpSimItr < numOfSimulationIterations; ++_adpSimItr)
            {
                // simulate one epidemic trajectory
                SimulateTrajectoriesUntilOneAcceptibleFound(rndSeeds[_adpSimItr], rndSeeds[_adpSimItr] + 1, _adpSimItr, _set.TimeIndexToStop);
                
                // store outcomes of this epidemic 
                _arrSimulationObjectiveFunction[_adpSimItr] = AccumulatedReward();
            }
        }
        private void SimulateNTrajectories(int numOfSimulationIterations, ref int seedNumber)
        {
            _arrSimulationObjectiveFunction = new double[numOfSimulationIterations];
            for (_adpSimItr = 0; _adpSimItr < numOfSimulationIterations; ++_adpSimItr)
            {
                // simulate one epidemic trajectory
                SimulateTrajectoriesUntilOneAcceptibleFound(seedNumber, seedNumber + 1, _adpSimItr, _set.TimeIndexToStop);
                seedNumber = _rndSeedResultedInAnAcceptibleTrajectory + 1;
                // store outcomes of this epidemic 
                _arrSimulationObjectiveFunction[_adpSimItr] = AccumulatedReward();
            }
        }

        // set up POMDP-ADP
        public void SetUpADPAlgorithm(EnumObjectiveFunction objectiveFunction,
            int numOfADPIterations, int numOfSimRunsToBackPropogate, 
            double wtpForHealth, double harmonicRule_a, double epsilonGreedy_beta, double epsilonGreedy_delta,
            bool storeADPIterationResults)
        {
            _modelUse = EnumModelUse.Optimization;         

            // ADP
            //_adpSimItr = 0;
            //_simDecisionMaker.SetupTraining(numOfSimRunsToBackPropogate);
            //_simDecisionMaker.SetupAHarmonicStepSizeRule(harmonicRule_a);
            //_simDecisionMaker.SetupAnEpsilonGreedyExplorationRule(epsilonGreedy_beta, epsilonGreedy_delta);
            //_simDecisionMaker.NumberOfADPIterations = numOfADPIterations;

            _storeADPIterationResults = storeADPIterationResults;
            if (_storeADPIterationResults)
                _arrADPIterationResults = new double[0][]; 
        }
        // set up the ADP random number source
        public void SetUpADPRandomNumberSource(int[] rndSeeds)
        {
            //_simulationRNDSeedsSource = EnumSimulationRNDSeedsSource.PrespecifiedSquence;
            _rndSeeds = (int[])rndSeeds.Clone();            
        }
        public void SetUpADPRandomNumberSource(int[] rndSeeds, double[] rndSeedsGoodnessOfFit)
        {
            //_simulationRNDSeedsSource = EnumSimulationRNDSeedsSource.WeightedPrespecifiedSquence;
            _rndSeeds = (int[])rndSeeds.Clone();

            double[] arrProb = new double[rndSeeds.Length];
            for (int i = 0; i < rndSeeds.Length; i++)
                arrProb[i] = 1 / rndSeedsGoodnessOfFit[i];

            // form a probability function over rnd seeds
            double sumInv = 1/arrProb.Sum();
            for (int i = 0; i < rndSeeds.Length; i++)
                arrProb[i] = arrProb[i] * sumInv;

            // define the sampling object
            _discreteDistOverSeeds = new Discrete("Discrete distribution over RND seeds", arrProb);            
        }
        // find the approximately optimal dynamic policy
        public void FindOptimalDynamicPolicy()
        {
            // sample rnd seeds if trajectories need to be sampled
            //int[,] sampledRNDseeds = new int[_simDecisionMaker.NumberOfADPIterations, _simDecisionMaker.NumOfSimRunsToBackPropogate];
            //if (_simulationRNDSeedsSource == enumSimulationRNDSeedsSource.WeightedPrespecifiedSquence)
            //{
            //    _rng = new RNG(0);
            //    for (int itr = 1; itr <= _simDecisionMaker.NumberOfADPIterations; ++itr)
            //        for (int j = 1; j <= _simDecisionMaker.NumOfSimRunsToBackPropogate; ++j)
            //            sampledRNDseeds[itr - 1, j - 1] = _rndSeeds[_discreteDistOverSeeds.SampleDiscrete(_rng)];
            //}

            //int rndSeedForThisItr = 0;
            //int[] rndSeeds = new int[_simDecisionMaker.NumOfSimRunsToBackPropogate];
            //int indexOfRNDSeeds = 0;
            //int adpItr = 1;
            //_storeEpidemicTrajectories = false;

            //int optStartTime, optEndTime;
            //optStartTime = Environment.TickCount;
            //_totalSimulationTimeToFindOneDynamicPolicy = 0;       

            //// optimize
            //for (int itr = 1; itr <= _simDecisionMaker.NumberOfADPIterations; ++itr)
            //{                
            //    // find the RND seed for this iteration
            //    switch (_simulationRNDSeedsSource)
            //    {
            //        case enumSimulationRNDSeedsSource.StartFrom0:
            //            break;
            //        case enumSimulationRNDSeedsSource.PrespecifiedSquence:
            //            {
            //                for (int j = 1; j <= _simDecisionMaker.NumOfSimRunsToBackPropogate; j++)
            //                {
            //                    if (indexOfRNDSeeds >= _rndSeeds.Length)
            //                        indexOfRNDSeeds = 0;
            //                    rndSeeds[j-1] = _rndSeeds[indexOfRNDSeeds++];
            //                }
            //            }
            //            break;
            //        case enumSimulationRNDSeedsSource.WeightedPrespecifiedSquence:
            //            {
            //                for (int j = 1; j <= _simDecisionMaker.NumOfSimRunsToBackPropogate; j++)
            //                    rndSeeds[j-1] = sampledRNDseeds[itr-1, j-1];
            //            }
            //            break;
            //    }

            //    // get ready for another ADP iteration
            //    GetReadyForAnotherADPIteration(itr);

            //    // simulate 
            //    int simStartTime, simEndTime;
            //    simStartTime = Environment.TickCount;
            //    switch (_simulationRNDSeedsSource)
            //    {
            //        case enumSimulationRNDSeedsSource.StartFrom0:
            //            SimulateNTrajectories(_simDecisionMaker.NumOfSimRunsToBackPropogate, ref rndSeedForThisItr);
            //            break;
            //        case enumSimulationRNDSeedsSource.PrespecifiedSquence:                        
            //        case enumSimulationRNDSeedsSource.WeightedPrespecifiedSquence:
            //            SimulateNTrajectories(_simDecisionMaker.NumOfSimRunsToBackPropogate, rndSeeds);
            //            break;
            //    }
                
            //    // accumulated simulation time
            //    simEndTime = Environment.TickCount;
            //    _totalSimulationTimeToFindOneDynamicPolicy += (double)(simEndTime - simStartTime) / 1000;

            //    // do backpropogation
            //    _simDecisionMaker.DoBackpropagation(itr, _discountRate, _stoppedDueToEradication, false);

            //    // store ADP iterations
            //    if (_modelUse == enumModelUse.Optimization && _storeADPIterationResults == true)
            //    {
            //        for (int dim = 0; dim < _simDecisionMaker.NumOfSimRunsToBackPropogate; ++dim)
            //        {
            //            if (_simDecisionMaker.BackPropagationResult(dim) == true)
            //            {
            //                double[][] thisADPIterationResult = new double[1][];
            //                thisADPIterationResult[0] = new double[5];
                            
            //                // iteration
            //                thisADPIterationResult[0][0] = adpItr;
            //                // reward
            //                thisADPIterationResult[0][1] = _arrSimulationObjectiveFunction[dim];
            //                // report errors
            //                thisADPIterationResult[0][2] = _simDecisionMaker.ADPPredictionErrors(dim, 0); //_POMDP_ADP.PredictionErrorForTheFirstEligibleADPState(dim);
            //                thisADPIterationResult[0][3] = _simDecisionMaker.ADPPredictionErrors(dim, (int)_simDecisionMaker.NumberOfValidADPStates(dim) / 2);
            //                thisADPIterationResult[0][4] = _simDecisionMaker.ADPPredictionErrors(dim, _simDecisionMaker.NumberOfValidADPStates(dim) - 1); //_POMDP_ADP.PredictionErrorForTheLastEligibleADPState(dim); //
            //                // concatenate 
            //                _arrADPIterationResults = SupportFunctions.ConcatJaggedArray(_arrADPIterationResults, thisADPIterationResult);
            //                ++adpItr;
            //            }
            //        }
            //    }
            //}
            //// optimization time
            //optEndTime = Environment.TickCount;
            //_timeUsedToFindOneDynamicPolicy = (double)(optEndTime - optStartTime) / 1000;

            //// reverse back the rnd seeds
            //if (_rndSeeds != null)
            //    _rndSeeds = _rndSeeds.Reverse().ToArray();
        }
        // get q-function coefficient estimates
        public double[] GetQFunctionCoefficientEstiamtes()
        {
            return new double[0];// _simDecisionMaker.GetQFunctionCoefficientEstimates();
        }
        // get dynamic policy // only one dimensional
        public void GetOptimalDynamicPolicy(ref string featureName, ref double[] headers, ref int[] optimalDecisions,
            int numOfIntervalsToDescretizeFeatures)
        {
            if (_features.Count > 1) return; // this procedure works when the feature set constrains 1 feature

            headers = new double[numOfIntervalsToDescretizeFeatures + 1];
            optimalDecisions = new int[numOfIntervalsToDescretizeFeatures + 1];

            // get the only feature
            Feature theOnlyFeature = (Feature)_features[0];

            // setup the interval length for each feature
            int featureIndex = theOnlyFeature.Index;
            double featureMin = theOnlyFeature.Min;
            double featureMax = theOnlyFeature.Max;
            double featureIntervalLength = (featureMax - featureMin) / numOfIntervalsToDescretizeFeatures;
            double featureNumOfBreakPoints = numOfIntervalsToDescretizeFeatures + 1;
            featureName = theOnlyFeature.Name;

            // reset available action combinations to the initial setting
            //_simDecisionMaker.MakeAllDynamicallyControlledActionsAvailable();
            // find the dynamic policy
            double featureValue = 0;
            for (int fIndex = 0; fIndex < featureNumOfBreakPoints; ++fIndex)
            {
                // value of the feature
                featureValue = featureMin + fIndex * featureIntervalLength;
                // header
                headers[fIndex] = featureValue;
                // make an array of feature
                double[] arrFeatureValue = new double[1];
                arrFeatureValue[0] = featureValue;
                // find the optimal decision
                //optimalDecisions[fIndex] = ComputationLib.SupportFunctions.ConvertToBase10FromBase2(
                //        _simDecisionMaker.FindTheOptimalDynamicallyControlledActionCombination(arrFeatureValue));//, _timeIndexToStartDecisionMaking));
            }
        }
        // get dynamic policy // only two dimensional
        public void GetOptimalDynamicPolicy(ref string[] strFeatureNames, ref double[][] headers, ref int[,] optimalDecisions,
            int numOfIntervalsToDescretizeFeatures)
        {       
            int numOfFeatures = _features.Count;            

            strFeatureNames = new string[numOfFeatures];
            headers = new double[2][];
            headers[0] = new double[numOfIntervalsToDescretizeFeatures + 1];
            headers[1] = new double[numOfIntervalsToDescretizeFeatures + 1];
            optimalDecisions = new int[numOfIntervalsToDescretizeFeatures + 1, numOfIntervalsToDescretizeFeatures + 1];

            // setup the interval length for each feature
            double[] arrFeatureMin = new double[numOfFeatures];
            double[] arrFeatureMax = new double[numOfFeatures];
            double[] arrFeatureIntervalLengths = new double[numOfFeatures];
            double[] arrFeatureNumOfBreakPoints = new double[numOfFeatures];

            int featureIndex;
            foreach (Feature thisFeature in _features)
            {
                featureIndex = thisFeature.Index;
                arrFeatureMin[featureIndex] = thisFeature.Min;
                arrFeatureMax[featureIndex] = thisFeature.Max;
                arrFeatureIntervalLengths[featureIndex] = (arrFeatureMax[featureIndex] - arrFeatureMin[featureIndex]) / numOfIntervalsToDescretizeFeatures;
                arrFeatureNumOfBreakPoints[featureIndex] = numOfIntervalsToDescretizeFeatures + 1;
            }
            // feature names
            foreach (Feature thisFeature in _features)
                strFeatureNames[thisFeature.Index] = thisFeature.Name;

            // reset available action combinations to the initial setting
            //_simDecisionMaker.MakeAllDynamicallyControlledActionsAvailable();

            // write the dynamic Policy
            double[] arrFeatureValues = new double[_features.Count];
            for (int f0Index = 0; f0Index < arrFeatureNumOfBreakPoints[0]; ++f0Index)
            {
                // feature 0 values
                arrFeatureValues[0] = arrFeatureMin[0] + f0Index * arrFeatureIntervalLengths[0];
                // header
                headers[0][f0Index] = arrFeatureValues[0];
                for (int f1Index = 0; f1Index < arrFeatureNumOfBreakPoints[1]; f1Index++)
                {
                    // feature 1 value
                    arrFeatureValues[1] = arrFeatureMin[1] + f1Index * arrFeatureIntervalLengths[1];
                    // header
                    headers[1][f1Index] = arrFeatureValues[1];
                    // optimal decision ID
                    //optimalDecisions[f0Index, f1Index] = ComputationLib.SupportFunctions.ConvertToBase10FromBase2(
                    //    _simDecisionMaker.FindTheOptimalDynamicallyControlledActionCombination(arrFeatureValues));
                }
            }
        }
        // get dynamic policy //  two dimensional + 1 resource feature
        public void GetOptimalDynamicPolicy(ref string[] strFeatureNames, ref double[][] headers, ref int[][,] optimalDecisions, int numOfIntervalsToDescretizeFeatures)
        {
            int numOfFeatures = _features.Count;

            strFeatureNames = new string[numOfFeatures];
            headers = new double[3][];
            headers[0] = new double[numOfIntervalsToDescretizeFeatures + 1];
            headers[1] = new double[numOfIntervalsToDescretizeFeatures + 1];
            headers[2] = new double[numOfIntervalsToDescretizeFeatures + 1];
            optimalDecisions = new int[numOfIntervalsToDescretizeFeatures + 1][,];
            //                [numOfIntervalsToDescretizeEpidemicFeatures + 1, numOfIntervalsToDescretizeEpidemicFeatures + 1];

            // setup the interval length for each feature
            double[] arrFeatureMin = new double[numOfFeatures];
            double[] arrFeatureMax = new double[numOfFeatures];
            double[] arrFeatureIntervalLengths = new double[numOfFeatures];
            double[] arrFeatureNumOfBreakPoints = new double[numOfFeatures];

            int featureIndex;
            foreach (Feature thisFeature in _features)
            {
                featureIndex = thisFeature.Index;
                arrFeatureMin[featureIndex] = thisFeature.Min;
                arrFeatureMax[featureIndex] = thisFeature.Max;
                arrFeatureIntervalLengths[featureIndex] = (arrFeatureMax[featureIndex] - arrFeatureMin[featureIndex]) / numOfIntervalsToDescretizeFeatures;
                arrFeatureNumOfBreakPoints[featureIndex] = numOfIntervalsToDescretizeFeatures + 1;
            }
            // feature names
            foreach (Feature thisFeature in _features)
                strFeatureNames[thisFeature.Index] = thisFeature.Name;

            // reset available action combinations to the initial setting
            //_simDecisionMaker.MakeAllDynamicallyControlledActionsAvailable();

            // write the dynamic Policy
            double[] arrFeatureValues = new double[_features.Count];
            for (int fResourceIndex = 0; fResourceIndex < arrFeatureNumOfBreakPoints[0]; ++fResourceIndex)
            {
                optimalDecisions[fResourceIndex] = new int[numOfIntervalsToDescretizeFeatures + 1, numOfIntervalsToDescretizeFeatures + 1];

                // feature 0 (resource) values
                arrFeatureValues[0] = arrFeatureMin[0] + fResourceIndex * arrFeatureIntervalLengths[0];
                // header
                headers[0][fResourceIndex] = arrFeatureValues[0];
                for (int f1Index = 0; f1Index < arrFeatureNumOfBreakPoints[1]; f1Index++)
                {
                    // feature 1 values
                    arrFeatureValues[1] = arrFeatureMin[1] + f1Index * arrFeatureIntervalLengths[1];
                    // header
                    headers[1][f1Index] = arrFeatureValues[1];
                    for (int f2Index = 0; f2Index < arrFeatureNumOfBreakPoints[2]; f2Index++)
                    {
                        // feature 1 value
                        arrFeatureValues[2] = arrFeatureMin[2] + f2Index * arrFeatureIntervalLengths[2];
                        // header
                        headers[2][f2Index] = arrFeatureValues[2];
                        // optimal decision ID
                        //optimalDecisions[fResourceIndex][f1Index, f2Index] =
                        //    ComputationLib.SupportFunctions.ConvertToBase10FromBase2(_simDecisionMaker.FindTheOptimalDynamicallyControlledActionCombination(arrFeatureValues));
                    }
                }
            }
        }
        // switch off all interventions controlled by decision rule
        //public void SwitchOffAllInterventionsControlledByDecisionRule()
        //{
        //    int[] interventionCombination = new int[_simDecisionMaker.NumOfActions];

        //    foreach (Intervention thisIntervention in _simDecisionMaker.Actions)
        //    {
        //        if (thisIntervention.Type == SimulationLib.Intervention.enumActionType.Default)
        //            interventionCombination[thisIntervention.ID] = 1;
        //        else if (thisIntervention.OnOffSwitchSetting == SimulationLib.Intervention.enumOnOffSwitchSetting.Dynamic)
        //            interventionCombination[thisIntervention.ID] = 0;
        //        else if (thisIntervention.OnOffSwitchSetting == SimulationLib.Intervention.enumOnOffSwitchSetting.Predetermined)
        //            interventionCombination[thisIntervention.ID] = (int)thisIntervention.PredeterminedSwitchValue;
        //    }

        //    //_POMDP_ADP.DefaultActionCombination(interventionCombination);
        //}

        // get the value of parameters to calibrate
        public double[] GetValuesOfParametersToCalibrate()
        {
            double[] parValues = new double[_numOfParametersToCalibrate];
            int i = 0;
            foreach (Parameter thisParameter in Parameters.Where(p => p.IncludedInCalibration == true))
                parValues[i++] =  thisParameter.Value;

            return (double[])parValues.Clone();
        }

        // get possible designs for interval-based policies
        public ArrayList GetIntervalBasedStaticPoliciesDesigns()
        {
            //TODO: update this procedure based on the latest changes to POMDP_ADP 

            ArrayList designs = new ArrayList();
            int numOfActions = _decisionMaker.NumOfInterventions;

            // add design for when all interventions are off (if allowed)
            //designs.Add(new IntervalBasedStaticPolicy(0,
              //  GetIntervetionCombinationCodeWithAllSpecifiedByDecisionRuleInterventionTurnedOff(), new double[numOfActions], new int[numOfActions]));

            // for each possible combination 
            int designID = 1;
            for (int decisionsCode = 0; decisionsCode < Math.Pow(2, numOfActions); decisionsCode++)
            {
                // find a combination
                int[] interventionCombination = new int[0];// _simDecisionMaker.DynamicallyControlledActionCombinations[decisionsCode];
                // see if this is a feasible combination
                bool feasible = true;
                foreach (Intervention thisIntervention in _decisionMaker.Interventions)
                {
                    int index = thisIntervention.Index;
                    // if this intervention is default, it can't be turned off
                    if (thisIntervention.Type == EnumInterventionType.Default)
                    {
                        if (interventionCombination[index] == 0)
                            feasible = false;
                    }
                    else if (thisIntervention.DecisionRule is DecionRule_Predetermined)
                    {
                        if (interventionCombination[index] != _decisionMaker.DefaultDecision[index])
                            feasible = false;
                    }
                    else if (thisIntervention.DecisionRule is DecionRule_IntervalBased && interventionCombination[index] == 0)
                        feasible = false;                    

                    if (feasible == false)
                        break;
                }
                // if not feasible break the loop
                if (feasible == true)
                {
                    // find the design for the interval-based policy (ASSUMING that only one intervention can be used by the interval-based policy)
                    int interventionID = 0;
                    double startTime = _set.EpidemicTimeIndexToStartDecisionMaking * _set.DeltaT;
                    int numOfDecisionIntervals = 0;
                    double lastTimeToUseThisIntervention = 0;
                    int minNumOfDecisionPeriodsToUse = 0;
                    double[] interventionStartTimes = new double[numOfActions];
                    int[] numOfDecisionPeriodsToUse = new int[numOfActions];

                    foreach (Intervention thisIntervention in _decisionMaker.Interventions)
                        if (thisIntervention.DecisionRule is DecionRule_IntervalBased)
                        {
                            interventionID = thisIntervention.Index;
                            //lastTimeToUseThisIntervention = thisIntervention.IntervalBaseEmployment_availableUntilThisTime;
                            //minNumOfDecisionPeriodsToUse = thisIntervention.IntervalBaseEmployment_minNumOfDecisionPeriodsToUse;
                        }

                    // interval-based parameters
                    while (startTime < lastTimeToUseThisIntervention)
                    {
                        numOfDecisionIntervals = minNumOfDecisionPeriodsToUse;
                        while (numOfDecisionIntervals * _set.NumOfDeltaT_inDecisionInterval*_set.DeltaT<= lastTimeToUseThisIntervention - startTime)
                        {
                            // design
                            interventionStartTimes[interventionID] = startTime;
                            numOfDecisionPeriodsToUse[interventionID] = numOfDecisionIntervals;
                            // add design
                            designs.Add(new IntervalBasedStaticPolicy(designID++, decisionsCode, interventionStartTimes, numOfDecisionPeriodsToUse));

                            numOfDecisionIntervals += minNumOfDecisionPeriodsToUse;
                        }
                        startTime += minNumOfDecisionPeriodsToUse * _set.NumOfDeltaT_inDecisionInterval * _set.DeltaT;
                    }
                }
            }            
                        
            return designs;
        }
        // get intervention combination for always on/off static policies with all interventions off expect for those that are always on
        //public int GetIntervetionCombinationCodeWithAllSpecifiedByDecisionRuleInterventionTurnedOff()
        //{
        //    int[] thisCombination = (int[])_simDecisionMaker.DefaultActionCombination.Clone();
        //    foreach (Intervention thisIntervention in _simDecisionMaker.Actions)
        //    {
        //        if (thisIntervention.Type == SimulationLib.Intervention.enumActionType.Default)
        //            thisCombination[thisIntervention.ID] = 1;
        //        if (thisIntervention.OnOffSwitchSetting == SimulationLib.Intervention.enumOnOffSwitchSetting.IntervalBased ||
        //            thisIntervention.OnOffSwitchSetting == SimulationLib.Intervention.enumOnOffSwitchSetting.ThresholdBased||
        //            thisIntervention.OnOffSwitchSetting == SimulationLib.Intervention.enumOnOffSwitchSetting.Periodic ||
        //            thisIntervention.OnOffSwitchSetting == SimulationLib.Intervention.enumOnOffSwitchSetting.Dynamic)
        //            thisCombination[thisIntervention.ID] = 0;
        //    }
        //    return SupportFunctions.ConvertToBase10FromBase2(thisCombination);
        //}        

        // ********* private subs to run the simulation model *************
        #region private subs to run the simulation model
        
            // simulate the trajectory assuming that parameter values are already assigned
        private bool Simulate(int simReplication, int timeIndexToStop)
        {
            bool toStop;
            bool acceptableTrajectory = false;
            bool ifThisIsAFeasibleCalibrationTrajectory = true;        

            // simulate the epidemic
            toStop = false;
            while (!toStop)
            {
                // make decisions if decision is not predetermined and announce the new decisions (may not necessarily go into effect)
                _monitorofInterventionsInEffect.Update(_epiTimeIndex, false, ref _classes);

                // update the effect of chance in time dependent parameter value
                UpdateTheEffectOfChangeInTimeDependentParameterValues(_simTimeIndex * _set.DeltaT);

                // update recorded trajectories 
                EpidemicHistory.Record(_simTimeIndex);

                // check if this is has been a feasible trajectory for calibration
                if (_modelUse == EnumModelUse.Calibration && !ifThisIsAFeasibleCalibrationTrajectory)
                {
                    acceptableTrajectory = false;
                    return acceptableTrajectory;
                }

                // update transmission rates
                UpdateTransmissionRates();

                // send transfer class members                    
                TransferClassMembers();

                // advance time  
                _simTimeIndex += 1;
                UpdateCurrentEpidemicTimeIndex();

                // if optimizing, update the cost of current decision period
                if (_modelUse == EnumModelUse.Optimization)
                    UpdateRewardOfCurrentADPStateDecision();                

                // check if stopping rules are satisfied 
                if (_epiTimeIndex >= timeIndexToStop || IsEradicationConditionsSatisfied() == true)
                {
                    toStop = true;
                    // update recorded trajectories 
                    EpidemicHistory.Record(_simTimeIndex);

                    // find if it is an acceptable trajectory
                    acceptableTrajectory = true;
                    if (_epiTimeIndex < _set.EpidemicConditionTimeIndex)
                        acceptableTrajectory = false;
                }
            } // end while (!toStop)
            return acceptableTrajectory;
        }

        //// Update the resource status
        //private void CheckIfResourcesHaveBecomeAvailable()
        //{
        //    _aResourceJustReplinished = false;

        //    foreach (Resource thisResource in _resources)
        //    {
        //        thisResource.ReplenishIfAvailable(_currentEpidemicTimeIndex * _set.DeltaT);
        //        if (_arrAvailableResources[thisResource.ID] != thisResource.CurrentUnitsAvailable)
        //        {
        //            _arrAvailableResources[thisResource.ID] = thisResource.CurrentUnitsAvailable;
        //            _aResourceJustReplinished = true;
        //        }
        //    }
        //    // update the available resources to other classes
        //    if (_aResourceJustReplinished)
        //        UpdateResourceAvailabilityInformationForEachClass(_arrAvailableResources);
        //}
        //// update resources
        //private void UpdateResourceAvailabilityBasedOnConsumptionOfThisClass(int[] arrResourcesConsumed)
        //{
        //    if (arrResourcesConsumed.Length == 0 || arrResourcesConsumed.Sum() == 0)
        //        return;

        //    foreach (Resource thisResource in _resources)
        //    {
        //        thisResource.CurrentUnitsAvailable -= arrResourcesConsumed[thisResource.ID];
        //        _arrAvailableResources[thisResource.ID] = thisResource.CurrentUnitsAvailable;
        //    }

        //    // update the available resources to other classes
        //    UpdateResourceAvailabilityInformationForEachClass(_arrAvailableResources);
        //}
        //// update the information of resource availability for each resource-contained class 
        //private void UpdateResourceAvailabilityInformationForEachClass(int[] arrAvailableResources)
        //{            
        //    foreach (Class thisClass in _classes)
        //        thisClass.UpdateAvailableResources(arrAvailableResources);         
        //}

        // read the feature values

       
      
        
        // find the index of this intervention combination in transmission matrix
        private int FindIndexOfInterventionCombimbinationInTransmissionMatrix(int[] interventionCombination)
        {
            for (int i = 0; i < _numOfInterventionsAffectingContactPattern; i++)
            {
                _onOffStatusOfInterventionsAffectingContactPattern[i] = interventionCombination[_indecesOfInterventionsAffectingContactPattern[i]];
            }
            return SupportFunctions.ConvertToBase10FromBase2(_onOffStatusOfInterventionsAffectingContactPattern);
        }
        // find the index of this intervention combination in contact matrices
        private int FindIndexOfInterventionCombimbinationInContactMatrices(int[] interventionCombination)
        {
            for (int i = 0; i < _numOfInterventionsAffectingContactPattern; i++)
            {
                _onOffStatusOfInterventionsAffectingContactPattern[i] = interventionCombination[_indecesOfInterventionsAffectingContactPattern[i]];
            }
            return SupportFunctions.ConvertToBase10FromBase2(_onOffStatusOfInterventionsAffectingContactPattern);
        }
        
        
        // transfer class members        
        private void TransferClassMembers()
        {
            // reset number of new members over past period for all classes
            foreach (Class thisClass in _classes)
                thisClass.ClassStat.NumOfNewMembersOverPastPeriod = 0;

            // do the transfer on all members
            foreach (Class thisClass in _classes.Where(c => c.ClassStat.Prevalence>0))
                thisClass.IfNeedsToBeProcessed = true;

            bool thereAreClassesToBeProcessed= true;
            while (thereAreClassesToBeProcessed)
            {
                // if members are waiting
                foreach (Class thisClass in _classes.Where(c => c.IfNeedsToBeProcessed))
                {                    
                    // calculate the number of members to be sent out from each class
                    thisClass.SendOutMembers(_set.DeltaT, _rng);
                    // all departing members are processed
                    thisClass.IfNeedsToBeProcessed = false;
                }

                // receive members
                foreach (Class thisSendingOutClass in _classes.Where(c => c.IfMembersWaitingToSendOutBeforeNextDeltaT))
                {
                    for (int j = 0; j < thisSendingOutClass.ArrDestinationClasseIDs.Length; j++)
                        _classes[thisSendingOutClass.ArrDestinationClasseIDs[j]].AddNewMembers(thisSendingOutClass.ArrNumOfMembersSendingToEachDestinationClasses[j]);
                    // reset number of members sending out to each destination class
                    thisSendingOutClass.ResetNumOfMembersSendingToEachDestinationClasses();
                }

                // check if there are members waiting to be sent out
                thereAreClassesToBeProcessed = false;
                for (int i = _numOfClasses - 1; i >= 0; i--)
                    if (_classes[i].IfNeedsToBeProcessed)
                    {
                        thereAreClassesToBeProcessed = true;
                        break;
                    }
            } // end of while (membersWaitingToBeTransferred)

            foreach (Class thisClass in _classes)
                _arrNumOfMembersInEachClass[thisClass.ID] = thisClass.ClassStat.Prevalence;

            // update class statistics                      
            foreach (Class thisClass in _classes)
            {
                thisClass.ClassStat.CollectEndOfDeltaTStats(_simTimeIndex);
                if (_modelUse != EnumModelUse.Calibration)
                {
                    EpidemicCostHealth.Add(
                        _simTimeIndex,
                        thisClass.ClassStat.DeltaCostHealthCollector.DeltaTCost,
                        thisClass.ClassStat.DeltaCostHealthCollector.DeltaTDALY);
                }
            }

            // update summation statistics
            foreach (SumTrajectory thisSumTaj in SumTrajectories)
            {
                if (thisSumTaj is SumClassesTrajectory)
                    ((SumClassesTrajectory)thisSumTaj).Add(_simTimeIndex, ref _classes);
                else
                    ((SumEventTrajectory)thisSumTaj).Add(_simTimeIndex, ref _events);
            }

            // update the aggregated trajectories
            EpidemicHistory.Record(_simTimeIndex);
            
            // reset number of members out of active events for all classes
            foreach (Class thisClass in _classes)
                thisClass.ResetNumOfMembersOutOfEventsOverPastDeltaT();

            // update decision costs
            if (_modelUse != EnumModelUse.Calibration)
            {
                EpidemicCostHealth.Add(_simTimeIndex, _decisionMaker.CostOverThisDecisionPeriod, 0);
                _decisionMaker.CostOverThisDecisionPeriod = 0;
            }

        }
        
        // update the cost of current ADP state-decision
        private void UpdateRewardOfCurrentADPStateDecision()
        {
            //// add reward
            //int numOfADPStates = _decisionMaker.NumberOfADPStates(_adpSimItr);
            //if (numOfADPStates > 0)
            //    _decisionMaker.AddToDecisionIntervalReward(_adpSimItr, numOfADPStates - 1, CurrentDeltaTReward());
            //((ADP_State)_POMDP_ADP.ADPStates[numOfADPStates - 1]).AddToDecisionIntervalReward();
        }

        // return accumulated reward
        private double AccumulatedReward()
        {
            double reward = 0;
            switch (_set.ObjectiveFunction)
            {
                case EnumObjectiveFunction.MaximizeNMB:
                    reward = EpidemicCostHealth.GetDiscountedNMB(_set.WTPForHealth);
                    break;
                case EnumObjectiveFunction.MaximizeNHB:
                    reward = EpidemicCostHealth.GetDiscountedNHB(_set.WTPForHealth);
                    break;
            }
            return reward;
        }
        
        // check if stopping condition is satisfied
        private bool IsEradicationConditionsSatisfied()
        {
            bool eradicated = true;

            // check if any class has eradication condition
            if (_classes.Where(s => s.EmptyToEradicate).Count() == 0)
                eradicated = false;
            else
            {
                foreach (Class thisClass in _classes)
                {
                    // if a class should be empty while it is not then return false
                    if (thisClass.EmptyToEradicate == true && thisClass.ClassStat.Prevalence > 0)
                    {
                        eradicated = false;
                        break;
                    }
                }
            }
            _stoppedDueToEradication = eradicated;
            return eradicated;
        }
        // reset for another simulation
        private void ResetForAnotherSimulation(int seed)
        {
            ResetForAnotherSimulation(seed, true, new double[0]);
        }        
        private void ResetForAnotherSimulation(int seed, bool sampleParameters, double[] parameterValues)
        {
            // reset the rnd object
            _rng = new RNG(seed);

            // reset time
            _timeIndexOfTheFirstObservation = 0;
            _firstObservationObtained = false;
            _simTimeIndex = 0;
            // epidemic start time
            UpdateCurrentEpidemicTimeIndex();

            // sample from parameters
            _arrSampledParameterValues = new double[Parameters.Count];
            if (sampleParameters == true)
                SampleFromParameters(0, false);
            else
                _arrSampledParameterValues = parameterValues;

            // update intervention information
            _onOffStatusOfInterventionsAffectingContactPattern = new int[_numOfInterventionsAffectingContactPattern];
            foreach (Intervention thisIntervention in _decisionMaker.Interventions)
                thisIntervention.NumOfTimeIndeciesDelayedToGoIntoEffectOnceTurnedOn = (int)(_arrSampledParameterValues[thisIntervention.ParIDDelayToGoIntoEffectOnceTurnedOn] / _set.DeltaT);

            // reset the number of people in each compartment
            _arrNumOfMembersInEachClass = new int[_numOfClasses];
            foreach (Class thisClass in _classes)
            {
                thisClass.UpdateInitialNumberOfMembers((int)Math.Round(_arrSampledParameterValues[thisClass.InitialMemebersParID]));
                _arrNumOfMembersInEachClass[thisClass.ID] = thisClass.ClassStat.Prevalence;
            }

            // reset epidemic history 
            EpidemicHistory.Reset();

            // health and cost outcomes
            EpidemicCostHealth = new EpidemicCostHealth(_set.DeltaTDiscountRate, _set.WarmUpPeriodTimeIndex);
            EpidemicCostHealth.Reset();

            // reset decisions
            _decisionMaker.Reset();//(ref _totalCost);

            // update decisions
            _monitorofInterventionsInEffect.Update(0, true, ref _classes);

            // calculate contact matrices
            CalculateContactMatrices();

            // update susceptibility and infectivity of classes
            UpdateSusceptilityAndInfectivityOfClasses(true, true);

            // update rates associated with each class and their initial size
            foreach (Class thisClass in _classes)
            {
                thisClass.UpdateRatesOfBirthAndEpiIndpEvents(_arrSampledParameterValues);
                thisClass.UpdateProbOfSuccess(_arrSampledParameterValues);
                thisClass.Reset();
            }

           
            // reset features
            _arrCurrentValuesOfFeatures = new double[_features.Count];
            
            // reset the jagged array containing trajectories
            _pastActionCombinations = new int[0][];
            _simulationTimeBasedOutputs = new double[0][];
            _simulationIntervalBasedOutputs = new double[0][]; 
            _simulationObservableOutputs = new double[0][];
            _calibrationObservation = new double[0][];
            _simulationResourceAvailabilityOutput = new double[0][];
            _timesOfEpidemicObservationsOverPastObservationPeriods = new double[0][];
        }

        // Sample this parameter
        private void SampleThisParameter(Parameter thisPar, double time)
        {
            switch (thisPar.Type)
            {
                // independent parameter
                case Parameter.EnumType.Independet:
                    {
                        _arrSampledParameterValues[thisPar.ID] = ((IndependetParameter)thisPar).Sample(_rng);
                    }
                    break;
                
                // correlated parameter
                case Parameter.EnumType.Correlated:
                    {
                        CorrelatedParameter thisCorrelatedParameter = thisPar as CorrelatedParameter;
                        int parameterIDCorrelatedTo = thisCorrelatedParameter.IDOfDepedentPar;
                        double valueOfTheParameterIDCorrelatedTo = _arrSampledParameterValues[parameterIDCorrelatedTo];
                        _arrSampledParameterValues[thisPar.ID] = thisCorrelatedParameter.Sample(valueOfTheParameterIDCorrelatedTo);
                    }
                    break;
                
                // multiplicative parameter
                case Parameter.EnumType.Multiplicative:
                    {
                        MultiplicativeParameter thisMultiplicativeParameter = thisPar as MultiplicativeParameter;
                        int firstParID = thisMultiplicativeParameter.FirstParameterID;
                        int secondParID = thisMultiplicativeParameter.SecondParameterID;
                        _arrSampledParameterValues[thisPar.ID] = thisMultiplicativeParameter.Sample(_arrSampledParameterValues[firstParID], _arrSampledParameterValues[secondParID]);
                    }
                    break;

                // linear combination parameter
                case  Parameter.EnumType.LinearCombination:
                    {
                        LinearCombination thisLinearCombinationPar = thisPar as LinearCombination;
                        int[] arrParIDs = thisLinearCombinationPar.arrParIDs;
                        double[] arrValueOfParameters = new double[arrParIDs.Length];

                        for (int i = 0; i < arrParIDs.Length; i++)
                            arrValueOfParameters[i] = _arrSampledParameterValues[arrParIDs[i]];

                        _arrSampledParameterValues[thisPar.ID] = thisLinearCombinationPar.Sample(arrValueOfParameters);
                    }
                    break;   

                // multiple combination parameter
                case Parameter.EnumType.MultipleCombination:
                    {
                        MultipleCombination thisMultipleCombinationPar = thisPar as MultipleCombination;
                        int[] arrParIDs = thisMultipleCombinationPar.arrParIDs;
                        double[] arrValueOfParameters = new double[arrParIDs.Length];
                        for (int i = 0; i < arrParIDs.Length; i++)
                            arrValueOfParameters[i] = _arrSampledParameterValues[arrParIDs[i]];

                        _arrSampledParameterValues[thisPar.ID] = thisMultipleCombinationPar.Sample(arrValueOfParameters);
                    }
                    break;
                          
                // time dependent linear parameter
                case Parameter.EnumType.TimeDependetLinear:
                    {
                        TimeDependetLinear thisTimeDepedentLinearPar = thisPar as TimeDependetLinear;
                        double intercept = _arrSampledParameterValues[thisTimeDepedentLinearPar.InterceptParID];
                        double slope = _arrSampledParameterValues[thisTimeDepedentLinearPar.SlopeParID];
                        double timeOn = thisTimeDepedentLinearPar.TimeOn;
                        double timeOff = thisTimeDepedentLinearPar.TimeOff;
                        
                        _arrSampledParameterValues[thisPar.ID] = thisTimeDepedentLinearPar.Sample(time, intercept, slope, timeOn, timeOff);
                    }
                    break;   
   
                // time dependent oscillating parameter
                case Parameter.EnumType.TimeDependetOscillating:
                    {
                        TimeDependetOscillating thisTimeDepedentOscillatingPar = thisPar as TimeDependetOscillating;
                        double a0 = _arrSampledParameterValues[thisTimeDepedentOscillatingPar.a0ParID];
                        double a1 = _arrSampledParameterValues[thisTimeDepedentOscillatingPar.a1ParID];
                        double a2 = _arrSampledParameterValues[thisTimeDepedentOscillatingPar.a2ParID];
                        double a3 = _arrSampledParameterValues[thisTimeDepedentOscillatingPar.a3ParID];
                        
                        _arrSampledParameterValues[thisPar.ID] = thisTimeDepedentOscillatingPar.Sample(time, a0, a1, a2, a3);
                    }
                    break;  
            }
        }
        // sample from parameters
        private void SampleFromParameters(double time, bool updateOnlyTimeDependent)
        {
            if (updateOnlyTimeDependent == false)
            {
                foreach (Parameter thisParameter in Parameters)
                    SampleThisParameter(thisParameter, time);
            }
            else
            {
                foreach (Parameter thisParameter in Parameters.Where(p => p.ShouldBeUpdatedByTime))
                    SampleThisParameter(thisParameter, time);
            }         
           
        }        

        // update the effect of change in time dependent parameters
        private void UpdateTheEffectOfChangeInTimeDependentParameterValues(double time)
        {
            if (_thereAreTimeDependentParameters)
            {
                // sample parameters
                SampleFromParameters(time, true);
                // update time dependent parameters in the model
                UpdateTimeDepedentParametersInTheModel();
            }

            //if (_thereAreTimeDependentParameters)
            //{
            //    bool parameterFound = false;
            //    // update time dependent parameter values
            //    foreach (Parameter thisParameter in _parameters)
            //    {
            //        parameterFound = false;
            //        TimeDependetLinear thisTimeDepedentLinearPar = thisParameter as TimeDependetLinear;
            //        if (thisTimeDepedentLinearPar != null)
            //        {
            //            parameterFound = true;
            //            double intercept = _arrSampledParameterValues[thisTimeDepedentLinearPar.InterceptParID];
            //            double slope = _arrSampledParameterValues[thisTimeDepedentLinearPar.SlopeParID];
            //            double timeOn = thisTimeDepedentLinearPar.TimeOn;
            //            double timeOff = thisTimeDepedentLinearPar.TimeOff;

            //            _arrSampledParameterValues[thisParameter.ID] = thisTimeDepedentLinearPar.Sample(time, intercept, slope, timeOn, timeOff);
            //        }

            //        if (!parameterFound)
            //        {
            //            TimeDependetOscillating thisTimeDepedentOscillatingPar = thisParameter as TimeDependetOscillating;
            //            if (thisTimeDepedentOscillatingPar != null)
            //            {
            //                double a0 = _arrSampledParameterValues[thisTimeDepedentOscillatingPar.a0ParID];
            //                double a1 = _arrSampledParameterValues[thisTimeDepedentOscillatingPar.a1ParID];
            //                double a2 = _arrSampledParameterValues[thisTimeDepedentOscillatingPar.a2ParID];
            //                double a3 = _arrSampledParameterValues[thisTimeDepedentOscillatingPar.a3ParID];

            //                _arrSampledParameterValues[thisParameter.ID] = thisTimeDepedentOscillatingPar.Sample(time, a0, a1, a2, a3);
            //            }
            //        }
            //    }
            //    // update dependent parameters
            //    UpdateDependentParameterValues();
            //    // update time dependent parameters in the model
            //    UpdateTimeDepedentParametersInTheModel();
            //}
        }
        // update time dependent parameters in the model
        private void UpdateTimeDepedentParametersInTheModel()
        {
            // update transmission dynamic matrix if necessary
            if (_thereAreTimeDependentParameters_affectingTranmissionDynamics)// || _thereAreTimeDependentParameters)
            {
                //CalculateTransmissionMatrix();
                // update transmission rates
                //UpdateTransmissionRates();
            }

            // update event rates if necessary
            if (_thereAreTimeDependentParameters_affectingNaturalHistoryRates)
            {
                // update rates associated with each class and their initial size
                foreach (Class thisClass in _classes)
                    thisClass.UpdateRatesOfBirthAndEpiIndpEvents(_arrSampledParameterValues);
            }

            // update value of splitting class parameters
            if (_thereAreTimeDependentParameters_affectingSplittingClasses)
            {
                // update the probability of success
                foreach (Class thisClass in _classes)
                    thisClass.UpdateProbOfSuccess(_arrSampledParameterValues);
            }
        }
        // calculate contract matrices
        private void CalculateContactMatrices() //[intervention ID][pathogen ID][group i, group j]
        {
            int contactMatrixSize = _baseContactMatrices[0].GetLength(0);
            int numOfCombinationsOfInterventionsAffectingContactPattern = (int)Math.Pow(2, _numOfInterventionsAffectingContactPattern);
            _contactMatrices = new double[numOfCombinationsOfInterventionsAffectingContactPattern][][,];

            // build the contact matrices
            for (int intCombIndex = 0; intCombIndex < numOfCombinationsOfInterventionsAffectingContactPattern; ++intCombIndex)
            {
                if (intCombIndex == 0)
                    _contactMatrices[intCombIndex] = _baseContactMatrices;
                else
                {
                    int[] onOfStatusOfInterventionsAffectingContactPattern = SupportFunctions.ConvertToBase2FromBase10(intCombIndex, _numOfInterventionsAffectingContactPattern);
                    for (int interventionIndex = 0; interventionIndex < _numOfInterventionsAffectingContactPattern; ++interventionIndex)
                    {
                        // initialize contact matrices
                        _contactMatrices[intCombIndex] = new double[_numOfPathogens][,];

                        if (onOfStatusOfInterventionsAffectingContactPattern[interventionIndex] == 1)
                        {
                            for (int pathogenID = 0; pathogenID < _numOfPathogens; pathogenID++)
                            {
                                _contactMatrices[intCombIndex][pathogenID] = new double[contactMatrixSize, contactMatrixSize];
                                for (int i = 0; i < contactMatrixSize; ++i)
                                    for (int j = 0; j < contactMatrixSize; ++j)
                                        _contactMatrices[intCombIndex][pathogenID][i, j] = _baseContactMatrices[pathogenID][i, j] +
                                            _baseContactMatrices[pathogenID][i, j] * _arrSampledParameterValues[_percentChangeInContactMatricesParIDs[interventionIndex][pathogenID][i, j]];
                            }
                        }
                    }
                }
            }
        }
        // update susceptibility and infectivity of classes
        private void UpdateSusceptilityAndInfectivityOfClasses(bool updateSusceptibility, bool updateInfectivity)
        {
            if (updateSusceptibility && updateInfectivity)
            {
                foreach (Class thisClass in _classes)
                {
                    // susceptibility
                    if (thisClass.IsEpiDependentEventActive)
                        thisClass.UpdateSusceptibilityParameterValues(_arrSampledParameterValues);
                    // infectivity
                    thisClass.UpdateInfectivityParameterValues(_arrSampledParameterValues);
                }
            }
            else if (updateSusceptibility)
            {
                // only susceptibility
                foreach (Class thisClass in _classes.Where(c => c.IsEpiDependentEventActive))
                    thisClass.UpdateSusceptibilityParameterValues(_arrSampledParameterValues);
            }
            else if (updateInfectivity)
            {
                // only infectivity
                foreach (Class thisClass in _classes)
                    thisClass.UpdateInfectivityParameterValues(_arrSampledParameterValues);
            }
        }
        // update transmission rates
        private void UpdateTransmissionRates()
        {
            // update susceptibility and infectivity of classes
            UpdateSusceptilityAndInfectivityOfClasses(_thereAreTimeDependentParameters_affectingSusceptibilities, _thereAreTimeDependentParameters_affectingInfectivities);

            // find the population size of each mixing group
            int[] populationSizeOfMixingGroups = new int[_baseContactMatrices[0].GetLength(0)];
            foreach (Class thisClass in _classes)
            {
                //_arrNumOfMembersInEachClass[thisClass.ID] = thisClass.ClassStat.Prevalence;
                populationSizeOfMixingGroups[thisClass.RowIndexInContactMatrix] += thisClass.ClassStat.Prevalence;
            }

            // find the index of current action in the contact matrices
            int indexOfIntCombInContactMatrices = FindIndexOfInterventionCombimbinationInTransmissionMatrix(_monitorofInterventionsInEffect.InterventionsInEffect);

            // calculate the transmission rates for each class
            double susContactInf = 0, rate = 0, infectivity = 0;
            double[] arrTransmissionRatesByPathogen = new double[_numOfPathogens];
            foreach (Class thisRecievingClass in _classes.Where(c => c.IsEpiDependentEventActive && c.ClassStat.Prevalence > 0))
            {
                // calculate the transmission rate for each pathogen
                for (int pathogenID = 0; pathogenID < _numOfPathogens; pathogenID++)
                {
                    rate = 0;
                    for (int j = 0; j < _numOfClasses; j++)
                    {
                        // find the infectivity of infection-causing class
                        if (_classes[j] is Class_Normal) 
                        {
                            infectivity = _classes[j].InfectivityValues[pathogenID];
                            if (infectivity > 0)
                            {
                                susContactInf = thisRecievingClass.SusceptibilityValues[pathogenID]
                                                * _contactMatrices[indexOfIntCombInContactMatrices][pathogenID][thisRecievingClass.RowIndexInContactMatrix, _classes[j].RowIndexInContactMatrix]
                                                * infectivity;

                                rate += susContactInf * _arrNumOfMembersInEachClass[j] / populationSizeOfMixingGroups[_classes[j].RowIndexInContactMatrix];
                            }
                        }
                    }

                    arrTransmissionRatesByPathogen[pathogenID] = rate;
                }

                // update the transition rates of this class for all pathogens
                thisRecievingClass.UpdateTransmissionRates(arrTransmissionRatesByPathogen);
            }
        }
        // calculate transmission matrix
        private void CalculateTransmissionMatrix()//(int[] nextPeriodActionCombinationInEffect)
        {
            int contactMatrixSize = _baseContactMatrices[0].GetLength(0);
            int numOfCombinationsOfInterventionsAffectingContactPattern = (int)Math.Pow(2, _numOfInterventionsAffectingContactPattern);

            double[][] arrInfectivity = new double[_numOfClasses][];            
            double[][] arrSusceptibility = new double[_numOfClasses][];
            int[] arrRowInContactMatrix = new int[_numOfClasses];
            double[][][,] contactMatrices = new double[numOfCombinationsOfInterventionsAffectingContactPattern][][,];
            _tranmissionMatrices = new double[numOfCombinationsOfInterventionsAffectingContactPattern][][][];

            // build the contact matrices
            // for all possible intervention combinations
            for (int intCombIndex = 0; intCombIndex < numOfCombinationsOfInterventionsAffectingContactPattern; ++intCombIndex)
            {
                if (intCombIndex == 0)
                    contactMatrices[intCombIndex] = _baseContactMatrices;
                else
                {
                    int[] onOfStatusOfInterventionsAffectingContactPattern = SupportFunctions.ConvertToBase2FromBase10(intCombIndex, _numOfInterventionsAffectingContactPattern);
                    for (int interventionIndex = 0; interventionIndex < _numOfInterventionsAffectingContactPattern; ++interventionIndex)
                    {
                        // initialize contact matrices
                        contactMatrices[intCombIndex] = new double[_numOfPathogens][,];

                        if (onOfStatusOfInterventionsAffectingContactPattern[interventionIndex] == 1)
                        {
                            for (int pathogenID = 0; pathogenID < _numOfPathogens; pathogenID++)
                            {
                                contactMatrices[intCombIndex][pathogenID] = new double[contactMatrixSize, contactMatrixSize];
                                for (int i = 0; i < contactMatrixSize; ++i)
                                    for (int j = 0; j < contactMatrixSize; ++j)
                                        contactMatrices[intCombIndex][pathogenID][i, j] = _baseContactMatrices[pathogenID][i,j]+
                                            _baseContactMatrices[pathogenID][i, j] * _arrSampledParameterValues[_percentChangeInContactMatricesParIDs[interventionIndex][pathogenID][i, j]];
                            }
                        }
                    }
                }
            }

            // get the sample of infectivity and susceptibility of classes                    
            int classID = 0;
            foreach (Class thisClass in _classes)
            {
                // update the susceptibility and infectivity parameters based on the sampled parameter values
                //thisClass.UpdateSusceptibilityAndInfectivityParameterValues(_arrSampledParameterValues);

                arrInfectivity[classID] = thisClass.InfectivityValues;
                arrSusceptibility[classID] = thisClass.SusceptibilityValues;
                arrRowInContactMatrix[classID] = thisClass.RowIndexInContactMatrix;
                ++classID;
            }

            // populate the transmission matrix
            for (int intCombIndex = 0; intCombIndex < numOfCombinationsOfInterventionsAffectingContactPattern; ++intCombIndex)
            {
                _tranmissionMatrices[intCombIndex] = new double[_numOfPathogens][][];
                for (int pathogenID = 0; pathogenID < _numOfPathogens; pathogenID++)
                {
                    _tranmissionMatrices[intCombIndex][pathogenID] = new double[_numOfClasses][];
                    for (int i = 0; i < _numOfClasses; ++i)
                    {
                        if (arrSusceptibility[i].Count() != 0)
                        {
                            _tranmissionMatrices[intCombIndex][pathogenID][i] = new double[_numOfClasses];
                            for (int j = 0; j < _numOfClasses; ++j)
                            {
                                if (arrInfectivity[j].Count() != 0)
                                {
                                    _tranmissionMatrices[intCombIndex][pathogenID][i][j]
                                        = arrSusceptibility[i][pathogenID]
                                            * contactMatrices[intCombIndex][pathogenID][arrRowInContactMatrix[i], arrRowInContactMatrix[j]]
                                            * arrInfectivity[j][pathogenID];
                                }
                            }
                        }
                    }
                }
            }
        }
        // get ready for another ADP iteration
        private void GetReadyForAnotherADPIteration(int itr)
        {
            // update the epsilon greedy
            //_decisionMaker.UpdateEpsilonGreedy(itr);
            // clear ADP state-decision collection      
            //_POMDP_ADP.ResetForAnotherSimulationRun(_totalCost);
        }
        // update current epidemic time
        private void UpdateCurrentEpidemicTimeIndex()
        {
            switch (_set.MarkOfEpidemicStartTime)
            {
                case EnumMarkOfEpidemicStartTime.TimeZero:
                    {
                        _epiTimeIndex = _simTimeIndex;
                    }
                    break;

                case EnumMarkOfEpidemicStartTime.TimeOfFirstObservation:
                    {
                        if (_firstObservationObtained)
                            _epiTimeIndex = _simTimeIndex - _timeIndexOfTheFirstObservation + _set.NumOfDeltaT_inObservationPeriod;
                        else
                            _epiTimeIndex = int.MinValue;
                    }
                    break;
            }            
        }
        
        #endregion

        // ********* public subs to set up the model *************
        #region public subs to set up the model
        // create the model
        public void BuildModel(ref ModelSettings modelSettings)
        {
            _set = modelSettings;

            _decisionMaker = new DecisionMaker(
                _set.EpidemicTimeIndexToStartDecisionMaking,
                _set.NumOfDeltaT_inDecisionInterval);            

            // reset the epidemic
            Reset();
            // add parameters
            AddParameters(modelSettings.ParametersSheet);
            // add pathogens
            AddPathogens(modelSettings.PathogenSheet);
            // add classes
            AddClasses(modelSettings.ClassesSheet);
            // add interventions
            AddInterventions(modelSettings.InterventionSheet);
            // add resources
            AddResources(modelSettings.ResourcesSheet);
            // add events
            AddEvents(modelSettings.EventSheet);            
            // add summation statistics
            AddSummationStatistics(modelSettings.SummationStatisticsSheet);
            // add ratio statistics
            AddRatioStatistics(modelSettings.RatioStatisticsSheet);
            // add connections
            AddConnections(modelSettings.ConnectionsMatrix);
            // update contact matrices
            UpdateContactMatrices();
            // monitor of interventions in effect
            _monitorofInterventionsInEffect = new MonitorOfInterventionsInEffect(ref _decisionMaker);
        }
        public void UpdateContactMatrices()
        {
            _baseContactMatrices = _set.GetBaseContactMatrices();
            _percentChangeInContactMatricesParIDs = _set.GetPercentChangeInContactMatricesParIDs();
        }

        // setup dynamic policy related settings
        public void SetupDynamicPolicySettings
            (ComputationLib.EnumQFunctionApproximationMethod qFunctionApproximationMethod, bool useEpidemicTimeAsFeature, int degreeOfPolynomialQFunction, double L2RegularizationPenalty)
        {
            _useEpidemicTimeAsFeature = useEpidemicTimeAsFeature;
            if (_useEpidemicTimeAsFeature)
            {
                _features.Add(new Feature_EpidemicTime("Epidemic Time", _numOfFeatures));
                ++_numOfFeatures;
            }            

            //_pastDecisionPeriodWithDecisionAsFeature = Math.Max(1, pastDecisionPeriodWithDecisionAsFeature);
            //_decisionsOverPastAndCurrentDecisionPeriods = new int[_pastDecisionPeriodWithDecisionAsFeature + 1];
            // setup Q-functions
            SetupPolynomialQFunctions(qFunctionApproximationMethod, degreeOfPolynomialQFunction);
            // add L2 regularization
            if (L2RegularizationPenalty > 0) 
                AddL2Regularization(L2RegularizationPenalty);
        }
        // setup always on/off and interval-based static policy settings
        public void SetupAlwaysOnOffAndIntervalBasedStaticPolicySettings(int[] interventionsOnOffSwitchStatus, double[] startTimes, int[] numOfDecisionPeriodsToUse)
        {
            //_POMDP_ADP.SetInitialActionCombination(interventionsOnOffSwitchStatus);

            //foreach (Intervention thisIntervention in _POMDP_ADP.Actions)
            //{
            //    int i = thisIntervention.ID;
            //    if (thisIntervention.StaticPolicyType ==  enumStaticPolicyType.IntervalBased)
            //        thisIntervention.AddIntervalBaseEmploymentSetting(
            //            (int)(startTimes[i] / _set.DeltaT), (int)((startTimes[i] + numOfDecisionPeriodsToUse[i] * _decisionIntervalLength) / _set.DeltaT));
            //}
        }
        // setup threshold-based static policy settings
        public void SetupThresholdBasedStaticPolicySettings(int[] interventionIDs, int[] specialStatisticsIDs, double[] thresholds, int[] numOfDecisionPeriodsToUseInterventions)
        {
            //for (int i = 0; i < interventionIDs.Length; ++i)
            //    ((Intervention)_POMDP_ADP.Actions[interventionIDs[i]]).SetupThresholdBasedEmployment                
            //        (specialStatisticsIDs[i], thresholds[i], numOfDecisionPeriodsToUseInterventions[i] * _numOfDeltaTsInADecisionInterval);
        }
        public void SetupThresholdBasedStaticPolicySettings(int[] interventionIDs, double[] thresholds, int[] numOfDecisionPeriodsToUseInterventions)
        {
            //for (int i = 0; i < interventionIDs.Length; ++i)
            //    ((Intervention)_POMDP_ADP.Actions[interventionIDs[i]]).SetupThresholdBasedEmployment   
            //        (thresholds[i], numOfDecisionPeriodsToUseInterventions[i] * _numOfDeltaTsInADecisionInterval);
        }
        // setup Q-functions with polynomial functions
        public void SetupPolynomialQFunctions(EnumQFunctionApproximationMethod qFunctionApproximationMethod, int degreeOfPolynomialQFunction)
        {
            int numOfFeatures = _features.Count;
            //_decisionMaker.SetUpQFunctionApproximationModel(
            //    qFunctionApproximationMethod, SimulationLib.enumResponseTransformation.None, 
            //    numOfFeatures, degreeOfPolynomialQFunction, 2);
        }
        // add L2 regularization
        public void AddL2Regularization(double penaltyParameter)
        {
            //_decisionMaker.AddL2Regularization(penaltyParameter);
        }
        /// <summary>
        /// update Q-function coefficients 
        /// </summary>
        /// <param name="qFunctionCoefficients"> [decisionID][coefficientIndex]</param>
        public void UpdateQFunctionCoefficients(double[][] qFunctionCoefficients)
        {
            double[] arrCoefficients = new double[qFunctionCoefficients.Length * qFunctionCoefficients[0].Length] ;

            // concatenate initial estimates
            int k= 0;
            for (int i = 0; i < qFunctionCoefficients.Length; i++)
                for (int j = 0; j < qFunctionCoefficients[i].Length; j++)
                    arrCoefficients[k++] = qFunctionCoefficients[i][j];

            //_decisionMaker.UpdateQFunctionCoefficients(arrCoefficients);                  
        }
        public void UpdateQFunctionCoefficients(double[] qFunctionCoefficients)
        {
            //_decisionMaker.UpdateQFunctionCoefficients(qFunctionCoefficients);
        }     
                       
        // get which interventions are affecting contact pattern
        public bool[] IfInterventionsAreAffectingContactPattern()
        {
            bool[] result = new bool[_decisionMaker.NumOfInterventions];

            foreach (Intervention thisIntervention in _decisionMaker.Interventions)
                if (thisIntervention.IfAffectingContactPattern)
                    result[thisIntervention.Index] = true;
            return result;
        }

        #endregion

        // private subs to create model
        #region private subs to create model
        
        // read parameters
        private void AddParameters(Array parametersSheet)
        {
            _numOfParametersToCalibrate = 0;
            int lastRowIndex = parametersSheet.GetLength(0);
            for (int rowIndex = 1; rowIndex <= lastRowIndex; ++rowIndex)
            {
                // ID and Name
                int parameterID = Convert.ToInt32(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.ID));
                string name = Convert.ToString(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.Name));
                bool updateAtEachTimeStep = SupportFunctions.ConvertYesNoToBool(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.UpdateAtEachTimeStep).ToString());
                string distribution = Convert.ToString(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.Distribution));
                EnumRandomVariates enumRVG = RandomVariateLib.SupportProcedures.ConvertToEnumRVG(distribution);
                bool includedInCalibration = SupportFunctions.ConvertYesNoToBool(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.IncludedInCalibration).ToString());

                Parameter thisParameter = null;
                double par1 = 0, par2 = 0, par3 = 0, par4 = 0;

                if (enumRVG == EnumRandomVariates.LinearCombination)
                {
                    string strPar1 = Convert.ToString(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.Par1));
                    string strPar2 = Convert.ToString(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.Par2));

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
                    string strPar1 = Convert.ToString(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.Par1));

                    // remove spaces and parenthesis
                    strPar1 = strPar1.Replace(" ", "");
                    strPar1 = strPar1.Replace("(", "");
                    strPar1 = strPar1.Replace(")", "");
                    // convert to array
                    string[] strParIDs = strPar1.Split(',');
                    // convert to numbers
                    int[] arrParIDs = Array.ConvertAll<string, int>(strParIDs, Convert.ToInt32);

                    thisParameter = new MultipleCombination(parameterID, name, arrParIDs);
                }
                else
                {
                    par1 = Convert.ToDouble(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.Par1));
                    par2 = Convert.ToDouble(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.Par2));
                    par3 = Convert.ToDouble(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.Par3));
                    par4 = Convert.ToDouble(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.Par4));
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
                            _thereAreTimeDependentParameters = true;
                        }
                        break;
                    case (EnumRandomVariates.TimeDependetOscillating):
                        {
                            thisParameter = new TimeDependetOscillating(parameterID, name, (int)par1, (int)par2, (int)par3, (int)par4);
                            _thereAreTimeDependentParameters = true;
                        }
                        break;
                    default:
                        thisParameter = new IndependetParameter(parameterID, name, enumRVG, par1, par2, par3, par4);
                        break;
                }

                thisParameter.ShouldBeUpdatedByTime = updateAtEachTimeStep;
                thisParameter.IncludedInCalibration = includedInCalibration;
                
                if (includedInCalibration)
                    ++_numOfParametersToCalibrate;

                // add the parameter to the list
                Parameters.Add(thisParameter);
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
            _numOfPathogens = _pathogenIDs.Length;
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
                bool collectIncidenceTimeSeries = SupportFunctions.ConvertYesNoToBool(Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.CollectIncidenceTimeSeries)));
                bool collectPrevalenceTimeSeries = SupportFunctions.ConvertYesNoToBool(Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.CollectPrevalenceTimeSeries)));
                bool collectAccumIncidenceTimeSeries = SupportFunctions.ConvertYesNoToBool(Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.CollectAccumIncidenceTimeSeries)));

                // simulation output
                bool showStatisticsInSimulationResults = SupportFunctions.ConvertYesNoToBool(Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ShowInSimulationSummaryReport)));
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
                            _classes.Add(thisNormalClass);

                            // check if infectivity and susceptibility parameters are time dependent
                            if (_thereAreTimeDependentParameters)
                            {
                                for (int i = 0; i < thisNormalClass.InfectivityParIDs.Length; i++)
                                    if (Parameters[thisNormalClass.InfectivityParIDs[i]].ShouldBeUpdatedByTime)
                                    {
                                        _thereAreTimeDependentParameters_affectingTranmissionDynamics = true;
                                        _thereAreTimeDependentParameters_affectingInfectivities = true;
                                    }
                                for (int i = 0; i < thisNormalClass.SusceptibilityParIDs.Length; i++)
                                    if (Parameters[thisNormalClass.SusceptibilityParIDs[i]].ShouldBeUpdatedByTime)
                                    {
                                        _thereAreTimeDependentParameters_affectingTranmissionDynamics = true;
                                        _thereAreTimeDependentParameters_affectingSusceptibilities = true;
                                    }
                            }
                        }
                        break;
                    case "Class: Death":
                        {
                            // build the class
                            Class_Death thisDealthClass = new Class_Death(classID, name);

                            // add class
                            _classes.Add(thisDealthClass);
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
                            _classes.Add(thisSplittingClass);

                            // check if the rate parameter is time dependent
                            if (_thereAreTimeDependentParameters && Parameters[parIDForProbOfSuccess].ShouldBeUpdatedByTime)
                                _thereAreTimeDependentParameters_affectingSplittingClasses = true;
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
                            _classes.Add(thisResourceMonitorClass);
                        }
                        break;
                }
                #endregion

                // class statistics 
                _classes.Last().ClassStat = new GeneralTrajectory(classID, name, _set.WarmUpPeriodTimeIndex);
                _classes.Last().ClassStat.SetupAvePrevalenceAndAccumIncidence(showAccumIncidence, false);
                // adding time series
                _classes.Last().ClassStat.AddTimeSeries(
                    collectIncidenceTimeSeries, collectPrevalenceTimeSeries, collectAccumIncidenceTimeSeries, _set.NumOfDeltaT_inSimulationOutputInterval);
                // adding cost and health outcomes
                _classes.Last().ClassStat.AddCostHealthOutcomes(
                    DALYPerNewMember, costPerNewMember, healthQualityPerUnitOfTime * _set.DeltaT, costPerUnitOfTime * _set.DeltaT);

                // set up which statistics to show
                _classes.Last().ShowStatisticsInSimulationResults = showStatisticsInSimulationResults;
                _classes.Last().ShowIncidence = showIncidence;
                _classes.Last().ShowPrevalence = showPrevalence;
                _classes.Last().ShowAccumIncidence = showAccumIncidence;

            }// end of for
            _numOfClasses = _classes.Count;
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

                    if (affectingContactPattern)
                    {
                        ++_numOfInterventionsAffectingContactPattern;
                        SupportFunctions.AddToEndOfArray(ref _indecesOfInterventionsAffectingContactPattern, rowIndex - 1);
                    }

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
                    (int)(timeBecomeAvailable / _set.DeltaT), (int)(timeBecomeUnavailable / _set.DeltaT), delayParID, ref simDecisionRule);

                // set up cost
                thisIntervention.SetUpCost(fixedCost, costPerUnitOfTime, penaltyForSwitchingFromOnToOff);

                // add the intervention
                _decisionMaker.AddAnIntervention(thisIntervention);
            }

            // gather info
            _decisionMaker.UpdateAfterAllInterventionsAdded();

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
                // add the resource
                _resources.Add(thisResource);

                // if a feature should be associated to this
                if (SupportFunctions.ConvertYesNoToBool(Convert.ToString(resourcesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceColumns.SelectAsFeature))))
                    _features.Add(new Feature_DefinedOnResources(name, _numOfFeatures++, ID));
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
                            if (_thereAreTimeDependentParameters && Parameters[IDOfRateParameter].ShouldBeUpdatedByTime)
                                    _thereAreTimeDependentParameters_affectingNaturalHistoryRates = true;
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
            if (_resources.Count == 0) return;

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
                _resourceRules.Add(thisResourceRule);
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

                // QALY loss and cost outcomes
                double QALYLossPerNewMember = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.DALYPerNewMember));
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
                    SumClassesTrajectory thisSumClassTraj = new SumClassesTrajectory(statID, name, type, sumFormula, ifDispay, _set.WarmUpPeriodTimeIndex, _set.NumOfDeltaT_inSimulationOutputInterval);
                    // add the summation statistics
                    SumTrajectories.Add(thisSumClassTraj);
                }
                else // if defined on events
                {
                    SumEventTrajectory thisSumEventTraj = new SumEventTrajectory(statID, name, type, sumFormula, ifDispay, _set.WarmUpPeriodTimeIndex, _set.NumOfDeltaT_inSimulationOutputInterval);
                    // add the summation statistics
                    SumTrajectories.Add(thisSumEventTraj);
                }

                // update calibraton infor
                SumTrajectories.Last().CalibInfo = new TrajectoryCalibrationInfo(ifIncludedInCalibration, ifCheckWithinFeasibleRange, feasibleMin, feasibleMax);
                
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

                bool ifSurveillanceDataAvailable = SupportFunctions.ConvertYesNoToBool(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.SurveillanceDataAvailable).ToString());
                
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
                RatioTrajectory thisRatioTraj = new RatioTrajectory(statID, name, type, ratioFormula, ifDispay, _set.WarmUpPeriodTimeIndex, _set.NumOfDeltaT_inSimulationOutputInterval);

                // set up calibration
                thisRatioTraj.CalibInfo = new TrajectoryCalibrationInfo(ifIncludedInCalibration, ifCheckWithinFeasibleRange, feasibleMin, feasibleMax);

                // add the summation statistics
                RatioTrajectories.Add(thisRatioTraj);
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
                ((Class_Normal)_classes[classID]).AddAnEvent((Event)_events[processID]);
                
                ++i;
            }
        }        
        #endregion

    }
}
