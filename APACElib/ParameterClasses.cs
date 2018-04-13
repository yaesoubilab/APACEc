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
        public double[] ParameterValues { get; private set; }
        private bool _thereAreTimeDepParms = false;
        private bool _thereAreTimeDepParms_affectingSusceptibilities = false;
        private bool _thereAreTimeDepParms_affectingInfectivities = false;
        private bool _thereAreTimeDepParms_affectingTranmissionDynamics = false;
        private bool _thereAreTimeDepParms_affectingNaturalHistoryRates = false;
        private bool _thereAreTimeDepParms_affectingSplittingClasses = false;

        public ParameterManager()
        {

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

        public void SampleAllParameters(ref RNG rng, double time)
        {
            // sample from parameters
            ParameterValues = new double[Parameters.Count];
            foreach (Parameter thisParameter in Parameters)
                SampleThisParameter(ref rng, thisParameter, time);
        }
        public void UpdateTimeDepedentParameters(ref RNG rng, double time)
        {
            foreach (Parameter thisParameter in Parameters.Where(p => p.ShouldBeUpdatedByTime))
                SampleThisParameter(ref rng, thisParameter, time);
        }

        // update time dependent parameters in the model
        private void UpdateTimeDepedentParametersInTheModel(ref List<Class> classes)
        {
            // update transmission dynamic matrix if necessary
            if (_thereAreTimeDepParms_affectingTranmissionDynamics)
            {
                //CalculateTransmissionMatrix();
                // update transmission rates
                //UpdateTransmissionRates();
            }

            // update event rates if necessary
            if (_thereAreTimeDepParms_affectingNaturalHistoryRates)
            {
                // update rates associated with each class and their initial size
                foreach (Class thisClass in classes)
                    thisClass.UpdateRatesOfBirthAndEpiIndpEvents(ParameterValues);
            }

            // update value of splitting class parameters
            if (_thereAreTimeDepParms_affectingSplittingClasses)
            {
                // update the probability of success
                foreach (Class thisClass in classes)
                    thisClass.UpdateProbOfSuccess(ParameterValues);
            }
        }

        // Sample this parameter
        private void SampleThisParameter(ref RNG rng, Parameter thisPar, double time)
        {
            switch (thisPar.Type)
            {
                // independent parameter
                case Parameter.EnumType.Independet:
                    {
                        ParameterValues[thisPar.ID] = ((IndependetParameter)thisPar).Sample(rng);
                    }
                    break;

                // correlated parameter
                case Parameter.EnumType.Correlated:
                    {
                        CorrelatedParameter thisCorrelatedParameter = thisPar as CorrelatedParameter;
                        int parameterIDCorrelatedTo = thisCorrelatedParameter.IDOfDepedentPar;
                        double valueOfTheParameterIDCorrelatedTo = ParameterValues[parameterIDCorrelatedTo];
                        ParameterValues[thisPar.ID] = thisCorrelatedParameter.Sample(valueOfTheParameterIDCorrelatedTo);
                    }
                    break;

                // multiplicative parameter
                case Parameter.EnumType.Multiplicative:
                    {
                        MultiplicativeParameter thisMultiplicativeParameter = thisPar as MultiplicativeParameter;
                        int firstParID = thisMultiplicativeParameter.FirstParameterID;
                        int secondParID = thisMultiplicativeParameter.SecondParameterID;
                        ParameterValues[thisPar.ID] = thisMultiplicativeParameter.Sample(ParameterValues[firstParID], ParameterValues[secondParID]);
                    }
                    break;

                // linear combination parameter
                case Parameter.EnumType.LinearCombination:
                    {
                        LinearCombination thisLinearCombinationPar = thisPar as LinearCombination;
                        int[] arrParIDs = thisLinearCombinationPar.arrParIDs;
                        double[] arrValueOfParameters = new double[arrParIDs.Length];

                        for (int i = 0; i < arrParIDs.Length; i++)
                            arrValueOfParameters[i] = ParameterValues[arrParIDs[i]];

                        ParameterValues[thisPar.ID] = thisLinearCombinationPar.Sample(arrValueOfParameters);
                    }
                    break;

                // multiple combination parameter
                case Parameter.EnumType.MultipleCombination:
                    {
                        MultipleCombination thisMultipleCombinationPar = thisPar as MultipleCombination;
                        int[] arrParIDs = thisMultipleCombinationPar.arrParIDs;
                        double[] arrValueOfParameters = new double[arrParIDs.Length];
                        for (int i = 0; i < arrParIDs.Length; i++)
                            arrValueOfParameters[i] = ParameterValues[arrParIDs[i]];

                        ParameterValues[thisPar.ID] = thisMultipleCombinationPar.Sample(arrValueOfParameters);
                    }
                    break;

                // time dependent linear parameter
                case Parameter.EnumType.TimeDependetLinear:
                    {
                        TimeDependetLinear thisTimeDepedentLinearPar = thisPar as TimeDependetLinear;
                        double intercept = ParameterValues[thisTimeDepedentLinearPar.InterceptParID];
                        double slope = ParameterValues[thisTimeDepedentLinearPar.SlopeParID];
                        double timeOn = thisTimeDepedentLinearPar.TimeOn;
                        double timeOff = thisTimeDepedentLinearPar.TimeOff;

                        ParameterValues[thisPar.ID] = thisTimeDepedentLinearPar.Sample(time, intercept, slope, timeOn, timeOff);
                    }
                    break;

                // time dependent oscillating parameter
                case Parameter.EnumType.TimeDependetOscillating:
                    {
                        TimeDependetOscillating thisTimeDepedentOscillatingPar = thisPar as TimeDependetOscillating;
                        double a0 = ParameterValues[thisTimeDepedentOscillatingPar.a0ParID];
                        double a1 = ParameterValues[thisTimeDepedentOscillatingPar.a1ParID];
                        double a2 = ParameterValues[thisTimeDepedentOscillatingPar.a2ParID];
                        double a3 = ParameterValues[thisTimeDepedentOscillatingPar.a3ParID];

                        ParameterValues[thisPar.ID] = thisTimeDepedentOscillatingPar.Sample(time, a0, a1, a2, a3);
                    }
                    break;
            }
        }
       
    }

    class ForceOfInfectionModeller
    {
        private ParameterManager _paramManager;
        private int _numOfPathogens;
        private double[][,] _baseContactMatrices = null;                    //[pathogen ID][group i, group j] 
        private int[][][,] _parID_percentageChangeInContactMatrices = null; //[intervention ID][pathogen ID][group i, group j]
        private double[][][,] _contactMatrices = null;                      //[intervention ID][pathogen ID][group i, group j]
        private double[][][][] _tranmissionMatrices = null;                 // [intervention ID][pathogen ID][group i][group j]
        private int[] _indicesOfIntrvsAffectingContactPattern;
        private int[] _onOffStatusOfIntrvsAffectingContactPattern;
        public int NumOfIntrvnAffectingContacts { get; private set; } = 0;

        public ForceOfInfectionModeller(
            ref ParameterManager paramManager, 
            double[][,] baseContactMatrices,
            int[][][,] parID_percentageChangeInContactMatrices)
        {
            _paramManager = paramManager;
            _baseContactMatrices = baseContactMatrices;
            _parID_percentageChangeInContactMatrices = parID_percentageChangeInContactMatrices;
        }

        public void Reset()
        {
            _onOffStatusOfIntrvsAffectingContactPattern = new int[NumOfIntrvnAffectingContacts];
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
                        _contactMatrices[intCombIndex] = new double[_numOfPathogens][,];

                        if (onOfStatusOfIntrvnAffectingContacts[intrvn] == 1)
                        {
                            for (int pathogenID = 0; pathogenID < _numOfPathogens; pathogenID++)
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

        // calculate transmission matrix
        private void CalculateTransmissionMatrix()//(int[] nextPeriodActionCombinationInEffect)
        {
            int contactMatrixSize = _baseContactMatrices[0].GetLength(0);
            int numOfCombinationsOfInterventionsAffectingContactPattern = (int)Math.Pow(2, NumOfIntrvnAffectingContacts);

            double[][] arrInfectivity = new double[_numOfClasses][];
            double[][] arrSusceptibility = new double[_numOfClasses][];
            int[] arrRowInContactMatrix = new int[_numOfClasses];
            double[][][,] contactMatrices = new double[numOfCombinationsOfInterventionsAffectingContactPattern][][,];
            _tranmissionMatrices = new double[numOfCombinationsOfInterventionsAffectingContactPattern][][][];

            // build the contact matrices
            // for all possible intervention combinations
            for (int intCombIndex = 0; intCombIndex < numOfCombinationsOfInterventionsAffectingContactPattern; ++intCombIndex)
            {
                if (intCombIndex == 0)
                    contactMatrices[intCombIndex] = _baseContactMatrices;
                else
                {
                    int[] onOfStatusOfInterventionsAffectingContactPattern = SupportFunctions.ConvertToBase2FromBase10(intCombIndex, NumOfIntrvnAffectingContacts);
                    for (int interventionIndex = 0; interventionIndex < NumOfIntrvnAffectingContacts; ++interventionIndex)
                    {
                        // initialize contact matrices
                        contactMatrices[intCombIndex] = new double[_numOfPathogens][,];

                        if (onOfStatusOfInterventionsAffectingContactPattern[interventionIndex] == 1)
                        {
                            for (int pathogenID = 0; pathogenID < _numOfPathogens; pathogenID++)
                            {
                                contactMatrices[intCombIndex][pathogenID] = new double[contactMatrixSize, contactMatrixSize];
                                for (int i = 0; i < contactMatrixSize; ++i)
                                    for (int j = 0; j < contactMatrixSize; ++j)
                                        contactMatrices[intCombIndex][pathogenID][i, j] = _baseContactMatrices[pathogenID][i, j] +
                                            _baseContactMatrices[pathogenID][i, j] 
                                            * _paramManager.ParameterValues[_parID_percentageChangeInContactMatrices[interventionIndex][pathogenID][i, j]];
                            }
                        }
                    }
                }
            }

            // get the sample of infectivity and susceptibility of classes                    
            int classID = 0;
            foreach (Class thisClass in Classes)
            {
                // update the susceptibility and infectivity parameters based on the sampled parameter values
                //thisClass.UpdateSusceptibilityAndInfectivityParameterValues(_arrSampledParameterValues);

                arrInfectivity[classID] = thisClass.InfectivityValues;
                arrSusceptibility[classID] = thisClass.SusceptibilityValues;
                arrRowInContactMatrix[classID] = thisClass.RowIndexInContactMatrix;
                ++classID;
            }

            // populate the transmission matrix
            for (int intCombIndex = 0; intCombIndex < numOfCombinationsOfInterventionsAffectingContactPattern; ++intCombIndex)
            {
                _tranmissionMatrices[intCombIndex] = new double[_numOfPathogens][][];
                for (int pathogenID = 0; pathogenID < _numOfPathogens; pathogenID++)
                {
                    _tranmissionMatrices[intCombIndex][pathogenID] = new double[_numOfClasses][];
                    for (int i = 0; i < _numOfClasses; ++i)
                    {
                        if (arrSusceptibility[i].Count() != 0)
                        {
                            _tranmissionMatrices[intCombIndex][pathogenID][i] = new double[_numOfClasses];
                            for (int j = 0; j < _numOfClasses; ++j)
                            {
                                if (arrInfectivity[j].Count() != 0)
                                {
                                    _tranmissionMatrices[intCombIndex][pathogenID][i][j]
                                        = arrSusceptibility[i][pathogenID]
                                            * contactMatrices[intCombIndex][pathogenID][arrRowInContactMatrix[i], arrRowInContactMatrix[j]]
                                            * arrInfectivity[j][pathogenID];
                                }
                            }
                        }
                    }
                }
            }
        }

        // find the index of this intervention combination in transmission matrix
        private int FindIndexOfInterventionCombimbinationInTransmissionMatrix(int[] interventionCombination)
        {
            for (int i = 0; i < NumOfIntrvnAffectingContacts; i++)
            {
                _onOffStatusOfIntrvsAffectingContactPattern[i] = interventionCombination[_indicesOfIntrvsAffectingContactPattern[i]];
            }
            return SupportFunctions.ConvertToBase10FromBase2(_onOffStatusOfIntrvsAffectingContactPattern);
        }
        // find the index of this intervention combination in contact matrices
        private int FindIndexOfInterventionCombimbinationInContactMatrices(int[] interventionCombination)
        {
            for (int i = 0; i < NumOfIntrvnAffectingContacts; i++)
            {
                _onOffStatusOfIntrvsAffectingContactPattern[i] = interventionCombination[_indicesOfIntrvsAffectingContactPattern[i]];
            }
            return SupportFunctions.ConvertToBase10FromBase2(_onOffStatusOfIntrvsAffectingContactPattern);
        }

        public void Clean()
        {
            _baseContactMatrices = null;
            _parID_percentageChangeInContactMatrices = null;
            _contactMatrices = null;
            _tranmissionMatrices = null;
            _indicesOfIntrvsAffectingContactPattern = null;
            _onOffStatusOfIntrvsAffectingContactPattern = null;
        }
    }
}
