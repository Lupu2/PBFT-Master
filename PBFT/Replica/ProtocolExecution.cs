using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using PBFT.Helper;
using PBFT.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Cleipnir.Rx.ExecutionEngine;

namespace PBFT.Replica
{
    public class ProtocolExecution
    {
        public Server Serv {get; set;}

        public int NrOfNodes {get; set;}
        
        public int FailureNr { get; set; }
        
        private readonly object _sync = new object();

        private Source<PhaseMessage> MesBridge;

        //public CancellationTokenSource cancel = new CancellationTokenSource(); //Set timeout for async functions

        public ProtocolExecution(Server server, int nrnodes, int fnodes, Source<PhaseMessage> bridge) 
        {
            Serv = server;
            NrOfNodes = nrnodes;
            FailureNr = fnodes;
            MesBridge = bridge;
        }

        public async CTask<Reply> HandleRequest(Request clireq) 
        {
            byte[] digest;
            QCertificate qcertpre;
            digest = Crypto.CreateDigest(clireq);
            int curSeq = 0; //change later
            
            prepare:
            if (Serv.IsPrimary()) //Primary
            {
                lock (_sync)
                {
                    curSeq = Serv.CurSeqNr++; //<-causes problems for multiple request    
                }
                
                PhaseMessage preprepare = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.PrePrepare);
                qcertpre = new QCertificate(preprepare.SeqNr, preprepare.ViewNr, CertType.Prepared, preprepare); //Log preprepare as Prepare
                await Serv.Multicast(preprepare.SerializeToBuffer()); //Send async message PrePrepare
            }else{ //Replicas
                // await incomming PhaseMessages Where = MessageType.PrePrepare
                    var preprepared = await MesBridge
                        .Where(pm => pm.Type == PMessageType.PrePrepare)
                        .Where(t => t.Validate(Serv.Pubkey, Serv.CurView, Serv.CurSeqRange))
                        .Next();
                    
                    qcertpre = new QCertificate(preprepared.SeqNr, Serv.CurView, CertType.Prepared, preprepared); //note Serv.CurView == prepared.ViewNr which is checked in t.Validate //Add Prepare to Certificate
                    curSeq = qcertpre.SeqNr;
                    PhaseMessage prepare = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.Prepare); //Send async message Prepare
                    await Serv.Multicast(prepare.SerializeToBuffer());
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
                .Where(pm => pm.Validate(Serv.Pubkey, Serv.CurView, Serv.CurSeqRange))
                .Do(pm => qcertpre.ProofList.Add(pm))
                .Where(pm => qcertpre.ValidateCertificate(FailureNr)) //probably won't work
                .Next();
            Serv.AddCertificate(qcertpre.SeqNr, qcertpre); //add first certificate to Log
            //Commit phase
            Commit:
            QCertificate qcertcom = new QCertificate(qcertpre.SeqNr, Serv.CurView, CertType.Committed);
            PhaseMessage commitmes = new PhaseMessage(Serv.ServID, curSeq, Serv.CurView, digest, PMessageType.Commit);
            await Serv.Multicast(commitmes.SerializeToBuffer()); //Send async message Commit
            await MesBridge  //await incomming PhaseMessages Where = MessageType.Commit Until Consensus Reached
                .Where(pm => pm.Type == PMessageType.Commit)
                .Where(t => t.Validate(Serv.Pubkey, Serv.CurView, Serv.CurSeqRange))
                .Do(pm => qcertcom.ProofList.Add(pm))
                .Where(pm => qcertcom.ValidateCertificate(FailureNr))
                .Next();
            
            //Reply
            //Save the 2 Certificates
            Serv.AddCertificate(qcertcom.SeqNr, qcertcom);
            //Need to move or insert await for curSeqNr to be the next request to be handled
            Console.WriteLine($"Completing operation: {clireq.Message}");
            var rep = new Reply(Serv.ServID, Serv.CurSeqNr, Serv.CurView, true, clireq.Message,DateTime.Now.ToString());
            await Serv.SendMessage(rep.SerializeToBuffer());
            return rep;
        }

        public async CTask HandlePrimaryChange()
        {
            
        }
    }
}