using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Newtonsoft.Json;
using PBFT.Certificates;
using PBFT.Helper;
using PBFT.Messages;

namespace PBFT.Replica
{
    public class ViewPrimary : IPersistable
    {
        public int ServID { get; set; }
        public int ViewNr { get; set; }

        public int NrOfNodes { get; set; }
        
        public ViewPrimary(int numberofReplicas)
        {
            NrOfNodes = numberofReplicas;
            ViewNr = 0;
            ServID = ViewNr % numberofReplicas;
        }
        
        [JsonConstructor]
        public ViewPrimary(int id, int vnr, int numberReplicas)
        {
            ServID = id;
            ViewNr = vnr;
            NrOfNodes = numberReplicas;
        }
        
        public void NextPrimary()
        {
            Console.WriteLine("Next Primary Called");
            ViewNr++;
            ServID = ViewNr % NrOfNodes;
        }
        
        public void UpdateView(int viewnr)
        {
            ViewNr = viewnr;
            ServID = ViewNr % NrOfNodes;
        }
        
        public CList<PhaseMessage> MakePrepareMessages(CDictionary<int, ProtocolCertificate> protcerts, int lowbound, int highbound)
        {
            CList<PhaseMessage> premessages = new CList<PhaseMessage>();
            for (int i = lowbound; i <= highbound; i++)
            {
                PhaseMessage newpre;
                if (protcerts.ContainsKey(i))
                    newpre = new PhaseMessage(
                        ServID, 
                        i, 
                        ViewNr, 
                        protcerts[i].CurReqDigest,
                        PMessageType.PrePrepare
                    );
                else newpre = new PhaseMessage(ServID, i, ViewNr, null, PMessageType.PrePrepare);
                premessages.Add(newpre);
            }
            return premessages;
        }
        
        public CList<PhaseMessage> MakePrepareMessagesver2(ViewChangeCertificate vcc, int lowbound, int highbound)
        {
            CList<PhaseMessage> premessages = new CList<PhaseMessage>();
            for (int i = lowbound; i <= highbound; i++)
            {
                bool foundproof = false;
                int proofidx = -1;
                PhaseMessage newpre;
                for (int j = 0; j<vcc.ProofList.Count; j++)
                {
                    if (vcc.ProofList[j].RemPreProofs.ContainsKey(i))
                    {
                        foundproof = true;
                        proofidx = j;
                        break;
                    }
                }
                if (foundproof)
                    newpre = new PhaseMessage(
                        ServID, 
                        i, 
                        ViewNr, 
                        vcc.ProofList[proofidx].RemPreProofs[i].CurReqDigest,
                        PMessageType.PrePrepare
                    );
                else newpre = new PhaseMessage(ServID, i, ViewNr, null, PMessageType.PrePrepare);
                premessages.Add(newpre);
            }
            return premessages;
        }

        public override string ToString() => $"Primary ServID: {ServID}, ViewNr: {ViewNr}, NrOfNodes: {NrOfNodes}";

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