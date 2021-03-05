using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace PBFT.Replica
{
    public class ViewPrimary : IPersistable
    {
        public int ServID {get; set;}
        public int ViewNr {get; set;}

        private int NrOfNodes {get; set;}
        
        public ViewPrimary(int numberofReplicas)
        {
            NrOfNodes = numberofReplicas;
            ViewNr = 0;
            ServID = ViewNr % numberofReplicas;
        }
        
        public ViewPrimary(int id, int vnr, int numberReplicas)
        {
            ServID = id;
            ViewNr = vnr;
            NrOfNodes = numberReplicas;
        }
        
        public void NextPrimary()
        {
            ViewNr++;
            ServID = ViewNr % NrOfNodes;
        }

        public void UpdateView(int viewnr)
        {
            ViewNr = viewnr;
            ServID = ViewNr % NrOfNodes;
        }
        
        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(ServID), ServID);
            stateToSerialize.Set(nameof(ViewNr), ViewNr);
            stateToSerialize.Set(nameof(NrOfNodes), NrOfNodes);
        }

        private static ViewPrimary Deserialize(IReadOnlyDictionary<string, object> sd)
            => new ViewPrimary(
                sd.Get<int>(nameof(ServID)), 
                sd.Get<int>(nameof(ViewNr)), 
                sd.Get<int>(nameof(NrOfNodes))
                );
    }
}