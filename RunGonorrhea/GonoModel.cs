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
        enum Drugs { Tx_A, Tx_B, Tx_B2};
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
            AddClasses();
            // add events
            AddEvents();
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
            int idIfSuccess = 0;
            int idIfFailure = 0;
            string parInitialSize;


            // add S
            int id = 0;
            Class_Normal S = new Class_Normal(id, "S");
            S.SetupInitialAndStoppingConditions(
                initialMembersPar: GetParam("Initial size of S"),
                ifShouldBeEmptyForEradication: false);
            S.SetupTransmissionDynamicsProperties(
                susceptibilityParams: GetParamList(name: "Dummy 1", repeat: 4),
                infectivityParams: GetParamList(name: "Dummy 0", repeat: 4),
                rowIndexInContactMatrix: 0);
            SetupClassStatsAndTimeSeries(thisClass: S,
                collectAccumIncidenceStats:false,
                collectPrevalenceStats:false,
                showIncidence:false,
                showPrevalence:true,
                showAccumIncidence:false);
            _classes.Add(S);
            _dicClasses["S"] = id++;

            // add I's, W's, and W''s, 
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
                        SetupClassStatsAndTimeSeries(thisClass: cls,
                            collectAccumIncidenceStats: false,
                            collectPrevalenceStats: false,
                            showIncidence: false,
                            showPrevalence: (c == Comparts.I) ? true : false,
                            showAccumIncidence: false);
                        _classes.Add(cls);
                        _dicClasses[name] = id++;
                    }

            // Prob symptomatic 
            idIfSuccess = _dicClasses["I | Sym | " + ResistStates.G_0];
            idIfFailure = _dicClasses["I | Asym | " + ResistStates.G_0];
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
            {
                string rStr = r.ToString();
                string name = "If Sympt | " + rStr;

                Class_Splitting ifSympt = new Class_Splitting(id, name);
                ifSympt.SetUp(
                    parOfProbSucess: GetParam("Prob sympt | " + rStr),
                    destinationClassIDIfSuccess: idIfSuccess + (int)r,
                    destinationClassIDIfFailure: idIfFailure + (int)r);
                SetupClassStatsAndTimeSeries(thisClass: ifSympt,
                    collectAccumIncidenceStats: false,
                    collectPrevalenceStats: false,
                    showIncidence: false,
                    showPrevalence: false,
                    showAccumIncidence: false);
                _classes.Add(ifSympt);
                _dicClasses[name] = id++;
            }

            // if seeking retreatment after resistance or failure
            
            // treatment outcomes 
            foreach (Drugs d in Enum.GetValues(typeof(Drugs)))
                foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                    foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                    {
                        string resistOrFail="";
                        switch (r)
                        {
                            case ResistStates.G_0:
                                resistOrFail = "R";
                                break;
                            case ResistStates.G_A:
                                resistOrFail = (d == Drugs.Tx_A) ? "F" : "R";
                                break;
                            case ResistStates.G_B:
                                resistOrFail = (d == Drugs.Tx_B) ? "F" : "R";
                                break;
                            case ResistStates.G_AB:
                                resistOrFail = "F";
                                break;
                        }
                        string infectedName = "I | " + s.ToString() + " | " + r.ToString();
                        string name = "If " + resistOrFail + " | " + d.ToString() + " --> " + infectedName;

                        Class_Splitting ifSympt = new Class_Splitting(id, name);
                        ifSympt.SetUp(
                            parOfProbSucess: GetParam( (resistOrFail=="R") ? "Prob resistance | " + d.ToString() : "Dummy 1"),
                            destinationClassIDIfSuccess: ????,
                            destinationClassIDIfFailure: ????
                            );
                        SetupClassStatsAndTimeSeries(thisClass: ifSympt,
                            collectAccumIncidenceStats: false,
                            collectPrevalenceStats: false,
                            showIncidence: false,
                            showPrevalence: false,
                            showAccumIncidence: false);
                        _classes.Add(ifSympt);
                        _dicClasses[name] = id++;
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
    }
}
