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
        public int IDOfRateParameter { get; protected set; } = -1;  // -1 for transmission rate and >=0 for birth and epidemic indepedent events

        protected double _rate;

        public double Rate { get => _rate; }
        public virtual int IDOfPathogenToGenerate { get { return -1; } }
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

        // update birth, transmission or other  rates
        public void UpdateRate(double value)
        {
            _rate = value;
        }
    }

    public class Event_Birth : Event
    {
        // Instantiation
        public Event_Birth(
            string name, 
            int ID, 
            int IDOfActivatingIntervention, 
            int IDOfRateParameter, 
            int IDOfDestinationClass)
            : base(name, ID, IDOfActivatingIntervention,IDOfDestinationClass)
        {
            this.IDOfRateParameter = IDOfRateParameter;
        }
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

    } 

    public class Event_EpidemicIndependent : Event
    {
        // Instantiation
        public Event_EpidemicIndependent(
            string name, 
            int ID, 
            int IDOfActivatingIntervention, 
            int IDOfRateParameter, 
            int IDOfDestinationClass)
            : base(name, ID, IDOfActivatingIntervention, IDOfDestinationClass)
        {
            this.IDOfRateParameter = IDOfRateParameter;
        }
    }        
   
}
