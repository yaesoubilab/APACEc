using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomNumberGeneratorLib;
using ComputationLib;
using OptimizationLib;

namespace APACE_lib
{
    // Intervention
    public class Intervention : SimulationLib.SimulationAction
    {
        public enum enumEpidemiologicalObservation
        {
            OverPastObservationPeriod = 1,
            Accumulating = 2
        }

        // Fields                       
        bool _affectingContactPattern = false;
        // availability
        private int _parIDDelayToGoIntoEffectOnceTurnedOn = 0;
        private long _numOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn = 0;
        private long _epidemicTimeIndexToGoIntoEffect = 0;
        private long _epidemicTimeIndexToBeLifted = 0;
        // employment
        private bool _ifThisInterventionHasBeenEmployedBefore = false;
        private long _epidemicTimeIndexWhenThisInterventionTurnedOn = 0;
        private long _epidemicTimeIndexWhenThisInterventionTurnedOff = 0;
        // periodic employment
        private int _periodicEmployment_Frequency_NumOfDcisionPeriods = 0;
        private int _periodicEmployment_Duration_NumOfDcisionPeriods = 0;
        // threshold-base employment          
        private int _thresholdBasedEmployment_IDOfTheSpecialStatisticsToObserveAccumulation = 0;
        private enumEpidemiologicalObservation _thresholdBasedEmployment_EpidemiologicalObservation = enumEpidemiologicalObservation.OverPastObservationPeriod;
        private double _thresholdBasedEmployment_thresholdToTriggerThisIntervention = 0;
        private long _thresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention = 0;        
        // interval-based employment
        private double _intervalBaseEmployment_availableUntilThisTime;
        private int _intervalBaseEmployment_minNumOfDecisionPeriodsToUse;        
        // dynamic employment
        //bool _dynamicEmployment_selectOnOffStatusAsFeature;
        //int _dynamicEmployment_previousObservationPeriodToObserveOnOffValue;
        //bool _dynamicEmployment_useNumOfDecisionPeriodEmployedAsFeature;

        // Instantiation
        public Intervention(int index, int ID, string name, enumActionType type, bool affectingContactPattern)
            : base (index, ID, name, type)
        {
            _affectingContactPattern = affectingContactPattern;     
            base.OnOffSwitchSetting = enumOnOffSwitchSetting.Predetermined;
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
        public long NumOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn
        {
            get { return _numOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn; }
        }
        public long EpidemicTimeIndexToGoIntoEffect
        {
            get { return _epidemicTimeIndexToGoIntoEffect; }
            set { _epidemicTimeIndexToGoIntoEffect = value; }
        }
        public long EpidemicTimeIndexToBeLifted
        {
            get { return _epidemicTimeIndexToBeLifted; }
            set { _epidemicTimeIndexToBeLifted = value; }
        }
        // employment
        public bool IfThisInterventionHasBeenEmployedBefore
        {
            get { return _ifThisInterventionHasBeenEmployedBefore; }
            set { _ifThisInterventionHasBeenEmployedBefore = value; }
        }
        public long EpidemicTimeIndexWhenThisInterventionTurnedOn
        {
            get { return _epidemicTimeIndexWhenThisInterventionTurnedOn; }
            set { _epidemicTimeIndexWhenThisInterventionTurnedOn = value; }
        }
        public long EpidemicTimeIndexWhenThisInterventionTurnedOff
        {
            get { return _epidemicTimeIndexWhenThisInterventionTurnedOff; }
            set { _epidemicTimeIndexWhenThisInterventionTurnedOff = value; }
        }
        // periodic employment
        public int PeriodicEmployment_Frequency_NumOfDcisionPeriods
        {
            get{return _periodicEmployment_Frequency_NumOfDcisionPeriods;}
        }
        public int PeriodicEmployment_Duration_NumOfDcisionPeriods
        {
            get { return _periodicEmployment_Duration_NumOfDcisionPeriods; }
        }
        // threshold-based employment       
        public int ThresholdBasedEmployment_IDOfTheSpecialStatisticsToObserveAccumulation
        {
            get { return _thresholdBasedEmployment_IDOfTheSpecialStatisticsToObserveAccumulation; }
        }
        public enumEpidemiologicalObservation ThresholdBasedEmployment_EpidemiologicalObservation
        {
            get { return _thresholdBasedEmployment_EpidemiologicalObservation; }
        }
        public double ThresholdBasedEmployment_thresholdToTriggerThisIntervention 
        { 
            get { return _thresholdBasedEmployment_thresholdToTriggerThisIntervention; } 
        }
        public long ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention
        { 
            get { return _thresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention; } 
        }
        // interval-based employment
        public double IntervalBaseEmployment_availableUntilThisTime
        {
            get { return _intervalBaseEmployment_availableUntilThisTime; }
        }
        public int IntervalBaseEmployment_minNumOfDecisionPeriodsToUse
        {
            get { return _intervalBaseEmployment_minNumOfDecisionPeriodsToUse; }
        }
        
        // feature
        //public bool DynamicEmployment_selectOnOffStatusAsFeature
        //{
        //    get { return _dynamicEmployment_selectOnOffStatusAsFeature; }
        //}
        //public int DynamicEmployment_previousObservationPeriodToObserveOnOffValue
        //{
        //    get { return _dynamicEmployment_previousObservationPeriodToObserveOnOffValue; }
        //}
        //public bool DynamicEmployment_useNumOfDecisionPeriodEmployedAsFeature
        //{
        //    get { return _dynamicEmployment_useNumOfDecisionPeriodEmployedAsFeature; }
        //}
        #endregion
        // Procedures
        // set up availability
        public void SetUpAvailability(enumOnOffSwitchSetting onOffSwitchSetting, long TimeIndexBecomeAvailable, long TimeIndexBecomeUnavailable, int parIDDelayToGoIntoEffectOnceTurnedOn)
        {
            base.SetupAvailability(onOffSwitchSetting, TimeIndexBecomeAvailable, TimeIndexBecomeUnavailable);
            _parIDDelayToGoIntoEffectOnceTurnedOn = parIDDelayToGoIntoEffectOnceTurnedOn;
        }
        // set up costs
        public void SetUpCosts(double fixedCost, double costPerUnitOfTime, double penaltyForSwitchingFromOnToOff)
        { 
            base.SetUpCost(fixedCost, costPerUnitOfTime, penaltyForSwitchingFromOnToOff);
        }
        // set up resource requirement
        public void SetUpResourceRequirement(int IDOfTheResourceRequiredToBeAvailable)
        {
            base.SetupResourceRequirement(IDOfTheResourceRequiredToBeAvailable);
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
                enumEpidemiologicalObservation epidemiologicalObservation,
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


    public abstract class DecisionRule
    {
        public DecisionRule()
        {
        }
    }

    public class Predetermined : DecisionRule
    {
    
    
    }
}
