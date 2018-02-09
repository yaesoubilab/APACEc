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
    public abstract class Process
    {
        protected string _name;
        protected int _ID;
        protected int _IDOfActivatingIntervention;
        protected int _IDOfDestinationClass;
        protected double _rate;
        protected enumType _type;
        protected long _membersOutOverPastDeltaT; 

        public enum enumType
        {
            Birth = 1,
            EpidemicDepedent = 2,
            EpidemicIndepedent = 3,
        }

        // Instantiation
        public Process(string name, int ID, int IDOfActivatingIntervention, int IDOfDestinationClass)
        {            
            _name = name;
            _ID = ID;
            _IDOfActivatingIntervention = IDOfActivatingIntervention;
            _IDOfDestinationClass = IDOfDestinationClass;
        }
        
        // clone
        public virtual object Clone()
        {
            return null;
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
        public long MembersOutOverPastDeltaT
        {
            get { return _membersOutOverPastDeltaT; }
            set { _membersOutOverPastDeltaT = value; }
        }
        public double Rate
        {
            get { return _rate; }
        }
        public virtual int IDOfPathogenToGenerate
        { get { return 0; } }
        public virtual enumType Type
        { get { return enumType.EpidemicIndepedent; } }

        // update transmission rate
        public virtual void UpdateTransmissionRate(double value)
        {
        }
               
    }

    public class Process_Birth : Process
    {
        // Fields        
        private int _IDOfRateParameter;

        // Instantiation
        public Process_Birth(string name, int ID, int IDOfActivatingIntervention, int IDOfRateParameter, int IDOfDestinationClass)
            : base(name, ID, IDOfActivatingIntervention,IDOfDestinationClass)
        {
            _IDOfRateParameter = IDOfRateParameter;
        }

        // Properties
        public int IDOfRateParameter
        { get { return _IDOfRateParameter; } }
        public override enumType Type
        { get { return enumType.Birth; } }

        // Clone
        public override object Clone()
        {
            // create a clone of this process
            Process_Birth clone = new Process_Birth(_name, _ID, _IDOfActivatingIntervention, _IDOfRateParameter, _IDOfDestinationClass);            
            return clone;
        }
        // update the birth rate
        public void UpdateBirthRate(double value)
        {
            _rate = value;
        }
    }

    public class Process_EpidemicDependent : Process
    {
        // Fields
        private int _IDOfPathogenToGenerate;
        //private double[] _arrTransmissionRatesAffectingthisClass;        
        //private double[] _arrProportionOfMembersInEachClass;

        // Instantiation
        public Process_EpidemicDependent(string name, int ID, int IDOfActivatingIntervention, int IDOfPathogenToGenerate, int IDOfDestinationClass)
            : base(name, ID, IDOfActivatingIntervention, IDOfDestinationClass)
        {
            _IDOfPathogenToGenerate = IDOfPathogenToGenerate;
        }

        // Properties
        public override int IDOfPathogenToGenerate
        { get { return _IDOfPathogenToGenerate; } }
        public override enumType Type
        { get { return enumType.EpidemicDepedent; } }

        // Clone
        public override object Clone()
        {
            // create a clone of this process
            Process_EpidemicDependent clone = new Process_EpidemicDependent(_name, _ID, _IDOfActivatingIntervention, _IDOfPathogenToGenerate, _IDOfDestinationClass);                    
            return clone;
        }

        // Methods

        // update transmission rate
        public override void UpdateTransmissionRate(double value)
        {
            _rate = value;
        }

        //// update the transmission rates of other classes affecting this class
        //public override void UpdateTransmissionRatesToGenerateInfections(double[] arrTransmissionRates)
        //{
        //    _arrTransmissionRatesAffectingthisClass = (double[])arrTransmissionRates.Clone();          
        //}        
        //// update the proportion of members in each class
        //public override void UpdateProportionOfMembersInEachClass(double[] arrMembersInEachClass, long currentPopulationSize)
        //{
        //    _arrProportionOfMembersInEachClass = arrProportionOfMembersInEachClass;
        //    // update rate accordingly
        //    _rate = 0;
        //    for (int i = 0; i < _arrProportionOfMembersInEachClass.Length; ++i)
        //        _rate += _arrTransmissionRatesAffectingthisClass[i] * _arrProportionOfMembersInEachClass[i];
        //}                
    } // end of Process_EpidemicDependent class

    public class Process_EpidemicIndependent : Process
    {
        // Fields        
        private int _IDOfRateParameter;

        // Instantiation
        public Process_EpidemicIndependent(string name, int ID, int IDOfActivatingIntervention, int IDOfRateParameter, int IDOfDestinationClass)
            : base(name, ID, IDOfActivatingIntervention, IDOfDestinationClass)
        {
            _IDOfRateParameter = IDOfRateParameter;
        }

        // Properties
        public int IDOfRateParameter
        { get { return _IDOfRateParameter; } }
        public override enumType Type
        { get { return enumType.EpidemicIndepedent; } }
        
        // Clone
        public override object Clone()
        {
            // create a clone of this process
            Process_EpidemicIndependent clone = new Process_EpidemicIndependent(_name, _ID, _IDOfActivatingIntervention, _IDOfRateParameter, _IDOfDestinationClass);
            return clone;
        }
        // update rate
        public void UpdateRate(double value)
        {
            _rate = value;
        }

    } // end of Process_EpidemicIndependent class        

    //public class Process_NoStayTime : Process
    //{
    //    public Process_NoStayTime(string name, int ID)
    //        : base(name, ID)
    //    {
    //    }
    //}
   
}
