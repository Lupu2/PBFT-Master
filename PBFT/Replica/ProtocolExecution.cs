using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Certificates;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;

namespace PBFT.Replica
{
    public class ProtocolExecution : IPersistable
    {
        public Server Serv { get; set;}
        public int FailureNr { get; set;}
        public bool Active { get; set; }
        private Source<PhaseMessage> MesBridge;
        private Source<PhaseMessage> ReMesBridge;
        private Source<bool> ViewChangeBridge;
        public Source<PhaseMessage> ShutdownBridgePhase;
        private Source<bool> ShutdownBridge;
        private Source<NewView> NewViewBridge;

        public ProtocolExecution(Server server, int fnodes, Source<PhaseMessage> mesbridge, Source<PhaseMessage> remesbridge, Source<PhaseMessage> shutdownphase, Source<bool> viewchangebridge, Source<NewView> newviewbridge, Source<bool> shutbridge) 
        {
            Serv = server;
            FailureNr = fnodes;
            Active = true;
            MesBridge = mesbridge;
            ReMesBridge = remesbridge;
            ViewChangeBridge = viewchangebridge;
            NewViewBridge = newviewbridge;
            ShutdownBridge = shutbridge;
            ShutdownBridgePhase = shutdownphase;
        }

        public async CTask<Reply> HandleRequest(Request clireq, int leaderseq, CancellationTokenSource cancel)
        {
            Console.WriteLine("HandleRequest");
            //await Sync.Next();
            try
            {
                byte[] digest;
                ProtocolCertificate qcertpre;
                digest = Crypto.CreateDigest(clireq);
                int curSeq; //change later

                if (Serv.IsPrimary()) //Primary
                {
                    curSeq = leaderseq;
                    Serv.InitializeLog(curSeq);
                    PhaseMessage preprepare = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.PrePrepare);
                    Serv.SignMessage(preprepare, MessageType.PhaseMessage);
                    qcertpre = new ProtocolCertificate(preprepare.SeqNr, preprepare.ViewNr, digest, CertType.Prepared, preprepare); //Log preprepare as Prepare
                    await Sleep.Until(1000);
                    Serv.Multicast(preprepare.SerializeToBuffer(), MessageType.PhaseMessage); //Send async message PrePrepare
                }else{ //Replicas
                    var preprepared = await MesBridge
                        .Where(pm => pm.PhaseType == PMessageType.PrePrepare)
                        .Where(pm =>
                        {
                            Console.WriteLine("PRE-Prepare MESSAGEBRIDGE VALIDATING MESSAGE");
                            return pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView, Serv.CurSeqRange);
                        })
                        .Merge(ShutdownBridgePhase)
                        .Next();
                    //Add functionality for if you get another prepare message with same view but different seq nr, while you are already working on another,then you know that the primary is faulty.
                    if (preprepared.ServID == -1 && preprepared.PhaseType == PMessageType.End) 
                        throw new TimeoutException("Timeout Occurred! System is no longer active!");
                    Console.WriteLine("GOT PRE-PREPARE");
                    qcertpre = new ProtocolCertificate(preprepared.SeqNr, Serv.CurView, digest, CertType.Prepared, preprepared); //note Serv.CurView == prepared.ViewNr which is checked in t.Validate //Add Prepare to Certificate
                    curSeq = qcertpre.SeqNr; 
                    Serv.InitializeLog(curSeq);
                    PhaseMessage prepare = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.Prepare); //Send async message Prepare
                    Serv.SignMessage(prepare, MessageType.PhaseMessage);
                    qcertpre.ProofList.Add(prepare); //add its own, really should be validated, but not sure how.
                    //Serv.EmitPhaseMessageLocally(prepare);
                    Serv.Multicast(prepare.SerializeToBuffer(), MessageType.PhaseMessage);
                }

                if (Active)
                {
                    Console.WriteLine("Active");
                    cancel.Cancel();
                }
                else
                {
                    Console.WriteLine("Not Active");
                    throw new TimeoutException("Timeout Occurred! System is no longer active!");
                }
                //Prepare phase
                ProtocolCertificate qcertcom = new ProtocolCertificate(qcertpre.SeqNr, Serv.CurView, digest, CertType.Committed);   
                var prepared = MesBridge
                    .Where(pm => pm.PhaseType == PMessageType.Prepare)
                    .Where(pm => pm.SeqNr == qcertpre.SeqNr)
                    .Where(pm =>
                    {
                        Console.WriteLine("MESSAGEBRIDGE VALIDATING MESSAGE");
                        return pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView, Serv.CurSeqRange,
                                qcertpre);
                    })
                    .Where(pm => pm.Digest.SequenceEqual(qcertpre.CurReqDigest))
                    .Scan(qcertpre.ProofList, (prooflist, message) =>
                    {
                        prooflist.Add(message);
                        return prooflist;
                    })
                    .Where(_ => qcertpre.ValidateCertificate(FailureNr))
                    .Next();
                
                var committed = MesBridge  //await incoming PhaseMessages Where = MessageType.Commit Until Consensus Reached
                    .Where(pm => pm.PhaseType == PMessageType.Commit)
                    .Where(pm => pm.SeqNr == qcertcom.SeqNr)
                    .Where(pm => pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView, Serv.CurSeqRange, qcertcom))
                    .Where(pm => pm.Digest.SequenceEqual(qcertcom.CurReqDigest))
                    .Scan(qcertcom.ProofList, (prooflist, message) =>
                    {
                        prooflist.Add(message);
                        return prooflist;
                    })
                    .Where(_ => qcertcom.ValidateCertificate(FailureNr))
                    .Where(_ => qcertpre.ValidateCertificate(FailureNr))
                    .Next();
                
                Console.WriteLine("Waiting for prepares");
                if (Active) await prepared;
                else throw new ConstraintException("System is no longer active!");
                
                //Commit phase
                Serv.AddProtocolCertificate(qcertpre.SeqNr, qcertpre); //add first certificate to Log
                PhaseMessage commitmes = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.Commit);
                Serv.SignMessage(commitmes, MessageType.PhaseMessage);
                Serv.Multicast(commitmes.SerializeToBuffer(), MessageType.PhaseMessage); //Send async message Commit
                Serv.EmitPhaseMessageLocally(commitmes);
                Console.WriteLine("Waiting for commits");
                if (Active) await committed;
                else throw new ConstraintException("System is no longer active!");
                
                //Reply
                Serv.AddProtocolCertificate(qcertcom.SeqNr, qcertcom);
                Console.WriteLine($"Completing operation: {clireq.Message}");
                
                var rep = new Reply(Serv.ServID, curSeq, Serv.CurView, true, clireq.Message,clireq.Timestamp);
                Serv.SignMessage(rep, MessageType.Reply);
                Serv.ReplyLog[curSeq] = rep;
                Serv.SendMessage(rep.SerializeToBuffer(), Serv.ClientConnInfo[clireq.ClientID].Socket, MessageType.Reply);
                return rep;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in ProtocolExecution!");
                Console.WriteLine(e);
                var rep = new Reply(Serv.ServID, -1, Serv.CurView, false, "Failure", clireq.Timestamp);
                Serv.SignMessage(rep, MessageType.Reply);
                Serv.SendMessage(rep.SerializeToBuffer(), Serv.ClientConnInfo[clireq.ClientID].Socket, MessageType.Reply);
                return rep;
            }
        }

        
        //Function that performs all the operations in HandleRequest but without sending anything. Used for testing the operations performed in the function..
        public async CTask<Reply> HandleRequestTest(Request clireq, int leaderseq, CancellationTokenSource cancel)
        {
            try
            {
                Console.WriteLine("HANDLEREQUESTTEST");
                byte[] digest;
                ProtocolCertificate qcertpre;
                digest = Crypto.CreateDigest(clireq);
                int curSeq; //change later

                if (Serv.IsPrimary()) //Primary
                {
                    curSeq = leaderseq;
                    //curSeq = ++Serv.CurSeqNr; //single threaded and asynchronous, only a single HandleRequest has access to this variable at the time.
                    Serv.InitializeLog(curSeq);
                    PhaseMessage preprepare = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.PrePrepare);
                    Serv.SignMessage(preprepare, MessageType.PhaseMessage);
                    qcertpre = new ProtocolCertificate(preprepare.SeqNr, preprepare.ViewNr, digest, CertType.Prepared, preprepare); //Log preprepare as Prepare
                    //await Sleep.Until(1000); //causes object not initialized execption for test fsr
                    Thread.Sleep(1000);
                }else{ //Replicas
                    var preprepared = await MesBridge.Merge(ShutdownBridgePhase)
                        .Where(pm => pm.PhaseType == PMessageType.PrePrepare)
                        .Where(pm => {
                                Console.WriteLine("PRE-Prepare MESSAGEBRIDGE VALIDATING MESSAGE");
                                return pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView, Serv.CurSeqRange);
                            })
                        .Merge(ShutdownBridgePhase)
                        .Next();
                    //Add functionality for if you get another prepare message with same view but different seq nr, while you are already working on another,then you know that the primary is faulty.
                    if (preprepared.ServID == -1 && preprepared.PhaseType == PMessageType.End) throw new TimeoutException("Timeout Occurred! System is no longer active!");
                    Console.WriteLine("GOT PRE-PREPARE");
                    qcertpre = new ProtocolCertificate(preprepared.SeqNr, Serv.CurView, digest, CertType.Prepared, preprepared); //note Serv.CurView == prepared.ViewNr which is checked in t.Validate //Add Prepare to Certificate
                    curSeq = qcertpre.SeqNr; 
                    Serv.InitializeLog(curSeq);
                    PhaseMessage prepare = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.Prepare); //Send async message Prepare
                    Serv.SignMessage(prepare, MessageType.PhaseMessage);
                    qcertpre.ProofList.Add(prepare); //add its own, really should be validated, but not sure how.
                    //Serv.EmitPhaseMessageLocally(prepare);
                }
                if (Active) cancel.Cancel();
                else throw new TimeoutException("Timeout Occurred! System is no longer active!");
                //Prepare phase
                ProtocolCertificate qcertcom = new ProtocolCertificate(qcertpre.SeqNr, Serv.CurView, digest, CertType.Committed);
                var prepared = MesBridge
                    .Where(pm => pm.PhaseType == PMessageType.Prepare)
                    .Where(pm => pm.SeqNr == qcertpre.SeqNr)
                    .Where(pm =>
                    {
                        Console.WriteLine("MESSAGEBRIDGE VALIDATING MESSAGE");
                        return pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView, Serv.CurSeqRange,
                            qcertpre);
                    })
                    .Where(pm => pm.Digest.SequenceEqual(qcertpre.CurReqDigest))
                    .Scan(qcertpre.ProofList, (prooflist, message) =>
                    {
                        prooflist.Add(message);
                        return prooflist;
                    })
                    .Where(_ => qcertpre.ValidateCertificate(FailureNr))
                    .Next();

                var committed = MesBridge  //await incoming PhaseMessages Where = MessageType.Commit Until Consensus Reached
                    .Where(pm => pm.PhaseType == PMessageType.Commit)
                    .Where(pm => pm.SeqNr == qcertcom.SeqNr)
                    .Where(pm => pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView, Serv.CurSeqRange, qcertcom))
                    .Where(pm => pm.Digest.SequenceEqual(qcertcom.CurReqDigest))
                    .Scan(qcertcom.ProofList, (prooflist, message) =>
                    {
                        prooflist.Add(message);
                        return prooflist;
                    })
                    .Where(_ => qcertcom.ValidateCertificate(FailureNr))
                    .Where(_ => qcertpre.ValidateCertificate(FailureNr))
                    .DisposeOn(ShutdownBridge.Next())
                    .Next();
                
                Console.WriteLine("Waiting for prepares");
                if (Active) await prepared;
                else throw new ConstraintException("System is no longer active!");
                
                //Commit phase
                Serv.AddProtocolCertificate(qcertpre.SeqNr, qcertpre); //add first certificate to Log
                PhaseMessage commitmes = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.Commit);
                Serv.SignMessage(commitmes, MessageType.PhaseMessage);
                //Serv.Multicast(commitmes.SerializeToBuffer(), MessageType.PhaseMessage); //Send async message Commit
                Serv.EmitPhaseMessageLocally(commitmes);
                Console.WriteLine("Waiting for commits");
                if (Active) await committed;
                else throw new ConstraintException("System is no longer active!");
                
                //Reply
                Serv.AddProtocolCertificate(qcertcom.SeqNr, qcertcom);
                Console.WriteLine($"Completing operation: {clireq.Message}");
                var rep = new Reply(Serv.ServID, curSeq, Serv.CurView, true, clireq.Message,clireq.Timestamp);
                Serv.SignMessage(rep, MessageType.Reply);
                Serv.ReplyLog[curSeq] = rep;
                //Serv.SendMessage(rep.SerializeToBuffer(), Serv.ClientConnInfo[clireq.ClientID].Socket, MessageType.Reply);
                return rep;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in ProtocolExecution!");
                Console.WriteLine(e);
                var rep = new Reply(Serv.ServID, Serv.CurSeqNr++, Serv.CurView, false, "Failure", clireq.Timestamp);
                
                return rep;
            }
        }
        
        public async CTask HandlePrimaryChange()
        {
            Console.WriteLine("HandlePrimaryChange");
            ViewChange:
            
            //Step 1.
            Serv.CurPrimary.NextPrimary();
            Serv.CurView++;
            ViewChangeCertificate vcc;
            Console.WriteLine("Initialize ViewChangeCertificate");
            if (!Serv.ViewMessageRegister.ContainsKey(Serv.CurView))
            {
                vcc = new ViewChangeCertificate(Serv.CurPrimary, Serv.StableCheckpointsCertificate, null, Serv.EmitViewChange);
                Console.WriteLine("Adding viewcert to registry");
                Serv.ViewMessageRegister[Serv.CurView] = vcc;
            }
            else
            {
                vcc = Serv.ViewMessageRegister[Serv.CurView];
                Console.WriteLine("Obtained viewcert from registry");
                vcc.CalledShutdown = true;
                vcc.EmitShutdown = null;
            }
            var listener = ListenForViewChange();
            var shutdownsource = new Source<bool>();
            ViewChange vc;
            CDictionary<int, ProtocolCertificate> preps;
            if (Serv.StableCheckpointsCertificate == null)
            {
                preps = Serv.CollectPrepareCertificates(-1);
                vc = new ViewChange(0,Serv.ServID, Serv.CurView, null, preps);
            }
            else
            {
                int stableseq = Serv.StableCheckpointsCertificate.LastSeqNr;
                preps = Serv.CollectPrepareCertificates(stableseq);
                vc = new ViewChange(stableseq,Serv.ServID, Serv.CurView, Serv.StableCheckpointsCertificate, preps);
            } 
            //Step 2.
            Serv.SignMessage(vc, MessageType.ViewChange);
            vcc.AppendViewChange(vc, Serv.Pubkey, FailureNr);
            Serv.Multicast(vc.SerializeToBuffer(), MessageType.ViewChange);
            CancellationTokenSource cancel = new CancellationTokenSource();
            _= TimeoutOps.AbortableProtocolTimeoutOperationCTask(shutdownsource, 10000, cancel.Token);
            bool vcs = await WhenAny<bool>.Of(listener, ListenForShutdown(shutdownsource));
            Console.WriteLine("vcs: " + vcs);
            if (!vcs) goto ViewChange;
            cancel.Cancel();
            
            //Step 3 -->.
            Source<bool> shutdownsource2 = new Source<bool>();
            CancellationTokenSource cancel2 = new CancellationTokenSource();
            _= TimeoutOps.AbortableProtocolTimeoutOperationCTask(shutdownsource2, 15000, cancel2.Token);
            bool val = await WhenAny<bool>.Of(ViewChangeProtocol(preps, vcc), ListenForShutdown(shutdownsource2));
            Console.WriteLine("val: " + val);
            if (!val) goto ViewChange;
            cancel2.Cancel();
        }

        public async CTask<bool> ListenForViewChange()
        {
            var test = await ViewChangeBridge.Next();
            Console.WriteLine("Received View-Change");
            return test;
        }

        public async CTask<bool> ListenForShutdown(Source<bool> shutemit)
        {
            var test = await shutemit.Next();
            Console.WriteLine("View Change Received Shutdown");
            return test;
        }

        public async CTask<bool> ViewChangeProtocol(CDictionary<int, ProtocolCertificate> preps, ViewChangeCertificate vcc)
        {
            Console.WriteLine("ViewChangeProtocol");
            if (Serv.IsPrimary())
            {
                Console.WriteLine("server is primary");
                //Step 3.
                //startval is first entry after last checkpoint, lastval is the last sequence number performed, which could be either CurSeq or CurSeq+1 depending on where the system called for view-change
                int low;
                if (Serv.StableCheckpointsCertificate == null) low = Serv.CurSeqRange.Start.Value;
                else low = Serv.StableCheckpointsCertificate.LastSeqNr + 1;
                int high = Serv.CurSeqNr;
                var prepares = Serv.CurPrimary.MakePrepareMessages(preps, low, high);
                for (var idx=0; idx<prepares.Count; idx++)
                    Serv.SignMessage(prepares[idx], MessageType.PhaseMessage);
                Console.WriteLine("Creating NewView");
                vcc.ScaleDownViewProofs();
                var nvmes = new NewView(Serv.CurView, vcc, prepares);
                Serv.SignMessage(nvmes, MessageType.NewView);
                
                //Step 4.
                await Sleep.Until(1000);
                Serv.Multicast(nvmes.SerializeToBuffer(), MessageType.NewView);
                Console.WriteLine("Calling RedoMessage");
                await RedoMessage(prepares);
                Console.WriteLine("RedoMessage fin");
                return true;
            }
            else
            {
                Console.WriteLine("server is not primary");
                //Step 4-2.
                var leaderpubkey = Serv.ServPubKeyRegister[Serv.CurPrimary.ServID];
                var newviewmes = await NewViewBridge
                    .Where(newview => newview.Validate(leaderpubkey, Serv.CurView))
                    .Next();
                Console.WriteLine("Received Valid NewView");
                var check = true;
                Console.WriteLine("Verifying pre-prepares");
                foreach (var prepre in newviewmes.PrePrepMessages)
                {
                    if (!Crypto.VerifySignature(
                        prepre.Signature, 
                        prepre.CreateCopyTemplate().SerializeToBuffer(),
                        leaderpubkey)
                    )
                    {
                        check = false;
                        break;
                    }
                }
                Console.WriteLine("Calling RedoMessage");
                if (check) await RedoMessage(newviewmes.PrePrepMessages);
                else return false;
                Console.WriteLine("RedoMessage fin");
                return true;
            }
        }

        public async CTask RedoMessage(CList<PhaseMessage> oldpreList)
        {
            Console.WriteLine("RedoMessage");
            //Step 5.
            foreach (var prepre in oldpreList)
            {
                var precert = new ProtocolCertificate(prepre.SeqNr, prepre.ViewNr, prepre.Digest, CertType.Prepared, prepre); //need a way to know request digest and request message
                var comcert = new ProtocolCertificate(prepre.SeqNr, prepre.ViewNr, prepre.Digest, CertType.Committed);
                Console.WriteLine("Initialize Log");
                Serv.InitializeLog(prepre.SeqNr);

                var preps = ReMesBridge
                    .Where(pm => pm.PhaseType == PMessageType.Prepare)
                    .Where(pm => pm.SeqNr == prepre.SeqNr)
                    .Where(pm => pm.ValidateRedo(Serv.ServPubKeyRegister[pm.ServID], prepre.ViewNr))
                    .Scan(precert.ProofList, (prooflist, message) =>
                    {
                        prooflist.Add(message);
                        return prooflist;
                    })
                    .Where(_ => precert.ValidateCertificate(FailureNr))
                    .Next();
                var coms = ReMesBridge
                    .Where(pm => pm.PhaseType == PMessageType.Commit)
                    .Where(pm => pm.SeqNr == comcert.SeqNr)
                    .Where(pm => pm.ValidateRedo(Serv.ServPubKeyRegister[pm.ServID], prepre.ViewNr))
                    .Scan(comcert.ProofList, (prooflist, message) =>
                    {
                        prooflist.Add(message);
                        return prooflist;
                    })
                    .Where(_ => comcert.ValidateCertificate(FailureNr))
                    .Next();
                    
                if (!Serv.IsPrimary())
                {
                    var prepare = new PhaseMessage(Serv.ServID, prepre.SeqNr, prepre.ViewNr, prepre.Digest, PMessageType.Prepare);
                    Serv.SignMessage(prepare, MessageType.PhaseMessage);
                    Serv.Multicast(prepare.SerializeToBuffer(), MessageType.PhaseMessage);
                    Serv.EmitRedistPhaseMessageLocally(prepare);
                }
                
                await preps;
                await Sleep.Until(500);
                Console.WriteLine("Prepare certificate: " + precert.SeqNr + " is finished");
                Serv.AddProtocolCertificate(prepre.SeqNr, precert);
                Console.WriteLine("Finished adding the new certificate to server!");
                var commes = new PhaseMessage(Serv.ServID, prepre.SeqNr, prepre.ViewNr, prepre.Digest, PMessageType.Commit);
                Console.WriteLine("Made commit");
                Serv.SignMessage(commes, MessageType.PhaseMessage);
                Serv.Multicast(commes.SerializeToBuffer(), MessageType.PhaseMessage);
                Serv.EmitRedistPhaseMessageLocally(commes);
                
                await coms;
                Console.WriteLine("Commit certificate: " + comcert.SeqNr + " is finished");
                Serv.AddProtocolCertificate(prepre.SeqNr, comcert);
            }
        }

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(Serv), Serv);
            stateToSerialize.Set(nameof(FailureNr), FailureNr);
            stateToSerialize.Set(nameof(MesBridge), MesBridge);
            stateToSerialize.Set(nameof(ReMesBridge), ReMesBridge);
            stateToSerialize.Set(nameof(NewViewBridge), NewViewBridge);
            stateToSerialize.Set(nameof(ViewChangeBridge), ViewChangeBridge);
            stateToSerialize.Set(nameof(ShutdownBridge), ShutdownBridge);
            stateToSerialize.Set(nameof(ShutdownBridgePhase), ShutdownBridgePhase);
        }

        public static ProtocolExecution Deserialize(IReadOnlyDictionary<string, object> sd)
            => new ProtocolExecution(
                sd.Get<Server>(nameof(Serv)),
                sd.Get<int>(nameof(FailureNr)),
                sd.Get<Source<PhaseMessage>>(nameof(MesBridge)),
                sd.Get<Source<PhaseMessage>>(nameof(ReMesBridge)),
                sd.Get<Source<PhaseMessage>>(nameof(ShutdownBridgePhase)),
                sd.Get<Source<bool>>(nameof(ViewChangeBridge)),
                sd.Get<Source<NewView>>(nameof(NewViewBridge)),
                sd.Get<Source<bool>>(nameof(ShutdownBridge))
            );
    }
}