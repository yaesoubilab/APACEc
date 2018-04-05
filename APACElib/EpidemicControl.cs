using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APACElib
{
    public class DecisionMaker
    {
        public List<Intervention> Interventions { get; set; } = new List<Intervention>();
        public int NumOfInterventions { get; set; } = 0;

        private int _nOfDeltaTsInADecisionInterval; // number of time steps in a decision interval
        private int _nextEpiTimeIndexToMakeDecision; // next epidemic time index to make a decision       
        public int DecisionIntervalIndex { get; set; } // index of the current decision interval
        public int EpiTimeIndexToStartDecisionMaking { get; set; }
        public int EpiTimeIndexToChangeIntervetionsInEffect { get; set; } = 0; // epidemic time index to change the interventions that are in effect
        private int[][] _prespecifiedDecisionsOverDecisionsPeriods; // prespecified decisions

        public int[] CurrentDecision { get; set; } = new int[0];   // array of 0 and 1 to represent which action is on or off
        public int[] DefaultDecision { get; set; } = new int[0];   // if all other actions become unavailable, we will use this action combination 
        public double CostOverThisDecisionPeriod { get; set; } = 0; // cost actions and decision making
        
        // Instantiation
        public DecisionMaker(int epiTimeIndexToStartDecisionMaking, int nOfDeltaTsInADecisionInterval)
        {
            EpiTimeIndexToStartDecisionMaking = epiTimeIndexToStartDecisionMaking;
            _nextEpiTimeIndexToMakeDecision = 0;
            _nOfDeltaTsInADecisionInterval = nOfDeltaTsInADecisionInterval;
            DecisionIntervalIndex = 0;
        }

        // add a decision
        public void AddAnIntervention(Intervention action)
        {
            // add the actions
            Interventions.Add(action);
            ++NumOfInterventions;
        }

        // update after all interventions are added
        public void UpdateAfterAllInterventionsAdded()
        {
            DefaultDecision = new int[Interventions.Count];
            CurrentDecision = new int[Interventions.Count];

            // make sure the default intervention is on
            foreach (Intervention intervention in Interventions
                .Where(s => s.Type == EnumInterventionType.Default))
            {
                DefaultDecision[intervention.Index] = 1;
                CurrentDecision[intervention.Index] = 1;
            }
        }

        // add prespecified decisions
        public void AddPrespecifiedDecisionsOverDecisionsPeriods(int[][] prespecifiedDecisionsOverDecisionsPeriods)
        {
            _prespecifiedDecisionsOverDecisionsPeriods = prespecifiedDecisionsOverDecisionsPeriods;
        }

        // find a new intervention combination (return true if there is a chance in decision)
        public void MakeANewDecision(int epiTimeIndex, bool toInitialize)
        {
            // make a decision only at epidemic time 0 or the next decision point
            if (!(toInitialize || epiTimeIndex == _nextEpiTimeIndexToMakeDecision))
                return; // no change in decision 

            int[] newDecision = new int[NumOfInterventions];
            bool ifThereIsAChange = false;

            // check if decisions are not prespecified
            if (_prespecifiedDecisionsOverDecisionsPeriods == null)
            {
                // find the switch status of each action
                foreach (Intervention inter in Interventions)
                {
                    newDecision[inter.Index] = inter.FindSwitchStatus(epiTimeIndex);
                }
            }
            else // if decisions are prespecified
                newDecision = _prespecifiedDecisionsOverDecisionsPeriods[DecisionIntervalIndex];


            // check if this new intervention combination is the same as the current one
            if (CurrentDecision.SequenceEqual(newDecision))
                ifThereIsAChange = false;
            else
                ifThereIsAChange = true;              

            // update the current intervention combination to the new one
            UpdateCurrentDecision(newDecision, epiTimeIndex, ifThereIsAChange);

            // update the index of the decision period
            DecisionIntervalIndex += 1;
            // update the next time decisions should be made
            _nextEpiTimeIndexToMakeDecision = Math.Max(epiTimeIndex + _nOfDeltaTsInADecisionInterval, EpiTimeIndexToStartDecisionMaking);
        }

        // update the currect decision    
        private void UpdateCurrentDecision(int[] newDecision, int epiTimeIndex, bool ifThereIsAChange)
        {
            CostOverThisDecisionPeriod = 0; // reset cost over the next decision period

            // if there is a change in decisoin
            if (ifThereIsAChange)
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
                        a.IfHasBeenTrunedOnBefore = true;
                        a.EpiTimeIndexTurnedOn = epiTimeIndex;
                        a.EpiTimeIndexToGoIntoEffect = epiTimeIndex + a.NumOfTimeIndeciesDelayedToGoIntoEffectOnceTurnedOn;
                        a.EpiTimeIndexToTurnOff = int.MaxValue;
                    }
                    // if the intervention is turning off
                    else if (CurrentDecision[i] == 1 && newDecision[i] == 0)
                    {
                        a.EpiTimeIndexTurnedOff = epiTimeIndex;
                        a.EpiTimeIndexToTurnOff = epiTimeIndex;
                        a.EpiTimeIndexToGoIntoEffect = int.MaxValue;
                    }
                    // if the intervention remains off
                    else if (CurrentDecision[i] == 0)
                    {
                        a.EpiTimeIndexToGoIntoEffect = int.MaxValue;
                    }

                    // calculate fixed cost
                    if (CurrentDecision[i] == 0 && newDecision[i] == 1)
                    {
                        CostOverThisDecisionPeriod += a.FixedCost;
                    }

                    // calculate the penalty cost for switching from on to off
                    if (a.PenaltyForSwitchingFromOnToOff > 0)
                    {
                        if (CurrentDecision[i] == 1 && newDecision[i] == 0)
                            CostOverThisDecisionPeriod += a.PenaltyForSwitchingFromOnToOff;
                    }

                    // update the cost per unit of time for this action combination
                    if (newDecision[i] == 1)
                    {
                        CostOverThisDecisionPeriod += a.CostPerDecisionPeriod;
                        ++a.NumOfDecisionPeriodsOverWhichThisInterventionWasUsed;
                    }

                    // update the new action
                    CurrentDecision[i] = newDecision[i];
                }

                // find the epidemic time index to change the interventions that are in effect
                EpiTimeIndexToChangeIntervetionsInEffect = FindNextEpiTimeIndexToChangeInterventionsInEffect();
            }

            // update cost of this period
            foreach (Intervention a in Interventions)
            {
                if (newDecision[a.Index] == 1)
                {
                    CostOverThisDecisionPeriod += a.CostPerDecisionPeriod;
                    ++a.NumOfDecisionPeriodsOverWhichThisInterventionWasUsed;
                }
            }
        }

        public void ResetForAnotherSimulationRun()
        {
            CostOverThisDecisionPeriod = 0;
            CurrentDecision = (int[])DefaultDecision.Clone();
        }

        private void ReadValuesOfFeatures(int epiTimeIndex)
        {
            // check if it is time to record current state
            if (epiTimeIndex == _nextEpiTimeIndexToMakeDecision) //|| _aResourceJustReplinished == true) //(EligibleToStoreADPStateDecision())
            {
                // update the values of features
                //int i = 0;
                //foreach (Feature thisFeature in _features)
                //{
                //    i = thisFeature.Index;

                //    if (thisFeature is Feature_EpidemicTime)
                //    {
                //        _arrCurrentValuesOfFeatures[i] = epiTimeIndex * _deltaT;
                //    }
                //    else if (thisFeature is Feature_DefinedOnNewClassMembers)
                //    {
                //        _arrCurrentValuesOfFeatures[i] = Math.Max((_classes[((Feature_DefinedOnNewClassMembers)thisFeature).ClassID])
                //                                                            .ReadFeatureValue((Feature_DefinedOnNewClassMembers)thisFeature), 0);
                //    }
                //    else if (thisFeature is Feature_DefinedOnSummationStatistics)
                //    {
                //        int sumStatID = ((Feature_DefinedOnSummationStatistics)thisFeature).SumStatisticsID;
                //        _arrCurrentValuesOfFeatures[i] = (_summationStatistics[sumStatID]).ReadFeatureValue((Feature_DefinedOnSummationStatistics)thisFeature);
                //    }
                //    else if (thisFeature is Feature_InterventionOnOffStatus)
                //    {
                //        int interventionID = ((Feature_InterventionOnOffStatus)thisFeature).InterventionID;
                //        int numOfPastObservationPeriodToObserveOnOffValue = ((Feature_InterventionOnOffStatus)thisFeature).PreviousObservationPeriodToObserveOnOffValue;
                //        _arrCurrentValuesOfFeatures[i] = _pastActionCombinations[numOfPastObservationPeriodToObserveOnOffValue][interventionID];
                //    }
                //    else if (thisFeature is Feature_NumOfDecisoinPeriodsOverWhichThisInterventionWasUsed)
                //    {
                //        int interventionID = ((Feature_InterventionOnOffStatus)thisFeature).InterventionID;
                //        _arrCurrentValuesOfFeatures[i] = (_decisionMaker.Interventions[interventionID]).NumOfDecisionPeriodsOverWhichThisInterventionWasUsed;
                //    }


                //    // update the min max on this feature
                //    thisFeature.UpdateMinMax(_arrCurrentValuesOfFeatures[i]);
                //}
            }
        }

        // find next epidemic time index when an intervention effect changes
        private int FindNextEpiTimeIndexToChangeInterventionsInEffect()
        {
            int tIndex = int.MaxValue;
            int temp;
            foreach (Intervention a in Interventions)
            {
                if (a.Type != EnumInterventionType.Default)
                {
                    temp = Math.Min(a.EpiTimeIndexToTurnOff, a.EpiTimeIndexToGoIntoEffect);
                    tIndex = Math.Min(temp, tIndex);
                }
            }
            return tIndex;
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

        public void Update(int epiTimeIndex, bool toInitialize, ref List<Class> classes)
        {
            // request for a decision
            _decisionMaker.MakeANewDecision(epiTimeIndex, toInitialize);

            // update interventions that are in effect
            if (epiTimeIndex == _decisionMaker.EpiTimeIndexToChangeIntervetionsInEffect)
            {
                foreach (Intervention a in _decisionMaker.Interventions)
                {
                    // the default intervention is always in effect
                    if (a.Type == EnumInterventionType.Default)
                        InterventionsInEffect[a.Index] = 1;
                    else
                    {
                        // if this intervention is going into effect
                        if (InterventionsInEffect[a.Index] == 0 && a.EpiTimeIndexToGoIntoEffect <= epiTimeIndex)
                        {
                            InterventionsInEffect[a.Index] = 1;

                            // find when it should be turned off
                            a.EpiTimeIndexToTurnOff = a.FindEpiTimeIndexToTurnOff(epiTimeIndex);
                            a.EpiTimeIndexToGoIntoEffect = int.MaxValue;
                        }
                        // if this intervention is being lifted
                        else if (InterventionsInEffect[a.Index] == 1 && a.EpiTimeIndexToTurnOff <= epiTimeIndex)
                        {
                            InterventionsInEffect[a.Index] = 0;

                            a.EpiTimeIndexToTurnOn = int.MaxValue;
                            a.EpiTimeIndexToGoIntoEffect = int.MaxValue;
                            a.EpiTimeIndexToTurnOff = int.MaxValue;
                        }
                    }
                }

                // update interventions that are in effect for each class
                foreach (Class thisClass in classes)
                    thisClass.SelectThisInterventionCombination(InterventionsInEffect);
            }

        }        
        
    }


    public class ADP
    {

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
//                                                    thisIntervention.ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention <= _currentEpidemicTimeIndex)
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