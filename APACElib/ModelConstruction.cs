using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputationLib;
using RandomVariateLib;

namespace APACElib
{
    public class ModelSheets
    {
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

        public void Populate(ExcelInterface excelInterface)
        {
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
        }
    }

    public class ModelSettings
    {
        public OptimizationSettings OptmzSets { get; private set;}
        public ModelSheets Sheets { get; private set; }

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

        public double[][,] GetBaseContactMatrices() { return _baseContactMatrices; }
        public int[][][,] GetPercentChangeInContactMatricesParIDs() { return _percentChangeInContactMatricesParIDs; }
        
        public int GetNumModelsToBuild()
        {
            // find how many epi models to create
            int nModels = 0;
            switch (ModelUse)
            {
                case EnumModelUse.Simulation:
                    nModels = NumOfSimItrs;
                    break;
                case EnumModelUse.Optimization:
                    nModels = -1; // this is determined based on how many parameters the policy has
                    break;
                case EnumModelUse.Calibration:
                    nModels = NumOfTrajsInParallelForCalibr;
                    break;
            }
            return nModels;
        }

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

            if (ModelUse == EnumModelUse.Optimization)
                OptmzSets = new OptimizationSettings(ref excelInterface);

            // read sheets
            Sheets = new ModelSheets();
            Sheets.Populate(excelInterface);

            // calibration 
            NumOfTrajsInParallelForCalibr = Math.Min(
                excelInterface.GetNumOfTrajsToSimForCalibr(), 
                excelInterface.GetNumOfTrajsInParallelForCalibr());

            // read prespecified decisions
            if (DecisionRule == EnumEpiDecisions.PredeterminedSequence)
                ReadPastActions(ref excelInterface);
        }

        // read optimization settings
        //public void ReadOptimizationSettings(ref ExcelInterface excelInterface)
        //{
            
        //}
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
            excelInterface.GetBaseAndPercentageChangeContactMatrix(numOfInterventionsAffectingContactPattern, Sheets.PathogenSheet.GetLength(0),
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
            Sheets.ObservedHistory = excelInterface.GetTableOfObservedHistory(numOfCalibrationTargets);
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
        public double[] WTPs { get; }
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

            string strWTPs = excelInterface.GetCellValue("General Settings", "wtps").ToString();
            WTPs = Array.ConvertAll(strWTPs.Split(','), Convert.ToDouble);

            Penalty = (double)excelInterface.GetCellValue("General Settings", "penalty");
        }
    }
    
    public class ModelInstruction
    {
        protected ModelSettings _modelSets;
        protected int[] _pathogenIDs;
        protected ParameterManager _paramManager;
        protected List<Class> _classes;
        protected Dictionary<string, int> _dicClasses;
        protected List<Event> _events;
        protected Dictionary<string, int> _dicEvents;
        protected EpidemicHistory _epiHist;
        protected ForceOfInfectionModel _FOIModel;
        protected DecisionMaker _decisionMaker;

        public ModelInstruction()
        {
            
        }

        public void AssignElements(
            ModelSettings modelSets,
            ref int[] pathogenIDs,
            ParameterManager paramManager,
            List<Class> classes,
            List<Event> events,
            EpidemicHistory epiHist,
            ForceOfInfectionModel FOIModel,
            DecisionMaker decisionMaker)
        {
            _dicClasses = new Dictionary<string, int>();
            _dicEvents = new Dictionary<string, int>();
            _modelSets = modelSets;
            _pathogenIDs = pathogenIDs;
            _paramManager = paramManager;
            _classes = classes;
            _events = events;
            _epiHist = epiHist;
            _FOIModel = FOIModel;
            _decisionMaker = decisionMaker;
        }

        public virtual void BuildModel()
        { 
            // add parameters 
            AddParameters();            
            // add classes
            AddClasses();
            // add events
            AddEvents();
            // add interventions
            AddInterventions();
            // add summation statistics
            AddSummationStatistics();
            // add ratio statistics
            AddRatioStatistics();         
            // add features
            AddFeatures();
            // add conditions
            AddConditions();
            // add connections
            AddConnections();
        }

        // add parameters
        public void AddParameters()
        {
            Array parametersSheet = _modelSets.Sheets.ParametersSheet;

            int lastRowIndex = parametersSheet.GetLength(0);
            for (int rowIndex = 1; rowIndex <= lastRowIndex; ++rowIndex)
            {
                // ID and Name
                int parameterID = Convert.ToInt32(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.ID));
                string name = Convert.ToString(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.Name));
                bool updateAtEachTimeStep = SupportFunctions.ConvertYesNoToBool(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.UpdateAtEachTimeStep).ToString());
                string distribution = Convert.ToString(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.Distribution));
                Parameter.EnumType enumParameterType = Parameter.FindParameterType(distribution);
                bool includedInCalibration = SupportFunctions.ConvertYesNoToBool(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.IncludedInCalibration).ToString());

                Parameter thisParameter = null;
                double par1 = 0, par2 = 0, par3 = 0, par4 = 0;

                // read parameter values
                switch (enumParameterType)
                {
                    case Parameter.EnumType.LinearCombination:
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

                            thisParameter = new LinearCombination(parameterID, name, _paramManager.GetParameters(arrParIDs), arrCoefficients);
                        }
                        break;
                    case Parameter.EnumType.Product:
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

                            thisParameter = new ProductParameter(parameterID, name, _paramManager.GetParameters(arrParIDs));
                        }
                        break;
                    default:
                        {
                            par1 = Convert.ToDouble(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.Par1));
                            par2 = Convert.ToDouble(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.Par2));
                            par3 = Convert.ToDouble(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.Par3));
                            par4 = Convert.ToDouble(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.EnumParamsColumns.Par4));
                        }
                        break;
                }

                // build parameters
                switch (enumParameterType)
                {
                    case Parameter.EnumType.LinearCombination:
                    case Parameter.EnumType.Product:
                        // created above
                        break;
                    case Parameter.EnumType.Correlated:
                        thisParameter = new CorrelatedParameter(parameterID, name,
                            _paramManager.Parameters[(int)par1], par2, par3);
                        break;
                    case Parameter.EnumType.TenToPower:
                        thisParameter = new TenToPowerParameter(parameterID, name,
                            _paramManager.Parameters[(int)par1]);
                        break;
                    case Parameter.EnumType.Multiplicative:
                        thisParameter = new MultiplicativeParameter(parameterID, name,
                            _paramManager.Parameters[(int)par1], _paramManager.Parameters[(int)par2], (bool)(par3 == 1));
                        break;
                    case Parameter.EnumType.TimeDependentLinear:
                        thisParameter = new TimeDependetLinear(parameterID, name,
                            _paramManager.Parameters[(int)par1], _paramManager.Parameters[(int)par2], par3, par4);
                        break;
                    case Parameter.EnumType.TimeDependentOscillating:
                        thisParameter = new TimeDependetOscillating(parameterID, name,
                             _paramManager.Parameters[(int)par1], _paramManager.Parameters[(int)par2],
                              _paramManager.Parameters[(int)par3], _paramManager.Parameters[(int)par4]);
                        break;
                    case Parameter.EnumType.TimeDependentExponential:
                        thisParameter = new TimeDependentExponential(parameterID, name,
                             _paramManager.Parameters[(int)par1], _paramManager.Parameters[(int)par2],
                             _paramManager.Parameters[(int)par3], _paramManager.Parameters[(int)par4]);
                        break;
                    case Parameter.EnumType.TimeDependentSigmoid:
                        thisParameter = new TimeDependentSigmoid(parameterID, name,
                             _paramManager.Parameters[(int)par1], _paramManager.Parameters[(int)par2],
                             _paramManager.Parameters[(int)par3]);
                        break;
                    case Parameter.EnumType.ComorbidityDisutility:
                        thisParameter = new ComorbidityDisutility(parameterID, name,
                            _paramManager.Parameters[(int)par1], _paramManager.Parameters[(int)par2]);
                        break;
                    default: // indepedent
                        {
                            EnumRandomVariates enumRVG = RandomVariateLib.SupportProcedures.ConvertToEnumRVG(distribution);
                            thisParameter = new IndependetParameter(parameterID, name, enumRVG, par1, par2, par3, par4);
                        }
                        break;
                }

                thisParameter.ShouldBeUpdatedByTime = updateAtEachTimeStep;
                thisParameter.IncludedInCalibration = includedInCalibration;

                // add the parameter
                _paramManager.Add(thisParameter);
            }
        }             

        // add classes
        public void AddClasses()
        {
            Array classesSheet = _modelSets.Sheets.ClassesSheet;

            for (int rowIndex = 1; rowIndex <= classesSheet.GetLength(0); ++rowIndex)
            {
                // ID and Name
                int classID = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ID));
                string name = Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.Name));
                // class type
                string strClassType = Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ClassType));

                // DALY loss and cost outcomes
                bool ifCollectOutcomes = false;
                int parID_DALYPerNewMember = 0; int parID_costPerNewMember = 0; int parID_healthQualityPerUnitOfTime = 0; int parID_costPerUnitOfTime = 0;
                ifCollectOutcomes = SupportFunctions.ConvertYesNoToBool(Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.IfCollectOutcomes)));
                if (ifCollectOutcomes)
                {
                    parID_DALYPerNewMember = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ParID_DALYPerNewMember));
                    parID_costPerNewMember = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ParID_CostPerNewMember));
                    parID_healthQualityPerUnitOfTime = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ParID_DisableWeightPerUnitOfTime));
                    parID_costPerUnitOfTime = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ParID_CostPerUnitOfTime));
                }

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
                            thisNormalClass.SetupInitialAndStoppingConditions(_paramManager.Parameters[initialMembersParID], emptyToEradicate);
                            // set up transmission dynamics properties                            
                            thisNormalClass.SetupTransmissionDynamicsProperties(
                                _paramManager.GetParameters(strSusceptibilityIDs), _paramManager.GetParameters(strInfectivityIDs), rowInContactMatrix);

                            // add class
                            _classes.Add(thisNormalClass);

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
                            int destClassIDGivenSuccess = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.DestinationClassIDIfSuccess));
                            int destClassIDGivenFailure = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.DestinationClassIDIfFailure));

                            // build the class
                            Class_Splitting thisSplittingClass = new Class_Splitting(classID, name);
                            thisSplittingClass.SetUp(_paramManager.Parameters[parIDForProbOfSuccess], destClassIDGivenSuccess, destClassIDGivenFailure);

                            // add class
                            _classes.Add(thisSplittingClass);
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
                _dicClasses[name] = classID;
                #endregion

                // set up class statistics and time series 
                SetupClassStatsAndTimeSeries(_classes.Last(), collectAccumIncidenceStats, collectPrevalenceStats, showIncidence, showPrevalence, showAccumIncidence);

                // adding cost and health outcomes
                if (ifCollectOutcomes)
                    AddCollectOutcomes(
                        thisClass: _classes.Last(),
                        DALY: _paramManager.Parameters[parID_DALYPerNewMember],
                        cost: _paramManager.Parameters[parID_costPerNewMember],
                        disabilityPerTime: _paramManager.Parameters[parID_healthQualityPerUnitOfTime],
                        costPerTime: _paramManager.Parameters[parID_costPerUnitOfTime]);

            }// end of for           
        }

        // set up class statistics and time series
        protected void SetupClassStatsAndTimeSeries(Class thisClass, 
            bool collectAccumIncidenceStats=false, 
            bool collectPrevalenceStats=false,
            bool showIncidence=false, 
            bool showPrevalence=false, 
            bool showAccumIncidence=false)
        {
            // class statistics 
            thisClass.ClassStat = new OneDimTrajectory(thisClass.ID, thisClass.Name, _modelSets.WarmUpPeriodSimTIndex);
            thisClass.ClassStat.SetupStatisticsCollectors(
                collectAccumIncidenceStats,
                collectPrevalenceStats
                );
            // adding time series
            thisClass.ClassStat.AddTimeSeries(
                collectIncidence: showIncidence,
                collectPrevalence: false,
                collectAccumIncidence: false,
                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval
                );
            // set up which statistics to show
            thisClass.ShowIncidence = showIncidence;
            thisClass.ShowPrevalence = showPrevalence;
            thisClass.ShowAccumIncidence = showAccumIncidence;
        }

        // set up class collect outcomes
        protected void AddCollectOutcomes(Class thisClass, Parameter DALY, Parameter cost, Parameter disabilityPerTime, Parameter costPerTime)
        {
            thisClass.ClassStat.DeltaCostHealthCollector
                = new DeltaTCostHealth(
                    _modelSets.DeltaT,
                    _modelSets.WarmUpPeriodSimTIndex,
                    DALY, cost, disabilityPerTime, costPerTime                          
                    );
        }


        // add events
        public void AddEvents()
        {
            Array eventSheet = _modelSets.Sheets.EventSheet;

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
                            Event_Birth thisEvent_Birth = new Event_Birth(name, ID, IDOfActivatingIntervention, _paramManager.Parameters[IDOfRateParameter], IDOfDestinationClass);
                            this._events.Add(thisEvent_Birth);
                        }
                        break;
                    case "Event: Epidemic-Independent":
                        {
                            int IDOfRateParameter = Convert.ToInt32(eventSheet.GetValue(rowIndex, (int)ExcelInterface.enumEventColumns.IDOfRateParameter));
                            // create the process
                            Event_EpidemicIndependent thisEvent_EpidemicIndependent = new Event_EpidemicIndependent(
                                name, ID, IDOfActivatingIntervention, _paramManager.Parameters[IDOfRateParameter], IDOfDestinationClass);
                            this._events.Add(thisEvent_EpidemicIndependent);
                        }
                        break;
                    case "Event: Epidemic-Dependent":
                        {
                            int IDOfPathogenToGenerate = Convert.ToInt32(eventSheet.GetValue(rowIndex, (int)ExcelInterface.enumEventColumns.IDOfGeneratingPathogen));
                            // create the process
                            Event_EpidemicDependent thisEvent_EpidemicDependent = new Event_EpidemicDependent(name, ID, IDOfActivatingIntervention, IDOfPathogenToGenerate, IDOfDestinationClass);
                            this._events.Add(thisEvent_EpidemicDependent);
                        }
                        break;
                }
                #endregion

            } // end of for
        }

        // add interventions
        public void AddInterventions()
        {
            //_useSameContactMatrixForAllDecisions = true;
            Array interventionsSheet = _modelSets.Sheets.InterventionSheet;

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
                        _FOIModel.AddIntrvnAffectingContacts(rowIndex - 1);

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
                        }
                        break;
                    case EnumDecisionRule.ConditionBased:
                        {
                            int conditionIDToTurnOn = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.ThresholdBased_ConditionIDToTurnOn));
                            int conditionIDToTurnOff = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.ThresholdBased_ConditionIDToTurnOff));
                            simDecisionRule = new DecisionRule_ConditionBased(_epiHist.Conditions, conditionIDToTurnOn, conditionIDToTurnOff);
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
                    timeIndexBecomesAvailable: (int)(timeBecomeAvailable / _modelSets.DeltaT),
                    timeIndexBecomesUnavailable: (int)(timeBecomeUnavailable / _modelSets.DeltaT),
                    parIDDelayToGoIntoEffectOnceTurnedOn: delayParID,
                    decisionRule: simDecisionRule);

                // set up cost
                thisIntervention.SetUpCost(fixedCost, costPerUnitOfTime, penaltyForSwitchingFromOnToOff);

                // add the intervention
                _decisionMaker.AddAnIntervention(thisIntervention);
            }

            // gather info
            //DecisionMaker.UpdateAfterAllInterventionsAdded();

        }

        // add summation statistics
        public void AddSummationStatistics()
        {
            Array summationStatisticsSheet = _modelSets.Sheets.SummationStatisticsSheet;

            if (summationStatisticsSheet == null) return;
            for (int rowIndex = 1; rowIndex <= summationStatisticsSheet.GetLength(0); ++rowIndex)
            {
                // common information between summation and ratio statistics
                CommonSumRatioStatistics info = new CommonSumRatioStatistics(summationStatisticsSheet, rowIndex);

                // ID and Name
                string strDefinedOn = Convert.ToString(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.DefinedOn));

                // defined on 
                SumTrajectory.EnumDefinedOn definedOn = SumTrajectory.EnumDefinedOn.Classes;
                if (strDefinedOn == "Events") definedOn = SumTrajectory.EnumDefinedOn.Events;

                // DALY and cost outcomes
                bool ifCollectOutcomes = false;
                int parID_DALYPerNewMember = 0; int parID_costPerNewMember = 0;
                ifCollectOutcomes = SupportFunctions.ConvertYesNoToBool(Convert.ToString(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.IfCollectOutcomes)));
                if (ifCollectOutcomes)
                {
                    parID_DALYPerNewMember = Convert.ToInt32(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.ParID_DALYPerNewMember));
                    parID_costPerNewMember = Convert.ToInt32(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.ParID_CostPerNewMember));
                }

                // build and add the summation statistics
                if (definedOn == SumTrajectory.EnumDefinedOn.Classes)
                {
                    SumClassesTrajectory thisSumClassTraj = new SumClassesTrajectory
                        (info.ID, info.Name, info.StrType, info.Formula, info.IfDisplay, _modelSets.WarmUpPeriodSimTIndex, _modelSets.NumOfDeltaT_inSimOutputInterval);
                    // add the summation statistics
                    _epiHist.SumTrajs.Add(thisSumClassTraj);
                    // add the survey 
                    if (info.SurveillanceDataAvailable)
                    {
                        // find the type of this summation statistics
                        switch (thisSumClassTraj.Type)
                        {
                            case SumTrajectory.EnumType.Incidence:
                                _epiHist.SurveyedIncidenceTrajs.Add(
                                    new SurveyedIncidenceTrajectory(
                                        id: info.ID,
                                        name: info.Name,
                                        displayInSimOutput: info.IfDisplay,
                                        firstObsMarksStartOfEpidemic: info.FirstObsMarksEpiStart,
                                        sumClassesTrajectory: thisSumClassTraj,
                                        sumEventTrajectory: null,
                                        ratioTrajectory: null,
                                        nDeltaTsObsPeriod: _modelSets.NumOfDeltaT_inObservationPeriod,
                                        nDeltaTsDelayed: info.NDeltaTDelayed,
                                        noise_nOfDemoninatorSampled: _paramManager.Parameters[info.ParIDNoiseDenominator])
                                        );
                                break;
                            case SumTrajectory.EnumType.AccumulatingIncident:
                            case SumTrajectory.EnumType.Prevalence:
                                _epiHist.SurveyedPrevalenceTrajs.Add(
                                    new SurveyedPrevalenceTrajectory(
                                        id: info.ID,
                                        name: info.Name,
                                        displayInSimOutput: info.IfDisplay,
                                        firstObsMarksStartOfEpidemic: info.FirstObsMarksEpiStart,
                                        sumClassesTrajectory: thisSumClassTraj,
                                        ratioTrajectory: null,
                                        nDeltaTsObsPeriod: _modelSets.NumOfDeltaT_inObservationPeriod,
                                        nDeltaTsDelayed: info.NDeltaTDelayed,
                                        noise_percOfDemoninatorSampled: _paramManager.Parameters[info.ParIDNoiseDenominator])
                                        );
                                break;
                        }
                    }

                    // update class time-series
                    UpdateClassTimeSeries(thisSumClassTraj);

                }
                else // if defined on events
                {
                    SumEventTrajectory thisSumEventTraj = new SumEventTrajectory
                        (info.ID, info.Name, info.StrType, info.Formula, info.IfDisplay, _modelSets.WarmUpPeriodSimTIndex, _modelSets.NumOfDeltaT_inSimOutputInterval);
                    // add the summation statistics
                    _epiHist.SumTrajs.Add(thisSumEventTraj);

                    // add the survey 
                    if (info.SurveillanceDataAvailable)
                    {
                        // find the type of this summation statistics
                        switch (thisSumEventTraj.Type)
                        {
                            case SumTrajectory.EnumType.Incidence:
                                _epiHist.SurveyedIncidenceTrajs.Add(
                                    new SurveyedIncidenceTrajectory(
                                        info.ID,
                                        info.Name,
                                        info.IfDisplay,
                                        info.FirstObsMarksEpiStart,
                                        null,
                                        thisSumEventTraj,
                                        null,
                                        _modelSets.NumOfDeltaT_inObservationPeriod,
                                        info.NDeltaTDelayed,
                                        noise_nOfDemoninatorSampled: _paramManager.Parameters[info.ParIDNoiseDenominator])
                                        );
                                break;
                            case SumTrajectory.EnumType.AccumulatingIncident:
                            case SumTrajectory.EnumType.Prevalence:
                                break;
                        }
                    }
                }

                // adding cost and health outcomes
                if (ifCollectOutcomes)
                    _epiHist.SumTrajs.Last().DeltaCostHealthCollector =
                        new DeltaTCostHealth(
                            _modelSets.DeltaT,
                            _modelSets.WarmUpPeriodSimTIndex,
                            _paramManager.Parameters[parID_DALYPerNewMember],
                            _paramManager.Parameters[parID_costPerNewMember]);

                // update calibraton infor
                if (_modelSets.ModelUse == EnumModelUse.Calibration && info.IfIncludedInCalibration)
                    _epiHist.SumTrajs.Last().CalibInfo =
                        new SpecialStatCalibrInfo(
                            info.StrMeasureOfFit,
                            info.StrLikelihood,
                            info.StrLikelihoodParam,
                            info.IfCheckWithinFeasibleRange,
                            info.FeasibleMin,
                            info.FeasibleMax,
                            info.FeasibleMinThreshodToHit);

            }

            // identify sum statistics for which time-series should be collected
            UpdateSumStatTimeSeries();
        }

        // add ratio statistics
        public void AddRatioStatistics()
        {
            Array ratioStatsSheet = _modelSets.Sheets.RatioStatisticsSheet;

            if (ratioStatsSheet == null) return;

            for (int rowIndex = 1; rowIndex <= ratioStatsSheet.GetLength(0); ++rowIndex)
            {
                // common information between summation and ratio statistics
                CommonSumRatioStatistics info = new CommonSumRatioStatistics(ratioStatsSheet, rowIndex);

                // build a ratio stat
                RatioTrajectory thisRatioTraj = new RatioTrajectory(
                    info.ID,
                    info.Name,
                    info.StrType,
                    info.Formula,
                    info.IfDisplay,
                    _modelSets.WarmUpPeriodSimTIndex,
                    _modelSets.NumOfDeltaT_inSimOutputInterval);

                // set up calibration
                if (_modelSets.ModelUse == EnumModelUse.Calibration && info.IfIncludedInCalibration)
                    thisRatioTraj.CalibInfo = new SpecialStatCalibrInfo(
                        info.StrMeasureOfFit,
                        info.StrLikelihood,
                        info.StrLikelihoodParam,
                        info.IfCheckWithinFeasibleRange,
                        info.FeasibleMin,
                        info.FeasibleMax,
                        info.FeasibleMinThreshodToHit);

                // add the ratio statistics
                _epiHist.RatioTrajs.Add(thisRatioTraj);

                // add the survey 
                if (info.SurveillanceDataAvailable)
                {
                    switch (thisRatioTraj.Type)
                    {
                        case RatioTrajectory.EnumType.PrevalenceOverPrevalence:
                        case RatioTrajectory.EnumType.AccumulatedIncidenceOverAccumulatedIncidence:
                            _epiHist.SurveyedPrevalenceTrajs.Add(
                                new SurveyedPrevalenceTrajectory(
                                    info.ID,
                                    info.Name,
                                    info.IfDisplay,
                                    info.FirstObsMarksEpiStart,
                                    null,
                                    thisRatioTraj,
                                    _modelSets.NumOfDeltaT_inObservationPeriod,
                                    info.NDeltaTDelayed,
                                     _paramManager.Parameters[info.ParIDNoiseDenominator])
                                    );
                            break;
                        case RatioTrajectory.EnumType.IncidenceOverIncidence:
                        case RatioTrajectory.EnumType.IncidenceOverPrevalence:
                            _epiHist.SurveyedIncidenceTrajs.Add(
                                new SurveyedIncidenceTrajectory(
                                    info.ID,
                                    info.Name,
                                    info.IfDisplay,
                                    info.FirstObsMarksEpiStart,
                                    null,
                                    null,
                                    thisRatioTraj,
                                    _modelSets.NumOfDeltaT_inObservationPeriod,
                                    info.NDeltaTDelayed,
                                     _paramManager.Parameters[info.ParIDNoiseDenominator])
                                );
                            break;
                    }

                }
            }

            // identify ratio statistics for which time-series should be collected
            UpdateRatioStatTimeSeries();
        }

        // add features
        public void AddFeatures()
        {
            Array featuresSheet = _modelSets.Sheets.FeaturesSheet;

            for (int rowIndex = 1; rowIndex <= featuresSheet.GetLength(0); ++rowIndex)
            {
                int id = Convert.ToInt32(featuresSheet.GetValue(rowIndex, (int)ExcelInterface.enumFeaturesColumns.ID));
                string name = Convert.ToString(featuresSheet.GetValue(rowIndex, (int)ExcelInterface.enumFeaturesColumns.Name));
                string strFeatureObs = Convert.ToString(featuresSheet.GetValue(rowIndex, (int)ExcelInterface.enumFeaturesColumns.FeatureObservation));

                int specialStatID;
                string strSpecialStatFeatureType;
                int parID;
                int interventionID;
                string strInterventionFeatureType;

                if (strFeatureObs == "Time")
                {
                    // create a feature
                    _epiHist.Features.Add(new Feature_EpidemicTime(name, id));
                }
                else if (strFeatureObs == "Special Statistics")
                {
                    specialStatID = Convert.ToInt32(featuresSheet.GetValue(rowIndex, (int)ExcelInterface.enumFeaturesColumns.SpecialStatID));
                    strSpecialStatFeatureType = Convert.ToString(featuresSheet.GetValue(rowIndex, (int)ExcelInterface.enumFeaturesColumns.SpecialStatFeatureType));
                    parID = Convert.ToInt32(featuresSheet.GetValue(rowIndex, (int)ExcelInterface.enumFeaturesColumns.Par));
                    // create a feature
                    _epiHist.AddASpecialStatisticsFeature(name, id, specialStatID, strSpecialStatFeatureType, 
                        _paramManager.Parameters[parID]);
                }
                else if (strFeatureObs == "Intervention")
                {
                    interventionID = Convert.ToInt32(featuresSheet.GetValue(rowIndex, (int)ExcelInterface.enumFeaturesColumns.InterventionID));
                    strInterventionFeatureType = Convert.ToString(featuresSheet.GetValue(rowIndex, (int)ExcelInterface.enumFeaturesColumns.InterventionFeatureType));
                    // create a feature
                    _epiHist.Features.Add(new Feature_Intervention(name, id, strInterventionFeatureType, _decisionMaker.Interventions[interventionID]));
                }
            }
        }

        // add conditions
        public void AddConditions()
        {
            Array conditionsSheet = _modelSets.Sheets.ConditionsSheet;

            for (int rowIndex = 1; rowIndex <= conditionsSheet.GetLength(0); ++rowIndex)
            {
                int id = Convert.ToInt32(conditionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumConditionsColumns.ID));
                string name = Convert.ToString(conditionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumConditionsColumns.Name));
                string strDefinedOn = Convert.ToString(conditionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumConditionsColumns.DefinedOn));

                switch (strDefinedOn)
                {
                    case "Features":
                        {                            
                            string strFeatureIDs = Convert.ToString(conditionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumConditionsColumns.FeatureIDs));                            
                            string strSigns = Convert.ToString(conditionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumConditionsColumns.FeatureSigns));
                            string strThresholds = Convert.ToString(conditionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumConditionsColumns.FeatureThresholds));
                            string strConclusion = Convert.ToString(conditionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumConditionsColumns.FeatureConclusion));
                            _epiHist.Conditions.Add(new Condition_OnFeatures(id, name, _epiHist.Features, _paramManager.Parameters, strFeatureIDs, strThresholds, strSigns, strConclusion));
                        }
                        break;
                    case "Conditions":
                        {
                            string strConditions = Convert.ToString(conditionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumConditionsColumns.Conditions));
                            string strConclusion = Convert.ToString(conditionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumConditionsColumns.ConditionsConclusions));
                            _epiHist.Conditions.Add(new Condition_OnConditions(id, name, _epiHist.Conditions, strConditions, strConclusion));
                        }
                        break;
                    case "Always True":
                        _epiHist.Conditions.Add(new Condition_AlwaysTrue(id, name));
                        break;
                    case "Always False":
                        _epiHist.Conditions.Add(new Condition_AlwaysFalse(id, name));
                        break;
                }
            }
        }

        // add connections
        public void AddConnections()
        {
            int[,] connectionsMatrix = _modelSets.Sheets.ConnectionsMatrix;

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

        // update class time series
        public void UpdateClassTimeSeries(SumClassesTrajectory thisSumClassTraj)
        {
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

        // update sum statistics for which time-series should be collected
        public void UpdateSumStatTimeSeries()
        {
            if (_modelSets.ModelUse == EnumModelUse.Calibration)
                foreach (SumTrajectory st in _epiHist.SumTrajs.Where(s => !(s.CalibInfo is null)))
                {
                    switch (st.Type)
                    {
                        case SumTrajectory.EnumType.Incidence:
                            st.AddTimeSeries(
                                collectIncidence: true,
                                collectPrevalence: false,
                                collectAccumIncidence: false,
                                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inObservationPeriod
                                );
                            break;
                        case SumTrajectory.EnumType.AccumulatingIncident:
                            st.AddTimeSeries(
                                collectIncidence: false,
                                collectPrevalence: false,
                                collectAccumIncidence: true,
                                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inObservationPeriod
                                );
                            break;
                        case SumTrajectory.EnumType.Prevalence:
                            st.AddTimeSeries(
                                collectIncidence: false,
                                collectPrevalence: true,
                                collectAccumIncidence: false,
                                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inObservationPeriod
                                );
                            break;
                    }

                    if (st.CalibInfo.GoodnessOfFit == SpecialStatCalibrInfo.EnumMeasureOfFit.Likelihood)
                    {
                        switch (st.CalibInfo.LikelihoodFunc)
                        {
                            case SpecialStatCalibrInfo.EnumLikelihoodFunc.Binomial:
                                {
                                    _epiHist.SumTrajs[st.CalibInfo.LikelihoodParam.Value].AddTimeSeries(
                                        collectIncidence: false,
                                        collectPrevalence: true,
                                        collectAccumIncidence: false,
                                        nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inObservationPeriod
                                        );
                                }
                                break;
                        }
                    }
                }
        }

        // update ratio statistics for which time-series should be collected
        public void UpdateRatioStatTimeSeries()
        {
            if (_modelSets.ModelUse == EnumModelUse.Calibration)
                foreach (RatioTrajectory rt in _epiHist.RatioTrajs.Where(s => !(s.CalibInfo is null)))
                {
                    if (rt.CalibInfo.GoodnessOfFit == SpecialStatCalibrInfo.EnumMeasureOfFit.Likelihood)
                    {
                        switch (rt.CalibInfo.LikelihoodFunc)
                        {
                            case SpecialStatCalibrInfo.EnumLikelihoodFunc.Binomial:
                                {
                                    switch (rt.Type)
                                    {
                                        case RatioTrajectory.EnumType.PrevalenceOverPrevalence:
                                            {
                                                _epiHist.SumTrajs[rt.NominatorSpecialStatID].AddTimeSeries(
                                                    collectIncidence: false,
                                                    collectPrevalence: true,
                                                    collectAccumIncidence: false,
                                                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inObservationPeriod
                                                    );
                                                _epiHist.SumTrajs[rt.DenominatorSpecialStatID].AddTimeSeries(
                                                    collectIncidence: false,
                                                    collectPrevalence: true,
                                                    collectAccumIncidence: false,
                                                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inObservationPeriod
                                                    );
                                            }
                                            break;
                                        case RatioTrajectory.EnumType.IncidenceOverIncidence:
                                            {
                                                _epiHist.SumTrajs[rt.NominatorSpecialStatID].AddTimeSeries(
                                                    collectIncidence: true,
                                                    collectPrevalence: false,
                                                    collectAccumIncidence: false,
                                                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inObservationPeriod
                                                    );
                                                _epiHist.SumTrajs[rt.DenominatorSpecialStatID].AddTimeSeries(
                                                    collectIncidence: true,
                                                    collectPrevalence: false,
                                                    collectAccumIncidence: false,
                                                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inObservationPeriod
                                                    );
                                            }
                                            break;
                                        case RatioTrajectory.EnumType.IncidenceOverPrevalence:
                                            {
                                                _epiHist.SumTrajs[rt.NominatorSpecialStatID].AddTimeSeries(
                                                    collectIncidence: true,
                                                    collectPrevalence: false,
                                                    collectAccumIncidence: false,
                                                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inObservationPeriod
                                                    );
                                                _epiHist.SumTrajs[rt.DenominatorSpecialStatID].AddTimeSeries(
                                                    collectIncidence: false,
                                                    collectPrevalence: true,
                                                    collectAccumIncidence: false,
                                                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inObservationPeriod
                                                    );
                                            }
                                            break;
                                    }
                                }
                                break;
                        }
                    }
                }
        }
    }
}
