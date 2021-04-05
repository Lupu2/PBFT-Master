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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.PersistentDataStructures;

namespace PBFT.Replica
{
    public class ProtocolExecution : IPersistable
    {
        public Server Serv {get; set;}
        public int FailureNr {get; set;}
        public bool Active { get; set; }
        private readonly object _sync = new object();
        private Source<PhaseMessage> MesBridge;
        private Source<bool> ViewChangeBridge;
        //private Source<ViewChangeCertificate> ShutdownBridge;
        private Source<NewView> NewViewBridge;
        //public CancellationTokenSource cancel = new CancellationTokenSource(); //Set timeout for async functions

        public ProtocolExecution(Server server, int fnodes, Source<PhaseMessage> mesbridge, Source<bool> viewchangebridge, Source<NewView> newviewbridge) 
        {
            Serv = server;
            FailureNr = fnodes;
            Active = true;
            MesBridge = mesbridge;
            ViewChangeBridge = viewchangebridge;
            NewViewBridge = newviewbridge;
            //ShutdownBridge = shutbridge;
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
                    //curSeq = ++Serv.CurSeqNr; //single threaded and asynchronous, only a single HandleRequest has access to this variable at the time.
                    Serv.InitializeLog(curSeq);
                    PhaseMessage preprepare = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.PrePrepare);
                    Serv.SignMessage(preprepare, MessageType.PhaseMessage);
                    qcertpre = new ProtocolCertificate(preprepare.SeqNr, preprepare.ViewNr, digest, CertType.Prepared, preprepare); //Log preprepare as Prepare
                    Thread.Sleep(1000);
                    Serv.Multicast(preprepare.SerializeToBuffer(), MessageType.PhaseMessage); //Send async message PrePrepare
                }else{ //Replicas
                    var preprepared = await MesBridge
                        .Where(pm => pm.PhaseType == PMessageType.PrePrepare)
                        .Where(pm => {
                                Console.WriteLine("PRE-Prepare MESSAGEBRIDGE VALIDATING MESSAGE");
                                return pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView, Serv.CurSeqRange);
                            })
                        .Next();
                    //Add functionality for if you get another prepare message with same view but different seq nr, while you are already working on another,then you know that the primary is faulty.
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
                cancel.Cancel();
                //Prepare phase
                ProtocolCertificate qcertcom = new ProtocolCertificate(qcertpre.SeqNr, Serv.CurView, digest, CertType.Committed);   
                var prepared = MesBridge
                    .Where(pm => pm.PhaseType == PMessageType.Prepare)
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
                await prepared;
                
                //Commit phase
                Serv.AddProtocolCertificate(qcertpre.SeqNr, qcertpre); //add first certificate to Log
                PhaseMessage commitmes = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.Commit);
                Serv.SignMessage(commitmes, MessageType.PhaseMessage);
                Serv.Multicast(commitmes.SerializeToBuffer(), MessageType.PhaseMessage); //Send async message Commit
                //qcertcom.ProofList.Add(commitmes);
                Serv.EmitPhaseMessageLocally(commitmes);
                Console.WriteLine("Waiting for commits");
                await committed;
                
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
                var rep = new Reply(Serv.ServID, Serv.CurSeqNr++, Serv.CurView, false, "Failure", clireq.Timestamp);
                return rep;
            }
        }

        //TODO change the test version to fit the new version 
        //Function that performs all the operations in HandleRequest but without sending anything. Used for testing the operations performed in the function..
        public async CTask<Reply> HandleRequestTest(Request clireq, int leaderseq)
        {
            try
            {
                byte[] digest;
                ProtocolCertificate qcertpre;
                digest = Crypto.CreateDigest(clireq);
                int curSeq;

                //Prepare:
                if (Serv.IsPrimary()) //Primary
                {
                    Console.WriteLine("Server is primary");
                    curSeq = leaderseq; //single threaded and asynchronous, only a single HandleRequest has access to this variable at the time.
                    Serv.InitializeLog(curSeq);
                    PhaseMessage preprepare = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest,
                        PMessageType.PrePrepare);
                    Serv.SignMessage(preprepare, MessageType.PhaseMessage);
                    qcertpre = new ProtocolCertificate(preprepare.SeqNr, preprepare.ViewNr, digest, CertType.Prepared,
                        preprepare); //Log preprepare as Prepare
                }
                else
                {
                    //Replicas
                    Console.WriteLine("Server is not primary");
                    // await incomming PhaseMessages Where = MessageType.PrePrepare
                    //_ = TimeoutOps.ProtocolTimeoutOperation(ShutdownBridge, 1000);
                    var preprepared = await MesBridge
                        //.DisposeOn(ShutdownBridge.Next())
                        .Where(pm => pm.PhaseType == PMessageType.PrePrepare)
                        .Where(pm =>
                            pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView,
                                Serv.CurSeqRange))
                        .Next();
                    Console.WriteLine("Finished Preprepare");
                    Console.WriteLine(preprepared);
                    qcertpre = new ProtocolCertificate(preprepared.SeqNr, Serv.CurView, digest, CertType.Prepared,
                        preprepared); //note Serv.CurView == prepared.ViewNr which is checked in t.Validate //Add Prepare to Certificate
                    curSeq = qcertpre.SeqNr;
                    Serv.InitializeLog(curSeq);
                    PhaseMessage prepare = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest,
                        PMessageType.Prepare);
                    Serv.SignMessage(prepare, MessageType.PhaseMessage);
                    qcertpre.ProofList.Add(prepare);
                }

                //Prepare phase
                //await incoming PhaseMessages Where = MessageType.Prepare Add to Certificate Until Consensus Reached
                ProtocolCertificate qcertcom =
                    new ProtocolCertificate(qcertpre.SeqNr, Serv.CurView, digest, CertType.Committed);
                var prepared = MesBridge
                    .Where(pm => pm.PhaseType == PMessageType.Prepare)
                    .Where(pm => pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView, Serv.CurSeqRange, qcertpre))
                    .Where(pm => pm.Digest.SequenceEqual(qcertpre.CurReqDigest))
                    .Scan(qcertpre.ProofList, (prooflist, message) =>
                    {
                        prooflist.Add(message);
                        return prooflist;
                    })
                    .Where(_ => qcertpre.ValidateCertificate(FailureNr))
                    .Next(); //probably won't work
                
                //CList<PhaseMessage> eks = new CList<PhaseMessage>();
                var committed= MesBridge //await incoming PhaseMessages Where = MessageType.Commit Until Consensus Reached
                        .Where(pm => pm.PhaseType == PMessageType.Commit)
                        .Where(pm => pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView, Serv.CurSeqRange,
                            qcertcom))
                        .Where(pm => pm.Digest.SequenceEqual(qcertcom.CurReqDigest))
                        .Scan(qcertcom.ProofList, (prooflist, message) =>
                        {
                            prooflist.Add(message);
                            return prooflist;
                        })
                        .Where(_ => qcertcom.ValidateCertificate(FailureNr))
                        .Where(_ => qcertpre.ValidateCertificate(FailureNr))
                        //.DisposeOn(ShutdownBridge.Next())
                        .Next();
                //qcertcom.ProofList = eks;
                Console.WriteLine("Waiting for Prepare messages");
                await prepared;
                Serv.AddProtocolCertificate(qcertpre.SeqNr, qcertpre); //add first certificate to Log
                Console.WriteLine("Prepare phase finished");

                //Commit phase
                PhaseMessage commitmes = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.Commit);
                Serv.SignMessage(commitmes, MessageType.PhaseMessage);
                Serv.EmitPhaseMessageLocally(commitmes);
                Console.WriteLine("Waiting for Commit messages");
                await committed;
                
                //Reply
                //Save the 2 Certificates
                Serv.AddProtocolCertificate(qcertcom.SeqNr, qcertcom);
                Console.WriteLine("Commit phase finished");
                Console.WriteLine($"Completing operation: {clireq.Message}");
                var rep = new Reply(Serv.ServID, curSeq, Serv.CurView, true, clireq.Message, clireq.Timestamp);
                Serv.SignMessage(rep, MessageType.Reply);
                return rep;
                
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in ProtocolExecution!");
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task HandlePrimaryChange()
        {
            ViewChange:
            //Step 1.
            Serv.CurPrimary.NextPrimary();
            Serv.CurView++;
            //Serv.CurView = Serv.CurPrimary.ViewNr;
            ViewChangeCertificate vcc;
            if (Serv.ViewMessageRegister.ContainsKey(Serv.CurView))
            {
                vcc = new ViewChangeCertificate(Serv.CurPrimary, Serv.StableCheckpointsCertificate, Serv.EmitShutdown, Serv.EmitViewChange);
                Serv.ViewMessageRegister[Serv.CurView] = vcc;
            }
            else vcc = Serv.ViewMessageRegister[Serv.CurView];
            var listener = ListenForViewChange();
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
            Serv.Multicast(vc.SerializeToBuffer(), MessageType.ViewChange);
            bool vcs = await Task.WhenAny(ListenTaskHandler(listener), TimeoutOps.TimeoutOperation(10000)).Result;
            if (!vcs) goto ViewChange;
                //Step 3 -->.
            bool val = await Task.WhenAny(ViewChangeProtocol(preps, vcc), TimeoutOps.TimeoutOperation(15000)).Result;
            if (!val) goto ViewChange;
            Active = true;
            Serv.GarbageViewChangeRegistry(Serv.CurView);
        }
        
        public async Task<bool> ListenTaskHandler(CTask<bool> listener) => await listener;
        
        public async CTask<bool> ListenForViewChange() => await ViewChangeBridge.Next();
        
        public async Task<bool> ViewChangeProtocol(CDictionary<int, ProtocolCertificate> preps, ViewChangeCertificate vcc)
        {
            if (Serv.IsPrimary())
            {
                //startval is first entry after last checkpoint, lastval is the last sequence number performed, which could be either CurSeq or CurSeq+1 depending on where the system called for view-change
                //Step 3.
                int low;
                if (Serv.StableCheckpointsCertificate == null) low = Serv.CurSeqRange.Start.Value;
                else low = Serv.StableCheckpointsCertificate.LastSeqNr + 1;
                int high = Serv.CurSeqNr + 1;
                var prepares = Serv.CurPrimary.MakePrepareMessages(preps, low, high);
                for (var idx=0; idx<prepares.Count; idx++)
                    Serv.SignMessage(prepares[idx], MessageType.PhaseMessage);
                var nvmes = new NewView(Serv.CurView, vcc, prepares);
                Serv.SignMessage(nvmes, MessageType.NewView);
                //Step 4.
                Serv.Multicast(nvmes.SerializeToBuffer(), MessageType.NewView);
                await RedoMessage(prepares);
                return true;
            }
            else
            {
                //Step 4-2.
                var leaderpubkey = Serv.ServPubKeyRegister[Serv.CurPrimary.ServID];
                var newviewmes = await NewViewBridge
                    .Where(newview => newview.Validate(leaderpubkey, Serv.CurView))
                    .Next();
                var check = true;
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

                if (check) await RedoMessage(newviewmes.PrePrepMessages);
                else return false;
                return true;
            }
        }

        public async CTask RedoMessage(CList<PhaseMessage> oldpreList)
        {
            //Step 5.
            foreach (var prepre in oldpreList)
            {
                var precert = new ProtocolCertificate(prepre.SeqNr, prepre.ViewNr, prepre.Digest, CertType.Prepared, prepre); //need a way to know request digest and request message
                var comcert = new ProtocolCertificate(prepre.SeqNr, prepre.ViewNr, prepre.Digest, CertType.Committed);
                if (!Serv.IsPrimary())
                {
                    var prepare = new PhaseMessage(Serv.ServID, prepre.SeqNr, prepre.ViewNr, prepre.Digest, PMessageType.Prepare);
                    Serv.SignMessage(prepare, MessageType.PhaseMessage);
                    Serv.Multicast(prepare.SerializeToBuffer(), MessageType.PhaseMessage);
                }
                
                var preps = MesBridge
                    .Where(pm => pm.PhaseType == PMessageType.Prepare)
                    .Where(pm => pm.ValidateRedo(Serv.ServPubKeyRegister[pm.ServID], prepre.ViewNr))
                    .Next();
                var coms = MesBridge.Where(pm => pm.PhaseType == PMessageType.Commit)
                    .Where(pm => pm.ValidateRedo(Serv.ServPubKeyRegister[pm.ServID], prepre.ViewNr))
                    .Next();
                await preps;
                Serv.AddProtocolCertificate(prepre.SeqNr, precert);
                
                var commes = new PhaseMessage(Serv.ServID, prepre.SeqNr, prepre.ViewNr, prepre.Digest, PMessageType.Commit);
                Serv.SignMessage(commes, MessageType.PhaseMessage);
                Serv.Multicast(commes.SerializeToBuffer(), MessageType.PhaseMessage);
                Serv.EmitPhaseMessageLocally(commes);
                await coms;
                Serv.AddProtocolCertificate(prepre.SeqNr, comcert);
            }
        }

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(Serv), Serv);
            stateToSerialize.Set(nameof(FailureNr), FailureNr);
            stateToSerialize.Set(nameof(MesBridge), MesBridge);
            stateToSerialize.Set(nameof(NewViewBridge), NewViewBridge);
            stateToSerialize.Set(nameof(ViewChangeBridge), ViewChangeBridge);
            //stateToSerialize.Set(nameof(ShutdownBridge), ShutdownBridge);
        }

        public static ProtocolExecution Deserialize(IReadOnlyDictionary<string, object> sd)
            => new ProtocolExecution(
                sd.Get<Server>(nameof(Serv)),
                sd.Get<int>(nameof(FailureNr)),
                sd.Get<Source<PhaseMessage>>(nameof(MesBridge)),
                sd.Get<Source<bool>>(nameof(ViewChangeBridge)),
                sd.Get<Source<NewView>>(nameof(NewViewBridge))
                //sd.Get<Source<ViewChangeCertificate>>(nameof(ShutdownBridge))
                );
    }
}