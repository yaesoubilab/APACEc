using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputationLib;

namespace SimulationLib
{
    public class Calibration
    {        
        #region Variables

        int _numOfParameters;
        string[] _namesOfParameters;
        string[] _namesOfSimOutsWithNonZeroWeights;
        private List<ResultOfASimulation> _colOfSimulationResults = new List<ResultOfASimulation>();
        private List<CalibrationTarget> _colOfCalibrationTargets = new List<CalibrationTarget>();

        protected int[] _arrSimulationItr;
        protected int[] _arrSimulationRNDSeeds;
        protected double[][] _matrixOfGoodnessOfFit;
        protected double[][] _matrixOfParameterValues;
        protected double[][] _matrixOfSimObs; // matrix of simulation observations used for calibratoin 

        private int[] _arrSelectedSimulationItr;
        private int[] _arrSelectedSimulationRNDSeeds;
        private double[,] _matrixOfSelectedGoodnessOfFit;
        private double[,] _matrixOfSelectedParameterValues;
        private double[,] _matrixOfSelectedSimObs;

        #endregion

        // Properties
        #region Properties

        public int[] SimulationItrs
        {
            get { return _arrSimulationItr; }
        }
        public int[] SimulationRNDSeeds
        {
            get { return _arrSimulationRNDSeeds; }
        }
        public double[][] GoodnessOfFitValues
        {
            get { return _matrixOfGoodnessOfFit; }
        }
        public double[][] MatrixOfParameterValues
        {
            get { return _matrixOfParameterValues; }
        }
        public double[][] MatrixOfSimObs
        {
            get { return _matrixOfSimObs; } 
        }
        public string[] NamesOfParameters
        {
            get { return _namesOfParameters; }
            set { _namesOfParameters = value; }
        }
        public string[] NamesOfSimOutsWithNonZeroWeights
        {
            get { return _namesOfSimOutsWithNonZeroWeights; }
        }
        public int[] SelectedSimulationItrs
        { get { return _arrSelectedSimulationItr; } }
        public int[] SelectedSimulationRNDSeeds
        { get { return _arrSelectedSimulationRNDSeeds; } }
        public double[,] SelectedGoodnessOfFit
        { get { return _matrixOfSelectedGoodnessOfFit; } }
        public double[,] SelectedParameterValues
        { get { return _matrixOfSelectedParameterValues; } }
        public double[,] SelectedSimObservations
        { get { return _matrixOfSelectedSimObs; } }

        #endregion

        // Instantiation
        public Calibration(int numOfParameters)
        {
            _numOfParameters = numOfParameters;
        }
        // reset
        public void Reset()
        { 
            _arrSimulationItr = new int[0];
            _arrSimulationRNDSeeds = new int[0];
            _matrixOfGoodnessOfFit = new double[0][];
            _matrixOfParameterValues = new double[0][];
            _matrixOfSimObs = new double[0][];
        }

        // add a calibration target with sum of squared erros of the time-series as the measure of fit
        public void AddACalibrationTarget_timeSeries(string name, double weightOfThisCalibrationTarget, double[] observations, double[] weightsOfObservations)
        {
            // create a new target
            CalibrationTarget thisCalibrationTarget = new CalibrationTarget();
            thisCalibrationTarget.GoodnessOfFitMeasure = CalibrationTarget.enumGoodnessOfFitMeasure.SumSqurError_timeSeries;
            // enter observations and observation weights
            thisCalibrationTarget.EnterObservation_timeSeries(observations, weightsOfObservations);
            // enter the weight of this calibration target
            thisCalibrationTarget.CalibrationTargetWeight = weightOfThisCalibrationTarget;
            // store
            _colOfCalibrationTargets.Add(thisCalibrationTarget);

            // record names
            for (int i = 0; i < weightsOfObservations.Length; i++)
            {
                if (weightsOfObservations[i] > 0)
                    SupportFunctions.AddToEndOfArray(ref _namesOfSimOutsWithNonZeroWeights, name + " | period " + (i + 1).ToString());
            }
            
        }
        // add a calibration target with sum of squared erros of the average time-series as the measure of fit
        public void AddACalibrationTarget_aveTimeSeries(string name, double weightOfThisCalibrationTarget, double[] observations, double[] weightsOfObservations)
        {
            // create a new target
            CalibrationTarget thisCalibrationTarget = new CalibrationTarget();
            thisCalibrationTarget.GoodnessOfFitMeasure = CalibrationTarget.enumGoodnessOfFitMeasure.SumSqurError_average;
            // enter observations and observation weights
            thisCalibrationTarget.EnterObservation_aveTimeSeries(observations[observations.Length-1], weightsOfObservations[weightsOfObservations.Length-1]);
            // enter the weight of this calibration target
            thisCalibrationTarget.CalibrationTargetWeight = weightOfThisCalibrationTarget;
            // store
            _colOfCalibrationTargets.Add(thisCalibrationTarget);

            // record names
            SupportFunctions.AddToEndOfArray(ref _namesOfSimOutsWithNonZeroWeights, name);

        }
        // add a calibration target with Fourier transform as measure of fit
        public void AddACalibrationTarget_fourier(string name, double weightOfThisCalibrationTarget, double[] observations, double[] weightsOfFourierSimilarityMeasures)
        {
            // create a new target
            CalibrationTarget thisCalibrationTarget = new CalibrationTarget();
            thisCalibrationTarget.GoodnessOfFitMeasure = CalibrationTarget.enumGoodnessOfFitMeasure.Fourier;
            // enter observations and observation weights
            thisCalibrationTarget.EnterObservation_fourier(observations, weightsOfFourierSimilarityMeasures);
            // enter the weight of this calibration target
            thisCalibrationTarget.CalibrationTargetWeight = weightOfThisCalibrationTarget;
            // store
            _colOfCalibrationTargets.Add(thisCalibrationTarget);

            // record the names
            SupportFunctions.AddToEndOfArray(ref _namesOfSimOutsWithNonZeroWeights, name + " | cos(theta)");
            SupportFunctions.AddToEndOfArray(ref _namesOfSimOutsWithNonZeroWeights, name + " | norm F");
            SupportFunctions.AddToEndOfArray(ref _namesOfSimOutsWithNonZeroWeights, name + " | ave");
            SupportFunctions.AddToEndOfArray(ref _namesOfSimOutsWithNonZeroWeights, name + " | stDev");
            SupportFunctions.AddToEndOfArray(ref _namesOfSimOutsWithNonZeroWeights, name + " | min");
            SupportFunctions.AddToEndOfArray(ref _namesOfSimOutsWithNonZeroWeights, name + " | max");
        }        

        // add a simulation result
        public void AddResultOfASimulationRun(int simItr, int rndSeed, ref double[] parameterValues, ref double[,] matrixOfObservations)
        {
            // store this result
            _colOfSimulationResults.Add(new ResultOfASimulation(simItr, rndSeed, ref parameterValues, ref matrixOfObservations));
        }
        // find the fit of the recorded simulation results
        public void FindTheFitOfRecordedSimulationResults(bool ifToProcessInParallel)
        {
            double[] matrixOfGoodnessOfFit = new double[_colOfSimulationResults.Count];

            if (ifToProcessInParallel == false)
            {
                foreach (ResultOfASimulation thisResult in _colOfSimulationResults)
                {
                    // find the goodness of fit 
                    thisResult.CalculateGoodnessOfFit(_colOfCalibrationTargets);
                }
            }
            else // process in parallel
            {
                Parallel.ForEach(_colOfSimulationResults.Cast<object>(), thisResult =>
                {
                    ((ResultOfASimulation)thisResult).CalculateGoodnessOfFit(_colOfCalibrationTargets);
                });
            }

            foreach (ResultOfASimulation thisResult in _colOfSimulationResults)
            {
                // store the iteration and rnd seeds
                SupportFunctions.AddToEndOfArray(ref _arrSimulationItr, thisResult.SimItr);
                SupportFunctions.AddToEndOfArray(ref _arrSimulationRNDSeeds, thisResult.RndSeed);
                // parameter values for this simulation run
                _matrixOfParameterValues = SupportFunctions.ConcatJaggedArray(_matrixOfParameterValues, thisResult.ParameterValues);
                // simulation observations for this simulation run
                _matrixOfSimObs = SupportFunctions.ConcatJaggedArray(_matrixOfSimObs, thisResult.VectorOfSimOutsNonZerpWeight);
                // concatenate results
                _matrixOfGoodnessOfFit = SupportFunctions.ConcatJaggedArray(_matrixOfGoodnessOfFit, thisResult.GoodnessOfFit);
            }

            // clear the collection of simulation results
            _colOfSimulationResults.Clear();
        }
        
        // find the fittest simulation run
        public void FindTheFittestSimulationRuns(long numOfSelectedSimulationRuns)
        {
            numOfSelectedSimulationRuns = Math.Min(numOfSelectedSimulationRuns, _arrSimulationItr.Length);

            if (numOfSelectedSimulationRuns == 0) return;

            _arrSelectedSimulationItr = new int[numOfSelectedSimulationRuns];
            _arrSelectedSimulationRNDSeeds = new int[numOfSelectedSimulationRuns];
            _matrixOfSelectedGoodnessOfFit = new double[numOfSelectedSimulationRuns, _colOfCalibrationTargets.Count + 1];
            _matrixOfSelectedParameterValues = new double[numOfSelectedSimulationRuns, _numOfParameters];
            _matrixOfSelectedSimObs = new double[numOfSelectedSimulationRuns, _matrixOfSimObs[0].Length];

            // sort by goodness of fit
            SortAllIterationsByGoodnessOfFit();

            // store the acceptable runs among all simulation runs
            for (int i = 0; i < Math.Min(numOfSelectedSimulationRuns, _arrSimulationItr.Length); i++)
            {
                _arrSelectedSimulationItr[i] = _arrSimulationItr[i];
                _arrSelectedSimulationRNDSeeds[i] = _arrSimulationRNDSeeds[i];
                for (int j = 0; j < _colOfCalibrationTargets.Count + 1; j++)
                    _matrixOfSelectedGoodnessOfFit[i, j] = _matrixOfGoodnessOfFit[i][j];
                // parameter values for this simulation run
                for (int j = 0; j < _numOfParameters; j++)
                    _matrixOfSelectedParameterValues[i, j] = _matrixOfParameterValues[i][j];
                // simulation observations with non-zero weights
                for (int j = 0; j < _matrixOfSelectedSimObs.GetLength(1); j++)
                    _matrixOfSelectedSimObs[i, j] = _matrixOfSimObs[i][j];
            }
        }

        // sort by goodness of fit
        private void SortAllIterationsByGoodnessOfFit()
        {
            int numOfRows = _matrixOfGoodnessOfFit.GetLength(0);

            // make a key array
            int[] arrKeys = new int[numOfRows];
            for (int i = 0; i < numOfRows; i++)
                arrKeys[i] = i;

            // sort by goodness of fit
            double[] arrOveralGoodnessOfFit = new double[numOfRows];
            for (int i = 0; i < numOfRows; i++)
                arrOveralGoodnessOfFit[i] = _matrixOfGoodnessOfFit[i][0];

            Array.Sort(arrOveralGoodnessOfFit, arrKeys);

            // sort other columns
            int[] arrOriginalCalibrationItr = (int[])_arrSimulationItr.Clone();
            int[] arrOriginalCalibrationRNDSeeds = (int[])_arrSimulationRNDSeeds.Clone();
            double[][] originalMatrixOfParametrValues = (double[][])_matrixOfParameterValues.Clone();
            double[][] originalGoodnessOfFitValues = (double[][]) _matrixOfGoodnessOfFit.Clone();
            double[][] originalSimObs = (double[][])_matrixOfSimObs.Clone();
            for (int i = 0; i < numOfRows; i++)
            {
                _arrSimulationItr[i] = arrOriginalCalibrationItr[arrKeys[i]];
                _arrSimulationRNDSeeds[i] = arrOriginalCalibrationRNDSeeds[arrKeys[i]];
                _matrixOfParameterValues[i] = originalMatrixOfParametrValues[arrKeys[i]];
                _matrixOfGoodnessOfFit[i] = originalGoodnessOfFitValues[arrKeys[i]];
                _matrixOfSimObs[i] = originalSimObs[arrKeys[i]];
            }
        }

        //// find the fittest times series by shifting the observations 
        //private void FindTheFittestTimeSeries(double[,] targets, double[,] observations, double[,] weights, double[] arrScalingFactors, int baseNumOfObservationToShift,
        //                                     ref double [,] shiftedObservationsWithBestFit)
        //{
        //    double measureOfFit = 0; 
        //    int numOfRows = targets.GetLength(0);
        //    double[,] relativeErrorMatrix = new double[numOfRows, targets.GetLength(1)];
        //    double minFit = double.MaxValue;

        //    for (int shiftStep = 0; shiftStep < numOfRows; shiftStep  += baseNumOfObservationToShift)
        //    {
        //        // shift the observatoin matrix by shiftStep
        //        double[,] shiftedObservations = null;
        //        ShiftRows(observations, shiftStep, ref shiftedObservations);

        //        // find the relative error matrix
        //        double denominator = 0;
        //        for (int j = 0; j < targets.GetLength(1); j++)
        //            for (int i = 0; i < numOfRows; i++)
        //        {
        //            if (double.IsNaN(targets[i, j]))
        //                relativeErrorMatrix[i, j] = double.NaN;
        //            else
        //            {
        //                if (Math.Abs(targets[i, j]) < double.MinValue)
        //                    denominator = arrScalingFactors[j];
        //                else
        //                    denominator = targets[i, j];

        //                relativeErrorMatrix[i, j] = Math.Abs((shiftedObservations[i, j] - targets[i, j]) / denominator);
        //            }
        //        }

        //        // find the measure of fit
        //        measureOfFit = LinearAlgebraFunctions.Norm(relativeErrorMatrix, weights, LinearAlgebraFunctions.enumMatrixNorm.Frobenius);
        //        // compare this measure of fit to the best so far
        //        if (measureOfFit < minFit)
        //        {
        //            minFit = measureOfFit;
        //            shiftedObservationsWithBestFit = (double[,])shiftedObservations.Clone();
        //        }
        //    }
        //}

        //// shift the rows of a matrix
        //private void ShiftRows(double[,] data, int step, ref double[,] shiftedData)
        //{
        //    int numOfRows = data.GetLength(0);
        //    int numOfCols = data.GetLength(1);
        //    int desRow = 0;

        //    shiftedData = new double[numOfRows, numOfCols];

        //    for (int i = 0; i < numOfRows; i++)
        //    {
        //        Math.DivRem(i + step, numOfRows, out desRow);
        //        for (int j = 0; j < numOfCols; j++)
        //            shiftedData[desRow, j] = data[i, j];
        //    }
        //}
    }

    public class CalibrationTarget
    {
        public enum enumGoodnessOfFitMeasure : int
        {
            SumSqurError_timeSeries = 1,
            SumSqurError_average = 2,
            Fourier = 3,
        }
        public enum enumFourierSimilarityMeasures : int
        {
            Average = 0,
            StDev = 1,
            Min = 2,
            Max = 3,
            Norm2 = 4,
            Cosine = 5,
            SIZE = 6,
        }

        enumGoodnessOfFitMeasure _goodnessOfFitMeasure = enumGoodnessOfFitMeasure.SumSqurError_timeSeries;
        double _calibrationTargetWeight;
        double[] _observations;
        double[] _weightsOfObservations;

        double[] _observedFourierAmplitudes;
        double[] _observedFourierSimilarityMeasures; // 6 similarity measures as described by enumFourierGoodnessOfFitComponents
        double[] _weightsOfFourierSimilarityMeasures; // 6 similarity measures as described by enumFourierGoodnessOfFitComponents

        // Properties
        public enumGoodnessOfFitMeasure GoodnessOfFitMeasure
        {
            get { return _goodnessOfFitMeasure; }
            set { _goodnessOfFitMeasure = value; }
        }
        public double CalibrationTargetWeight
        {
            get { return _calibrationTargetWeight; }
            set { _calibrationTargetWeight = value; }
        }
        public double[] WeightsOfObservations
        {
            get { return _weightsOfObservations; }
        }
        public double[] Observations
        {
            get { return _observations; }
        }
        public double[] ObservedFourierAmplitudes
        {
            get { return _observedFourierAmplitudes; }
        }

        // Instantiation
        public CalibrationTarget()
        {
        }

        // return the observed Fourier similarity measure
        public double ObservedFourierSimilarityMeasures(enumFourierSimilarityMeasures measure)
        {
            return _observedFourierSimilarityMeasures[(int)measure];
        }
        public double WeightsOfFourierSimilarityMeasures(enumFourierSimilarityMeasures measure)
        {
            return _weightsOfFourierSimilarityMeasures[(int)measure];
        }

        // enter observation with measure of fit being sum of squared error or time-series
        public void EnterObservation_timeSeries(double[] observations, double[] weightsOfObservations)
        {
            _observations = (double[])observations.Clone();
            _weightsOfObservations = (double[])weightsOfObservations.Clone();
        }
        // enter observation with measure of fit being sum of squared error of average time-series
        public void EnterObservation_aveTimeSeries(double observation, double weight)
        {
            _observations = new double[1];
            _observations[0] = observation;

            _weightsOfObservations = new double[1];
            _weightsOfObservations[0] = weight;
        }
        // enter observation with Fourier transform as measure of fit
        public void EnterObservation_fourier(double[] observations, double[] weightsOfFourierSimilarityMeasures)
        {
            _observations = (double[])observations.Clone();
            _weightsOfFourierSimilarityMeasures = (double[])weightsOfFourierSimilarityMeasures.Clone();

            // find the Fourier weights of time-series
            _observedFourierAmplitudes = new double[0];
            double[] arrReconstructedFourierObs = new double[0], arrPeriods = new double[0];
            FourierTransform.DoFourierTransform(observations, ref arrPeriods, ref _observedFourierAmplitudes, ref arrReconstructedFourierObs);

            // record average, min and max
            _observedFourierSimilarityMeasures = new double[5];
            _observedFourierSimilarityMeasures[(int)enumFourierSimilarityMeasures.Average] = observations.Average();
            _observedFourierSimilarityMeasures[(int)enumFourierSimilarityMeasures.StDev] = StatisticalFunctions.StDev(observations);
            _observedFourierSimilarityMeasures[(int)enumFourierSimilarityMeasures.Min] = observations.Min();
            _observedFourierSimilarityMeasures[(int)enumFourierSimilarityMeasures.Max] = observations.Max();
            _observedFourierSimilarityMeasures[(int)enumFourierSimilarityMeasures.Norm2] = ComputationLib.LinearAlgebraFunctions.Norm(_observedFourierAmplitudes, LinearAlgebraFunctions.enumVectorNorm.L_2);
        }
    }

    class ResultOfASimulation
    {
        private int _simItr;
        private int _rndSeed;
        private double[] _parameterValues;
        private double[,] _matrixOfSimulationOutputs;
        private double[] _vectorOfSimOutsNonZerpWeight;
        private double[] _arrGoodnessOfFit;

        public ResultOfASimulation(int simItr, int rndSeed, ref double[] parameterValues, ref double[,] matrixOfObservations)
        {
            _simItr = simItr;
            _rndSeed = rndSeed;
            _parameterValues = (double[])parameterValues.Clone();
            _matrixOfSimulationOutputs = (double[,])matrixOfObservations.Clone();
            _vectorOfSimOutsNonZerpWeight = new double[0];
        }

        public int SimItr
        { get { return _simItr; } }
        public int RndSeed
        { get { return _rndSeed; } }
        public double[] ParameterValues
        { get { return _parameterValues; } }
        public double[] GoodnessOfFit
        {
            get { return _arrGoodnessOfFit; }
            set { _arrGoodnessOfFit = value; }
        }
        public double[] VectorOfSimOutsNonZerpWeight
        {
            get { return _vectorOfSimOutsNonZerpWeight; }
        }
        // calculate the goodness of fit for one observation matrix
        public void CalculateGoodnessOfFit(List<CalibrationTarget> colOfCalibrationTargets)
        {
            _arrGoodnessOfFit = new double[colOfCalibrationTargets.Count + 1];

            // find the goodness of fit for each calibration target
            int j = 0;
            double weightedGoodnessOfFit = 0.0;
            double totalWeight = 0.0;
            foreach (CalibrationTarget thisCalibrationTarget in colOfCalibrationTargets)
            {
                if (thisCalibrationTarget.CalibrationTargetWeight > 0)
                {
                    // find the goodness of fit with respect to this calibration target
                    _arrGoodnessOfFit[j + 1] = CalcuateGoodnessOfFitToThisCalibrationTarget(thisCalibrationTarget, j);

                    // update goodness of fit and weights
                    weightedGoodnessOfFit += _arrGoodnessOfFit[j + 1] * thisCalibrationTarget.CalibrationTargetWeight;
                    totalWeight += thisCalibrationTarget.CalibrationTargetWeight;

                }
                // next calibration target
                j++;
            }
            // find the weighted goodness of fit
            _arrGoodnessOfFit[0] = weightedGoodnessOfFit / totalWeight;
        }

        // calculate goodness of fit with respect to this calibration target
        private double CalcuateGoodnessOfFitToThisCalibrationTarget(CalibrationTarget calibrationTarget, int colIndexInSimuOutputMatrix)
        {
            double goodnessOfFit = 0;
            
            switch (calibrationTarget.GoodnessOfFitMeasure)
            {
                case CalibrationTarget.enumGoodnessOfFitMeasure.SumSqurError_timeSeries:
                    {
                        // calculate the weighted sum of squired errors
                        for (int i = 0; i < _matrixOfSimulationOutputs.GetLength(0); i++)
                        {
                            if (calibrationTarget.WeightsOfObservations[i] > 0)
                            {
                                goodnessOfFit += calibrationTarget.WeightsOfObservations[i]
                                    * Math.Pow(calibrationTarget.Observations[i] - _matrixOfSimulationOutputs[i, colIndexInSimuOutputMatrix], 2);

                                SupportFunctions.AddToEndOfArray(ref _vectorOfSimOutsNonZerpWeight, _matrixOfSimulationOutputs[i, colIndexInSimuOutputMatrix]);
                            }
                        }
                    }
                    break;
                case CalibrationTarget.enumGoodnessOfFitMeasure.SumSqurError_average:
                    {
                        // calcualte the average
                        double sum = 0, ave = 0;
                        for (int i = 0; i < _matrixOfSimulationOutputs.GetLength(0); i++)
                            sum += _matrixOfSimulationOutputs[i, colIndexInSimuOutputMatrix];
                        ave = sum / _matrixOfSimulationOutputs.GetLength(0);

                        // calculate the weighted sum of squired errors
                        goodnessOfFit += Math.Pow(calibrationTarget.Observations[0] - ave, 2);

                        SupportFunctions.AddToEndOfArray(ref _vectorOfSimOutsNonZerpWeight, ave);
                    }
                    break;
                case CalibrationTarget.enumGoodnessOfFitMeasure.Fourier:
                    {
                        // find the vector of observations for this calibration target
                        double[] simOutputForThisCalibrationTarget = new double[_matrixOfSimulationOutputs.GetLength(0)];
                        for (int i = 0; i < simOutputForThisCalibrationTarget.Length; i++)
                            simOutputForThisCalibrationTarget[i] = _matrixOfSimulationOutputs[i, colIndexInSimuOutputMatrix];

                        // find the Fourier weights of time-series (only if the weight of cosine is greater than 0)
                        double normFourierWeights = -1;
                        double dotProduct= -1;
                        double cosAngle = -1;
                        if (calibrationTarget.WeightsOfFourierSimilarityMeasures(CalibrationTarget.enumFourierSimilarityMeasures.Cosine) > 0)
                        {
                            double[] arrFourierTransformWeights = new double[0], arrReconstructedFourierObs = new double[0], arrPeriods = new double[0];
                            FourierTransform.DoFourierTransform(simOutputForThisCalibrationTarget, ref arrPeriods, ref arrFourierTransformWeights, ref arrReconstructedFourierObs);

                            normFourierWeights = LinearAlgebraFunctions.Norm(arrFourierTransformWeights, LinearAlgebraFunctions.enumVectorNorm.L_2);
                            dotProduct = LinearAlgebraFunctions.DotProduct(calibrationTarget.ObservedFourierAmplitudes, arrFourierTransformWeights);
                            cosAngle = dotProduct / (calibrationTarget.ObservedFourierSimilarityMeasures(CalibrationTarget.enumFourierSimilarityMeasures.Norm2) * normFourierWeights);
                        }

                        // find the goodness of fit                        
                        double average = simOutputForThisCalibrationTarget.Average();
                        double stDev = ComputationLib.StatisticalFunctions.StDev(simOutputForThisCalibrationTarget);
                        double min = simOutputForThisCalibrationTarget.Min();
                        double max = simOutputForThisCalibrationTarget.Max();

                        // store simulation observations
                        SupportFunctions.AddToEndOfArray(ref _vectorOfSimOutsNonZerpWeight, cosAngle);
                        SupportFunctions.AddToEndOfArray(ref _vectorOfSimOutsNonZerpWeight, normFourierWeights);
                        SupportFunctions.AddToEndOfArray(ref _vectorOfSimOutsNonZerpWeight, average);
                        SupportFunctions.AddToEndOfArray(ref _vectorOfSimOutsNonZerpWeight, stDev);
                        SupportFunctions.AddToEndOfArray(ref _vectorOfSimOutsNonZerpWeight, min);
                        SupportFunctions.AddToEndOfArray(ref _vectorOfSimOutsNonZerpWeight, max);

                        // calculate the goodness of fit
                        if (normFourierWeights == -1)
                        {
                            goodnessOfFit = double.MaxValue;
                        }
                        else
                        {
                            // cosine similarity 
                            goodnessOfFit += calibrationTarget.WeightsOfFourierSimilarityMeasures(CalibrationTarget.enumFourierSimilarityMeasures.Cosine)
                                * 1 / 4 * Math.Pow(1 - cosAngle, 2);
                            // euclidean distance
                            goodnessOfFit += calibrationTarget.WeightsOfFourierSimilarityMeasures(CalibrationTarget.enumFourierSimilarityMeasures.Norm2)
                                * Math.Pow(
                                            calibrationTarget.ObservedFourierSimilarityMeasures(CalibrationTarget.enumFourierSimilarityMeasures.Norm2) - normFourierWeights
                                       , 2);
                            // average
                            goodnessOfFit += calibrationTarget.WeightsOfFourierSimilarityMeasures(CalibrationTarget.enumFourierSimilarityMeasures.Average)
                                * Math.Pow(calibrationTarget.ObservedFourierSimilarityMeasures(CalibrationTarget.enumFourierSimilarityMeasures.Average) - average, 2);
                            // st dev
                            goodnessOfFit += calibrationTarget.WeightsOfFourierSimilarityMeasures(CalibrationTarget.enumFourierSimilarityMeasures.StDev)
                                * Math.Pow(calibrationTarget.ObservedFourierSimilarityMeasures(CalibrationTarget.enumFourierSimilarityMeasures.StDev) - stDev, 2);
                            // min
                            goodnessOfFit += calibrationTarget.WeightsOfFourierSimilarityMeasures(CalibrationTarget.enumFourierSimilarityMeasures.Min)
                                * Math.Pow(calibrationTarget.ObservedFourierSimilarityMeasures(CalibrationTarget.enumFourierSimilarityMeasures.Min) - min, 2);
                            // max
                            goodnessOfFit += calibrationTarget.WeightsOfFourierSimilarityMeasures(CalibrationTarget.enumFourierSimilarityMeasures.Max)
                                * Math.Pow(calibrationTarget.ObservedFourierSimilarityMeasures(CalibrationTarget.enumFourierSimilarityMeasures.Max) - max, 2);
                            //_goodnessOfFit = _goodnessOfFit / _weightsOfFourierSimilarityMeasures.Sum();
                        }
                    }
                    break;
            }

            return goodnessOfFit;
        }

    }
}



