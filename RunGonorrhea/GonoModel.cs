using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APACElib;
using ComputationLib;
using RandomVariateLib;

namespace RunGonorrhea
{
    public class GonoModel : ModelInstruction
    {
        enum Comparts { I, W, W2 };
        enum Drugs { A1, B1, B2};
        enum Ms { M1, M2};
        enum SymStates { Sym, Asym };
        enum ResistStates { G_0, G_A, G_B, G_AB }
        enum Interventions { A1 = 2, B1, M1, B2_A, M2_A, M2_A_AB}
        enum DummyParam { D_0, D_1, D_Minus1, D_Inf} // 0, 1, 2, 3

        private List<string> _infProfiles = new List<string>();

        public GonoModel() : base()
        {
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    _infProfiles.Add(s.ToString() + " | " + r.ToString());
                }
        }

        public override void BuildModel()
        {
            // add parameters 
            AddGonoParameters();
            // add classes
            AddGonoClasses();
            // add events
            AddGonoEvents();
            // add interventions
            AddInterventions();
            // add summation statistics
            AddGonoSumStats();
            // add ratio statistics
            AddRatioStatistics();
            // add features
            AddFeatures();
            // add conditions
            AddConditions();
            // add connections
            AddGonoConnections();
        }

        private void AddGonoParameters()
        {
            // add the parameters from the parameter sheet
            AddParameters();
            int id = _paramManager.Parameters.Count;

            // initial size of S
            _paramManager.Add(new ProductParameter(
                ID: id++,
                name: "Initial size of S",
                parameters: GetParamList(new List<string>() {"Initial N", "1-Initial prevalence"}))
                );

            // initial size of I compartments
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    string name = "Initial size of I | " + s.ToString() + " | " + r.ToString();
                    List<string> paramNames = new List<string>() { "Initial N", "Initial prevalence" };

                    if (s == SymStates.Sym)
                        paramNames.Add("Initial symptomatic");
                    else
                        paramNames.Add("1-Initial symptomatic");

                    switch (r)
                    {
                        case ResistStates.G_0:
                            paramNames.Add("1-Initial resistant to A or B");
                            break;
                        case ResistStates.G_A:
                            paramNames.Add("Initial resistant to A");
                            break;
                        case ResistStates.G_B:
                            paramNames.Add("Initial resistant to B");
                            break;
                        case ResistStates.G_AB:
                            paramNames.Add("Initial resistant to AB");
                            break;
                    }

                    if (r == ResistStates.G_AB)
                        _paramManager.Add(new IndependetParameter(
                            ID: id++,
                            name: name,
                            enumRandomVariateGenerator: RandomVariateLib.SupportProcedures.ConvertToEnumRVG("Constant"),
                            par1: 0, par2: 0, par3: 0, par4: 0)
                            );
                    else
                        _paramManager.Add(new ProductParameter(
                            ID: id++,
                            name: name,
                            parameters: GetParamList(paramNames))
                            );
                }

        }

        private void AddGonoClasses()
        {
            int classIdIfSymp = 0;
            int classIDIfAsymp = 0;
            string parInitialSize;
            int id = 0;
            int inf = 0; // infection profile

            // add S
            Class_Normal S = new Class_Normal(id, "S");
            S.SetupInitialAndStoppingConditions(
                initialMembersPar: GetParam("Initial size of S"));
            S.SetupTransmissionDynamicsProperties(
                susceptibilityParams: GetParamList(dummyParam: DummyParam.D_1, repeat: 4),
                infectivityParams: GetParamList(dummyParam: DummyParam.D_0, repeat: 4),
                rowIndexInContactMatrix: 0);
            SetupClassStatsAndTimeSeries(
                thisClass: S,
                showPrevalence:true);
            _classes.Add(S);
            _dicClasses[S.Name] = id++;

            // add classes to count the treatment outcomes
            // Success with A1, B1, or B2
            foreach (Drugs d in Enum.GetValues(typeof(Drugs)))
            {
                Class_Normal c = new Class_Normal(id, "Success with " + d.ToString());
                S.SetupInitialAndStoppingConditions(
                initialMembersPar: GetParam("Dummy 0"));
                S.SetupTransmissionDynamicsProperties(
                susceptibilityParams: GetParamList(dummyParam: DummyParam.D_0, repeat: 4),
                infectivityParams: GetParamList(dummyParam: DummyParam.D_0, repeat: 4),
                rowIndexInContactMatrix: 0);
                SetupClassStatsAndTimeSeries(
                    thisClass: c,
                    showIncidence: true);
                _classes.Add(c);
                _dicClasses[c.Name] = id++;
            }
            // Success with M1 or M2
            foreach (Ms m in Enum.GetValues(typeof(Ms)))
            {
                Class_Normal c = new Class_Normal(id, "Success with " + m.ToString());
                S.SetupInitialAndStoppingConditions(
                initialMembersPar: GetParam("Dummy 0"));
                S.SetupTransmissionDynamicsProperties(
                susceptibilityParams: GetParamList(dummyParam: DummyParam.D_0, repeat: 4),
                infectivityParams: GetParamList(dummyParam: DummyParam.D_0, repeat: 4),
                rowIndexInContactMatrix: 0);
                SetupClassStatsAndTimeSeries(
                    thisClass: c,
                    showIncidence: true);
                _classes.Add(c);
                _dicClasses[c.Name] = id++;
            }

            // add death
            Class_Death D = new Class_Death(id, "Death");
            _classes.Add(D);
            _dicClasses[D.Name] = id++;

            // add I's, W's, and W2's, 
            // example: "I | Sym | G_0"            
            foreach (Comparts c in Enum.GetValues(typeof(Comparts)))
            {
                inf = 0;
                foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                    foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                    {
                        string name = c.ToString() + " | " + _infProfiles[inf];
                        string parName = "Infectivity of | " + Comparts.I.ToString() + " | " + _infProfiles[inf];
                        Class_Normal cls = new Class_Normal(id, name);

                        if (c == Comparts.I)
                            parInitialSize = "Initial size of " + name;
                        else // else for W and W2 the initial size if 0
                            parInitialSize = "Dummy 0";

                        cls.SetupInitialAndStoppingConditions(
                            initialMembersPar: GetParam(parInitialSize),
                            ifShouldBeEmptyForEradication: false);  // to simulate until the end of the simulation hirozon
                        cls.SetupTransmissionDynamicsProperties(
                            susceptibilityParams: GetParamList(dummyParam: DummyParam.D_0, repeat: 4), // no reinfection in I, W, or W2
                            infectivityParams: GetParamList(
                                paramName: parName,
                                pos: (int)r,
                                size: 4,
                                dummyParam: DummyParam.D_0),
                            rowIndexInContactMatrix: 0);
                        SetupClassStatsAndTimeSeries(
                            thisClass: cls,
                            showPrevalence: (c == Comparts.I) ? true : false,
                            showIncidence: (c == Comparts.W) ? true : false);
                        _classes.Add(cls);
                        _dicClasses[name] = id++;
                        ++inf;
                    }
            }

            // Prob symptomatic after infection
            classIdIfSymp = _dicClasses["I | Sym | G_0"];
            classIDIfAsymp = _dicClasses["I | Asym | G_0"];
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
            {
                string rStr = r.ToString();
                string name = "If Sym | " + rStr;

                Class_Splitting ifSym = new Class_Splitting(id, name);
                ifSym.SetUp(
                    parOfProbSucess: GetParam("Prob sym | " + rStr),
                    destinationClassIDIfSuccess: classIdIfSymp + (int)r,
                    destinationClassIDIfFailure: classIDIfAsymp + (int)r);
                SetupClassStatsAndTimeSeries(thisClass: ifSym);
                _classes.Add(ifSym);
                _dicClasses[name] = id++;
            }

            // if seeking retreatment after resistance or failure
            // examples "If retreat A | A --> I | Sym | G_0"
            //          "If retreat F | A --> I | Sym | G_A"
            foreach (Drugs drug in Enum.GetValues(typeof(Drugs)))   // A1, B1, B2
                if (drug == Drugs.A1 || drug == Drugs.B1)  // Assuming that failure after B2 will always seek retreatment 
                {
                    inf = 0;
                    foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                        foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                        {
                            string resistOrFail = GetResistOrFail(resistStat: r, drug: drug);
                            string className = "If retreat " + resistOrFail + " | " + drug.ToString() + " --> I | " + _infProfiles[inf];

                            string classIfSeekTreatment = "", classIfNotSeekTreatment = "";
                            // if failed
                            if (resistOrFail == "F")
                            {
                                // and seeks treatment -> waiting for retreatment
                                classIfSeekTreatment = "W2 | " + s.ToString() + " | " + r.ToString();
                                // and does not seek treatment -> the infectious state 
                                classIfNotSeekTreatment = "I | " + _infProfiles[inf];
                            }
                            else // if developed resistance
                            {
                                // update the infection profile
                                string newInfProfile = s.ToString() + " | G_" + resistOrFail;
                                // and seeks treatment -> waiting for retreatment
                                classIfSeekTreatment = "W2 | " + newInfProfile;
                                // and does not seek treatment -> the infectious state
                                classIfNotSeekTreatment = "I | " + newInfProfile;
                            }

                            Class_Splitting ifRetreat = new Class_Splitting(id, className);
                            ifRetreat.SetUp(
                                parOfProbSucess: GetParam("Prob retreatment | " + s.ToString()),
                                destinationClassIDIfSuccess: _dicClasses[classIfSeekTreatment],
                                destinationClassIDIfFailure: _dicClasses[classIfNotSeekTreatment]
                                );
                            SetupClassStatsAndTimeSeries(thisClass: ifRetreat);
                            _classes.Add(ifRetreat);
                            _dicClasses[className] = id++;
                            ++inf;
                        }
                }

            // if symptomatic after the emergence of resistance
            // example: "If symp | A | A --> I | Asym | G_0"
            //          true    -> "If retreat A | A --> I | Sym | G_0"
            //          false   -> "I | Asym | G_A"         
            foreach (Drugs drug in Enum.GetValues(typeof(Drugs))) // A1, B1, or B2
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    string resistOrFail = GetResistOrFail(resistStat: r, drug: drug);
                    // if developed resistance
                    if (resistOrFail != "F")
                    {                        
                        string className = "If sym | " + resistOrFail + " | " + drug.ToString() + " --> I | Asym | " + r.ToString();
                        string classIfSym = "", classIfAsym = "";

                        // assuming that failure after B2 will receive M2
                        if (drug == Drugs.A1 || drug == Drugs.B1)
                            classIfSym = "If retreat " + resistOrFail + " | " + drug.ToString() + " --> I | Sym | " + r.ToString();
                        else
                            classIfSym = "Success with M2";
                        classIfAsym = "I | Asym | G_" + resistOrFail;

                        Class_Splitting ifSymp = new Class_Splitting(id, className);
                        ifSymp.SetUp(
                            parOfProbSucess: GetParam("Prob sym | G_" + resistOrFail),
                            destinationClassIDIfSuccess: _dicClasses[classIfSym],
                            destinationClassIDIfFailure: _dicClasses[classIfAsym]
                            );
                        SetupClassStatsAndTimeSeries(thisClass: ifSymp);
                        _classes.Add(ifSymp);
                        _dicClasses[className] = id++;   
                    }
                }

            // treatment outcomes (resistance)    
            // example: "If A | A --> I | Sym | G_0"
            //          true: "If retreat A | A --> I | Sym | G_0"
            //          false: "Success A1"
            // example: "If A | A --> I | Asym | G_0"
            //          true: "If sym | A | A --> I | Asym | G_0"
            //          false: "Success A1"
            int classIDSuccessA1 = _dicClasses["Success with " + Drugs.A1.ToString()];
            foreach (Drugs drug in Enum.GetValues(typeof(Drugs))) // A1, B1, B2
                foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                    foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                    {
                        // considering only resistance outcome
                        string resistOrFail = GetResistOrFail(resistStat: r, drug: drug);
                        if (resistOrFail == "F")
                            continue;

                        string infProfile = "I | " + s.ToString() + " | " + r.ToString();  // "I | Sym | G_0"                
                        string treatmentProfile = resistOrFail + " | " + drug.ToString() + " --> " + infProfile; // "A | A --> I | Sym | G_0"
                        string className = "If " + treatmentProfile; // "If A | A --> I | Sym | G_0"
                        string classIfResist = "";

                        // find the destination classes
                        if (drug == Drugs.A1 || drug == Drugs.B1)
                        {
                            // if already symptomatic 
                            if (s == SymStates.Sym)
                                classIfResist = "If retreat " + treatmentProfile;
                            else // if not symtomatic
                                classIfResist = "If sym | " + treatmentProfile;
                        }
                        else // if already received B2
                        {
                            //if (r != ResistStates.G_A)
                            //    continue;
                            classIfResist = "W2 | " + s.ToString() + " | G_" + resistOrFail;
                        }

                        // parameter name
                        string paramName = (drug == Drugs.A1) ? "Prob resistance | Drug A" : "Prob resistance | Drug B";

                        // make the splitting class
                        Class_Splitting ifResist = new Class_Splitting(id, className);
                        ifResist.SetUp(
                            parOfProbSucess: GetParam(paramName),
                            destinationClassIDIfSuccess: _dicClasses[classIfResist],
                            destinationClassIDIfFailure: classIDSuccessA1 + (int)drug
                            );
                        SetupClassStatsAndTimeSeries(thisClass: ifResist);
                        _classes.Add(ifResist);
                        _dicClasses[className] = id++;
                    }
        }

        private void AddGonoEvents()
        {
            int id = 0;
            string eventName = "";
            // rates
            int infRate = _paramManager.Dic["Dummy Inf"];
            int birthRate = _paramManager.Dic["Annual birth rate (per pop member)"];
            int deathRate = _paramManager.Dic["Annual death rate"];
            int naturalRecoveryRate = _paramManager.Dic["Natural recovery"];
            int screeningRate = _paramManager.Dic["Annual screening rate"];
            int seekingTreatmentRate = _paramManager.Dic["Annual rate of seeking treatment (symptomatic)"];
            int seekingReTreatmentRate = _paramManager.Dic["Annual rate of retreatment"];
            
            int idS = _dicClasses["S"];
            int idDeath = _dicClasses["Death"];
            int idSuccessM1 = _dicClasses["Success with " + Ms.M1.ToString()];
            int idSuccessM2 = _dicClasses["Success with " + Ms.M2.ToString()];

            // main compartments: S, I
            List<string> mainComp = new List<string>();
            mainComp.Add("S");
            for (int inf = 0; inf <_infProfiles.Count; inf ++)
                mainComp.Add("I | " + _infProfiles[inf]);

            // add Birth events
            foreach (string comp in mainComp)
            {
                eventName = "Birth | " + comp;
                _events.Add(new Event_Birth(
                    name: eventName,
                    ID: id,
                    IDOfActivatingIntervention: 0,
                    rateParameter: _paramManager.Parameters[birthRate],
                    IDOfDestinationClass: idS)
                    );
                _dicEvents[eventName] = id++;
            }
            // add Death events
            foreach (string comp in mainComp)
            {
                eventName = "Death | " + comp;
                _events.Add(new Event_EpidemicIndependent(
                    name: eventName,
                    ID: id,
                    IDOfActivatingIntervention: 0,
                    rateParameter: _paramManager.Parameters[deathRate],
                    IDOfDestinationClass: idDeath)
                    );
                _dicEvents[eventName] = id++;
            }
            

            // add Infection events
            int idIfSympG_0 = _dicClasses["If Sym | G_0"];
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
            {
                eventName = "Infection | " + r.ToString();
                _events.Add(new Event_EpidemicDependent(
                    name: eventName,
                    ID: id,
                    IDOfActivatingIntervention: 0,
                    IDOfPathogenToGenerate: (int)r,
                    IDOfDestinationClass: idIfSympG_0 + (int)r)
                    );
                _dicEvents[eventName] = id++;
            }

            // add Natual Recovery events
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    string infProfile = s.ToString() + " | " + r.ToString();
                    eventName = "Natural Recovery | I | " + infProfile;
                    _events.Add(new Event_EpidemicIndependent(
                        name: eventName,
                        ID: id,
                        IDOfActivatingIntervention: 0,
                        rateParameter: _paramManager.Parameters[naturalRecoveryRate],
                        IDOfDestinationClass: idS)
                    );
                    _dicEvents[eventName] = id++;
                }

            // add Seeking Treatment events
            int idWSymG_0 = _dicClasses["W | Sym | G_0"];
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    string infProfile = s.ToString() + " | " + r.ToString();
                    eventName = "Seeking Treatment | I | " + infProfile;
                    _events.Add(new Event_EpidemicIndependent(
                        name: eventName,
                        ID: id,
                        IDOfActivatingIntervention: 0,
                        rateParameter: (s == SymStates.Sym) ? _paramManager.Parameters[seekingTreatmentRate] : _paramManager.Parameters[(int)DummyParam.D_0],
                        IDOfDestinationClass: idWSymG_0 + (int)r)
                    );
                    _dicEvents[eventName] = id++;
                }

            // add Screening events
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    string infProfile = s.ToString() + " | " + r.ToString();
                    eventName = "Screening | I | " + infProfile;
                    _events.Add(new Event_EpidemicIndependent(
                        name: eventName,
                        ID: id,
                        IDOfActivatingIntervention: 0,
                        rateParameter: _paramManager.Parameters[screeningRate],
                        IDOfDestinationClass: idWSymG_0 + 2*(int)s + (int)r)
                    );
                    _dicEvents[eventName] = id++;
                }

            // add First-Line Treatment with A1 and B1
            foreach (Drugs d in Enum.GetValues(typeof(SymStates)))
                if (d == Drugs.A1 || d == Drugs.B1)
                    foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                        foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                        {
                            string resistOrFail = GetResistOrFail(resistStat: r, drug: d);
                            string infProfile = s.ToString() + " | " + r.ToString();
                            string treatmentProfile = resistOrFail + " | " + d.ToString() + " --> I | " + infProfile;
                            eventName = "Tx_" + d.ToString() + " | W | " + infProfile;
                            string destClassName = "";

                            if (resistOrFail == "F")
                                destClassName = "If retreat " + treatmentProfile;
                            else
                                destClassName = "If " + treatmentProfile;

                            _events.Add(new Event_EpidemicIndependent(
                                name: eventName,
                                ID: id,
                                IDOfActivatingIntervention: (d == Drugs.A1) ? (int)Interventions.A1 : (int)Interventions.B1,
                                rateParameter: _paramManager.Parameters[infRate],
                                IDOfDestinationClass: _dicClasses[destClassName])
                                );
                            _dicEvents[eventName] = id++;
                        }

            // add First-Line Treatment with M1
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    string infProfile = s.ToString() + " | " + r.ToString();
                    eventName = "Tx_M1 | W | " + infProfile;
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
                    eventName = "Tx_B2 | W2 | " + infProfile;

                    string destClassName = "";
                    if (resistOrFail == "F")
                        // if treatment failure occurs, the patient will receive M2 
                        destClassName = "Success with M2";
                    else
                        destClassName = "If " + treatmentProfile;

                    _events.Add(new Event_EpidemicIndependent(
                        name: eventName,
                        ID: id,
                        IDOfActivatingIntervention: (int)Interventions.M2_A,
                        rateParameter: (r == ResistStates.G_A) ? _paramManager.Parameters[(int)DummyParam.D_0] : _paramManager.Parameters[seekingReTreatmentRate],
                        IDOfDestinationClass: _dicClasses[destClassName])
                        );
                    _dicEvents[eventName] = id++;
                }

            // add Second-Line Treatment with M2              
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    string infProfile = s.ToString() + " | " + r.ToString();
                    eventName = "Tx_M2 | W2 | " + infProfile;
                    _events.Add(new Event_EpidemicIndependent(
                        name: eventName,
                        ID: id,
                        IDOfActivatingIntervention: (r == ResistStates.G_A) ? (int)Interventions.M2_A : (int)Interventions.M2_A_AB,
                        rateParameter: (r == ResistStates.G_A) ? _paramManager.Parameters[(int)DummyParam.D_0] : _paramManager.Parameters[seekingReTreatmentRate],
                        IDOfDestinationClass: idSuccessM2)
                        );
                    _dicEvents[eventName] = id++;
                }

            // add Leaving Success with A1, B1, or B2
            foreach (Drugs d in Enum.GetValues(typeof(Drugs)))
            {
                eventName = "Leaving Success with " + d.ToString();
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
                eventName = "Leaving Success with " + m.ToString();
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

        private void AddGonoSumStats()
        {
            int id = 0;
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

            // gonorrhea prevalence formulas
            string pFormula = "";
            List<string> pSymFormula = new List<string>() { "", "" }; // Sym, Asym
            List<string> pResistFormula = new List<string>() { "", "", "" }; // A, B, AB
            foreach (Class c in _classes.Where(c => c is Class_Normal ))
            {
                if (c.Name.First() == 'I' || c.Name.First() == 'W')
                {
                    pFormula += c.ID + "+";
                    if (c.Name.Contains("Sym"))
                        pSymFormula[0] += c.ID + "+";
                    else
                        pSymFormula[1] += c.ID + "+";

                    if (c.Name.Substring(c.Name.Length - 2) == "_A")
                        pResistFormula[0] += c.ID + "+";
                    else if (c.Name.Substring(c.Name.Length-2) == "_B")
                        pResistFormula[1] += c.ID + "+";
                    else if (c.Name.Substring(c.Name.Length - 2) == "AB")
                        pResistFormula[2] += c.ID + "+";
                }
            }

            // gonorrhea prevalence
            _epiHist.SumTrajs.Add(
                new SumClassesTrajectory(
                    ID: id++,
                    name: "Prevalence",
                    strType: "Prevalence",
                    sumFormula: pFormula,
                    displayInSimOutput: true,
                    warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                    );

            // gonorrhea prevalence by symptom status
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
            {
                _epiHist.SumTrajs.Add(
                    new SumClassesTrajectory(
                        ID: id++,
                        name: "Prevalence | " + s.ToString(),
                        strType: "Prevalence",
                        sumFormula: pSymFormula[(int)s],
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
                            sumFormula: pResistFormula[(int)r-1],
                            displayInSimOutput: true,
                            warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                            nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                            );

            // first-line treatment 
            string treatedFormula = "", treatedAndSymFormula = "";
            List<string> treatedResistFormula = new List<string>() { "", "", "" }; // A, B, AB
            foreach (Class c in _classes.Where(c => c is Class_Normal))
            {
                if (c.Name.Length > 1 && c.Name.Substring(0, 2) == "W ")
                {
                    treatedFormula += c.ID + "+";
                    if (c.Name.Contains("Sym"))
                        treatedAndSymFormula += c.ID + "+";

                    if (c.Name.Substring(c.Name.Length - 2) == "_A")
                        treatedResistFormula[0] += c.ID + "+";
                    else if (c.Name.Substring(c.Name.Length - 2) == "_B")
                        treatedResistFormula[1] += c.ID + "+";
                    else if (c.Name.Substring(c.Name.Length - 2) == "AB")
                        treatedResistFormula[2] += c.ID + "+";
                }
            }

            // received first-line treatment (= number of cases)
            SumClassesTrajectory t1st = new SumClassesTrajectory(
                ID: id++,
                name: "Received 1st Tx",
                strType: "Incidence",
                sumFormula: treatedFormula,
                displayInSimOutput: true,
                warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);
            UpdateClassTimeSeries(t1st);
            t1st.DeltaCostHealthCollector =
                new DeltaTCostHealth(
                    deltaT: _modelSets.DeltaT, 
                    warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                    DALYPerNewMember: GetParam("Dummy 1"),
                    costPerNewMember: GetParam("Dummy 0")
                    );
            _epiHist.SumTrajs.Add(t1st);

            // received first-line treatment and symptomatic 
            _epiHist.SumTrajs.Add(new SumClassesTrajectory(
                ID: id++,
                name: "Received 1st Tx & Symptomatic",
                strType: "Incidence",
                sumFormula: treatedAndSymFormula,
                displayInSimOutput: true,
                warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                );

            // received first-line treatment by resistance status
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates))) // G_0, G_A, G_B, G_AB
                if (r != ResistStates.G_0)
                    _epiHist.SumTrajs.Add(
                        new SumClassesTrajectory(
                            ID: id++,
                            name: "Received 1st Tx & Resistant to " + r.ToString(),
                            strType: "Incidence",
                            sumFormula: treatedResistFormula[(int)r-1],
                            displayInSimOutput: true,
                            warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                            nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                            );

            // sucessful treatment
            string treatedA1 = _classes[_dicClasses["Success with A1"]].ID.ToString();
            string treatedB1 = _classes[_dicClasses["Success with B1"]].ID.ToString();
            string treatedB2 = _classes[_dicClasses["Success with B2"]].ID.ToString();
            string treatedM1 = _classes[_dicClasses["Success with M1"]].ID.ToString();
            string treatedM2 = _classes[_dicClasses["Success with M2"]].ID.ToString();

            string success1st = treatedA1 + "+" + treatedB1 + "+" + treatedM1;
            string successAorB = treatedA1 + "+" + treatedB1 + "+" + treatedB2;
            string successM = treatedM1 + "+" + treatedM2;
            string successAll = successAorB + "+" + successM;

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

            // # sucessfully treated with M
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
                    DALYPerNewMember: GetParam("Dummy 0"),
                    costPerNewMember: GetParam("Dummy 1")
                    );
            _epiHist.SumTrajs.Add(tM);

            // # sucessfully treated 
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
        }

        private void AddGonoConnections()
        {
            int i = 0;           
            int birthID = _dicEvents["Birth | S"];
            int deathID = _dicEvents["Death | S"];
            int infectionID = _dicEvents["Infection | G_0"];
            int naturalRecoveryID = _dicEvents["Natural Recovery | I | Sym | G_0"];
            int seekingTreatmentID = _dicEvents["Seeking Treatment | I | Sym | G_0"];
            int screeningID = _dicEvents["Screening | I | Sym | G_0"];
            int txA = _dicEvents["Tx_A1 | W | " + _infProfiles[0]];
            int txB = _dicEvents["Tx_B1 | W | " + _infProfiles[0]]; 
            int txM = _dicEvents["Tx_M1 | W | " + _infProfiles[0]]; 
            int txB2 = _dicEvents["Tx_B2 | W2 | " + _infProfiles[0]];
            int txM2 = _dicEvents["Tx_M2 | W2 | " + _infProfiles[0]];
            int leaveSuccess = _dicEvents["Leaving Success with A1"];
            int success = _dicClasses["Success with A1"];

            // ----------------
            // add events for S
            Class_Normal S = (Class_Normal)_classes[_dicClasses["S"]];
            // birth and death
            S.AddAnEvent(_events[birthID]);
            S.AddAnEvent(_events[deathID]);
            // infections
            i = 0;
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                S.AddAnEvent(_events[infectionID + i++]);

            // ----------------
            // add events for I, W, W2
            i = 1;
            int w = 0, w2 = 0;
            foreach (Class c in _classes.Where(c => (c is Class_Normal)))
            {
                // for I
                if (c.Name.StartsWith("I"))
                {                    
                    ((Class_Normal)c).AddAnEvent(_events[birthID + i++]);
                    ((Class_Normal)c).AddAnEvent(_events[deathID + i++]);
                    ((Class_Normal)c).AddAnEvent(_events[naturalRecoveryID + i++]);
                    ((Class_Normal)c).AddAnEvent(_events[seekingTreatmentID + i++]);
                    ((Class_Normal)c).AddAnEvent(_events[screeningID + i++]);
                }
                // for W
                else if (c.Name.Substring(0,2) == "W ")
                {
                    ((Class_Normal)c).AddAnEvent(_events[txA + w++]);
                    ((Class_Normal)c).AddAnEvent(_events[txB+ w++]);
                    ((Class_Normal)c).AddAnEvent(_events[txM+ w++]);
                }
                else if (c.Name.Substring(0, 2) == "W2")
                {
                    ((Class_Normal)c).AddAnEvent(_events[txB2 + w2++]);
                    ((Class_Normal)c).AddAnEvent(_events[txM + w2++]);
                }
            }

            // add leaving success with A1, B1, B2, M1, M2
            i = 0;
            for (int j = 0; j<5; j++)
                ((Class_Normal)_classes[success + j]).AddAnEvent(_events[leaveSuccess + j]);
        }

        private List<Parameter> GetParamList(string paramName)
        {            
            return new List<Parameter>() { _paramManager.GetParameter(paramName) };
        }
        private List<Parameter> GetParamList( List<string> paramNames)
        {
            List<Parameter> list = new List<Parameter>();
            foreach (string name in paramNames)
                list.Add(GetParam(name));

            return list;
        }
        private List<Parameter> GetParamList(DummyParam dummyParam, int repeat)
        {
            List<Parameter> list = new List<Parameter>();
            for (int i = 0; i < repeat; i++)
                list.Add(_paramManager.Parameters[(int)dummyParam]);
            return list;
        }
        private List<Parameter> GetParamList(string paramName, int pos, int size, DummyParam dummyParam)
        {
            List<Parameter> list = new List<Parameter>();
            for (int i = 0; i < size; i++)
                if (i == pos)
                    list.Add(GetParam(paramName));
                else
                    list.Add(_paramManager.Parameters[(int)dummyParam]);
            return list;
        }
        private Parameter GetParam(string paramName)
        {
            return  _paramManager.GetParameter(paramName);
        }

        private string GetResistOrFail(ResistStates resistStat, Drugs drug)
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
    }
}
