using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputationLib;
using RandomVariateLib;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace APACElib
{
    public class APACE
    {
        private ExcelInterface _excelInterface;
        public ExcelInterface ExcelIntface { get => _excelInterface; set => _excelInterface = value; }
        private ModelSettings _modelSettings = new ModelSettings();
        public ModelSettings ModelSetting { get => _modelSettings; }
        private List<ModelInstruction> _listModelInstr = new List<ModelInstruction>();
        private EpidemicModeller _epidemicModeller;
        private ArrayList _epidemicModellers = new ArrayList();
        private DateTime dt;
        private StatusBox _statusBox;

        // computation time
        private double _actualTimeUsedToFindAllDynamicPolicies; // considering several ADP parameter designs
        private ObsBasedStat _obsTotalSimulationTimeToEvaluateOneStaticPolicy = new ObsBasedStat("");
        private ObsBasedStat _obsTimeUsedToFindOneStaticPolicy = new ObsBasedStat("");
        private ObsBasedStat _obsRatioOfTimeUsedToFindOneStaticPolicyToTotalSimulationTimeToEvaluateOneStaticPolicy = new ObsBasedStat("");
        private ObsBasedStat _obsTotalSimulationTimeToFindOneDynamicPolicy = new ObsBasedStat("");
        private ObsBasedStat _obsTimeUsedToFindOneDynamicPolicy = new ObsBasedStat("");
        private ObsBasedStat _obsRatioOfTimeUsedToFindOneDynamicPolicyToTotalSimulationTimeToFindOneDynamicPolicy = new ObsBasedStat("");

        private ObsBasedStat _obsRealTimeSimulation_staticOptimization_totalCalibrationTime = new ObsBasedStat("");
        private ObsBasedStat _obsRealTimeSimulation_staticOptimization_totalOptimizationTime = new ObsBasedStat("");
        private ObsBasedStat _obsRealTimeSimulation_staticOptimization_totalCalibrationTimeOverAverageSimulationTime = new ObsBasedStat("");
        private ObsBasedStat _obsRealTimeSimulation_staticOptimization_totalOptimizationTimeOverAverageSimulationTime = new ObsBasedStat("");        
        private ObsBasedStat _obsRealTimeSimulation_dynamicOptimization_totalCalibrationTime = new ObsBasedStat("");
        private ObsBasedStat _obsRealTimeSimulation_dynamicOptimization_totalOptimizationTime = new ObsBasedStat("");
        private ObsBasedStat _obsRealTimeSimulation_dynamicOptimization_totalCalibrationTimeOverAverageSimulationTime = new ObsBasedStat("");
        private ObsBasedStat _obsRealTimeSimulation_dynamicOptimization_totalOptimizationTimeOverAverageSimulationTime = new ObsBasedStat("");

        // connect to the excel interface
        public void ConnectToExcelInteface()
        {
            ExcelIntface = new ExcelInterface();
            ExcelIntface.ConnectToExcelInterface();
            ExcelIntface.Visible = true;
            // read model settings
            _modelSettings.ReadSettings(ref _excelInterface);
        }

        // run
        public void Run(string model="None", RichTextBox textBox=null)
        {
            dt = DateTime.Now;
            _statusBox = new StatusBox(textBox);

            // read model settings
            _modelSettings.ReadSettings(ref _excelInterface);

            _listModelInstr = null;
            if (model == "Gonorrhea")
            {
                _listModelInstr = new List<ModelInstruction>();
                for (int i = 0; i < ModelSetting.GetNumModelsToBuild(); i++)
                    _listModelInstr.Add(new GonoModel());
            }

            // find the task            
            switch (ExcelIntface.GetWhatToDo())
            {
                case ExcelInterface.enumWhatToDo.Simulate:
                    {
                        // simulate a policy
                        _statusBox.AddText("Simulation started.");
                        // Console.WriteLine(dt.ToString() + ": Simulating a policy.");
                        SimulateAPolicy();
                        _statusBox.AddText("Simulation ended.");
                        break;
                    }
                case ExcelInterface.enumWhatToDo.Calibrate:
                    {
                        // calibrate
                        _statusBox.AddText("Calibration started.");
                        Calibrate();
                        _statusBox.AddText("Calibration ended.");
                        break;
                    }
                case ExcelInterface.enumWhatToDo.OptimizeTheDynamicPolicy:
                    {
                       // optimize the dynamic policy
                        OptimizeTheDynamicPolicy();
                        break;
                    }
                case ExcelInterface.enumWhatToDo.Optimize:
                    {
                        // optimize 
                        Optimize();
                        break;
                    }
                case ExcelInterface.enumWhatToDo.RunExperiments:
                    {
                        // run experiments
                        RunExperiments();
                        break;
                    }                   
            }
            // make a noise!
            System.Media.SystemSounds.Beep.Play();
        }

        // simulate a policy
        private void SimulateAPolicy()
        {
            // create an epidemic modeler
            _epidemicModeller = new EpidemicModeller(0, _excelInterface, _modelSettings, _listModelInstr);

            // setup simulation output sheet
            SetupSimulationOutputSheet();

            // simulate epidemics
            _epidemicModeller.BuildAndSimulateEpidemics();

            // report simulation results
            ReportTrajsAndSimStats(_epidemicModeller);
        }

        // calibrate
        private void Calibrate()
        {
            // making sure seeds are incrementing from 0
            _modelSettings.SimRNDSeedsSource = EnumSimRNDSeedsSource.StartFrom0;

            // create an epidemic modeler
            _epidemicModeller = new EpidemicModeller(0, _excelInterface, _modelSettings, _listModelInstr);

            // read the observed epidemic history 
            ReadObservedHistory();

            // calibrate
            _epidemicModeller.Calibrate(_statusBox);
            
            // report calibration results
            ReportCalibrationResult();
        }

        private void Optimize()
        {
            // read optimization settings
            _modelSettings.ReadOptimizationSettings(ref _excelInterface);

            // making sure seeds are correctly sampled
            if (_modelSettings.SimRNDSeedsSource == EnumSimRNDSeedsSource.Prespecified)
                _modelSettings.SimRNDSeedsSource = EnumSimRNDSeedsSource.RandomUnweighted;

            //OptimizeGonohrreaRandomizedWTP optimizer = new OptimizeGonohrreaRandomizedWTP();
            OptimizeGonohrrea_StructuredPolicy optimizer = new OptimizeGonohrrea_StructuredPolicy();
            optimizer.Run(_excelInterface, _modelSettings, _listModelInstr);

            ExcelIntface.ReportOptimization(optimizer.Summary);
        }
        
        // optimize the dynamic policy
        private void OptimizeTheDynamicPolicy()
        {
            // read optimization settings
            _modelSettings.ReadADPOptimizationSettings(ref _excelInterface);

            // initialize dynamic policy optimization
            InitializeDynamicPolicyOptimization();
            // set up ADP parameter designs
            _modelSettings.SetUpADPParameterDesigns();
            // optimize epidemics
            BuildAndOptimizeEachEpidemicModeller_DynamicPolicyOptimization(true, _modelSettings.EpidemicTimeIndexToStartDecisionMaking, 0);

            // single value for wtp for health
            if (Math.Abs(_modelSettings.OptmzSets.WTP_min - _modelSettings.OptmzSets.WTP_max) < SupportProcedures.minimumWTPforHealth)
            {
                double harmonicStep_a = 0, epsilonGreedy_beta = 0;
                // find the optimal epidemic modeler
                EpidemicModeller optimalEpidemicModeller = FindOptimalEpiModeller_DynamicPolicy(_modelSettings.OptmzSets.WTP_min, ref harmonicStep_a, ref epsilonGreedy_beta);
                // report optimization result
                ReportADPResultsForThisEpidemic(optimalEpidemicModeller, harmonicStep_a, epsilonGreedy_beta);
                // report simulation result
                ReportTrajsAndSimStats(optimalEpidemicModeller);
            }
            else
            {
                // report dynamic policy optimization results
                ReportADPResultsForAllEpidemic();
            }

            // report computation time
            ReportADPComputationTimes();
        }       
        // optimize the static policy
        private void OptimizeTheStaticPolicy()
        {
            // read optimization settings
            _modelSettings.ReadADPOptimizationSettings(ref _excelInterface);

            ArrayList staticPolicyDesigns = new ArrayList();
            // build a temp modeler to find the available decisions
            EpidemicModeller tempEpidemicModeller = new EpidemicModeller(0, _excelInterface, _modelSettings, _listModelInstr);

            // get interval-based static policy designs
            //staticPolicyDesigns = tempEpidemicModeller.GetIntervalBasedStaticPoliciesDesigns();

            // evaluate the defined interval-based static policies
            //BuildAndSimulateEpidemicModellersToEvaluateIntervalBasedStaticPolicies
                // (staticPolicyDesigns, 0);

            // report static policy optimization
            ReportStaticPolicyOptimizationResults(staticPolicyDesigns);
        }        

        // run experiments
        private void RunExperiments()
        {
            double[][] mainSimOutcomes = new double[0][];   // cost, health, and objective function
            string[] simItrOutcomeLabels = new string[0];   // labels of all simulation outcomes
            List<string> simItrScenarioNames = new List<string>();  // list of scenario names
            double[][] simItrVarAndObjFunValues = new double[0][];  // variables and objective function for all simulation runs and scenarios 
            double[][] simItrOutcomes = new double[0][];    

            // read model variable names to perform senstivity analysis on
            ExcelIntface.ActivateSheet("Experimental Designs");
            int firstRow = ExcelIntface.RowIndex("baseExperimentalDesignsVariableValues");
            int firstCol = ExcelIntface.ColIndex("baseExperimentalDesignsVariableValues") + 1;
            int lastRow = firstRow;
            int lastCol = ExcelIntface.ColIndex("baseExperimentalDesignsVariableValues", ExcelInteractorLib.ExcelInteractor.enumRangeDirection.RightEnd);
            string[] varNames = ExcelIntface.ReadStringRangeFromActiveSheet(firstRow, firstCol, lastRow, lastCol);
            
            // read designs
            double[,] experimentDesigns = ExcelIntface.GetExperimentalDesignMatrix();

            // read scenario names
            firstRow = ExcelIntface.RowIndex("baseExperimentalDesigns") + 1;
            firstCol = ExcelIntface.ColIndex("baseExperimentalDesigns");
            lastRow = ExcelIntface.RowIndex("baseExperimentalDesigns", ExcelInteractorLib.ExcelInteractor.enumRangeDirection.DownEnd);
            lastCol = firstCol;
            string[] scenarioNames = ExcelIntface.ReadStringRangeFromActiveSheet(firstRow, firstCol, lastRow, lastCol);

            // build epidemic modelers            
            _epidemicModellers.Clear();
            int numOfDesigns = experimentDesigns.GetLength(0);
            int numOfVars = experimentDesigns.GetLength(1);
            for (int designIndex = 0; designIndex < numOfDesigns; designIndex++)
            {
                // find the design
                double[] thisDesign = new double[numOfVars];
                for (int varIndex = 0; varIndex < numOfVars; varIndex++)
                    thisDesign[varIndex] = experimentDesigns[designIndex, varIndex];
                // write back the design
                ExcelIntface.WriteADesign(thisDesign);
                // recalculate Excel worksheet
                ExcelIntface.Recalculate();
                // read model settings
                _modelSettings.ReadSettings(ref _excelInterface);
                // create and epidemic modeler
                EpidemicModeller thisEpidemicModeller = new EpidemicModeller(designIndex, _excelInterface, _modelSettings, _listModelInstr);
                // don't store epidemic trajectories
                thisEpidemicModeller.StoreEpiTrajsForExcelOutput(false);
                // run the simulation
                thisEpidemicModeller.BuildAndSimulateEpidemics();

                // concatenate main simulation outcomes (cost, health, and objective function) 
                mainSimOutcomes = SupportFunctions.ConcatJaggedArray(mainSimOutcomes, ExtractMainSimOutcomes(thisEpidemicModeller));

                // simulation iterations
                // first read variable values and the objective function samples
                double[][] thisSimItrVarAndObjFunValues = new double[_modelSettings.NumOfSimItrs][];
                for (int simItr = 0; simItr < _modelSettings.NumOfSimItrs; simItr++)
                {
                    thisSimItrVarAndObjFunValues[simItr] = new double[numOfVars + 1]; // 1 for the objective function 
                    // scenario name
                    simItrScenarioNames.Add(scenarioNames[designIndex]);
                    // this design
                    for (int varIndex = 0; varIndex < numOfVars; varIndex++)
                        thisSimItrVarAndObjFunValues[simItr][varIndex] = experimentDesigns[designIndex, varIndex];
                    // objective function
                    if (_modelSettings.ObjectiveFunction == EnumObjectiveFunction.MaximizeNHB)
                        thisSimItrVarAndObjFunValues[simItr][numOfVars + 0]
                            = -thisEpidemicModeller.SimSummary.DALYs[simItr] - thisEpidemicModeller.SimSummary.Costs[simItr] 
                            / Math.Max(_modelSettings.WTPForHealth, SupportProcedures.minimumWTPforHealth);
                    else
                        thisSimItrVarAndObjFunValues[simItr][numOfVars + 0]
                            = -_modelSettings.WTPForHealth* thisEpidemicModeller.SimSummary.DALYs[simItr] 
                            - thisEpidemicModeller.SimSummary.Costs[simItr];

                }
                // then read all simulation outcomes 
                double[][] thisSimItrOutcomes = new double[0][];
                thisEpidemicModeller.SimSummary.GetIndvEpidemicOutcomes(
                    ref simItrOutcomeLabels, 
                    ref thisSimItrOutcomes);

                // concatenate
                simItrVarAndObjFunValues = SupportFunctions.ConcatJaggedArray(
                    simItrVarAndObjFunValues, 
                    thisSimItrVarAndObjFunValues
                    );
                simItrOutcomes = SupportFunctions.ConcatJaggedArray(
                    simItrOutcomes, 
                    thisSimItrOutcomes);
            }

            // report summary statistics
            ExcelIntface.ReportSummaryOutcomes(
                "Experimental Designs", "baseExperimentalDesignsResults",
                SupportFunctions.ConvertJaggedArrayToRegularArray(
                    mainSimOutcomes, 6), 
                _modelSettings.ObjectiveFunction
                );

            // report outcomes for each run for each design
            ExcelIntface.ReportSAReplicationOutcomes(
                strVarNames: varNames,
                strSimItrOutcomeLabels: simItrOutcomeLabels,
                strScenarios: simItrScenarioNames.ToArray(),
                varAndObjFuncValues: SupportFunctions.ConvertJaggedArrayToRegularArray(
                    simItrVarAndObjFunValues,
                    numOfVars + 1),
                simItrOutcomes: SupportFunctions.ConvertJaggedArrayToRegularArray(
                    simItrOutcomes,
                    simItrOutcomes[0].Length),
                objectiveFunction: _modelSettings.ObjectiveFunction
                    );
        }

        // dynamic policy optimization for several epidemics 
        private void BuildAndOptimizeEachEpidemicModeller_DynamicPolicyOptimization(bool storeEpidemicTrajectories,
            double epidemicTimeToStartDecisionMaking, double warmUpPeriod)
        {
            // computation time
            int optStartTime, optEndTime;
            optStartTime = Environment.TickCount;
            _obsTotalSimulationTimeToFindOneDynamicPolicy.Reset();
            _obsTimeUsedToFindOneDynamicPolicy.Reset();
            _obsRatioOfTimeUsedToFindOneDynamicPolicyToTotalSimulationTimeToFindOneDynamicPolicy.Reset();

            // build epidemic modelers
            _epidemicModellers.Clear();

            // use parallel computing? 
            if (_modelSettings.UseParallelComputing == false)
            {
                #region Use sequential computing
                for (int epiID = 0; epiID < _modelSettings.AdpParameterDesigns.Length; epiID++)
                {
                    // build an epidemic model
                    EpidemicModeller thisEpidemicModeller
                        = GetAnEpidemicModellerForOptimizatingDynamicPolicies(epiID,
                        _modelSettings.AdpParameterDesigns[epiID][(int)enumADPParameter.WTPForHealth],
                        _modelSettings.AdpParameterDesigns[epiID][(int)enumADPParameter.HarmonicStepSize_a],
                        _modelSettings.AdpParameterDesigns[epiID][(int)enumADPParameter.EpsilonGreedy_beta]);

                    // update time to start decision making and storing outcomes
                    //thisEpidemicModeller.UpdateTheTimeToStartDecisionMakingAndWarmUpPeriod
                    //    (epidemicTimeToStartDecisionMaking, warmUpPeriod);

                    // add this epidemic modeler
                    _epidemicModellers.Add(thisEpidemicModeller);
                }
                // run the epidemics
                foreach (EpidemicModeller thisEpiModeller in _epidemicModellers)
                {
                    // find the dynamic Policy
                    //thisEpiModeller.FindOptimalDynamicPolicy();
                    // simulate the dynamic Policy
                    thisEpiModeller.SimulateTheOptimalDynamicPolicy(_modelSettings.NumOfSimItrs, _modelSettings.TimeIndexToStop,
                        _modelSettings.WarmUpPeriodSimTIndex, storeEpidemicTrajectories);
                }
                #endregion
            }
            else // (_useParallelComputing == true)
            {
                #region Use parallel computing
                Object thisLock = new Object();
                var options = new ParallelOptions() { MaxDegreeOfParallelism = _modelSettings.MaxDegreeOfParallelism};
                
                Parallel.For(0, _modelSettings.AdpParameterDesigns.Length, options, epiID =>
                {
                    // build an epidemic model
                    EpidemicModeller thisEpidemicModeller
                        = GetAnEpidemicModellerForOptimizatingDynamicPolicies(epiID,
                        _modelSettings.AdpParameterDesigns[epiID][(int)enumADPParameter.WTPForHealth],
                        _modelSettings.AdpParameterDesigns[epiID][(int)enumADPParameter.HarmonicStepSize_a],
                        _modelSettings.AdpParameterDesigns[epiID][(int)enumADPParameter.EpsilonGreedy_beta]);

                    // update time to start decision making and storing outcomes
                    //thisEpidemicModeller.UpdateTheTimeToStartDecisionMakingAndWarmUpPeriod
                    //    (epidemicTimeToStartDecisionMaking, warmUpPeriod);

                    // add this epidemic modeler
                    lock (thisLock)
                    {
                        _epidemicModellers.Add(thisEpidemicModeller);
                    }
                });                
                // run the epidemics
                Parallel.ForEach(_epidemicModellers.Cast<object>(), options, thisEpiModeller =>
                {
                    // find the dynamic Policy
                    //((EpidemicModeller)thisEpiModeller).FindOptimalDynamicPolicy();
                    // simulate the dynamic Policy
                    ((EpidemicModeller)thisEpiModeller).SimulateTheOptimalDynamicPolicy(_modelSettings.NumOfSimItrs, _modelSettings.TimeIndexToStop,
                        _modelSettings.WarmUpPeriodSimTIndex, storeEpidemicTrajectories);
                });     
                
                #endregion
            }

            // computation time
            optEndTime = Environment.TickCount;
            _actualTimeUsedToFindAllDynamicPolicies = (optEndTime - optStartTime) / 1000;
        }        
        
        // find the epidemic modeler with optimal adaptive policy
        private EpidemicModeller FindOptimalEpiModeller_DynamicPolicy(double forThisWTPforHealth, ref double optHarmonicStep_a, ref double optEpsilonGreedy_beta)
        {
            //double maxMean = double.MinValue;
            //double maxLowerBound = double.MinValue;
            //double maxUpperBound = double.MinValue;
            //double mean, lowerBound, upperBound;

            int IDOfOptimalEpiModeller = 0;
            int position = 0, positionOfOptimalEpiModeller = 0;
            // find the objective function of each epidemic modeler 
            foreach (EpidemicModeller thisEpiModeller in _epidemicModellers)
            {
                if (Math.Abs(_modelSettings.AdpParameterDesigns[thisEpiModeller.ID][(int)enumADPParameter.WTPForHealth] - forThisWTPforHealth) 
                    < SupportProcedures.minimumWTPforHealth)
                {
                    //mean = thisEpiModeller.GetObjectiveFunction_Mean(_modelSettings.ObjectiveFunction);
                    //lowerBound = thisEpiModeller.GetObjectiveFunction_LowerBound(_modelSettings.ObjectiveFunction, 0.05);
                    //upperBound = thisEpiModeller.GetObjectiveFunction_UpperBound(_modelSettings.ObjectiveFunction, 0.05);
                    //if ((mean >= maxMean && lowerBound >= maxLowerBound)
                    //    || (mean < maxMean && upperBound > maxMean && lowerBound > maxLowerBound))
                    //{
                    //    maxMean = mean;
                    //    maxLowerBound = lowerBound;
                    //    maxUpperBound = upperBound;
                    //    IDOfOptimalEpiModeller = thisEpiModeller.ID;
                    //    positionOfOptimalEpiModeller = position;                        
                    //}                    
                }
                ++position;
            }
            optHarmonicStep_a = _modelSettings.AdpParameterDesigns[IDOfOptimalEpiModeller][(int)enumADPParameter.HarmonicStepSize_a];
            optEpsilonGreedy_beta = _modelSettings.AdpParameterDesigns[IDOfOptimalEpiModeller][(int)enumADPParameter.EpsilonGreedy_beta];

            return (EpidemicModeller)_epidemicModellers[positionOfOptimalEpiModeller];
        }
        
        // find the epidemic modeler with optimal static policy - full factorial is used to evaluate static policies
        private EpidemicModeller FindOptimalEpiModeller_StaticPolicyEvaluatedUsingFullFacturial
            (ArrayList staticPolicyParameterDesigns, double forThisWTPforHealth,
            ref int interventionCombinationBode, ref double[] optimalStartTimes, ref int[] optimalNumOfDecisionPeriodsToUse)
        {
            //double maxMean = double.MinValue;
            //double maxLowerBound = double.MinValue;
            //double maxUpperBound = double.MinValue;
            //double mean, lowerBound, upperBound;

            forThisWTPforHealth = Math.Max(forThisWTPforHealth, SupportProcedures.minimumWTPforHealth);
            
            // find the objective function of each epidemic modeler 
            int IDOfOptimalEpiModeller = 0;
            int position = 0, positionOfOptimalEpiModeller = 0;
            foreach (EpidemicModeller thisEpiModeller in _epidemicModellers)
            {
                //mean = thisEpiModeller.GetObjectiveFunction_Mean(_modelSettings.ObjectiveFunction, forThisWTPforHealth);
                //lowerBound = thisEpiModeller.GetObjectiveFunction_LowerBound(_modelSettings.ObjectiveFunction, forThisWTPforHealth, 0.05);
                //upperBound = thisEpiModeller.GetObjectiveFunction_UpperBound(_modelSettings.ObjectiveFunction, forThisWTPforHealth, 0.05);
                //if ((mean >= maxMean && lowerBound >= maxLowerBound)
                //    || (mean < maxMean && upperBound > maxMean && lowerBound > maxLowerBound))
                //{
                //    maxMean = mean;
                //    maxLowerBound = lowerBound;
                //    maxUpperBound = upperBound;
                //    IDOfOptimalEpiModeller = thisEpiModeller.ID;
                //    positionOfOptimalEpiModeller = position;
                //}
                ++position;
            }
            // find the optimal policy (assuming that only interval-based policies are considered) 
            interventionCombinationBode = ((IntervalBasedStaticPolicy)staticPolicyParameterDesigns[IDOfOptimalEpiModeller]).InterventionCombinationCode;
            optimalStartTimes = ((IntervalBasedStaticPolicy)staticPolicyParameterDesigns[IDOfOptimalEpiModeller]).TimeToUseInterventions;
            optimalNumOfDecisionPeriodsToUse = ((IntervalBasedStaticPolicy)staticPolicyParameterDesigns[IDOfOptimalEpiModeller]).NumOfDecisionPointsToUseInterventions;

            return (EpidemicModeller)_epidemicModellers[positionOfOptimalEpiModeller];
        }        
       
        // initialize dynamic policy optimization
        private void InitializeDynamicPolicyOptimization()
        {
            // create and epidemic modeler (sequential processing)
            _epidemicModeller = new EpidemicModeller(0, _excelInterface, _modelSettings, _listModelInstr); // BuildAnEpidemicModeller_SequentialSimulation(0);

            // setup dynamic policy optimization setting
            _epidemicModeller.AddDynamicPolicySettings(ref _excelInterface);

            // set up Q-function approximation worksheet if necessary
            if (ExcelIntface.GetIfToUseCurrentQFunctionApproximationSettings() == false)
            {                
                // decision names
                //string[] strDecisionNames = _epidemicModeller.NamesOfDefaultInterventionsAndThoseSpecifiedByDynamicRule;
                // feature names
                //string[] strFeatureNames = _epidemicModeller.FeatureNames;
                string[] strAbbreviatedFeatureNames = new string[_epidemicModeller.ModelInfo.NumOfFeatures];
                //foreach (Feature thisFeature in _epidemicModeller.Features)
                //    strAbbreviatedFeatureNames[thisFeature.Index] = "F" + thisFeature.Index;
                
                // setup the feature worksheet
                //ExcelInterface.SetUpQFunctionWorksheet(
                //    strDecisionNames, strFeatureNames, strAbbreviatedFeatureNames, _epidemicModeller.GetQFunctionPolynomialTerms());
            }

            // read initial q-function coefficients
            _modelSettings.ReadQFunctionCoefficientsInitialValues(ref _excelInterface, _epidemicModeller.ModelInfo.NumOfFeatures);
           
            // set up ADP Iterations sheet
            ExcelIntface.SetUpOptimizationOutput(_modelSettings.NumOfADPIterations* _modelSettings.NumOfSimRunsToBackPropogate);
            // setup simulation output sheet
            SetupSimulationOutputSheet();
        }
        
        // get an epidemic modeler for optimizing adaptive policies
        private EpidemicModeller GetAnEpidemicModellerForOptimizatingDynamicPolicies
            (int epiModellerID, double wtpForHealth, double harmonicStepSize_a, double epsilonGreedy_beta)
        {
            // build an epidemic model
            EpidemicModeller thisEpidemicModeller = new EpidemicModeller(epiModellerID, _excelInterface, _modelSettings, _listModelInstr);

            // add dynamic policy settings
            thisEpidemicModeller.AddDynamicPolicySettings(ref _excelInterface);

            // make it ready for optimization
            thisEpidemicModeller.SetUpOptimization(wtpForHealth, harmonicStepSize_a, epsilonGreedy_beta, 0);

            return thisEpidemicModeller;
        }              
        
        //private void SetUpADPParameterDesigns(double wtpForHealth)
        //{
        //    _adpParameterDesigns = new double[0][];
        //    double thisHarmonicStepSize_a, thisEpsilonGreedy_beta;            
            
        //    thisHarmonicStepSize_a = _harmonicRule_a_min;
        //    while (thisHarmonicStepSize_a <= _harmonicRule_a_max)
        //    {
        //        thisEpsilonGreedy_beta = _epsilonGreedy_beta_min;
        //        while (thisEpsilonGreedy_beta <= _epsilonGreedy_beta_max)
        //        {
        //            double[][] thisDesign = new double[1][];
        //            thisDesign[0] = new double[3];
        //            // design
        //            thisDesign[0][(int)enumADPParameter.WTPForHealth] = wtpForHealth;
        //            thisDesign[0][(int)enumADPParameter.HarmonicStepSize_a] = thisHarmonicStepSize_a;
        //            thisDesign[0][(int)enumADPParameter.EpsilonGreedy_beta] = thisEpsilonGreedy_beta;
        //            // add design
        //            _adpParameterDesigns = SupportFunctions.ConcatJaggedArray(_adpParameterDesigns, thisDesign);

        //            thisEpsilonGreedy_beta += _epsilonGreedy_beta_step;
        //        }
        //        thisHarmonicStepSize_a += _harmonicRule_a_step;
        //    }
        //}

        // extract main simulation outcomes (cost, health and objecstive function) from this epidemic modeler
        private double[][] ExtractMainSimOutcomes(EpidemicModeller epidemicModeller)
        {
            double[][] thisSimulationOutcomes = new double[1][];
            thisSimulationOutcomes[0] = new double[6];

            // simulation outcomes
            if (_modelSettings.ObjectiveFunction == EnumObjectiveFunction.MaximizeNHB)
            {
                thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.E_ObjectiveFunction] 
                    = epidemicModeller.SimSummary.NHBStat.Mean;
                thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.stDev_ObjectiveFunction] 
                    = epidemicModeller.SimSummary.NHBStat.StDev;
            }
            else
            {
                thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.E_ObjectiveFunction] 
                    = epidemicModeller.SimSummary.NMBStat.Mean;
                thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.stDev_ObjectiveFunction] 
                    = epidemicModeller.SimSummary.NMBStat.StDev;
            }
            thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.E_health] 
                = epidemicModeller.SimSummary.DALYStat.Mean;
            thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.stDev_health] 
                = epidemicModeller.SimSummary.DALYStat.StDev;
            thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.E_cost] 
                = epidemicModeller.SimSummary.CostStat.Mean;
            thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.stDev_cost] 
                = epidemicModeller.SimSummary.CostStat.StDev;

            return thisSimulationOutcomes;
        }

        // set up observation worksheet
        private void ReadObservedHistory()
        {
            if (!(_modelSettings.ModelUse == EnumModelUse.Calibration)) // _modelSettings.DecisionRule == EnumEpiDecisions.PredeterminedSequence
                return;

            // check if current epidemic history can be used if not get the history from the user
            if (ExcelIntface.GetIfUseCurrentHistoryToCalibr() == false)
            {
                // get the names of calibration data
                string[] namesOfCalibrationTargets = _epidemicModeller.GetNamesOfCalibrTargets();

                // create the header
                string[,] strObsHeader = new string[2, 2 * namesOfCalibrationTargets.Length];
                for (int j = 0; j < namesOfCalibrationTargets.Length; j++)
                {
                    strObsHeader[0, 2 * j] = namesOfCalibrationTargets[j];
                    strObsHeader[0, 2 * j + 1] = "";
                    strObsHeader[1, 2 * j] = "Observed Value";
                    strObsHeader[1, 2 * j + 1] = "Likelihood Parameters";
                }

                // get the index of observation periods 
                int numOfObsPeriods = (int)(_modelSettings.TimeIndexToStop/ _modelSettings.NumOfDeltaT_inObservationPeriod);
                int[] obsPeriodIndex = new int[numOfObsPeriods+1]; // (note that we also include the 0th observation period)
                for (int i = 0; i < numOfObsPeriods+1; i++)
                    obsPeriodIndex[i] = i;

                // set up the epidemic history sheet
                ExcelIntface.SetupEpidemicHistoryWorksheet(strObsHeader, obsPeriodIndex); 
            }
            // read past actions
            _modelSettings.ReadPastActions(ref _excelInterface);
            _modelSettings.ReadObservedHistory(ref _excelInterface, _epidemicModeller.GetNamesOfCalibrTargets().Length);
        }

        // report results subs
        #region Report results subs
        // setup simulation output sheet
        private void SetupSimulationOutputSheet()
        {
            string[] incidenceOutputs = new string[0];
            string[] observableOutputs = new string[0];
            string[] prevalenceOutputs = new string[0];
            string[] resourceOutputs = new string[0];

            // write header
            ExcelIntface.SetupSimulationOutputSheet(
                simIncidenceHeader: _epidemicModeller.ParentEpidemic.EpiHist.SimOutputTrajs.IncidenceOutputsHeader.ToArray(),
                simPrevalenceHeader: _epidemicModeller.ParentEpidemic.EpiHist.SimOutputTrajs.PrevalenceOutputsHeader.ToArray(),                
                obsIncidenceHeader: _epidemicModeller.ParentEpidemic.EpiHist.SurveyedOutputTrajs.IncidenceOutputsHeader.ToArray(),
                obsPrevalenceHeader: _epidemicModeller.ParentEpidemic.EpiHist.SurveyedOutputTrajs.PrevalenceOutputsHeader.ToArray(),
                resouceOutputs: resourceOutputs);                             
        }
        // report simulation statistics
        private void ReportTrajsAndSimStats(EpidemicModeller epiModeller)
        {
            // first find the strings of past action combinations
            string[] strSimActionCombinations = null;
            foreach (int[] thisActionCombination in epiModeller.SimSummary.SimSummaryTrajs.TrajsSimIntrvCombinations)
                SupportFunctions.AddToEndOfArray(
                    ref strSimActionCombinations, 
                    SupportFunctions.ConvertArrayToString(thisActionCombination,",")
                    );
            string[] strObsActionCombinations = null;
            foreach (int[] thisActionCombination in epiModeller.SimSummary.SimSummaryTrajs.TrajsObsIntrvCombinations)
                SupportFunctions.AddToEndOfArray(
                    ref strObsActionCombinations,
                    SupportFunctions.ConvertArrayToString(thisActionCombination, ",")
                    );

            // report trajectories
            SimSummaryTrajs s = epiModeller.SimSummary.SimSummaryTrajs;
            if (epiModeller.ModelSettings.IfShowSimulatedTrajs)
                ExcelIntface.ReportEpidemicTrajectories(
                    // simulation trajectories 
                    simRepIndeces: SupportFunctions.ConvertJaggedArrayToRegularArray(
                        s.TrajsSimRepIndex,
                        1),
                    simIncidenceOutputs: SupportFunctions.ConvertJaggedArrayToRegularArray(
                        s.TrajsSimIncidence,
                        s.NumOfSimIncidenceInTraj), 
                    simPrevalenceOutputs: SupportFunctions.ConvertJaggedArrayToRegularArray(
                        s.TrajsSimPrevalence,
                        s.NumOfSimPrevalenceInTraj),
                    simIntrvnCombinationCodes: strSimActionCombinations,
                    // observable trajectories
                    obsRepIndeces: SupportFunctions.ConvertJaggedArrayToRegularArray(
                        s.TrajsObsRepIndex,
                        1),
                    obsIncidenceOutputs: SupportFunctions.ConvertJaggedArrayToRegularArray(
                        s.TrajsObsIncidence,
                        s.NumOfObsIncidenceInTraj),
                    obsPrevalenceOutputs: SupportFunctions.ConvertJaggedArrayToRegularArray(
                        s.TrajsObsPrevalence,
                        s.NumOfObsPrevalenceInTraj),
                    obsIntrvnCombinationCodes: strObsActionCombinations
                    );

            string[] strSummaryStatistics = null;
            string[] strClassAndSumStatistics = null;
            string[] strRatioStatistics = null;
            string[] strComputationStatistics = null;
            string[] strIterationOutcomes = null;
            double[][] arrSummaryStatistics = null;
            double[][] arrClassAndSumStatistics = null;
            double[][] arrRatioStatistics = null;
            double[,] arrComputationStatistics = null;
            double[][] arrIterationOutcomes = null;

            // get these statistics
            epiModeller.SimSummary.GetSummaryOutcomes(
                ref strSummaryStatistics, 
                ref strClassAndSumStatistics, 
                ref strRatioStatistics, 
                ref strComputationStatistics, 
                ref strIterationOutcomes,
                ref arrSummaryStatistics, 
                ref arrClassAndSumStatistics, 
                ref arrRatioStatistics, 
                ref arrComputationStatistics, 
                ref arrIterationOutcomes);

            // report
            ExcelIntface.ReportSimulationStatistics(
                strSummaryStatistics, SupportFunctions.ConvertJaggedArrayToRegularArray(arrSummaryStatistics, 3),
                strClassAndSumStatistics, SupportFunctions.ConvertJaggedArrayToRegularArray(arrClassAndSumStatistics, 3),
                strRatioStatistics, SupportFunctions.ConvertJaggedArrayToRegularArray(arrRatioStatistics, 3), 
                strComputationStatistics, arrComputationStatistics,
                strIterationOutcomes, SupportFunctions.ConvertJaggedArrayToRegularArray(arrIterationOutcomes,arrIterationOutcomes[0].Length));

            // report sampled parameter values
            ExcelIntface.ReportSampledParameterValues(
                epiModeller.ModelInfo.NamesOfParams, 
                epiModeller.SimSummary.SimItrs,
                epiModeller.SimSummary.RNDSeeds,
                SupportFunctions.ConvertJaggedArrayToRegularArray(
                    epiModeller.SimSummary.ParamValues, epiModeller.ModelInfo.NamesOfParams.Length)
                    );
        }
        // report calibration result
        private void ReportCalibrationResult()
        {
            ExcelIntface.ReportCalibrationResults(
                _epidemicModeller.Timer.TimePassed/60,
                _epidemicModeller.Calibration.NumOfDiscardedTrajs,
                _epidemicModeller.Calibration.ResultsForExcel.SimItrs.ToArray(),
                _epidemicModeller.Calibration.ResultsForExcel.RndSeeds.ToArray(),
                _epidemicModeller.Calibration.ResultsForExcel.Probs.ToArray()
                );
        }
        // report dynamic policy optimization result
        private void ReportADPResultsForThisEpidemic(EpidemicModeller thisEpidemicModeller, double harmonicStep_a, double epsilonGreedy_beta)
        {
            // report Q-function coefficients
            //_excelInterface.ReportQFunctionsCoefficientEstimates(thisEpidemicModeller.GetQFunctionCoefficientEstiamtes(), thisEpidemicModeller.NumOfFeatures);

            // report optimization iterations
            //_excelInterface.ReportADPIterations(thisEpidemicModeller.GetADPIterations(), harmonicStep_a, epsilonGreedy_beta);

            // read dynamic policy
            if (thisEpidemicModeller.ModelInfo.NumOfFeatures == 1)
            {
                string featureName = "";
                double[] headers = null;
                int[] optimalDecisions = null;
                //thisEpidemicModeller.GetOptimalDynamicPolicy(ref featureName, ref headers, ref optimalDecisions, _modelSettings.NumOfIntervalsToDescretizeFeatures);

                // report the dynamic policy
                ExcelIntface.Report1DimOptimalPolicy(featureName, headers, optimalDecisions);

            }
            else if (thisEpidemicModeller.ModelInfo.NumOfFeatures == 2)
            {
                string[] strFeatureNames = null;
                double[][] headers = null;
                int[,] optimalDecisions = null;
                //thisEpidemicModeller.GetOptimalDynamicPolicy(ref strFeatureNames, ref headers, ref optimalDecisions, _modelSettings.NumOfIntervalsToDescretizeFeatures);

                // report the dynamic policy
                ExcelIntface.Report2DimOptimalPolicy(strFeatureNames, headers, optimalDecisions);
            }
            else if (thisEpidemicModeller.ModelInfo.NumOfFeatures == 3)
            {
                string[] strFeatureNames = null;
                double[][] headers = null;
                int[][,] optimalDecisions = null;
                //thisEpidemicModeller.GetOptimalDynamicPolicy(ref strFeatureNames, ref headers, ref optimalDecisions, _modelSettings.NumOfIntervalsToDescretizeFeatures);

                // report the dynamic policy
                ExcelIntface.Report3DimOptimalPolicy(strFeatureNames, headers, optimalDecisions);
            }
        }
        // report the results of adaptive policy optimization 
        public void ReportADPResultsForAllEpidemic()
        {
            double optHarmonicStep_a = 0, optEpsilonGreedy_beta = 0;
            double[][] adpOptParameterDesigns = new double[0][];
            double[][] adpSASimulationOutcomes = new double[0][];
            double[][] adpSASimulationIterations = new double[0][];              

            double thisWTPForHealth = _modelSettings.OptmzSets.WTP_min;
            while (thisWTPForHealth <= _modelSettings.OptmzSets.WTP_max)
            {
                // find the optimal static policy for this epidemic
                optHarmonicStep_a = 0; 
                optEpsilonGreedy_beta = 0;
                EpidemicModeller thisEpiModeller = FindOptimalEpiModeller_DynamicPolicy(thisWTPForHealth, ref optHarmonicStep_a, ref optEpsilonGreedy_beta);
                
                double[][] thisAdpOptParameterDesigns = new double[1][];
                thisAdpOptParameterDesigns[0] = new double[3];
                thisAdpOptParameterDesigns[0][(int)enumADPParameter.WTPForHealth] = thisWTPForHealth;
                thisAdpOptParameterDesigns[0][(int)enumADPParameter.HarmonicStepSize_a] = optHarmonicStep_a;
                thisAdpOptParameterDesigns[0][(int)enumADPParameter.EpsilonGreedy_beta] = optEpsilonGreedy_beta;
                adpOptParameterDesigns = SupportFunctions.ConcatJaggedArray(adpOptParameterDesigns, thisAdpOptParameterDesigns);

                // simulation outcomes
                adpSASimulationOutcomes = SupportFunctions.ConcatJaggedArray(adpSASimulationOutcomes, ExtractMainSimOutcomes(thisEpiModeller));

                // simulation iterations
                double[][] thisSimulationIterations = new double[_modelSettings.NumOfSimItrs][];
                for (int simItr = 0; simItr < _modelSettings.NumOfSimItrs; simItr++)
                {
                    thisSimulationIterations[simItr] = new double[7];
                    thisSimulationIterations[simItr][(int)ExcelInterface.enumADPSASimulationIterationsOffsets.wtpForHealth]
                        = thisWTPForHealth;
                    thisSimulationIterations[simItr][(int)ExcelInterface.enumADPSASimulationIterationsOffsets.harmonic_a]
                        = optHarmonicStep_a;
                    thisSimulationIterations[simItr][(int)ExcelInterface.enumADPSASimulationIterationsOffsets.epsilonGreedy_beta]
                        = optEpsilonGreedy_beta;

                    if (_modelSettings.ObjectiveFunction == EnumObjectiveFunction.MaximizeNHB)
                        thisSimulationIterations[simItr][(int)ExcelInterface.enumADPSASimulationIterationsOffsets.objectiveFunction]
                            = thisEpiModeller.SimSummary.NHBs[simItr];
                    else
                        thisSimulationIterations[simItr][(int)ExcelInterface.enumADPSASimulationIterationsOffsets.objectiveFunction]
                            = thisEpiModeller.SimSummary.NMBs[simItr];
                    thisSimulationIterations[simItr][(int)ExcelInterface.enumADPSASimulationIterationsOffsets.health]
                        = thisEpiModeller.SimSummary.DALYs[simItr];
                    thisSimulationIterations[simItr][(int)ExcelInterface.enumADPSASimulationIterationsOffsets.cost]
                        = thisEpiModeller.SimSummary.Costs[simItr];
                    thisSimulationIterations[simItr][(int)ExcelInterface.enumADPSASimulationIterationsOffsets.annualCost]
                        = thisEpiModeller.SimSummary.AnnualCosts[simItr];
                }
                // concatenate
                adpSASimulationIterations = SupportFunctions.ConcatJaggedArray(adpSASimulationIterations, thisSimulationIterations);

                // increment the wtp for health 
                thisWTPForHealth += _modelSettings.OptmzSets.WTP_step;
            }

            // report
            ExcelIntface.ReportADPResultsForAllEpidemic(
                SupportFunctions.ConvertJaggedArrayToRegularArray(adpOptParameterDesigns, 3),
                SupportFunctions.ConvertJaggedArrayToRegularArray(adpSASimulationOutcomes, 6),
                SupportFunctions.ConvertJaggedArrayToRegularArray(adpSASimulationIterations, 7), 
                _modelSettings.ObjectiveFunction);
        }
        // report the ADP algorithm computation times
        public void ReportADPComputationTimes()
        {
            string[] measures = new string[4];
            measures[0] = "Total simulation time during ADP iterations";
            measures[1] = "Time spent by ADP iterations";
            measures[2] = "Ratio of optimization time to total simulation time for optimization";
            measures[3] = "Total optimization time";            

            double[,] statistics = new double[4, 3];

            statistics[0, 0] = _obsTotalSimulationTimeToFindOneDynamicPolicy.Mean;
            statistics[0, 1] = _obsTotalSimulationTimeToFindOneDynamicPolicy.StDev;
            statistics[0, 2] = _obsTotalSimulationTimeToFindOneDynamicPolicy.StErr;
            statistics[1, 0] = _obsTimeUsedToFindOneDynamicPolicy.Mean;
            statistics[1, 1] = _obsTimeUsedToFindOneDynamicPolicy.StDev;
            statistics[1, 2] = _obsTimeUsedToFindOneDynamicPolicy.StErr;
            statistics[2, 0] = _obsRatioOfTimeUsedToFindOneDynamicPolicyToTotalSimulationTimeToFindOneDynamicPolicy.Mean;
            statistics[2, 1] = _obsRatioOfTimeUsedToFindOneDynamicPolicyToTotalSimulationTimeToFindOneDynamicPolicy.StDev;
            statistics[2, 2] = _obsRatioOfTimeUsedToFindOneDynamicPolicyToTotalSimulationTimeToFindOneDynamicPolicy.StErr;
            statistics[3, 0] = _actualTimeUsedToFindAllDynamicPolicies;

            // report
            ExcelIntface.ReportADPComputationTimes(measures, statistics);
        }
        // report static optimization results
        public void ReportStaticPolicyOptimizationResults(ArrayList staticPolicyParameterDesigns)
        {
            int interventionCombinationCode = 0;
            double[] optimalStartTimes = new double[0];
            int[] optimalNumOfDecisionPeriodsToUse = new int[0];

            double[] wtp = new double[0];
            double[][] simulationOutcomes = new double[0][], simulationIterations = new double[0][];
            string[][] staticPolicies = new string[0][];

            double thisWTPForHealth = _modelSettings.OptmzSets.WTP_min;
            while (thisWTPForHealth <= _modelSettings.OptmzSets.WTP_max)
            {
                // find the optimal static policy for this epidemic
                EpidemicModeller thisEpiModeller = null;

                thisEpiModeller = FindOptimalEpiModeller_StaticPolicyEvaluatedUsingFullFacturial
                                        (staticPolicyParameterDesigns, thisWTPForHealth, ref interventionCombinationCode, ref optimalStartTimes, ref optimalNumOfDecisionPeriodsToUse);
                
                // read static policy
                string[] thisStaticPolicy = new string[3];
                thisStaticPolicy[0] = SupportFunctions.ConvertArrayToString(SupportFunctions.ConvertToBase2FromBase10(interventionCombinationCode, optimalStartTimes.Length), ",");
                thisStaticPolicy[1] = SupportFunctions.ConvertArrayToString(optimalStartTimes, ",");
                thisStaticPolicy[2] = SupportFunctions.ConvertArrayToString(optimalNumOfDecisionPeriodsToUse, ",");

                // concatenate
                SupportFunctions.AddToEndOfArray(ref wtp, thisWTPForHealth);
                staticPolicies = SupportFunctions.ConcatJaggedArray(staticPolicies, thisStaticPolicy);

                // concatenate simulation outcomes
                //simulationOutcomes = SupportFunctions.ConcatJaggedArray(simulationOutcomes, ExtractSimulationOutcomes(thisEpiModeller, thisWTPForHealth));

                // simulation iterations
                double[][] thisSimulationIterations = new double[_modelSettings.NumOfSimItrs][];
                for (int simItr = 0; simItr < _modelSettings.NumOfSimItrs; simItr++)
                {
                    thisSimulationIterations[simItr] = new double[5];
                    thisSimulationIterations[simItr][(int)ExcelInterface.enumStaticPolicyOptimizationSimulationIterationsOffsets.wtpForHealth] = thisWTPForHealth;

                    if (_modelSettings.ObjectiveFunction == EnumObjectiveFunction.MaximizeNHB)
                        thisSimulationIterations[simItr][(int)ExcelInterface.enumStaticPolicyOptimizationSimulationIterationsOffsets.objectiveFunction]
                            = -thisEpiModeller.SimSummary.DALYs[simItr] 
                            - thisEpiModeller.SimSummary.Costs[simItr] / Math.Max(thisWTPForHealth, SupportProcedures.minimumWTPforHealth);
                    else
                        thisSimulationIterations[simItr][(int)ExcelInterface.enumStaticPolicyOptimizationSimulationIterationsOffsets.objectiveFunction]
                            = -thisWTPForHealth * thisEpiModeller.SimSummary.DALYs[simItr] 
                            - thisEpiModeller.SimSummary.Costs[simItr];
                    thisSimulationIterations[simItr][(int)ExcelInterface.enumStaticPolicyOptimizationSimulationIterationsOffsets.health]
                        = thisEpiModeller.SimSummary.DALYs[simItr];
                    thisSimulationIterations[simItr][(int)ExcelInterface.enumStaticPolicyOptimizationSimulationIterationsOffsets.cost]
                        = thisEpiModeller.SimSummary.Costs[simItr];
                    thisSimulationIterations[simItr][(int)ExcelInterface.enumStaticPolicyOptimizationSimulationIterationsOffsets.annualCost]
                        = thisEpiModeller.SimSummary.AnnualCosts[simItr];
                }
                // concatenate
                simulationIterations = SupportFunctions.ConcatJaggedArray(simulationIterations, thisSimulationIterations);
                
                // increment the wtp for health 
                thisWTPForHealth += _modelSettings.OptmzSets.WTP_step;
            }

            // report
            ExcelIntface.ReportStaticPolicyOptimization( wtp, 
                SupportFunctions.ConvertJaggedArrayToRegularArray(staticPolicies, 3),
                SupportFunctions.ConvertJaggedArrayToRegularArray(simulationOutcomes, 6),
                SupportFunctions.ConvertJaggedArrayToRegularArray(simulationIterations, 5),
                _modelSettings.ObjectiveFunction);

        }
        #endregion
    }
}
