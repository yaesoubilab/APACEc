using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimulationLib;

namespace APACElib
{
    // class for collecting cost and health outcomes over deltaT
    public class DeltaTCostHealth
    {
        public double DeltaTCost { get; set; }
        public double DeltaTDALY { get; set; }

        bool _ifCollecting;
        double _deltaT;
        double _warmUpSimIndex;
        Parameter _DALYPerNewMember;
        Parameter _costPerNewMember;
        Parameter _disabilityWeightPerUnitOfTime;
        Parameter _costPerUnitOfTime;

        public DeltaTCostHealth(
            double deltaT,
            int warmUpSimIndex,
            Parameter DALYPerNewMember,
            Parameter costPerNewMember,
            Parameter disabilityWeightPerUnitOfTime = null,
            Parameter costPerUnitOfTime = null)
        {            
            _ifCollecting = true;
            _deltaT = deltaT;
            _warmUpSimIndex = warmUpSimIndex;
            // store parameters
            _DALYPerNewMember = DALYPerNewMember;
            _costPerNewMember = costPerNewMember;
            if (disabilityWeightPerUnitOfTime is null)
                _disabilityWeightPerUnitOfTime 
                    = new IndependetParameter(0, "dummy", RandomVariateLib.EnumRandomVariates.Constant, 0, 0, 0, 0);
            else
                _disabilityWeightPerUnitOfTime = disabilityWeightPerUnitOfTime;
            if (costPerUnitOfTime is null)
                _costPerUnitOfTime 
                    = new IndependetParameter(0, "dummy", RandomVariateLib.EnumRandomVariates.Constant, 0, 0, 0, 0);
            else
                _costPerUnitOfTime = disabilityWeightPerUnitOfTime;
        }

        public void Update(int simIndex, double prevalence, double incidence)
        {
            if (_ifCollecting && simIndex >= _warmUpSimIndex)
            {
                DeltaTCost = _costPerNewMember.Value * incidence + _costPerUnitOfTime.Value * _deltaT * prevalence;
                DeltaTDALY = _DALYPerNewMember.Value * incidence + _disabilityWeightPerUnitOfTime.Value * _deltaT * prevalence;
            }
        }

        public void Reset()
        {
            DeltaTCost = 0;
            DeltaTDALY = 0;
        }
    }

    // class for collecting discounted cost and health outcomes over the epidemic
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
                // if we have moved to a new deltaT
                if (simIndex > _currentSimIndex)
                {
                    // discount health and cost outcomes over the past deltaT
                    UpdateDiscountedOutcomes(simIndex);
                    _currentSimIndex = simIndex;
                    // reset the accumulated cost and health outcomes (over the next deltaT)
                    _deltaTCost = deltaTCost;
                    _deltaTDALY = deltaTDALY;
                }
                else
                {
                    // accumulate health and cost outcomes over the current deltaT
                    _deltaTCost += deltaTCost;
                    _deltaTDALY += deltaTDALY;
                }
            }
        }
        
        public void UpdateDiscountedOutcomes(int simIndex)
        {
            if (_deltaTDiscountRate <= 0)
            {
                TotalDisountedCost += _deltaTCost;
                TotalDiscountedDALY += _deltaTDALY;
            }
            else
            {
                TotalDisountedCost += _deltaTCost / Math.Pow(1 + _deltaTDiscountRate, simIndex - _warmUpSimIndex);
                TotalDiscountedDALY += _deltaTDALY / Math.Pow(1 + _deltaTDiscountRate, simIndex - _warmUpSimIndex);
            }
        }

        public double GetEquivalentAnnualCost(double annualDiscountRate, int warmUpYear, int currentYear)
        {
            if (annualDiscountRate == 0)
                return TotalDisountedCost / (currentYear - warmUpYear);
            else
                return annualDiscountRate * TotalDisountedCost / (1 - Math.Pow(1 + annualDiscountRate, -(currentYear - warmUpYear)));
        }
        public double GetEquivalentAnnualDALY(double annualDiscountRate, int warmUpYear, int currentYear)
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
            double value = 0;
            if (wtp > 0)
                value = -TotalDisountedCost / wtp - TotalDiscountedDALY;
            else
                value = double.MinValue;
            return value;
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
