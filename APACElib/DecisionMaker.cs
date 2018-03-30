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
        public int NumOfInterventions { get; set; } = 0;
        public int[] CurrentInterventionCombination { get; set; } = new int[0];   // array of 0 and 1 to represent which action is on or off
        public int[] DefaultInterventionCombination { get; set; } = new int[0];   // if all other actions become unavailable, we will use this action combination 
        public double CostOverThisDecisionPeriod { get; set; } = 0; // cost actions and decision making
        // Instantiation
        public DecisionMaker()
        {
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
            DefaultInterventionCombination = new int[Interventions.Count];
            CurrentInterventionCombination = new int[Interventions.Count];

            // make sure the default intervention is on
            foreach (Intervention intervention in Interventions
                .Where(s => s.Type == EnumInterventionType.Default))
            {
                    DefaultInterventionCombination[intervention.Index] = 1;
                    CurrentInterventionCombination[intervention.Index] = 1;
            }
        }

        // find a new action combination 
        public void MakeANewDecision(ref int[] newActionCombination, int timeIndex)
        {
            newActionCombination = new int[NumOfInterventions];
            // find the switch status of each action
            foreach (Intervention inter in Interventions)
            {
                newActionCombination[inter.Index] = inter.FindSwitchStatus(timeIndex);
            }
        }
        
        // update the currect intervention combination       
        public void UpdateInterventionCombination(int[] newActionCombination)
        {
            CostOverThisDecisionPeriod = 0; // reset cost over the next decision period

            int thisActionIndex = 0;
            foreach (Intervention thisIntervention in Interventions)
            {
                thisActionIndex = thisIntervention.Index;
                // calculate the number of switches        
                if (CurrentInterventionCombination[thisActionIndex] != newActionCombination[thisActionIndex])
                    thisIntervention.NumOfSwitchesOccured += 1;

                // if turning on
                if (CurrentInterventionCombination[thisActionIndex] == 0 && newActionCombination[thisActionIndex] == 1)
                {
                    CostOverThisDecisionPeriod += thisIntervention.FixedCost;
                    thisIntervention.NumOfSwitchesOccured += 1;
                    thisIntervention.IfHasBeenTrunedOnBefore = true;
                }

                // calculate the penalty cost for switching from on to off
                if (thisIntervention.PenaltyForSwitchingFromOnToOff > 0)
                {
                    if (CurrentInterventionCombination[thisActionIndex] == 1 && newActionCombination[thisActionIndex] == 0)
                        CostOverThisDecisionPeriod += thisIntervention.PenaltyForSwitchingFromOnToOff;
                }

                // update the new action
                CurrentInterventionCombination[thisActionIndex] = newActionCombination[thisActionIndex];

                // update the cost per unit of time for this action combination
                if (CurrentInterventionCombination[thisActionIndex] == 1)
                {
                    CostOverThisDecisionPeriod += thisIntervention.CostPerDecisionPeriod;
                    ++thisIntervention.NumOfDecisionPeriodsOverWhichThisInterventionWasUsed;
                }
            }
        }

        public void ResetForAnotherSimulationRun()
        {
            CostOverThisDecisionPeriod = 0;
            CurrentInterventionCombination = (int[])DefaultInterventionCombination.Clone();
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