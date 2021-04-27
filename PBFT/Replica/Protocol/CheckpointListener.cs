using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using PBFT.Certificates;
using PBFT.Helper;
using PBFT.Messages;

namespace PBFT.Replica.Protocol
{
    public class CheckpointListener : IPersistable
    {
        private int StableSeqNr { get; set; }
        private int FailureNr { get; set; }
        private byte[] StateDigest { get; set; }
        private Source<Checkpoint> CheckpointBridge;

        [JsonConstructor]
        public CheckpointListener(int seqnr, int failnr, byte[] dig, Source<Checkpoint> checkbridge)
        {
            StableSeqNr = seqnr;
            FailureNr = failnr;
            StateDigest = dig;
            CheckpointBridge = checkbridge;
        }

        public async CTask Listen(CheckpointCertificate cpc, Dictionary<int, RSAParameters> keys, Action<CheckpointCertificate> finCallback)
        {
            Console.WriteLine("Checkpoint Listener: " + StableSeqNr);
            await CheckpointBridge
                .Where(check => check.StableSeqNr == StableSeqNr)
                .Where(check =>
                {
                    Console.WriteLine("Checkpoint VALIDATING MESSAGE");
                    return check.Validate(keys[check.ServID]);
                })
                .Scan(cpc.ProofList, (prooflist, message) =>
                {
                    prooflist.Add(message);
                    return prooflist;
                })
                .Where(_ => cpc.ValidateCertificate(FailureNr))
                .Next();
            Console.WriteLine("Finished Listener!");
            finCallback(cpc);
        }
        
        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(StableSeqNr), StableSeqNr);
            stateToSerialize.Set(nameof(FailureNr), FailureNr);
            stateToSerialize.Set(nameof(StateDigest), Serializer.SerializeHash(StateDigest));
            stateToSerialize.Set(nameof(CheckpointBridge), CheckpointBridge);
        }

        private static CheckpointListener Deserialize(IReadOnlyDictionary<string, object> sd)
            => new CheckpointListener(
                sd.Get<int>(nameof(StableSeqNr)),
                sd.Get<int>(nameof(FailureNr)),
                Deserializer.DeserializeHash(sd.Get<string>(nameof(StateDigest))),
                sd.Get<Source<Checkpoint>>(nameof(CheckpointBridge))
                );
    }
}