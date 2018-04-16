using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APACElib
{
    public class DeltaTCostHealth
    {
        public double DeltaTCost { get; set; }
        public double DeltaTDALY { get; set; }

        bool _ifCollecting;
        double _warmUpSimIndex;
        double _DALYPerNewMember;
        double _costPerNewMember;
        double _disabilityWeightPerDeltaT;
        double _costPerDeltaT;

        public DeltaTCostHealth(
            int warmUpSimIndex,
            double DALYPerNewMember,
            double costPerNewMember,
            double disabilityWeightPerDeltaT,
            double costPerDeltaT)
        {
            // find if cost and health outcomes should be collected
            double m = new[] { DALYPerNewMember, costPerNewMember, disabilityWeightPerDeltaT, costPerDeltaT }.Max();
            if (m > 0) _ifCollecting = true;

            _warmUpSimIndex = warmUpSimIndex;
            _DALYPerNewMember = DALYPerNewMember;
            _costPerNewMember = costPerNewMember;
            _disabilityWeightPerDeltaT = disabilityWeightPerDeltaT;
            _costPerDeltaT = costPerDeltaT;
        }

        public void Update(int simIndex, double prevalence, double incidence)
        {
            if (_ifCollecting && simIndex >= _warmUpSimIndex)
            {
                DeltaTCost = _costPerNewMember * incidence + _costPerDeltaT * prevalence;
                DeltaTDALY = _DALYPerNewMember * incidence + _disabilityWeightPerDeltaT * prevalence;
            }
        }

        public void Reset()
        {
            DeltaTCost = 0;
            DeltaTDALY = 0;
        }
    }

    public class EpidemicCostHealth
    {
        public double TotalDisountedCost { get; set; }
        public double TotalDiscountedDALY { get; set; }
        private double _deltaTCost;
        private double _deltaTDALY;
        private int _currentSimIndex;

        double _deltaTDiscountRate;
        double _warmUpSimIndex;

        public EpidemicCostHealth(double deltaTDiscountRate, int warmUpSimIndex)
        {
            _deltaTDiscountRate = deltaTDiscountRate;
            _warmUpSimIndex = warmUpSimIndex;
            _currentSimIndex = 0;
        }

        public void Add(int simIndex, double deltaTCost, double deltaTDALY)
        {
            if (simIndex >= _warmUpSimIndex)
            {
                if (simIndex > _currentSimIndex)
                {
                    TotalDisountedCost += _deltaTCost / Math.Pow(1 + _deltaTDiscountRate, simIndex - _warmUpSimIndex);
                    TotalDiscountedDALY += _deltaTDALY / Math.Pow(1 + _deltaTDiscountRate, simIndex - _warmUpSimIndex);

                    _currentSimIndex = simIndex;
                    _deltaTCost = deltaTCost;
                    _deltaTDALY = deltaTDALY;
                }
                else
                {
                    _deltaTCost += deltaTCost;
                    _deltaTDALY += deltaTDALY;
                }
            }
        }

        public double GetEquivalentAnnualCost(double annualDiscountRate, int currentYear, int warmUpYear)
        {
            if (annualDiscountRate == 0)
                return TotalDisountedCost / (currentYear - warmUpYear);
            else
                return annualDiscountRate * TotalDisountedCost / (1 - Math.Pow(1 + annualDiscountRate, -(currentYear - warmUpYear)));
        }
        public double GetEquivalentAnnualDALY(double annualDiscountRate, int currentYear, int warmUpYear)
        {
            if (annualDiscountRate == 0)
                return TotalDiscountedDALY / (currentYear - warmUpYear);
            else
                return annualDiscountRate * TotalDiscountedDALY / (1 - Math.Pow(1 + annualDiscountRate, -(currentYear - warmUpYear)));
        }

        public double GetDiscountedNMB(double wtp)
        {
            return -TotalDisountedCost - wtp * TotalDiscountedDALY;
        }
        public double GetDiscountedNHB(double wtp)
        {
            return -TotalDisountedCost / wtp - TotalDiscountedDALY;
        }

        public void Reset()
        {
            _currentSimIndex = 0;
            _deltaTDALY = 0;
            _deltaTCost = 0;
            TotalDisountedCost = 0;
            TotalDiscountedDALY = 0;
        }
    }
}
