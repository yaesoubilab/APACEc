using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputationLib;
using RandomVariateLib;

namespace APACElib
{
    public class ModelSettings
    {
        public OptimizationSettings OptmzSets { get; private set;}

        private double[][,] _baseContactMatrices = new double[0][,]; //[pathogen ID][group i, group j]
        private int[][][,] _percentChangeInContactMatricesParIDs = new int[0][][,]; //[intervention ID][pathogen ID][group i, group j]
        public double DeltaT { get; set; }
        // simulation, observation and decision periods
        public int NumOfDeltaT_inSimOutputInterval { get; set; }
        public int NumOfDeltaT_inObservationPeriod { get; set; }
        public int NumOfDeltaT_inDecisionInterval { get; set; }

        public int EpidemicTimeIndexToStartDecisionMaking { get; set; }
        public EnumMarkOfEpidemicStartTime MarkOfEpidemicStartTime { get; set; }
        public int WarmUpPeriodSimTIndex { get; set; }
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
        public int ScenarioSeed { get; set; }
        public int EpiTimeIndexToChangeSeed { get; set; }
        public EnumSimRNDSeedsSource SimRNDSeedsSource { get; set; } = EnumSimRNDSeedsSource.StartFrom0;
        public int[] RndSeeds { get; set; }
        public double[] RndSeedsGoodnessOfFit { get; set; }
        public int NumOfSimItrs { get; set; }
        public int NumOfSeedsToRead { get; set; }
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
                case ExcelInterface.enumWhatToDo.Optimize:
                    ModelUse = EnumModelUse.Optimization;
                    break;
                case ExcelInterface.enumWhatToDo.RunExperiments:
                    ModelUse = EnumModelUse.Simulation;
                    break;
            }

            DeltaT = excelInterface.GetTimeStep();

            UseParallelComputing = excelInterface.IsUsingParallelComputing();
            MaxDegreeOfParallelism = excelInterface.GetMaxDegreeOfParallelism();
            FirstRNGSeed = excelInterface.GetFirstRNGSeed();
            ScenarioSeed = excelInterface.GetScenarioSeed();
            EpiTimeIndexToChangeSeed = (int)(Math.Round(excelInterface.GetEpiTimeIndexToChangeSeed() / DeltaT,0));
            NumOfSimItrs = excelInterface.GetNumberOfSimulationIterations();
            NumOfSeedsToRead = excelInterface.GetNumberOfSeedsToRead();
            SimRNDSeedsSource = excelInterface.GetSimulationRNDSeedsSource();
            
            NumOfDeltaT_inDecisionInterval = excelInterface.GetNumDeltaTDecisionInterval();
            NumOfDeltaT_inSimOutputInterval = excelInterface.GetNumDeltaTSimulationOutputInterval();
            NumOfDeltaT_inObservationPeriod = excelInterface.GetNumDeltaTObservationPeriod();

            EpidemicTimeIndexToStartDecisionMaking = (int)(excelInterface.GetTimeToStartDecisionMaking()/DeltaT);
            MarkOfEpidemicStartTime = excelInterface.GetMarkOfEpidemicStartTime();
            WarmUpPeriodSimTIndex = (int)(Math.Round(excelInterface.GetWarmUpPeriod() / DeltaT,0));
            TimeIndexToStop = (int)(Math.Round(excelInterface.GetTimeToStop() / DeltaT, 0));
            EpidemicConditionTimeIndex = (int)(Math.Round(excelInterface.GetEpidemicConditionTime() / DeltaT,0));
            DecisionRule = excelInterface.GetDecisionRule();
            IfShowSimulatedTrajs = excelInterface.GetIfToShowSimulationTrajectories();
            
            AnnualDiscountRate = excelInterface.GetAnnualInterestRate();
            DeltaTDiscountRate = AnnualDiscountRate * DeltaT;
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
                    case EnumSimRNDSeedsSource.RandomUnweighted:
                    case EnumSimRNDSeedsSource.RandomWeighted:
                        {
                            RndSeeds = excelInterface.GetRNDSeeds(NumOfSeedsToRead);
                            RndSeedsGoodnessOfFit = excelInterface.GetGoodnessOfFitForRNDSeeds(NumOfSeedsToRead);
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

        // read optimization settings
        public void ReadOptimizationSettings(ref ExcelInterface excelInterface)
        {
            OptmzSets = new OptimizationSettings(ref excelInterface);
        }
            // read feature and approximation related settings
            public void ReadADPOptimizationSettings(ref ExcelInterface excelInterface)
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

            HarmonicRule_a_min = excelInterface.GetHarmonicRule_a_min();
            HarmonicRule_a_max = excelInterface.GetHarmonicRule_a_max();
            HarmonicRule_a_step = excelInterface.GetHarmonicRule_a_step();

            EpsilonGreedy_beta_min = excelInterface.GetEpsilonGreedy_beta_min();
            EpsilonGreedy_beta_max = excelInterface.GetEpsilonGreedy_beta_max();
            EpsilonGreedy_beta_step = excelInterface.GetEpsilonGreedy_beta_step();
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

        // read q-function coefficient initial values
        public void ReadQFunctionCoefficientsInitialValues(ref ExcelInterface excelInterface, int numOfFeatures)
        {
            QFunctionCoefficientsInitialValues =
               excelInterface.GetQFunctionCoefficientsInitialValues(numOfFeatures);
        }

        // set up ADP parameter designs
        public void SetUpADPParameterDesigns()
        {
            //AdpParameterDesigns = new double[0][];
            //double thisWTPForHealth, thisHarmonicStepSize_a, thisEpsilonGreedy_beta;

            //thisWTPForHealth = WTP_min;
            //while (thisWTPForHealth <= WTP_max)
            //{
            //    thisHarmonicStepSize_a = HarmonicRule_a_min;
            //    while (thisHarmonicStepSize_a <= HarmonicRule_a_max)
            //    {
            //        thisEpsilonGreedy_beta = EpsilonGreedy_beta_min;
            //        while (thisEpsilonGreedy_beta <= EpsilonGreedy_beta_max)
            //        {
            //            double[][] thisDesign = new double[1][];
            //            thisDesign[0] = new double[3];
            //            // design
            //            thisDesign[0][(int)enumADPParameter.WTPForHealth] = thisWTPForHealth;
            //            thisDesign[0][(int)enumADPParameter.HarmonicStepSize_a] = thisHarmonicStepSize_a;
            //            thisDesign[0][(int)enumADPParameter.EpsilonGreedy_beta] = thisEpsilonGreedy_beta;
            //            // add design
            //            AdpParameterDesigns = SupportFunctions.ConcatJaggedArray(AdpParameterDesigns, thisDesign);

            //            thisEpsilonGreedy_beta += EpsilonGreedy_beta_step;
            //        }
            //        thisHarmonicStepSize_a += HarmonicRule_a_step;
            //    }
            //    thisWTPForHealth += WTP_step;
            //}
        }
    }

    
    public class OptimizationSettings
    {
        public int NOfItrs { get; }
        public int NOfLastItrsToAverage { get; }
        public double[] X0 { get; } 
        public double[] XScale { get; }
        public bool IfExportResults { get; }

        public double[] StepSize_GH_a0s { get; }
        public double[] StepSize_GH_bs { get; }
        public double[] DerivativeStep_cs { get; }
        public double WTP_min { get; }
        public double WTP_max { get; }
        public double WTP_step { get; }
        public double Penalty { get; }

        public OptimizationSettings(ref ExcelInterface excelInterface)
        {            
            NOfItrs = (int)(double)excelInterface.GetCellValue("General Settings", "numOfOptIterations");
            NOfLastItrsToAverage = (int)(double)excelInterface.GetCellValue("General Settings", "numOfLastOptmItrsToAve");

            string strX0 = excelInterface.GetCellValue("General Settings", "initialX").ToString();
            X0 =Array.ConvertAll(strX0.Split(','), Convert.ToDouble);

            string strXScale = excelInterface.GetCellValue("General Settings", "xScale").ToString();
            XScale = Array.ConvertAll(strXScale.Split(','), Convert.ToDouble);            

            IfExportResults = SupportFunctions.ConvertYesNoToBool(excelInterface.GetCellValue("General Settings", "ifExportOptResults").ToString());

            string str_a0s = excelInterface.GetCellValue("General Settings", "stepSize_GH_a0s").ToString();
            StepSize_GH_a0s = Array.ConvertAll(str_a0s.Split(','), Convert.ToDouble);
            string str_bs = excelInterface.GetCellValue("General Settings", "stepSize_GH_bs").ToString();
            StepSize_GH_bs = Array.ConvertAll(str_bs.Split(','), Convert.ToDouble);

            string strCs = excelInterface.GetCellValue("General Settings", "derivativeStep").ToString();
            DerivativeStep_cs = Array.ConvertAll(strCs.Split(','), Convert.ToDouble);
                      
            WTP_min = (double) excelInterface.GetCellValue("General Settings", "wtpMin");
            WTP_max = (double) excelInterface.GetCellValue("General Settings", "wtpMax");
            WTP_step = (double) excelInterface.GetCellValue("General Settings", "wtpStep");

            Penalty = (double)excelInterface.GetCellValue("General Settings", "penalty");
        }
    }
}
