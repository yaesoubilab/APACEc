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

        public int Index { get; private set; }   // 0, 1, 2, ...
        public string Name { get; private set; }
        public EnumInterventionType Type { get; private set; }  // default or additive
        public DecisionRule DecisionRule { get; private set; }   // pointer to the decision rule that guides the employment of this action
        public bool IfAffectingContactPattern { get; set; }

        // costs
        public double FixedCost { get; private set; }          // fixed cost to switch on
        public double CostPerDecisionPeriod { get; private set; }  // cost of using during a decision period
        public double PenaltyForSwitchingFromOnToOff { get; private set; }

        // availability
        public long TIndexBecomesAvailable { get; private set; }
        public long TIndexBecomesUnavailable { get; private set; }
        public bool RemainOnOnceTurnedOn { get; private set; }

        // usage statistics
        public bool IfEverTurnedOnBefore { get; set; } = false;
        public bool IfEverTurnedOffBefore { get; set; } = false;
        public int NumOfSwitchesOccured { get; set; }
        public int NumOfDecisionPeriodsUsedOver { get; set; }        
        
        // availability
        public int ParIDDelayToGoIntoEffectOnceTurnedOn { get; set; }
        public int NumOfTimeIndeciesDelayedToGoIntoEffect { get; set; } = 0; // delay after turned on

        public int EpiTimeIndexToTurnOn { get; set; }
        public int EpiTimeIndexToGoIntoEffect { get; set; }
        public int EpiTimeIndexToTurnOff { get; set; }
        public int EpiTimeIndexLastTurnedOn { get; set; }
        public int EpiTimeIndexLastTurnedOff { get; set; }
        
        // Instantiation
        public Intervention(
            int index, 
            string name, 
            EnumInterventionType actionType, 
            bool affectingContactPattern,
            int timeIndexBecomesAvailable,
            int timeIndexBecomesUnavailable,
            int parIDDelayToGoIntoEffectOnceTurnedOn,
            DecisionRule decisionRule)
               
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
        public int FindSwitchStatus(int currentSwitchStatus, int epiTimeIndex)
        {
            // defualt intervention is always on
            if (Type == EnumInterventionType.Default)
                return 1;

            // check if the intervention is available at this time index
            if (epiTimeIndex < TIndexBecomesAvailable || epiTimeIndex >= TIndexBecomesUnavailable)
                return 0;
            else if (RemainOnOnceTurnedOn && IfEverTurnedOnBefore)
                return 1;
            else
                return DecisionRule.GetSwitchStatus(currentSwitchStatus, epiTimeIndex);
        }

        // reset for another simulation run
        public void ResetForAnotherSimulationRun()
        {
            IfEverTurnedOnBefore = false;
            NumOfSwitchesOccured = 0;
            NumOfDecisionPeriodsUsedOver = 0;

            EpiTimeIndexToTurnOn = int.MaxValue;
            EpiTimeIndexToTurnOff = int.MaxValue;
            EpiTimeIndexToGoIntoEffect = int.MaxValue;
            EpiTimeIndexLastTurnedOn = int.MaxValue;
            EpiTimeIndexLastTurnedOff = int.MaxValue;

            //// find the time to go into effect
            //if (Type == EnumInterventionType.Default)
            //{
            //    EpiTimeIndexToTurnOn = int.MinValue;
            //    EpiTimeIndexLastTurnedOn = int.MinValue;
            //    EpiTimeIndexToGoIntoEffect = int.MinValue;
            //    EpiTimeIndexToTurnOff = int.MaxValue;
            //}
            //else
            //{
                
            //}
        }
        
    }
}
