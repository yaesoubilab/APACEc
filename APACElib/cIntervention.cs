using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomVariateLib;
using ComputationLib;
using SimulationLib;

namespace APACElib
{
    public enum EnumInterventionType : int
    {
        Default = 1,    // represents the "no action" alternative that is always on
        Additive = 2,   // represents actions that could be added to the "default" interventions
    }

    // Intervention
    public class Intervention
    {
        public static EnumInterventionType ConvertToActionType(string value)
        {
            EnumInterventionType actionType = EnumInterventionType.Default;

            switch (value)
            {
                case "Default":
                    actionType = EnumInterventionType.Default;
                    break;
                case "Additive":
                    actionType = EnumInterventionType.Additive;
                    break;
            }
            return actionType;
        }

        public int Index { get; set; }   // 0, 1, 2, ...
        public string Name { get; set; }
        public EnumInterventionType Type { get; set; }  // default or additive
        public DecisionRule DecisionRule { get; set; }   // pointer to the decision rule that guides the employment of this action

        // costs
        public double FixedCost { get; set; }          // fixed cost to switch on
        public double CostPerDecisionPeriod { get; set; }  // cost of using during a decision period
        public double PenaltyForSwitchingFromOnToOff { get; set; }

        // availability
        public long TIndexBecomesAvailable { get; set; }
        public long TIndexBecomesUnavailable { get; set; }
        public bool RemainOnOnceTurnedOn { get; set; }

        // usage statistics
        public bool IfHasBeenTrunedOnBefore { get; set; } = false;
        public int NumOfSwitchesOccured { get; set; }
        public int NumOfDecisionPeriodsOverWhichThisInterventionWasUsed { get; set; }

        public bool IfAffectingContactPattern { get; set; }
        
        // availability
        public int ParIDDelayToGoIntoEffectOnceTurnedOn { get; set; }
        public int NumOfTimeIndeciesDelayedToGoIntoEffectOnceTurnedOn { get; set; } = 0;

        public int EpiTimeIndexToTurnOn { get; set; }
        public int EpiTimeIndexToGoIntoEffect { get; set; }
        public int EpiTimeIndexToTurnOff { get; set; }
        public int EpiTimeIndexTurnedOn { get; set; }
        public int EpiTimeIndexTurnedOff { get; set; }
        
        // Instantiation
        public Intervention(
            int index, 
            string name, 
            EnumInterventionType actionType, 
            bool affectingContactPattern,
            int timeIndexBecomesAvailable,
            int timeIndexBecomesUnavailable,
            int parIDDelayToGoIntoEffectOnceTurnedOn,
            ref DecisionRule decisionRule)
               
        {
            Index = index;
            Name = name;
            Type = actionType;
            TIndexBecomesAvailable = timeIndexBecomesAvailable;
            TIndexBecomesUnavailable = timeIndexBecomesUnavailable;
            DecisionRule = decisionRule;
            IfAffectingContactPattern = affectingContactPattern;
            ParIDDelayToGoIntoEffectOnceTurnedOn = parIDDelayToGoIntoEffectOnceTurnedOn;
        }

        // set up cost
        public void SetUpCost(double fixedCost, double costPerDecisionPeriod, double penaltyForSwitchingFromOnToOff)
        {
            FixedCost = fixedCost;
            CostPerDecisionPeriod = costPerDecisionPeriod;
            PenaltyForSwitchingFromOnToOff = penaltyForSwitchingFromOnToOff;
        }

        // find when should be turned off
        public int FindEpiTimeIndexToTurnOff(int epiTimeIndex)
        {
            return int.MaxValue;
        }

        // find the switch status
        public int FindSwitchStatus(int epiTimeIndex)
        {
            // defualt intervention is always on
            if (Type == EnumInterventionType.Default)
                return 1;

            // check if the intervention is available at this time index
            if (epiTimeIndex < TIndexBecomesAvailable || epiTimeIndex >= TIndexBecomesUnavailable)
                return 0;
            else if (RemainOnOnceTurnedOn && IfHasBeenTrunedOnBefore)
                return 1;
            else
                return DecisionRule.GetSwitchStatus(epiTimeIndex);
        }

        // reset for another simulation run
        public void ResetForAnotherSimulationRun()
        {
            IfHasBeenTrunedOnBefore = false;
            NumOfSwitchesOccured = 0;
            NumOfDecisionPeriodsOverWhichThisInterventionWasUsed = 0;

            // find the time to go into effect
            if (Type == EnumInterventionType.Default)
            {
                EpiTimeIndexToTurnOn = int.MinValue;
                EpiTimeIndexTurnedOn = int.MinValue;
                EpiTimeIndexToGoIntoEffect = int.MinValue;
                EpiTimeIndexToTurnOff = int.MaxValue;
            }
            else
            {
                EpiTimeIndexToTurnOn = int.MaxValue;
                EpiTimeIndexTurnedOn = int.MaxValue;
                EpiTimeIndexToGoIntoEffect = int.MaxValue;
                EpiTimeIndexToTurnOff = int.MaxValue;
            }
        }
        
    }
}
