using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomVariateLib;
using SimulationLib;
using ComputationLib;

namespace APACElib
{
    // Process
    public abstract class Event
    {
        public string Name { get; }
        public int ID { get; }
        public int IDOfActivatingIntervention { get; }
        public int IDOfDestinationClass { get; }
        protected Parameter _rateParam = null; // null for transmission rate and not null for birth and epidemic indepedent events
        protected double _rate;

        public virtual double Rate => 0;
        public virtual int IDOfPathogenToGenerate => -1;
        public int MembersOutOverPastDeltaT { get; set; }
        public enum EumType
        {
            Birth = 1,
            EpidemicDepedent = 2,
            EpidemicIndepedent = 3,
        }

        // Instantiation
        public Event(string name, int id, int idOfActivatingIntervention, int idOfDestinationClass)
        {            
            Name = name;
            ID = id;
            IDOfActivatingIntervention = idOfActivatingIntervention;
            IDOfDestinationClass = idOfDestinationClass;
        }

        // update transmission
        public virtual void UpdateRate(double value){}
    }

    public class Event_Birth : Event
    {
        // Instantiation
        public Event_Birth(
            string name, 
            int ID, 
            int IDOfActivatingIntervention, 
            Parameter rateParameter, 
            int IDOfDestinationClass)
            : base(name, ID, IDOfActivatingIntervention,IDOfDestinationClass)
        {
            _rateParam = rateParameter;
        }

        public override double Rate => _rateParam.Value;
    }

    public class Event_EpidemicDependent : Event
    {
        private int _IDOfPathogenToGenerate;

        public Event_EpidemicDependent(
            string name, 
            int ID, int 
            IDOfActivatingIntervention, 
            int IDOfPathogenToGenerate, int 
            IDOfDestinationClass)
            : base(name, ID, IDOfActivatingIntervention, IDOfDestinationClass)
        {
            _IDOfPathogenToGenerate = IDOfPathogenToGenerate;
        }

        // Properties
        public override int IDOfPathogenToGenerate
        { get { return _IDOfPathogenToGenerate; } }

        public override double Rate => _rate;
        public override void UpdateRate(double value)
        {
            _rate = value;
        }

    } 

    public class Event_EpidemicIndependent : Event
    {
        // Instantiation
        public Event_EpidemicIndependent(
            string name, 
            int ID, 
            int IDOfActivatingIntervention, 
            Parameter rateParameter, 
            int IDOfDestinationClass)
            : base(name, ID, IDOfActivatingIntervention, IDOfDestinationClass)
        {
            _rateParam = rateParameter;
        }

        public override double Rate => _rateParam.Value;
    }   
}
