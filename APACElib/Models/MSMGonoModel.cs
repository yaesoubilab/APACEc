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

        

       

        

        
    }
}
