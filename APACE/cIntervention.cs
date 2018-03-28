using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomVariateLib;
using ComputationLib;
using SimulationLib;

namespace APACE_lib
{
    // Intervention
    public class Intervention : SimulationLib.SimulationAction
    {
        public bool IfAffectingContactPattern { get; set; }
        
        // availability
        public int ParIDDelayToGoIntoEffectOnceTurnedOn { get; set; }
        public int NumOfTimeIndeciesDelayedToGoIntoEffectOnceTurnedOn { get; set; } = 0;

        public int EpidemicTimeIndexToTurnOn { get; set; }
        public int EpidemicTimeIndexToGoIntoEffect { get; set; }
        public int EpidemicTimeIndexToTurnOff { get; set; }
        public int EpidemicTimeIndexTurnedOn { get; set; }
        public int EpidemicTimeIndexTurnedOff { get; set; }
        
        // Instantiation
        public Intervention(
            int index, 
            string name, 
            EnumActionType actionType, 
            bool affectingContactPattern,
            int timeIndexBecomeAvailable,
            int timeIndexBecomeUnavailable,
            int parIDDelayToGoIntoEffectOnceTurnedOn,
            ref SimulationDecisionRule decisionRule)
                : base (
                      index, 
                      name,
                      actionType,
                      timeIndexBecomeAvailable, 
                      timeIndexBecomeUnavailable, 
                      ref decisionRule)
        {
            IfAffectingContactPattern = affectingContactPattern;
            ParIDDelayToGoIntoEffectOnceTurnedOn = parIDDelayToGoIntoEffectOnceTurnedOn;
        }

        // find if this decision can be used
        //public bool IfCanBeEmployedAccordingToTheThresholdBasedPolicy(int currentEpidemicTimeIndex, double currentObservation)
        //{
        //    bool ifCanBeUsed = false;

        //    // check if this intervention is trigged based on observation
        //    if (base.OnOffSwitchSetting != enumOnOffSwitchSetting.ThresholdBased)
        //        return false;

        //    // check if this decision is available at current time index
        //    if (currentEpidemicTimeIndex < base.TimeIndexBecomeAvailable || currentEpidemicTimeIndex > base.TimeIndexBecomeUnavailable)
        //        return false;

        //    // if duration is zero
        //    if (_thresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention <= 0)
        //        return false;

        //    // if this decision has been employed before
        //    if (_ifThisInterventionHasBeenEmployedBefore)
        //    {
        //        // check if this decision can still be used
        //        if (currentEpidemicTimeIndex >= _epidemicTimeIndexToStopThisIntervention)
        //            return false;
        //        else
        //            ifCanBeUsed = true;
        //    }
        //    else
        //    {
        //        // check if threshold is passed
        //        if (currentObservation >= _thresholdBasedEmployment_thresholdToTriggerThisIntervention)
        //        {
        //            ifCanBeUsed = true;
        //            // record the first time this decision is employed
        //            _ifThisInterventionHasBeenEmployedBefore = true;
        //            _epidemicTimeIndexWhenThisInterventionIsTriggered = currentEpidemicTimeIndex;
        //            // find the time when this decision can no longer be used
        //            _epidemicTimeIndexToStopThisIntervention = _epidemicTimeIndexWhenThisInterventionIsTriggered
        //                + _thresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention;
        //        }
        //    }            
        //    return ifCanBeUsed;
        //}

        // reset for another simulation run

        public void ResetForAnotherSimulationRun()
        {
            // reset the base class
            base.ResetForAnotherSimulation();

            // find the time to go into effect
            if (ActionType == EnumActionType.Default)
            {
                EpidemicTimeIndexToGoIntoEffect = 0;
                EpidemicTimeIndexToTurnOff = int.MaxValue;
            }
            else
            {
                EpidemicTimeIndexToGoIntoEffect = int.MaxValue;
                EpidemicTimeIndexToTurnOff = int.MaxValue;
            }
            EpidemicTimeIndexTurnedOn = int.MaxValue;
            EpidemicTimeIndexToTurnOff = int.MinValue;
        }
        
    }
}
