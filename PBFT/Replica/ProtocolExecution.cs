using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using PBFT.Helper;
using PBFT.Messages;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.Rx.ExecutionEngine;

namespace PBFT.Replica
{
    public class ProtocolExecution : IPersistable
    {
        public Server Serv {get; set;}
        
        public int FailureNr {get; set;}
        
        private readonly object _sync = new object();

        private Source<PhaseMessage> MesBridge;

        //public CancellationTokenSource cancel = new CancellationTokenSource(); //Set timeout for async functions

        public ProtocolExecution(Server server, int fnodes, Source<PhaseMessage> bridge) 
        {
            Serv = server;
            FailureNr = fnodes;
            MesBridge = bridge;
        }

        public async CTask<Reply> HandleRequest(Request clireq) 
        {
            byte[] digest;
            QCertificate qcertpre;
            digest = Crypto.CreateDigest(clireq);
            int curSeq; //change later
            
            //Prepare:
            if (Serv.IsPrimary()) //Primary
            {
                /*lock (_sync)
                {
                    curSeq = Serv.CurSeqNr++; //<-causes problems for multiple request 
                }*/
                curSeq = ++Serv.CurSeqNr; //single threaded and asynchronous, only a single HandleRequest has access to this variable at the time.
                var init = Serv.InitializeLog(curSeq);
                if (!init) return null; 
                PhaseMessage preprepare = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.PrePrepare);
                preprepare = (PhaseMessage) Serv.SignMessage(preprepare, MessageType.PhaseMessage);
                qcertpre = new QCertificate(preprepare.SeqNr, preprepare.ViewNr, CertType.Prepared, preprepare); //Log preprepare as Prepare
                await Serv.Multicast(preprepare.SerializeToBuffer(), MessageType.PhaseMessage); //Send async message PrePrepare
            }else{ //Replicas
                // await incomming PhaseMessages Where = MessageType.PrePrepare
                    var preprepared = await MesBridge
                        .Where(pm => pm.PhaseType == PMessageType.PrePrepare)
                        
                        .Where(pm => pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView, Serv.CurSeqRange)) //oversight, not the servers pubkey the message id's pubkey!!!
                        .Next();
                    
                    qcertpre = new QCertificate(preprepared.SeqNr, Serv.CurView, CertType.Prepared, preprepared); //note Serv.CurView == prepared.ViewNr which is checked in t.Validate //Add Prepare to Certificate
                    curSeq = qcertpre.SeqNr;
                    var init = Serv.InitializeLog(curSeq);
                    if (!init) return null;
                    PhaseMessage prepare = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.Prepare); //Send async message Prepare
                    prepare = (PhaseMessage) Serv.SignMessage(prepare, MessageType.PhaseMessage);
                    qcertpre.ProofList.Add(prepare); //add its own, really should be validated, but not sure how.
                    await Serv.Multicast(prepare.SerializeToBuffer(), MessageType.PhaseMessage);
                    /*catch(TaskCanceledException) //Probably placed so that the rest of the code is not runned after primary is deemed faulty
                    {   
                        Console.WriteLine("Primary deemed faulty start sending new messages");
                        //ViewChange vc = new ViewChange(Serv.ServID, Serv.CurView);
                        //await Serv.Multicast(vc.SerializeToBuffer());
                    }*/
            }
            
            //Prepare phase
            //await incoming PhaseMessages Where = MessageType.Prepare Add to Certificate Until Consensus Reached
            await MesBridge
                .Where(pm => pm.PhaseType == PMessageType.Prepare)
                .Where(pm => pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView, Serv.CurSeqRange, qcertpre))
                .Scan(qcertpre.ProofList, (prooflist, message) =>
                {
                    prooflist.Add(message);
                    return prooflist;
                })
                .Where(pm => qcertpre.ValidateCertificate(FailureNr)) //probably won't work
                .Next();
            
            //validate
            //add list
            //if not quorum -> continue in await
            //else break out continue with rest of the code
            //WAITFORALL()
            
            Serv.AddCertificate(qcertpre.SeqNr, qcertpre); //add first certificate to Log
            
            //Commit phase
            //Commit:
            QCertificate qcertcom = new QCertificate(qcertpre.SeqNr, Serv.CurView, CertType.Committed);
            PhaseMessage commitmes = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.Commit);
            commitmes = (PhaseMessage) Serv.SignMessage(commitmes, MessageType.PhaseMessage);
            await Serv.Multicast(commitmes.SerializeToBuffer(), MessageType.PhaseMessage); //Send async message Commit
            await MesBridge  //await incoming PhaseMessages Where = MessageType.Commit Until Consensus Reached
                .Where(pm => pm.PhaseType == PMessageType.Commit)
                .Where(pm => pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView, Serv.CurSeqRange, qcertcom))
                .Scan(qcertcom.ProofList, (prooflist, message) =>
                {
                    prooflist.Add(message);
                    return prooflist;
                })
                .Where(pm => qcertcom.ValidateCertificate(FailureNr))
                .Next();
            
            //Reply
            //Save the 2 Certificates
            Serv.AddCertificate(qcertcom.SeqNr, qcertcom);
            //Need to move or insert await for curSeqNr to be the next request to be handled
            Console.WriteLine($"Completing operation: {clireq.Message}");
            var rep = new Reply(Serv.ServID, Serv.CurSeqNr, Serv.CurView, true, clireq.Message,DateTime.Now.ToString());
            rep = (Reply) Serv.SignMessage(rep, MessageType.Reply);
            await Serv.SendMessage(rep.SerializeToBuffer(), clireq.ClientID, MessageType.Reply);
            return rep;
        }

        //Function that performs all the operations in HandleRequest but without sending anything. Used for testing the operations performed in the function..
        public async CTask<Reply> HandleRequestTest(Request clireq)
        {
            byte[] digest;
            QCertificate qcertpre;
            digest = Crypto.CreateDigest(clireq);
            int curSeq; 
            
            //Prepare:
            if (Serv.IsPrimary()) //Primary
            {
                Console.WriteLine("Server is primary");
                curSeq = ++Serv.CurSeqNr; //single threaded and asynchronous, only a single HandleRequest has access to this variable at the time.
                var init = Serv.InitializeLog(curSeq);
                if (!init) return null; 
                PhaseMessage preprepare = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.PrePrepare);
                preprepare = (PhaseMessage) Serv.SignMessage(preprepare, MessageType.PhaseMessage);
                qcertpre = new QCertificate(preprepare.SeqNr, preprepare.ViewNr, CertType.Prepared, preprepare); //Log preprepare as Prepare
            }else{ //Replicas
                Console.WriteLine("Server is not primary");
                // await incomming PhaseMessages Where = MessageType.PrePrepare
                    var preprepared = await MesBridge
                        .Where(pm => pm.PhaseType == PMessageType.PrePrepare)
                        
                        .Where(pm => pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView, Serv.CurSeqRange)) //oversight, not the servers pubkey the message id's pubkey!!!
                        .Next();
                    
                    qcertpre = new QCertificate(preprepared.SeqNr, Serv.CurView, CertType.Prepared, preprepared); //note Serv.CurView == prepared.ViewNr which is checked in t.Validate //Add Prepare to Certificate
                    curSeq = qcertpre.SeqNr;
                    var init = Serv.InitializeLog(curSeq);
                    if (!init) return null;
                    PhaseMessage prepare = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.PrePrepare);
                    prepare = (PhaseMessage) Serv.SignMessage(prepare, MessageType.PhaseMessage);
                    qcertpre.ProofList.Add(prepare);
                    //MesBridge.Emit(prepare);
            }
            
            //Prepare phase
            //await incoming PhaseMessages Where = MessageType.Prepare Add to Certificate Until Consensus Reached
            Console.WriteLine("Waiting for Prepare messages");
            await MesBridge
                .Where(pm => pm.PhaseType == PMessageType.Prepare)
                .Where(pm => pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView, Serv.CurSeqRange, qcertpre))
                //.Do(pm => qcertpre.ProofList.Add(pm))
                .Scan(qcertpre.ProofList, (prooflist, message) =>
                {
                    prooflist.Add(message);
                    return prooflist;
                })
                .Where(pm => qcertpre.ValidateCertificate(FailureNr)) //probably won't work
                .Next();
            
            Serv.AddCertificate(qcertpre.SeqNr, qcertpre); //add first certificate to Log
            Console.WriteLine("Prepare phase finished");
            
            //Commit phase
            //Commit:
            PhaseMessage commitmes = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.Commit);
            commitmes = (PhaseMessage) Serv.SignMessage(commitmes, MessageType.PhaseMessage);
            QCertificate qcertcom = new QCertificate(qcertpre.SeqNr, Serv.CurView, CertType.Committed, commitmes);
            Console.WriteLine("Waiting for Commit messages");
            await MesBridge  //await incoming PhaseMessages Where = MessageType.Commit Until Consensus Reached
                .Where(pm => pm.PhaseType == PMessageType.Commit)
                .Where(pm => pm.Validate(Serv.ServPubKeyRegister[pm.ServID], Serv.CurView, Serv.CurSeqRange, qcertcom))
                //.Do(pm => qcertcom.ProofList.Add(pm))
                .Scan(qcertcom.ProofList, (prooflist, message) =>
                {
                    prooflist.Add(message);
                    return prooflist;
                })
                .Where(pm => qcertcom.ValidateCertificate(FailureNr))
                .Next();
            
            //Reply
            //Save the 2 Certificates
            Serv.AddCertificate(qcertcom.SeqNr, qcertcom);
            Console.WriteLine("Commit phase finished");
            //Need to move or insert await for curSeqNr to be the next request to be handled
            Console.WriteLine($"Completing operation: {clireq.Message}");
            var rep = new Reply(Serv.ServID, Serv.CurSeqNr, Serv.CurView, true, clireq.Message,DateTime.Now.ToString());
            rep = (Reply) Serv.SignMessage(rep, MessageType.Reply);
            return rep;
        }

        public async CTask HandlePrimaryChange()
        {
               
        }

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(Serv), Serv);
            stateToSerialize.Set(nameof(FailureNr), FailureNr);
            stateToSerialize.Set(nameof(MesBridge), MesBridge);
        }

        public static ProtocolExecution Deserialize(IReadOnlyDictionary<string, object> sd)
            => new ProtocolExecution(
                sd.Get<Server>(nameof(Serv)),
                sd.Get<int>(nameof(FailureNr)),
                sd.Get<Source<PhaseMessage>>(nameof(MesBridge))
                );
    }
}