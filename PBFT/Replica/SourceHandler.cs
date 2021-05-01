using System.Collections.Generic;
using System.Text.Json.Serialization;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.Rx;
using PBFT.Certificates;
using PBFT.Messages;

namespace PBFT.Replica
{
    public class SourceHandler : IPersistable
    {
        public Source<Request> RequestSubject { get; set; }
        public Source<PhaseMessage> ProtocolSubject { get; set; }
        public Source<PhaseMessage> RedistSubject { get; set; }
        public Source<ViewChange> ViewChangeSubject { get; set; }
        public Source<bool> ViewChangeFinSubject { get; set; }
        public Source<bool> ShutdownSubject { get; set; }
        public Source<NewView> NewViewSubject { get; set; }
        public Source<Checkpoint> CheckpointSubject { get; set; }
        public Source<CheckpointCertificate> CheckpointFinSubject { get; set; }

        [JsonConstructor]
        public SourceHandler(
            Source<Request> reqbr,
            Source<PhaseMessage> protbr,
            Source<ViewChange>viewbr,
            Source<bool> viewfinbr,
            Source<bool> shbr,
            Source<NewView> nvbr,
            Source<PhaseMessage> redist,
            Source<Checkpoint> cpbr,
            Source<CheckpointCertificate> cpfinbr)
        {
            RequestSubject = reqbr;
            ProtocolSubject = protbr;
            ViewChangeSubject = viewbr;
            ViewChangeFinSubject = viewfinbr;
            ShutdownSubject = shbr;
            NewViewSubject = nvbr;
            RedistSubject = redist;
            CheckpointSubject = cpbr;
            CheckpointFinSubject = cpfinbr;
        }

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(RequestSubject), RequestSubject);
            stateToSerialize.Set(nameof(ProtocolSubject), ProtocolSubject);
            stateToSerialize.Set(nameof(ViewChangeSubject), ViewChangeSubject);
            stateToSerialize.Set(nameof(ViewChangeFinSubject), ViewChangeFinSubject);
            stateToSerialize.Set(nameof(ShutdownSubject), ShutdownSubject);
            stateToSerialize.Set(nameof(NewViewSubject), NewViewSubject);
            stateToSerialize.Set(nameof(RedistSubject), RedistSubject);
            stateToSerialize.Set(nameof(CheckpointSubject), CheckpointSubject);
            stateToSerialize.Set(nameof(CheckpointFinSubject), CheckpointFinSubject);
        }

        private static SourceHandler Deserialize(IReadOnlyDictionary<string, object> sd)
            => new SourceHandler(
                    sd.Get<Source<Request>>(nameof(RequestSubject)),
                    sd.Get<Source<PhaseMessage>>(nameof(ProtocolSubject)),
                    sd.Get<Source<ViewChange>>(nameof(ViewChangeSubject)),
                    sd.Get<Source<bool>>(nameof(ViewChangeFinSubject)),
                    sd.Get<Source<bool>>(nameof(ShutdownSubject)),
                    sd.Get<Source<NewView>>(nameof(NewViewSubject)),
                    sd.Get<Source<PhaseMessage>>(nameof(RedistSubject)),
                    sd.Get<Source<Checkpoint>>(nameof(CheckpointSubject)),
                    sd.Get<Source<CheckpointCertificate>>(nameof(CheckpointFinSubject))
                );
    }
}