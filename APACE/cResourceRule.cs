using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APACE_lib
{
    public class ResourceRule
    {
        public enum enumUnavailabilityRule
        {
            SendToAnotherClass = 1,
            SendToAnotherProcess = 2,
        }

        private int _ID;
        private int _associatedResourceID;
        private string _name;
        private int _consumptionPerUnitOfTime;
        private int _consumptionPerArrival;
        private enumUnavailabilityRule _unavailabilityRule;
        private int _ifSatifiedClassID;
        private int _ifNotSatifiedClassID;
        private int _ifSatifiedProcessID;
        private int _ifNotSatifiedProcessID;

        // Instantiation
        public ResourceRule(int ID, string resourceRuleName, int associatedResourceID, int consumptionPerArrival, int consumptionPerUnitOfTime)
        {
            _ID = ID;
            _associatedResourceID = associatedResourceID;
            _name = resourceRuleName;
            _consumptionPerUnitOfTime = consumptionPerUnitOfTime;
            _consumptionPerArrival = consumptionPerArrival;
        }
        // Clone
        public ResourceRule Clone()
        {
            // create a clone of this resource rule
            ResourceRule clone = new ResourceRule(_ID, _name, _associatedResourceID, _consumptionPerArrival, _consumptionPerUnitOfTime);
            switch (_unavailabilityRule)
            {
                case enumUnavailabilityRule.SendToAnotherClass:
                    {
                        clone.SetupUnavailabilityRuleSendToAnotherClass(_ifSatifiedClassID, _ifNotSatifiedClassID);
                    }
                    break;
                case enumUnavailabilityRule.SendToAnotherProcess:
                    {
                        clone.SetupUnavailabilityRuleSendToAnotherProcess(_ifSatifiedProcessID, _ifNotSatifiedProcessID);
                    }
                    break;
            }
            return clone;
        }

        // Properties
        public int ID
        {
            get { return _ID; }
        }
        public int AssociatedResourceID
        {
            get { return _associatedResourceID; }
        }
        public int ConsumptionPerUnitOfTime
        {
            get{return _consumptionPerUnitOfTime;}
        }
        public int ConsumptionPerArrival
        {
            get{return _consumptionPerArrival;}
        }
        public int IfSatisfiedClassID
        {
            get { return _ifSatifiedClassID; }
        }        
        public int IfNotSatisfiedClassID
        {
            get { return _ifNotSatifiedClassID; }
        }

        public void SetupUnavailabilityRuleSendToAnotherClass(int ifSatifiedClassID, int ifNotSatifiedClassID)
        {
            _unavailabilityRule = enumUnavailabilityRule.SendToAnotherClass;
            _ifSatifiedClassID = ifSatifiedClassID;
            _ifNotSatifiedClassID = ifNotSatifiedClassID;
        }
        public void SetupUnavailabilityRuleSendToAnotherProcess(int ifSatifiedProcessID, int ifNotSatifiedProcessID)
        {
            _unavailabilityRule = enumUnavailabilityRule.SendToAnotherProcess;
            _ifSatifiedProcessID = ifSatifiedProcessID;
            _ifNotSatifiedProcessID = ifNotSatifiedProcessID;
        }


    }
}
