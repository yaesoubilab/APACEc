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
    // used for total statis over all districts 
    //enum GonoSpecialStatIDs { PopSize = 0, Prev, FirstTx, SuccessAOrB, SuccessAOrBOrM, PercFirstTxAndResist }
    //enum Features { Time = 0, PercResist, ChangeInPercResist }
    enum GonoSpecialStatIDs { PopSize = 0, Prev, Tx1, Tx1Sym, Tx1Resist, SuccessAOrB, SuccessAOrBOrM, PercFirstTxAndResist }
    enum Features { Time = 0, PercResist, ChangeInPercResist }
    enum Conditions { AOut = 0, BOut, ABOut, AOk, BOk, ABOk, BNeverUsed, M1NeverUsed, AOn, AOff, BOn, BOff, MOn, MOff };
    enum Interventions {A1=2, B1, M1, B2_A, M2_A, M2_B_AB }
    // A1:    A is used for 1st line treatment
    // B1:    B is used for 1st line treatment
    // M1:    M is used for 1st line treatment
    // B2_A:  retreating those infected with G_A with B after 1st line treatment failure
    // M2_A:  retreating those infected with G_A with M after 1st line treatment failure
    // M2_B_AB: retreating those infected with G_B or G_AB with M after 1st line treatment failure


    public class GonoInterventionInfo
    {
        public List<int> InterventionsIDs { get; set; }

        // TODO: needs to be tested
        public void Reset(int nRegions)
        {
            InterventionsIDs = new List<int>(new int[Enum.GetValues(typeof(Interventions)).Length + 2]);

            foreach (Interventions intv in Enum.GetValues(typeof(Interventions)))
            {
                // 2 is added for the Default and Always Off interventions 
                if (nRegions > 1)
                    InterventionsIDs[(int)intv] = nRegions * ((int)intv - 2) + 2;
                else
                    InterventionsIDs[(int)intv] = (int)intv;
            }
        }
    }

    public class GonoConditionsInfo
    {
        public List<int> ConditionIDs { get; set; }

        public void Reset(int nRegions)
        {
            ConditionIDs = new List<int>(new int[Enum.GetValues(typeof(Conditions)).Length]);

            foreach (Conditions c in Enum.GetValues(typeof(Conditions)))
            {
                if (nRegions > 1)
                    ConditionIDs[(int)c] = (nRegions + 1) * (int)c;
                else
                    ConditionIDs[(int)c] = (int)c;
            }
        }
    }

    public class GonoSpecialStatInfo
    {
        public List<int> SpecialStatIDs { get; set; }

        public List<string> FormulaPopSize { get; set; }
        public List<string> FormulaPrev { get; set; }
        public List<List<string>> FormulaPrevSym { get; set; } // Sym, Asym
        public List<List<string>> FormulaPrevResist { get; set; } // 0, A, B, AB
        public List<string> FormulaTreated { get; set; }
        public List<string> FormulaTreatedAndSym { get; set; } // Sym, Asym
        public List<List<string>> FormulaTreatedResist { get; set; } // 0, A, B, AB
        public List<string> FormulaTreatedA1B1B2 { get; set; } // A1, B1, B2
        public List<string> FormulaTreatedM1M2 { get; set; } // M1, M2

        public int[] IDPercOfPrevResist { get; set; } // 0, A, B, AB
        public int[] IDTxResist { get; set; } // 0, A, B, AB
        public int[] IDRatioTxResist { get; set; } // 0, A, B, AB 

        public void Reset(int nRegions)
        {
            SpecialStatIDs = new List<int>(new int[Enum.GetValues(typeof(GonoSpecialStatIDs)).Length]);
            IDPercOfPrevResist = new int[4]; // 4 for 0, A, B, AB
            IDTxResist = new int[4]; // 4 for 0, A, B, AB
            IDRatioTxResist = new int[4]; // 4 for 0, A, B, AB

            FormulaPopSize = new List<string>();
            FormulaPrev = new List<string>();
            FormulaPrevSym = new List<List<string>>();
            FormulaPrevResist = new List<List<string>>();
            FormulaTreated = new List<string>();
            FormulaTreatedAndSym = new List<string>();
            FormulaTreatedResist = new List<List<string>>();

            for (int i = 0; i < nRegions + 1; i++)
            {
                FormulaPopSize.Add("");
                FormulaPrev.Add("");
                FormulaPrevSym.Add(new List<string>() { "", "" }); // Sym, Asym
                FormulaPrevResist.Add(new List<string> { "", "", "", "" }); // 0, A, B, AB
                FormulaTreated.Add("");
                FormulaTreatedAndSym.Add("");
                FormulaTreatedResist.Add(new List<string> { "", "", "", "" }); // 0, A, B, AB
            }

            FormulaTreatedA1B1B2 = new List<string>() { "", "", "" };
            FormulaTreatedM1M2 = new List<string>() { "", "" };
        }
    }

    public class GonoFeatureInfo
    {
        public List<int> FeatureIDs { get; set; }
        public int[] IDPercResist { get; set; }
        public int[] IDChangeInPercResist { get; set; }
        public int IfAEverOff { get; set; }
        public int IfBEverOff { get; set; }
        public int IfMEverOn { get; set; }

        public void Reset(int nRegions)
        {
            FeatureIDs = new List<int>(new int[Enum.GetValues(typeof(Features)).Length]);
            IDPercResist = new int[4]; // 4 for 0, A, B, AB
            IDChangeInPercResist = new int[4]; // 4 for 0, A, B, AB
        }
    }

    public class MSMGonoModel : GonoModel
    {       
        public MSMGonoModel() : base()
        {           
        }

        public override void BuildModel()
        {
            List<string> regions = new List<string>() { "MSM" };
            base.BuildGonoModel(regions, new List<double[]>());
        }
    }

    public class SpatialGonoModel : GonoModel
    {
        public SpatialGonoModel() : base()
        {
        }

        public override void BuildModel()
        {
            List<string> sites = new List<string>() {
                "Atlanta", "Boston", "Chicago", "Dallas", "Houston",
                "Los Angeles", "Miami", "Minneapolis", "New York", "Philadelphia",
                "Phoenix", "Riverside", "San Diego", "San Francisco", "Seattle", "Washington" };

            double MIN = 0.001, MAX = 0.25;

            List<double[]> rateBounds = new List<double[]> {
                new double[2] { MIN, MAX }, // 
                new double[2] { MIN, MAX }, //
                new double[2] { MIN, MAX }, //
                new double[2] { MIN, MAX }, //
                new double[2] { MIN, MAX }, //
                new double[2] { MIN, MAX }, //
                new double[2] { MIN, MAX }, //
                new double[2] { MIN, MAX }, //
                new double[2] { MIN, MAX }, //
                new double[2] { MIN, MAX }, //
                new double[2] { MIN, MAX }, //
                new double[2] { MIN, MAX }, //
                new double[2] { MIN, MAX }, //
                new double[2] { MIN, MAX }, //
                new double[2] { MIN, MAX }, //
                new double[2] { MIN, MAX }  // 
            };

            base.BuildGonoModel(sites, rateBounds);
        }
    }
}
