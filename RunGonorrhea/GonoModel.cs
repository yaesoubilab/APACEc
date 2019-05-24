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
        enum Comparts { I, W, W2};
        enum SymStates { Sym, Asym};
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

            // add S
            int id = 0;
            Class_Normal S = new Class_Normal(id++, "S");
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

            // add I's
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {

                }

            Class_Normal I = new Class_Normal(id++, "I");
            I.SetupInitialAndStoppingConditions(
                initialMembersPar: GetParam("Initial size of I"),
                ifShouldBeEmptyForEradication: true);
            I.SetupTransmissionDynamicsProperties(
                susceptibilityParams: GetParamList("Dummy 0"),
                infectivityParams: GetParamList("Infectivity"),
                rowIndexInContactMatrix: 0);
            SetupClassStatsAndTimeSeries(thisClass: I,
                collectAccumIncidenceStats: true,
                collectPrevalenceStats: true,
                showIncidence: true,
                showPrevalence: true,
                showAccumIncidence: false);
            _classes.Add(I);

            Class_Normal R = new Class_Normal(id++, "R");
            R.SetupInitialAndStoppingConditions(
                initialMembersPar: GetParam("Initial size of R"),
                ifShouldBeEmptyForEradication: false);
            R.SetupTransmissionDynamicsProperties(
                susceptibilityParams: GetParamList("Dummy 0"),
                infectivityParams: GetParamList("Dummy 0"),
                rowIndexInContactMatrix: 0);
            SetupClassStatsAndTimeSeries(thisClass: R,
                collectAccumIncidenceStats: false,
                collectPrevalenceStats: false,
                showIncidence: false,
                showPrevalence: true,
                showAccumIncidence: false);
            _classes.Add(R);

            Class_Splitting ifFastRecovery = new Class_Splitting(id++, "If fast recovery");
            ifFastRecovery.SetUp(
                parOfProbSucess: GetParam("Prob of fast recovery"),
                destinationClassIDIfSuccess: R.ID, 
                destinationClassIDIfFailure: I.ID);
            SetupClassStatsAndTimeSeries(thisClass: ifFastRecovery,
                collectAccumIncidenceStats: false,
                collectPrevalenceStats: false,
                showIncidence: false,
                showPrevalence: false,
                showAccumIncidence: false);
            _classes.Add(ifFastRecovery);
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
        private Parameter GetParam(string paramName)
        {
            return  _paramManager.GetParameter(paramName);
        }
    }
}
