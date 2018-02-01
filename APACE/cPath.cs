using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomNumberGeneratorLib;
using SimulationLib;

namespace APACE_lib
{
    // Path
    public class Path
    {
        // Fields
        string _name;        
        int _destinationClassID;  

        // Instantionation
        public Path(string name, int destinationClassID)
        {
            _name = name;            
            _destinationClassID = destinationClassID;
        }

        // Properties        
        public int DestinationClassID
        {
            get {return _destinationClassID;}
        }       
                
    } // End of class Path


}
