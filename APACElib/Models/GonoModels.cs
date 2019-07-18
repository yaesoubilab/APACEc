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
    enum GonoSpecialStatIDs { PopSize = 0, Prev, FirstTx, SuccessAOrB, SuccessAOrBOrM, PercFirstTxAndResist }
    enum Features { Time = 0, PercResist, ChangeInPercResist }

    public class GonoSpecialStatInfo
    {
        public List<int> SpecialStatIDs { get; set; }
        public List<string> Prev { get; set; }
        public List<List<string>> PrevSym { get; set; } // Sym, Asym
        public List<List<string>> PrevResist { get; set; } // 0, A, B, AB

        public List<string> Treated { get; set; }
        public List<string> TreatedAndSym { get; set; } // Sym, Asym
        public List<List<string>> TreatedResist { get; set; } // 0, A, B, AB

        public List<string> TreatedA1B1B2 { get; set; } // A1, B1, B2
        public List<string> TreatedM1M2 { get; set; } // M1, M2

        public List<int> SumTxResistFirstRegion { get; set; }
        public List<int> RatioTxResistFirstRegion { get; set; }

        public void Reset(int nRegions)
        {
            SpecialStatIDs = new List<int>(new int[Enum.GetValues(typeof(GonoSpecialStatIDs)).Length]);
            Prev = new List<string>();
            PrevSym = new List<List<string>>();
            PrevResist = new List<List<string>>();
            Treated = new List<string>();
            TreatedAndSym = new List<string>();
            TreatedResist = new List<List<string>>();

            SumTxResistFirstRegion = new List<int>(new int[4]); // 4 for 0, A, B, AB
            RatioTxResistFirstRegion = new List<int>(new int[4]); // 4 for 0, A, B, AB

            for (int i = 0; i < nRegions; i++)
            {
                Prev.Add("");
                PrevSym.Add(new List<string>() { "", "" }); // Sym, Asym
                PrevResist.Add(new List<string> { "", "", "", "" }); // 0, A, B, AB
                Treated.Add("");
                TreatedAndSym.Add("");
                TreatedResist.Add(new List<string> { "", "", "", "" }); // 0, A, B, AB
            }

            TreatedA1B1B2 = new List<string>() { "", "", "" };
            TreatedM1M2 = new List<string>() { "", "" };
        }
    }

    public class GonoFeatureInfo
    {
        public List<int> FeatureIDs { get; set; }
        public int[] PercResistFirstRegion { get; set; }
        public int[] PChangeInPercResistFirstRegion { get; set; }
        public int IfAEverOff { get; set; }
        public int IfBEverOff { get; set; }
        public int IfMEverOn { get; set; }

        public void Reset(int nRegions)
        {
            FeatureIDs = new List<int>(new int[Enum.GetValues(typeof(Features)).Length]);
            PercResistFirstRegion = new int[4]; // 4 for 0, A, B, AB
            PChangeInPercResistFirstRegion = new int[4]; // 4 for 0, A, B, AB
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
            base.BuildGonoModel(regions);
        }
    }

    public class SpatialGonoModel : GonoModel
    {
        enum Sites { Site1, Site2 };       

        public SpatialGonoModel() : base()
        {
        }

        public override void BuildModel()
        {
            List<string> sites = new List<string>() { "Site 1", "Site 2" };
            base.BuildGonoModel(sites);
        }
    }
}
