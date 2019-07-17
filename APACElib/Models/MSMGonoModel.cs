using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APACElib;
using ComputationLib;
using RandomVariateLib;

namespace APACElib.Models
{

    public class MSMGonoModel : GonoModel
    {       
        public MSMGonoModel() : base()
        {
           
        }

        public override void BuildModel()
        {
            _specialStatInfo.Reset(1);

            List<string> regions = new List<string>() { "MSM" };

            // add the parameters from the parameter sheet
            AddParameters();
            // add gono parameters 
            AddGonoParameters(regions);
            // add classes
            AddGonoClasses(regions);
            // add events
            AddGonoEvents(regions);
            // add interventions
            AddGonoInterventions(regions);
            // add summation statistics
            AddGonoSumStats(regions);
            // add ratio statistics
            AddGonoRatioStatistics(regions);
            // add features
            AddGonoFeatures(regions);
            // add conditions
            AddGonoConditions();
            // add connections
            AddGonoConnections(regions);
        }

        private void AddGonoEvents(List<string> regions)
        {
            int id = 0;
            int inf = 0;
            int regionID = 0;
            string eventName = "";
            // rates
            int infRate = _paramManager.Dic["Dummy Inf"];
            int birthRate = _paramManager.Dic["Annual birth rate (per pop member)"];
            int deathRate = _paramManager.Dic["Annual death rate"];
            int naturalRecoveryRate = _paramManager.Dic["Natural recovery"];
            int screeningRate = _paramManager.Dic["Annual screening rate"];
            int seekingTreatmentRate = _paramManager.Dic["Annual rate of seeking treatment (symptomatic)"];
            int seekingReTreatmentRate = _paramManager.Dic["Annual rate of retreatment"];

            int idS = _dicClasses[regions[0] + " | S"];
            int idDeath = _dicClasses[regions[0] + " | Death"];
            int idSuccessM1 = _dicClasses[regions[0] + " | Success with " + Ms.M1.ToString()];
            int idSuccessM2 = _dicClasses[regions[0] + " | Success with " + Ms.M2.ToString()];

            // create a list of main compartments: S, I
            List<string> mainComp = new List<string>();
            mainComp.Add("S");
            for (inf = 0; inf < _infProfiles.Count; inf++)
                mainComp.Add("I | " + _infProfiles[inf]);

            // add Birth events
            foreach (string comp in mainComp)
            {
                for (regionID = 0; regionID < regions.Count; regionID++)
                { 
                    eventName = regions[regionID] + " | Birth | " + comp;
                    _events.Add(new Event_Birth(
                        name: eventName,
                        ID: id,
                        IDOfActivatingIntervention: 0,
                        rateParameter: _paramManager.Parameters[birthRate],
                        IDOfDestinationClass: idS + regionID)
                        );
                    _dicEvents[eventName] = id++;
                }
            }
            // add Death events
            foreach (string comp in mainComp)
            {
                for (regionID = 0; regionID < regions.Count; regionID++)
                {
                    eventName = regions[regionID] + " | Death | " + comp;
                    _events.Add(new Event_EpidemicIndependent(
                        name: eventName,
                        ID: id,
                        IDOfActivatingIntervention: 0,
                        rateParameter: _paramManager.Parameters[deathRate],
                        IDOfDestinationClass: idDeath + regionID)
                        );
                    _dicEvents[eventName] = id++;
                }
            }

            // add Infection events
            int idIfSympG_0 = _dicClasses[regions[0] + " | If Sym | G_0"];
            int i = 0;
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
            {
                for (regionID = 0; regionID < regions.Count; regionID++)
                {
                    eventName = regions[regionID] + " | Infection | " + r.ToString();
                    _events.Add(new Event_EpidemicDependent(
                        name: eventName,
                        ID: id,
                        IDOfActivatingIntervention: 0,
                        IDOfPathogenToGenerate: (int)r,
                        IDOfDestinationClass: idIfSympG_0 + i++)
                        );
                    _dicEvents[eventName] = id++;
                }
            }

            // add Natual Recovery events
            inf = 0;
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    for (regionID = 0; regionID < regions.Count; regionID++)
                    {                        
                        eventName = regions[regionID] + " | Natural Recovery | I | " + _infProfiles[inf];
                        _events.Add(new Event_EpidemicIndependent(
                            name: eventName,
                            ID: id,
                            IDOfActivatingIntervention: 0,
                            rateParameter: _paramManager.Parameters[naturalRecoveryRate],
                            IDOfDestinationClass: idS + regionID)
                        );
                        _dicEvents[eventName] = id++;
                    }
                    inf++;
                }

            // add Seeking Treatment events
            int idWSymG_0 = _dicClasses[regions[0] + " | W | Sym | G_0"];
            inf = 0;
            i = 0;
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    for (regionID = 0; regionID < regions.Count; regionID++)
                    {
                        eventName = regions[regionID] + " | Seeking Treatment | I | " + _infProfiles[inf];
                        _events.Add(new Event_EpidemicIndependent(
                            name: eventName,
                            ID: id,
                            IDOfActivatingIntervention: 0,
                            rateParameter: (s == SymStates.Sym) ? _paramManager.Parameters[seekingTreatmentRate] : _paramManager.Parameters[(int)DummyParam.D_0],
                            IDOfDestinationClass: idWSymG_0 + i++)
                        );
                        _dicEvents[eventName] = id++;
                    }
                    inf++;
                }

            // add Screening events
            inf = 0;
            i = 0;
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    for (regionID = 0; regionID < regions.Count; regionID++)
                    {
                        eventName = regions[regionID] + " | Screening | I | " + _infProfiles[inf];
                        _events.Add(new Event_EpidemicIndependent(
                            name: eventName,
                            ID: id,
                            IDOfActivatingIntervention: 0,
                            rateParameter: _paramManager.Parameters[screeningRate],
                            IDOfDestinationClass: idWSymG_0 + i++)
                        );
                        _dicEvents[eventName] = id++;
                    }
                    inf++;
                }
            // add First-Line Treatment with A1 and B1
            foreach (Drugs d in Enum.GetValues(typeof(SymStates)))
                if (d == Drugs.A1 || d == Drugs.B1)
                {
                    inf = 0;
                    foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                        foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                        {
                            for (regionID = 0; regionID < regions.Count; regionID++)
                            {
                                string resistOrFail = GetResistOrFail(resistStat: r, drug: d);
                                string treatmentProfile = resistOrFail + " | " + d.ToString() + " --> I | " + _infProfiles[inf];
                                eventName = regions[regionID] + " | Tx_" + d.ToString() + " | W | " + _infProfiles[inf];
                                string destClassName = "";

                                if (resistOrFail == "F")
                                    destClassName = regions[regionID] + " | If retreat " + treatmentProfile;
                                else
                                    destClassName = regions[regionID] + " | If " + treatmentProfile;

                                _events.Add(new Event_EpidemicIndependent(
                                    name: eventName,
                                    ID: id,
                                    IDOfActivatingIntervention: (d == Drugs.A1) ? (int)Interventions.A1 : (int)Interventions.B1,
                                    rateParameter: _paramManager.Parameters[infRate], // infinity rate
                                    IDOfDestinationClass: _dicClasses[destClassName])
                                    );
                                _dicEvents[eventName] = id++;
                            }
                            inf++;
                        }
                }

            // add First-Line Treatment with M1
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    string infProfile = s.ToString() + " | " + r.ToString();
                    eventName = regions[0] + " | Tx_M1 | W | " + infProfile;
                    _events.Add(new Event_EpidemicIndependent(
                        name: eventName,
                        ID: id,
                        IDOfActivatingIntervention: (int)Interventions.M1,
                        rateParameter: _paramManager.Parameters[infRate],
                        IDOfDestinationClass: idSuccessM1)
                        );
                    _dicEvents[eventName] = id++;
                }

            // add Second-Line Treatment with B2            
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    string resistOrFail = GetResistOrFail(resistStat: r, drug: Drugs.B2);
                    string infProfile = s.ToString() + " | " + r.ToString();
                    string treatmentProfile = resistOrFail + " | B2 --> I | " + infProfile;
                    eventName = regions[0] + " | Tx_B2 | U | " + infProfile;

                    string destClassName = "";
                    if (resistOrFail == "F")
                        // if treatment failure occurs, the patient will receive M2 
                        destClassName = regions[0] + " | Success with M2";
                    else
                        destClassName = regions[0] + " | If " + treatmentProfile;

                    _events.Add(new Event_EpidemicIndependent(
                        name: eventName,
                        ID: id,
                        IDOfActivatingIntervention: (r == ResistStates.G_A) ? (int)Interventions.B2_A : 1,
                        rateParameter: _paramManager.Parameters[seekingReTreatmentRate],
                        IDOfDestinationClass: _dicClasses[destClassName])
                        );
                    _dicEvents[eventName] = id++;
                }

            // add Second-Line Treatment with M2              
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    string infProfile = s.ToString() + " | " + r.ToString();
                    eventName = regions[0] + " | Tx_M2 | U | " + infProfile;
                    _events.Add(new Event_EpidemicIndependent(
                        name: eventName,
                        ID: id,
                        IDOfActivatingIntervention: (r == ResistStates.G_A) ? (int)Interventions.M2_A : (int)Interventions.M2_B_AB,
                        rateParameter: (r == ResistStates.G_A) ? _paramManager.Parameters[(int)DummyParam.D_0] : _paramManager.Parameters[seekingReTreatmentRate],
                        IDOfDestinationClass: idSuccessM2)
                        );
                    _dicEvents[eventName] = id++;
                }

            // add Leaving Success with A1, B1, or B2
            foreach (Drugs d in Enum.GetValues(typeof(Drugs)))
            {
                eventName = regions[0] + " | Leaving Success with " + d.ToString();
                _events.Add(new Event_EpidemicIndependent(
                    name: eventName,
                    ID: id,
                    IDOfActivatingIntervention: 0, // always on
                    rateParameter: _paramManager.Parameters[(int)DummyParam.D_Inf],
                    IDOfDestinationClass: 0) // back to S
                    );
                _dicEvents[eventName] = id++;
            }
            // add Leaving Success with M1 or M2
            foreach (Ms m in Enum.GetValues(typeof(Ms)))
            {
                eventName = regions[0] + " | Leaving Success with " + m.ToString();
                _events.Add(new Event_EpidemicIndependent(
                    name: eventName,
                    ID: id,
                    IDOfActivatingIntervention: 0, // always on
                    rateParameter: _paramManager.Parameters[(int)DummyParam.D_Inf],
                    IDOfDestinationClass: 0) // back to S
                    );
                _dicEvents[eventName] = id++;
            }
        }

        private void AddGonoInterventions(List<string> regions)
        {
            AddInterventions();

            int id = _decisionMaker.Interventions.Count();
            int i = 0;

            // add interventions
            foreach (Interventions intrv in Enum.GetValues(typeof(Interventions)))
            {
                int conditionIDToTurnOn = 0, conditionIDToTurnOff = 0;
                switch (intrv)
                {
                    case Interventions.A1:
                        {
                            conditionIDToTurnOn = (int)Conditions.AOn;
                            conditionIDToTurnOff = (int)Conditions.AOff;
                        }
                        break;
                    case Interventions.B1:
                        {
                            conditionIDToTurnOn = (int)Conditions.BOn;
                            conditionIDToTurnOff = (int)Conditions.BOff;
                        }
                        break;
                    case Interventions.M1:
                        {
                            conditionIDToTurnOn = (int)Conditions.MOn;
                            conditionIDToTurnOff = (int)Conditions.MOff;
                        }
                        break;
                    case Interventions.B2_A:
                        {
                            conditionIDToTurnOn = (int)Conditions.AOn;
                            conditionIDToTurnOff = (int)Conditions.BOff;
                        }
                        break;
                    case Interventions.M2_A:
                        {
                            conditionIDToTurnOn = (int)Conditions.MOn;
                            conditionIDToTurnOff = (int)Conditions.MOff;
                        }
                        break;
                }

                // decision rule 
                DecisionRule simDecisionRule = null;
                if (intrv == Interventions.M2_B_AB)
                    simDecisionRule = new DecionRule_Predetermined(predeterminedSwitchValue: 1);
                else
                    simDecisionRule = new DecisionRule_ConditionBased(
                        conditions: _epiHist.Conditions,
                        conditionIDToTurnOn: conditionIDToTurnOn,
                        conditionIDToTurnOff: conditionIDToTurnOff);

                // intervention
                _decisionMaker.AddAnIntervention(
                    new Intervention(
                        index: id++,
                        name: regions[0] + " | " + intrv.ToString(),
                        actionType: EnumInterventionType.Additive,
                        affectingContactPattern: false,
                        timeIndexBecomesAvailable: 0,
                        timeIndexBecomesUnavailable: _modelSets.TimeIndexToStop,
                        parIDDelayToGoIntoEffectOnceTurnedOn: 0,
                        decisionRule: simDecisionRule));

                i++;
            }
        }



        private void AddGonoConnections(List<string> regions)
        {
            int i = 0;
            int birthID = _dicEvents[regions[0] + " | Birth | S"];
            int deathID = _dicEvents[regions[0] + " | Death | S"];
            int infectionID = _dicEvents[regions[0] + " | Infection | G_0"];
            int naturalRecoveryID = _dicEvents[regions[0] + " | Natural Recovery | I | Sym | G_0"];
            int seekingTreatmentID = _dicEvents[regions[0] + " | Seeking Treatment | I | Sym | G_0"];
            int screeningID = _dicEvents[regions[0] + " | Screening | I | Sym | G_0"];
            int txA = _dicEvents[regions[0] + " | Tx_A1 | W | " + _infProfiles[0]];
            int txB = _dicEvents[regions[0] + " | Tx_B1 | W | " + _infProfiles[0]];
            int txM = _dicEvents[regions[0] + " | Tx_M1 | W | " + _infProfiles[0]];
            int txB2 = _dicEvents[regions[0] + " | Tx_B2 | U | " + _infProfiles[0]];
            int txM2 = _dicEvents[regions[0] + " | Tx_M2 | U | " + _infProfiles[0]];
            int leaveSuccess = _dicEvents[regions[0] + " | Leaving Success with A1"];
            int success = _dicClasses[regions[0] + " | Success with A1"];

            // ----------------
            // add events for S
            Class_Normal S = (Class_Normal)_classes[_dicClasses[regions[0] + " | S"]];
            // birth and death
            S.AddAnEvent(_events[birthID]);
            S.AddAnEvent(_events[deathID]);
            // infections
            i = 0;
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                S.AddAnEvent(_events[infectionID + i++]);

            // ----------------
            // add events for I, W, U
            i = 0;
            int w = 0, u = 0;
            foreach (Class c in _classes.Where(c => (c is Class_Normal)))
            {
                // for I
                if (c.Name.StartsWith(regions[0] + " | I"))
                {
                    ((Class_Normal)c).AddAnEvent(_events[birthID + i + 1]);
                    ((Class_Normal)c).AddAnEvent(_events[deathID + i + 1]);
                    ((Class_Normal)c).AddAnEvent(_events[naturalRecoveryID + i]);
                    ((Class_Normal)c).AddAnEvent(_events[seekingTreatmentID + i]);
                    ((Class_Normal)c).AddAnEvent(_events[screeningID + i]);
                    ++i;
                }
                // for W
                else if (c.Name.StartsWith(regions[0] + " | W "))
                {
                    ((Class_Normal)c).AddAnEvent(_events[txA + w]);
                    ((Class_Normal)c).AddAnEvent(_events[txB + w]);
                    ((Class_Normal)c).AddAnEvent(_events[txM + w]);
                    ++w;
                }
                else if (c.Name.StartsWith(regions[0] + " | U"))
                {
                    ((Class_Normal)c).AddAnEvent(_events[txB2 + u]);
                    ((Class_Normal)c).AddAnEvent(_events[txM2 + u]);
                    ++u;
                }
            }

            // add leaving success with A1, B1, B2, M1, M2
            for (int j = 0; j < 5; j++)
                ((Class_Normal)_classes[success + j]).AddAnEvent(_events[leaveSuccess + j]);
        }

       

        private void AddGonoConditions()
        {
            int id = 0;
            EnumSign[] signs;
            List<Parameter> thresholdParams = new List<Parameter>{
                _paramManager.Parameters[(int)DummyParam.T_Prev],
                _paramManager.Parameters[(int)DummyParam.T_DeltaPrev]};
            List<Parameter> thresholdParams0 = new List<Parameter> {
                _paramManager.Parameters[(int)DummyParam.D_0] };
            List<Parameter> thresholdParams00 = new List<Parameter> {
                _paramManager.Parameters[(int)DummyParam.D_0],
                _paramManager.Parameters[(int)DummyParam.D_0] };

            // out condition for A, B, or both
            signs = new EnumSign[2] { EnumSign.q, EnumSign.q };
            for (int i = 0; i < 3; i++)
                _epiHist.Conditions.Add(new Condition_OnFeatures(
                    id: id++,
                    features: new List<Feature> {
                        _epiHist.Features[_featureIDs[(int)Features.PercResist] + i],
                        _epiHist.Features[_featureIDs[(int)Features.ChangeInPercResist] + i] },
                    thresholdParams: thresholdParams,
                    signs: signs,
                    conclusion: EnumAndOr.Or));

            // ok condition for A, B, or both
            signs = new EnumSign[2] { EnumSign.le, EnumSign.le };
            for (int i = 0; i < 3; i++)
                _epiHist.Conditions.Add(new Condition_OnFeatures(
                    id: id++,
                    features: new List<Feature> {
                        _epiHist.Features[_featureIDs[(int)Features.PercResist] + i],
                        _epiHist.Features[_featureIDs[(int)Features.ChangeInPercResist] + i] },
                    thresholdParams: thresholdParams,
                    signs: signs,
                    conclusion: EnumAndOr.And));

            // B is never used
            _epiHist.Conditions.Add(new Condition_OnFeatures(
                id: id++,
                features: new List<Feature> {
                        _epiHist.Features[_featureIDs[(int)Features.IfEverUsed] + 1] },
                signs: new EnumSign[1] { EnumSign.e },
                thresholdParams: thresholdParams0,
                conclusion: EnumAndOr.And));

            // M1 is neer used
            thresholdParams = new List<Parameter> { _paramManager.Parameters[(int)DummyParam.D_0] };
            _epiHist.Conditions.Add(new Condition_OnFeatures(
                id: id++,
                features: new List<Feature> {
                        _epiHist.Features[_featureIDs[(int)Features.IfEverUsed] + 2] },
                signs: new EnumSign[1] { EnumSign.e },
                thresholdParams: thresholdParams0,
                conclusion: EnumAndOr.And));

            // turn on A
            _epiHist.Conditions.Add(new Condition_OnFeatures(
                id: id++,
                features: new List<Feature> {
                    _epiHist.Features[_featureIDs[(int)Features.Time]],
                    _epiHist.Features[_featureIDs[(int)Features.IfEverUsed]] },
                signs: new EnumSign[2] { EnumSign.qe, EnumSign.e },
                thresholdParams: thresholdParams00,
                conclusion: EnumAndOr.And));

            // turn off A
            _epiHist.Conditions.Add(new Condition_OnConditions(
                id: id++,
                conditions: new List<Condition> {
                    _epiHist.Conditions[(int)Conditions.AOut],
                    _epiHist.Conditions[(int)Conditions.ABOut] },
                conclusion: EnumAndOr.Or));

            // turn on B
            _epiHist.Conditions.Add(new Condition_OnConditions(
                id: id++,
                conditions: new List<Condition> {
                    _epiHist.Conditions[(int)Conditions.AOut],
                    _epiHist.Conditions[(int)Conditions.BOk],
                    _epiHist.Conditions[(int)Conditions.ABOk],
                    _epiHist.Conditions[(int)Conditions.BNeverUsed],
                    _epiHist.Conditions[(int)Conditions.MNeverUsed]},
                conclusion: EnumAndOr.And));

            // turn off B
            _epiHist.Conditions.Add(new Condition_OnConditions(
                id: id++,
                conditions: new List<Condition> {
                    _epiHist.Conditions[(int)Conditions.BOut],
                    _epiHist.Conditions[(int)Conditions.ABOut] },
                conclusion: EnumAndOr.Or));

            // turn on M
            _epiHist.Conditions.Add(new Condition_OnConditions(
                id: id,
                conditions: new List<Condition> {
                    _epiHist.Conditions[id - 1] },
                conclusion: EnumAndOr.And));
            id++;

            // turn off M
            _epiHist.Conditions.Add(new Condition_AlwaysFalse(id: id++));

        }

        
    }
}
