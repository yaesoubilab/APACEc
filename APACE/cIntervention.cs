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
        private long NumOfTimeIndeciesDelayedToGoIntoEffectOnceTurnedOn { get; set; } = 0;

        public long EpidemicTimeIndexToTurnOn { get; set; }
        public long EpidemicTimeIndexToGoIntoEffect { get; set; }
        public long EpidemicTimeIndexToTurnOff { get; set; }
        public long EpidemicTimeIndexTurnedOn { get; set; }
        public long EpidemicTimeIndexTurnedOff { get; set; }


        // Instantiation
        public Intervention(
            int index, 
            string name, 
            SimulationLib.EnumActionType actionType, 
            bool affectingContactPattern,
            long timeIndexBecomeAvailable,
            long timeIndexBecomeUnavailable,
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

        // set up costs
        public void SetUpCosts(double fixedCost, double costPerUnitOfTime, double penaltyForSwitchingFromOnToOff)
        { 
            base.SetUpCost(fixedCost, costPerUnitOfTime, penaltyForSwitchingFromOnToOff);
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
            // find the time to go into effect
            if (ActionType == EnumActionType.Default)
            {
                EpidemicTimeIndexToGoIntoEffect = long.MinValue;
                EpidemicTimeIndexToTurnOff = long.MaxValue;
            }
            else
            {
                EpidemicTimeIndexToGoIntoEffect = long.MaxValue;
                EpidemicTimeIndexToTurnOff = long.MaxValue;
            }

            IfHasBeenTrunedOnBefore = false;
            EpidemicTimeIndexTurnedOn = long.MaxValue;
            EpidemicTimeIndexToTurnOff = long.MinValue;
        }

        // support methods
        public static EnumActionType ConvertToActionType(string strInterventionType)
        {
            EnumActionType interventionType = EnumActionType.Default;
            switch (strInterventionType)
            {
                case "Default":
                    interventionType = EnumActionType.Default;
                    break;
                case "Additive":
                    interventionType = EnumActionType.Additive;
                    break;
            }
            return interventionType;
        }
    }
}
