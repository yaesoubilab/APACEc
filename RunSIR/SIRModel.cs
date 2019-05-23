using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APACElib;

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
    }
}
