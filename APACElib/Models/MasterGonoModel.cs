using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APACElib;
using ComputationLib;
using RandomVariateLib;

namespace APACElib
{
    enum GonoSpecialStatIDs { PopSize = 0, Prev, FirstTx, SuccessAOrB, SuccessAOrBOrM, PercFirstTxAndResist }

    public class GonoSpecialStatInfo
    {        
        public List<int> SpecialStatIDs { get; set; } = new List<int>(new int[Enum.GetValues(typeof(GonoSpecialStatIDs)).Length]);
        public string Prev { get; set; } = "";
        public List<string> PrevSym { get; set; } = new List<string>() { "", "" }; // Sym, Asym
        public List<string> PrevResist { get; set; } = new List<string>() { "", "", "" }; // A, B, AB

        public string Treated { get; set; } = "";
        public string TreatedAndSym { get; set; } = "";
        public List<string> TreatedResist { get; set; } = new List<string>() { "", "", "" }; // A, B, AB

        public void Reset()
        {
            SpecialStatIDs = new List<int>(new int[Enum.GetValues(typeof(GonoSpecialStatIDs)).Length]);
            Prev = "";
            PrevSym = new List<string>() { "", "" }; // Sym, Asym
            PrevResist = new List<string>() { "", "", "" }; // A, B, AB
            Treated = "";
            TreatedAndSym = "";
            TreatedResist = new List<string>() { "", "", "" }; // A, B, AB
        }
    }


    public abstract class GonoModel : ModelInstruction
    {
        protected enum Comparts { I, W, U }; // infection, waiting for treatment, waiting for retreatment 
        protected enum Drugs { A1, B1, B2 }; // 1st line treatment with A, 1st line treatment with B, and 2nd line treatment with B
        protected enum Ms { M1, M2 };  // 1st line treatment with M, and 2nd line treatment with M
        protected enum SymStates { Sym, Asym };
        protected enum ResistStates { G_0, G_A, G_B, G_AB }
        protected enum Interventions { A1 = 2, B1, M1, B2_A, M2_A, M2_B_AB } // A1:    A is used for 1st line treatment
                                                                             // B1:    B is used for 1st line treatment
                                                                             // M1:    M is used for 1st line treatment
                                                                             // B2_A:  retreating those infected with G_A with B after 1st line treatment failure
                                                                             // M2_A:  retreating those infected with G_A with M after 1st line treatment failure
                                                                             // M2_B_AB: retreating those infected with G_B or G_AB with M after 1st line treatment failure
        protected enum DummyParam { D_0, D_1, D_Minus1, D_Inf, T_Prev, T_DeltaPrev } // 0, 1, 2, 3, 4, 5

        protected enum Features { Time, PercResist, ChangeInPercResist, IfEverUsed }
        protected enum Conditions { AOut, BOut, ABOut, AOk, BOk, ABOk, BNeverUsed, MNeverUsed, AOn, AOff, BOn, BOff, MOn, MOff };
        protected List<int> _featureIDs = new List<int>(new int[Enum.GetValues(typeof(Features)).Length]);
        protected List<string> _infProfiles = new List<string>();
        protected GonoSpecialStatInfo _specialStatInfo = new GonoSpecialStatInfo();

        public GonoModel()
        {
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    _infProfiles.Add(s.ToString() + " | " + r.ToString());
                }
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

    }
    
}
