using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APACElib;
using ComputationLib;

namespace RunSIR
{
    public class SIRModel : ModelInstruction
    {
        public SIRModel(): base()
        {
        }

        public override void BuildModel()
        {
            // add parameters 
            AddParameters();
            // add classes
            AddSIRClasses();
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

        private void AddSIRClasses()
        {
            int id = 0;
            Class_Normal S = new Class_Normal(id++, "S");
            S.SetupInitialAndStoppingConditions(
                initialMembersPar: GetParam("Initial size of S"),
                ifShouldBeEmptyForEradication: false);
            S.SetupTransmissionDynamicsProperties(
                susceptibilityParams: GetParamList("Dummy 1"),
                infectivityParams: GetParamList("Dummy 0"),
                rowIndexInContactMatrix: 0);
            SetupClassStatsAndTimeSeries(thisClass: S,
                collectAccumIncidenceStats:false,
                collectPrevalenceStats:false,
                showIncidence:false,
                showPrevalence:true,
                showAccumIncidence:false);
            _classes.Add(S);

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
        private Parameter GetParam(string paramName)
        {
            return  _paramManager.GetParameter(paramName);
        }
    }
}
