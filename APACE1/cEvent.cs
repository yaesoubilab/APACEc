using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomVariateLib;
using SimulationLib;
using ComputationLib;

namespace APACE_lib
{
    // Process
    public abstract class Event
    {
        protected string _name;
        protected int _ID;
        protected int _IDOfActivatingIntervention;
        protected int _IDOfDestinationClass;
        protected int _IDOfRateParameter;   // for birth and epidemic indepedent events
        protected double _rate;

        public int MembersOutOverPastDeltaT { get; set; }

        public enum enumType
        {
            Birth = 1,
            EpidemicDepedent = 2,
            EpidemicIndepedent = 3,
        }

        // Instantiation
        public Event(string name, int ID, int IDOfActivatingIntervention, int IDOfDestinationClass)
        {            
            _name = name;
            _ID = ID;
            _IDOfActivatingIntervention = IDOfActivatingIntervention;
            _IDOfDestinationClass = IDOfDestinationClass;
        }
        
        // Properties   
        public int ID
        {
            get{return _ID;}
        }
        public int IDOfActivatingIntervention
        {
            get { return _IDOfActivatingIntervention; }
        }
        public int IDOfDestinationClass
        {
            get { return _IDOfDestinationClass; }
        }
        public double Rate
        {
            get { return _rate; }
        }
        public virtual int IDOfPathogenToGenerate
        { get { return 0; } }

        // update transmission rate
        public virtual void UpdateTransmissionRate(double value)
        {
        }
               
    }

    public class Event_Birth : Event
    {
        // Instantiation
        public Event_Birth(string name, int ID, int IDOfActivatingIntervention, int IDOfRateParameter, int IDOfDestinationClass)
            : base(name, ID, IDOfActivatingIntervention,IDOfDestinationClass)
        {
            _IDOfRateParameter = IDOfRateParameter;
        }

        // Properties
        public int IDOfRateParameter
        { get { return _IDOfRateParameter; } }

        // update the birth rate
        public void UpdateBirthRate(double value)
        {
            _rate = value;
        }
    }

    public class Event_EpidemicDependent : Event
    {
        // Fields
        private int _IDOfPathogenToGenerate;

        // Instantiation
        public Event_EpidemicDependent(string name, int ID, int IDOfActivatingIntervention, int IDOfPathogenToGenerate, int IDOfDestinationClass)
            : base(name, ID, IDOfActivatingIntervention, IDOfDestinationClass)
        {
            _IDOfPathogenToGenerate = IDOfPathogenToGenerate;
        }

        // Properties
        public override int IDOfPathogenToGenerate
        { get { return _IDOfPathogenToGenerate; } }

        // update transmission rate
        public override void UpdateTransmissionRate(double value)
        {
            _rate = value;
        }
              
    } // end of Process_EpidemicDependent class

    public class Event_EpidemicIndependent : Event
    {
        // Instantiation
        public Event_EpidemicIndependent(string name, int ID, int IDOfActivatingIntervention, int IDOfRateParameter, int IDOfDestinationClass)
            : base(name, ID, IDOfActivatingIntervention, IDOfDestinationClass)
        {
            _IDOfRateParameter = IDOfRateParameter;
        }

        // Properties
        public int IDOfRateParameter
        { get { return _IDOfRateParameter; } }
        // update rate
        public void UpdateRate(double value)
        {
            _rate = value;
        }

    } // end of Process_EpidemicIndependent class        
   
}
