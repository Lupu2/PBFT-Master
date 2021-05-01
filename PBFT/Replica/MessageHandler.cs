using System;
using Cleipnir.ExecutionEngine;
using Cleipnir.Rx;
using PBFT.Certificates;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Replica.Network;
using PBFT.Replica.Protocol;

namespace PBFT.Replica
{
    public static class MessageHandler
    {
        public static void HandleSessionMessage(Session sesmes, TempInteractiveConn conn, Server serv)
        {
            int id = sesmes.DevID;
            DeviceType devtype = sesmes.Devtype;
            Console.WriteLine("Handle session message");
            if (devtype == DeviceType.Client)
            {
                if (!serv.ClientConnInfo.ContainsKey(id)) //New Client Connections
                {
                    Console.WriteLine("Adding client");
                    serv.ClientActive[id] = false;
                    serv.ClientConnInfo[id] = conn;
                    serv.AddPubKeyClientRegister(id, sesmes.Publickey);
                }
                else
                {
                    if (!serv.ClientPubKeyRegister[id].Equals(sesmes.Publickey)) // Updated Client Connection
                    {
                        serv.ClientConnInfo[id].Dispose();
                        serv.ClientConnInfo[id] = conn;
                        serv.AddPubKeyClientRegister(id, sesmes.Publickey);
                    }
                }
            }
            else
            {
                if (!serv.ServConnInfo.ContainsKey(id)) //New Server Connections
                {
                    Console.WriteLine("Adding server");
                    serv.ServConnInfo[id] = conn;
                    serv.AddPubKeyServerRegister(id, sesmes.Publickey);
                }
                else
                {
                    if (!serv.ServPubKeyRegister[id].Equals(sesmes.Publickey)) // Updated Server Connection
                    {
                        serv.ServConnInfo[id] = conn;
                        serv.AddPubKeyServerRegister(id, sesmes.Publickey);
                    }
                }
            }
        }

        public static void HandlePhaseMessage(
            PhaseMessage pesmes, 
            int curView, 
            bool protocolActive, 
            Engine scheduler, 
            Source<PhaseMessage> protocolSource, 
            Source<PhaseMessage> redistSource)
        {
            Console.WriteLine("Handling PhaseMessage");
            if (pesmes.ViewNr == curView && protocolActive)
            {
                Console.WriteLine("Emitting Protocol PhaseMessage"); //protocol, emit
                scheduler.Schedule(() => { protocolSource.Emit(pesmes); });
            }
            else if (pesmes.ViewNr == curView && !protocolActive)
            {
                Console.WriteLine("Emitting Redistribute PhaseMessage");
                scheduler.Schedule(() => { redistSource.Emit(pesmes); });
            }
        }

        public static void HandleViewChange(ViewChange vc, Server serv, Engine scheduler)
        {
            Console.WriteLine("Handling view-change message");
            bool val = vc.Validate(serv.ServPubKeyRegister[vc.ServID], vc.NextViewNr);
            Console.WriteLine("ViewChange validation result: " + val);
            if (val && serv.ViewMessageRegister.ContainsKey(vc.NextViewNr))
                //will already have a view-change message for view n, therefore count = 2
            {
                Console.WriteLine("ViewChange cert already registered, adding to it");
                Console.WriteLine("Count:" +
                                  serv.ViewMessageRegister[vc.NextViewNr].ProofList.Count);
                if (!serv.ViewMessageRegister[vc.NextViewNr].IsValid())
                {
                    scheduler.Schedule(() =>
                    {
                        Console.WriteLine("Scheduling adding view-change");
                        if (!serv.ViewMessageRegister[vc.NextViewNr].IsValid())
                        {
                            serv.ViewMessageRegister[vc.NextViewNr].AppendViewChange(
                                vc,
                                serv.ServPubKeyRegister[vc.ServID],
                                Quorum.CalculateFailureLimit(serv.TotalReplicas)
                            );
                        }
                    });
                }
            }
            else if (val && !serv.ViewMessageRegister.ContainsKey(vc.NextViewNr)
            ) //does not already have any view-change messages for view n
            {
                Console.WriteLine("Creating a new view-change certificate");
                scheduler.Schedule(() =>
                {
                    Console.WriteLine("Scheduling creating viewcert and adding view-change");
                    serv.ViewMessageRegister[vc.NextViewNr] = new ViewChangeCertificate(
                        new ViewPrimary(vc.ServID, vc.NextViewNr, serv.TotalReplicas),
                        vc.CertProof,
                        serv.EmitShutdown,
                        serv.EmitViewChange
                    );
                    serv.ViewMessageRegister[vc.NextViewNr].AppendViewChange(
                        vc,
                        serv.ServPubKeyRegister[vc.ServID],
                        Quorum.CalculateFailureLimit(serv.TotalReplicas)
                    );
                });
            }
            else
                Console.WriteLine("Things did not go as planned :(");
            
        }
        
        public static void HandleViewChange2(ViewChange vc, Server serv, Engine scheduler)
        {
            Console.WriteLine("Handling view-change message");
            bool val = vc.Validate(serv.ServPubKeyRegister[vc.ServID], vc.NextViewNr);
            Console.WriteLine("ViewChange validation result: " + val);
            if (val && serv.ViewMessageRegister.ContainsKey(vc.NextViewNr)
            ) //will already have a view-change message for view n, therefore count = 2
            {
                Console.WriteLine("ViewChange cert already registered, adding to it");
                Console.WriteLine("Count:" + serv.ViewMessageRegister[vc.NextViewNr].ProofList.Count);
                scheduler.Schedule(() =>
                {
                    Console.WriteLine("Scheduling adding view-change");
                    if (!serv.ViewMessageRegister[vc.NextViewNr].IsValid()) serv.Subjects.ViewChangeSubject.Emit(vc);
                });
            }
            else if (val && !serv.ViewMessageRegister.ContainsKey(vc.NextViewNr)
            ) //does not already have any view-change messages for view n
            {
                Console.WriteLine("Creating a new-view certificate");
                scheduler.Schedule(() =>
                {
                    Console.WriteLine("Scheduling creating viewcert and adding view-change");
                    var newvp = new ViewPrimary(vc.ServID, vc.NextViewNr, serv.TotalReplicas);
                    var viewcert = new ViewChangeCertificate(
                        newvp,
                        vc.CertProof,
                        null,
                        null
                    );
                    serv.ViewMessageRegister[vc.NextViewNr] = viewcert;
                    var viewlistener = new ViewChangeListener(
                        vc.NextViewNr,
                        Quorum.CalculateFailureLimit(serv.TotalReplicas),
                        newvp,
                        serv.Subjects.ViewChangeSubject,
                        true);
                    _ = viewlistener.Listen(viewcert, serv.ServPubKeyRegister, serv.EmitViewChange, serv.EmitShutdown);
                    serv.Subjects.ViewChangeSubject.Emit(vc);
                });
            }
            else
                Console.WriteLine("Things did not go as planned :(");
        }

        public static void HandleCheckpoint(Checkpoint check, Server serv)
        {
            Console.WriteLine("Handling checkpoint message");
            if (serv.CheckpointLog.ContainsKey(check.StableSeqNr))
            {
                Console.WriteLine("Found existing certificate");
                Console.WriteLine("StableCert: " + serv.StableCheckpointsCertificate);
                foreach (var (key, _) in serv.CheckpointLog) Console.WriteLine("Key: " + key);
                    if (serv.StableCheckpointsCertificate == null ||
                        serv.StableCheckpointsCertificate.LastSeqNr != check.StableSeqNr)
                        serv.CheckpointLog[check.StableSeqNr].AppendProof(
                            check,
                            serv.ServPubKeyRegister[check.ServID],
                            Quorum.CalculateFailureLimit(serv.TotalReplicas)
                        );
            }
            else if (!serv.CheckpointLog.ContainsKey(check.StableSeqNr))
            {
                Console.WriteLine("Could not find existing certificate");
                Console.WriteLine("Adding new Checkpoint to checkpoint log");
                if (serv.StableCheckpointsCertificate == null ||
                    serv.StableCheckpointsCertificate.LastSeqNr != check.StableSeqNr)
                {
                    CheckpointCertificate cert = new CheckpointCertificate
                    (
                        check.StableSeqNr,
                        check.StateDigest,
                        serv.EmitCheckpoint
                    );
                    cert.AppendProof
                    (
                        check,
                        serv.ServPubKeyRegister[check.ServID],
                        Quorum.CalculateFailureLimit(serv.TotalReplicas)
                    );
                    serv.CheckpointLog[check.StableSeqNr] = cert;
                }
            }
        }
        
        public static void HandleCheckpoint2(Checkpoint check, Server serv)
        {
            Console.WriteLine("Handling checkpoint message");
            Console.WriteLine("Scheduling checkpoint in server");
            if (serv.CheckpointLog.ContainsKey(check.StableSeqNr))
            {
                Console.WriteLine("Found existing certificate");
                Console.WriteLine("StableCert: " + serv.StableCheckpointsCertificate);
                if (serv.StableCheckpointsCertificate == null || 
                    serv.StableCheckpointsCertificate.LastSeqNr != check.StableSeqNr
                    )
                    Console.WriteLine("EMITTING Found EXISTING CERTIFICATE IN SERVER");
                    serv.Subjects.CheckpointSubject.Emit(check);
            }
            else if(!serv.CheckpointLog.ContainsKey(check.StableSeqNr))
            {
                Console.WriteLine("Could not find existing certificate");
                Console.WriteLine("Adding new Checkpoint to checkpoint log");
                if (serv.StableCheckpointsCertificate == null ||
                    serv.StableCheckpointsCertificate.LastSeqNr < check.StableSeqNr)
                {
                    CheckpointCertificate cert = new CheckpointCertificate(
                        check.StableSeqNr,
                        check.StateDigest, 
                        null
                    );
                    serv.CheckpointLog[check.StableSeqNr] = cert;
                    var checklistener = new CheckpointListener(
                    check.StableSeqNr,
                    Quorum.CalculateFailureLimit(serv.TotalReplicas), 
                         check.StateDigest, 
                         serv.Subjects.CheckpointSubject
                    );
                    _= checklistener.Listen(cert, serv.ServPubKeyRegister, serv.EmitCheckpoint);
                    Console.WriteLine("EMITTING NOT FIND EXISTING CERTIFICATE IN SERVER");
                    serv.Subjects.CheckpointSubject.Emit(check);
                }
            }
        }
    }
}