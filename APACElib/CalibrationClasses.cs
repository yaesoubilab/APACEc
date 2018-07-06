using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APACElib
{
    /// <summary>
    /// store the seed, ln of the likelihood, and the probability of a simulated epidemic
    /// </summary>
    public class ResulOfASimEpi
    {
        public int SimItr { get; }
        public int Seed { get; }
        public double LnL { get; }
        public double Prob { get; set; }

        public ResulOfASimEpi (int simItr, int seed, double lnL)
        {
            SimItr = simItr;
            Seed = seed;
            LnL = lnL;
        }
    }

    /// <summary>
    /// final results of calibration to report to Excel
    /// </summary>
    public class CalibResultsToReportToExcel
    {
        public List<int> SimItrs = new List<int>();
        public List<int> RndSeeds = new List<int>();
        public List<double> Probs = new List<double>();
    }

    /// <summary>
    /// a Bayesian calibration approach
    /// </summary>
    public class Calibration
    {
        private List<ObsAndLikelihoodParams> _obsHist = new List<ObsAndLikelihoodParams>();
        private List<LikelihoodTimeSeries> _likelihoodTSs = new List<LikelihoodTimeSeries>();
        public List<ResulOfASimEpi> SimEpiResults { get; private set; } = new List<ResulOfASimEpi>();
        public List<ResulOfASimEpi> SortedResults;
        public CalibResultsToReportToExcel ResultsForExcel = new CalibResultsToReportToExcel();
        private double _maxLnL=double.MinValue;         // maximum ln(likelihood) recorded
        public int NumOfDiscardedTrajs { get; set; } = 0;
        public double TimeUsed { get; set; } = 0;

        public Calibration(Array sheetOfObsdHist)
        {
            // store epidemic history and the time-specific likelihood parameters
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
                _obsHist.Add(calibInfo);
            }
        }

        public void AddCalibTargets(List<SumTrajectory> sumTrajs, List<RatioTrajectory> ratioTrajs)
        {
            // summation statistics
            int calibTargIdx = 0;
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
                                    _likelihoodTSs.Add(
                                        new LikelihoodTS_Normal(_obsHist[calibTargIdx], st.ID)
                                        );
                                    break;
                                case SpecialStatCalibrInfo.EnumLikelihoodFunc.Binomial:
                                    _likelihoodTSs.Add(
                                        new LikelihoodTS_BinomialIncd(
                                            _obsHist[calibTargIdx], st.ID, st.CalibInfo.LikelihoodParam.Value)
                                        );
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
                ++calibTargIdx;
            }

            // ratio statistics
            foreach (RatioTrajectory rt in ratioTrajs.Where(s => !(s.CalibInfo is null)))
            {
                switch (rt.CalibInfo.GoodnessOfFit)
                {
                    // likelihood as measure of fit
                    case SpecialStatCalibrInfo.EnumMeasureOfFit.Likelihood:
                        {
                            switch (rt.CalibInfo.LikelihoodFunc)
                            {
                                case SpecialStatCalibrInfo.EnumLikelihoodFunc.Binomial:
                                    _likelihoodTSs.Add(
                                        new LikelihoodTS_BinomialPrev(
                                            _obsHist[calibTargIdx],
                                            rt.NominatorSpecialStatID,
                                            rt.DenominatorSpecialStatID)
                                        );
                                    break;
                                case SpecialStatCalibrInfo.EnumLikelihoodFunc.Multinomial:
                                    break;
                            }
                        }
                        break;
                }
            }
        }

        public void CalculateLnL(Epidemic epi)
        {            
            if (epi.SeedProducedAcceptibleTraj == -1)
                epi.LnL = double.MinValue;

            epi.LnL=0;
            foreach (LikelihoodTimeSeries L in _likelihoodTSs)
                epi.LnL += L.LnLikelihood(epi.EpiHist.SumTrajs, epi.EpiHist.RatioTrajs);
        }
        
        public void AddCalibSummary(Epidemic epi)
        {
            // update calibration time and trajectories discarded
            TimeUsed += epi.Timer.TimePassed;
            NumOfDiscardedTrajs += epi.SeedsDiscarded;

            if (epi.SeedProducedAcceptibleTraj != -1)
            {  
                // store the summary of likelihood calculation
                SimEpiResults.Add(
                    new ResulOfASimEpi(
                        epi.ID, epi.SeedProducedAcceptibleTraj, epi.LnL)
                        );
                // update the maximum LnL
                if (epi.LnL > _maxLnL)
                    _maxLnL = epi.LnL;
            }
        }

        public void CalcProbsAndSort()
        {
            // adjust likelihoods
            double sum =0;
            foreach (ResulOfASimEpi s in SimEpiResults)
            {
                s.Prob = Math.Exp(s.LnL - _maxLnL);
                sum += s.Prob;
            }
            // calculate probabilities
            foreach (ResulOfASimEpi s in SimEpiResults)
                s.Prob /= sum;

            // sort
            SortedResults = SimEpiResults.OrderByDescending(o => o.Prob).ToList();

            // prepare results for Excel
            foreach (ResulOfASimEpi s in SortedResults)
            {
                ResultsForExcel.SimItrs.Add(s.SimItr);
                ResultsForExcel.RndSeeds.Add(s.Seed);
                ResultsForExcel.Probs.Add(s.Prob);
            }
        }
    }

    /// <summary>
    /// class to store the calibration settings of a calibration target
    /// </summary>
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

    /// <summary>
    /// store the observations and likelihood settings for a calibration target
    /// </summary>
    public class ObsAndLikelihoodParams
    {
        public List<double?> Obs { get; } = new List<double?>();              // observations (can take null)
        public List<double?> LikelihoodParam { get; } = new List<double?>();  // likelihood settings (can take null)

        public ObsAndLikelihoodParams() { }

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

    
    /// <summary>
    /// abstract class to calculate ln(likelihood) of an observed time-series
    /// </summary>
    public abstract class LikelihoodTimeSeries
    {
        protected ObsAndLikelihoodParams info;
        protected double LnL = 0;
        protected int n = 0;
        protected double sumLnL = 0;

        public LikelihoodTimeSeries(ObsAndLikelihoodParams obsAndLikelihoodParams)
        {
            info = obsAndLikelihoodParams;
        }
        public abstract double LnLikelihood(List<SumTrajectory> sumTrajs, List<RatioTrajectory> ratioTrajs);
    }

    /// <summary>
    /// class to calculate ln(likelihood) of an observed time-series when observations follow normal distributions
    /// </summary>
    public class LikelihoodTS_Normal : LikelihoodTimeSeries
    {
        public int IndexOfSumStat_Prev { get; }

        public LikelihoodTS_Normal(ObsAndLikelihoodParams calibObsAndLikelihoodParams, int indexOfSumStat_Prev) 
            : base(calibObsAndLikelihoodParams)
        {
            IndexOfSumStat_Prev = indexOfSumStat_Prev;
        }

        public override double LnLikelihood(List<SumTrajectory> sumTrajs, List<RatioTrajectory> ratioTrajs)
        {
            sumLnL = 0; n = 0;
            // go over calibration observations
            for (int i = 0; i<info.Obs.Count; i++)
                // if an observation is recorded
                if (info.Obs[i].HasValue)
                {
                    double simValue = sumTrajs[IndexOfSumStat_Prev].PrevalenceTimeSeries.Recordings[i].Value;
                    double stDev = info.LikelihoodParam[i].Value * simValue / 3; // measurement error * simulated prevalence

                    // pdf of normal calcualted at x = observation
                    LnL = MathNet.Numerics.Distributions.Normal.PDFLn(simValue, stDev, info.Obs[i].Value);
                    ++n;
                    sumLnL += LnL;
                }
            return sumLnL/n;
        }
    }

    /// <summary>
    /// class to calculate ln(likelihood) of an observed time-series when incidence observations follow binomial distributions
    /// </summary>
    public class LikelihoodTS_BinomialIncd : LikelihoodTimeSeries
    {
        public int IndexOfSumStat_Prev { get; }
        public int IndexOfSumStat_Incd { get; }

        public LikelihoodTS_BinomialIncd(
            ObsAndLikelihoodParams calibObsAndLikelihoodParams,
            int indexOfSumStat_Incd,
            int indexOfSumStat_Prev)
            : base(calibObsAndLikelihoodParams)
        {
            IndexOfSumStat_Incd = indexOfSumStat_Incd;
            IndexOfSumStat_Prev = indexOfSumStat_Prev;
        }

        public override double LnLikelihood(List<SumTrajectory> sumTrajs, List<RatioTrajectory> ratioTrajs)
        {
            sumLnL = 0; n = 0;
            // go over calibration observations
            for (int i = 0; i < info.Obs.Count; i++)
                // if an observation is recorded
                if (info.Obs[i].HasValue)
                {
                    double simPrev = sumTrajs[IndexOfSumStat_Prev].PrevalenceTimeSeries.Recordings[i-1].Value;
                    double simInc = sumTrajs[IndexOfSumStat_Incd].IncidenceTimeSeries.Recordings[i-1].Value;
                    double p = Math.Min(simInc / simPrev, 1);

                    // pdf of binomial calcualted at x = observation
                    LnL = MathNet.Numerics.Distributions.Binomial.PMFLn(p, (int)simPrev, (int)info.Obs[i].Value);
                    ++n;
                    sumLnL += LnL;
                }
            return sumLnL / n;
        }
    }

    /// <summary>
    /// class to calculate ln(likelihood) of an observed time-series when prevalence observations follow binomial distributions
    /// </summary>
    public class LikelihoodTS_BinomialPrev : LikelihoodTimeSeries
    {
        public int IndexOfSumStat_PrevNomin { get; }
        public int IndexOfSumStat_PrevDenom { get; }

        public LikelihoodTS_BinomialPrev(ObsAndLikelihoodParams calibObsAndLikelihoodParams,
            int indexOfSumStat_PrevNomin,
            int indexOfSumStat_PrevDenom)
            : base(calibObsAndLikelihoodParams)
        {
            IndexOfSumStat_PrevNomin = indexOfSumStat_PrevNomin;
            IndexOfSumStat_PrevDenom = indexOfSumStat_PrevDenom;
        }

        public override double LnLikelihood(List<SumTrajectory> sumTrajs, List<RatioTrajectory> ratioTrajs)
        {
            sumLnL = 0; n = 0;
            // go over calibration observations
            for (int i = 0; i < info.Obs.Count; i++)
                // if an observation is recorded
                if (info.Obs[i].HasValue)
                {
                    double simNomin = sumTrajs[IndexOfSumStat_PrevNomin].PrevalenceTimeSeries.Recordings[i].Value;
                    double simDenomin = sumTrajs[IndexOfSumStat_PrevDenom].PrevalenceTimeSeries.Recordings[i].Value;
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
