using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputationLib;
using RandomVariateLib;
using SimulationLib;
using System.Runtime.InteropServices;

namespace APACElib
{
    public class APACE
    {
        // Variable Definition 
        #region Variable Definition

        //private Epidemic _excelEpidemic; // only to store basic simulation settings

        private ExcelInterface _excelInterface;
        private ModelSettings _modelSettings = new ModelSettings();
        private EpidemicModeller _epidemicModeller;
        private ArrayList _epidemicModellers = new ArrayList();

        // computation time
        private double _actualTimeUsedToFindAllDynamicPolicies; // considering several ADP parameter designs
        private ObservationBasedStatistics _obsTotalSimulationTimeToEvaluateOneStaticPolicy = new ObservationBasedStatistics("");
        private ObservationBasedStatistics _obsTimeUsedToFindOneStaticPolicy = new ObservationBasedStatistics("");
        private ObservationBasedStatistics _obsRatioOfTimeUsedToFindOneStaticPolicyToTotalSimulationTimeToEvaluateOneStaticPolicy = new ObservationBasedStatistics("");
        private ObservationBasedStatistics _obsTotalSimulationTimeToFindOneDynamicPolicy = new ObservationBasedStatistics("");
        private ObservationBasedStatistics _obsTimeUsedToFindOneDynamicPolicy = new ObservationBasedStatistics("");
        private ObservationBasedStatistics _obsRatioOfTimeUsedToFindOneDynamicPolicyToTotalSimulationTimeToFindOneDynamicPolicy = new ObservationBasedStatistics("");

        private ObservationBasedStatistics _obsRealTimeSimulation_staticOptimization_totalCalibrationTime = new ObservationBasedStatistics("");
        private ObservationBasedStatistics _obsRealTimeSimulation_staticOptimization_totalOptimizationTime = new ObservationBasedStatistics("");
        private ObservationBasedStatistics _obsRealTimeSimulation_staticOptimization_totalCalibrationTimeOverAverageSimulationTime = new ObservationBasedStatistics("");
        private ObservationBasedStatistics _obsRealTimeSimulation_staticOptimization_totalOptimizationTimeOverAverageSimulationTime = new ObservationBasedStatistics("");        
        private ObservationBasedStatistics _obsRealTimeSimulation_dynamicOptimization_totalCalibrationTime = new ObservationBasedStatistics("");
        private ObservationBasedStatistics _obsRealTimeSimulation_dynamicOptimization_totalOptimizationTime = new ObservationBasedStatistics("");
        private ObservationBasedStatistics _obsRealTimeSimulation_dynamicOptimization_totalCalibrationTimeOverAverageSimulationTime = new ObservationBasedStatistics("");
        private ObservationBasedStatistics _obsRealTimeSimulation_dynamicOptimization_totalOptimizationTimeOverAverageSimulationTime = new ObservationBasedStatistics("");
        #endregion

        const int NumOfNelderMeanSearchIterations = 200;

        // connect to the excel interface
        public void ConnectToExcelInteface()
        {
            _excelInterface = new ExcelInterface();
            _excelInterface.ConnectToExcelInterface();
        }

        // get excel file name
        public string ExcelFileName()
        {
            return _excelInterface.GetFileName();
        }

        // save excel file
        public void SaveExcelFile()
        {
            _excelInterface.Save();
        }

        // make the excel file visible
        public void MakeModelVisible()
        {
            _excelInterface.Visible = true;
        }
        // make the excel file invisible
        public void MakeModelInvisible()
        {
            _excelInterface.Visible = false;
        }

        // run
        public void Run()
        {
            // read model settings
            _modelSettings.ReadSettings(ref _excelInterface);

            // find the task            
            switch (_excelInterface.GetWhatToDo())
            {
                case ExcelInterface.enumWhatToDo.Simulate:
                    {
                        // simulate a policy
                        SimulateAPolicy();                        
                        break;
                    }
                case ExcelInterface.enumWhatToDo.Calibrate:
                    {
                        // calibrate
                        Calibrate();
                        break;
                    }
                case ExcelInterface.enumWhatToDo.OptimizeTheDynamicPolicy:
                    {
                       // optimize the dynamic policy
                        OptimizeTheDynamicPolicy();
                        break;
                    }
                case ExcelInterface.enumWhatToDo.OptimizeTheStaticPolicy:
                    {
                        // optimize the static policy
                        OptimizeTheStaticPolicy();
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
            _epidemicModeller = new EpidemicModeller(0, ref _excelInterface, ref _modelSettings);

            // setup simulation output sheet
            SetupSimulationOutputSheet();

            // simulate epidemics
            _epidemicModeller.SimulateEpidemics();

            // report simulation results
            ReportTrajectoriesAndSimulationStatistics(_epidemicModeller);

            // destruct the modeller 
            _epidemicModeller.MyDestruct();
        }

        // calibrate
        private void Calibrate()
        {
            // initialize calibration
            InitializeCalibration();

            // calibrate
            _epidemicModeller.Calibrate(
                _excelInterface.GetInitialNumberOfTrajectoriesForCalibration(), 
                _excelInterface.GetNumOfFittestTrajectoriesToReturn());
            
            // report calibration results
            ReportCalibrationResult();

            // destruct the modeller 
            _epidemicModeller.MyDestruct();
        }
        
        // optimize the dynamic policy
        private void OptimizeTheDynamicPolicy()
        {
            // read optimization settings
            _modelSettings.ReadOptimizationSettings(ref _excelInterface);

            // initialize dynamic policy optimization
            InitializeDynamicPolicyOptimization();
            // set up ADP parameter designs
            _modelSettings.SetUpADPParameterDesigns();
            // optimize epidemics
            BuildAndOptimizeEachEpidemicModeller_DynamicPolicyOptimization(true, _modelSettings.EpidemicTimeIndexToStartDecisionMaking, 0);

            // single value for wtp for health
            if (Math.Abs(_modelSettings.WtpForHealth_min - _modelSettings.WtpForHealth_max) < SupportProcedures.minimumWTPforHealth)
            {
                double harmonicStep_a = 0, epsilonGreedy_beta = 0;
                // find the optimal epidemic modeler
                EpidemicModeller optimalEpidemicModeller = FindOptimalEpiModeller_DynamicPolicy(_modelSettings.WtpForHealth_min, ref harmonicStep_a, ref epsilonGreedy_beta);
                // report optimization result
                ReportADPResultsForThisEpidemic(optimalEpidemicModeller, harmonicStep_a, epsilonGreedy_beta);
                // report simulation result
                ReportTrajectoriesAndSimulationStatistics(optimalEpidemicModeller);
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
            _modelSettings.ReadOptimizationSettings(ref _excelInterface);

            ArrayList staticPolicyDesigns = new ArrayList();
            // build a temp modeler to find the available decisions
            EpidemicModeller tempEpidemicModeller = new EpidemicModeller(0, ref _excelInterface, ref _modelSettings);

            // get interval-based static policy designs
            staticPolicyDesigns = tempEpidemicModeller.GetIntervalBasedStaticPoliciesDesigns();

            // evaluate the defined interval-based static policies
            BuildAndSimulateEpidemicModellersToEvaluateIntervalBasedStaticPolicies
                (staticPolicyDesigns, 0);

            // report static policy optimization
            ReportStaticPolicyOptimizationResults(staticPolicyDesigns);
        }        

        // run experiments
        private void RunExperiments()
        {
            int epidemicModelIndex = 0;
            double[][] simulationSummaryOutcomes = new double[0][];
            string[] simulationIterations_lables = new string[0];
            double[][] simulationIterations_objFunction = new double[0][];
            double[][] simulationIterations_otherOutcomes = new double[0][];

            // read designs
            double[,] experimentDesigns = (double[,])_excelInterface.GetExperimentalDesignMatrix();

            // build epidemic modelers            
            _epidemicModellers.Clear();
            int numOfDesigns = experimentDesigns.GetLength(0);
            int numOfVars = experimentDesigns.GetLength(1);

            for (int designIndex = 0; designIndex < numOfDesigns; designIndex++)
            {

                #region read a design, and build and run the epidemic model 
                // find the design
                double[] thisDesign = new double[numOfVars];
                for (int varIndex = 0; varIndex < numOfVars; varIndex++)
                    thisDesign[varIndex] = experimentDesigns[designIndex, varIndex];
                // write back the design
                _excelInterface.WriteADesign(thisDesign);
                // recalculate
                _excelInterface.Recalculate();
                // read model settings
                _modelSettings.ReadSettings(ref _excelInterface);

                // create and epidemic modeler
                EpidemicModeller thisEpidemicModeller = new EpidemicModeller(designIndex, ref _excelInterface, ref _modelSettings);
                // don't store epidemic trajectories
                thisEpidemicModeller.ShouldStoreEpidemicTrajectories(false);

                // run the simulation
                thisEpidemicModeller.SimulateEpidemics();

                #endregion

                // concatenate simulation summary statistics 
                simulationSummaryOutcomes = SupportFunctions.ConcatJaggedArray(simulationSummaryOutcomes, ExtractSimulationOutcomes(thisEpidemicModeller));
                //simulationSummaryOutcomes.Concat(ExtractSimulationOutcomes(thisEpidemicModeller));

                // simulation iterations
                // first read variable values and the objective function samples
                double[][] thisSimulationIterations_objFunction = new double[_modelSettings.NumOfSimulationIterations][];
                for (int simItr = 0; simItr < _modelSettings.NumOfSimulationIterations; simItr++)
                {
                    thisSimulationIterations_objFunction[simItr] = new double[numOfVars + 1]; // 1 for the objective function 

                    // this design
                    for (int varIndex = 0; varIndex < numOfVars; varIndex++)
                        thisSimulationIterations_objFunction[simItr][varIndex] = experimentDesigns[epidemicModelIndex, varIndex];
                    // objective function
                    if (_modelSettings.ObjectiveFunction == EnumObjectiveFunction.MaximizeNHB)
                        thisSimulationIterations_objFunction[simItr][numOfVars + 0]
                            = thisEpidemicModeller.SimulationIterations_QALY[simItr] - thisEpidemicModeller.SimulationIterations_Cost[simItr] 
                            / Math.Max(_modelSettings.WTPForHealth, SupportProcedures.minimumWTPforHealth);
                    else
                        thisSimulationIterations_objFunction[simItr][numOfVars + 0]
                            = _modelSettings.WTPForHealth* thisEpidemicModeller.SimulationIterations_QALY[simItr] - thisEpidemicModeller.SimulationIterations_Cost[simItr];

                }
                // then read the other outcomes samples
                double[][] thisSimulationItreration_otherOutcomes = new double[0][];
                thisEpidemicModeller.GetSimulationIterationOutcomes(ref simulationIterations_lables, ref thisSimulationItreration_otherOutcomes);

                // concatenate
                simulationIterations_objFunction = SupportFunctions.ConcatJaggedArray(simulationIterations_objFunction, thisSimulationIterations_objFunction);
                simulationIterations_otherOutcomes = SupportFunctions.ConcatJaggedArray(simulationIterations_otherOutcomes, thisSimulationItreration_otherOutcomes);

                // clear this epidemic modeler 
                thisEpidemicModeller.MyDestruct();

                ++epidemicModelIndex;

            }
            

            #region report results
            // report summary statistics
            _excelInterface.ReportSimulationOutcomes("Experimental Designs", "baseExperimentalDesignsResults",
                SupportFunctions.ConvertFromJaggedArrayToRegularArray(simulationSummaryOutcomes, 6), _modelSettings.ObjectiveFunction);

            // report outcomes for each run for each design
            _excelInterface.ReportExperimentalDesignSimulationOutcomes(numOfVars,
                SupportFunctions.ConvertFromJaggedArrayToRegularArray(simulationIterations_objFunction, numOfVars + 1), _modelSettings.ObjectiveFunction,
                simulationIterations_lables, SupportFunctions.ConvertFromJaggedArrayToRegularArray(simulationIterations_otherOutcomes, simulationIterations_otherOutcomes[0].Length));
            #endregion

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
                    thisEpidemicModeller.UpdateTheTimeToStartDecisionMakingAndWarmUpPeriod
                        (epidemicTimeToStartDecisionMaking, warmUpPeriod);

                    // add this epidemic modeler
                    _epidemicModellers.Add(thisEpidemicModeller);
                }
                // run the epidemics
                foreach (EpidemicModeller thisEpiModeller in _epidemicModellers)
                {
                    // find the dynamic Policy
                    thisEpiModeller.FindOptimalDynamicPolicy();
                    // simulate the dynamic Policy
                    thisEpiModeller.SimulateTheOptimalDynamicPolicy(_modelSettings.NumOfSimulationIterations, _modelSettings.TimeIndexToStop,
                        _modelSettings.WarmUpPeriodTimeIndex, storeEpidemicTrajectories);
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
                    thisEpidemicModeller.UpdateTheTimeToStartDecisionMakingAndWarmUpPeriod
                        (epidemicTimeToStartDecisionMaking, warmUpPeriod);

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
                    ((EpidemicModeller)thisEpiModeller).FindOptimalDynamicPolicy();
                    // simulate the dynamic Policy
                    ((EpidemicModeller)thisEpiModeller).SimulateTheOptimalDynamicPolicy(_modelSettings.NumOfSimulationIterations, _modelSettings.TimeIndexToStop,
                        _modelSettings.WarmUpPeriodTimeIndex, storeEpidemicTrajectories);
                });     
                
                #endregion
            }

            // computation time
            optEndTime = Environment.TickCount;
            _actualTimeUsedToFindAllDynamicPolicies = (optEndTime - optStartTime) / 1000;
        }        
        
        // build and simulate epidemic modelers to evaluate the interval-based static policies
        private void BuildAndSimulateEpidemicModellersToEvaluateIntervalBasedStaticPolicies
            (ArrayList designs, double warmUpPeriod)
        {
            // computation time
            _obsTotalSimulationTimeToEvaluateOneStaticPolicy.Reset();

            // build epidemic modelers
            _epidemicModellers.Clear();

            // use parallel computing? 
            if (_modelSettings.UseParallelComputing== false)
            {
                #region Use sequential computing
                foreach (IntervalBasedStaticPolicy thisIntervalBasedPolicy in designs)
                {
                    // build an epidemic model
                    EpidemicModeller thisEpidemicModeller
                        = GetAnEpidemicModellerToEvaluateIntervalBasedStaticPolicy(
                        thisIntervalBasedPolicy.ID,
                        thisIntervalBasedPolicy.InterventionCombinationCode,
                        thisIntervalBasedPolicy.TimeToUseInterventions,
                        thisIntervalBasedPolicy.NumOfDecisionPointsToUseInterventions,
                        _modelSettings.SimulationRNDSeedsSource,
                        0);

                    // update time to start decision making and storing outcomes
                    thisEpidemicModeller.UpdateTheTimeToStartDecisionMakingAndWarmUpPeriod
                        (_modelSettings.EpidemicTimeIndexToStartDecisionMaking, warmUpPeriod);

                    // add this epidemic modeler
                    _epidemicModellers.Add(thisEpidemicModeller);
                }

                // run the epidemics
                foreach (EpidemicModeller thisEpiModeller in _epidemicModellers)
                {
                    // simulate the static Policy
                    thisEpiModeller.SimulateEpidemics();
                    // computation time
                    _obsTotalSimulationTimeToEvaluateOneStaticPolicy.Record(thisEpiModeller.ObsTimeUsedToSimulateATrajectory.Total);

                }
                #endregion
            }
            else // (_useParallelComputing == true)
            {
                #region Use parallel computing
                Object thisLock = new Object();
                Parallel.ForEach(designs.Cast<object>(), thisIntervalBasedPolicy =>
                {
                    // build an epidemic model
                    EpidemicModeller thisEpidemicModeller
                        = GetAnEpidemicModellerToEvaluateIntervalBasedStaticPolicy(
                        ((IntervalBasedStaticPolicy)thisIntervalBasedPolicy).ID,
                        ((IntervalBasedStaticPolicy)thisIntervalBasedPolicy).InterventionCombinationCode,
                        ((IntervalBasedStaticPolicy)thisIntervalBasedPolicy).TimeToUseInterventions,
                        ((IntervalBasedStaticPolicy)thisIntervalBasedPolicy).NumOfDecisionPointsToUseInterventions,
                        _modelSettings.SimulationRNDSeedsSource,
                        0);

                    // update time to start decision making and storing outcomes
                    thisEpidemicModeller.UpdateTheTimeToStartDecisionMakingAndWarmUpPeriod
                        (_modelSettings.EpidemicTimeIndexToStartDecisionMaking, warmUpPeriod);

                    // add this epidemic modeler
                    lock (thisLock)
                    {
                        // add this epidemic modeler
                        _epidemicModellers.Add(thisEpidemicModeller);
                    }
                });

                // run the epidemics
                Parallel.ForEach(_epidemicModellers.Cast<object>(), thisEpiModeller =>
                {
                    // simulate the static Policy
                    ((EpidemicModeller)thisEpiModeller).SimulateEpidemics();
                });
                // computation time
                foreach (EpidemicModeller thisEpiModeller in _epidemicModellers)
                    _obsTotalSimulationTimeToEvaluateOneStaticPolicy.Record(thisEpiModeller.ObsTimeUsedToSimulateATrajectory.Total);
                #endregion
            }
        }
        
        // build and simulate epidemic modelers to evaluate the threshold-based static policies
        private void BuildAndSimulateEpidemicModellersToEvaluateThresholdBasedStaticPolicies
            (double[][] staticPolicyParameterDesigns, int numOfSimulationRunsForEachDesign, EnumSimulationRNDSeedsSource simulationRNDSeedsSource)
        {
            // build epidemic modelers
            _epidemicModellers.Clear();
            // use parallel computing? 
            if (_modelSettings.UseParallelComputing== false)
            {
                #region Use sequential computing
                for (int epiID = 0; epiID < staticPolicyParameterDesigns.Length; epiID++)
                {
                    int[] decisionIDs = new int[1];
                    double[] thresholds = new double[1];
                    int[] numOfDecisionPeriodsToUse = new int[1]; 
                    decisionIDs[0] = (int)staticPolicyParameterDesigns[epiID][(int)enumThresholdBasedStaticPolicySetting.DecisionID];
                    thresholds[0] = staticPolicyParameterDesigns[epiID][(int)enumThresholdBasedStaticPolicySetting.Threshold];
                    numOfDecisionPeriodsToUse[0] = (int)staticPolicyParameterDesigns[epiID][(int)enumThresholdBasedStaticPolicySetting.NumOfDecisionPeriodsToUse];
                    // build an epidemic model
                    EpidemicModeller thisEpidemicModeller
                        = GetAnEpidemicModellerToEvaluateThresholdBasedStaticPolicy(epiID,
                        decisionIDs, thresholds, numOfDecisionPeriodsToUse);
                    thisEpidemicModeller.ToggleModellerTo(EnumModelUse.Simulation, EnumEpiDecisions.SpecifiedByPolicy, false);

                    // add this epidemic modeler
                    _epidemicModellers.Add(thisEpidemicModeller);
                }
                // run the epidemics
                foreach (EpidemicModeller thisEpiModeller in _epidemicModellers)
                {
                    // simulate the static Policy                             
                    thisEpiModeller.SimulateEpidemics();
                }
                #endregion
            }
            else // (_useParallelComputing == true)
            {
                #region Use parallel computing
                Object thisLock = new Object();
                Parallel.For(0, staticPolicyParameterDesigns.Length, (Action<int>)(epiID =>
                {
                    int[] decisionIDs = new int[1];
                    double[] thresholds = new double[1];
                    int[] numOfDecisionPeriodsToUse = new int[1];
                    decisionIDs[0] = (int)staticPolicyParameterDesigns[epiID][(int)enumThresholdBasedStaticPolicySetting.DecisionID];
                    thresholds[0] = staticPolicyParameterDesigns[epiID][(int)enumThresholdBasedStaticPolicySetting.Threshold];
                    numOfDecisionPeriodsToUse[0] = (int)staticPolicyParameterDesigns[epiID][(int)enumThresholdBasedStaticPolicySetting.NumOfDecisionPeriodsToUse];
                    // build an epidemic model
                    EpidemicModeller thisEpidemicModeller
                        = GetAnEpidemicModellerToEvaluateThresholdBasedStaticPolicy(epiID,
                        (int[])decisionIDs.Clone(), (double[])thresholds.Clone(), (int[])numOfDecisionPeriodsToUse.Clone());

                    thisEpidemicModeller.ToggleModellerTo(EnumModelUse.Simulation, EnumEpiDecisions.SpecifiedByPolicy, false);

                    // add this epidemic modeler
                    lock (thisLock)
                    {
                        _epidemicModellers.Add(thisEpidemicModeller);
                    }
                }));
                // run the epidemics
                Parallel.ForEach(_epidemicModellers.Cast<object>(), thisEpiModeller =>
                {
                    // simulate the dynamic Policy                    
                    ((EpidemicModeller)thisEpiModeller).SimulateEpidemics();
                });
                #endregion
            }
        }
        
        // find the epidemic modeler with optimal adaptive policy
        private EpidemicModeller FindOptimalEpiModeller_DynamicPolicy(double forThisWTPforHealth, ref double optHarmonicStep_a, ref double optEpsilonGreedy_beta)
        {
            double maxMean = double.MinValue;
            double maxLowerBound = double.MinValue;
            double maxUpperBound = double.MinValue;
            double mean, lowerBound, upperBound;

            int IDOfOptimalEpiModeller = 0;
            int position = 0, positionOfOptimalEpiModeller = 0;
            // find the objective function of each epidemic modeler 
            foreach (EpidemicModeller thisEpiModeller in _epidemicModellers)
            {
                if (Math.Abs(_modelSettings.AdpParameterDesigns[thisEpiModeller.ID][(int)enumADPParameter.WTPForHealth] - forThisWTPforHealth) 
                    < SupportProcedures.minimumWTPforHealth)
                {
                    mean = thisEpiModeller.GetObjectiveFunction_Mean(_modelSettings.ObjectiveFunction);
                    lowerBound = thisEpiModeller.GetObjectiveFunction_LowerBound(_modelSettings.ObjectiveFunction, 0.05);
                    upperBound = thisEpiModeller.GetObjectiveFunction_UpperBound(_modelSettings.ObjectiveFunction, 0.05);
                    if ((mean >= maxMean && lowerBound >= maxLowerBound)
                        || (mean < maxMean && upperBound > maxMean && lowerBound > maxLowerBound))
                    {
                        maxMean = mean;
                        maxLowerBound = lowerBound;
                        maxUpperBound = upperBound;
                        IDOfOptimalEpiModeller = thisEpiModeller.ID;
                        positionOfOptimalEpiModeller = position;                        
                    }                    
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
            double maxMean = double.MinValue;
            double maxLowerBound = double.MinValue;
            double maxUpperBound = double.MinValue;
            double mean, lowerBound, upperBound;

            forThisWTPforHealth = Math.Max(forThisWTPforHealth, SupportProcedures.minimumWTPforHealth);
            
            // find the objective function of each epidemic modeler 
            int IDOfOptimalEpiModeller = 0;
            int position = 0, positionOfOptimalEpiModeller = 0;
            foreach (EpidemicModeller thisEpiModeller in _epidemicModellers)
            {
                mean = thisEpiModeller.GetObjectiveFunction_Mean(_modelSettings.ObjectiveFunction, forThisWTPforHealth);
                lowerBound = thisEpiModeller.GetObjectiveFunction_LowerBound(_modelSettings.ObjectiveFunction, forThisWTPforHealth, 0.05);
                upperBound = thisEpiModeller.GetObjectiveFunction_UpperBound(_modelSettings.ObjectiveFunction, forThisWTPforHealth, 0.05);
                if ((mean >= maxMean && lowerBound >= maxLowerBound)
                    || (mean < maxMean && upperBound > maxMean && lowerBound > maxLowerBound))
                {
                    maxMean = mean;
                    maxLowerBound = lowerBound;
                    maxUpperBound = upperBound;
                    IDOfOptimalEpiModeller = thisEpiModeller.ID;
                    positionOfOptimalEpiModeller = position;
                }
                ++position;
            }
            // find the optimal policy (assuming that only interval-based policies are considered) 
            interventionCombinationBode = ((IntervalBasedStaticPolicy)staticPolicyParameterDesigns[IDOfOptimalEpiModeller]).InterventionCombinationCode;
            optimalStartTimes = ((IntervalBasedStaticPolicy)staticPolicyParameterDesigns[IDOfOptimalEpiModeller]).TimeToUseInterventions;
            optimalNumOfDecisionPeriodsToUse = ((IntervalBasedStaticPolicy)staticPolicyParameterDesigns[IDOfOptimalEpiModeller]).NumOfDecisionPointsToUseInterventions;

            return (EpidemicModeller)_epidemicModellers[positionOfOptimalEpiModeller];
        }        
       
        // initialize calibration
        private void InitializeCalibration()
        {
            _modelSettings.SimulationRNDSeedsSource = EnumSimulationRNDSeedsSource.StartFrom0;

            // create an epidemic modeler
            _epidemicModeller = new EpidemicModeller(0, ref _excelInterface, ref _modelSettings);

            // read the observed epidemic history 
            ReadObservedHistory();

            // setup calibration            
            _epidemicModeller.SetUpCalibration();
            
            // specify RNG source
            //_epidemicModeller.SpecifyRNGSource(_firstRNGSeed, _distanceBtwRNGSeeds);
        }
        // initialize dynamic policy optimization
        private void InitializeDynamicPolicyOptimization()
        {
            // create and epidemic modeler (sequential processing)
            _epidemicModeller = new EpidemicModeller(0, ref _excelInterface, ref _modelSettings); // BuildAnEpidemicModeller_SequentialSimulation(0);

            // setup dynamic policy optimization setting
            _epidemicModeller.AddDynamicPolicySettings(ref _excelInterface);

            // set up Q-function approximation worksheet if necessary
            if (_excelInterface.GetIfToUseCurrentQFunctionApproximationSettings() == false)
            {                
                // decision names
                string[] strDecisionNames = _epidemicModeller.NamesOfDefaultInterventionsAndThoseSpecifiedByDynamicRule;
                // feature names
                string[] strFeatureNames = _epidemicModeller.FeatureNames;
                string[] strAbbreviatedFeatureNames = new string[_epidemicModeller.NumOfFeatures];
                foreach (Feature thisFeature in _epidemicModeller.Features)
                    strAbbreviatedFeatureNames[thisFeature.Index] = "F" + thisFeature.Index;
                
                // setup the feature worksheet
                _excelInterface.SetUpQFunctionWorksheet(
                    strDecisionNames, strFeatureNames, strAbbreviatedFeatureNames, _epidemicModeller.GetQFunctionPolynomialTerms());
            }

            // read initial q-function coefficients
            _modelSettings.ReadQFunctionCoefficientsInitialValues(ref _excelInterface, _epidemicModeller.NumOfFeatures);
           
            // set up ADP Iterations sheet
            _excelInterface.SetUpOptimizationOutput(_modelSettings.NumOfADPIterations* _modelSettings.NumOfSimRunsToBackPropogate);
            // setup simulation output sheet
            SetupSimulationOutputSheet();
        }
        
        //// build an epidemic modeler
        //private EpidemicModeller BuildAnEpidemicModeller_SequentialSimulation(int epiModellerID)
        //{
        //    // create and epidemic modeler
        //    EpidemicModeller epidemicModeller = new EpidemicModeller(epiModellerID);
        //    epidemicModeller.BuildAnEpidemicModelForSequentialSimulation(
        //        _excelEpidemic,
        //        _parametersSheet, _pathogenSheet, _classesSheet, _interventionSheet, _resourcesSheet, _processesSheet,
        //        _summationStatisticsSheet, _ratioStatisticsSheet,
        //        _connectionsMatrix);
        //    return epidemicModeller;
        //}
        //private EpidemicModeller BuildAnEpidemicModeller_ParallelSimulation(int epiModellerID, int numOfParallelEpidemics)
        //{
        //    // create and epidemic modeler
        //    EpidemicModeller epidemicModeller = new EpidemicModeller(epiModellerID);
        //    // build the parent epidemic modeler
        //    epidemicModeller.BuildCollectionOfEpidemicModelForParralelSimulation(
        //        numOfParallelEpidemics, _excelEpidemic,
        //        _parametersSheet, _pathogenSheet, _classesSheet, _interventionSheet, _resourcesSheet, _processesSheet,
        //        _summationStatisticsSheet, _ratioStatisticsSheet,
        //        _connectionsMatrix);
        //    return epidemicModeller;
        //} 

        // get an epidemic modeler for optimizing adaptive policies
        private EpidemicModeller GetAnEpidemicModellerForOptimizatingDynamicPolicies
            (int epiModellerID, double wtpForHealth, double harmonicStepSize_a, double epsilonGreedy_beta)
        {
            // build an epidemic model
            EpidemicModeller thisEpidemicModeller = new EpidemicModeller(epiModellerID, ref _excelInterface, ref _modelSettings);

            // add dynamic policy settings
            thisEpidemicModeller.AddDynamicPolicySettings(ref _excelInterface);

            // make it ready for optimization
            thisEpidemicModeller.SetUpOptimization(wtpForHealth, harmonicStepSize_a, epsilonGreedy_beta, 0);

            return thisEpidemicModeller;
        }
        // get an epidemic modeler to evaluate interval-based static policy
        private EpidemicModeller GetAnEpidemicModellerToEvaluateIntervalBasedStaticPolicy
            (int epiModellerID, int interventionsCombinationBinaryCode, double[] startTimes, int[] numOfDecisionPeriodsToUse, EnumSimulationRNDSeedsSource simulationRNDSeedsSource, int firstRNDSeedIfNotPrespecified)
        {
            // build an epidemic model
            EpidemicModeller thisEpidemicModeller = new EpidemicModeller(epiModellerID, ref _excelInterface, ref _modelSettings);

            // add static policy settings
            thisEpidemicModeller.AddAlwaysOnOffAndIntervalBasedStaticPolicySettings(
                SupportFunctions.ConvertToBase2FromBase10(interventionsCombinationBinaryCode, thisEpidemicModeller.NumOfInterventions), 
                startTimes, numOfDecisionPeriodsToUse);
            thisEpidemicModeller.ShouldStoreEpidemicTrajectories(false);

            return thisEpidemicModeller;
        }
        // get an epidemic modeler to evaluate threshold-based static policy
        private EpidemicModeller GetAnEpidemicModellerToEvaluateThresholdBasedStaticPolicy
            (int epiModellerID, int[] decisionIDs, double[] thresholds, int[] numOfDecisionPeriodsToUseInterventions)
        {
            // build an epidemic model
            EpidemicModeller thisEpidemicModeller = new EpidemicModeller(epiModellerID, ref _excelInterface, ref _modelSettings);

            // add static policy settings
            thisEpidemicModeller.AddThresholdBasedStaticPolicySettings(decisionIDs, thresholds, numOfDecisionPeriodsToUseInterventions);
            thisEpidemicModeller.ShouldStoreEpidemicTrajectories(false);

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
        // extract simulation outcomes from this epidemic modeler
        private double[][] ExtractSimulationOutcomes(EpidemicModeller epidemicModeller)
        {
            double[][] thisSimulationOutcomes = new double[1][];
            thisSimulationOutcomes[0] = new double[6];

            // simulation outcomes
            if (_modelSettings.ObjectiveFunction == EnumObjectiveFunction.MaximizeNHB)
            {
                thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.E_ObjectiveFunction] = epidemicModeller.obsTotalNHB.Mean;
                thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.stDev_ObjectiveFunction] = epidemicModeller.obsTotalNHB.StDev;
            }
            else
            {
                thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.E_ObjectiveFunction] = epidemicModeller.obsTotalNMB.Mean;
                thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.stDev_ObjectiveFunction] = epidemicModeller.obsTotalNMB.StDev;
            }
            thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.E_health] = epidemicModeller.obsTotalQALY.Mean;
            thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.stDev_health] = epidemicModeller.obsTotalQALY.StDev;
            thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.E_cost] = epidemicModeller.obsTotalCost.Mean;
            thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.stDev_cost] = epidemicModeller.obsTotalCost.StDev;

            return thisSimulationOutcomes;
        }
        private double[][] ExtractSimulationOutcomes(EpidemicModeller epidemicModeller, double wtpForHealth)
        {
            wtpForHealth = Math.Max(wtpForHealth, SupportProcedures.minimumWTPforHealth);

            double[][] thisSimulationOutcomes = new double[1][];
            thisSimulationOutcomes[0] = new double[6];

            // simulation outcomes
            thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.E_ObjectiveFunction] 
                = epidemicModeller.GetObjectiveFunction_Mean(_modelSettings.ObjectiveFunction, wtpForHealth);
            thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.stDev_ObjectiveFunction] 
                = epidemicModeller.GetObjectiveFunction_StDev(_modelSettings.ObjectiveFunction, wtpForHealth);
            
            thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.E_health] = epidemicModeller.obsTotalQALY.Mean;
            thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.stDev_health] = epidemicModeller.obsTotalQALY.StDev;
            thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.E_cost] = epidemicModeller.obsTotalCost.Mean;
            thisSimulationOutcomes[0][(int)ExcelInterface.enumSimulationOutcomesOffsets.stDev_cost] = epidemicModeller.obsTotalCost.StDev;

            return thisSimulationOutcomes;
        }
 

        // ******* model construction subs ***********
        #region Model construction subs
            
        // set up observation worksheet
        private void ReadObservedHistory()
        {
            if (!(_modelSettings.ModelUse == EnumModelUse.Calibration|| 
                    _modelSettings.DecisionRule == EnumEpiDecisions.PredeterminedSequence))
                return;

            // check if current epidemic history can be used if not get the history from the user
            if (_excelInterface.GetIfUseCurrentObservationsToCalibrate() == false)
            {
                // get the names of calibration data
                string[] namesOfCalibrationTargets = _epidemicModeller.GetNamesOfSpecialStatisticsIncludedInCalibratoin();

                // create the header
                string[,] strObsHeader = new string[2, 2 * namesOfCalibrationTargets.Length];
                for (int j = 0; j < namesOfCalibrationTargets.Length; j++)
                {
                    strObsHeader[0, 2 * j] = namesOfCalibrationTargets[j];
                    strObsHeader[0, 2 * j + 1] = "";
                    strObsHeader[1, 2 * j] = "Observed Value";
                    strObsHeader[1, 2 * j + 1] = "Weight";
                }

                // get the index of observation periods
                int numOfObsPeriods = (int)(_modelSettings.TimeIndexToStop/ _modelSettings.ObservationPeriodLength);
                int[] obsPeriodIndex = new int[numOfObsPeriods];
                for (int i = 0; i < numOfObsPeriods; i++)
                    obsPeriodIndex[i] = i+1;

                // set up the epidemic history sheet
                _excelInterface.SetupEpidemicHistoryWorksheet(strObsHeader, obsPeriodIndex); 
            }
            // read past actions
            _modelSettings.ReadPastActions(ref _excelInterface);
            _modelSettings.ReadPastObservations(ref _excelInterface, _epidemicModeller.NumOfCalibratoinTargets);
        }

        #endregion

        // report results subs
        #region Report results subs

        // setup simulation output sheet
        private void SetupSimulationOutputSheet()
        {
            //if (_epidemicModeller.ReportEpidemicTrajectories == false)
            //    return;
            
            // define the header
            string[] arrTimeBasedOutputsHeading = new string[0];
            string[] arrIntervalBasedOutputs = new string[0];
            string[] arrObservableOutputs = new string[0];
            string[] arrResourceOutputs = new string[0];

            // create headers
            ComputationLib.SupportFunctions.AddToEndOfArray(ref arrTimeBasedOutputsHeading, "Simulation Replication");
            ComputationLib.SupportFunctions.AddToEndOfArray(ref arrTimeBasedOutputsHeading, "Simulation Time");
            ComputationLib.SupportFunctions.AddToEndOfArray(ref arrIntervalBasedOutputs, "Decision Code");
            ComputationLib.SupportFunctions.AddToEndOfArray(ref arrIntervalBasedOutputs, "Observation Period");            
            ComputationLib.SupportFunctions.AddToEndOfArray(ref arrObservableOutputs, "Surveillance Period");

            // class headers
            foreach (Class thisClass in _epidemicModeller.Classes)            
            {
                if (thisClass.ShowIncidence)
                    ComputationLib.SupportFunctions.AddToEndOfArray(ref arrIntervalBasedOutputs, "To: " + thisClass.Name);
                if (thisClass.ShowPrevalence)
                    ComputationLib.SupportFunctions.AddToEndOfArray(ref arrTimeBasedOutputsHeading, "In: " + thisClass.Name);
                if (thisClass.ShowAccumIncidence)
                    ComputationLib.SupportFunctions.AddToEndOfArray(ref arrTimeBasedOutputsHeading, "Sum To: " + thisClass.Name);
            }
            // summation statistics header
            foreach (SummationStatistics thisSumStat in _epidemicModeller.SummationStatistics.Where(s => s.IfDisplay))
            {
                switch (thisSumStat.Type)
                {
                    case SummationStatistics.enumType.Incidence:
                        ComputationLib.SupportFunctions.AddToEndOfArray(ref arrIntervalBasedOutputs, thisSumStat.Name);
                        break;
                    case SummationStatistics.enumType.AccumulatingIncident:
                    case SummationStatistics.enumType.Prevalence:
                        ComputationLib.SupportFunctions.AddToEndOfArray(ref arrTimeBasedOutputsHeading, thisSumStat.Name);
                        break;
                }
            }

            // surveillance statistics header for summation statistics
            foreach (SummationStatistics thisSumStat in _epidemicModeller.SummationStatistics.Where(s => (s.SurveillanceDataAvailable && s.IfDisplay)))
            {    
                 ComputationLib.SupportFunctions.AddToEndOfArray(ref arrObservableOutputs, "Surveillance | " + thisSumStat.Name);
            }

            // ratio statistics
            foreach (RatioStatistics thisRatioStat in _epidemicModeller.RatioStatistics.Where(s => s.IfDisplay))
            {
                // find the type of this ratio statistics
                switch (thisRatioStat.Type)
                {
                    case APACElib.RatioStatistics.enumType.IncidenceOverIncidence:
                        ComputationLib.SupportFunctions.AddToEndOfArray(ref arrIntervalBasedOutputs, thisRatioStat.Name);
                        break;
                    case APACElib.RatioStatistics.enumType.AccumulatedIncidenceOverAccumulatedIncidence:
                        ComputationLib.SupportFunctions.AddToEndOfArray(ref arrTimeBasedOutputsHeading, thisRatioStat.Name);
                        break;
                    case APACElib.RatioStatistics.enumType.PrevalenceOverPrevalence:
                        ComputationLib.SupportFunctions.AddToEndOfArray(ref arrTimeBasedOutputsHeading, thisRatioStat.Name);
                        break;
                    case APACElib.RatioStatistics.enumType.IncidenceOverPrevalence:
                        ComputationLib.SupportFunctions.AddToEndOfArray(ref arrIntervalBasedOutputs, thisRatioStat.Name);
                        break;
                }

                // surveillance statistics header for ratio statistics
                if (thisRatioStat.SurveillanceDataAvailable)
                    ComputationLib.SupportFunctions.AddToEndOfArray(ref arrObservableOutputs, "Surveillance | " + thisRatioStat.Name);
            }

            // resource header
            foreach (Resource thisResource in _epidemicModeller.Resources)
            {
                if (thisResource.ShowAvailability)
                    ComputationLib.SupportFunctions.AddToEndOfArray(ref arrResourceOutputs, thisResource.Name);
            }

            // write header
            _excelInterface.SetupSimulationOutputSheet(arrTimeBasedOutputsHeading, arrIntervalBasedOutputs, arrObservableOutputs, arrResourceOutputs);                             
        }
        // report simulation statistics
        private void ReportTrajectoriesAndSimulationStatistics(EpidemicModeller thisEpidemicModeller)
        {
            // first find the strings of past action combinations
            string[] strActionCombinations = null;
            foreach (int[] thisActionCombination in thisEpidemicModeller.PastActionCombinations)
                SupportFunctions.AddToEndOfArray(ref strActionCombinations, SupportFunctions.ConvertArrayToString(thisActionCombination,","));

            // report trajectories
            if (thisEpidemicModeller.StoreEpidemicTrajectories)
                _excelInterface.ReportAnEpidemicTrajectory
                    (thisEpidemicModeller.SimulationTimeBasedOutputs, strActionCombinations, thisEpidemicModeller.SimulationIntervalBasedOutputs,
                    thisEpidemicModeller.TimeOfSimulationObservableOutputs, thisEpidemicModeller.SimulationMonitoredOutputs, thisEpidemicModeller.SimulationResourcesOutputs);

            string[] strSummaryStatistics = null;
            string[] strClassAndSumStatistics = null;
            string[] strRatioStatistics = null;
            string[] strComputationStatistics = null;
            string[] strIterationOutcomes = null;
            double[,] arrSummaryStatistics = null;
            double[][] arrClassAndSumStatistics = null;
            double[][] arrRatioStatistics = null;
            double[,] arrComputationStatistics = null;
            double[][] arrIterationOutcomes = null;

            // get these statistics
            thisEpidemicModeller.GetSimulationStatistics(
                ref strSummaryStatistics, ref strClassAndSumStatistics, ref strRatioStatistics, ref strComputationStatistics, ref strIterationOutcomes,
                ref arrSummaryStatistics, ref arrClassAndSumStatistics, ref arrRatioStatistics, ref arrComputationStatistics, ref arrIterationOutcomes);

            // report
            _excelInterface.ReportSimulationStatistics(
                strSummaryStatistics, arrSummaryStatistics,
                strClassAndSumStatistics, SupportFunctions.ConvertFromJaggedArrayToRegularArray(arrClassAndSumStatistics, 3),
                strRatioStatistics, SupportFunctions.ConvertFromJaggedArrayToRegularArray(arrRatioStatistics, 3), 
                strComputationStatistics, arrComputationStatistics,
                strIterationOutcomes, SupportFunctions.ConvertFromJaggedArrayToRegularArray(arrIterationOutcomes,arrIterationOutcomes[0].Length));

            // report sampled parameter values
            _excelInterface.ReportSampledParameterValues(thisEpidemicModeller.ParameterNames, thisEpidemicModeller.SimulationIterations_itrs ,thisEpidemicModeller.SimulationIterations_RNGSeeds,
                SupportFunctions.ConvertFromJaggedArrayToRegularArray(thisEpidemicModeller.SimulationIterations_ParameterValues, thisEpidemicModeller.ParameterNames.Length));
        }
        // report calibration result
        private void ReportCalibrationResult()
        {
            int numOfParametersToCalibrate = _epidemicModeller.Calibration.NamesOfParameters.Length;
            // report
            _excelInterface.ReportCalibrationResults(
                _epidemicModeller.ActualTimeUsedByCalibration / 60,
                _epidemicModeller.NumOfTrajectoriesDiscardedByCalibration,
                _epidemicModeller.GetNamesOfSpecialStatisticsIncludedInCalibratoin(),
                //_epidemicModeller.Calibration.NamesOfParameters,
                _epidemicModeller.Calibration.NamesOfSimOutsWithNonZeroWeights,                
                _epidemicModeller.Calibration.SelectedSimulationItrs,
                _epidemicModeller.Calibration.SelectedSimulationRNDSeeds,
                //_epidemicModeller.Calibration.SelectedGoodnessOfFit,
                //_epidemicModeller.Calibration.SelectedParameterValues,
                _epidemicModeller.Calibration.SelectedSimObservations);
        }
        // report dynamic policy optimization result
        private void ReportADPResultsForThisEpidemic(EpidemicModeller thisEpidemicModeller, double harmonicStep_a, double epsilonGreedy_beta)
        {
            // report Q-function coefficients
            _excelInterface.ReportQFunctionsCoefficientEstimates(thisEpidemicModeller.GetQFunctionCoefficientEstiamtes(), thisEpidemicModeller.NumOfFeatures);

            // report optimization iterations
            _excelInterface.ReportADPIterations(thisEpidemicModeller.GetADPIterations(), harmonicStep_a, epsilonGreedy_beta);

            // read dynamic policy
            if (thisEpidemicModeller.NumOfFeatures == 1)
            {
                string featureName = "";
                double[] headers = null;
                int[] optimalDecisions = null;
                thisEpidemicModeller.GetOptimalDynamicPolicy(ref featureName, ref headers, ref optimalDecisions, _modelSettings.NumOfIntervalsToDescretizeFeatures);

                // report the dynamic policy
                _excelInterface.Report1DimOptimalPolicy(featureName, headers, optimalDecisions);

            }
            else if (thisEpidemicModeller.NumOfFeatures == 2)
            {
                string[] strFeatureNames = null;
                double[][] headers = null;
                int[,] optimalDecisions = null;
                thisEpidemicModeller.GetOptimalDynamicPolicy(ref strFeatureNames, ref headers, ref optimalDecisions, _modelSettings.NumOfIntervalsToDescretizeFeatures);

                // report the dynamic policy
                _excelInterface.Report2DimOptimalPolicy(strFeatureNames, headers, optimalDecisions);
            }
            else if (thisEpidemicModeller.NumOfFeatures == 3)
            {
                string[] strFeatureNames = null;
                double[][] headers = null;
                int[][,] optimalDecisions = null;
                thisEpidemicModeller.GetOptimalDynamicPolicy(ref strFeatureNames, ref headers, ref optimalDecisions, _modelSettings.NumOfIntervalsToDescretizeFeatures);

                // report the dynamic policy
                _excelInterface.Report3DimOptimalPolicy(strFeatureNames, headers, optimalDecisions);
            }
        }
        // report the results of adaptive policy optimization 
        public void ReportADPResultsForAllEpidemic()
        {
            double optHarmonicStep_a = 0, optEpsilonGreedy_beta = 0;
            double[][] adpOptParameterDesigns = new double[0][];
            double[][] adpSASimulationOutcomes = new double[0][];
            double[][] adpSASimulationIterations = new double[0][];              

            double thisWTPForHealth = _modelSettings.WtpForHealth_min;
            while (thisWTPForHealth <= _modelSettings.WtpForHealth_max)
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
                adpSASimulationOutcomes = SupportFunctions.ConcatJaggedArray(adpSASimulationOutcomes, ExtractSimulationOutcomes(thisEpiModeller));

                // simulation iterations
                double[][] thisSimulationIterations = new double[_modelSettings.NumOfSimulationIterations][];
                for (int simItr = 0; simItr < _modelSettings.NumOfSimulationIterations; simItr++)
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
                            = thisEpiModeller.SimulationIterations_NHB[simItr];
                    else
                        thisSimulationIterations[simItr][(int)ExcelInterface.enumADPSASimulationIterationsOffsets.objectiveFunction]
                            = thisEpiModeller.SimulationIterations_NMB[simItr];
                    thisSimulationIterations[simItr][(int)ExcelInterface.enumADPSASimulationIterationsOffsets.health]
                        = thisEpiModeller.SimulationIterations_QALY[simItr];
                    thisSimulationIterations[simItr][(int)ExcelInterface.enumADPSASimulationIterationsOffsets.cost]
                        = thisEpiModeller.SimulationIterations_Cost[simItr];
                    thisSimulationIterations[simItr][(int)ExcelInterface.enumADPSASimulationIterationsOffsets.annualCost]
                        = thisEpiModeller.SimulationIterations_AnnualCost[simItr];
                }
                // concatenate
                adpSASimulationIterations = SupportFunctions.ConcatJaggedArray(adpSASimulationIterations, thisSimulationIterations);

                // increment the wtp for health 
                thisWTPForHealth += _modelSettings.WtpForHealth_step;
            }

            // report
            _excelInterface.ReportADPResultsForAllEpidemic(
                SupportFunctions.ConvertFromJaggedArrayToRegularArray(adpOptParameterDesigns, 3),
                SupportFunctions.ConvertFromJaggedArrayToRegularArray(adpSASimulationOutcomes, 6),
                SupportFunctions.ConvertFromJaggedArrayToRegularArray(adpSASimulationIterations, 7), 
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
            _excelInterface.ReportADPComputationTimes(measures, statistics);
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

            double thisWTPForHealth = _modelSettings.WtpForHealth_min;
            while (thisWTPForHealth <= _modelSettings.WtpForHealth_max)
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
                simulationOutcomes = SupportFunctions.ConcatJaggedArray(simulationOutcomes, ExtractSimulationOutcomes(thisEpiModeller, thisWTPForHealth));

                // simulation iterations
                double[][] thisSimulationIterations = new double[_modelSettings.NumOfSimulationIterations][];
                for (int simItr = 0; simItr < _modelSettings.NumOfSimulationIterations; simItr++)
                {
                    thisSimulationIterations[simItr] = new double[5];
                    thisSimulationIterations[simItr][(int)ExcelInterface.enumStaticPolicyOptimizationSimulationIterationsOffsets.wtpForHealth] = thisWTPForHealth;

                    if (_modelSettings.ObjectiveFunction == EnumObjectiveFunction.MaximizeNHB)
                        thisSimulationIterations[simItr][(int)ExcelInterface.enumStaticPolicyOptimizationSimulationIterationsOffsets.objectiveFunction]
                            = thisEpiModeller.SimulationIterations_QALY[simItr] - thisEpiModeller.SimulationIterations_Cost[simItr] / Math.Max(thisWTPForHealth, SupportProcedures.minimumWTPforHealth);
                    else
                        thisSimulationIterations[simItr][(int)ExcelInterface.enumStaticPolicyOptimizationSimulationIterationsOffsets.objectiveFunction]
                            = thisWTPForHealth * thisEpiModeller.SimulationIterations_QALY[simItr] - thisEpiModeller.SimulationIterations_Cost[simItr];
                    thisSimulationIterations[simItr][(int)ExcelInterface.enumStaticPolicyOptimizationSimulationIterationsOffsets.health]
                        = thisEpiModeller.SimulationIterations_QALY[simItr];
                    thisSimulationIterations[simItr][(int)ExcelInterface.enumStaticPolicyOptimizationSimulationIterationsOffsets.cost]
                        = thisEpiModeller.SimulationIterations_Cost[simItr];
                    thisSimulationIterations[simItr][(int)ExcelInterface.enumStaticPolicyOptimizationSimulationIterationsOffsets.annualCost]
                        = thisEpiModeller.SimulationIterations_AnnualCost[simItr];
                }
                // concatenate
                simulationIterations = SupportFunctions.ConcatJaggedArray(simulationIterations, thisSimulationIterations);
                
                // increment the wtp for health 
                thisWTPForHealth += _modelSettings.WtpForHealth_step;
            }

            // report
            _excelInterface.ReportStaticPolicyOptimization( wtp, 
                SupportFunctions.ConvertFromJaggedArrayToRegularArray(staticPolicies, 3),
                SupportFunctions.ConvertFromJaggedArrayToRegularArray(simulationOutcomes, 6),
                SupportFunctions.ConvertFromJaggedArrayToRegularArray(simulationIterations, 5),
                _modelSettings.ObjectiveFunction);

        }
        #endregion
    }
}
