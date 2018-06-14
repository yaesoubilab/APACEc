using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APACElib
{
    public class Calibration
    {
        private List<ObsAndLikelihoodParams> _history = new List<ObsAndLikelihoodParams>();
        private List<Likelihood> _likelihoods = new List<Likelihood>();
        public List<CalibSimTraj> CalibSimTrajs = new List<CalibSimTraj>();

        public int[] SimulationItrs { get; }
        public int[] SimulationRNDSeeds { get; }
        public double[][] MatrixOfGoodnessOfFit { get; }
        public double[][] MatrixOfParameterValues { get; }
        public double[][] MatrixOfSimObs { get; } // matrix of simulation observations used for calibratoin 

        public int NumOfCalibratoinTargets { get; } = 0;
        public int NumOfDiscardedTrajs { get; set; } = 0;
        public double TimeUsed { get; set; } = 0;

        public Calibration(Array sheetOfObsdHist)
        {
            // store epidemic history along with the time-specific likelihood parameters
            string strObsValue; string strLikePar;
            for (int colIdx = 3; colIdx < sheetOfObsdHist.GetLength(1); colIdx += 2)
            {
                ObsAndLikelihoodParams calibInfo = new ObsAndLikelihoodParams();
                for (int rowIdx = 1; rowIdx <= sheetOfObsdHist.GetLength(0); rowIdx++)
                {
                    // observation and likelihood parameter
                    strObsValue = Convert.ToString(sheetOfObsdHist.GetValue(rowIdx, colIdx));
                    strLikePar = Convert.ToString(sheetOfObsdHist.GetValue(rowIdx, colIdx+1));
                    calibInfo.AddObsPar(strObsValue, strLikePar);
                }
                _history.Add(calibInfo);
            }
        }
        
        public void AddTargets(List<SumTrajectory> sumTrajs, List<RatioTrajectory> ratioTrajs)
        {
            int i = 0;

            // summation statistics
            foreach (SumTrajectory st in sumTrajs.Where(s => !(s.CalibInfo is null)))
            {
                switch (st.CalibInfo.GoodnessOfFit)
                {
                    // likelihood as measure of fit
                    case SpecialStatCalibrInfo.EnumMeasureOfFit.Likelihood:
                        {
                            switch (st.CalibInfo.LikelihoodFunc)
                            {
                                case SpecialStatCalibrInfo.EnumLikelihoodFunc.Normal:
                                    _likelihoods.Add(new Likelihood_Normal(_history[i]));
                                    break;
                                case SpecialStatCalibrInfo.EnumLikelihoodFunc.Binomial:
                                    _likelihoods.Add(new Likelihood_BinomialIncd(_history[i], st.CalibInfo.LikelihoodParam.Value));
                                    break;                                    
                                case SpecialStatCalibrInfo.EnumLikelihoodFunc.Multinomial:
                                    break;
                            }
                        }
                        break;

                    // fourier as measure of fit
                    case SpecialStatCalibrInfo.EnumMeasureOfFit.Fourier:
                        break;
                }
                ++i;
            }

            // ratio statistics


        }

    }

    public class CalibSimTraj
    {
        public int Itr { get; }
        public int RndSeed { get; }
        public double[] ParamValues { get; }
        public List<TimeSeries> Obs { get; } = new List<TimeSeries>();
        public double[] GoodnessOfFit { get; }

        public CalibSimTraj(int simItr, int rndSeed, double[] paramValues, List<SumTrajectory> sumTrajs, List<RatioTrajectory> ratioTrajs)
        {
            Itr = simItr;
            RndSeed = rndSeed;
            ParamValues = paramValues;

            //foreach (SumTrajectory st in sumTrajs.Where(s=> !(s.CalibInfo is null)))
            //{
            //    switch (st.Type)
            //    {
            //        case SumTrajectory.EnumType.Incidence:
            //            Obs.Add(st.IncidenceTimeSeries);
            //            break;
            //        case SumTrajectory.EnumType.Prevalence:
            //            Obs.Add(st.PrevalenceTimeSeries);
            //            break;
            //        case SumTrajectory.EnumType.AccumulatingIncident:
            //            Obs.Add(st.AccumIncidenceTimeSeries);
            //            break;
            //    }                
            //}
        }
    }

    public class SpecialStatCalibrInfo
    {
        public enum EnumMeasureOfFit : int
        {
            FeasibleRangeOnly = 1,
            Likelihood = 2,
            Fourier = 3,
        }
        public enum EnumLikelihoodFunc: int
        {
            NotSpecified = 0,
            Normal = 1,
            Binomial = 2,
            Multinomial = 3,
        }

        public EnumMeasureOfFit GoodnessOfFit { get; }
        public EnumLikelihoodFunc LikelihoodFunc { get; }
        public int? LikelihoodParam { get; }
        public bool IfCheckWithinFeasibleRange { get; } = false;
        public double LowFeasibleRange { get; }
        public double UpFeasibleRange { get; }

        public SpecialStatCalibrInfo(
            string measureOfFit,
            string likelihoodFunction = "",
            string likelihoodParam = "",
            bool ifCheckWithinFeasibleRange = false, 
            double lowFeasibleBound = 0, 
            double upFeasibleBound = double.MaxValue)
        {
            switch (measureOfFit)
            {
                case "Feasible Range Only":
                    {
                        GoodnessOfFit = EnumMeasureOfFit.FeasibleRangeOnly;
                        if (!ifCheckWithinFeasibleRange)
                            throw new ArgumentException("Inconsistant setting.");                        
                    }
                    break;
                case "Likelihood":
                    {
                        if (likelihoodParam == "")
                            LikelihoodParam = null;
                        else
                            LikelihoodParam = Convert.ToInt32(likelihoodParam);

                        GoodnessOfFit = EnumMeasureOfFit.Likelihood;
                        switch (likelihoodFunction)
                        {
                            case "Normal":
                                LikelihoodFunc = EnumLikelihoodFunc.Normal;
                                break;
                            case "Binomial":
                                LikelihoodFunc = EnumLikelihoodFunc.Binomial;
                                break;
                            case "Multinomial":
                                LikelihoodFunc = EnumLikelihoodFunc.Multinomial;
                                break;
                            default:
                                throw new ArgumentException("Likelihood function not defined.");
                        }
                    }
                    break;
                case "Fourier":
                    {
                        GoodnessOfFit = EnumMeasureOfFit.Fourier;
                    }
                    break;
                default:
                    throw new ArgumentException("Goodness-of-fit not defined.");
            }

            IfCheckWithinFeasibleRange = ifCheckWithinFeasibleRange;
            LowFeasibleRange = lowFeasibleBound;
            UpFeasibleRange = upFeasibleBound;
        }

    }

    public class ObsAndLikelihoodParams
    {
        public List<double?> Obs { get; } = new List<double?>();                // observations (can take null)
        public List<double?> LikelihoodParam { get; } = new List<double?>();    // likelihood settings (can take null)

        public ObsAndLikelihoodParams()
        {                        
        }

        public void AddObsPar(string strObsValue, string strLikePar)
        {
            AddTo(Obs, strObsValue);
            AddTo(LikelihoodParam, strLikePar);
        }

        private void AddTo(List<double?> list, string text)
        {
            if (text == "")
                list.Add(null);
            else
                list.Add(Convert.ToDouble(text));
        }
    }

    ////////// Likelihoods for Calibration Targets //////////

    public abstract class Likelihood
    {
        protected ObsAndLikelihoodParams info;
        protected double LnL = 0;
        protected int n = 0;
        protected double sumLnL = 0;

        public Likelihood(ObsAndLikelihoodParams obsAndLikelihoodParams)
        {
            info = obsAndLikelihoodParams;
        }
        public abstract double LnLikelihood(List<SumTrajectory> sumTrajs, List<RatioTrajectory> ratioTrajs);
    }

    public class Likelihood_Normal : Likelihood
    {
        public Likelihood_Normal(ObsAndLikelihoodParams calibObsAndLikelihoodParams) 
            : base(calibObsAndLikelihoodParams)
        {           
        }

        public double CalculateLnLikelihood(PrevalenceTimeSeries prevTS)
        {
            sumLnL = 0; n = 0;
            // go over calibration observations
            for (int i = 0; i<info.Obs.Count; i++)
                // if an observation is recorded
                if (info.Obs[i].HasValue)
                {
                    double simValue = prevTS.Recordings[i];
                    double stDev = info.LikelihoodParam[i].Value * simValue / 3; // measurement error * simulated prevalence

                    // pdf of normal calcualted at x = observation
                    LnL = MathNet.Numerics.Distributions.Normal.PDFLn(simValue, stDev, info.Obs[i].Value);
                    ++n;
                    sumLnL += LnL;
                }
            return sumLnL/n;
        }
    }

    public class Likelihood_BinomialIncd : Likelihood
    {
        public int DenominatorID { get; }

        public Likelihood_BinomialIncd(ObsAndLikelihoodParams calibObsAndLikelihoodParams, int denominatorID)
            : base(calibObsAndLikelihoodParams)
        {
            DenominatorID = denominatorID;
        }

        public double CalculateLnLikelihood(PrevalenceTimeSeries prevTS, IncidenceTimeSeries incidTS)
        {
            sumLnL = 0; n = 0;
            // go over calibration observations
            for (int i = 0; i < info.Obs.Count; i++)
                // if an observation is recorded
                if (info.Obs[i].HasValue)
                {
                    double simPrev = prevTS.Recordings[i];
                    double simInc = incidTS.Recordings[i];
                    double p = simInc / simPrev;

                    // pdf of binomial calcualted at x = observation
                    LnL = MathNet.Numerics.Distributions.Binomial.PMFLn(p, (int)simPrev, (int)info.Obs[i].Value);
                    ++n;
                    sumLnL += LnL;
                }
            return sumLnL / n;
        }

    }

    public class Likelihood_BinomialPrev : Likelihood
    {
        public Likelihood_BinomialPrev(ObsAndLikelihoodParams calibObsAndLikelihoodParams)
            : base(calibObsAndLikelihoodParams)
        {
        }

        public double CalculateLnLikelihood(PrevalenceTimeSeries prevTS_nomin, PrevalenceTimeSeries prevTS_denom)
        {
            sumLnL = 0; n = 0;
            // go over calibration observations
            for (int i = 0; i < info.Obs.Count; i++)
                // if an observation is recorded
                if (info.Obs[i].HasValue)
                {
                    double simNomin = prevTS_nomin.Recordings[i];
                    double simDenomin = prevTS_denom.Recordings[i];
                    double simPrev = simNomin / simDenomin;
                    int obs = (int)(info.Obs[i].Value * info.LikelihoodParam[i].Value);

                    // pdf of binomial calcualted at x = observation
                    LnL = MathNet.Numerics.Distributions.Binomial.PMFLn(simPrev, (int)simDenomin, obs);
                    ++n;
                    sumLnL += LnL;
                }
            return sumLnL / n;
        }

    }

}
