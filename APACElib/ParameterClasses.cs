using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputationLib;
using RandomVariateLib;

namespace APACElib
{
    public class ParameterManager
    {
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();
        public double[] ParameterValues { get; private set; }
        public int NumParamsInCalibration { get; private set; } = 0;
        public bool ThereAreTimeDepParms { get; private set; } = false;
        public bool ThereAreTimeDepParms_susceptibilities { get; set; } = false;
        public bool ThereAreTimeDepParms_infectivities { get; set; } = false;
        public bool ThereAreTimeDepParms_splittingClasses { get; set; } = false;

        public ParameterManager()
        {

        }

        public void Add(Parameter thisParam)
        {
            Parameters.Add(thisParam);

            if (thisParam.ShouldBeUpdatedByTime)
                ThereAreTimeDepParms = true;
            if (thisParam.IncludedInCalibration)
                ++NumParamsInCalibration;
        }

        public List<Parameter> GetParameters(int[] parIDs)
        {
            List<Parameter> list = new List<Parameter>();
            foreach (int i in parIDs)
                list.Add(Parameters[i]);

            return list;
        }

        // get the value of parameters to calibrate
        public double[] GetValuesOfParametersToCalibrate()
        {
            double[] parValues = new double[1];
            int i = 0;
            foreach (Parameter thisParameter in Parameters.Where(p => p.IncludedInCalibration == true))
                parValues[i++] = thisParameter.Value;

            return parValues;
        }

        // update the effect of change in time dependent parameters
        public void UpdateTimeDepParams(RNG rng, double time, List<Class> classes)
        {
            if (ThereAreTimeDepParms)
            {
                // update time depedent parameters 
                foreach (Parameter thisParameter in Parameters.Where(p => p.ShouldBeUpdatedByTime))
                    ParameterValues[thisParameter.ID] = thisParameter.Sample(time, rng);
                
                // update value of splitting class parameters
                if (ThereAreTimeDepParms_splittingClasses)
                {
                    // update the probability of success
                    foreach (Class thisClass in classes)
                        thisClass.UpdateProbOfSuccess(ParameterValues);
                }
            }
        }

        // update susceptibility and infectivity of classes
        public void UpdateClassesSusceptInfect(int simTimeIndex, ref List<Class> classes)
        {
            // calculate only at the initialization or when there are time depedent susceptibility or infectivity parameters
            if (!(simTimeIndex == 0 || ThereAreTimeDepParms_susceptibilities || ThereAreTimeDepParms_infectivities))
                return;

            if (ThereAreTimeDepParms_susceptibilities || simTimeIndex == 0)
            {
                // only susceptibility
                foreach (Class thisClass in classes.Where(c => c.IsEpiDependentEventActive))
                    thisClass.UpdateSusceptibilityParams(ParameterValues);
            }
            if (ThereAreTimeDepParms_infectivities || simTimeIndex == 0)
            {
                // only infectivity
                foreach (Class thisClass in classes)
                    thisClass.UpdateInfectivityParams(ParameterValues);
            }
        }

        public void SampleAllParameters(RNG rng, double time)
        {
            // sample from parameters
            ParameterValues = new double[Parameters.Count];
            foreach (Parameter thisParameter in Parameters)
                ParameterValues[thisParameter.ID] = thisParameter.Sample(time, rng);
        }    
    }

    public class ForceOfInfectionModel
    {
        private ParameterManager _paramManager;
        private int _nOfPathogens;
        private double[][,] _baseContactMatrices = null;                    //[pathogen ID][group i, group j] 
        private int[][][,] _parID_percentageChangeInContactMatrices = null; //[intervention ID][pathogen ID][group i, group j]
        private double[][][,] _contactMatrices = null;                      //[intervention ID][pathogen ID][group i, group j]
        public int NumOfIntrvnAffectingContacts { get; private set; } = 0;
        private int[] _iOfIntrvsAffectingContacts;              // indeces of interventions affecting contacts
        private int[] _onOffStatusOfIntrvsAffectingContacts;    // 0 and 1 array
        
        public ForceOfInfectionModel(
            int nOfPathogens,
            ref ParameterManager paramManager
           )
        {
            _nOfPathogens = nOfPathogens;
            _paramManager = paramManager;
        }

        public void AddContactInfo(
            double[][,] baseContactMatrices,
            int[][][,] parID_percentageChangeInContactMatrices)
        {
            _baseContactMatrices = baseContactMatrices;
            _parID_percentageChangeInContactMatrices = parID_percentageChangeInContactMatrices;
        }

        public void AddIntrvnAffectingContacts(int interventionIndex)
        {
            SupportFunctions.AddToEndOfArray(ref _iOfIntrvsAffectingContacts, interventionIndex);
            ++NumOfIntrvnAffectingContacts;
        }

        public void Reset()
        {
            _onOffStatusOfIntrvsAffectingContacts = new int[NumOfIntrvnAffectingContacts];
        }

        // update transmission rates
        public void UpdateTransmissionRates(int simTimeIndex, int[] intvnsInEffect, ref List<Class> classes)
        {
            // update susceptibility and infectivity of classes
            _paramManager.UpdateClassesSusceptInfect(simTimeIndex, ref classes);

            // find the population size of each mixing group
            int[] popSizeOfMixingGroups = new int[_baseContactMatrices[0].GetLength(0)];
            foreach (Class thisClass in classes)
                popSizeOfMixingGroups[thisClass.RowIndexInContactMatrix] += thisClass.ClassStat.Prevalence;

            // find the index of current interventions in effect in the contact matrices
            int indexOfIntCombInContactMatrices = IndxInContactMatrices(intvnsInEffect);

            // calculate the transmission rates for each class
            double susContactInf = 0, rate = 0, infectivity = 0;
            double[] arrTransmissionRatesByPathogen = new double[_nOfPathogens];
            foreach (Class thisRecievingClass in classes.Where(c => c.IsEpiDependentEventActive && c.ClassStat.Prevalence > 0))
            {
                // calculate the transmission rate for each pathogen
                for (int pathogenID = 0; pathogenID < _nOfPathogens; pathogenID++)
                {
                    rate = 0;
                    for (int j = 0; j < classes.Count; j++)
                    {
                        // find the infectivity of infection-causing class
                        if (classes[j] is Class_Normal)
                        {
                            infectivity = classes[j].InfectivityValues[pathogenID];
                            if (infectivity > 0)
                            {
                                susContactInf = thisRecievingClass.SusceptibilityValues[pathogenID]
                                                * _contactMatrices[indexOfIntCombInContactMatrices][pathogenID][thisRecievingClass.RowIndexInContactMatrix, classes[j].RowIndexInContactMatrix]
                                                * infectivity;

                                rate += susContactInf * classes[j].ClassStat.Prevalence / popSizeOfMixingGroups[classes[j].RowIndexInContactMatrix];
                            }
                        }
                    }

                    arrTransmissionRatesByPathogen[pathogenID] = rate;
                }

                // update the transition rates of this class for all pathogens
                thisRecievingClass.UpdateTransmissionRates(arrTransmissionRatesByPathogen);
            }
        }

        // calculate contract matrices
        public void CalculateContactMatrices() //[intervention ID][pathogen ID][group i, group j]
        {
            int contactMatrixSize = _baseContactMatrices[0].GetLength(0);
            int sizeOfPowerSetOfIntrvAffectingContacts = (int)Math.Pow(2, NumOfIntrvnAffectingContacts);
            _contactMatrices = new double[sizeOfPowerSetOfIntrvAffectingContacts][][,];

            // build the contact matrices
            for (int intCombIndex = 0; intCombIndex < sizeOfPowerSetOfIntrvAffectingContacts; ++intCombIndex)
            {
                if (intCombIndex == 0)
                    _contactMatrices[intCombIndex] = _baseContactMatrices;
                else
                {
                    int[] onOfStatusOfIntrvnAffectingContacts 
                        = SupportFunctions.ConvertToBase2FromBase10(intCombIndex, NumOfIntrvnAffectingContacts);
                    for (int intrvn = 0; intrvn < NumOfIntrvnAffectingContacts; ++intrvn)
                    {
                        // initialize contact matrices
                        _contactMatrices[intCombIndex] = new double[_nOfPathogens][,];

                        if (onOfStatusOfIntrvnAffectingContacts[intrvn] == 1)
                        {
                            for (int pathogenID = 0; pathogenID < _nOfPathogens; pathogenID++)
                            {
                                _contactMatrices[intCombIndex][pathogenID] 
                                    = new double[contactMatrixSize, contactMatrixSize];

                                for (int i = 0; i < contactMatrixSize; ++i)
                                    for (int j = 0; j < contactMatrixSize; ++j)
                                        _contactMatrices[intCombIndex][pathogenID][i, j] 
                                            = _baseContactMatrices[pathogenID][i, j] +
                                            _baseContactMatrices[pathogenID][i, j] * _paramManager.ParameterValues
                                                [_parID_percentageChangeInContactMatrices[intrvn][pathogenID][i, j]];
                            }
                        }
                    }
                }
            }
        }

        // find the index of this intervention combination in contact matrices
        private int IndxInContactMatrices(int[] interventionCombination)
        {
            for (int i = 0; i < NumOfIntrvnAffectingContacts; i++)
            {
                _onOffStatusOfIntrvsAffectingContacts[i] = interventionCombination[_iOfIntrvsAffectingContacts[i]];
            }
            return SupportFunctions.ConvertToBase10FromBase2(_onOffStatusOfIntrvsAffectingContacts);
        }

        public void Clean()
        {
            _baseContactMatrices = null;
            _parID_percentageChangeInContactMatrices = null;
            _contactMatrices = null;
            _iOfIntrvsAffectingContacts = null;
            _onOffStatusOfIntrvsAffectingContacts = null;
        }
    }
}
