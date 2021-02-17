using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using PBFT.Helper;
using PBFT.Messages;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Cleipnir.Rx.ExecutionEngine;

namespace PBFT.Replica
{
    public class ProtocolExecution
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
                curSeq = Serv.CurSeqNr++; //single threaded and asynchronous, only a single HandleRequest has access to this variable at the time.
                var init = Serv.InitializeLog(curSeq);
                if (!init) return null; 
                PhaseMessage preprepare = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.PrePrepare);
                qcertpre = new QCertificate(preprepare.SeqNr, preprepare.ViewNr, CertType.Prepared, preprepare); //Log preprepare as Prepare
                await Serv.Multicast(preprepare.SerializeToBuffer(), MessageType.PhaseMessage); //Send async message PrePrepare
            }else{ //Replicas
                // await incomming PhaseMessages Where = MessageType.PrePrepare
                    var preprepared = await MesBridge
                        .Where(pm => pm.Type == PMessageType.PrePrepare)
                        
                        .Where(t => t.Validate(Serv.Pubkey, Serv.CurView, Serv.CurSeqRange))
                        .Next();
                    
                    qcertpre = new QCertificate(preprepared.SeqNr, Serv.CurView, CertType.Prepared, preprepared); //note Serv.CurView == prepared.ViewNr which is checked in t.Validate //Add Prepare to Certificate
                    curSeq = qcertpre.SeqNr;
                    var init = Serv.InitializeLog(curSeq);
                    if (!init) return null;
                    PhaseMessage prepare = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.Prepare); //Send async message Prepare
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
                .Where(pm => pm.Type == PMessageType.Prepare)
                .Where(pm => pm.Validate(Serv.Pubkey, Serv.CurView, Serv.CurSeqRange, qcertpre))
                //.Do(pm => qcertpre.ProofList.Add(pm))
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
            
            await Serv.Multicast(commitmes.SerializeToBuffer(), MessageType.PhaseMessage); //Send async message Commit
            await MesBridge  //await incoming PhaseMessages Where = MessageType.Commit Until Consensus Reached
                .Where(pm => pm.Type == PMessageType.Commit)
                .Where(t => t.Validate(Serv.Pubkey, Serv.CurView, Serv.CurSeqRange, qcertcom))
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
            //Need to move or insert await for curSeqNr to be the next request to be handled
            Console.WriteLine($"Completing operation: {clireq.Message}");
            var rep = new Reply(Serv.ServID, Serv.CurSeqNr, Serv.CurView, true, clireq.Message,DateTime.Now.ToString());
            await Serv.SendMessage(rep.SerializeToBuffer(), clireq.ClientID, MessageType.Reply);
            return rep;
        }

        public async CTask HandlePrimaryChange()
        {
            
        }
    }
}