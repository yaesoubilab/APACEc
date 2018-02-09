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
        public enum EnumEpidemiologicalObservation
        {
            OverPastObservationPeriod = 1,
            Accumulating = 2
        }

        // Fields                       
        bool _affectingContactPattern = false;

        // availability
        private int _parIDDelayToGoIntoEffectOnceTurnedOn = 0;

        private long _epidemicTimeIndexToTurnOn = 0;
        private long _epidemicTimeIndexToGoIntoEffect = 0;
        private long _epidemicTimeIndexToTrunOff = 0;

        // threshold-base employment          
        private int _thresholdBasedEmployment_IDOfTheSpecialStatisticsToObserveAccumulation = 0;
        private EnumEpidemiologicalObservation _thresholdBasedEmployment_EpidemiologicalObservation = EnumEpidemiologicalObservation.OverPastObservationPeriod;
             
        
        // Instantiation
        public Intervention(
            int index, 
            string name, 
            EnumActionType type, 
            bool affectingContactPattern,
            long timeIndexBecomeAvailable,
            long timeIndexBecomeUnavailable,
            long nOfTimeIndeciesDelayedToGoIntoEffectOnceTurnedOn,
            ref SimulationDecisionRule decisionRule,
            int IDOfTheResourceRequiredToBeAvailable = -1)
                : base (
                      index, 
                      name, 
                      type, 
                      timeIndexBecomeAvailable, 
                      timeIndexBecomeUnavailable, 
                      nOfTimeIndeciesDelayedToGoIntoEffectOnceTurnedOn,
                      ref decisionRule, 
                      IDOfTheResourceRequiredToBeAvailable)
        {
            _affectingContactPattern = affectingContactPattern;     
        }

        // Properties    
        #region
        public bool AffectingContactPattern
        {
            get { return _affectingContactPattern; }
        }
        // availability
        public int ParIDDelayToGoIntoEffectOnceTurnedOn
        {
            get { return _parIDDelayToGoIntoEffectOnceTurnedOn; }
        }
        public long EpidemicTimeIndexToTurnOn
        {
            get { return _epidemicTimeIndexToTurnOn; }
            set { _epidemicTimeIndexToTurnOn = value; }
        }
        public long EpidemicTimeIndexToGoIntoEffect
        {
            get { return _epidemicTimeIndexToGoIntoEffect; }
            set { _epidemicTimeIndexToGoIntoEffect = value; }
        }
        public long EpidemicTimeIndexToTurnOff
        {
            get { return _epidemicTimeIndexToTrunOff; }
            set { _epidemicTimeIndexToTrunOff = value; }
        }
        #endregion

        // set up costs
        public void SetUpCosts(double fixedCost, double costPerUnitOfTime, double penaltyForSwitchingFromOnToOff)
        { 
            base.SetUpCost(fixedCost, costPerUnitOfTime, penaltyForSwitchingFromOnToOff);
        }
        
        // add the settings for periodic employment
        public void AddPeriodicEmploymentSetting(int frequency_numOfDcisionPeriods, int duration_numOfDcisionPeriods)
        {   
            base.OnOffSwitchSetting = enumOnOffSwitchSetting.Periodic;
            _periodicEmployment_Frequency_NumOfDcisionPeriods = frequency_numOfDcisionPeriods;
            _periodicEmployment_Duration_NumOfDcisionPeriods = duration_numOfDcisionPeriods;            
        }  
        // add the settings for interval-base employment
        public void AddIntervalBaseEmploymentSetting(double availableUntilThisTime, int minNumOfDecisionPeriodsToUse)
        {
            base.OnOffSwitchSetting = enumOnOffSwitchSetting.IntervalBased;
            _intervalBaseEmployment_availableUntilThisTime = availableUntilThisTime;
            _intervalBaseEmployment_minNumOfDecisionPeriodsToUse = minNumOfDecisionPeriodsToUse;
        }
        // add the settings for threshold-based employment
        public void AddThresholdBasedEmploymentSetting
                (int IDOfTheSpecialStatisticsToObserveAccumulation,
                EnumEpidemiologicalObservation epidemiologicalObservation,
                double thresholdToTriggerThisDecision, 
                long numOfTimeIndicesToUseThisDecision)
        {
            base.OnOffSwitchSetting = enumOnOffSwitchSetting.ThresholdBased;
            _thresholdBasedEmployment_IDOfTheSpecialStatisticsToObserveAccumulation = IDOfTheSpecialStatisticsToObserveAccumulation;
            _thresholdBasedEmployment_EpidemiologicalObservation = epidemiologicalObservation;
            _thresholdBasedEmployment_thresholdToTriggerThisIntervention = thresholdToTriggerThisDecision;
            _thresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention = numOfTimeIndicesToUseThisDecision;
        }              
        // dynamic policy settings
        public void AddDynamicPolicySettings( bool remainsOnOnceSwitchedOn) //bool selectOnOffStatusAsFeature, int previousObservationPeriodToObserveOnOffValue, bool useNumOfDecisionPeriodEmployedAsFeature,
        {
            //_dynamicEmployment_selectOnOffStatusAsFeature = selectOnOffStatusAsFeature;
            //_dynamicEmployment_previousObservationPeriodToObserveOnOffValue = previousObservationPeriodToObserveOnOffValue;
            //_dynamicEmployment_useNumOfDecisionPeriodEmployedAsFeature = useNumOfDecisionPeriodEmployedAsFeature;

            base.SetupDynamicEmployment(remainsOnOnceSwitchedOn);            
        }

        // update the delay
        public void UpdateDelay(long numOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn)
        {
            _numOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn = numOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn;
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
            if (base.Type == enumActionType.Default)
            {
                _epidemicTimeIndexToGoIntoEffect = long.MinValue;
                _epidemicTimeIndexToBeLifted = long.MaxValue;
            }
            else
            {
                _epidemicTimeIndexToGoIntoEffect = long.MaxValue;
                _epidemicTimeIndexToBeLifted = long.MaxValue;
            }

            _ifThisInterventionHasBeenEmployedBefore = false;
            _epidemicTimeIndexWhenThisInterventionTurnedOn = long.MaxValue;
            _epidemicTimeIndexWhenThisInterventionTurnedOff = long.MinValue;
        }
    }
}
