using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using PBFT.Helper;

namespace PBFT.Replica
{
    public class ViewPrimary : IPersistable
    {
        public int ServID {get; set;}
        public int ViewNr {get; set;}

        public ViewPrimary(int id, int vnr)
        {
            ServID = id;
            ViewNr = vnr;
        }

        public void NextPrimary(int numberOfReplicas)
        {
            ViewNr++;
            ServID = ViewNr % numberOfReplicas;
        }

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(ServID), ServID);
            stateToSerialize.Set(nameof(ViewNr), ViewNr);
        }

        private static ViewPrimary Deserialize(IReadOnlyDictionary<string, object> sd)
            => new ViewPrimary(sd.Get<int>(nameof(ServID)), sd.Get<int>(nameof(ViewNr)));
    }
}