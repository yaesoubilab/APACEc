using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputationLib;
using RandomVariateLib;
using SimulationLib;

namespace APACE_lib
{
    public class ModelSettings
    {
        private Epidemic _tempEpidemic = new Epidemic(0);
        enumModelUse _modelUse = enumModelUse.Simulation;
        private bool _useParallelComputing;
        private int _maxDegreeOfParallelism;
        private int _firstRNGSeed;
        private int _distanceBtwRNGSeeds;
        private enumSimulationRNDSeedsSource _simulationRNDSeedsSource = enumSimulationRNDSeedsSource.StartFrom0;
        private int[] _rndSeeds;
        private double[] _rndSeedsGoodnessOfFit;
        private int _numOfSimulationIterations;

        private double[][,] _baseContactMatrices = null; //[pathogen ID][group i, group j]
        private int[][][,] _percentChangeInContactMatricesParIDs = null; //[intervention ID][pathogen ID][group i, group j]
        private Array _parametersSheet;
        private Array _pathogenSheet;
        private Array _classesSheet;
        private Array _interventionSheet;
        private Array _resourcesSheet;
        private Array _processesSheet;
        private Array _summationStatisticsSheet;
        private Array _ratioStatisticsSheet;
        private int[,] _connectionsMatrix;

        // policy related settings
        private SimulationLib.enumQFunctionApproximationMethod _qFunApxMethod = SimulationLib.enumQFunctionApproximationMethod.Q_Approximation;
        private bool _ifEpidemicTimeIsUsedAsFeature;
        private int _pastDecisionPeriodWithDecisionAsFeature;
        private int _degreeOfPolynomialQFunction;
        private double _L2RegularizationPenalty;
        private int _numberOfHiddenNeurons;
        double[] _qFunctionCoefficients;
        double[] _qFunctionCoefficientsInitialValues;

        // adaptive policy optimization
        private enumObjectiveFunction _objectiveFunction;
        private int _numOfSimRunsToBackPropogate;
        private double _wtpForHealth_min, _wtpForHealth_max, _wtpForHealth_step;
        private double _harmonicRule_a;
        private double _harmonicRule_a_min, _harmonicRule_a_max, _harmonicRule_a_step;
        private double _epsilonGreedy_beta, _epsilonGreedy_delta;
        private double _epsilonGreedy_beta_min, _epsilonGreedy_beta_max, _epsilonGreedy_beta_step;
        private int _numOfADPIterations;
        private int _numOfIntervalsToDescretizeFeatures;
        private int _numOfPrespecifiedRNDSeedsToUse;
        private double[][] _adpParameterDesigns;

        // static policy optimization
        private enumStaticPolicyOptimizationMethod _staticPolicyOptimizationMethod;
        private int _numOfIterationsToOptimizeStaticPolicies;
        private int _numOfSimsInEachIterationForStaticPolicyOpt;
        private int _degreeOfPolyFunctionForStochasticApproximation;
        private double _intervalBasedPolicy_lastTimeToUseIntervention;
        private int _intervalBasedPolicy_numOfDecisionPeriodsToUse;
        private int _thresholdBasedPolicy_MaximumNumOfDecisionPeriodsToUse;
        private int _thresholdBasedPolicy_MaximumValueOfThresholds;

        // calibration
        private int[][] _prespecifiedSequenceOfInterventions;
        private double[,] _matrixOfObservationsAndWeights;
        private int _numOfSimulationsRunInParallelForCalibration;


        public enumModelUse ModelUse { get => _modelUse; set => _modelUse = value; }
        public bool UseParallelComputing { get => _useParallelComputing; set => _useParallelComputing = value; }
        public int MaxDegreeOfParallelism { get => _maxDegreeOfParallelism; set => _maxDegreeOfParallelism = value; }
        public int FirstRNGSeed { get => _firstRNGSeed; set => _firstRNGSeed = value; }
        public int DistanceBtwRNGSeeds { get => _distanceBtwRNGSeeds; set => _distanceBtwRNGSeeds = value; }
        public enumSimulationRNDSeedsSource SimulationRNDSeedsSource { get => _simulationRNDSeedsSource; set => _simulationRNDSeedsSource = value; }
        public int[] RndSeeds { get => _rndSeeds; set => _rndSeeds = value; }
        public double[] RndSeedsGoodnessOfFit { get => _rndSeedsGoodnessOfFit; set => _rndSeedsGoodnessOfFit = value; }
        public int NumOfSimulationIterations { get => _numOfSimulationIterations; set => _numOfSimulationIterations = value; }
        public SimulationLib.enumQFunctionApproximationMethod QFunApxMethod { get => _qFunApxMethod; set => _qFunApxMethod = value; }
        public bool IfEpidemicTimeIsUsedAsFeature { get => _ifEpidemicTimeIsUsedAsFeature; set => _ifEpidemicTimeIsUsedAsFeature = value; }
        public int PastDecisionPeriodWithDecisionAsFeature { get => _pastDecisionPeriodWithDecisionAsFeature; set => _pastDecisionPeriodWithDecisionAsFeature = value; }
        public int DegreeOfPolynomialQFunction { get => _degreeOfPolynomialQFunction; set => _degreeOfPolynomialQFunction = value; }
        public double L2RegularizationPenalty { get => _L2RegularizationPenalty; set => _L2RegularizationPenalty = value; }
        public int NumberOfHiddenNeurons { get => _numberOfHiddenNeurons; set => _numberOfHiddenNeurons = value; }
        public double[] QFunctionCoefficients { get => _qFunctionCoefficients; set => _qFunctionCoefficients = value; }
        public double[] QFunctionCoefficientsInitialValues { get => _qFunctionCoefficientsInitialValues; set => _qFunctionCoefficientsInitialValues = value; }
        public enumObjectiveFunction ObjectiveFunction { get => _objectiveFunction; set => _objectiveFunction = value; }
        public int NumOfSimRunsToBackPropogate { get => _numOfSimRunsToBackPropogate; set => _numOfSimRunsToBackPropogate = value; }
        public double WtpForHealth_min { get => _wtpForHealth_min; set => _wtpForHealth_min = value; }
        public double WtpForHealth_max { get => _wtpForHealth_max; set => _wtpForHealth_max = value; }
        public double WtpForHealth_step { get => _wtpForHealth_step; set => _wtpForHealth_step = value; }
        public double HarmonicRule_a { get => _harmonicRule_a; set => _harmonicRule_a = value; }
        public double HarmonicRule_a_min { get => _harmonicRule_a_min; set => _harmonicRule_a_min = value; }
        public double HarmonicRule_a_max { get => _harmonicRule_a_max; set => _harmonicRule_a_max = value; }
        public double HarmonicRule_a_step { get => _harmonicRule_a_step; set => _harmonicRule_a_step = value; }
        public double EpsilonGreedy_beta { get => _epsilonGreedy_beta; set => _epsilonGreedy_beta = value; }
        public double EpsilonGreedy_delta { get => _epsilonGreedy_delta; set => _epsilonGreedy_delta = value; }
        public double EpsilonGreedy_beta_min { get => _epsilonGreedy_beta_min; set => _epsilonGreedy_beta_min = value; }
        public double EpsilonGreedy_beta_max { get => _epsilonGreedy_beta_max; set => _epsilonGreedy_beta_max = value; }
        public double EpsilonGreedy_beta_step { get => _epsilonGreedy_beta_step; set => _epsilonGreedy_beta_step = value; }
        public int NumOfADPIterations { get => _numOfADPIterations; set => _numOfADPIterations = value; }
        public int NumOfIntervalsToDescretizeFeatures { get => _numOfIntervalsToDescretizeFeatures; set => _numOfIntervalsToDescretizeFeatures = value; }
        public int NumOfPrespecifiedRNDSeedsToUse { get => _numOfPrespecifiedRNDSeedsToUse; set => _numOfPrespecifiedRNDSeedsToUse = value; }
        public double[][] AdpParameterDesigns { get => _adpParameterDesigns; set => _adpParameterDesigns = value; }
        public enumStaticPolicyOptimizationMethod StaticPolicyOptimizationMethod { get => _staticPolicyOptimizationMethod; set => _staticPolicyOptimizationMethod = value; }
        public int NumOfIterationsToOptimizeStaticPolicies { get => _numOfIterationsToOptimizeStaticPolicies; set => _numOfIterationsToOptimizeStaticPolicies = value; }
        public int NumOfSimsInEachIterationForStaticPolicyOpt { get => _numOfSimsInEachIterationForStaticPolicyOpt; set => _numOfSimsInEachIterationForStaticPolicyOpt = value; }
        public int DegreeOfPolyFunctionForStochasticApproximation { get => _degreeOfPolyFunctionForStochasticApproximation; set => _degreeOfPolyFunctionForStochasticApproximation = value; }
        public double IntervalBasedPolicy_lastTimeToUseIntervention { get => _intervalBasedPolicy_lastTimeToUseIntervention; set => _intervalBasedPolicy_lastTimeToUseIntervention = value; }
        public int IntervalBasedPolicy_numOfDecisionPeriodsToUse { get => _intervalBasedPolicy_numOfDecisionPeriodsToUse; set => _intervalBasedPolicy_numOfDecisionPeriodsToUse = value; }
        public int ThresholdBasedPolicy_MaximumNumOfDecisionPeriodsToUse { get => _thresholdBasedPolicy_MaximumNumOfDecisionPeriodsToUse; set => _thresholdBasedPolicy_MaximumNumOfDecisionPeriodsToUse = value; }
        public int ThresholdBasedPolicy_MaximumValueOfThresholds { get => _thresholdBasedPolicy_MaximumValueOfThresholds; set => _thresholdBasedPolicy_MaximumValueOfThresholds = value; }
        public int[][] PrespecifiedSequenceOfInterventions { get => _prespecifiedSequenceOfInterventions; set => _prespecifiedSequenceOfInterventions = value; }
        public double[,] MatrixOfObservationsAndWeights { get => _matrixOfObservationsAndWeights; set => _matrixOfObservationsAndWeights = value; }
        public int NumOfSimulationsRunInParallelForCalibration { get => _numOfSimulationsRunInParallelForCalibration; set => _numOfSimulationsRunInParallelForCalibration = value; }
        public Array ParametersSheet { get => _parametersSheet; set => _parametersSheet = value; }
        public Array PathogenSheet { get => _pathogenSheet; set => _pathogenSheet = value; }
        public Array ClassesSheet { get => _classesSheet; set => _classesSheet = value; }
        public Array InterventionSheet { get => _interventionSheet; set => _interventionSheet = value; }
        public Array ResourcesSheet { get => _resourcesSheet; set => _resourcesSheet = value; }
        public Array ProcessesSheet { get => _processesSheet; set => _processesSheet = value; }
        public Array SummationStatisticsSheet { get => _summationStatisticsSheet; set => _summationStatisticsSheet = value; }
        public Array RatioStatisticsSheet { get => _ratioStatisticsSheet; set => _ratioStatisticsSheet = value; }
        public int[,] ConnectionsMatrix { get => _connectionsMatrix; set => _connectionsMatrix = value; }
        public Epidemic TempEpidemic { get => _tempEpidemic; set => _tempEpidemic = value; }

        public double[][,] GetBaseContactMatrices() { return _baseContactMatrices; }
        public int[][][,] GetPercentChangeInContactMatricesParIDs() { return _percentChangeInContactMatricesParIDs; }
        

        // read settings from the excel interface
        public void ReadSettings(ref ExcelInterface excelInterface)
        {

            switch (excelInterface.GetWhatToDo())
            {
                case ExcelInterface.enumWhatToDo.Simulate:
                    _modelUse = enumModelUse.Simulation;
                    break;
                case ExcelInterface.enumWhatToDo.Calibrate:
                    _modelUse = enumModelUse.Calibration;
                    break;
                case ExcelInterface.enumWhatToDo.OptimizeTheDynamicPolicy:
                    _modelUse = enumModelUse.Optimization;
                    break;
                case ExcelInterface.enumWhatToDo.OptimizeTheStaticPolicy:
                    _modelUse = enumModelUse.Optimization;
                    break;
                case ExcelInterface.enumWhatToDo.RunExperiments:
                    _modelUse = enumModelUse.Simulation;
                    break;
            }

            _useParallelComputing = excelInterface.IsUsingParallelComputing();
            _maxDegreeOfParallelism = excelInterface.GetMaxDegreeOfParallelism();
            _firstRNGSeed = excelInterface.GetFirstRNGSeed();
            _distanceBtwRNGSeeds = excelInterface.GetDistanceBtwRNGSeeds();
            _numOfSimulationIterations = excelInterface.GetNumberOfSimulationIterations();
            _simulationRNDSeedsSource = excelInterface.GetSimulationRNDSeedsSource();

            //setup simulation settings
            TempEpidemic.SetupSimulationSettings(
                excelInterface.GetMarkOfEpidemicStartTime(),
                excelInterface.GetTimeStep(),
                excelInterface.GetDecisionIntervalLength(),
                excelInterface.GetWarmUpPeriod(),
                excelInterface.GetTimeToStop(),
                excelInterface.GetEpidemicConditionTime(),
                excelInterface.GetTimeToStartDecisionMaking(),
                0,
                excelInterface.GetDecisionRule(),
                excelInterface.GetIfToShowSimulationTrajectories(),
                excelInterface.GetSimulationOutputIntervalLength(),
                excelInterface.GetObservationPeriodLength(),
                (double)excelInterface.GetAnnualInterestRate(),
                (double)excelInterface.GetWTPForHealth()
                );

            // read RND seeds if necessary
            if (_modelUse == enumModelUse.Simulation || _modelUse == enumModelUse.Optimization)
            {
                switch (_simulationRNDSeedsSource)
                {
                    case enumSimulationRNDSeedsSource.StartFrom0:
                        break;
                    case enumSimulationRNDSeedsSource.PrespecifiedSquence:
                        _rndSeeds = excelInterface.GetRNDSeeds(_numOfSimulationIterations);
                        break;
                    case enumSimulationRNDSeedsSource.WeightedPrespecifiedSquence:
                        {
                            _rndSeeds = excelInterface.GetRNDSeeds(_numOfSimulationIterations);
                            _rndSeedsGoodnessOfFit = excelInterface.GetGoodnessOfFitForRNDSeeds(_numOfSimulationIterations);
                        }
                        break;
                }
            }

            // read sheets
            ParametersSheet = excelInterface.GetTableOfParameters();
            PathogenSheet = excelInterface.GetTableOfPathogens();
            ClassesSheet = excelInterface.GetTableOfClasses();
            InterventionSheet = excelInterface.GetTableOfInterventions();
            ResourcesSheet = excelInterface.GetTableOfResources();
            ProcessesSheet = excelInterface.GetTableOfProcesses();
            SummationStatisticsSheet = excelInterface.GetTableOfSummationStatistics();
            RatioStatisticsSheet = excelInterface.GetTableOfRatioStatistics();
            ConnectionsMatrix = excelInterface.GetConnectionsMatrix();

            // calibration 
            _numOfSimulationsRunInParallelForCalibration = Math.Min(
                excelInterface.GetInitialNumberOfTrajectoriesForCalibration(), 
                excelInterface.GetNumOfSimulationsRunInParallelForCalibration());

        }

        // read feature and approximation related settings
        public void ReadOptimizationSettings(ref ExcelInterface excelInterface)
        {
            string strQFunctionApproximationMethod = excelInterface.GetQFunctionApproximationMethod();
            switch (strQFunctionApproximationMethod)
            {
                case "Q-Approximation":
                    _qFunApxMethod = POMDP_ADP.enumQFunctionApproximationMethod.Q_Approximation;
                    break;
                case "Additive-Approximation":
                    _qFunApxMethod = POMDP_ADP.enumQFunctionApproximationMethod.A_Approximation;
                    break;
                case "H-Approximation":
                    _qFunApxMethod = POMDP_ADP.enumQFunctionApproximationMethod.H_Approximation;
                    break;
            }

            _ifEpidemicTimeIsUsedAsFeature = excelInterface.GetIfEpidemicTimeIsUsedAsFeature();
            _pastDecisionPeriodWithDecisionAsFeature = excelInterface.GetPastDecisionPeriodWithDecisionAsFeature();
            _degreeOfPolynomialQFunction = excelInterface.GetDegreeOfPolynomialQFunction();
            _L2RegularizationPenalty = excelInterface.GetL2RegularizationPenalty();
            _numberOfHiddenNeurons = excelInterface.GetNumOfHiddenNeurons();

            _objectiveFunction = excelInterface.GetObjectiveFunction();
            _numOfADPIterations = excelInterface.GetNumOfADPIterations();
            _numOfSimRunsToBackPropogate = excelInterface.GetNumOfSimulationRunsToBackPropogate();
            _numOfPrespecifiedRNDSeedsToUse = excelInterface.GetNumOfPrespecifiedRNDSeedsToUse();
            _harmonicRule_a = excelInterface.GetHarmonicRule_a();
            _epsilonGreedy_beta = excelInterface.GetEpsilonGreedy_beta();
            _epsilonGreedy_delta = excelInterface.GetEpsilonGreedy_delta();

            _numOfIntervalsToDescretizeFeatures = excelInterface.GetnumOfIntervalsToDescretizeFeatures();

            _wtpForHealth_min = excelInterface.GetWTPForHealth_min();
            _wtpForHealth_max = excelInterface.GetWTPForHealth_max();
            _wtpForHealth_step = excelInterface.GetWTPForHealth_step();

            _harmonicRule_a_min = excelInterface.GetHarmonicRule_a_min();
            _harmonicRule_a_max = excelInterface.GetHarmonicRule_a_max();
            _harmonicRule_a_step = excelInterface.GetHarmonicRule_a_step();

            _epsilonGreedy_beta_min = excelInterface.GetEpsilonGreedy_beta_min();
            _epsilonGreedy_beta_max = excelInterface.GetEpsilonGreedy_beta_max();
            _epsilonGreedy_beta_step = excelInterface.GetEpsilonGreedy_beta_step();

            // static policy settings
            _staticPolicyOptimizationMethod = excelInterface.GetStaticPolicyOptimizationMethod();
            _numOfIterationsToOptimizeStaticPolicies = excelInterface.GetNumOfIterationsForOptimizingStaticPolicies();
            _numOfSimsInEachIterationForStaticPolicyOpt = excelInterface.GetNumOfSimsInEachIterationForStaticPolicyOpt();
            _degreeOfPolyFunctionForStochasticApproximation = excelInterface.GetDegreeOfPolyFunctionForStochasticApproximation();
            _intervalBasedPolicy_lastTimeToUseIntervention = excelInterface.GetIntervalBasedPolicy_LastTimeToUseIntervention();
            _intervalBasedPolicy_numOfDecisionPeriodsToUse = excelInterface.GetIntervalBasedPolicy_NumOfDecisionPeriodsToUse();
            _thresholdBasedPolicy_MaximumNumOfDecisionPeriodsToUse = excelInterface.GetThresholdBasedPolicy_MaximumNumOfDecisionPeriodsToUse();
            _thresholdBasedPolicy_MaximumValueOfThresholds = excelInterface.GetThresholdBasedPolicy_MaximumValueOfThresholds();
        }

        // read the contact matrices
        public void ReadContactMatrices(ref ExcelInterface excelInterface, int numOfInterventionsAffectingContactPattern)
        {
            excelInterface.GetBaseAndPercentageChangeContactMatrix(numOfInterventionsAffectingContactPattern, _pathogenSheet.GetLength(0),
                ref _baseContactMatrices, ref _percentChangeInContactMatricesParIDs);
        }

        // read past actions
        public void ReadPastActions(ref ExcelInterface excelInterface)
        {
            _prespecifiedSequenceOfInterventions = new int[0][];
            string[] pastActions = excelInterface.GetPrespecifiedSequenceOfInterventions();
            for (int i = 0; i < pastActions.Length; i++)
            {
                int[] actionCombination = SupportFunctions.ConvertStringToIntArray(pastActions[i], ',');
                _prespecifiedSequenceOfInterventions = SupportFunctions.ConcatJaggedArray(_prespecifiedSequenceOfInterventions, actionCombination);
            }

            
        }
        // read past observations
        public void ReadPastObservations(ref ExcelInterface excelInterface, int numOfCalibrationTargets)
        {
            // find the number of observations that should be eliminated during the warm-up period
            int numOfInitialObsToRemove = (int)(_tempEpidemic.WarmUpPeriodIndex* _tempEpidemic.DeltaT/ _tempEpidemic.ObservationPeriodLengh);
            // read observations
            _matrixOfObservationsAndWeights = excelInterface.GetMatrixOfObservationsAndWeights(numOfInitialObsToRemove, numOfCalibrationTargets);
        }

        // read q-function coefficient initial values
        public void ReadQFunctionCoefficientsInitialValues(ref ExcelInterface excelInterface, int numOfFeatures)
        {
            _qFunctionCoefficientsInitialValues =
               excelInterface.GetQFunctionCoefficientsInitialValues(numOfFeatures);
        }

        // set up ADP parameter designs
        public void SetUpADPParameterDesigns()
        {
            _adpParameterDesigns = new double[0][];
            double thisWTPForHealth, thisHarmonicStepSize_a, thisEpsilonGreedy_beta;

            thisWTPForHealth = _wtpForHealth_min;
            while (thisWTPForHealth <= _wtpForHealth_max)
            {
                thisHarmonicStepSize_a = _harmonicRule_a_min;
                while (thisHarmonicStepSize_a <= _harmonicRule_a_max)
                {
                    thisEpsilonGreedy_beta = _epsilonGreedy_beta_min;
                    while (thisEpsilonGreedy_beta <= _epsilonGreedy_beta_max)
                    {
                        double[][] thisDesign = new double[1][];
                        thisDesign[0] = new double[3];
                        // design
                        thisDesign[0][(int)enumADPParameter.WTPForHealth] = thisWTPForHealth;
                        thisDesign[0][(int)enumADPParameter.HarmonicStepSize_a] = thisHarmonicStepSize_a;
                        thisDesign[0][(int)enumADPParameter.EpsilonGreedy_beta] = thisEpsilonGreedy_beta;
                        // add design
                        _adpParameterDesigns = SupportFunctions.ConcatJaggedArray(_adpParameterDesigns, thisDesign);

                        thisEpsilonGreedy_beta += _epsilonGreedy_beta_step;
                    }
                    thisHarmonicStepSize_a += _harmonicRule_a_step;
                }
                thisWTPForHealth += _wtpForHealth_step;
            }
        }
    }
}
