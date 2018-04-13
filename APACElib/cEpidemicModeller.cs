using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SimulationLib;
using RandomVariateLib;
using ComputationLib;

namespace APACElib
{
    public class EpidemicModeller
    {
        // Variable Definition 
        public int ID { get; private set; }
        private ModelSettings _set;
        private RNDSeedGenerator _seedGenerator;
        private Epidemic _parentEpidemic;
        private List<Epidemic> _epidemics = new List<Epidemic>();
        public Epidemic ParentEpidemic { get => _parentEpidemic; }        
        public ModelInfo ModelInfo { get; private set; }
        public SimSummary SimSummary { get; private set; }
        public Timer Timer { get; private set; }

        // simulation setting
        RNG _rng = new RNG(0);

        // calibration
        public CalibrationOld Calibration { get; set; }

        // Instantiation
        public EpidemicModeller(int ID, ref ExcelInterface excelInterface, ref ModelSettings modelSettings)
        {
            this.ID = ID;
            _set = modelSettings;

            // build a parent epidemic model 
            _parentEpidemic = new Epidemic(0);
            _parentEpidemic.BuildModel(ref _set);

            // read contact matrices
            _set.ReadContactMatrices(ref excelInterface, _parentEpidemic.FOIModel.NumOfIntrvnAffectingContacts);

            // extract model information 
            ModelInfo = new ModelInfo(ref _parentEpidemic);

            // if use parallel computing, create a collection of epidemics
            if (_set.UseParallelComputing)
            {
                // find how many epi model to create
                int numOfEpis = 0;
                switch (_set.ModelUse)
                {
                    case EnumModelUse.Simulation:
                        numOfEpis = _set.NumOfSimItrs;
                        break;
                    case EnumModelUse.Calibration:
                        numOfEpis = _set.NumOfSimulationsRunInParallelForCalibration;
                        break;
                }

                // create the epi models
                _epidemics.Clear();
                Object thisLock = new Object();
                Parallel.For(0, numOfEpis, simItr =>
                {
                    // create an epidemic
                    Epidemic thisEpidemic = new Epidemic(simItr);
                    // add this epidemic
                    lock (thisLock)
                        {
                            _epidemics.Add(thisEpidemic);
                        }
                });
            }

            //// read prespecified interventions
            //if (_modelSettings.TempEpidemic.DecisionRule == enumDecisionRule.PredeterminedSequence ||
            //    _modelSettings.ModelUse == enumModelUse.Calibration)
            //    _prespecifiedDecisionsOverObservationPeriods = (int[][])_modelSettings.PrespecifiedSequenceOfInterventions.Clone();
        }

        // simulate several epidemics
        public void SimulateEpidemics()
        {
            // create a rnd seed generator 
            _seedGenerator = new RNDSeedGenerator(ref _set, ref _rng);
            // simulatoin summary
            SimSummary = new SimSummary(ref _set, ref _parentEpidemic);
            SimSummary.Reset();

            // simulation time
            Timer.Start();

            // use parallel computing? 
            if (!_set.UseParallelComputing)
            {
                int seed = 0;
                for (int simItr = 0; simItr < _set.NumOfSimItrs; ++simItr)
                {
                    // find the RND seed for this iteration
                    seed = _seedGenerator.FindRNDSeed(simItr);

                    // simulate one epidemic trajectory
                    _parentEpidemic.SimulateTrajectoriesUntilOneAcceptibleFound(
                        seed,
                        seed + Math.Max(_set.DistanceBtwRNGSeeds, 1),
                        simItr,
                        _set.TimeIndexToStop);

                    // store epidemic trajectories and outcomes
                    SimSummary.Add(_parentEpidemic);
                }
            }
            else // (_modelSettings.UseParallelComputing == true)
            {
                int seed = 0;
                Parallel.ForEach(_epidemics, thisEpidemic =>
                {
                    // build the parent epidemic model
                    thisEpidemic.BuildModel(ref _set);

                    // find the RND seed for this iteration
                    seed = _seedGenerator.FindRNDSeed(thisEpidemic.ID);

                    // simulate
                    thisEpidemic.SimulateTrajectoriesUntilOneAcceptibleFound(
                        seed,
                        seed + Math.Max(_set.DistanceBtwRNGSeeds, 1),
                        ((Epidemic)thisEpidemic).ID,
                        _set.TimeIndexToStop);
                });

                // store epidemic trajectories and outcomes
                foreach (Epidemic thisEpidemic in _epidemics)
                    SimSummary.Add(thisEpidemic);
            }
            // simulation run time
            Timer.Stop();
        }
        // calibrate
        public void Calibrate(int numOfInitialSimulationRuns, int numOfFittestRunsToReturn)
        {   
            int calibrationTimeHorizonIndex = _set.TimeIndexToStop;
            int numOfSimulationsRunInParallelForCalibration = _set.NumOfSimulationsRunInParallelForCalibration;

            // reset calibration
            Calibration.Reset();
            // toggle to calibration
            ToggleModellerTo(EnumModelUse.Calibration, EnumEpiDecisions.PredeterminedSequence, false); 

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
            if (_set.UseParallelComputing)
            {
                Parallel.ForEach(_epidemics, thisEpidemic =>
                {
                    // build the parent epidemic model
                    thisEpidemic.BuildModel(ref _set);
                });
            }

            while (simItr <  numOfInitialSimulationRuns - 1)
                //|| !_calibration.IfAcceptedSimulationRunsAreSymmetricAroundObservations)
            {
                // use parallel computing? 
                if (_set.UseParallelComputing == false)
                {
                    #region Use sequential computing
                    // increment the simulation iteration
                    ++simItr;
                    // find the RND seed for this iteration
                    int rndSeedToGetAnAcceptibleEpidemic = _seedGenerator.FindRNDSeed(simItr);                    

                    // simulate one epidemic trajectory
                    //_parentEpidemic.SimulateTrajectoriesUntilOneAcceptibleFound(rndSeedToGetAnAcceptibleEpidemic, int.MaxValue, simItr, calibrationTimeHorizonIndex);
                    _parentEpidemic.SimulateOneTrajectory(rndSeedToGetAnAcceptibleEpidemic, simItr, calibrationTimeHorizonIndex);

                    // calibration time
                    _totalSimulationTimeUsedByCalibration += _parentEpidemic.Timer.TimePassed;

                    // find the number of discarded trajectories    
                    if (_parentEpidemic.SeedProducedAcceptibleTraj == -1)
                        _numOfTrajectoriesDiscardedByCalibration += 1;
                    else
                    {
                        // add this simulation observations
                        //_calibration.AddResultOfASimulationRun(simItr, _parentEpidemic.RNDSeedResultedInAnAcceptibleTrajectory, _parentEpidemic.GetValuesOfParametersToCalibrate(),
                        //SupportFunctions.ConvertFromJaggedArrayToRegularArray(_parentEpidemic.CalibrationObservation, _parentEpidemic.NumOfCalibratoinTargets));
                        double[,] mOfObs = SupportFunctions.ConvertFromJaggedArrayToRegularArray(new double[0][], 1);//_parentEpidemic.NumOfCalibratoinTargets);
                        double[] par = new double[0];
                        Calibration.AddResultOfASimulationRun(simItr, _parentEpidemic.SeedProducedAcceptibleTraj, ref par, ref mOfObs);

                        // find the fit of the stored simulation results
                        Calibration.FindTheFitOfRecordedSimulationResults(_set.UseParallelComputing);
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
                        rndSeedToGetAnAcceptibleEpidemic = _seedGenerator.FindRNDSeed(simItr);

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
                        _totalSimulationTimeUsedByCalibration += thisEpidemic.Timer.TimePassed;
                        // sim itr
                        simItr = numOfSimulationsRunInParallelForCalibration * parallelLoopIndex + thisEpidemic.ID;

                        // find the number of discarded trajectories    
                        //_numOfTrajectoriesDiscardedByCalibration += thisEpidemic.NumOfDiscardedTrajectoriesAmongCalibrationRuns;
                        if (thisEpidemic.SeedProducedAcceptibleTraj == -1)
                            _numOfTrajectoriesDiscardedByCalibration += 1;
                        else
                        {
                            double[,] mOfObs = SupportFunctions.ConvertFromJaggedArrayToRegularArray(new double[0][], 1);// thisEpidemic.NumOfCalibratoinTargets);
                            double[] par = new double[0];
                            // add this simulation observations
                            Calibration.AddResultOfASimulationRun(simItr, thisEpidemic.SeedProducedAcceptibleTraj, ref par, ref mOfObs);
                        }
                    }
                    
                    // find the fit of the stored simulation results
                    Calibration.FindTheFitOfRecordedSimulationResults(_set.UseParallelComputing);

                    // increment the loop id
                    ++parallelLoopIndex;
                    simItr = simItrParallel;
                    #endregion
                }                
            }

            // find the fittest runs
            Calibration.FindTheFittestSimulationRuns(numOfFittestRunsToReturn);

            // computation time
            endTime = Environment.TickCount;
            _actualTimeUsedByCalibration = (double)(endTime - startTime) / 1000;

        }

        // simulate the optimal dynamic policy
        public void SimulateTheOptimalDynamicPolicy(int numOfSimulationIterations, int timeIndexToStop, int warmUpPeriodIndex, bool storeEpidemicTrajectories)
        {
            // toggle to simulation
            ToggleModellerTo(EnumModelUse.Simulation, EnumEpiDecisions.SpecifiedByPolicy, storeEpidemicTrajectories);            
            // simulate epidemic (sequential)
            SimulateEpidemics();
        }
       
        // change the status of storing epidemic trajectories
        public void ShouldStoreEpidemicTrajectories(bool yesOrNo)
        {
            _storeEpidemicTrajectories = yesOrNo;
            if (_set.UseParallelComputing)
            {
                foreach (Epidemic thisEpidemic in _epidemics)
                    thisEpidemic.StoreEpidemicTrajectories = yesOrNo;
            }
            else
                _parentEpidemic.StoreEpidemicTrajectories = yesOrNo;
        }
        
        // get possible intervention combinations for on/off static policies
        public ArrayList GetIntervalBasedStaticPoliciesDesigns()
        {
            //if (_set.UseParallelComputing)
            //    return ((Epidemic)_epidemics[0]).GetIntervalBasedStaticPoliciesDesigns();
            //else
            //    return _parentEpidemic.GetIntervalBasedStaticPoliciesDesigns();
            return null;
        }        

        // add policy related settings
        public void AddDynamicPolicySettings(ref ExcelInterface excelInterface)
        {
            //if (_set.UseParallelComputing)
            //{                
            //    Parallel.ForEach(_epidemics.Cast<object>(), thisEpidemic =>
            //    {
            //        // setup policy-related settings
            //        ((Epidemic)thisEpidemic).SetupDynamicPolicySettings(
            //            _set.QFunApxMethod, _set.IfEpidemicTimeIsUsedAsFeature,
            //            _set.DegreeOfPolynomialQFunction, _set.L2RegularizationPenalty);                    
            //    });
            //    // find the number of features
            //    _numOfFeatures = ((Epidemic)_epidemics[0]).NumOfFeatures;
            //    // read feature names                
            //    _featureNames = new string[((Epidemic)_epidemics[0]).NumOfFeatures];
            //    foreach (Feature thisFeature in ((Epidemic)_epidemics[0]).Features)
            //        _featureNames[thisFeature.Index] = thisFeature.Name;
            //}
            //else
            //{
            //    // setup policy-related settings
            //    _parentEpidemic.SetupDynamicPolicySettings(
            //        _set.QFunApxMethod, _set.IfEpidemicTimeIsUsedAsFeature,
            //        _set.DegreeOfPolynomialQFunction, _set.L2RegularizationPenalty);
            //    // find the number of features
            //    _numOfFeatures = _parentEpidemic.NumOfFeatures;
            //    // read feature names                
            //    _featureNames = new string[_parentEpidemic.NumOfFeatures];
            //    foreach (Feature thisFeature in _parentEpidemic.Features)
            //        _featureNames[thisFeature.Index] = thisFeature.Name;
            //}

            //// update the q-function coefficients
            //double[] qFunCoefficients = excelInterface.GetQFunctionCoefficients(_numOfFeatures);
            //if (_set.UseParallelComputing)
            //{
            //    Parallel.ForEach(_epidemics.Cast<object>(), thisEpidemic =>
            //    {
            //        // q-function coefficients
            //        ((Epidemic)thisEpidemic).UpdateQFunctionCoefficients(qFunCoefficients);
            //    });
            //}
            //else
            //{
            //    // q-function coefficients
            //    _parentEpidemic.UpdateQFunctionCoefficients(qFunCoefficients);
            //}
        }
        
        public void UpdateQFunctionCoefficients(double[] qFunctionCoefficients)
        {
            if (_set.UseParallelComputing)
            {
                Parallel.ForEach(_epidemics.Cast<object>(), thisEpidemic =>
                {
                    // q-function coefficients
                    //((Epidemic)thisEpidemic).UpdateQFunctionCoefficients(qFunctionCoefficients);
                });
            }
            else
            {
                // q-function coefficients
                //_parentEpidemic.UpdateQFunctionCoefficients(qFunctionCoefficients);
            }
        }   
        // set up calibration
        public void SetUpCalibration()
        {
            Calibration = new CalibrationOld(0);
            _storeEpidemicTrajectories = false;                        

            // add observations
            AddObservationsToSetUpCalibration(ref _parentEpidemic, _set.MatrixOfObservationsAndWeights);
        }
   
        // add observations to set up calibration
        private void AddObservationsToSetUpCalibration(ref Epidemic thisEpiModel, double[,] matrixOfObservationsAndWeights)
        {
            int j = 0;
            int numOfRows = matrixOfObservationsAndWeights.GetLength(0);

            // go over summation statistics
            foreach (SumTrajectory sumStat in thisEpiModel.SumTrajectories.Where(s => s.CalibInfo.IfIncluded))
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
                //switch (sumStat.GoodnessOfFitMeasure)
                //{
                //    case CalibrationTarget.enumGoodnessOfFitMeasure.SumSqurError_timeSeries:
                //        _calibration.AddACalibrationTarget_timeSeries(sumStat.Name, sumStat.Weight_overalFit, arrObservations, arrWeights);
                //        break;
                //    case CalibrationTarget.enumGoodnessOfFitMeasure.SumSqurError_average:
                //        _calibration.AddACalibrationTarget_aveTimeSeries(sumStat.Name, sumStat.Weight_overalFit, arrObservations, arrWeights);
                //        break;
                //    case CalibrationTarget.enumGoodnessOfFitMeasure.Fourier:
                //        _calibration.AddACalibrationTarget_fourier(sumStat.Name, sumStat.Weight_overalFit, arrObservations, sumStat.Weight_fourierSimilarities);
                //        break;
                //}
            }

            // go over ratio statistics
            foreach (RatioTrajectory ratioStat in thisEpiModel.RatioTrajectories.Where(r => r.CalibInfo.IfIncluded))
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
                //switch (ratioStat.GoodnessOfFitMeasure)
                //{
                //    case CalibrationTarget.enumGoodnessOfFitMeasure.SumSqurError_timeSeries:
                //        _calibration.AddACalibrationTarget_timeSeries(ratioStat.Name, ratioStat.Weight_overalFit, arrObservations, arrWeights);
                //        break;
                //    case CalibrationTarget.enumGoodnessOfFitMeasure.SumSqurError_average:
                //        _calibration.AddACalibrationTarget_aveTimeSeries(ratioStat.Name, ratioStat.Weight_overalFit, arrObservations, arrWeights);
                //        break;
                //    case CalibrationTarget.enumGoodnessOfFitMeasure.Fourier:
                //        _calibration.AddACalibrationTarget_fourier(ratioStat.Name, ratioStat.Weight_overalFit, arrObservations, ratioStat.Weight_fourierSimilarities);
                //        break;
                //}
            }
        }
        // set up optimization 
        public void SetUpOptimization(
            double wtpForHealth, double harmonicRule_a, double epsilonGreedy_beta, double epsilonGreedy_delta)
        {
            //// setup ADP algorithm
            //_parentEpidemic.SetUpADPAlgorithm(_set.ObjectiveFunction, _set.NumOfADPIterations, _set.NumOfSimRunsToBackPropogate,
            //    wtpForHealth, harmonicRule_a, epsilonGreedy_beta, epsilonGreedy_delta, true);
            //// update initial coefficients of the Q-function
            //_parentEpidemic.UpdateQFunctionCoefficients(_set.QFunctionCoefficientsInitialValues);
            //// don't store trajectories while simulating
            //_parentEpidemic.StoreEpidemicTrajectories = false;

            //// specify rnd seeds for ADP algorithm
            //switch (_set.SimulationRNDSeedsSource)
            //{
            //    case EnumSimulationRNDSeedsSource.StartFrom0:
            //        break;
            //    case EnumSimulationRNDSeedsSource.PrespecifiedSquence:
            //        _parentEpidemic.SetUpADPRandomNumberSource(_set.RndSeeds);
            //        break;
            //    case EnumSimulationRNDSeedsSource.WeightedPrespecifiedSquence:
            //        _parentEpidemic.SetUpADPRandomNumberSource(_set.RndSeeds, _set.RndSeedsGoodnessOfFit);
            //        break;
            //}
        }

        // ******** Subs to return some information about the model *******
        #region Subs to return some information about the model
        // get name of special statistics included in calibratoin 
        public string[] GetNamesOfSpecialStatisticsIncludedInCalibratoin()
        {
            // find the names of the parameters
            string[] names = new string[0];

            // summation statistics
            foreach (SumTrajectory thisSumTraj in _parentEpidemic.SumTrajectories.Where(s => s.CalibInfo.IfIncluded))
                SupportFunctions.AddToEndOfArray(ref names, thisSumTraj.Name);
            // ratio statistics
            foreach (RatioTrajectory thisRatioTraj in _parentEpidemic.RatioTrajectories.Where(s => s.CalibInfo.IfIncluded))
                SupportFunctions.AddToEndOfArray(ref names, thisRatioTraj.Name);
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
                (int)ExcelInterface.enumSimulationStatisticsColumns.Mean - 2] = _obsTotalDALY.Mean;
            arrSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.TotalQALY - 1,
                (int)ExcelInterface.enumSimulationStatisticsColumns.StDev - 2] = _obsTotalDALY.StDev;
            arrSummaryStatistics[(int)ExcelInterface.enumSimulationStatisticsRows.TotalQALY - 1,
                (int)ExcelInterface.enumSimulationStatisticsColumns.StError - 2] = _obsTotalDALY.StErr;
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
        public double GetObjectiveFunction_Mean(EnumObjectiveFunction objectiveFunction)
        {
            double mean = 0;
            switch (objectiveFunction)
            {
                case EnumObjectiveFunction.MaximizeNHB:
                    mean = _obsTotalNHB.Mean;
                    break;
                case EnumObjectiveFunction.MaximizeNMB:
                    mean = _obsTotalNMB.Mean;
                    break;
            }
            return mean;
        }
        public double GetObjectiveFunction_Mean(EnumObjectiveFunction objectiveFunction, double wtpForHealth)
        {
            double mean = 0;
            switch (objectiveFunction)
            {
                case EnumObjectiveFunction.MaximizeNHB:
                    mean = _obsTotalDALY.Mean - _obsTotalCost.Mean / wtpForHealth;
                    break;
                case EnumObjectiveFunction.MaximizeNMB:
                    mean = wtpForHealth * _obsTotalDALY.Mean - _obsTotalCost.Mean;
                    break;
            }
            return mean;
        }
        // get objective function StDev
        public double GetObjectiveFunction_StDev(EnumObjectiveFunction objectiveFunction)
        {
            double mean = 0;
            switch (objectiveFunction)
            {
                case EnumObjectiveFunction.MaximizeNHB:
                    mean = _obsTotalNHB.StDev;
                    break;
                case EnumObjectiveFunction.MaximizeNMB:
                    mean = _obsTotalNMB.StDev;
                    break;
            }
            return mean;
        }
        public double GetObjectiveFunction_StDev(EnumObjectiveFunction objectiveFunction, double wtpForHealth)
        {
            double mean = 0;
            switch (objectiveFunction)
            {
                case EnumObjectiveFunction.MaximizeNHB:
                    mean = _obsTotalDALY.StDev + _obsTotalCost.StDev / wtpForHealth;
                    break;
                case EnumObjectiveFunction.MaximizeNMB:
                    mean = wtpForHealth * _obsTotalDALY.StDev + _obsTotalCost.StDev;
                    break;
            }
            return mean;
        }
        // get lower bound of objective function
        public double GetObjectiveFunction_LowerBound(EnumObjectiveFunction objectiveFunction, double significanceLevel)
        {
            double lowerBound = 0;
            switch (objectiveFunction)
            {
                case EnumObjectiveFunction.MaximizeNHB:
                    lowerBound = _obsTotalNHB.LBoundConfidenceInterval(significanceLevel);
                    break;
                case EnumObjectiveFunction.MaximizeNMB:
                    lowerBound = _obsTotalNMB.LBoundConfidenceInterval(significanceLevel);
                    break;
            }
            return lowerBound;
        }
        public double GetObjectiveFunction_LowerBound(EnumObjectiveFunction objectiveFunction, double wtpForHealth, double significanceLevel)
        {
            double lowerBound = 0;
            switch (objectiveFunction)
            {
                case EnumObjectiveFunction.MaximizeNHB:
                    lowerBound = _obsTotalDALY.Mean - _obsTotalCost.Mean / wtpForHealth
                        - (_obsTotalDALY.HalfWidth(significanceLevel) + _obsTotalCost.HalfWidth(significanceLevel) / wtpForHealth);
                    break;
                case EnumObjectiveFunction.MaximizeNMB:
                    lowerBound = wtpForHealth*_obsTotalDALY.Mean - _obsTotalCost.Mean
                        - (wtpForHealth * _obsTotalDALY.HalfWidth(significanceLevel) + _obsTotalCost.HalfWidth(significanceLevel));
                    break;
            }
            return lowerBound;
        }
        // get upper bound of objective function
        public double GetObjectiveFunction_UpperBound(EnumObjectiveFunction objectiveFunction, double significanceLevel)
        {
            double upperBound = 0;
            switch (objectiveFunction)
            {
                case EnumObjectiveFunction.MaximizeNHB:
                    upperBound = _obsTotalNHB.UBoundConfidenceInterval(significanceLevel);
                    break;
                case EnumObjectiveFunction.MaximizeNMB:
                    upperBound = _obsTotalNMB.UBoundConfidenceInterval(significanceLevel);
                    break;
            }
            return upperBound;
        }
        public double GetObjectiveFunction_UpperBound(EnumObjectiveFunction objectiveFunction, double wtpForHealth, double significanceLevel)
        {
            double upperBound = 0;
            switch (objectiveFunction)
            {
                case EnumObjectiveFunction.MaximizeNHB:
                    upperBound = _obsTotalDALY.Mean - _obsTotalCost.Mean / wtpForHealth
                        + (_obsTotalDALY.HalfWidth(significanceLevel) + _obsTotalCost.HalfWidth(significanceLevel) / wtpForHealth);
                    break;
                case EnumObjectiveFunction.MaximizeNMB:
                    upperBound = wtpForHealth * _obsTotalDALY.Mean - _obsTotalCost.Mean
                        + (wtpForHealth * _obsTotalDALY.HalfWidth(significanceLevel) + _obsTotalCost.HalfWidth(significanceLevel));
                    break;
            }
            return upperBound;
        }
        #endregion

         // ********  Private Subs ******** 
        #region Private Subs
        // initialize simulation
        private void InitializeSimulation()
        {    
                      
        }
               
        
        // toggle modeller to different operation
        public void ToggleModellerTo(EnumModelUse modelUse, EnumEpiDecisions decisionRule, bool reportEpidemicTrajectories)
        {
            _storeEpidemicTrajectories = reportEpidemicTrajectories;            
            // toggle each epidemic
            if (_set.UseParallelComputing)
                foreach (Epidemic thisEpidemic in _epidemics)
                    ToggleAnEpidemicTo(thisEpidemic, modelUse, decisionRule, reportEpidemicTrajectories);
            else
                ToggleAnEpidemicTo(_parentEpidemic, modelUse, decisionRule, reportEpidemicTrajectories);
        }
        // toggle one epidemic
        private void ToggleAnEpidemicTo(Epidemic thisEpidemic, EnumModelUse modelUse, EnumEpiDecisions decisionRule, bool storeEpidemicTrajectories)
        {   
            thisEpidemic.ModelUse = modelUse;
            //thisEpidemic.DecisionRule = decisionRule;
            thisEpidemic.StoreEpidemicTrajectories = storeEpidemicTrajectories;

            switch (modelUse)
            {
                case EnumModelUse.Simulation:
                    {                        
                        if (decisionRule == EnumEpiDecisions.PredeterminedSequence)
                            thisEpidemic.DecisionMaker.AddPrespecifiedDecisionsOverDecisionsPeriods(_set.PrespecifiedSequenceOfInterventions);
                    }
                    break;
                case EnumModelUse.Calibration:
                    #region Calibration
                    {
                        thisEpidemic.DecisionMaker.AddPrespecifiedDecisionsOverDecisionsPeriods(_set.PrespecifiedSequenceOfInterventions);
                    }
                    break;
                    #endregion
                case EnumModelUse.Optimization:
                    {
                        thisEpidemic.StoreEpidemicTrajectories = false;
                    }
                    break;
            }
        }

        #endregion
    }

    public class SimSummary
    {
        private ModelSettings _set;
        private int _nSim;   // number of simulated epidemics 
        public int[] SimItrs;
        public int[] RNDSeeds;
        public double[][] ParamValues;
        public int[][] InterventionsCombinations;
        public double[][] SimIncidenceOutputs;
        public double[][] SimPrevalenceOutputs;

        // simulation statistics collection
        public List<ObservationBasedStatistics> IncidenceStats { get; private set; } = new List<ObservationBasedStatistics>();
        public List<ObservationBasedStatistics> PrevalenceStats { get; private set; } = new List<ObservationBasedStatistics>();
        public List<ObservationBasedStatistics> RatioStats { get; private set; } = new List<ObservationBasedStatistics>();
        public List<ObservationBasedStatistics> ComputationStats { get; private set; } = new List<ObservationBasedStatistics>();

        public double[] Costs;
        public double[] AnnualCosts;
        public double[] DALYs;
        public double[] NMBs;
        public double[] NHBs;
        public ObservationBasedStatistics CostStat { get; private set; } = new ObservationBasedStatistics("Total cost");
        public ObservationBasedStatistics AnnualCostStat { get; private set; } = new ObservationBasedStatistics("Annual cost");
        public ObservationBasedStatistics DALYStat { get; private set; } = new ObservationBasedStatistics("Total DALY");
        public ObservationBasedStatistics NMBStat { get; private set; } = new ObservationBasedStatistics("Total NMB");
        public ObservationBasedStatistics NHBStat { get; private set; } = new ObservationBasedStatistics("Total NHB");
        public ObservationBasedStatistics TimeStat { get; private set; } = new ObservationBasedStatistics("Time used to simulate a trajectory");
        
        public SimSummary(ref ModelSettings settings, ref Epidemic parentEpidemic)
        {
            _set = settings;
            _nSim = settings.NumOfSimItrs;   // number of simulated epidemics
            IncidenceStats.Clear();
            PrevalenceStats.Clear();
            RatioStats.Clear();
            ComputationStats.Clear();

            foreach (Class thisClass in parentEpidemic.Classes.Where(c=>c.ShowStatisticsInSimulationResults))
            {
                IncidenceStats.Add(new ObservationBasedStatistics("Total New: " + thisClass.Name, _nSim));
                if (thisClass is Class_Normal)
                    PrevalenceStats.Add(new ObservationBasedStatistics("Average Size: " + thisClass.Name, _nSim));
            }
            foreach (SumTrajectory thisSumTraj in parentEpidemic.SumTrajectories)
            {
                // incidence stats
                if (thisSumTraj.Type == SumTrajectory.EnumType.Incidence || thisSumTraj.Type == SumTrajectory.EnumType.AccumulatingIncident)
                    IncidenceStats.Add(new ObservationBasedStatistics("Total: " + thisSumTraj.Name, _nSim));
                // prevalence stats
                else if (thisSumTraj.Type == SumTrajectory.EnumType.Prevalence)
                    PrevalenceStats.Add(new ObservationBasedStatistics("Averge size: " + thisSumTraj.Name, _nSim));
            }
            foreach (RatioTrajectory thisRatioTaj in parentEpidemic.RatioTrajectories)
                RatioStats.Add(new ObservationBasedStatistics("Average ratio: " + thisRatioTaj.Name, _nSim));

            // reset the jagged array containing trajectories
            InterventionsCombinations = new int[0][];
            SimIncidenceOutputs = new double[0][];
            SimPrevalenceOutputs = new double[0][];
        }

        public void Add(Epidemic simulatedEpi)
        {
            // store trajectories
            if (_set.IfShowSimulatedTrajectories)
            {
                InterventionsCombinations = SupportFunctions.ConcatJaggedArray(
                    InterventionsCombinations, simulatedEpi.TrajsForSimOutput.InterventionCombinations);
                SimPrevalenceOutputs = SupportFunctions.ConcatJaggedArray(
                    SimPrevalenceOutputs, simulatedEpi.TrajsForSimOutput.SimPrevalenceOutputs);
                SimIncidenceOutputs = SupportFunctions.ConcatJaggedArray(
                    SimIncidenceOutputs, simulatedEpi.TrajsForSimOutput.SimIncidenceOutputs);
            }

            // store sampled parameter values
            ParamValues[simulatedEpi.ID] = simulatedEpi.ParamManager.ParameterValues;

            // if the outcomes should be recorded
            if (!(simulatedEpi.ModelUse == EnumModelUse.Simulation))
                return;

            // summary statistics
            DALYStat.Record(simulatedEpi.EpidemicCostHealth.TotalDiscountedDALY);
            CostStat.Record(simulatedEpi.EpidemicCostHealth.TotalDisountedCost);
            AnnualCostStat.Record(simulatedEpi.EpidemicCostHealth.GetEquivalentAnnualCost(
                _set.AnnualDiscountRate,
                (int)(_set.WarmUpPeriodTimeIndex * _set.DeltaT),
                (int)(_set.TimeIndexToStop * _set.DeltaT)));
            NHBStat.Record(simulatedEpi.EpidemicCostHealth.GetDiscountedNHB(_set.WTPForHealth));
            NMBStat.Record(simulatedEpi.EpidemicCostHealth.GetDiscountedNMB(_set.WTPForHealth));
            TimeStat.Record(simulatedEpi.Timer.TimePassed);

            // incidence and prevalence statistics
            int incidentStatIndex = 0, prevalenceStatIndex = 0, ratioStatIndex = 0;
            foreach (Class thisClass in simulatedEpi.Classes.Where(c => c.ShowStatisticsInSimulationResults))
            {
                IncidenceStats[incidentStatIndex++].Record(thisClass.ClassStat.AccumulatedIncidenceAfterWarmUp, simulatedEpi.ID);
                if (thisClass is Class_Normal)
                    PrevalenceStats[prevalenceStatIndex++].Record(thisClass.ClassStat.AveragePrevalenceStat.Mean, simulatedEpi.ID);
            }
            foreach (SumTrajectory sumTraj in simulatedEpi.SumTrajectories)
            {
                if (sumTraj.Type == SumTrajectory.EnumType.Incidence || sumTraj.Type == SumTrajectory.EnumType.AccumulatingIncident)
                    IncidenceStats[incidentStatIndex++].Record(sumTraj.AccumulatedIncidenceAfterWarmUp, simulatedEpi.ID);
                if (sumTraj.Type == SumTrajectory.EnumType.Prevalence)
                    PrevalenceStats[prevalenceStatIndex++].Record(sumTraj.AveragePrevalenceStat.Mean, simulatedEpi.ID);
            }
            foreach (RatioTrajectory ratioTraj in simulatedEpi.RatioTrajectories)
            {
                switch (ratioTraj.Type)
                {
                    case RatioTrajectory.EnumType.AccumulatedIncidenceOverAccumulatedIncidence:
                        RatioStats[ratioStatIndex].Record(ratioTraj.TimeSeries.GetLastObs(), simulatedEpi.ID);
                        break;
                    case RatioTrajectory.EnumType.PrevalenceOverPrevalence:
                        RatioStats[ratioStatIndex].Record(ratioTraj.AveragePrevalenceStat.Mean, simulatedEpi.ID);
                        break;
                    case RatioTrajectory.EnumType.IncidenceOverIncidence:
                        RatioStats[ratioStatIndex].Record(ratioTraj.TimeSeries.ObsList.Average(), simulatedEpi.ID);
                        break;
                }
                ++ratioStatIndex;
            }

            // statistics on individual simulation
            SimItrs[simulatedEpi.ID] = simulatedEpi.ID;
            RNDSeeds[simulatedEpi.ID] = simulatedEpi.SeedProducedAcceptibleTraj;
            NHBs[simulatedEpi.ID] = simulatedEpi.EpidemicCostHealth.GetDiscountedNHB(_set.WTPForHealth);
            NMBs[simulatedEpi.ID] = simulatedEpi.EpidemicCostHealth.GetDiscountedNMB(_set.WTPForHealth);
            DALYs[simulatedEpi.ID] = simulatedEpi.EpidemicCostHealth.TotalDiscountedDALY;
            Costs[simulatedEpi.ID] = simulatedEpi.EpidemicCostHealth.TotalDisountedCost;
            AnnualCosts[simulatedEpi.ID] = simulatedEpi.EpidemicCostHealth.GetEquivalentAnnualCost(
                _set.AnnualDiscountRate,
                (int)(_set.WarmUpPeriodTimeIndex * _set.DeltaT),
                (int)(_set.TimeIndexToStop * _set.DeltaT));
        }

        public void Reset()
        {
            SimItrs = new int[_nSim];
            RNDSeeds = new int[_nSim];
            ParamValues = new double[_nSim][];
            InterventionsCombinations = new int[_nSim][];
            SimIncidenceOutputs = new double[_nSim][];
            SimPrevalenceOutputs = new double[_nSim][];

            // reset simulation statistics
            foreach (ObservationBasedStatistics thisObsStat in IncidenceStats)
                thisObsStat.Reset();
            foreach (ObservationBasedStatistics thisObsStat in PrevalenceStats)
                thisObsStat.Reset();
            foreach (ObservationBasedStatistics thisObsStat in RatioStats)
                thisObsStat.Reset();
            foreach (ObservationBasedStatistics thisObsStat in ComputationStats)
                thisObsStat.Reset();

            Costs = new double[_nSim];
            AnnualCosts = new double[_nSim];
            DALYs = new double[_nSim];
            NMBs = new double[_nSim];
            NHBs = new double[_nSim];
            
            CostStat.Reset();
            AnnualCostStat.Reset();
            DALYStat.Reset();
            NMBStat.Reset();
            NHBStat.Reset();
            TimeStat.Reset();
            TimeToSimulateAllEpidemics = 0;
        }
               
    }

    public class ModelInfo
    {
        private Epidemic _epi;
        public string[] NamesOfParams { get; private set; }
        public string[] NamesOfParamsInCalib { get; private set; }
        public int NumOfFeatures { get; private set; }

        public ModelInfo(ref Epidemic epidemic)
        {
            _epi = epidemic;
            FindNamesOfParams();
            FindNamesOfParamsInCalib();

        }

        // find the names of parameters
        private void FindNamesOfParams()
        {
            // find the names of the parameters
            int i = 0;
            NamesOfParams = new string[_epi.ParamManager.Parameters.Count];
            foreach (Parameter thisParameter in _epi.ParamManager.Parameters)
                NamesOfParams[i++] = thisParameter.Name;
        }
        // find the names of parameters to calibrate
        private void FindNamesOfParamsInCalib()
        {
            NamesOfParamsInCalib = new string[_epi.ParamManager.Parameters.Where(p => p.IncludedInCalibration).Count()];
            int i = 0;
            foreach (Parameter thisParameter in _epi.ParamManager.Parameters.Where(p => p.IncludedInCalibration))
                NamesOfParamsInCalib[i++] = thisParameter.Name;
        }
    }

    public class RNDSeedGenerator
    {
        ModelSettings _modelSet;
        Discrete _discreteDist;
        private int[] _sampledSeeds;

        public RNDSeedGenerator(ref ModelSettings modelSet, ref RNG rng)
        {
            _modelSet = modelSet;

            // reset the rnd object            
            if (_modelSet.SimRNDSeedsSource == EnumSimRNDSeedsSource.Weighted)
            {
                // read weights of rnd seeds
                int n = _modelSet.RndSeeds.Length;
                double[] arrProb = new double[n];
                for (int i = 0; i < n; i++)
                    arrProb[i] = _modelSet.RndSeedsGoodnessOfFit[i];

                // normalize the weights 
                double sum = arrProb.Sum();
                for (int i = 0; i < n; i++)
                    arrProb[i] = arrProb[i] / sum;

                // define the sampling object
                _discreteDist = new Discrete("Discrete distribution over RND seeds", arrProb);

                // re-sample seeds
                _sampledSeeds = new int[_modelSet.NumOfSimItrs];
                for (int i = 0; i < _modelSet.NumOfSimItrs; i++)
                    _sampledSeeds[i] =  _modelSet.RndSeeds[_discreteDist.SampleDiscrete(rng)];
            }
        }

        // find the RND seed for this iteration
        public int FindRNDSeed(int simItr)
        {
            int r = 0;
            switch (_modelSet.SimRNDSeedsSource)
            {
                case EnumSimRNDSeedsSource.StartFrom0:
                    r = _modelSet.DistanceBtwRNGSeeds * simItr + _modelSet.FirstRNGSeed;
                    break;
                case EnumSimRNDSeedsSource.Prespecified:
                    r = _modelSet.RndSeeds[simItr];
                    break;
                case EnumSimRNDSeedsSource.Weighted:
                    r = _sampledSeeds[simItr];
                    break;
            }
            return r;
        }
    }
}
