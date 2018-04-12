using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputationLib;
using SimulationLib;
using RandomVariateLib;

namespace APACElib
{
    class ParameterManager
    {
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();

        // parameters
        private bool _thereAreTimeDependentParameters = false;
        private bool _thereAreTimeDependentParameters_affectingSusceptibilities = false;
        private bool _thereAreTimeDependentParameters_affectingInfectivities = false;
        private bool _thereAreTimeDependentParameters_affectingTranmissionDynamics = false;
        private bool _thereAreTimeDependentParameters_affectingNaturalHistoryRates = false;
        private bool _thereAreTimeDependentParameters_affectingSplittingClasses = false;

        public double[] ParValues { get; private set; }

        // get the value of parameters to calibrate
        public double[] GetValuesOfParametersToCalibrate()
        {
            double[] parValues = new double[1];
            int i = 0;
            foreach (Parameter thisParameter in Parameters.Where(p => p.IncludedInCalibration == true))
                parValues[i++] = thisParameter.Value;

            return parValues;
        }

        public void SampleAllParameters(ref RNG rng, double time)
        {
            // sample from parameters
            ParValues = new double[Parameters.Count];
            foreach (Parameter thisParameter in Parameters)
                SampleThisParameter(ref rng, thisParameter, time);
        }
        public void UpdateTimeDepedentParameters(ref RNG rng, double time)
        {
            foreach (Parameter thisParameter in Parameters.Where(p => p.ShouldBeUpdatedByTime))
                SampleThisParameter(ref rng, thisParameter, time);
        }

        // Sample this parameter
        private void SampleThisParameter(ref RNG rng, Parameter thisPar, double time)
        {
            switch (thisPar.Type)
            {
                // independent parameter
                case Parameter.EnumType.Independet:
                    {
                        ParValues[thisPar.ID] = ((IndependetParameter)thisPar).Sample(rng);
                    }
                    break;

                // correlated parameter
                case Parameter.EnumType.Correlated:
                    {
                        CorrelatedParameter thisCorrelatedParameter = thisPar as CorrelatedParameter;
                        int parameterIDCorrelatedTo = thisCorrelatedParameter.IDOfDepedentPar;
                        double valueOfTheParameterIDCorrelatedTo = ParValues[parameterIDCorrelatedTo];
                        ParValues[thisPar.ID] = thisCorrelatedParameter.Sample(valueOfTheParameterIDCorrelatedTo);
                    }
                    break;

                // multiplicative parameter
                case Parameter.EnumType.Multiplicative:
                    {
                        MultiplicativeParameter thisMultiplicativeParameter = thisPar as MultiplicativeParameter;
                        int firstParID = thisMultiplicativeParameter.FirstParameterID;
                        int secondParID = thisMultiplicativeParameter.SecondParameterID;
                        ParValues[thisPar.ID] = thisMultiplicativeParameter.Sample(ParValues[firstParID], ParValues[secondParID]);
                    }
                    break;

                // linear combination parameter
                case Parameter.EnumType.LinearCombination:
                    {
                        LinearCombination thisLinearCombinationPar = thisPar as LinearCombination;
                        int[] arrParIDs = thisLinearCombinationPar.arrParIDs;
                        double[] arrValueOfParameters = new double[arrParIDs.Length];

                        for (int i = 0; i < arrParIDs.Length; i++)
                            arrValueOfParameters[i] = ParValues[arrParIDs[i]];

                        ParValues[thisPar.ID] = thisLinearCombinationPar.Sample(arrValueOfParameters);
                    }
                    break;

                // multiple combination parameter
                case Parameter.EnumType.MultipleCombination:
                    {
                        MultipleCombination thisMultipleCombinationPar = thisPar as MultipleCombination;
                        int[] arrParIDs = thisMultipleCombinationPar.arrParIDs;
                        double[] arrValueOfParameters = new double[arrParIDs.Length];
                        for (int i = 0; i < arrParIDs.Length; i++)
                            arrValueOfParameters[i] = ParValues[arrParIDs[i]];

                        ParValues[thisPar.ID] = thisMultipleCombinationPar.Sample(arrValueOfParameters);
                    }
                    break;

                // time dependent linear parameter
                case Parameter.EnumType.TimeDependetLinear:
                    {
                        TimeDependetLinear thisTimeDepedentLinearPar = thisPar as TimeDependetLinear;
                        double intercept = ParValues[thisTimeDepedentLinearPar.InterceptParID];
                        double slope = ParValues[thisTimeDepedentLinearPar.SlopeParID];
                        double timeOn = thisTimeDepedentLinearPar.TimeOn;
                        double timeOff = thisTimeDepedentLinearPar.TimeOff;

                        ParValues[thisPar.ID] = thisTimeDepedentLinearPar.Sample(time, intercept, slope, timeOn, timeOff);
                    }
                    break;

                // time dependent oscillating parameter
                case Parameter.EnumType.TimeDependetOscillating:
                    {
                        TimeDependetOscillating thisTimeDepedentOscillatingPar = thisPar as TimeDependetOscillating;
                        double a0 = ParValues[thisTimeDepedentOscillatingPar.a0ParID];
                        double a1 = ParValues[thisTimeDepedentOscillatingPar.a1ParID];
                        double a2 = ParValues[thisTimeDepedentOscillatingPar.a2ParID];
                        double a3 = ParValues[thisTimeDepedentOscillatingPar.a3ParID];

                        ParValues[thisPar.ID] = thisTimeDepedentOscillatingPar.Sample(time, a0, a1, a2, a3);
                    }
                    break;
            }
        }
       
    }

    class ForceOfInfectionModeller
    {
        // contact and transmission matrices
        private double[][,] _baseContactMatrices = null;                    //[pathogen ID][group i, group j] 
        private int[][][,] _percentChangeInContactMatricesParIDs = null;    //[intervention ID][pathogen ID][group i, group j]
        private double[][][,] _contactMatrices = null;                      //[intervention ID][pathogen ID][group i, group j]
        private double[][][][] _tranmissionMatrices = null;                 // [intervention ID][pathogen ID][group i][group j]
        private int[] _indecesOfInterventionsAffectingContactPattern;
        private int[] _onOffStatusOfInterventionsAffectingContactPattern;

        public int NumOfInterventionsAffectingContactPattern { get; private set; } = 0;

        public void Reset()
        {
            _onOffStatusOfInterventionsAffectingContactPattern = new int[NumOfInterventionsAffectingContactPattern];
        }

        // find the index of this intervention combination in transmission matrix
        private int FindIndexOfInterventionCombimbinationInTransmissionMatrix(int[] interventionCombination)
        {
            for (int i = 0; i < NumOfInterventionsAffectingContactPattern; i++)
            {
                _onOffStatusOfInterventionsAffectingContactPattern[i] = interventionCombination[_indecesOfInterventionsAffectingContactPattern[i]];
            }
            return SupportFunctions.ConvertToBase10FromBase2(_onOffStatusOfInterventionsAffectingContactPattern);
        }
        // find the index of this intervention combination in contact matrices
        private int FindIndexOfInterventionCombimbinationInContactMatrices(int[] interventionCombination)
        {
            for (int i = 0; i < NumOfInterventionsAffectingContactPattern; i++)
            {
                _onOffStatusOfInterventionsAffectingContactPattern[i] = interventionCombination[_indecesOfInterventionsAffectingContactPattern[i]];
            }
            return SupportFunctions.ConvertToBase10FromBase2(_onOffStatusOfInterventionsAffectingContactPattern);
        }

        public void Clean()
        {
            _baseContactMatrices = null;
            _percentChangeInContactMatricesParIDs = null;
            _contactMatrices = null;
            _tranmissionMatrices = null;
            _indecesOfInterventionsAffectingContactPattern = null;
            _onOffStatusOfInterventionsAffectingContactPattern = null;
        }
    }
}
