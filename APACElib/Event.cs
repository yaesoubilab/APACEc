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
        protected string _name;
        protected int _ID;
        protected int _IDOfActivatingIntervention;
        protected int _IDOfDestinationClass;
        protected int _IDOfRateParameter = - 1;  // -1 for transmission rate and >=0 for birth and epidemic indepedent events
        public int IDOfRateParameter { get=> _IDOfRateParameter; }  
        protected double _rate;

        public int MembersOutOverPastDeltaT { get; set; }

        public enum EumType
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
        { get { return -1; } }

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
            _IDOfRateParameter = IDOfRateParameter;
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
            _IDOfRateParameter = IDOfRateParameter;
        }

    }        
   
}
