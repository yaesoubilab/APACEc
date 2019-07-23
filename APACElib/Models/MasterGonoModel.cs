using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APACElib;
using ComputationLib;
using RandomVariateLib;
using APACElib.Models;

namespace APACElib
{
    public abstract class GonoModel : ModelInstruction
    {
        protected enum Comparts { I, W, U }; // infection, waiting for treatment, waiting for retreatment 
        protected enum Drugs { A1, B1, B2 }; // 1st line treatment with A, 1st line treatment with B, and 2nd line treatment with B
        protected enum Ms { M1, M2 };  // 1st line treatment with M, and 2nd line treatment with M
        protected enum SymStates { Sym, Asym };
        protected enum ResistStates { G_0, G_A, G_B, G_AB }
        protected enum DummyParam { D_0 = 0, D_1, D_Minus1, D_Inf, T_Prev, T_DeltaPrev, N_IsolateTested, NationalLocal } // 0, 1, 2, 3, 4, 5

        
        protected List<string> _infProfiles = new List<string>();
        protected GonoInterventionInfo _interventionInfo = new GonoInterventionInfo();
        protected GonoSpecialStatInfo _specialStatInfo = new GonoSpecialStatInfo();
        protected GonoFeatureInfo _featureInfo = new GonoFeatureInfo();
        protected GonoConditionsInfo _conditionInfo = new GonoConditionsInfo();

        public GonoModel()
        {
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    _infProfiles.Add(s.ToString() + " | " + r.ToString());
                }
        }

        protected void BuildGonoModel(List<string> regions)
        {
            _interventionInfo.Reset(regions.Count);
            _specialStatInfo.Reset(regions.Count);
            _featureInfo.Reset(regions.Count);
            _conditionInfo.Reset(regions.Count);

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
            AddGonoConditions(regions);
            // add connections
            AddGonoConnections(regions);
        }


        protected void AddGonoParameters(List<string> regions)
        {
            int parIDInitialPop = _paramManager.Dic["Initial population size | " + regions[0]];
            int parID1MinusPrev = _paramManager.Dic["1-Initial prevalence | " + regions[0]];

            // initial size of S
            for (int i = 0; i < regions.Count; i++)
                AddGonoParamSize_S(
                    region: regions[i], 
                    parIDInitialPop: parIDInitialPop + i, 
                    parID1MinusPrev: parID1MinusPrev + i);

            // initial size of I compartments
            int infProfileID = 0;
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                    AddGonoParamSize_I(regions, s, r, infProfileID++);

        }

        protected void AddGonoClasses(List<string> regions)
        {
            int classID = 0;
            int regionID = 0;
            int infProfile = 0; // infection profile
            int parIDSize = 0;

            // add S's
            parIDSize = _paramManager.Dic["Initial size of " + regions[0] + " | S"];
            for (regionID = 0; regionID < regions.Count; regionID++)
            {
                Class_Normal S = Get_S(
                    id: classID,
                    region: regions[regionID],
                    parInitialSizeID: parIDSize + regionID);
                _classes.Add(S);
                _dicClasses[S.Name] = classID++;
            }

            // add classes to count the treatment outcomes
            // Success with A1, B1, or B2
            foreach (Drugs d in Enum.GetValues(typeof(Drugs)))
                for (regionID = 0; regionID < regions.Count; regionID++)
                {
                    Class_Normal c = Get_Success(id: classID, region: regions[regionID], drug: d.ToString());
                    _classes.Add(c);
                    _dicClasses[c.Name] = classID++;
                    _specialStatInfo.FormulaTreatedA1B1B2[(int)d] += c.ID + "+";
                }

            // Success with M1 or M2
            foreach (Ms m in Enum.GetValues(typeof(Ms)))
                for (regionID = 0; regionID < regions.Count; regionID++)
                {
                    Class_Normal c = Get_Success(id: classID, region: regions[regionID], drug: m.ToString());
                    _classes.Add(c);
                    _dicClasses[c.Name] = classID++;
                    _specialStatInfo.FormulaTreatedM1M2[(int)m] += c.ID + "+";
                }

            // add death
            for (regionID = 0; regionID < regions.Count; regionID++)
            {
                Class_Death D = Get_D(id: classID, region: regions[regionID]);
                _classes.Add(D);
                _dicClasses[D.Name] = classID++;
            }

            // add I's, W's, and U's, 
            // example: "I | Sym | G_0"     
            int infictivityParID = _paramManager.Dic["Infectivity of | I | " + _infProfiles[0]];
            parIDSize = _paramManager.Dic["Initial size of " + regions[0] + " | I | " + _infProfiles[0]];
            foreach (Comparts c in Enum.GetValues(typeof(Comparts)))
            {
                infProfile = 0;
                foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                    foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                    {
                        for (regionID = 0; regionID < regions.Count; regionID++)
                        {
                            Class_Normal C = Get_I_W_U(
                                id: classID,
                                region: regions[regionID],
                                infProfileID: infProfile,
                                c: c,
                                r: r,
                                parIDSize: (c == Comparts.I) ? parIDSize++ : (int)DummyParam.D_0,
                                infectivityParID: infictivityParID + infProfile);
                            _classes.Add(C);
                            _dicClasses[C.Name] = classID++;

                            // update formulas of special statistics 
                            _specialStatInfo.FormulaPrev[0] += C.ID + "+";
                            if (s == SymStates.Sym)
                                _specialStatInfo.FormulaPrevSym[0][0] += C.ID + "+";
                            else
                                _specialStatInfo.FormulaPrevSym[0][1] += C.ID + "+";

                            if (r != ResistStates.G_0)
                                _specialStatInfo.FormulaPrevResist[0][(int)r] += C.ID + "+";

                            // special statics on treatment
                            if (c == Comparts.W)
                            {
                                _specialStatInfo.FormulaTreated[0] += C.ID + "+";
                                if (regions.Count > 1)
                                    _specialStatInfo.FormulaTreated[1 + regionID] += C.ID + "+";

                                if (s == SymStates.Sym)
                                    _specialStatInfo.FormulaTreatedAndSym[0] += C.ID + "+";

                                if (r != ResistStates.G_0)
                                {
                                    _specialStatInfo.FormulaTreatedResist[0][(int)r] += C.ID + "+";
                                    if (regions.Count > 1)
                                        _specialStatInfo.FormulaTreatedResist[1+regionID][(int)r] += C.ID + "+";
                                }

                            }
                        }
                        ++infProfile;
                    }
            }

            // Prob symptomatic after infection
            int classIDIfSymp = _dicClasses[regions[0] + " | I | Sym | G_0"];
            int classIDIfAsymp = _dicClasses[regions[0] + " | I | Asym | G_0"];
            int ifSymParID = _paramManager.Dic["Prob sym | G_0"];
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                for (regionID = 0; regionID < regions.Count; regionID++)
                {
                    Class_Splitting ifSym = Get_IfSym(
                        id: classID,
                        region: regions[regionID],
                        r: r,
                        ifSymParID: ifSymParID + (int)r,
                        classIDIfSym: classIDIfSymp++,
                        classIDIfAsym: classIDIfAsymp++
                        );
                    _classes.Add(ifSym);
                    _dicClasses[ifSym.Name] = classID++;
                }

            // if seeking retreatment after resistance or failure
            // examples "If retreat A | A --> I | Sym | G_0"
            //          "If retreat F | A --> I | Sym | G_A"
            int parIDProbRetreatIfSym = _paramManager.Dic["Prob retreatment | Sym"];
            int parIDProbRetreatIfAsym = _paramManager.Dic["Prob retreatment | Asym"];
            foreach (Drugs drug in Enum.GetValues(typeof(Drugs)))   // A1, B1, B2
                // assume that failure after B2 will always seek retreatment 
                if (drug == Drugs.A1 || drug == Drugs.B1)
                {
                    infProfile = 0;
                    foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                        foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                        {
                            for (regionID = 0; regionID < regions.Count; regionID++)
                            {
                                Class_Splitting ifRetreat = Get_IfRetreat(
                                    id: classID,
                                    region: regions[regionID],
                                    r: r,
                                    s: s,
                                    drug: drug,
                                    infProfile: infProfile,
                                    parIDProbRetreat: (s == SymStates.Sym) ? parIDProbRetreatIfSym : parIDProbRetreatIfAsym);
                                _classes.Add(ifRetreat);
                                _dicClasses[ifRetreat.Name] = classID++;

                            }
                            ++infProfile;
                        }
                }

            // if symptomatic after the emergence of resistance
            // example: "If sym | A | A --> I | Asym | G_0"
            //          true    -> "If retreat A | A --> I | Sym | G_0"
            //          false   -> "I | Asym | G_A"         
            int parIDProbSym = _paramManager.Dic["Prob sym | G_0"];
            foreach (Drugs drug in Enum.GetValues(typeof(Drugs))) // A1, B1, or B2
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                    for (regionID = 0; regionID < regions.Count; regionID++)
                    {
                        string resistOrFail = GetResistOrFail(resistStat: r, drug: drug);
                        // if developed resistance
                        if (resistOrFail != "F")
                        {
                            Class_Splitting ifSymp = Get_IfSymAfterR(
                                id: classID,
                                region: regions[regionID],
                                resistOrFail: resistOrFail,
                                r: r,
                                drug: drug);
                            _classes.Add(ifSymp);
                            _dicClasses[ifSymp.Name] = classID++;
                        }
                    }

            // treatment outcomes (resistance)    
            // example: "If A | A --> I | Sym | G_0"
            //          true: "If retreat A | A --> I | Sym | G_0"
            //          false: "Success A1"
            // example: "If A | A --> I | Asym | G_0"
            //          true: "If sym | A | A --> I | Asym | G_0"
            //          false: "Success A1"
            int classIDSuccess = _dicClasses[regions[0] + " | Success with " + Drugs.A1.ToString()];
            int parIDProbResistA = _paramManager.Dic["Prob resistance | Drug A"];
            int parIDProbResistB = parIDProbResistA + 1;
            foreach (Drugs drug in Enum.GetValues(typeof(Drugs))) // A1, B1, B2
                foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                    foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                        for (regionID = 0; regionID < regions.Count; regionID++)
                        {
                            // considering only resistance outcome
                            string resistOrFail = GetResistOrFail(resistStat: r, drug: drug);
                            if (resistOrFail == "F")
                                continue;

                            Class_Splitting ifResist = Get_ifR(
                                id: classID,
                                region: regions[regionID],
                                resistOrFail: resistOrFail,
                                r: r,
                                s: s,
                                drug: drug,
                                parIDProbResist: (drug == Drugs.A1) ? parIDProbResistA : parIDProbResistB,
                                classIDSuccess: classIDSuccess + (int)drug * regions.Count + regionID);
                            _classes.Add(ifResist);
                            _dicClasses[ifResist.Name] = classID++;
                        }
        }

        protected void AddGonoEvents(List<string> regions)
        {
            int i = 0;
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

            int n = regions.Count;

            // create a list of main compartments: S, I
            List<string> SandIs = new List<string>();
            SandIs.Add("S");
            for (inf = 0; inf < _infProfiles.Count; inf++)
                SandIs.Add("I | " + _infProfiles[inf]);

            // add Birth events
            foreach (string comp in SandIs)
                for (regionID = 0; regionID < n; regionID++)
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

            // add Death events
            foreach (string comp in SandIs)
                for (regionID = 0; regionID < n; regionID++)
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

            // add Infection events
            int idIfSympG_0 = _dicClasses[regions[0] + " | If Sym | G_0"];
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                for (regionID = 0; regionID < n; regionID++)
                {
                    eventName = regions[regionID] + " | Infection | " + r.ToString();
                    _events.Add(new Event_EpidemicDependent(
                        name: eventName,
                        ID: id,
                        IDOfActivatingIntervention: 0,
                        IDOfPathogenToGenerate: (int)r,
                        IDOfDestinationClass: idIfSympG_0 + (int)r * n + regionID)
                        );
                    _dicEvents[eventName] = id++;
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
                            IDOfDestinationClass: idS * n + regionID)
                        );
                        _dicEvents[eventName] = id++;
                    }
                    inf++;
                }

            // add Seeking Treatment events
            int idWSymG_0 = _dicClasses[regions[0] + " | W | Sym | G_0"];
            inf = 0;
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    for (regionID = 0; regionID < n; regionID++)
                    {
                        eventName = regions[regionID] + " | Seeking Treatment | I | " + _infProfiles[inf];
                        _events.Add(new Event_EpidemicIndependent(
                            name: eventName,
                            ID: id,
                            IDOfActivatingIntervention: 0,
                            rateParameter: (s == SymStates.Sym) ? _paramManager.Parameters[seekingTreatmentRate] : _paramManager.Parameters[(int)DummyParam.D_0],
                            IDOfDestinationClass: idWSymG_0 + inf * n + regionID)
                        );
                        _dicEvents[eventName] = id++;
                    }
                    inf++;
                }

            // add Screening events
            inf = 0;
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    for (regionID = 0; regionID < n; regionID++)
                    {
                        eventName = regions[regionID] + " | Screening | I | " + _infProfiles[inf];
                        _events.Add(new Event_EpidemicIndependent(
                            name: eventName,
                            ID: id,
                            IDOfActivatingIntervention: 0,
                            rateParameter: _paramManager.Parameters[screeningRate],
                            IDOfDestinationClass: idWSymG_0 + inf * n + regionID)
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
                            string resistOrFail = GetResistOrFail(resistStat: r, drug: d);
                            string treatmentProfile = resistOrFail + " | " + d.ToString() + " --> I | " + _infProfiles[inf];
                            string suffix = " | Tx_" + d.ToString() + " | W | " + _infProfiles[inf];
                            for (regionID = 0; regionID < n; regionID++)
                            {                                
                                eventName = regions[regionID] + suffix;
                                string destClassName = "";

                                if (resistOrFail == "F")
                                    destClassName = regions[regionID] + " | If retreat " + treatmentProfile;
                                else
                                    destClassName = regions[regionID] + " | If " + treatmentProfile;

                                int intID = 0;
                                if (d == Drugs.A1)
                                    intID = _interventionInfo.InterventionsIDs[(int)Interventions.A1];
                                else
                                    intID = _interventionInfo.InterventionsIDs[(int)Interventions.B1];

                                _events.Add(new Event_EpidemicIndependent(
                                    name: eventName,
                                    ID: id,
                                    IDOfActivatingIntervention: intID + regionID,
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
                    for (regionID = 0; regionID < n; regionID++)
                    {
                        string infProfile = s.ToString() + " | " + r.ToString();
                        eventName = regions[regionID] + " | Tx_M1 | W | " + infProfile;
                        _events.Add(new Event_EpidemicIndependent(
                            name: eventName,
                            ID: id,
                            IDOfActivatingIntervention: _interventionInfo.InterventionsIDs[(int)Interventions.M1] + regionID,
                            rateParameter: _paramManager.Parameters[infRate],
                            IDOfDestinationClass: idSuccessM1 + regionID)
                            );
                        _dicEvents[eventName] = id++;
                    }
                }

            // add Second-Line Treatment with B2            
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    string resistOrFail = GetResistOrFail(resistStat: r, drug: Drugs.B2);
                    string infProfile = s.ToString() + " | " + r.ToString();
                    string treatmentProfile = resistOrFail + " | B2 --> I | " + infProfile;

                    for (regionID = 0; regionID < n; regionID++)
                    {
                        eventName = regions[regionID] + " | Tx_B2 | U | " + infProfile;

                        string destClassName = "";
                        if (resistOrFail == "F")
                            // if treatment failure occurs, the patient will receive M2 
                            destClassName = regions[regionID] + " | Success with M2";
                        else
                            destClassName = regions[regionID] + " | If " + treatmentProfile;

                        int intID = 0;
                        if (r == ResistStates.G_A)
                            intID = _interventionInfo.InterventionsIDs[(int)Interventions.B2_A] + regionID;
                        else
                            intID = 1;

                        _events.Add(new Event_EpidemicIndependent(
                            name: eventName,
                            ID: id,
                            IDOfActivatingIntervention: intID,
                            rateParameter: _paramManager.Parameters[seekingReTreatmentRate],
                            IDOfDestinationClass: _dicClasses[destClassName])
                            );
                        _dicEvents[eventName] = id++;
                    }
                }

            // add Second-Line Treatment with M2  
            inf = 0;
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    for (regionID = 0; regionID < n; regionID++)
                    {
                        eventName = regions[regionID] + " | Tx_M2 | U | " + _infProfiles[inf];

                        int intID = 0;
                        if (r == ResistStates.G_0)
                            intID = 1;
                        else if (r == ResistStates.G_A)
                            intID = _interventionInfo.InterventionsIDs[(int)Interventions.M2_A] + regionID;
                        else
                            intID = _interventionInfo.InterventionsIDs[(int)Interventions.M2_B_AB] + regionID;

                        _events.Add(new Event_EpidemicIndependent(
                            name: eventName,
                            ID: id,
                            IDOfActivatingIntervention: intID,
                            rateParameter: _paramManager.Parameters[seekingReTreatmentRate],
                            IDOfDestinationClass: idSuccessM2 + regionID)
                            );
                        _dicEvents[eventName] = id++;
                    }
                    inf++;
                }

            // add Leaving Success with A1, B1, or B2
            foreach (Drugs d in Enum.GetValues(typeof(Drugs)))
                for (regionID = 0; regionID < n; regionID++)
                {
                    eventName = regions[regionID] + " | Leaving Success with " + d.ToString();
                    _events.Add(new Event_EpidemicIndependent(
                        name: eventName,
                        ID: id,
                        IDOfActivatingIntervention: 0, // always on
                        rateParameter: _paramManager.Parameters[(int)DummyParam.D_Inf],
                        IDOfDestinationClass: idS + regionID) // back to S
                        );
                    _dicEvents[eventName] = id++;
                }

            // add Leaving Success with M1 or M2
            foreach (Ms m in Enum.GetValues(typeof(Ms)))
            {
                for (regionID = 0; regionID < n; regionID++)
                {
                    eventName = regions[regionID] + " | Leaving Success with " + m.ToString();
                    _events.Add(new Event_EpidemicIndependent(
                        name: eventName,
                        ID: id,
                        IDOfActivatingIntervention: 0, // always on
                        rateParameter: _paramManager.Parameters[(int)DummyParam.D_Inf],
                        IDOfDestinationClass: idS + regionID) // back to S
                        );
                    _dicEvents[eventName] = id++;
                }
            }
        }

        protected void AddGonoConnections(List<string> regions)
        {
            int n = regions.Count;
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
            // add events for S, I, W, U
            Class_Normal C;
            int offset;
            int i = 0, c = 0, w = 0, u = 0;
            while (c < _classes.Count)
            {
                if (!(_classes[c] is Class_Normal))
                {
                    c++;
                    continue;
                }
                // for S
                if (_classes[c].Name.StartsWith(regions[0] + " | S") && _classes[c].Name.Length==regions[0].Length + " | S".Length)
                {
                    for (int regionID = 0; regionID < n; regionID++)
                    {
                        C = (Class_Normal)_classes[c];
                        // birth and death
                        C.AddAnEvent(_events[birthID + regionID]);
                        C.AddAnEvent(_events[deathID + regionID]);
                        // infections
                        foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                            C.AddAnEvent(_events[infectionID + (int)r * n + regionID]);
                        c++;
                    }
                }
                // for I
                else if (_classes[c].Name.StartsWith(regions[0] + " | I"))
                {
                    for (int regionID = 0; regionID < n; regionID++)
                    {
                        offset = i * n + regionID;
                        C = (Class_Normal)_classes[c];
                        C.AddAnEvent(_events[birthID + 1 + offset]);
                        C.AddAnEvent(_events[deathID + 1 + offset]);
                        C.AddAnEvent(_events[naturalRecoveryID + offset]);
                        C.AddAnEvent(_events[seekingTreatmentID + offset]);
                        C.AddAnEvent(_events[screeningID + offset]);
                        c++;
                    }
                    i++;
                }
                // for W
                else if (_classes[c].Name.StartsWith(regions[0] + " | W"))
                {
                    for (int regionID = 0; regionID < n; regionID++)
                    {
                        offset = w * n + regionID;
                        C = (Class_Normal)_classes[c];
                        C.AddAnEvent(_events[txA + offset]);
                        C.AddAnEvent(_events[txB + offset]);
                        C.AddAnEvent(_events[txM + offset]);
                        c++;
                    }
                    ++w;
                }
                // for U
                else if (_classes[c].Name.StartsWith(regions[0] + " | U"))
                {
                    for (int regionID = 0; regionID < n; regionID++)
                    {
                        offset = u * n + regionID;
                        C = (Class_Normal)_classes[c];
                        C.AddAnEvent(_events[txB2 + offset]);
                        C.AddAnEvent(_events[txM2 + offset]);
                        c++;
                    }
                    ++u;
                }
                else
                    c++;
            }

            // add leaving success with A1, B1, B2, M1, M2
            for (int j = 0; j < 5; j++)
                for (int regionID = 0; regionID < n; regionID++)
                {
                    offset = n * j + regionID;
                    ((Class_Normal)_classes[success + offset]).AddAnEvent(_events[leaveSuccess + offset]);
                }
        }

        protected void AddGonoSumStats(List<string> regions)
        {
            int id = 0;
            int regionID = 0;
            string formula = "";

            // population size
            formula = "";
            foreach (Class c in _classes.Where(c => c is Class_Normal))
                formula += c.ID + "+";
            _epiHist.SumTrajs.Add(
                new SumClassesTrajectory(
                    ID: id++,
                    name: "Population size",
                    strType: "Prevalence",
                    sumFormula: formula,
                    displayInSimOutput: true,
                    warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                    );
            _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.PopSize] = id - 1;

            // gonorrhea prevalence
            _epiHist.SumTrajs.Add(
                new SumClassesTrajectory(
                    ID: id++,
                    name: "Prevalence",
                    strType: "Prevalence",
                    sumFormula: _specialStatInfo.FormulaPrev[0],
                    displayInSimOutput: true,
                    warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                    );
            _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.Prev] = id - 1;

            // prevalence of symptomatic gonorrhea 
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
            {
                if (s == SymStates.Sym)
                    _epiHist.SumTrajs.Add(
                        new SumClassesTrajectory(
                            ID: id++,
                            name: "Prevalence | " + s.ToString(),
                            strType: "Prevalence",
                            sumFormula: _specialStatInfo.FormulaPrevSym[0][(int)s],
                            displayInSimOutput: true,
                            warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                            nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                            );
            }

            // gonorrhea prevalence by resistance 
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates))) // G_0, G_A, G_B, G_AB
                if (r != ResistStates.G_0)
                    _epiHist.SumTrajs.Add(
                        new SumClassesTrajectory(
                            ID: id++,
                            name: "Prevalence | " + r.ToString(),
                            strType: "Prevalence",
                            sumFormula: _specialStatInfo.FormulaPrevResist[0][(int)r],
                            displayInSimOutput: true,
                            warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                            nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                            );

            // received first-line treatment (= number of cases)
            SumClassesTrajectory t1st = new SumClassesTrajectory(
                ID: id++,
                name: "Received 1st Tx",
                strType: "Incidence",
                sumFormula: _specialStatInfo.FormulaTreated[0],
                displayInSimOutput: true,
                warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);
            UpdateClassTimeSeries(t1st); // this is because we are collecting outcomes on this
            t1st.DeltaCostHealthCollector =
                new DeltaTCostHealth(
                    deltaT: _modelSets.DeltaT,
                    warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                    DALYPerNewMember: _paramManager.Parameters[(int)DummyParam.D_1],
                    costPerNewMember: _paramManager.Parameters[(int)DummyParam.D_0]
                    );
            _epiHist.SumTrajs.Add(t1st);
            _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.Tx1] = id - 1;

            // received first-line treatment by region
            if (regions.Count > 1)
            {
                for (regionID = 0; regionID < regions.Count; regionID++)
                {
                    _epiHist.SumTrajs.Add(new SumClassesTrajectory(
                        ID: id++,
                        name: "Received 1st Tx | " + regions[regionID],
                        strType: "Incidence",
                        sumFormula: _specialStatInfo.FormulaTreated[1+regionID],
                        displayInSimOutput: true,
                        warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                        nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                        );
                }
            }

            // received first-line treatment and symptomatic 
            _epiHist.SumTrajs.Add(new SumClassesTrajectory(
                ID: id++,
                name: "Received 1st Tx & Symptomatic",
                strType: "Incidence",
                sumFormula: _specialStatInfo.FormulaTreatedAndSym[0],
                displayInSimOutput: true,
                warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                );
            _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.Tx1Sym] = id - 1;

            // received first-line treatment by resistance status
            _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.Tx1Resist] = id;
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates))) // G_0, G_A, G_B, G_AB
                if (r != ResistStates.G_0)
                {
                    _specialStatInfo.IDTxResist[(int)r] = id;
                    _epiHist.SumTrajs.Add(
                        new SumClassesTrajectory(
                            ID: id++,
                            name: "Received 1st Tx & Resistant to " + r.ToString(),
                            strType: "Incidence",
                            sumFormula: _specialStatInfo.FormulaTreatedResist[0][(int)r],
                            displayInSimOutput: true,
                            warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                            nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                            );

                    // received first-line treatment by resistance status and region
                    if (regions.Count > 1)
                    {                        
                        for (regionID = 0; regionID < regions.Count; regionID++)
                        {
                            _epiHist.SumTrajs.Add(
                                new SumClassesTrajectory(
                                    ID: id++,
                                    name: "Received 1st Tx & Resistant to " + r.ToString() + " | " + regions[regionID],
                                    strType: "Incidence",
                                    sumFormula: _specialStatInfo.FormulaTreatedResist[1 + regionID][(int)r],
                                    displayInSimOutput: true,
                                    warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                                    );
                        }
                    }
                }

            // sucessful treatment
            string success1st =
                _specialStatInfo.FormulaTreatedA1B1B2[(int)Drugs.A1] +
                _specialStatInfo.FormulaTreatedA1B1B2[(int)Drugs.B1] +
                _specialStatInfo.FormulaTreatedM1M2[(int)Ms.M1];
            string successAorB =
                _specialStatInfo.FormulaTreatedA1B1B2[(int)Drugs.A1] +
                _specialStatInfo.FormulaTreatedA1B1B2[(int)Drugs.B1] +
                _specialStatInfo.FormulaTreatedA1B1B2[(int)Drugs.B2];
            string successM =
                _specialStatInfo.FormulaTreatedM1M2[(int)Ms.M1] +
                _specialStatInfo.FormulaTreatedM1M2[(int)Ms.M2];
            string successAll = successAorB + successM;

            // # sucessfully treated with 1st line treatment 
            _epiHist.SumTrajs.Add(
                new SumClassesTrajectory(
                    ID: id++,
                    name: "Success 1st",
                    strType: "Incidence",
                    sumFormula: success1st,
                    displayInSimOutput: true,
                    warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                    );
            // # sucessfully treated with A or B 
            _epiHist.SumTrajs.Add(
                new SumClassesTrajectory(
                    ID: id++,
                    name: "Success A or B",
                    strType: "Incidence",
                    sumFormula: successAorB,
                    displayInSimOutput: true,
                    warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                    );
            _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.SuccessAOrB] = id - 1;

            // sucessfully treated with M
            SumClassesTrajectory tM = new SumClassesTrajectory(
               ID: id++,
               name: "Success M",
               strType: "Incidence",
               sumFormula: successM,
               displayInSimOutput: true,
               warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
               nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);
            UpdateClassTimeSeries(tM);
            tM.DeltaCostHealthCollector =
                new DeltaTCostHealth(
                    deltaT: _modelSets.DeltaT,
                    warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                    DALYPerNewMember: _paramManager.Parameters[(int)DummyParam.D_0],
                    costPerNewMember: _paramManager.Parameters[(int)DummyParam.D_1]
                    );
            _epiHist.SumTrajs.Add(tM);

            // sucessfully treated 
            _epiHist.SumTrajs.Add(
                new SumClassesTrajectory(
                    ID: id++,
                    name: "Success All",
                    strType: "Incidence",
                    sumFormula: successAll,
                    displayInSimOutput: true,
                    warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                    );
            _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.SuccessAOrBOrM] = id - 1;

            // update times series of summation statistics
            UpdateSumStatTimeSeries();
        }

        protected void AddGonoRatioStatistics(List<string> regions)
        {
            int id = _epiHist.SumTrajs.Count();
            int idPopSize = _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.PopSize];
            int idPrevalence = _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.Prev];
            int idTx1 = _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.Tx1];
            int idTx1Sym = _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.Tx1Sym];
            int idTx1Resist = _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.Tx1Resist];
            int idSuccessAOrB = _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.SuccessAOrB];
            int idSuccessAOrBOrM = _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.SuccessAOrBOrM];
            Parameter nIsolateTested = _paramManager.Parameters[(int)DummyParam.N_IsolateTested];

            // gonorrhea prevalence
            RatioTrajectory prevalence = new RatioTrajectory(
                id: id,
                name: "Prevalence",
                strType: "Prevalence/Prevalence",
                ratioFormula: idPrevalence + "/" + idPopSize,
                displayInSimOutput: true,
                warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);
            if (_modelSets.ModelUse == EnumModelUse.Calibration)
                prevalence.CalibInfo = new SpecialStatCalibrInfo(
                    measureOfFit: "Likelihood",
                    likelihoodFunction: "Binomial",
                    likelihoodParam: "",
                    ifCheckWithinFeasibleRange: true,
                    lowFeasibleBound: 0.005,
                    upFeasibleBound: 0.04,
                    minThresholdToHit: 0);
            _epiHist.RatioTrajs.Add(prevalence);
            id++;

            // % infection symptomatic (prevalence)
            RatioTrajectory prevalenceSym = new RatioTrajectory(
                id: id++,
                name: "% infection symptomatic",
                strType: "Prevalence/Prevalence",
                ratioFormula: (idPrevalence + 1) + "/" + idPrevalence,
                displayInSimOutput: true,
                warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);
            _epiHist.RatioTrajs.Add(prevalenceSym);

            // % infection resistant to A, B, or AB (prevalence)
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates))) // G_0, G_A, G_B, G_AB
            {
                if (r != ResistStates.G_0)
                {
                    RatioTrajectory prev = new RatioTrajectory(
                        id: id++,
                        name: "% infection resistant to " + r.ToString(),
                        strType: "Prevalence/Prevalence",
                        ratioFormula: (idPrevalence + 1 + (int)r) + "/" + idPrevalence,
                        displayInSimOutput: true,
                        warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                        nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);
                    _epiHist.RatioTrajs.Add(prev);
                }
            }

            // % received 1st Tx and symptomatic (incidence)            
            RatioTrajectory firstTxSym = new RatioTrajectory(
                id: id++,
                name: "% received 1st Tx & symptomatic ",
                strType: "Incidence/Incidence",
                ratioFormula: idTx1Sym + "/" + idTx1,
                displayInSimOutput: true,
                warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);
            if (_modelSets.ModelUse == EnumModelUse.Calibration)
                firstTxSym.CalibInfo = new SpecialStatCalibrInfo(
                    measureOfFit: "Likelihood",
                    likelihoodFunction: "Binomial",
                    likelihoodParam: "",
                    ifCheckWithinFeasibleRange: true,
                    lowFeasibleBound: 0.5,
                    upFeasibleBound: 0.8,
                    minThresholdToHit: 0);
            _epiHist.RatioTrajs.Add(firstTxSym);

            // % received 1st Tx and resistant to A, B, or AB (incidence)
            _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.PercFirstTxAndResist] = id;
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates))) // G_0, G_A, G_B, G_AB
            {
                if (r != ResistStates.G_0)
                {
                    _specialStatInfo.IDRatioTxResist[(int)r] = id;
                    RatioTrajectory firstTx = new RatioTrajectory(
                        id: id,
                        name: "% received 1st Tx & resistant to " + r.ToString(),
                        strType: "Incidence/Incidence",
                        ratioFormula: _specialStatInfo.IDTxResist[(int)r] + "/" + idTx1, 
                        displayInSimOutput: true,
                        warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                        nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);
                    if (_modelSets.ModelUse == EnumModelUse.Calibration && r != ResistStates.G_AB)
                        firstTx.CalibInfo = new SpecialStatCalibrInfo(
                            measureOfFit: "Feasible Range Only",
                            likelihoodFunction: "",
                            likelihoodParam: "",
                            ifCheckWithinFeasibleRange: true,
                            lowFeasibleBound: 0,
                            upFeasibleBound: 1,
                            minThresholdToHit: 0.05);
                    _epiHist.SurveyedIncidenceTrajs.Add(
                        new SurveyedIncidenceTrajectory(
                            id: id,
                           name: "% received 1st Tx & resistant to " + r.ToString(),
                           displayInSimOutput: true,
                           firstObsMarksStartOfEpidemic: false,
                           sumClassesTrajectory: null,
                           sumEventTrajectory: null,
                           ratioTrajectory: firstTx,
                           nDeltaTsObsPeriod: _modelSets.NumOfDeltaT_inObservationPeriod,
                           nDeltaTsDelayed: 0,
                           noise_nOfDemoninatorSampled: nIsolateTested)
                           );
                    _epiHist.RatioTrajs.Add(firstTx);
                    id++;

                    // % received 1st Tx and resistant to A, B, or AB (incidence) by region
                    if (regions.Count > 1)
                    {                       
                        int firstID = _specialStatInfo.IDTxResist[(int)r] + 1;
                        for (int regionID = 0; regionID < regions.Count; regionID++)
                        {
                            RatioTrajectory traj = new RatioTrajectory(
                                id: id,
                                name: "% received 1st Tx & resistant to " + r.ToString() + " | " + regions[regionID],
                                strType: "Incidence/Incidence",
                                ratioFormula: (firstID + regionID) + "/" + (idTx1 + 1 + regionID),
                                displayInSimOutput: true,
                                warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);

                            _epiHist.SurveyedIncidenceTrajs.Add(
                                new SurveyedIncidenceTrajectory(
                                    id: id,
                                    name: "% received 1st Tx & resistant to " + r.ToString() + " | " + regions[regionID],
                                    displayInSimOutput: true,
                                    firstObsMarksStartOfEpidemic: false,
                                    sumClassesTrajectory: null,
                                    sumEventTrajectory: null,
                                    ratioTrajectory: traj,
                                    nDeltaTsObsPeriod: _modelSets.NumOfDeltaT_inObservationPeriod,
                                    nDeltaTsDelayed: 0,
                                    noise_nOfDemoninatorSampled: nIsolateTested,
                                    ratioNoiseN: 1 / regions.Count)
                                    );
                            _epiHist.RatioTrajs.Add(traj);
                            id++;
                        }
                    }
                }
            }

            // annual rate of gonorrhea cases
            RatioTrajectory rate = new RatioTrajectory(
                id: id++,
                name: "Annual rate of gonorrhea cases",
                strType: "Incidence/Prevalence",
                ratioFormula: idTx1 + "/" + idPopSize,
                displayInSimOutput: true,
                warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);
            if (_modelSets.ModelUse == EnumModelUse.Calibration)
                rate.CalibInfo = new SpecialStatCalibrInfo(
                    measureOfFit: "Likelihood",
                    likelihoodFunction: "Binomial",
                    likelihoodParam: "",
                    ifCheckWithinFeasibleRange: true,
                    lowFeasibleBound: 0.02,
                    upFeasibleBound: 0.08,
                    minThresholdToHit: 0);
            _epiHist.RatioTrajs.Add(rate);

            // effective life of drugs A and B
            RatioTrajectory effLifeAandB = new RatioTrajectory(
                id: id++,
                name: "Effective life of A and B",
                strType: "Incidence/Incidence",
                ratioFormula: idSuccessAOrB + "/" + idSuccessAOrBOrM,
                displayInSimOutput: true,
                warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);
            _epiHist.RatioTrajs.Add(effLifeAandB);

            // update times series of ratio statistics
            UpdateRatioStatTimeSeries();
        }

        protected void AddGonoFeatures(List<string> regions)
        {
            int id = 0;
            int intv = 0;
            int idPercFirstTxAndResist = _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.PercFirstTxAndResist];

            // add time
            _featureInfo.FeatureIDs[(int)Features.Time] = id;
            _epiHist.Features.Add(new Feature_EpidemicTime("Epidemic Time", id++));

            // % receieved 1st Tx and resistant to A, B, or AB
            _featureInfo.FeatureIDs[(int)Features.PercResist] = id;
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates))) // G_0, G_A, G_B, G_AB
            {
                if (r != ResistStates.G_0)
                {
                    _epiHist.AddASpecialStatisticsFeature(
                        name: "% received 1st Tx & resistant to " + r.ToString(),
                        featureID: id++,
                        specialStatID: _specialStatInfo.IDRatioTxResist[(int)r],
                        strFeatureType: "Current Observed Value",
                        par: 0);

                    if (regions.Count > 1)
                    {
                        _featureInfo.PercResistFirstRegion[(int)r] = id;
                        int firstID = _specialStatInfo.IDRatioTxResist[(int)r] + 1;
                        for (int regionID = 0; regionID < regions.Count; regionID++)
                        {                            
                            _epiHist.AddASpecialStatisticsFeature(
                                name: "% received 1st Tx & resistant to " + r.ToString() + " | " + regions[regionID],
                                featureID: id++,
                                specialStatID: firstID + regionID,
                                strFeatureType: "Current Observed Value",
                                par: 0);
                        }
                    }
                }
            }

            // change in % receieved 1st Tx and resistant to A, B, or AB
            _featureInfo.FeatureIDs[(int)Features.ChangeInPercResist] = id;
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates))) // G_0, G_A, G_B, G_AB
            {
                if (r != ResistStates.G_0)
                {
                    _epiHist.AddASpecialStatisticsFeature(
                        name: "Change in % received 1st Tx & resistant to " + r.ToString(),
                        featureID: id++,
                        specialStatID: _specialStatInfo.IDRatioTxResist[(int)r],
                        strFeatureType: "Slope",
                        par: 0);

                    if (regions.Count > 1)
                    {
                        _featureInfo.PChangeInPercResistFirstRegion[(int)r] = id;
                        int firstID = _specialStatInfo.IDRatioTxResist[(int)r] + 1;
                        for (int regionID = 0; regionID < regions.Count; regionID++)
                        {
                            _epiHist.AddASpecialStatisticsFeature(
                                name: "Change in % received 1st Tx & resistant to " + r.ToString() + " | " + regions[regionID],
                                featureID: id++,
                                specialStatID: firstID + regionID,
                                strFeatureType: "Slope",
                                par: 0);
                        }
                    }
                }
            }

            // if A1 ever switched off 
            _featureInfo.IfAEverOff = id;
            intv = _interventionInfo.InterventionsIDs[(int)Interventions.A1];
            _epiHist.Features.Add(new Feature_Intervention(
                name: "If A1 ever switched off",
                featureID: id++,
                featureType: Feature_Intervention.EnumFeatureType.IfEverSwitchedOff,
                intervention: _decisionMaker.Interventions[intv])
                );
            if (regions.Count > 1)
                for (int regionID = 0; regionID < regions.Count; regionID++)
                {
                    _epiHist.Features.Add(new Feature_Intervention(
                        name: "If A1 ever switched off | " + regions[regionID],
                        featureID: id++,
                        featureType: Feature_Intervention.EnumFeatureType.IfEverSwitchedOff,
                        intervention: _decisionMaker.Interventions[intv + regionID])
                        );
                }

            // if B1 ever switched off
            _featureInfo.IfBEverOff = id;
            intv = _interventionInfo.InterventionsIDs[(int)Interventions.B1];
            _epiHist.Features.Add(new Feature_Intervention(
                name: "If B1 ever switched off",
                featureID: id++,
                featureType: Feature_Intervention.EnumFeatureType.IfEverSwitchedOff,
                intervention: _decisionMaker.Interventions[intv])
                );
            if (regions.Count > 1)
                for (int regionID = 0; regionID < regions.Count; regionID++)
                {
                    _epiHist.Features.Add(new Feature_Intervention(
                        name: "If B1 ever switched off | " + regions[regionID],
                        featureID: id++,
                        featureType: Feature_Intervention.EnumFeatureType.IfEverSwitchedOff,
                        intervention: _decisionMaker.Interventions[intv + regionID]) 
                        );
                }

            // if M ever switched on
            _featureInfo.IfMEverOn = id;
            intv = _interventionInfo.InterventionsIDs[(int)Interventions.M1];
            _epiHist.Features.Add(new Feature_Intervention(
                name: "If M1 ever switched on",
                featureID: id++,
                featureType: Feature_Intervention.EnumFeatureType.IfEverSwitchedOn,
                intervention: _decisionMaker.Interventions[intv])
                );
            if (regions.Count > 1)
                for (int regionID = 0; regionID < regions.Count; regionID++)
                {
                    _epiHist.Features.Add(new Feature_Intervention(
                        name: "If M ever switched off | " + regions[regionID],
                        featureID: id++,
                        featureType: Feature_Intervention.EnumFeatureType.IfEverSwitchedOff,
                        intervention: _decisionMaker.Interventions[intv + regionID])
                        );
                }
        }

        protected void AddGonoConditions(List<string> regions)
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
            string[] drugUse = new string[3] { "A", "B", "AB" };
            for (int i = 0; i < 3; i++) // over A, B, or both A B out
            {
                int firstIDPerc = _featureInfo.FeatureIDs[(int)Features.PercResist];
                int firstIDChange = _featureInfo.FeatureIDs[(int)Features.ChangeInPercResist];
                _epiHist.Conditions.Add(new Condition_OnFeatures(
                    id: id++,
                    name: drugUse[i] + "Out Condition",
                    features: new List<Feature> {
                        _epiHist.Features[firstIDPerc + i],
                        _epiHist.Features[firstIDChange + i] },
                    thresholdParams: thresholdParams,
                    signs: signs,
                    conclusion: EnumAndOr.Or));
                if (regions.Count > 1)
                {
                    firstIDPerc = _featureInfo.PercResistFirstRegion[i+1];
                    firstIDChange = _featureInfo.PChangeInPercResistFirstRegion[i+1];
                    for (int regionID = 0; regionID < regions.Count; regionID++)
                    {
                        _epiHist.Conditions.Add(new Condition_OnFeatures(
                            id: id++,
                            name: drugUse[i] + "Out Condition | " + regions[regionID],
                            features: new List<Feature> {
                                _epiHist.Features[firstIDPerc + regionID],
                                _epiHist.Features[firstIDChange + regionID] },
                            thresholdParams: thresholdParams,
                            signs: signs,
                            conclusion: EnumAndOr.Or));
                    }
                }
            }

            // ok condition for A, B, or both
            signs = new EnumSign[2] { EnumSign.le, EnumSign.le };
            for (int i = 0; i < 3; i++)
            {
                int firstIDPerc = _featureInfo.FeatureIDs[(int)Features.PercResist];
                int firstIDChange = _featureInfo.FeatureIDs[(int)Features.ChangeInPercResist];
                _epiHist.Conditions.Add(new Condition_OnFeatures(
                    id: id++,
                     name: drugUse[i] + "OK Condition",
                    features: new List<Feature> {
                        _epiHist.Features[firstIDPerc + i],
                        _epiHist.Features[firstIDChange + i] },
                    thresholdParams: thresholdParams,
                    signs: signs,
                    conclusion: EnumAndOr.And));

                if (regions.Count > 1)
                {
                    firstIDPerc = _featureInfo.PercResistFirstRegion[i+1];
                    firstIDChange = _featureInfo.PChangeInPercResistFirstRegion[i+1];
                    for (int regionID = 0; regionID < regions.Count; regionID++)
                    {
                        _epiHist.Conditions.Add(new Condition_OnFeatures(
                            id: id++,
                            name: drugUse[i] + "OK Condition | " + regions[regionID],
                            features: new List<Feature> {
                                _epiHist.Features[firstIDPerc + regionID],
                                _epiHist.Features[firstIDChange + regionID] },
                            thresholdParams: thresholdParams,
                            signs: signs,
                            conclusion: EnumAndOr.Or));
                    }
                }
            }

            // B is never used
            _epiHist.Conditions.Add(new Condition_OnFeatures(
                id: id++,
                name: "B is never used",
                features: new List<Feature> {
                        _epiHist.Features[_featureInfo.IfBEverOff] },
                signs: new EnumSign[1] { EnumSign.e },
                thresholdParams: thresholdParams0,
                conclusion: EnumAndOr.And));
            if (regions.Count > 1)
            {
                for (int regionID = 0; regionID < regions.Count; regionID++)
                {
                    _epiHist.Conditions.Add(new Condition_OnFeatures(
                        id: id++,
                        name: "B is never used | " + regions[regionID],
                        features: new List<Feature> {
                                _epiHist.Features[_featureInfo.IfBEverOff + regionID + 1] },
                        signs: new EnumSign[1] { EnumSign.e },
                        thresholdParams: thresholdParams0,
                        conclusion: EnumAndOr.And));
                }
            }

            // M1 is neer used
            thresholdParams = new List<Parameter> { _paramManager.Parameters[(int)DummyParam.D_0] };
            _epiHist.Conditions.Add(new Condition_OnFeatures(
                id: id++,
                name: "M1 is never used",
                features: new List<Feature> {
                        _epiHist.Features[_featureInfo.IfMEverOn] },
                signs: new EnumSign[1] { EnumSign.e },
                thresholdParams: thresholdParams0,
                conclusion: EnumAndOr.And));
            if (regions.Count > 1)
            {
                for (int regionID = 0; regionID < regions.Count; regionID++)
                {
                    _epiHist.Conditions.Add(new Condition_OnFeatures(
                        id: id++,
                        name: "M1 is never used | " + regions[regionID],
                        features: new List<Feature> {
                                _epiHist.Features[_featureInfo.IfMEverOn + regionID + 1] },
                        signs: new EnumSign[1] { EnumSign.e },
                        thresholdParams: thresholdParams0,
                        conclusion: EnumAndOr.And));
                }
            }

            // turn on A
            _epiHist.Conditions.Add(new Condition_OnFeatures(
                id: id++,
                name: "Drug A - Turn On",
                features: new List<Feature> {
                    _epiHist.Features[_featureInfo.FeatureIDs[(int)Features.Time]],
                    _epiHist.Features[_featureInfo.IfAEverOff] },
                signs: new EnumSign[2] { EnumSign.qe, EnumSign.e },
                thresholdParams: thresholdParams00,
                conclusion: EnumAndOr.And));
            if (regions.Count > 1)
            {
                for (int regionID = 0; regionID < regions.Count; regionID++)
                {
                    _epiHist.Conditions.Add(new Condition_OnFeatures(
                        id: id++,
                        name: "Drug A - Turn On | " + regions[regionID],
                        features: new List<Feature> {
                            _epiHist.Features[_featureInfo.FeatureIDs[(int)Features.Time]],
                            _epiHist.Features[_featureInfo.IfAEverOff + regionID + 1] },
                        signs: new EnumSign[2] { EnumSign.qe, EnumSign.e },
                        thresholdParams: thresholdParams00,
                        conclusion: EnumAndOr.And));
                }
            }

            // turn off A
            _epiHist.Conditions.Add(new Condition_OnConditions(
                id: id++,
                name: "Drug A - Turn Off",
                conditions: new List<Condition> {
                    _epiHist.Conditions[(int)Conditions.AOut],
                    _epiHist.Conditions[(int)Conditions.ABOut] },
                conclusion: EnumAndOr.Or));
            if (regions.Count > 1)
            {
                for (int regionID = 0; regionID < regions.Count; regionID++)
                {
                    _epiHist.Conditions.Add(new Condition_OnConditions(
                        id: id++,
                        name: "Drug A - Turn Off | " + regions[regionID],
                        conditions: new List<Condition> {
                            _epiHist.Conditions[(int)Conditions.AOut + regionID + 1],
                            _epiHist.Conditions[(int)Conditions.ABOut + regionID + 1] },
                        conclusion: EnumAndOr.Or));
                }
            }

            // turn on B
            _epiHist.Conditions.Add(new Condition_OnConditions(
                id: id++,
                name: "Drug B - Turn On",
                conditions: new List<Condition> {
                    _epiHist.Conditions[(int)Conditions.AOut],
                    _epiHist.Conditions[(int)Conditions.BOk],
                    _epiHist.Conditions[(int)Conditions.ABOk],
                    _epiHist.Conditions[(int)Conditions.BNeverUsed],
                    _epiHist.Conditions[(int)Conditions.M1NeverUsed]},
                conclusion: EnumAndOr.And));
            if (regions.Count > 1)
            {
                for (int regionID = 0; regionID < regions.Count; regionID++)
                {
                    _epiHist.Conditions.Add(new Condition_OnConditions(
                        id: id++,
                        name: "Drug B - Turn On | " + regions[regionID],
                        conditions: new List<Condition> {
                            _epiHist.Conditions[(int)Conditions.AOut + regionID + 1],
                            _epiHist.Conditions[(int)Conditions.BOk + regionID + 1],
                            _epiHist.Conditions[(int)Conditions.ABOk + regionID + 1],
                            _epiHist.Conditions[(int)Conditions.BNeverUsed + regionID + 1],
                            _epiHist.Conditions[(int)Conditions.M1NeverUsed + regionID + 1]},
                        conclusion: EnumAndOr.And));
                }
            }

            // turn off B
            _epiHist.Conditions.Add(new Condition_OnConditions(
                id: id++,
                name: "Drug B - Turn Off",
                conditions: new List<Condition> {
                    _epiHist.Conditions[(int)Conditions.BOut],
                    _epiHist.Conditions[(int)Conditions.ABOut] },
                conclusion: EnumAndOr.Or));
            if (regions.Count > 1)
            {
                for (int regionID = 0; regionID < regions.Count; regionID++)
                {
                    _epiHist.Conditions.Add(new Condition_OnConditions(
                        id: id++,
                        name: "Drug B - Turn Off | " + regions[regionID],
                        conditions: new List<Condition> {
                            _epiHist.Conditions[(int)Conditions.BOut + regionID + 1],
                            _epiHist.Conditions[(int)Conditions.ABOut + regionID + 1] },
                        conclusion: EnumAndOr.Or));
                }
            }

            // turn on M
            _epiHist.Conditions.Add(new Condition_OnConditions(
                id: id++,
                name: "Drug M - Turn On",
                conditions: new List<Condition> {
                    _epiHist.Conditions[(int)Conditions.BOff] },
                conclusion: EnumAndOr.And));
            if (regions.Count > 1)
            {
                for (int regionID = 0; regionID < regions.Count; regionID++)
                {
                    _epiHist.Conditions.Add(new Condition_OnConditions(
                        id: id++,
                        name: "Drug M - Turn On | " + regions[regionID],
                        conditions: new List<Condition> {
                            _epiHist.Conditions[(int)Conditions.BOff + regionID + 1] },
                        conclusion: EnumAndOr.And));
                }
            }

            // turn off M
            _epiHist.Conditions.Add(new Condition_AlwaysFalse(
                id: id++,
                name: "Drug M - Turn Off"));
            if (regions.Count > 1)
            {
                for (int regionID = 0; regionID < regions.Count; regionID++)
                {
                    _epiHist.Conditions.Add(new Condition_AlwaysFalse(
                        id: id++,
                        name: "Drug M - Turn Off | " + regions[regionID]));
                }
            }

        }

        protected void AddGonoInterventions(List<string> regions)
        {
            // add default and always off interventions
            AddInterventions();
            int id = _decisionMaker.Interventions.Count();

            // determine if decisions are made nationally or locally (0, 1)
            int deci;
            if (regions.Count == 1)
                deci = 0;
            else
                deci = (int)_paramManager.Parameters[(int)DummyParam.NationalLocal].Sample(time: 0, rng: new RNG(1));

            // add interventions
            foreach (Interventions intrv in Enum.GetValues(typeof(Interventions)))
            {
                int conditionIDToTurnOn = 0, conditionIDToTurnOff = 0;
                switch (intrv)
                {
                    case Interventions.A1:
                        {
                            conditionIDToTurnOn = _conditionInfo.ConditionIDs[(int)Conditions.AOn];
                            conditionIDToTurnOff = _conditionInfo.ConditionIDs[(int)Conditions.AOff];
                        }
                        break;
                    case Interventions.B1:
                        {
                            conditionIDToTurnOn = _conditionInfo.ConditionIDs[(int)Conditions.BOn];
                            conditionIDToTurnOff = _conditionInfo.ConditionIDs[(int)Conditions.BOff];
                        }
                        break;
                    case Interventions.M1:
                        {
                            conditionIDToTurnOn = _conditionInfo.ConditionIDs[(int)Conditions.MOn];
                            conditionIDToTurnOff = _conditionInfo.ConditionIDs[(int)Conditions.MOff];
                        }
                        break;
                    case Interventions.B2_A:
                        {
                            conditionIDToTurnOn = _conditionInfo.ConditionIDs[(int)Conditions.AOn];
                            conditionIDToTurnOff = _conditionInfo.ConditionIDs[(int)Conditions.BOff];
                        }
                        break;
                    case Interventions.M2_A:
                        {
                            conditionIDToTurnOn = _conditionInfo.ConditionIDs[(int)Conditions.MOn];
                            conditionIDToTurnOff = _conditionInfo.ConditionIDs[(int)Conditions.MOff];
                        }
                        break;
                }

                DecisionRule simDecisionRule;
                for (int regionID = 0; regionID < regions.Count; regionID++)
                {                        
                    if (intrv == Interventions.M2_B_AB)
                        simDecisionRule = new DecionRule_Predetermined(predeterminedSwitchValue: 1);
                    else
                        simDecisionRule = new DecisionRule_ConditionBased(
                            conditions: _epiHist.Conditions,
                            conditionIDToTurnOn: (deci == 0) ? conditionIDToTurnOn : (conditionIDToTurnOn + regionID + 1),
                            conditionIDToTurnOff: (deci == 0) ? conditionIDToTurnOff : (conditionIDToTurnOff + regionID + 1));

                    // intervention
                    _decisionMaker.AddAnIntervention(
                        new Intervention(
                            index: id++,
                            name: intrv.ToString() + " | " + regions[regionID],
                            actionType: EnumInterventionType.Additive,
                            affectingContactPattern: false,
                            timeIndexBecomesAvailable: 0,
                            timeIndexBecomesUnavailable: _modelSets.TimeIndexToStop,
                            parIDDelayToGoIntoEffectOnceTurnedOn: 0,
                            decisionRule: simDecisionRule));
                }
            }
        }

        private void AddGonoParamSize_S(string region, int parIDInitialPop, int parID1MinusPrev)
        {
            _paramManager.Add(new ProductParameter(
                ID: _paramManager.Parameters.Count,
                name: "Initial size of " + region + " | S",
                parameters: GetParamList(new List<int>() { parIDInitialPop, parID1MinusPrev }))
                );
        }

        private void AddGonoParamSize_I(List<string> regions, SymStates s, ResistStates r, int infProfileID)
        {
            List<int> paramIDs = new List<int>();
            int parID = _paramManager.Parameters.Count;
            int parIDInitialPopSize = _paramManager.Dic["Initial population size | " + regions[0]];
            int parIDInitialPrevalence = _paramManager.Dic["Initial prevalence | " + regions[0]];
            int parIDInitialSym = _paramManager.Dic["Initial symptomatic | " + regions[0]];
            int parIDInitialAsym = _paramManager.Dic["1-Initial symptomatic | " + regions[0]];
            int parIDInitialResistToA = _paramManager.Dic["Initial resistant to A | " + regions[0]];
            int parIDInitialResistToB = _paramManager.Dic["Initial resistant to B | " + regions[0]];
            int parIDInitialResistToAorB = _paramManager.Dic["1-Initial resistant to A or B | " + regions[0]];

            for (int regionID = 0; regionID < regions.Count; regionID++)
            {
                string parName = "Initial size of " + regions[regionID] + " | I | " + _infProfiles[infProfileID];

                // par ID for population size of this region
                paramIDs.Add(parIDInitialPopSize + regionID);
                // par ID for prevalence of gonorrhea in this region
                paramIDs.Add(parIDInitialPrevalence + regionID);

                // par ID for proportion symptomatic or asymptomatic 
                if (s == SymStates.Sym)
                    paramIDs.Add(parIDInitialSym + regionID);
                else
                    paramIDs.Add(parIDInitialAsym + regionID);

                // par ID for prevalence of resistance to A, B, or AB
                switch (r)
                {
                    case ResistStates.G_0:
                        paramIDs.Add(parIDInitialResistToAorB + regionID);
                        break;
                    case ResistStates.G_A:
                        paramIDs.Add(parIDInitialResistToA + regionID);
                        break;
                    case ResistStates.G_B:
                        paramIDs.Add(parIDInitialResistToB + regionID);
                        break;
                    case ResistStates.G_AB:
                        paramIDs.Add(0);
                        break;
                }

                if (r == ResistStates.G_AB)
                    _paramManager.Add(new IndependetParameter(
                        ID: parID++,
                        name: parName,
                        enumRandomVariateGenerator: RandomVariateLib.SupportProcedures.ConvertToEnumRVG("Constant"),
                        par1: 0, par2: 0, par3: 0, par4: 0)
                        );
                else
                    _paramManager.Add(new ProductParameter(
                        ID: parID++,
                        name: parName,
                        parameters: GetParamList(paramIDs))
                        );
            }
        }

        protected List<Parameter> GetParamList(string paramName)
        {
            return new List<Parameter>() { _paramManager.GetParameter(paramName) };
        }
        protected List<Parameter> GetParamList(List<int> paramIDs)
        {
            List<Parameter> list = new List<Parameter>();
            foreach (int i in paramIDs)
                list.Add(_paramManager.Parameters[i]);

            return list;
        }
        protected List<Parameter> GetParamList(List<string> paramNames)
        {
            List<Parameter> list = new List<Parameter>();
            foreach (string name in paramNames)
                list.Add(GetParam(name));

            return list;
        }
        protected List<Parameter> GetParamList(DummyParam dummyParam, int repeat)
        {
            List<Parameter> list = new List<Parameter>();
            for (int i = 0; i < repeat; i++)
                list.Add(_paramManager.Parameters[(int)dummyParam]);
            return list;
        }
        protected List<Parameter> GetParamList(string paramName, int pos, int size, DummyParam dummyParam)
        {
            List<Parameter> list = new List<Parameter>();
            for (int i = 0; i < size; i++)
                if (i == pos)
                    list.Add(GetParam(paramName));
                else
                    list.Add(_paramManager.Parameters[(int)dummyParam]);
            return list;
        }
        protected List<Parameter> GetParamList(int parID, int pos, int size, DummyParam dummyParam)
        {
            List<Parameter> list = new List<Parameter>();
            for (int i = 0; i < size; i++)
                if (i == pos)
                    list.Add(_paramManager.Parameters[parID]);
                else
                    list.Add(_paramManager.Parameters[(int)dummyParam]);
            return list;
        }
        protected Parameter GetParam(string paramName)
        {
            return _paramManager.GetParameter(paramName);
        }

        protected string GetResistOrFail(ResistStates resistStat, Drugs drug)
        {
            string resistOrFail = "";
            switch (resistStat)
            {
                case ResistStates.G_0:
                    resistOrFail = (drug == Drugs.A1) ? "A" : "B";
                    break;
                case ResistStates.G_A:
                    resistOrFail = (drug == Drugs.A1) ? "F" : "AB";
                    break;
                case ResistStates.G_B:
                    resistOrFail = (drug == Drugs.A1) ? "AB" : "F";
                    break;
                case ResistStates.G_AB:
                    resistOrFail = "F";
                    break;
            }
            return resistOrFail;
        }

        private Class_Normal Get_S(int id, string region, int parInitialSizeID)
        {
            Class_Normal S = new Class_Normal(id, region + " | S");
            S.SetupInitialAndStoppingConditions(
                initialMembersPar: _paramManager.Parameters[parInitialSizeID]);
            S.SetupTransmissionDynamicsProperties(
                susceptibilityParams: GetParamList(dummyParam: DummyParam.D_1, repeat: 4),
                infectivityParams: GetParamList(dummyParam: DummyParam.D_0, repeat: 4),
                rowIndexInContactMatrix: 0);
            SetupClassStatsAndTimeSeries(
                thisClass: S,
                showPrevalence: true);
            return S;
        }

        private Class_Normal Get_Success(int id, string region, string drug)
        {
            Class_Normal c = new Class_Normal(id, region + " | Success with " + drug);
            c.SetupInitialAndStoppingConditions(
                initialMembersPar: _paramManager.Parameters[(int)DummyParam.D_0]);
            c.SetupTransmissionDynamicsProperties(
                susceptibilityParams: GetParamList(dummyParam: DummyParam.D_0, repeat: 4),
                infectivityParams: GetParamList(dummyParam: DummyParam.D_0, repeat: 4),
                rowIndexInContactMatrix: 0);
            SetupClassStatsAndTimeSeries(
                thisClass: c,
                showIncidence: true);
            return c;
        }

        private Class_Death Get_D(int id, string region)
        {
            Class_Death D = new Class_Death(id, region + " | Death");
            SetupClassStatsAndTimeSeries(
                    thisClass: D,
                    showIncidence: true);
            return D;
        }

        private Class_Normal Get_I_W_U(int id, string region, int infProfileID,
            Comparts c, ResistStates r, int parIDSize, int infectivityParID)
        {
            Class_Normal C = new Class_Normal(
                ID: id,
                name: region + " | " + c.ToString() + " | " + _infProfiles[infProfileID]);
            C.SetupInitialAndStoppingConditions(
                initialMembersPar: _paramManager.Parameters[parIDSize],
                ifShouldBeEmptyForEradication: false);  // to simulate until the end of the simulation hirozon
            C.SetupTransmissionDynamicsProperties(
                susceptibilityParams: GetParamList(dummyParam: DummyParam.D_0, repeat: 4), // no reinfection in I, W, or U
                infectivityParams: GetParamList(
                    parID: infectivityParID,
                    pos: (int)r,
                    size: 4,
                    dummyParam: DummyParam.D_0),
                rowIndexInContactMatrix: 0);
            SetupClassStatsAndTimeSeries(
                thisClass: C,
                showPrevalence: (c == Comparts.I || c == Comparts.U) ? true : false,
                showIncidence: (c == Comparts.W) ? true : false);
            return C;
        }

        private Class_Splitting Get_IfSym(int id, string region, ResistStates r,
            int ifSymParID, int classIDIfSym, int classIDIfAsym)
        {
            Class_Splitting ifSym = new Class_Splitting(id, region + " | If Sym | " + r.ToString());
            ifSym.SetUp(
                parOfProbSucess: _paramManager.Parameters[ifSymParID],
                destinationClassIDIfSuccess: classIDIfSym,
                destinationClassIDIfFailure: classIDIfAsym);
            SetupClassStatsAndTimeSeries(thisClass: ifSym);
            return ifSym;
        }

        private Class_Splitting Get_IfRetreat(int id, string region,
            ResistStates r, SymStates s, Drugs drug, int infProfile, int parIDProbRetreat)
        {
            string resistOrFail = GetResistOrFail(resistStat: r, drug: drug);
            string className = region + " | If retreat " + resistOrFail + " | " + drug.ToString() + " --> I | " + _infProfiles[infProfile];

            string classIfSeekTreatment = "", classIfNotSeekTreatment = "";
            // if failed
            if (resistOrFail == "F")
            {
                // and seeks treatment -> waiting for retreatment
                classIfSeekTreatment = region + " | U | " + _infProfiles[infProfile];
                // and does not seek treatment -> the infectious state 
                classIfNotSeekTreatment = region + " | I | " + _infProfiles[infProfile];
            }
            else // if developed resistance
            {
                // update the infection profile
                string newInfProfile = s.ToString() + " | G_" + resistOrFail;
                // and seeks treatment -> waiting for retreatment
                classIfSeekTreatment = region + " | U | " + newInfProfile;
                // and does not seek treatment -> the infectious state
                classIfNotSeekTreatment = region + " | I | " + newInfProfile;
            }

            Class_Splitting ifRetreat = new Class_Splitting(id, className);
            ifRetreat.SetUp(
                parOfProbSucess: _paramManager.Parameters[parIDProbRetreat],
                destinationClassIDIfSuccess: _dicClasses[classIfSeekTreatment],
                destinationClassIDIfFailure: _dicClasses[classIfNotSeekTreatment]
                );
            SetupClassStatsAndTimeSeries(thisClass: ifRetreat);
            return ifRetreat;
        }

        private Class_Splitting Get_IfSymAfterR(int id, string region,
            string resistOrFail, ResistStates r, Drugs drug)
        {
            string className = region + " | If sym | " + resistOrFail + " | " + drug.ToString() + " --> I | Asym | " + r.ToString();
            string classIfSym = "", classIfAsym = "";

            // assuming that failure after B2 will receive M2
            if (drug == Drugs.A1 || drug == Drugs.B1)
                classIfSym = region + " | If retreat " + resistOrFail + " | " + drug.ToString() + " --> I | Sym | " + r.ToString();
            else
                classIfSym = region + " | Success with M2";
            classIfAsym = region + " | I | Asym | G_" + resistOrFail;

            Class_Splitting ifSymp = new Class_Splitting(id, className);
            ifSymp.SetUp(
                parOfProbSucess: GetParam("Prob sym | G_" + resistOrFail),
                destinationClassIDIfSuccess: _dicClasses[classIfSym],
                destinationClassIDIfFailure: _dicClasses[classIfAsym]
                );
            SetupClassStatsAndTimeSeries(thisClass: ifSymp);
            return ifSymp;
        }

        private Class_Splitting Get_ifR(int id, string region, string resistOrFail,
            ResistStates r, SymStates s, Drugs drug, int parIDProbResist, int classIDSuccess)
        {
            string strInfProfile = "I | " + s.ToString() + " | " + r.ToString();  // "I | Sym | G_0"                
            string treatmentProfile = resistOrFail + " | " + drug.ToString() + " --> " + strInfProfile; // "A | A --> I | Sym | G_0"
            string className = region + " | If " + treatmentProfile; // "If A | A --> I | Sym | G_0"
            string classIfResist = "";

            // find the destination classes
            if (drug == Drugs.A1 || drug == Drugs.B1)
            {
                // if already symptomatic 
                if (s == SymStates.Sym)
                    classIfResist = region + " | If retreat " + treatmentProfile;
                else // if not symtomatic
                    classIfResist = region + " | If sym | " + treatmentProfile;
            }
            else // if already received B2
            {
                classIfResist = region + " | U | " + s.ToString() + " | G_" + resistOrFail;
            }

            // make the splitting class
            Class_Splitting ifResist = new Class_Splitting(id, className);
            ifResist.SetUp(
                parOfProbSucess: _paramManager.Parameters[parIDProbResist],
                destinationClassIDIfSuccess: _dicClasses[classIfResist],
                destinationClassIDIfFailure: classIDSuccess);
            SetupClassStatsAndTimeSeries(thisClass: ifResist, showIncidence: true);
            return ifResist;
        }
    }
    
}
