
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Reflection.PortableExecutable;
using System.Text.Json.Serialization;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.Rx;
using PBFT.Certificates;
using PBFT.Messages;

namespace PBFT.Helper
{
    public class SourceHandler : IPersistable
    {
        public Source<Request> RequestSubject { get; set; }
        public Source<PhaseMessage> ProtocolSubject { get; set; }
        public Source<ViewChange> ViewChangeSubject { get; set; }
        public Source<ViewChangeCertificate> ShutdownSubject { get; set; }
        public Source<NewView> NewViewSubject { get; set; }
        
        public Source<CheckpointCertificate> CheckpointSubject { get; set; }

        [JsonConstructor]
        public SourceHandler(Source<Request> reqbr, Source<PhaseMessage> protbr, Source<ViewChange> viewbr, Source<ViewChangeCertificate> shbr, Source<NewView> nvbr, Source<CheckpointCertificate> cpbr)
        {
            RequestSubject = reqbr;
            ProtocolSubject = protbr;
            ViewChangeSubject = viewbr;
            ShutdownSubject = shbr;
            NewViewSubject = nvbr;
            CheckpointSubject = cpbr;
        }

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(RequestSubject), RequestSubject);
            stateToSerialize.Set(nameof(ProtocolSubject), ProtocolSubject);
            stateToSerialize.Set(nameof(ViewChangeSubject), ViewChangeSubject);
            stateToSerialize.Set(nameof(ShutdownSubject), ShutdownSubject);
            stateToSerialize.Set(nameof(NewViewSubject), NewViewSubject);
            stateToSerialize.Set(nameof(CheckpointSubject), CheckpointSubject);
        }

        private static SourceHandler Deserialize(IReadOnlyDictionary<string, object> sd)
            => new SourceHandler(
                    sd.Get<Source<Request>>(nameof(RequestSubject)),
                    sd.Get<Source<PhaseMessage>>(nameof(ProtocolSubject)),
                    sd.Get<Source<ViewChange>>(nameof(ViewChangeSubject)),
                    sd.Get<Source<ViewChangeCertificate>>(nameof(ShutdownSubject)),
                    sd.Get<Source<NewView>>(nameof(NewViewSubject)),
                    sd.Get<Source<CheckpointCertificate>>(nameof(CheckpointSubject))
                );

    }
}