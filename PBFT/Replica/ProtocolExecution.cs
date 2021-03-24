using Cleipnir.ExecutionEngine.Providers;
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
using System.Threading;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.Rx.ExecutionEngine;
using Newtonsoft.Json;

namespace PBFT.Replica
{
    public class ProtocolExecution : IPersistable
    {
        public Server Serv {get; set;}
        public int FailureNr {get; set;}
        public bool Active { get; set; }
        private readonly object _sync = new object();
        private Source<PhaseMessage> MesBridge;
        private Source<ViewChangeCertificate> ShutdownBridge;

        //public CancellationTokenSource cancel = new CancellationTokenSource(); //Set timeout for async functions

        public ProtocolExecution(Server server, int fnodes, Source<PhaseMessage> mesbridge, Source<ViewChangeCertificate> shutbridge) 
        {
            Serv = server;
            FailureNr = fnodes;
            Active = true;
            MesBridge = mesbridge;
            ShutdownBridge = shutbridge;
        }

        public async CTask<Reply> HandleRequest(Request clireq)
        {

            //await Sync.Next();
            try
            {
                byte[] digest;
            ProtocolCertificate qcertpre;
            digest = Crypto.CreateDigest(clireq);
            int curSeq; //change later
            
            //Prepare:
            if (Serv.IsPrimary()) //Primary
            {
                /*lock (_sync)
                {
                    curSeq = Serv.CurSeqNr++; //<-causes problems for multiple request 
                }*/
                curSeq = ++Serv.CurSeqNr;
                //curSeq = ++Serv.CurSeqNr; //single threaded and asynchronous, only a single HandleRequest has access to this variable at the time.
                var init = Serv.InitializeLog(curSeq);
                if (!init) return null; 
                PhaseMessage preprepare = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.PrePrepare);
                preprepare = (PhaseMessage) Serv.SignMessage(preprepare, MessageType.PhaseMessage);
                qcertpre = new ProtocolCertificate(preprepare.SeqNr, preprepare.ViewNr, clireq, CertType.Prepared, preprepare); //Log preprepare as Prepare
                await Serv.Multicast(preprepare.SerializeToBuffer(), MessageType.PhaseMessage); //Send async message PrePrepare
            }else{ //Replicas
                // await incomming PhaseMessages Where = MessageType.PrePrepare
                    
                var preprepared = await MesBridge
                    .Where(pm => pm.PhaseType == PMessageType.PrePrepare)
                    .Where(pm => pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView, Serv.CurSeqRange))
                    .Next();
                //Add functionality for if you get another prepare message with same view but different seq nr, while you are already working on another,then you know that the primary is faulty.
                
                qcertpre = new ProtocolCertificate(preprepared.SeqNr, Serv.CurView, clireq, CertType.Prepared, preprepared); //note Serv.CurView == prepared.ViewNr which is checked in t.Validate //Add Prepare to Certificate
                curSeq = qcertpre.SeqNr;
                var init = Serv.InitializeLog(curSeq);
                if (!init) return null;
                PhaseMessage prepare = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.Prepare); //Send async message Prepare
                prepare = (PhaseMessage) Serv.SignMessage(prepare, MessageType.PhaseMessage);
                qcertpre.ProofList.Add(prepare); //add its own, really should be validated, but not sure how.
                //Serv.EmitPhaseMessageLocally(prepare);
                await Serv.Multicast(prepare.SerializeToBuffer(), MessageType.PhaseMessage);
            }
            
            //Prepare phase
            ProtocolCertificate qcertcom = new ProtocolCertificate(qcertpre.SeqNr, Serv.CurView, clireq, CertType.Committed);   
            var prepared = MesBridge
                .Where(pm => pm.PhaseType == PMessageType.Prepare)
                .Where(pm => pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView, Serv.CurSeqRange, qcertpre))
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
            Serv.AddProtocolCertificate(qcertpre.SeqNr, qcertpre); //add first certificate to Log
            
            PhaseMessage commitmes = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.Commit);
            commitmes = (PhaseMessage) Serv.SignMessage(commitmes, MessageType.PhaseMessage);
            await Serv.Multicast(commitmes.SerializeToBuffer(), MessageType.PhaseMessage); //Send async message Commit
            //qcertcom.ProofList.Add(commitmes);
            Serv.EmitPhaseMessageLocally(commitmes);
            Console.WriteLine("Waiting for commits");
            await committed;
            
            //validate
            //add list
            //if not quorum -> continue in await
            //else break out continue with rest of the code
            //WAITFORALL()
            
            //Commit phase
            //Commit:
            
            /*await MesBridge  //await incoming PhaseMessages Where = MessageType.Commit Until Consensus Reached
                .Where(pm => pm.PhaseType == PMessageType.Commit)
                .Where(pm => pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView, Serv.CurSeqRange, qcertcom))
                .Scan(qcertcom.ProofList, (prooflist, message) =>
                {
                    prooflist.Add(message);
                    return prooflist;
                })
                .Where(pm => qcertcom.ValidateCertificate(FailureNr))
                .Next();
            */
            
            //Reply
            //Save the 2 Certificates
            Console.WriteLine(qcertcom);
            Serv.AddProtocolCertificate(qcertcom.SeqNr, qcertcom);
            //Need to move or insert await for curSeqNr to be the next request to be handled
            Console.WriteLine($"Completing operation: {clireq.Message}");
            var rep = new Reply(Serv.ServID, curSeq, Serv.CurView, true, clireq.Message,DateTime.Now.ToString());
            rep = (Reply) Serv.SignMessage(rep, MessageType.Reply);
            await Serv.SendMessage(rep.SerializeToBuffer(), Serv.ClientConnInfo[clireq.ClientID].Socket, MessageType.Reply);
            return rep;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in ProtocolExecution!");
                Console.WriteLine(e);
                throw;
            }
        }

        //Function that performs all the operations in HandleRequest but without sending anything. Used for testing the operations performed in the function..
        public async CTask<Reply> HandleRequestTest(Request clireq)
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
                    curSeq = ++Serv
                        .CurSeqNr; //single threaded and asynchronous, only a single HandleRequest has access to this variable at the time.
                    var init = Serv.InitializeLog(curSeq);
                    if (!init) return null;
                    PhaseMessage preprepare = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest,
                        PMessageType.PrePrepare);
                    preprepare = (PhaseMessage) Serv.SignMessage(preprepare, MessageType.PhaseMessage);
                    qcertpre = new ProtocolCertificate(preprepare.SeqNr, preprepare.ViewNr, clireq, CertType.Prepared,
                        preprepare); //Log preprepare as Prepare
                }
                else
                {
                    //Replicas
                    Console.WriteLine("Server is not primary");
                    // await incomming PhaseMessages Where = MessageType.PrePrepare
                    var preprepared = await MesBridge
                        .Where(pm => pm.PhaseType == PMessageType.PrePrepare)

                        .Where(pm =>
                            pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView,
                                Serv.CurSeqRange)) 
                        .Next();

                    qcertpre = new ProtocolCertificate(preprepared.SeqNr, Serv.CurView, clireq, CertType.Prepared,
                        preprepared); //note Serv.CurView == prepared.ViewNr which is checked in t.Validate //Add Prepare to Certificate
                    curSeq = qcertpre.SeqNr;
                    var init = Serv.InitializeLog(curSeq);
                    if (!init) return null;
                    PhaseMessage prepare = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest,
                        PMessageType.Prepare);
                    prepare = (PhaseMessage) Serv.SignMessage(prepare, MessageType.PhaseMessage);
                    //Serv.EmitPhaseMessageLocally(prepare);
                    qcertpre.ProofList.Add(prepare);
                    //MesBridge.Emit(prepare);
                }

                //Prepare phase
                //await incoming PhaseMessages Where = MessageType.Prepare Add to Certificate Until Consensus Reached
                ProtocolCertificate qcertcom =
                    new ProtocolCertificate(qcertpre.SeqNr, Serv.CurView, clireq, CertType.Committed);
                var prepared = MesBridge
                    .Where(pm => pm.PhaseType == PMessageType.Prepare)
                    .Where(pm => pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView, Serv.CurSeqRange, qcertpre))
                    //.Do(pm => qcertpre.ProofList.Add(pm))
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
                        //.Do(pm => qcertcom.ProofList.Add(pm))
                        .Scan(qcertcom.ProofList, (prooflist, message) =>
                        {
                            prooflist.Add(message);
                            return prooflist;
                        })
                        .Where(_ => qcertcom.ValidateCertificate(FailureNr))
                        .Where(_ => qcertpre.ValidateCertificate(FailureNr))
                        .Next();
                //qcertcom.ProofList = eks;
                Console.WriteLine("Waiting for Prepare messages");
                //await prepared.Where(_ => qcertpre.ValidateCertificate(FailureNr)).Next();
                await prepared;
                Serv.AddProtocolCertificate(qcertpre.SeqNr, qcertpre); //add first certificate to Log
                Console.WriteLine("Prepare phase finished");

                //Commit phase
                //Commit:
                PhaseMessage commitmes = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.Commit);
                commitmes = (PhaseMessage) Serv.SignMessage(commitmes, MessageType.PhaseMessage);
                Serv.EmitPhaseMessageLocally(commitmes);
                Console.WriteLine("Waiting for Commit messages");
                //await committed.Where(_ => qcertcom.ValidateCertificate(FailureNr)).Next();
                await committed;

                //Reply
                //Save the 2 Certificates
                Serv.AddProtocolCertificate(qcertcom.SeqNr, qcertcom);
                Console.WriteLine("Commit phase finished");
                //Need to move or insert await for curSeqNr to be the next request to be handled
                Console.WriteLine($"Completing operation: {clireq.Message}");
                var rep = new Reply(Serv.ServID, curSeq, Serv.CurView, true, clireq.Message,
                    DateTime.Now.ToString());
                rep = (Reply) Serv.SignMessage(rep, MessageType.Reply);
                //await Serv.SendMessage(rep.SerializeToBuffer(), Serv.ClientConnInfo[clireq.ClientID].Socket, MessageType.Reply);
                return rep;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in ProtocolExecution!");
                Console.WriteLine(e);
                throw;
            }
        }

        public async CTask HandlePrimaryChange(ViewChangeCertificate vcc)
        {
            Active = false;
            ViewChange:
            Serv.CurPrimary.NextPrimary();
            ViewChange vc;
            CList<ProtocolCertificate> preps;
            if (Serv.StableCheckpoints == null)
            {
                preps = Serv.CollectPrepareCertificates(0);
                vc = new ViewChange(0,Serv.ServID, Serv.CurView, Serv.StableCheckpoints, preps);
            }
            else
            {
                int stableseq = Serv.StableCheckpoints.LastSeqNr;
                preps = Serv.CollectPrepareCertificates(stableseq);
                vc = new ViewChange(stableseq,Serv.ServID, Serv.CurView, Serv.StableCheckpoints, preps);
            }

            await Serv.Multicast(vc.SerializeToBuffer(), MessageType.ViewChange);
            
            //Start timeout
            //await View Change message validation/ have enough, need referanse to existing View Certificate
            //if timeout --> goto ViewChange
            bool primary = Serv.IsPrimary();
            if (primary)
            {
                var prepares = Serv.CurPrimary.MakePrepareMessages(preps, Serv.CurSeqRange.Start.Value, Serv.CurSeqRange.End.Value);
                var nvmes = new NewView(Serv.CurView, vcc, prepares);
                await Serv.Multicast(nvmes.SerializeToBuffer(), MessageType.NewView);
                await RedoMessage(prepares);
                Active = true;
            }
            else
            {
                
            }
            
        }

        public async CTask RedoMessage(CList<PhaseMessage> oldpreList)
        {
            
        }
        
        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(Serv), Serv);
            stateToSerialize.Set(nameof(FailureNr), FailureNr);
            stateToSerialize.Set(nameof(MesBridge), MesBridge);
            stateToSerialize.Set(nameof(ShutdownBridge), ShutdownBridge);
        }

        public static ProtocolExecution Deserialize(IReadOnlyDictionary<string, object> sd)
            => new ProtocolExecution(
                sd.Get<Server>(nameof(Serv)),
                sd.Get<int>(nameof(FailureNr)),
                sd.Get<Source<PhaseMessage>>(nameof(MesBridge)),
                sd.Get<Source<ViewChangeCertificate>>(nameof(ShutdownBridge))
                );
    }
}