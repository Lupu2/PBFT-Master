using System.Collections.Generic;
using System.Text.Json.Serialization;
using Cleipnir.ObjectDB.PersistentDataStructures;
using PBFT.Certificates;
using PBFT.Messages;

namespace PBFT.Helper.JsonObjects
{
    public class JsonCheckpointCertificate
    {
        public int LastSeqNr { get; set; }
        public byte[] StateDigest { get; set; }
        public bool Stable { get; set; }
        public List<Checkpoint> ProofList { get; set; }

        [JsonConstructor]
        public JsonCheckpointCertificate(int seq, byte[] dig, bool stable, List<Checkpoint> proofs)
        {
            LastSeqNr = seq;
            StateDigest = dig;
            Stable = stable;
            ProofList = proofs;
        }

        public static JsonCheckpointCertificate ConvertToJsonCheckpointCertificate(CheckpointCertificate checkcert)
        {
            var checklist = new List<Checkpoint>();
            if (checkcert.ProofList != null)
                foreach (var checkpoint in checkcert.ProofList)
                    checklist.Add(checkpoint);
            else checklist = null;
            var jsoncheckcert = new JsonCheckpointCertificate(checkcert.LastSeqNr, checkcert.StateDigest,
                checkcert.Stable, checklist);
            return jsoncheckcert;
        }

        public CheckpointCertificate ConvertToCheckpointCertificate()
        {
            var clist = new CList<Checkpoint>();
            foreach (var checkpoint in ProofList) clist.Add(checkpoint);
            var checkcert = new CheckpointCertificate(LastSeqNr, StateDigest, Stable,null, clist);
            return checkcert;
        }
    }
}