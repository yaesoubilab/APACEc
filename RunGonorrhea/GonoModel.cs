using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APACElib;
using ComputationLib;

namespace RunGonorrhea
{
    public class GonoModel : ModelInstruction
    {
        enum Comparts { I, W, W2 };
        enum Drugs { A1, B1, B2};
        enum Ms { M1, M2};
        enum SymStates { Sym, Asym };
        enum ResistStates { G_0, G_A, G_B, G_AB }

        public GonoModel() : base()
        {
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
            AddSummationStatistics();
            // add ratio statistics
            AddRatioStatistics();
            // add features
            AddFeatures();
            // add conditions
            AddConditions();
            // add connections
            AddConnections();
        }

        private void AddGonoParameters()
        {
            // add the parameters from the parameter sheet
            AddParameters();
            int id = _paramManager.Parameters.Count;

            // add parameters of gonorrhea model 

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
                            paramNames.Add("1-Initial gonorrhea resistant to A or B");
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

            // add S
            Class_Normal S = new Class_Normal(id, "S");
            S.SetupInitialAndStoppingConditions(
                initialMembersPar: GetParam("Initial size of S"));
            S.SetupTransmissionDynamicsProperties(
                susceptibilityParams: GetParamList(name: "Dummy 1", repeat: 4),
                infectivityParams: GetParamList(name: "Dummy 0", repeat: 4),
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
                susceptibilityParams: GetParamList(name: "Dummy 0", repeat: 4),
                infectivityParams: GetParamList(name: "Dummy 0", repeat: 4),
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
                susceptibilityParams: GetParamList(name: "Dummy 0", repeat: 4),
                infectivityParams: GetParamList(name: "Dummy 0", repeat: 4),
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

            // add I's, W's, and W''s, 
            // example: "I | Sym | G_0"
            foreach (Comparts c in Enum.GetValues(typeof(Comparts)))
                foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                    foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                    {
                        string name = c.ToString() + " | " + s.ToString() + " | " + r.ToString();
                        Class_Normal cls = new Class_Normal(id, name);
                        
                        if (c == Comparts.I)
                            parInitialSize = "Initial size of " + name;
                        else
                            parInitialSize = "Dummy 0";

                        cls.SetupInitialAndStoppingConditions(
                            initialMembersPar: GetParam(parInitialSize),
                            ifShouldBeEmptyForEradication: false);  // to simulate until the end of the simulation hirozon
                        cls.SetupTransmissionDynamicsProperties(
                            susceptibilityParams: GetParamList(name: "Dummy 0", repeat: 4),
                            infectivityParams: GetParamList(
                                name: name, pos: (int)r, size: 4, remainingParName: "Dummy 0"),
                            rowIndexInContactMatrix: 0);
                        SetupClassStatsAndTimeSeries(
                            thisClass: cls,
                            showPrevalence: (c == Comparts.I) ? true : false);
                        _classes.Add(cls);
                        _dicClasses[name] = id++;
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
                    parOfProbSucess: GetParam("Prob sympt | " + rStr),
                    destinationClassIDIfSuccess: classIdIfSymp + (int)r,
                    destinationClassIDIfFailure: classIDIfAsymp + (int)r);
                SetupClassStatsAndTimeSeries(thisClass: ifSym);
                _classes.Add(ifSym);
                _dicClasses[name] = id++;
            }

            // if seeking retreatment after resistance or failure
            // examples "If retreat | A | A --> I | Sym | G_0"
            //          "If retreat | F | A --> I | Sym | G_A"
            foreach (Drugs drug in Enum.GetValues(typeof(Drugs)))
                foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                    foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                    {
                        string resistOrFail = GetResistOrFail(resistStat: r, drug: drug);
                        string infProfile = s.ToString() + " | " + r.ToString();
                        string className = "If retreat | " + resistOrFail + " | " + drug.ToString() + " --> I | " + infProfile;

                        string classIfSeekTreatment="", classIfNotSeekTreatment="";
                        // if failed
                        if (resistOrFail=="F")
                        {
                            // and seeks treatment -> waiting for retreatment
                            classIfSeekTreatment = "W' | " + s.ToString() + " | " + r.ToString();
                            // and does not seek treatment -> the infectious state 
                            classIfNotSeekTreatment = "I | " + infProfile;
                        }
                        else // if developed resistance
                        {
                            // update the infection profile
                            string  newInfProfile = s.ToString() + " | G_" + resistOrFail;
                            // and seeks treatment -> waiting for retreatment
                            classIfSeekTreatment = "W' | " + newInfProfile;
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
                    }

            // if symptomatic after the emergence of resistance
            // example: "If symp | A | A --> I | Asym | G_0" 
            foreach (Drugs drug in Enum.GetValues(typeof(Drugs)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    SymStates s = SymStates.Asym;
                    string resistOrFail = GetResistOrFail(resistStat: r, drug: drug);
                    // if developed resistance
                    if (resistOrFail != "F")
                    {
                        string infProfile = s.ToString() + " | " + r.ToString();
                        string newInfProfile = s.ToString() + " | G_" + resistOrFail;
                        string treatmentProfile = resistOrFail + " | " + drug.ToString() + " --> I |" + infProfile;
                        string className = "If symp | " + treatmentProfile;
                        
                        string classIfSym = "If retreat | " + treatmentProfile;
                        string classIfAsym = "I | " + newInfProfile;

                        Class_Splitting ifSymp = new Class_Splitting(id, className);
                        ifSymp.SetUp(
                            parOfProbSucess: GetParam("Prob resistance | Tx_" + resistOrFail),
                            destinationClassIDIfSuccess: _dicClasses[classIfSym],
                            destinationClassIDIfFailure: _dicClasses[classIfAsym]
                            );
                        SetupClassStatsAndTimeSeries(thisClass: ifSymp);
                        _classes.Add(ifSymp);
                        _dicClasses[className] = id++;
                    }
                }

            // treatment outcomes (resistance or failure)             
            int classIDIfTreatmentSuccess = _dicClasses["Success with " + Drugs.A1.ToString()];
            foreach (Drugs drug in Enum.GetValues(typeof(Drugs)))
                foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                    foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                    {
                        string resistOrFail = GetResistOrFail(resistStat: r, drug: drug);
                        string infProfile = s.ToString() + " | " + r.ToString();                        
                        string treatmentProfile = resistOrFail + " | " + drug.ToString() + " --> I |" + infProfile;
                        string className = "If " + treatmentProfile;
                        string classIfResistOrFailed = "";

                        // if failed treatment
                        if (resistOrFail == "F")
                        {
                            classIfResistOrFailed = "If retreat | " + treatmentProfile;
                        }
                        else // if developed resistance 
                        {
                            // if already symptomatic 
                            if (s == SymStates.Sym)
                            {
                                classIfResistOrFailed = "If retreat | " + treatmentProfile;
                            }
                            else // if not symtomatic
                            {
                                classIfResistOrFailed = "If symp | " + treatmentProfile;
                            }
                        }
                        Class_Splitting ifRorF = new Class_Splitting(id, className);
                        ifRorF.SetUp(
                            parOfProbSucess: GetParam( (resistOrFail=="F") ? "Dummy 1" : "Prob resistance | Tx_" + drug.ToString()),
                            destinationClassIDIfSuccess: _dicClasses[classIfResistOrFailed],
                            destinationClassIDIfFailure: classIDIfTreatmentSuccess + (int)drug
                            );
                        SetupClassStatsAndTimeSeries(thisClass: ifRorF);
                        _classes.Add(ifRorF);
                        _dicClasses[className] = id++;
                    }
        }

        private void AddGonoEvents()
        {
            int id = 0;

            // add birth events
            int birthRate = _paramManager.Dic["Annual birth rate (per pop member)"];
            int deathRate = _paramManager.Dic["Annual death rate"];
            int idS = _dicClasses["S"];
            int idDeath = _dicClasses["Death"];

            // main compartments: S, I
            List<string> mainComp = new List<string>();
            mainComp.Add("S");
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                    mainComp.Add("I " + s.ToString() + " | " + r.ToString());

            // add Birth and Death events
            foreach (string comp in mainComp)
            {
                _events.Add(new Event_Birth(
                    name: "Birth | " + comp,
                    ID: id++,
                    IDOfActivatingIntervention: 0,
                    rateParameter: _paramManager.Parameters[birthRate],
                    IDOfDestinationClass: idS)
                    );
                _events.Add(new Event_EpidemicIndependent(
                    name: "Death | " + comp,
                    ID: id++,
                    IDOfActivatingIntervention: 0,
                    rateParameter: _paramManager.Parameters[deathRate],
                    IDOfDestinationClass: idDeath)
                    );
            }



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
        private List<Parameter> GetParamList(string name, int repeat)
        {
            List<Parameter> list = new List<Parameter>();
            for (int i = 0; i < repeat; i++)
                list.Add(GetParam(name));
            return list;
        }
        private List<Parameter> GetParamList(string name, int pos, int size, string remainingParName)
        {
            List<Parameter> list = new List<Parameter>();
            for (int i = 0; i < size; i++)
                if (i == pos)
                    list.Add(GetParam(name));
                else
                    list.Add(GetParam(remainingParName));
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
