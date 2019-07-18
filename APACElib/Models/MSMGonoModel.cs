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
            _featureInfo.Reset(1);

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
            AddGonoConditions(regions);
            // add connections
            AddGonoConnections(regions);
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

       

        

        
    }
}
