using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Newtonsoft.Json;
using PBFT.Certificates;
using PBFT.Messages;

namespace PBFT.Replica
{
    public class ViewChangeListener : IPersistable
    {
        private int NewViewNr;
        private int FailureNr;
        private ViewPrimary ServerViewInfo;
        private Source<ViewChange> ViewBridge;
        private bool Shutdown;
        
        [JsonConstructor]
        public ViewChangeListener(
            int viewnr, 
            int failnr,
            ViewPrimary vp, 
            Source<ViewChange> viewbridge,
            bool shutdown
        )
        {
            NewViewNr = viewnr;
            FailureNr = failnr;
            ServerViewInfo = vp;
            ViewBridge = viewbridge;
            Shutdown = shutdown;
        }
        
        public async CTask Listen(ViewChangeCertificate vcc, Dictionary<int, RSAParameters> keys, Action finCallback, Action shutdownCallback)
        {
            Console.WriteLine("ViewChange Listener: " + NewViewNr);
            if (Shutdown && shutdownCallback != null)
            {
                Console.WriteLine("With shutdown");
                await ViewBridge
                    .Where(vc => vc.NextViewNr == NewViewNr)
                    .Where(vc =>
                    {
                        Console.WriteLine("ViewChange VALIDATING MESSAGE");
                        return vc.Validate(keys[vc.ServID], ServerViewInfo.ViewNr);
                    })
                    .Scan(vcc.ProofList, (prooflist, message) =>
                    {
                        prooflist.Add(message);
                        return prooflist;
                    })
                    .Where(_ =>
                    {
                        Console.WriteLine("ShutdownReached");
                        return vcc.ShutdownReached(FailureNr);
                    })
                    .Next();
                Console.WriteLine("Calling shutdown");
                shutdownCallback();
                Console.WriteLine("Moving on");
            }
            await ViewBridge
                .Where(vc => vc.NextViewNr == NewViewNr)
                .Where(vc =>
                {
                    Console.WriteLine("ViewChange VALIDATING MESSAGE");
                    return vc.Validate(keys[vc.ServID], ServerViewInfo.ViewNr);
                })
                .Scan(vcc.ProofList, (prooflist, message) =>
                {
                    prooflist.Add(message);
                    return prooflist;
                })
                .Where(_ => vcc.ValidateCertificate(FailureNr))
                .Next();
            Console.WriteLine("Finished Listen view changes");
            finCallback();
        }
        
        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(NewViewNr), NewViewNr);
            stateToSerialize.Set(nameof(FailureNr), FailureNr);
            stateToSerialize.Set(nameof(ServerViewInfo), ServerViewInfo);
            stateToSerialize.Set(nameof(ViewBridge), ViewBridge);
            stateToSerialize.Set(nameof(Shutdown), Shutdown);
        }

        private static ViewChangeListener Deserialize(IReadOnlyDictionary<string, object> sd)
            => new ViewChangeListener(
                sd.Get<int>(nameof(NewViewNr)),
                sd.Get<int>(nameof(FailureNr)),
                sd.Get<ViewPrimary>(nameof(ServerViewInfo)),
                sd.Get<Source<ViewChange>>(nameof(ViewBridge)),
                sd.Get<bool>(nameof(Shutdown))
            );

    }
}