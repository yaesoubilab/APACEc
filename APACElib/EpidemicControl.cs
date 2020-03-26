using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomVariateLib;
using ComputationLib;

namespace APACElib
{
    public class DecisionMaker
    {
        public List<Intervention> Interventions { get; set; } = new List<Intervention>();
        public List<Condition> Conditions { get; set; }
        public int NumOfInterventions { get; set; } = 0;

        private readonly int _nOfDeltaTsInADecisionInterval; // number of time steps in a decision interval
        private int _nextEpiTimeIndexToMakeDecision; // next epidemic time index to make a decision       
        public int DecisionIntervalIndex { get; set; } // index of the current decision interval
        public int EpiTimeIndexToStartDecisionMaking { get; set; }
        public int EpiTimeIndexToChangeIntervetionsInEffect { get; set; } = int.MaxValue; // epidemic time index to change the interventions that are in effect
        private int[][] _presetDecisionsOverDecisionsPeriods; // prespecified decisions

        public int[] CurrentDecision { get; set; } = new int[0];   // array of 0 and 1 to represent which action is on or off
        public int[] DefaultDecision { get; set; } = new int[0];   // if all other actions become unavailable, we will use this action combination 
        public double CostOverThisDecisionPeriod { get; set; } = 0; // cost actions and decision making
        
        // Instantiation
        public DecisionMaker(int epiTimeIndexToStartDecisionMaking, int nOfDeltaTsInADecisionInterval, List<Condition> conditions)
        {
            EpiTimeIndexToStartDecisionMaking = epiTimeIndexToStartDecisionMaking;
            _nextEpiTimeIndexToMakeDecision = 0;
            _nOfDeltaTsInADecisionInterval = nOfDeltaTsInADecisionInterval;
            DecisionIntervalIndex = 0;
            Conditions = conditions;
        }

        // add a decision
        public void AddAnIntervention(Intervention action)
        {
            // add the actions
            Interventions.Add(action);
            ++NumOfInterventions;
        }

        // update parameters
        public void UpdateParameters(ParameterManager parManager, double deltaT)
        {
            foreach (Intervention intv in Interventions)
                intv.NumOfTimeIndeciesDelayedToGoIntoEffect = 
                    (int)(parManager.ParameterValues[intv.ParIDDelayToGoIntoEffectOnceTurnedOn] / deltaT);
        }

        // add prespecified decisions
        public void AddPrespecifiedDecisionsOverDecisionsPeriods(int[][] prespecifiedDecisionsOverDecisionsPeriods)
        {
            _presetDecisionsOverDecisionsPeriods = prespecifiedDecisionsOverDecisionsPeriods;
        }

        // update conditions and make a decision
        private int[] UpdateConditionsAndMakeADecision(int epiTimeIndex, RNG rng)
        {
            // update conditions
            foreach (Condition c in Conditions)
                c.Update(epiTimeIndex, rng);

            // check if decisions are not prespecified
            int[] newDecision = new int[NumOfInterventions];
            if (_presetDecisionsOverDecisionsPeriods == null)
            {
                // find the switch status of each action
                foreach (Intervention intv in Interventions)
                {
                    intv.OnOffStatus = intv.FindSwitchStatus(CurrentDecision[intv.Index], epiTimeIndex);
                    newDecision[intv.Index] = intv.OnOffStatus;
                }
            }
            else // if decisions are prespecified
                newDecision = _presetDecisionsOverDecisionsPeriods[DecisionIntervalIndex];

            return newDecision;
        }

        // make the first decision (at time zero)
        public void MakeTheFirstDecision(int epiTimeIndex, RNG rng)
        {           
            CurrentDecision = new int[NumOfInterventions];
            
            // make a decision
            int[] newDecision = UpdateConditionsAndMakeADecision(epiTimeIndex, rng);

            // update the current intervention combination to the new one
            UpdateCurrentDecision(newDecision, epiTimeIndex, ifThereIsAChange: true);

            // update the next time decisions should be made
            _nextEpiTimeIndexToMakeDecision = Math.Max(epiTimeIndex + _nOfDeltaTsInADecisionInterval, EpiTimeIndexToStartDecisionMaking);
        }

        // find a new intervention combination
        public void MakeANewDecision(int epiTimeIndex, RNG rng)
        {
            // if next decision point is reached
            if (epiTimeIndex != _nextEpiTimeIndexToMakeDecision)
                return; // no change in decision 

            // make a decision 
            int[] newDecision = UpdateConditionsAndMakeADecision(epiTimeIndex, rng);

            // check if this new intervention combination is the same as the current one
            bool ifThereIsAChange = false;
            if (CurrentDecision.SequenceEqual(newDecision))
                ifThereIsAChange = false;
            else
                ifThereIsAChange = true;              

            // update the current intervention combination to the new one
            UpdateCurrentDecision(newDecision, epiTimeIndex, ifThereIsAChange);

            // update the index of the decision period
            DecisionIntervalIndex += 1;

            // update the next time decisions should be made
            _nextEpiTimeIndexToMakeDecision = epiTimeIndex + _nOfDeltaTsInADecisionInterval;
        }

        // update the currect decision    
        private void UpdateCurrentDecision(int[] newDecision, int epiTimeIndex, bool ifThereIsAChange)
        {
            CostOverThisDecisionPeriod = 0; // reset cost over the next decision period

            // if there is a change in decisoin
            if (ifThereIsAChange || epiTimeIndex == 0)
            {
                int i = 0;
                foreach (Intervention a in Interventions)
                {
                    i = a.Index;
                    // calculate the number of switches        
                    if (CurrentDecision[i] != newDecision[i])
                        a.NumOfSwitchesOccured += 1;

                    // if turning on
                    if (CurrentDecision[i] == 0 && newDecision[i] == 1)
                    {
                        a.IfEverTurnedOnBefore = true;
                        a.IfEverTurnedOffBefore = a.IfEverTurnedOffBefore; // unchanged

                        a.EpiTimeIndexToGoIntoEffect = epiTimeIndex + a.NumOfTimeIndeciesDelayedToGoIntoEffect;
                        a.EpiTimeIndexToTurnOff = int.MaxValue;
                        a.EpiTimeIndexLastTurnedOn = epiTimeIndex;
                        a.EpiTimeIndexLastTurnedOff = int.MaxValue;
                    }
                    // if the intervention is turning off
                    else if (CurrentDecision[i] == 1 && newDecision[i] == 0)
                    {
                        a.IfEverTurnedOnBefore = a.IfEverTurnedOnBefore; //unchagned
                        a.IfEverTurnedOffBefore = true;

                        a.EpiTimeIndexToGoIntoEffect = int.MaxValue;
                        a.EpiTimeIndexToTurnOff = epiTimeIndex;
                        a.EpiTimeIndexLastTurnedOn = a.EpiTimeIndexLastTurnedOn; //unchanged
                        a.EpiTimeIndexLastTurnedOff = epiTimeIndex;
                    }
                    // if the intervention remains off
                    else if (CurrentDecision[i] == 0)
                        a.EpiTimeIndexToGoIntoEffect = int.MaxValue;

                    // calculate fixed cost
                    if (CurrentDecision[i] == 0 && newDecision[i] == 1)
                        CostOverThisDecisionPeriod += a.FixedCost;

                    // calculate the penalty cost for switching from on to off
                    if (a.PenaltyForSwitchingFromOnToOff > 0)
                    {
                        if (CurrentDecision[i] == 1 && newDecision[i] == 0)
                            CostOverThisDecisionPeriod += a.PenaltyForSwitchingFromOnToOff;
                    }

                    // update the new action
                    CurrentDecision[i] = newDecision[i];
                }

                // find the epidemic time index to change the interventions that are in effect
                EpiTimeIndexToChangeIntervetionsInEffect = FindNextEpiTimeIndexToChangeInterventionsInEffect();
            }

            foreach (Intervention a in Interventions)
            {
                // update the cost per unit of time for this action combination
                if (newDecision[a.Index] == 1)
                {
                    CostOverThisDecisionPeriod += a.CostPerDecisionPeriod;
                    ++a.NumOfDecisionPeriodsUsedOver;
                }
            }
        }

        //public void UpdateNextEpiTimeIndexToChangeInterventionsInEffect()
        //{
        //    EpiTimeIndexToChangeIntervetionsInEffect = FindNextEpiTimeIndexToChangeInterventionsInEffect();
        //}

        public void Reset()
        {
            _nextEpiTimeIndexToMakeDecision = 0;
            DecisionIntervalIndex = 0;
            CostOverThisDecisionPeriod = 0;
            CurrentDecision = new int[NumOfInterventions];// (int[])DefaultDecision.Clone();

            foreach (Intervention thisIntervention in Interventions)
                thisIntervention.Reset();
        }

        // find next epidemic time index when an intervention effect changes
        private int FindNextEpiTimeIndexToChangeInterventionsInEffect()
        {
            int minT = int.MaxValue;  // minimum t so far
            foreach (Intervention a in Interventions)
            {
                //if (CurrentDecision[a.Index] == 0)
                //    tEffect = ;
                //else
                //    tOff = ;

                minT = Math.Min(Math.Min(a.EpiTimeIndexToGoIntoEffect, a.EpiTimeIndexToTurnOff), minT);
            }
            return minT;
        }
    }

    public class MonitorOfInterventionsInEffect
    {
        private DecisionMaker _decisionMaker;   // the decision maker
        public int[] InterventionsInEffect { get; set; } // array of 0 and 1 to represent which interventions are in effect 

        public MonitorOfInterventionsInEffect(ref DecisionMaker decisionMaker)
        {
            _decisionMaker = decisionMaker;
            InterventionsInEffect = new int[_decisionMaker.NumOfInterventions];
        }

        public void MakeADecision(int epiTimeIndex, RNG rng, bool toInitialize, ref List<Class> classes)
        {
            // request for a decision
            if (toInitialize)
                _decisionMaker.MakeTheFirstDecision(epiTimeIndex, rng);
            else
                _decisionMaker.MakeANewDecision(epiTimeIndex, rng);

            // update interventions that are in effect
            if (epiTimeIndex == _decisionMaker.EpiTimeIndexToChangeIntervetionsInEffect)
            {
                foreach (Intervention a in _decisionMaker.Interventions)
                {
                    // if this intervention is going into effect
                    if (InterventionsInEffect[a.Index] == 0 && a.EpiTimeIndexToGoIntoEffect <= epiTimeIndex)
                    {
                        InterventionsInEffect[a.Index] = 1;
                        // find when it should be turned off
                        //a.EpiTimeIndexToTurnOff = a.FindEpiTimeIndexToTurnOff(epiTimeIndex);
                        a.EpiTimeIndexToGoIntoEffect = int.MaxValue;
                    }
                    // if this intervention is being lifted
                    else if (InterventionsInEffect[a.Index] == 1 && a.EpiTimeIndexToTurnOff <= epiTimeIndex)
                    {
                        InterventionsInEffect[a.Index] = 0;
                        a.EpiTimeIndexToGoIntoEffect = int.MaxValue;
                        a.EpiTimeIndexToTurnOff = int.MaxValue;
                    }
                }

                // add active events to classes
                AddActiveEvents(epiTimeIndex, ref classes);
            }
        }

        public void AddActiveEvents(int epiTimeIndex, ref List<Class> classes)
        {
            if (epiTimeIndex == _decisionMaker.EpiTimeIndexToChangeIntervetionsInEffect)
            {
                // update interventions that are in effect for each class
                foreach (Class thisClass in classes)
                    thisClass.AddActiveEvents(InterventionsInEffect);
            }
        }

        public void Reset()
        {
            InterventionsInEffect = new int[_decisionMaker.NumOfInterventions];
        }
    }

    public class ADP
    {
        // ADP stuff
        //double[] _arrSimulationObjectiveFunction;
        //private double[] _arrCurrentValuesOfFeatures = null;
        //private int _adpSimItr; // the index of simulation runs that should be done before doing back-propagation
        //private int[] _adpRndSeeds;
        //Discrete _discreteDistOverSeeds;
        private bool _storeADPIterationResults;
        private double[][] _arrADPIterationResults;
        public ArrayList Features { get; private set; } = new ArrayList();

        public bool UseEpidemicTimeAsFeature { get; private set; }

        public double[,] ArrADPIterationResults
        {
            get { return SupportFunctions.ConvertJaggedArrayToRegularArray(_arrADPIterationResults, 5); }

        }

        // set up POMDP-ADP
        public void SetUpADPAlgorithm(EnumObjectiveFunction objectiveFunction,
            int numOfADPIterations, int numOfSimRunsToBackPropogate,
            double wtpForHealth, double harmonicRule_a, double epsilonGreedy_beta, double epsilonGreedy_delta,
            bool storeADPIterationResults)
        {
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

        // update the cost of current ADP state-decision
        private void UpdateRewardOfCurrentADPStateDecision()
        {
            //// add reward
            //int numOfADPStates = _decisionMaker.NumberOfADPStates(_adpSimItr);
            //if (numOfADPStates > 0)
            //    _decisionMaker.AddToDecisionIntervalReward(_adpSimItr, numOfADPStates - 1, CurrentDeltaTReward());
            //((ADP_State)_POMDP_ADP.ADPStates[numOfADPStates - 1]).AddToDecisionIntervalReward();
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

        // setup dynamic policy related settings
        public void SetupDynamicPolicySettings
            (ComputationLib.EnumQFunctionApproximationMethod qFunctionApproximationMethod, bool useEpidemicTimeAsFeature, int degreeOfPolynomialQFunction, double L2RegularizationPenalty)
        {
            UseEpidemicTimeAsFeature = useEpidemicTimeAsFeature;
            if (UseEpidemicTimeAsFeature)
            {
                Features.Add(new Feature_EpidemicTime("Epidemic Time", 0));
                //++_numOfFeatures;
            }

            //_pastDecisionPeriodWithDecisionAsFeature = Math.Max(1, pastDecisionPeriodWithDecisionAsFeature);
            //_decisionsOverPastAndCurrentDecisionPeriods = new int[_pastDecisionPeriodWithDecisionAsFeature + 1];
            // setup Q-functions
            SetupPolynomialQFunctions(qFunctionApproximationMethod, degreeOfPolynomialQFunction);
            // add L2 regularization
            if (L2RegularizationPenalty > 0)
                AddL2Regularization(L2RegularizationPenalty);
        }

        // get dynamic policy // only one dimensional
        public void GetOptimalDynamicPolicy(ref string featureName, ref double[] headers, ref int[] optimalDecisions,
            int numOfIntervalsToDescretizeFeatures)
        {
            if (Features.Count > 1) return; // this procedure works when the feature set constrains 1 feature

            headers = new double[numOfIntervalsToDescretizeFeatures + 1];
            optimalDecisions = new int[numOfIntervalsToDescretizeFeatures + 1];

            // get the only feature
            Feature theOnlyFeature = (Feature)Features[0];

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
            int numOfFeatures = Features.Count;

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
            foreach (Feature thisFeature in Features)
            {
                featureIndex = thisFeature.Index;
                arrFeatureMin[featureIndex] = thisFeature.Min;
                arrFeatureMax[featureIndex] = thisFeature.Max;
                arrFeatureIntervalLengths[featureIndex] = (arrFeatureMax[featureIndex] - arrFeatureMin[featureIndex]) / numOfIntervalsToDescretizeFeatures;
                arrFeatureNumOfBreakPoints[featureIndex] = numOfIntervalsToDescretizeFeatures + 1;
            }
            // feature names
            foreach (Feature thisFeature in Features)
                strFeatureNames[thisFeature.Index] = thisFeature.Name;

            // reset available action combinations to the initial setting
            //_simDecisionMaker.MakeAllDynamicallyControlledActionsAvailable();

            // write the dynamic Policy
            double[] arrFeatureValues = new double[Features.Count];
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

        // setup Q-functions with polynomial functions
        public void SetupPolynomialQFunctions(EnumQFunctionApproximationMethod qFunctionApproximationMethod, int degreeOfPolynomialQFunction)
        {
            // int numOfFeatures = Features.Count;
            //_decisionMaker.SetUpQFunctionApproximationModel(
            //    qFunctionApproximationMethod, SimulationLib.enumResponseTransformation.None, 
            //    numOfFeatures, degreeOfPolynomialQFunction, 2);
        }
        // add L2 regularization
        public void AddL2Regularization(double penaltyParameter)
        {
            //_decisionMaker.AddL2Regularization(penaltyParameter);
        }
        public void UpdateQFunctionCoefficients(double[][] qFunctionCoefficients)
        {
            double[] arrCoefficients = new double[qFunctionCoefficients.Length * qFunctionCoefficients[0].Length];

            // concatenate initial estimates
            int k = 0;
            for (int i = 0; i < qFunctionCoefficients.Length; i++)
                for (int j = 0; j < qFunctionCoefficients[i].Length; j++)
                    arrCoefficients[k++] = qFunctionCoefficients[i][j];

            //_decisionMaker.UpdateQFunctionCoefficients(arrCoefficients);                  
        }
        public void UpdateQFunctionCoefficients(double[] qFunctionCoefficients)
        {
            //_decisionMaker.UpdateQFunctionCoefficients(qFunctionCoefficients);
        }

        public void Reset()
        {
            Features = new ArrayList();
        }

        // add current ADP state-decision
        private void StoreCurrentADPStateDecision()
        {
            // check if it is time to record current state
            //if (_epiTimeIndex == _nextDecisionPointIndex || _modelUse = EnumModelUse.Optimization)
            //    return;

            // make a new state-decision
            //ADPState thisADPState = new ADPState(_arrCurrentValuesOfFeatures, _decisionMaker.CurrentInterventionCombination);
            //thisADPState.ValidStateToUpdateQFunction = true;

            //// check if this is eligible            
            ////thisADPState.ValidStateToUpdateQFunction = true;
            //if (_POMDP_ADP.EpsilonGreedyDecisionSelectedAmongThisManyAlternatives > 1)
            //    thisADPState.ValidStateToUpdateQFunction = true;
            //else
            //    thisADPState.ValidStateToUpdateQFunction = false;

            // store the adp state-decision
            //_simDecisionMaker.AddAnADPState(_adpSimItr, thisADPState);            
        }
    }
}

//// find the on/off switching type
//                switch (thisIntervention.OnOffSwitchSetting)
//                {
//                    #region predetermined
//                    case SimulationLib.Intervention.enumOnOffSwitchSetting.Predetermined:
//                        {
//                            if (thisIntervention.PredeterminedSwitchValue == SimulationLib.Intervention.enumSwitchValue.On &&
//                                onOffStatus == 0)
//                                return false;
//                            if (thisIntervention.PredeterminedSwitchValue == SimulationLib.Intervention.enumSwitchValue.Off &&
//                                onOffStatus == 1)
//                                return false;
//                        }
//                        break;
//                    #endregion

//                    #region interval based
//                    case SimulationLib.Intervention.enumOnOffSwitchSetting.IntervalBased:
//                        {
//                            // already checked
//                        }
//                        break;
//                    #endregion

//                    #region threshold base
//                    case SimulationLib.Intervention.enumOnOffSwitchSetting.ThresholdBased:
//                        {
//                            int idOfSpecialStat = thisIntervention.ThresholdBasedEmployment_IDOfTheSpecialStatisticsToObserveAccumulation;
//double threshold = thisIntervention.ThresholdBasedEmployment_thresholdToTriggerThisIntervention;
//int numOfTimeIncidesToUse = thisIntervention.ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention;

//// read the value of special statistics
//double observedEpidemiologicalMeasure = 0;
//                            foreach (SummationStatistics thisSumStat in _summationStatistics)
//                            {
//                                if (thisSumStat.ID == idOfSpecialStat)
//                                {
//                                    switch (thisIntervention.ThresholdBasedEmployment_EpidemiologicalObservation)
//                                    {
//                                        case Intervention.EnumEpidemiologicalObservation.Accumulating:
//                                            observedEpidemiologicalMeasure = thisSumStat.ObservedAccumulatedNewMembers;
//                                            break;
//                                        case Intervention.EnumEpidemiologicalObservation.OverPastObservationPeriod:
//                                            observedEpidemiologicalMeasure = thisSumStat.NewMembersOverPastObservableObsPeriod;
//                                            break;
//                                    }
//                                    break;
//                                }
//                            }
//                            foreach (RatioStatistics thisRatioStat in _ratioStatistics)
//                            {
//                                if (thisRatioStat.ID == idOfSpecialStat)
//                                {
//                                    observedEpidemiologicalMeasure = thisRatioStat.CurrentValue;
//                                    break;
//                                }

//                            }
//                            // check the feasibility conditions
//                            // if this intervention is on 
//                            if (onOffStatus == 1)
//                            {
//                                // find the type of threshold
//                                switch (thisIntervention.ThresholdBasedEmployment_EpidemiologicalObservation)
//                                {
//                                    case Intervention.EnumEpidemiologicalObservation.Accumulating:
//                                        #region
//                                        {
//                                            // compare the observation with the triggering threshold
//                                            if (observedEpidemiologicalMeasure<threshold)
//                                            {
//                                                // check if the intervention should still remain in use 
//                                                if (!thisIntervention.IfThisInterventionHasBeenEmployedBefore)
//                                                    return false;
//                                                // check how much longer this intervention should remain in use
//                                                else if (thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn +
//                                                    thisIntervention.NumOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn +
//                                                    thisIntervention.ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention <= _)
//                                                    return false;
//                                            }
//                                            else // if (observedEpidemiologicalMeasure >= threshold)
//                                            {
//                                                // it maybe infeasible only if this intervention has been used before
//                                                if (thisIntervention.IfThisInterventionHasBeenEmployedBefore &&
//                                                    thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn +
//                                                    thisIntervention.NumOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn +
//                                                    thisIntervention.ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention <= _currentEpidemicTimeIndex)
//                                                    return false;
//                                            }
//                                        }
//                                        #endregion
//                                        break;
//                                    case Intervention.EnumEpidemiologicalObservation.OverPastObservationPeriod:
//                                        #region
//                                        {
//                                            // compare the observation with the triggering threshold
//                                            if (observedEpidemiologicalMeasure<threshold)
//                                            {
//                                                // check if the intervention should still remain in use 
//                                                if (!thisIntervention.IfThisInterventionHasBeenEmployedBefore)
//                                                    return false;
//                                                // check how much longer this intervention should remain in use
//                                                else if (thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn +
//                                                    thisIntervention.NumOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn +
//                                                    thisIntervention.ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention <= _currentEpidemicTimeIndex)
//                                                    return false;
//                                            }
//                                            else // if (observedEpidemiologicalMeasure >= threshold)
//                                            {
//                                                // should remain on
//                                            }
//                                        }
//                                        #endregion
//                                        break;
//                                }
//                            }
//                            else // if this intervention is off
//                            {
//                                // find the type of threshold
//                                switch (thisIntervention.ThresholdBasedEmployment_EpidemiologicalObservation)
//                                {
//                                    case Intervention.EnumEpidemiologicalObservation.Accumulating:
//                                        #region
//                                        {
//                                            // compare the observation with the triggering threshold
//                                            if (observedEpidemiologicalMeasure<threshold)
//                                            {
//                                                // check if the intervention should still remain in use 
//                                                if (thisIntervention.IfThisInterventionHasBeenEmployedBefore &&
//                                                    thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn +
//                                                    thisIntervention.NumOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn +
//                                                    thisIntervention.ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention > _currentEpidemicTimeIndex)
//                                                    return false;
//                                            }
//                                            else // if (observedEpidemiologicalMeasure >= threshold)
//                                            {
//                                                // it is infeasible if this intervention has been used before
//                                                if (!thisIntervention.IfThisInterventionHasBeenEmployedBefore)
//                                                    return false;
//                                            }
//                                        }
//                                        #endregion
//                                        break;
//                                    case Intervention.EnumEpidemiologicalObservation.OverPastObservationPeriod:
//                                        #region
//                                        {
//                                            // compare the observation with the triggering threshold
//                                            if (observedEpidemiologicalMeasure<threshold)
//                                            {
//                                                // check if the intervention should still remain in use 
//                                                if (thisIntervention.IfThisInterventionHasBeenEmployedBefore &&
//                                                    thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn +
//                                                    thisIntervention.NumOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn +
//                                                    thisIntervention.ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention > _currentEpidemicTimeIndex)
//                                                    return false;
//                                            }
//                                            else // if (observedEpidemiologicalMeasure >= threshold)
//                                            {
//                                                return false;
//                                            }
//                                        }
//                                        #endregion
//                                        break;
//                                }
//                            }
//                        }
//                        break;
//                    #endregion

//                    #region periodic
//                    case SimulationLib.Intervention.enumOnOffSwitchSetting.Periodic:
//                        {
//                            int frequency_numOfDecisionPeriods = thisIntervention.PeriodicEmployment_Frequency_NumOfDcisionPeriods;
//int duration_numOfDecisionPeriods = thisIntervention.PeriodicEmployment_Duration_NumOfDcisionPeriods;

//                            // check the feasibility conditions
//                            if (onOffStatus == 1)
//                            {
//                                if (_currentEpidemicTimeIndex > 0 &&
//                                    _currentEpidemicTimeIndex >= thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn + duration_numOfDecisionPeriods* _numOfDeltaTsInADecisionInterval)
//                                    return false;
//                            }
//                            else // if this intervention is off
//                            {
//                                if (_currentEpidemicTimeIndex >= thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn + frequency_numOfDecisionPeriods* _numOfDeltaTsInADecisionInterval)
//                                    return false;
//                                if (_currentEpidemicTimeIndex<thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn + duration_numOfDecisionPeriods* _numOfDeltaTsInADecisionInterval)
//                                    return false;
//                            }
//                        }
//                        break;
//                    #endregion

//                    #region dynamic
//                    case SimulationLib.Intervention.enumOnOffSwitchSetting.Dynamic:
//                        {
//                            // if it has to remain on once it's turned on
//                            if (thisIntervention.RemainsOnOnceSwitchedOn == true)
//                            {
//                                if (onOffStatus == 0 && _decisionMaker.CurrentActionCombination[index] == 1)
//                                    return false;
//                            }
//                        }
//                        break;
//                    #endregion
//                }