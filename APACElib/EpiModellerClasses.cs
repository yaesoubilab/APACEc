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
        private ModelSettings _modelSet;
        public ModelSettings ModelSettings { get => _modelSet; }
        private RNDSeedGenerator _seedGenerator;
        private Epidemic _parentEpidemic;
        private List<Epidemic> _epidemics = new List<Epidemic>();
        public Epidemic ParentEpidemic { get => _parentEpidemic; }        
        public ModelInfo ModelInfo { get; private set; }
        public SimSummary SimSummary { get; private set; }
        public Timer Timer { get; private set; }
        public Calibration Calibration { get; set; }
        // simulation setting
        RNG _rng = new RNG(0);

        // Instantiation
        public EpidemicModeller(int ID, ref ExcelInterface excelInterface, ref ModelSettings modelSettings)
        {
            this.ID = ID;
            _modelSet = modelSettings;

            // build a parent epidemic model 
            _parentEpidemic = new Epidemic(0);
            _parentEpidemic.BuildModel(ref _modelSet, true);

            // read contact matrices
            _modelSet.ReadContactMatrices(ref excelInterface, _parentEpidemic.FOIModel.NumOfIntrvnAffectingContacts);

            // extract model information 
            ModelInfo = new ModelInfo(ref _parentEpidemic);

            // find how many epi models to create
            int numOfEpidemics = 0;
            switch (_modelSet.ModelUse)
            {
                case EnumModelUse.Simulation:
                    numOfEpidemics = _modelSet.NumOfSimItrs;
                    break;
                case EnumModelUse.Calibration:
                    {
                        if (_modelSet.UseParallelComputing)
                            numOfEpidemics = excelInterface.GetNumOfTrajsToSimForCalibr(); //_modelSet.NumOfTrajsInParallelForCalibr;
                        else
                            numOfEpidemics = excelInterface.GetNumOfTrajsToSimForCalibr(); // excelInterface.GetNumOfTrajsToSimForCalibr();
                    }
                    break;
            }
            // create the epi models
            _epidemics.Clear();
            for (int id = 0; id < numOfEpidemics; id++)                
                _epidemics.Add(new Epidemic(id));            
        }

        // simulate several epidemics
        public void SimulateEpidemics()
        {
            // simulatoin summary
            SimSummary = new SimSummary(ref _modelSet, ref _parentEpidemic);
            SimSummary.Reset();

            // simulation time
            Timer = new Timer();
            Timer.Start();

            // create a rnd seed generator 
            _seedGenerator = new RNDSeedGenerator(_modelSet, _rng);

            // use parallel computing? 
            if (!_modelSet.UseParallelComputing)
            {
                int seed = 0;
                foreach (Epidemic epi in _epidemics)
                {
                    // build the parent epidemic model
                    epi.BuildModel(ref _modelSet);
                    // find the RND seed for this iteration
                    seed = _seedGenerator.FindRNDSeed(epi.ID);
                    // simulate
                    epi.SimulateUntilOneAcceptibleTrajFound(_modelSet.TimeIndexToStop, seed);
                    // store epidemic trajectories and outcomes
                    SimSummary.Add(epi, epi.ID);
                }
            }
            else // (_modelSettings.UseParallelComputing == true)
            {
                int seed = 0;
                Parallel.ForEach(_epidemics, epi =>
                {
                    // build the parent epidemic model
                    epi.BuildModel(ref _modelSet);
                    // find the RND seed for this iteration
                    seed = _seedGenerator.FindRNDSeed(epi.ID);
                    // simulate
                    epi.SimulateUntilOneAcceptibleTrajFound(_modelSet.TimeIndexToStop, seed);
                });

                // store epidemic trajectories and outcomes
                foreach (Epidemic thisEpidemic in _epidemics)
                    SimSummary.Add(thisEpidemic, thisEpidemic.ID);
            }
            // simulation run time
            Timer.Stop();
            SimSummary.TimeToSimulateAllEpidemics = Timer.TimePassed;
        }

        // calibrate
        public void Calibrate()
        {
            // computation time
            Timer = new Timer();
            Timer.Start();

            // set up calibration 
            Calibration = new Calibration(_modelSet.ObservedHistory);
            Calibration.AddCalibTargets(_parentEpidemic.EpiHist.SumTrajs, _parentEpidemic.EpiHist.RatioTrajs);

            // use parallel computing? 
            if (_modelSet.UseParallelComputing == false)
                foreach(Epidemic epi in _epidemics)
                {
                    // build the epidemic model
                    epi.BuildModel(ref _modelSet);
                    // toggle to calibration
                    ToggleAnEpidemicTo(epi, EnumModelUse.Calibration, EnumEpiDecisions.PredeterminedSequence, false);
                    // simulate            
                    epi.SimulateUntilOneAcceptibleTrajFound(_modelSet.TimeIndexToStop);
                    // find the likeligood of this simulation 
                    if (epi.SeedProducedAcceptibleTraj != -1)
                        Calibration.CalculateLnL(epi);
                    // clean this simulation
                    epi.CleanMemory();
                    // store calibration summary
                    Calibration.AddCalibSummary(epi);                    
                }
            else // using parallel processing
            {
                // simulate and calculate likelihood
                Parallel.ForEach(_epidemics, epi =>
                {
                    // build the epidemic model
                    epi.BuildModel(ref _modelSet);
                    // toggle to calibration
                    ToggleAnEpidemicTo(epi, EnumModelUse.Calibration, EnumEpiDecisions.PredeterminedSequence, false);
                    // simulate            
                    epi.SimulateUntilOneAcceptibleTrajFound(_modelSet.TimeIndexToStop);
                    // find the likeligood of this simulation 
                    if (epi.SeedProducedAcceptibleTraj != -1)
                        Calibration.CalculateLnL(epi);
                    // clean this simulation
                    epi.CleanMemory();
                });
                // rstore calibration results
                foreach (Epidemic epi in _epidemics)
                    Calibration.AddCalibSummary(epi);
            }

            // finalize calibration
            Calibration.CalcProbsAndSort();

            // computation time
            Timer.Stop();
        }

        // calibrate
        public void CalibrateOld(int numOfTrajsToSim)
        {
            // computation time
            Timer = new Timer();
            Timer.Start();

            // set up calibration 
            Calibration = new Calibration(_modelSet.ObservedHistory);
            Calibration.AddCalibTargets(_parentEpidemic.EpiHist.SumTrajs, _parentEpidemic.EpiHist.RatioTrajs);

            // build the epidemic models 
            if (_modelSet.UseParallelComputing)
                Parallel.ForEach(_epidemics, thisEpidemic =>
                {                    
                    thisEpidemic.BuildModel(ref _modelSet);
                });
            else
                foreach (Epidemic epi in _epidemics)
                    epi.BuildModel(ref _modelSet);

            // toggle to calibration
            ToggleModellerTo(EnumModelUse.Calibration, EnumEpiDecisions.PredeterminedSequence, false);

            // keep obtaining trajectories until enough
            int simItr = -1;
            int simItrParallel = -1;
            int parallelLoopIndex = 0;
            while (simItr < numOfTrajsToSim - 1)
            {
                // use parallel computing? 
                if (_modelSet.UseParallelComputing == false)
                {
                    #region Use sequential computing
                    // increment the simulation iteration
                    ++simItr;
                    // find the RND seed for this iteration
                    int seed = _seedGenerator.FindRNDSeed(simItr);
                    // simulate one epidemic trajectory
                    _epidemics[0].SimulateUntilOneAcceptibleTrajFound
                        (seed, seed + _modelSet.DistanceBtwRNGSeeds, _modelSet.TimeIndexToStop);
                    //_parentEpidemic.SimulateOneTrajectory(rndSeedToGetAnAcceptibleEpidemic, simItr, calibrationTimeHorizonIndex);

                    // find the likeligood of this simulation 
                    if (_epidemics[0].SeedProducedAcceptibleTraj != -1)
                    {
                        Calibration.CalculateLnL(_epidemics[0]);
                        // store calibration summary
                        Calibration.AddCalibSummary(_epidemics[0]);
                    }
                    #endregion
                }
                else // (_modelSettings.UseParallelComputing == true)
                {
                    #region Use parallel computing                    
                    // simulate and calculate likelihood
                    Parallel.ForEach(_epidemics, epi =>
                    {
                        // build the epidemic model
                        epi.BuildModel(ref _modelSet);
                        // simulate            
                        epi.SimulateUntilOneAcceptibleTrajFound(_modelSet.TimeIndexToStop);
                        //thisEpidemic.SimulateOneTrajectory(rndSeedToGetAnAcceptibleEpidemic, simItr, _modelSet.TimeIndexToStop);

                        // find the likeligood of this simulation 
                        if (epi.SeedProducedAcceptibleTraj != -1)
                            Calibration.CalculateLnL(epi);
                    });

                    // run the calibration
                    foreach (Epidemic epi in _epidemics)
                    {
                        ++ simItrParallel;
                        // sim itr
                        simItr = _modelSet.NumOfTrajsInParallelForCalibr * parallelLoopIndex + epi.ID;
                        // store calibration summary
                        Calibration.AddCalibSummary(epi);                        
                    }

                    // increment the loop id
                    ++parallelLoopIndex;
                    simItr = simItrParallel;
                    #endregion
                }                
            }

            // finalize calibration
            Calibration.CalcProbsAndSort();

            // computation time
            Timer.Stop();
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
        public void StoreEpiTrajsForExcelOutput(bool yesOrNo)
        {
            foreach (Epidemic thisEpidemic in _epidemics)
                thisEpidemic.StoreEpiTrajsForExcelOutput = yesOrNo;
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
            if (_modelSet.UseParallelComputing)
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

        // get name of special statistics included in calibratoin 
        public string[] GetNamesOfCalibrTargets()
        {
            // find the names of the parameters
            List<string> names = new List<string>(); 
            // summation statistics
            foreach (SumTrajectory thisSumTraj in _parentEpidemic.EpiHist.SumTrajs.Where(s => !(s.CalibInfo is null)))
                names.Add(thisSumTraj.Name);
            // ratio statistics
            foreach (RatioTrajectory thisRatioTraj in _parentEpidemic.EpiHist.RatioTrajs.Where(s => !(s.CalibInfo is null)))
                names.Add(thisRatioTraj.Name);

            return names.ToArray();
        }

        // toggle modeller to different operation
        public void ToggleModellerTo(EnumModelUse modelUse, EnumEpiDecisions decisionRule, bool reportEpiTrajsToExcel)
        {
            foreach (Epidemic thisEpidemic in _epidemics)
                ToggleAnEpidemicTo(thisEpidemic, modelUse, decisionRule, reportEpiTrajsToExcel);

            //// toggle each epidemic
            //if (_modelSet.UseParallelComputing)                
            //else
            //    ToggleAnEpidemicTo(_parentEpidemic, modelUse, decisionRule, reportEpiTrajsToExcel);
        }
        // toggle one epidemic
        private void ToggleAnEpidemicTo(Epidemic thisEpidemic, EnumModelUse modelUse, EnumEpiDecisions decisionRule, bool reportEpiTrajsToExcel)
        {   
            thisEpidemic.ModelUse = modelUse;
            thisEpidemic.StoreEpiTrajsForExcelOutput = reportEpiTrajsToExcel;

            switch (modelUse)
            {
                case EnumModelUse.Simulation:
                    {                        
                        if (decisionRule == EnumEpiDecisions.PredeterminedSequence)
                            thisEpidemic.DecisionMaker.AddPrespecifiedDecisionsOverDecisionsPeriods(_modelSet.PrespecifiedSequenceOfInterventions);
                    }
                    break;
                case EnumModelUse.Calibration:
                    {
                        thisEpidemic.DecisionMaker.AddPrespecifiedDecisionsOverDecisionsPeriods(_modelSet.PrespecifiedSequenceOfInterventions);
                    }
                    break;
                case EnumModelUse.Optimization:
                    {
                        thisEpidemic.StoreEpiTrajsForExcelOutput = false;
                    }
                    break;
            }
        }
    }

    public class SimSummaryTrajs
    {
        public int[][] TrajsSimRepIndex { get; private set; }
        public double[][] TrajsSimIncidence { get; private set; }
        public double[][] TrajsSimPrevalence { get; private set; }
        public int[][] TrajsSimIntrvCombinations { get; private set; }

        public int NumOfSimIncidenceInTraj { get { return TrajsSimIncidence[0].Length; } }
        public int NumOfSimPrevalenceInTraj { get { return TrajsSimPrevalence[0].Length; } }

        public int[][] TrajsObsRepIndex { get; private set; }
        public double[][] TrajsObsIncidence { get; private set; }
        public double[][] TrajsObsPrevalence { get; private set; }
        public int[][] TrajsObsIntrvCombinations { get; private set; }

        public int NumOfObsIncidenceInTraj { get { return TrajsObsIncidence[0].Length; } }
        public int NumOfObsPrevalenceInTraj { get { return TrajsObsPrevalence[0].Length; } }

        public SimSummaryTrajs()
        {
            // reset the jagged array containing trajectories
            TrajsSimRepIndex = new int[0][];
            TrajsSimIncidence = new double[0][];
            TrajsSimPrevalence = new double[0][];
            TrajsSimIntrvCombinations = new int[0][];

            TrajsObsRepIndex = new int[0][];
            TrajsObsIncidence = new double[0][];
            TrajsObsPrevalence = new double[0][];
            TrajsObsIntrvCombinations = new int[0][];
        }

        public void Add(Epidemic simulatedEpi)
        {
            // store trajectories
            TrajsSimRepIndex = SupportFunctions.ConcatJaggedArray(
                TrajsSimRepIndex, simulatedEpi.EpiHist.SimOutputTrajs.SimRepIndeces);
            TrajsSimIncidence = SupportFunctions.ConcatJaggedArray(
                TrajsSimIncidence, simulatedEpi.EpiHist.SimOutputTrajs.SimIncidenceOutputs);
            TrajsSimPrevalence = SupportFunctions.ConcatJaggedArray(
                TrajsSimPrevalence, simulatedEpi.EpiHist.SimOutputTrajs.SimPrevalenceOutputs);
            TrajsSimIntrvCombinations = SupportFunctions.ConcatJaggedArray(
                TrajsSimIntrvCombinations, simulatedEpi.EpiHist.SimOutputTrajs.InterventionCombinations);

            TrajsObsRepIndex = SupportFunctions.ConcatJaggedArray(
                TrajsObsRepIndex, simulatedEpi.EpiHist.SurveyedOutputTrajs.SimRepIndeces);
            TrajsObsIncidence = SupportFunctions.ConcatJaggedArray(
                TrajsObsIncidence, simulatedEpi.EpiHist.SurveyedOutputTrajs.SimIncidenceOutputs);
            TrajsObsPrevalence = SupportFunctions.ConcatJaggedArray(
                TrajsObsPrevalence, simulatedEpi.EpiHist.SurveyedOutputTrajs.SimPrevalenceOutputs);
            TrajsObsIntrvCombinations = SupportFunctions.ConcatJaggedArray(
                TrajsObsIntrvCombinations, simulatedEpi.EpiHist.SurveyedOutputTrajs.InterventionCombinations);
        }

        public void Reset()
        {
            TrajsSimRepIndex = new int[0][];
            TrajsSimIncidence = new double[0][];
            TrajsSimPrevalence = new double[0][];
            TrajsSimIntrvCombinations = new int[0][];

            TrajsObsRepIndex = new int[0][];
            TrajsObsIncidence = new double[0][];
            TrajsObsPrevalence = new double[0][];
            TrajsObsIntrvCombinations = new int[0][];
        }
    }

    public class SimSummary
    {
        private ModelSettings _set;
        private int _nSim;   // number of simulated epidemics 
        public int[] SimItrs { get; private set; }
        public int[] RNDSeeds { get; private set; }
        public double[][] ParamValues { get; private set; }
        
        public SimSummaryTrajs SimSummaryTrajs { get; private set; }

        // simulation statistics collection
        public List<ObsBasedStat> IncidenceStats { get; private set; } = new List<ObsBasedStat>();
        public List<ObsBasedStat> PrevalenceStats { get; private set; } = new List<ObsBasedStat>();
        public List<ObsBasedStat> RatioStats { get; private set; } = new List<ObsBasedStat>();
        public List<ObsBasedStat> ComputationStats { get; private set; } = new List<ObsBasedStat>();

        public double[] Costs;
        public double[] AnnualCosts;
        public double[] DALYs;
        public double[] NMBs;
        public double[] NHBs;
        public ObsBasedStat CostStat { get; private set; } = new ObsBasedStat("Total cost");
        public ObsBasedStat AnnualCostStat { get; private set; } = new ObsBasedStat("Annual cost");
        public ObsBasedStat DALYStat { get; private set; } = new ObsBasedStat("Total DALY");
        public ObsBasedStat NMBStat { get; private set; } = new ObsBasedStat("Total NMB");
        public ObsBasedStat NHBStat { get; private set; } = new ObsBasedStat("Total NHB");
        public ObsBasedStat NumSwitchesStat { get; private set; } = new ObsBasedStat("Number of decision switched");
        public ObsBasedStat TimeStat { get; private set; } = new ObsBasedStat("Time used to simulate a trajectory");
        public double TimeToSimulateAllEpidemics { get; set; } = 0;

        public SimSummary(ref ModelSettings settings, ref Epidemic parentEpidemic)
        {
            _set = settings;
            _nSim = settings.NumOfSimItrs;   // number of simulated epidemics

            // summary statistics on classes
            foreach (Class thisClass in parentEpidemic.Classes)
            {
                if (thisClass.ClassStat.CollectAccumIncidenceStats)
                    IncidenceStats.Add(new ObsBasedStat("Total New: " + thisClass.Name, _nSim));
                if (thisClass.ClassStat.CollectPrevalenceStats)
                    PrevalenceStats.Add(new ObsBasedStat("Average Size: " + thisClass.Name, _nSim));
            }
            // summary statistics on summation
            foreach (SumTrajectory thisSumTraj in parentEpidemic.EpiHist.SumTrajs)
            {
                // incidence stats
                if (thisSumTraj.Type == SumTrajectory.EnumType.Incidence
                    || thisSumTraj.Type == SumTrajectory.EnumType.AccumulatingIncident)
                    IncidenceStats.Add(new ObsBasedStat("Total: " + thisSumTraj.Name, _nSim));
                // prevalence stats
                else if (thisSumTraj.Type == SumTrajectory.EnumType.Prevalence)
                    PrevalenceStats.Add(new ObsBasedStat("Averge size: " + thisSumTraj.Name, _nSim));
            }
            // summary statistics on ratio 
            foreach (RatioTrajectory thisRatioTaj in parentEpidemic.EpiHist.RatioTrajs)
                RatioStats.Add(new ObsBasedStat("Average ratio: " + thisRatioTaj.Name, _nSim));

            // trajectories
            SimSummaryTrajs = new SimSummaryTrajs();
            
        }

        public void Add(Epidemic simulatedEpi, int simItr)
        {
            // store trajectories
            if (_set.IfShowSimulatedTrajs)
                SimSummaryTrajs.Add(simulatedEpi);

            // store sampled parameter values
            ParamValues[simItr] = simulatedEpi.ParamManager.ParameterValues;

            // if the outcomes should be recorded
            if (!(simulatedEpi.ModelUse == EnumModelUse.Simulation))
                return;

            // summary statistics
            DALYStat.Record(simulatedEpi.EpidemicCostHealth.TotalDiscountedDALY);
            CostStat.Record(simulatedEpi.EpidemicCostHealth.TotalDisountedCost);
            AnnualCostStat.Record(simulatedEpi.EpidemicCostHealth.GetEquivalentAnnualCost(
                _set.AnnualDiscountRate,
                (int)(_set.WarmUpPeriodSimTIndex * _set.DeltaT),
                (int)(_set.TimeIndexToStop * _set.DeltaT)));
            NHBStat.Record(simulatedEpi.EpidemicCostHealth.GetDiscountedNHB(_set.WTPForHealth));
            NMBStat.Record(simulatedEpi.EpidemicCostHealth.GetDiscountedNMB(_set.WTPForHealth));
            TimeStat.Record(simulatedEpi.Timer.TimePassed);

            // incidence and prevalence statistics
            int incidentStatIndex = 0, prevalenceStatIndex = 0, ratioStatIndex = 0;
            foreach (Class thisClass in simulatedEpi.Classes)
            {
                if (thisClass.ClassStat.CollectAccumIncidenceStats)
                    IncidenceStats[incidentStatIndex++].Record(thisClass.ClassStat.AccumulatedIncidenceAfterWarmUp, simItr);
                if (thisClass.ClassStat.CollectPrevalenceStats)
                    PrevalenceStats[prevalenceStatIndex++].Record(thisClass.ClassStat.AveragePrevalenceStat.Mean, simItr);
            }
            foreach (SumTrajectory sumTraj in simulatedEpi.EpiHist.SumTrajs)
            {
                if (sumTraj.Type == SumTrajectory.EnumType.Incidence || sumTraj.Type == SumTrajectory.EnumType.AccumulatingIncident)
                    IncidenceStats[incidentStatIndex++].Record(sumTraj.AccumulatedIncidenceAfterWarmUp, simItr);
                if (sumTraj.Type == SumTrajectory.EnumType.Prevalence)
                    PrevalenceStats[prevalenceStatIndex++].Record(sumTraj.AveragePrevalenceStat.Mean, simItr);
            }
            foreach (RatioTrajectory ratioTraj in simulatedEpi.EpiHist.RatioTrajs)
            {
                switch (ratioTraj.Type)
                {
                    case RatioTrajectory.EnumType.AccumulatedIncidenceOverAccumulatedIncidence:
                        RatioStats[ratioStatIndex].Record(ratioTraj.PrevTimeSeries.GetLastRecording().GetValueOrDefault(-1), simItr);
                        break;
                    case RatioTrajectory.EnumType.PrevalenceOverPrevalence:
                        RatioStats[ratioStatIndex].Record(ratioTraj.AveragePrevalenceStat.Mean, simItr);
                        break;
                    case RatioTrajectory.EnumType.IncidenceOverIncidence:
                        RatioStats[ratioStatIndex].Record((double)ratioTraj.IncdTimeSeries.Recordings.Average(), simItr);
                        break;
                }
                ++ratioStatIndex;
            }

            // statistics on individual simulation
            SimItrs[simItr] = simItr;
            RNDSeeds[simItr] = simulatedEpi.SeedProducedAcceptibleTraj;
            NHBs[simItr] = simulatedEpi.EpidemicCostHealth.GetDiscountedNHB(_set.WTPForHealth);
            NMBs[simItr] = simulatedEpi.EpidemicCostHealth.GetDiscountedNMB(_set.WTPForHealth);
            DALYs[simItr] = simulatedEpi.EpidemicCostHealth.TotalDiscountedDALY;
            Costs[simItr] = simulatedEpi.EpidemicCostHealth.TotalDisountedCost;
            AnnualCosts[simItr] = simulatedEpi.EpidemicCostHealth.GetEquivalentAnnualCost(
                _set.AnnualDiscountRate,
                (int)(_set.WarmUpPeriodSimTIndex * _set.DeltaT),
                (int)(_set.TimeIndexToStop * _set.DeltaT));
        }

        // get simulation iteration outcomes
        public void GetIndvEpidemicOutcomes(ref string[] strItrOutcomeLabels, ref double[][] itrOutcomes)
        {
            // header
            strItrOutcomeLabels = new string[4];
            strItrOutcomeLabels[0] = "RNG Seed";
            strItrOutcomeLabels[1] = "DALY";
            strItrOutcomeLabels[2] = "Total Cost";
            strItrOutcomeLabels[3] = "Annual Cost";

            foreach (ObsBasedStat thisObs in IncidenceStats)
                SupportFunctions.AddToEndOfArray(ref strItrOutcomeLabels, thisObs.Name);

            foreach (ObsBasedStat thisObs in PrevalenceStats)
                SupportFunctions.AddToEndOfArray(ref strItrOutcomeLabels, thisObs.Name);

            foreach (ObsBasedStat thisObs in RatioStats)
                SupportFunctions.AddToEndOfArray(ref strItrOutcomeLabels, thisObs.Name);

            // individual observations 
            itrOutcomes = new double[_nSim][];
            for (int i = 0; i < _nSim; i++)
            {
                itrOutcomes[i] = new double[strItrOutcomeLabels.Length];
                itrOutcomes[i][0] = RNDSeeds[i];
                itrOutcomes[i][1] = DALYs[i];
                itrOutcomes[i][2] = Costs[i];
                itrOutcomes[i][3] = AnnualCosts[i];
            }
            int colIndex = 0;
            foreach (ObsBasedStat thisObs in IncidenceStats)
            {
                for (int i = 0; i < _nSim; i++)
                    itrOutcomes[i][4 + colIndex] = thisObs.Observations[i];
                ++colIndex;
            }
            foreach (ObsBasedStat thisObs in PrevalenceStats)
            {
                for (int i = 0; i < _nSim; i++)
                    itrOutcomes[i][4 + colIndex] = thisObs.Observations[i];
                ++colIndex;
            }
            foreach (ObsBasedStat thisObs in RatioStats)
            {
                for (int i = 0; i < _nSim; i++)
                    itrOutcomes[i][4 + colIndex] = thisObs.Observations[i];
                ++colIndex;
            }
        }

        // get summary outcomes
        public void GetSummaryOutcomes(
            ref string[] strSummaryStatistics, 
            ref string[] strClassAndSumStatistics,
            ref string[] strRatioStatistics, 
            ref string[] strComputationStatistics, 
            ref string[] strIterationOutcomes,
            ref double[][] arrSummaryStatistics, 
            ref double[][] arrClassAndSumStatistics,
            ref double[][] arrRatioStatistics, 
            ref double[,] arrComputationStatistics, 
            ref double[][] arrIterationOutcomes)
        {
            strSummaryStatistics = new string[6];
            strClassAndSumStatistics = new string[0];
            strRatioStatistics = new string[0];
            strComputationStatistics = new string[2];

            arrSummaryStatistics = new double[5][];
            arrClassAndSumStatistics = new double[0][];
            arrRatioStatistics = new double[0][];
            arrComputationStatistics = new double[2, 3];

            #region summary statistics
            strSummaryStatistics[(int)ExcelInterface.EnumSimStatsRows.TotalDALY - 1] = "Total discounted DALY";
            strSummaryStatistics[(int)ExcelInterface.EnumSimStatsRows.TotalCost - 1] = "Total discounted cost";
            strSummaryStatistics[(int)ExcelInterface.EnumSimStatsRows.AnnualCost - 1] = "Total annual cost";
            strSummaryStatistics[(int)ExcelInterface.EnumSimStatsRows.NHB - 1] = "Total discounted net health benefit";
            strSummaryStatistics[(int)ExcelInterface.EnumSimStatsRows.NMB - 1] = "Total discounted net monetary benefit";
            strSummaryStatistics[(int)ExcelInterface.EnumSimStatsRows.NumOfSwitches - 1] = "Number of switches between decisions";
            // Total DALY
            arrSummaryStatistics[(int)ExcelInterface.EnumSimStatsRows.TotalDALY - 1] = DALYStat.GetMeanStDevStErr();
            // Total cost
            arrSummaryStatistics[(int)ExcelInterface.EnumSimStatsRows.TotalCost - 1] = CostStat.GetMeanStDevStErr();
            arrSummaryStatistics[(int)ExcelInterface.EnumSimStatsRows.AnnualCost - 1] = AnnualCostStat.GetMeanStDevStErr();
            // NHB and NMB
            arrSummaryStatistics[(int)ExcelInterface.EnumSimStatsRows.NHB - 1] = NHBStat.GetMeanStDevStErr();
            arrSummaryStatistics[(int)ExcelInterface.EnumSimStatsRows.NMB - 1] = NMBStat.GetMeanStDevStErr();
            // Number of switches
            //arrSummaryStatistics[(int)ExcelInterface.EnumSimStatsRows.NumOfSwitches - 1] = NumSwitchesStat.GetMeanStDevStErr();
            #endregion

            foreach (ObsBasedStat thisObs in IncidenceStats)
            {
                // name of this statistics
                SupportFunctions.AddToEndOfArray(ref strClassAndSumStatistics, thisObs.Name);
                double[][] thisStatValues = new double[1][];
                thisStatValues[0] = thisObs.GetMeanStDevStErr();
                // concatinate 
                arrClassAndSumStatistics = SupportFunctions.ConcatJaggedArray(arrClassAndSumStatistics, thisStatValues);
            }
            foreach (ObsBasedStat thisObs in PrevalenceStats)
            {
                // name of this statistics
                SupportFunctions.AddToEndOfArray(ref strClassAndSumStatistics, thisObs.Name);
                double[][] thisStatValues = new double[1][];
                thisStatValues[0] = thisObs.GetMeanStDevStErr();
                // concatinate 
                arrClassAndSumStatistics = SupportFunctions.ConcatJaggedArray(arrClassAndSumStatistics, thisStatValues);
            }
            foreach (ObsBasedStat thisObs in RatioStats)
            {
                // name of this statistics
                SupportFunctions.AddToEndOfArray(ref strRatioStatistics, thisObs.Name);
                double[][] thisStatValues = new double[1][];
                thisStatValues[0] = thisObs.GetMeanStDevStErr();
                // concatinate 
                arrRatioStatistics = SupportFunctions.ConcatJaggedArray(arrRatioStatistics, thisStatValues);
            }

            GetIndvEpidemicOutcomes(ref strIterationOutcomes, ref arrIterationOutcomes);

            strComputationStatistics[0] = "Total simulation time (seconds)";
            strComputationStatistics[1] = "Simulation time of one trajectory (seconds)";
            arrComputationStatistics[0, 0] = TimeToSimulateAllEpidemics;
            arrComputationStatistics[1, 0] = TimeStat.Mean;
            arrComputationStatistics[1, 1] = TimeStat.StDev;
            arrComputationStatistics[1, 2] = TimeStat.StErr;
        }

        public void Reset()
        {
            SimItrs = new int[_nSim];
            RNDSeeds = new int[_nSim];
            ParamValues = new double[_nSim][];

            SimSummaryTrajs.Reset();

            // reset simulation statistics
            foreach (ObsBasedStat thisObsStat in IncidenceStats)
                thisObsStat.Reset();
            foreach (ObsBasedStat thisObsStat in PrevalenceStats)
                thisObsStat.Reset();
            foreach (ObsBasedStat thisObsStat in RatioStats)
                thisObsStat.Reset();
            foreach (ObsBasedStat thisObsStat in ComputationStats)
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

        public RNDSeedGenerator(ModelSettings modelSet, RNG rng)
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
                    r = 0;
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
