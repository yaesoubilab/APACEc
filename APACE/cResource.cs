using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomVariateLib;

namespace APACE_lib
{
    public class Resource
    {
        public enum enumReplenishmentType
        {
            OneTime = 1,
            Periodic = 2,
        }

        private int _ID;
        private string _name;
        private double _pricePerUnit;
        private long _currentUnitsAvailable;
        private bool _showAvailability;

        private enumReplenishmentType _replenishmentType; // one-time or periodic
        private double _firstTimeAvailable;
        private double _nextReplenishmentTime;
        private long _replenishmentQuantity;
        private double _replenishmentInterval;
        private int _parID_firstTimeAvailable;
        private int _parID_replenishmentQuantity;
        private int _parID_replenishmentInterval;

        private bool _everReplenished;        

        // Instantiation
        public Resource(int ID, string name, double pricePerUnit)
        {
            _ID = ID;
            _name = name;
            _pricePerUnit = pricePerUnit;
            _currentUnitsAvailable = 0;
            _everReplenished = false;
        }

        // Properties
        #region Properties        
        public int ID
        {
            get{return _ID;}
        }
        public string Name
        {
            get{return _name;}
        }
        public double PricePerUnit
        {
            get{return _pricePerUnit;}
        }
        public long CurrentUnitsAvailable
        {
            get {return _currentUnitsAvailable;}
            set { _currentUnitsAvailable = value; }
        }
        public enumReplenishmentType ReplenishmentType
        { 
            get { return _replenishmentType; } 
        }
        public int ParID_firstTimeAvailable
        {
            get { return _parID_firstTimeAvailable; }
        }
        public int ParID_replenishmentQuantity
        {
            get { return _parID_replenishmentQuantity; }
        }
        public int ParID_replenishmentInterval
        {
            get { return _parID_replenishmentInterval; }
        }
        public bool ShowAvailability
        {
            get { return _showAvailability; }
            set { _showAvailability = value; }
        }
        public bool EverReplenished
        {
            get { return _everReplenished; }
        }
        #endregion
        
        // Methods
        // setup availability of this resource
        public void SetupAvailability(int parID_firstTimeAvailable, int parID_replenishmentQuantity)
        {
            _replenishmentType = enumReplenishmentType.OneTime;
            _parID_firstTimeAvailable = parID_firstTimeAvailable;
            _parID_replenishmentQuantity = parID_replenishmentQuantity;
        }
        public void SetupAvailability(int parID_firstTimeAvailable, int parID_replenishmentQuantity, int parID_replenishmentInterval)
        {
            _replenishmentType = enumReplenishmentType.Periodic;
            _parID_firstTimeAvailable = parID_firstTimeAvailable;
            _parID_replenishmentQuantity = parID_replenishmentQuantity;
            _parID_replenishmentInterval = parID_replenishmentInterval;
        }

        // update the availability schedule
        public void UpdateAvailabilityScheme(double firstTimeAvailable, long replenishmentQuantity)
        {
            _firstTimeAvailable = firstTimeAvailable;
            _replenishmentQuantity = replenishmentQuantity;            
        }
        public void UpdateAvailabilityScheme(double firstTimeAvailable, long replenishmentQuantity, double replenishmentInterval)
        {
            _firstTimeAvailable = firstTimeAvailable;
            _replenishmentQuantity = replenishmentQuantity;
            _replenishmentInterval = replenishmentInterval;
        }

        // reset for new simulation run
        public void ResetForANewSimulationIteration()
        {
            _currentUnitsAvailable = 0;
            _everReplenished = false;
            if (_replenishmentType == enumReplenishmentType.Periodic)
                _nextReplenishmentTime = _firstTimeAvailable;
        }
        // replenish if available
        public void ReplenishIfAvailable(double currentTime)
        {
            switch (_replenishmentType)
                {
                    case enumReplenishmentType.OneTime:
                        {
                            if (_everReplenished == false && currentTime >= _firstTimeAvailable)
                            {
                                _currentUnitsAvailable = _replenishmentQuantity;
                                _everReplenished = true;
                            }
                        }
                        break;
                    case enumReplenishmentType.Periodic:
                        {
                            if (currentTime >= _nextReplenishmentTime)
                            {
                                _currentUnitsAvailable += _replenishmentQuantity;
                                _nextReplenishmentTime += _replenishmentInterval;
                                _everReplenished = true;
                            }
                        }
                        break;
                }            
        }        
    }
}
