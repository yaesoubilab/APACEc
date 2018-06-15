using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputationLib;
using RandomVariateLib;
using SimulationLib;

namespace APACElib
{
    public class ModelSettings
    {
        private double[][,] _baseContactMatrices = new double[0][,]; //[pathogen ID][group i, group j]
        private int[][][,] _percentChangeInContactMatricesParIDs = new int[0][][,]; //[intervention ID][pathogen ID][group i, group j]

        // delta t
        public double DeltaT { get; set; }
        // simulation, observation and decision periods
        public int NumOfDeltaT_inSimOutputInterval { get; set; }
        public int NumOfDeltaT_inObservationPeriod { get; set; }
        public int NumOfDeltaT_inDecisionInterval { get; set; }

        public int EpidemicTimeIndexToStartDecisionMaking { get; set; }
        public EnumMarkOfEpidemicStartTime MarkOfEpidemicStartTime { get; set; }
        public int WarmUpPeriodTimeIndex { get; set; }
        public int TimeIndexToStop { get; set; }
        public int EpidemicConditionTimeIndex { get; set; }
        public EnumEpiDecisions DecisionRule { get; set; }
        public bool IfShowSimulatedTrajs { get; set; }        
        public double AnnualDiscountRate { get; set; }
        public double DeltaTDiscountRate { get; set; }
        public double WTPForHealth { get; set; }

        public int NumOfPrevalenceOutputsToReport { get; set; }
        public int NumOfIncidenceOutputsToReport { get; set; }

        public EnumModelUse ModelUse { get; set; } = EnumModelUse.Simulation;
        public bool UseParallelComputing { get; set; }
        public int MaxDegreeOfParallelism { get; set; }
        public int FirstRNGSeed { get; set; }
        public int DistanceBtwRNGSeeds { get; set; }
        public EnumSimRNDSeedsSource SimRNDSeedsSource { get; set; } = EnumSimRNDSeedsSource.StartFrom0;
        public int[] RndSeeds { get; set; }
        public double[] RndSeedsGoodnessOfFit { get; set; }
        public int NumOfSimItrs { get; set; }
        public EnumQFunctionApproximationMethod QFunApxMethod { get; set; } = EnumQFunctionApproximationMethod.Q_Approximation;
        public bool IfEpidemicTimeIsUsedAsFeature { get; set; }
        public int PastDecisionPeriodWithDecisionAsFeature { get; set; }
        public int DegreeOfPolynomialQFunction { get; set; }
        public double L2RegularizationPenalty { get; set; }
        public int NumberOfHiddenNeurons { get; set; }
        public double[] QFunctionCoefficients { get; set; }
        public double[] QFunctionCoefficientsInitialValues { get; set; }
        public EnumObjectiveFunction ObjectiveFunction { get; set; }
        public int NumOfSimRunsToBackPropogate { get; set; }
        public double WtpForHealth_min { get; set; }
        public double WtpForHealth_max { get; set; }
        public double WtpForHealth_step { get; set; }
        public double HarmonicRule_a { get; set; }
        public double HarmonicRule_a_min { get; set; }
        public double HarmonicRule_a_max { get; set; }
        public double HarmonicRule_a_step { get; set; }
        public double EpsilonGreedy_beta { get; set; }
        public double EpsilonGreedy_delta { get; set; }
        public double EpsilonGreedy_beta_min { get; set; }
        public double EpsilonGreedy_beta_max { get; set; }
        public double EpsilonGreedy_beta_step { get; set; }
        public int NumOfADPIterations { get; set; }
        public int NumOfIntervalsToDescretizeFeatures { get; set; }
        public int NumOfPrespecifiedRNDSeedsToUse { get; set; }
        public double[][] AdpParameterDesigns { get; set; }
        public EnumStaticPolicyOptimizationMethod StaticPolicyOptimizationMethod { get; set; }
        public int NumOfIterationsToOptimizeStaticPolicies { get; set; }
        public int NumOfSimsInEachIterationForStaticPolicyOpt { get; set; }
        public int DegreeOfPolyFunctionForStochasticApproximation { get; set; }
        public double IntervalBasedPolicy_lastTimeToUseIntervention { get; set; }
        public int IntervalBasedPolicy_numOfDecisionPeriodsToUse { get; set; }
        public int ThresholdBasedPolicy_MaximumNumOfDecisionPeriodsToUse { get; set; }
        public int ThresholdBasedPolicy_MaximumValueOfThresholds { get; set; }
        public int[][] PrespecifiedSequenceOfInterventions { get; set; }
        public double[,] MatrixOfObservationsAndLikelihoodParams { get; set; }
        public int NumOfTrajsInParallelForCalibr { get; set; }
        public Array ParametersSheet { get; set; }
        public Array PathogenSheet { get; set; }
        public Array ClassesSheet { get; set; }
        public Array InterventionSheet { get; set; }
        public Array ResourcesSheet { get; set; }
        public Array EventSheet { get; set; }
        public Array SummationStatisticsSheet { get; set; }
        public Array RatioStatisticsSheet { get; set; }
        public Array FeaturesSheet { get; set; }
        public Array ConditionsSheet { get; set; }
        public Array ObservedHistory { get; set; }
        public int[,] ConnectionsMatrix { get; set; }

        public double[][,] GetBaseContactMatrices() { return _baseContactMatrices; }
        public int[][][,] GetPercentChangeInContactMatricesParIDs() { return _percentChangeInContactMatricesParIDs; }
        
        // read settings from the excel interface
        public void ReadSettings(ref ExcelInterface excelInterface)
        {

            switch (excelInterface.GetWhatToDo())
            {
                case ExcelInterface.enumWhatToDo.Simulate:
                    ModelUse = EnumModelUse.Simulation;
                    break;
                case ExcelInterface.enumWhatToDo.Calibrate:
                    ModelUse = EnumModelUse.Calibration;
                    break;
                case ExcelInterface.enumWhatToDo.OptimizeTheDynamicPolicy:
                    ModelUse = EnumModelUse.Optimization;
                    break;
                case ExcelInterface.enumWhatToDo.OptimizeTheStaticPolicy:
                    ModelUse = EnumModelUse.Optimization;
                    break;
                case ExcelInterface.enumWhatToDo.RunExperiments:
                    ModelUse = EnumModelUse.Simulation;
                    break;
            }

            UseParallelComputing = excelInterface.IsUsingParallelComputing();
            MaxDegreeOfParallelism = excelInterface.GetMaxDegreeOfParallelism();
            FirstRNGSeed = excelInterface.GetFirstRNGSeed();
            DistanceBtwRNGSeeds = excelInterface.GetDistanceBtwRNGSeeds();
            NumOfSimItrs = excelInterface.GetNumberOfSimulationIterations();
            SimRNDSeedsSource = excelInterface.GetSimulationRNDSeedsSource();

            DeltaT = excelInterface.GetTimeStep();
            NumOfDeltaT_inDecisionInterval = excelInterface.GetNumDeltaTDecisionInterval();
            NumOfDeltaT_inSimOutputInterval = excelInterface.GetNumDeltaTSimulationOutputInterval();
            NumOfDeltaT_inObservationPeriod = excelInterface.GetNumDeltaTObservationPeriod();

            EpidemicTimeIndexToStartDecisionMaking = (int)(excelInterface.GetTimeToStartDecisionMaking()/DeltaT);
            MarkOfEpidemicStartTime = excelInterface.GetMarkOfEpidemicStartTime();
            WarmUpPeriodTimeIndex = (int)(excelInterface.GetWarmUpPeriod() / DeltaT);
            TimeIndexToStop = (int)(excelInterface.GetTimeToStop() / DeltaT);
            EpidemicConditionTimeIndex = (int)(excelInterface.GetEpidemicConditionTime() / DeltaT);
            DecisionRule = excelInterface.GetDecisionRule();
            IfShowSimulatedTrajs = excelInterface.GetIfToShowSimulationTrajectories();
            
            AnnualDiscountRate = excelInterface.GetAnnualInterestRate();
            DeltaTDiscountRate = AnnualDiscountRate / DeltaT;
            WTPForHealth = excelInterface.GetWTPForHealth();

            // read RND seeds if necessary
            if (ModelUse == EnumModelUse.Simulation || ModelUse == EnumModelUse.Optimization)
            {
                switch (SimRNDSeedsSource)
                {
                    case EnumSimRNDSeedsSource.StartFrom0:
                        break;
                    case EnumSimRNDSeedsSource.Prespecified:
                        RndSeeds = excelInterface.GetRNDSeeds(NumOfSimItrs);
                        break;
                    case EnumSimRNDSeedsSource.Weighted:
                        {
                            RndSeeds = excelInterface.GetRNDSeeds(NumOfSimItrs);
                            RndSeedsGoodnessOfFit = excelInterface.GetGoodnessOfFitForRNDSeeds(NumOfSimItrs);
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
            EventSheet = excelInterface.GetTableOfEvents();
            SummationStatisticsSheet = excelInterface.GetTableOfSummationStatistics();
            RatioStatisticsSheet = excelInterface.GetTableOfRatioStatistics();
            ConnectionsMatrix = excelInterface.GetConnectionsMatrix();
            FeaturesSheet = excelInterface.GetTableOfFeatures();
            ConditionsSheet = excelInterface.GetTableOfConditions();

            // calibration 
            NumOfTrajsInParallelForCalibr = Math.Min(
                excelInterface.GetNumOfTrajsToSimForCalibr(), 
                excelInterface.GetNumOfTrajsInParallelForCalibr());

            // read prespecified decisions
            if (DecisionRule == EnumEpiDecisions.PredeterminedSequence)
                ReadPastActions(ref excelInterface);
        }

        // read feature and approximation related settings
        public void ReadOptimizationSettings(ref ExcelInterface excelInterface)
        {
            string strQFunctionApproximationMethod = excelInterface.GetQFunctionApproximationMethod();
            switch (strQFunctionApproximationMethod)
            {
                case "Q-Approximation":
                    QFunApxMethod = EnumQFunctionApproximationMethod.Q_Approximation;
                    break;
                case "Additive-Approximation":
                    QFunApxMethod = EnumQFunctionApproximationMethod.A_Approximation;
                    break;
                case "H-Approximation":
                    QFunApxMethod = EnumQFunctionApproximationMethod.H_Approximation;
                    break;
            }

            IfEpidemicTimeIsUsedAsFeature = excelInterface.GetIfEpidemicTimeIsUsedAsFeature();
            PastDecisionPeriodWithDecisionAsFeature = excelInterface.GetPastDecisionPeriodWithDecisionAsFeature();
            DegreeOfPolynomialQFunction = excelInterface.GetDegreeOfPolynomialQFunction();
            L2RegularizationPenalty = excelInterface.GetL2RegularizationPenalty();
            NumberOfHiddenNeurons = excelInterface.GetNumOfHiddenNeurons();

            ObjectiveFunction = excelInterface.GetObjectiveFunction();
            NumOfADPIterations = excelInterface.GetNumOfADPIterations();
            NumOfSimRunsToBackPropogate = excelInterface.GetNumOfSimulationRunsToBackPropogate();
            NumOfPrespecifiedRNDSeedsToUse = excelInterface.GetNumOfPrespecifiedRNDSeedsToUse();
            HarmonicRule_a = excelInterface.GetHarmonicRule_a();
            EpsilonGreedy_beta = excelInterface.GetEpsilonGreedy_beta();
            EpsilonGreedy_delta = excelInterface.GetEpsilonGreedy_delta();

            NumOfIntervalsToDescretizeFeatures = excelInterface.GetnumOfIntervalsToDescretizeFeatures();

            WtpForHealth_min = excelInterface.GetWTPForHealth_min();
            WtpForHealth_max = excelInterface.GetWTPForHealth_max();
            WtpForHealth_step = excelInterface.GetWTPForHealth_step();

            HarmonicRule_a_min = excelInterface.GetHarmonicRule_a_min();
            HarmonicRule_a_max = excelInterface.GetHarmonicRule_a_max();
            HarmonicRule_a_step = excelInterface.GetHarmonicRule_a_step();

            EpsilonGreedy_beta_min = excelInterface.GetEpsilonGreedy_beta_min();
            EpsilonGreedy_beta_max = excelInterface.GetEpsilonGreedy_beta_max();
            EpsilonGreedy_beta_step = excelInterface.GetEpsilonGreedy_beta_step();

            // static policy settings
            StaticPolicyOptimizationMethod = excelInterface.GetStaticPolicyOptimizationMethod();
            NumOfIterationsToOptimizeStaticPolicies = excelInterface.GetNumOfIterationsForOptimizingStaticPolicies();
            NumOfSimsInEachIterationForStaticPolicyOpt = excelInterface.GetNumOfSimsInEachIterationForStaticPolicyOpt();
            DegreeOfPolyFunctionForStochasticApproximation = excelInterface.GetDegreeOfPolyFunctionForStochasticApproximation();
            IntervalBasedPolicy_lastTimeToUseIntervention = excelInterface.GetIntervalBasedPolicy_LastTimeToUseIntervention();
            IntervalBasedPolicy_numOfDecisionPeriodsToUse = excelInterface.GetIntervalBasedPolicy_NumOfDecisionPeriodsToUse();
            ThresholdBasedPolicy_MaximumNumOfDecisionPeriodsToUse = excelInterface.GetThresholdBasedPolicy_MaximumNumOfDecisionPeriodsToUse();
            ThresholdBasedPolicy_MaximumValueOfThresholds = excelInterface.GetThresholdBasedPolicy_MaximumValueOfThresholds();
        }

        // read the contact matrices
        public void ReadContactMatrices(ref ExcelInterface excelInterface, int numOfInterventionsAffectingContactPattern)
        {
            excelInterface.GetBaseAndPercentageChangeContactMatrix(numOfInterventionsAffectingContactPattern, PathogenSheet.GetLength(0),
                ref _baseContactMatrices, ref _percentChangeInContactMatricesParIDs);
        }

        // read past actions
        public void ReadPastActions(ref ExcelInterface excelInterface)
        {
            PrespecifiedSequenceOfInterventions = new int[0][];
            string[] pastActions = excelInterface.GetPrespecifiedSequenceOfInterventions();
            for (int i = 0; i < pastActions.Length; i++)
            {
                int[] actionCombination = SupportFunctions.ConvertStringToIntArray(pastActions[i], ',');
                PrespecifiedSequenceOfInterventions = SupportFunctions.ConcatJaggedArray(PrespecifiedSequenceOfInterventions, actionCombination);
            }            
        }
        // read sheet of observed history
        public void ReadObservedHistory(ref ExcelInterface excelInterface, int numOfCalibrationTargets)
        {
            ObservedHistory = excelInterface.GetTableOfObservedHistory(numOfCalibrationTargets);
        }

        //// read past observations
        //public void ReadPastObservations(ref ExcelInterface excelInterface, int numOfCalibrationTargets)
        //{
        //    // find the number of observations that should be eliminated during the warm-up period
        //    int numOfInitialObsToRemove = (int)(WarmUpPeriodTimeIndex / NumOfDeltaT_inObservationPeriod);
        //    // read observations
        //    MatrixOfObservationsAndLikelihoodParams = excelInterface.GetMatrixOfObservationsAndWeights(numOfInitialObsToRemove, numOfCalibrationTargets);
        //}

        // read q-function coefficient initial values
        public void ReadQFunctionCoefficientsInitialValues(ref ExcelInterface excelInterface, int numOfFeatures)
        {
            QFunctionCoefficientsInitialValues =
               excelInterface.GetQFunctionCoefficientsInitialValues(numOfFeatures);
        }

        // set up ADP parameter designs
        public void SetUpADPParameterDesigns()
        {
            AdpParameterDesigns = new double[0][];
            double thisWTPForHealth, thisHarmonicStepSize_a, thisEpsilonGreedy_beta;

            thisWTPForHealth = WtpForHealth_min;
            while (thisWTPForHealth <= WtpForHealth_max)
            {
                thisHarmonicStepSize_a = HarmonicRule_a_min;
                while (thisHarmonicStepSize_a <= HarmonicRule_a_max)
                {
                    thisEpsilonGreedy_beta = EpsilonGreedy_beta_min;
                    while (thisEpsilonGreedy_beta <= EpsilonGreedy_beta_max)
                    {
                        double[][] thisDesign = new double[1][];
                        thisDesign[0] = new double[3];
                        // design
                        thisDesign[0][(int)enumADPParameter.WTPForHealth] = thisWTPForHealth;
                        thisDesign[0][(int)enumADPParameter.HarmonicStepSize_a] = thisHarmonicStepSize_a;
                        thisDesign[0][(int)enumADPParameter.EpsilonGreedy_beta] = thisEpsilonGreedy_beta;
                        // add design
                        AdpParameterDesigns = SupportFunctions.ConcatJaggedArray(AdpParameterDesigns, thisDesign);

                        thisEpsilonGreedy_beta += EpsilonGreedy_beta_step;
                    }
                    thisHarmonicStepSize_a += HarmonicRule_a_step;
                }
                thisWTPForHealth += WtpForHealth_step;
            }
        }

        public void Clean()
        {
            
        }
    }
}
