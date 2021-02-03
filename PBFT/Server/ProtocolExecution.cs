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

namespace PBFT.Server
{
    public class ProtocolExecution
    {
        public Server Serv {get; set;}

        public int NrOfNodes {get; set;}

        public CancellationTokenSource cancel = new CancellationTokenSource(); //Set timeout for async functions

        public ProtocolExecution(Server server, int nrnodes) 
        {
            Serv = server;
            NrOfNodes = nrnodes;
        }

        public async CTask<Reply> HandleRequest(Request clireq) 
        {
            byte[] digest;
            QCertificate qcertpre;
            digest = Crypto.CreateDigest(clireq);
            if (Serv.IsPrimary())
            {
                Serv.CurSeqNr++;
                qcertpre = new QCertificate(CertType.Prepared, Serv.CurSeqNr, Serv.CurView);
                PhaseMessage preprepare = new PhaseMessage(Serv.ServID, Serv.CurSeqNr, Serv.CurView, digest, PMessageType.PrePrepare);
                //Log preprepare as Prepare
                //Send async message PrePrepare
                await Serv.Multicast(preprepare.SerializeToBuffer());
                
            }else{
                try 
                {
                    // await incomming PhaseMessages Where = MessageType.PrePrepare
                //Add Prepare to Certificate
                //Send async message Prepare
                cancel.CancelAfter(60000); 
                PhaseMessage prepare = new PhaseMessage(Serv.ServID, Serv.CurSeqNr, Serv.CurView, digest, PMessageType.Prepare);
                await Serv.Multicast(prepare.SerializeToBuffer());    
                }
                catch(TaskCanceledException) //Probably placed so that the rest of the code is not runned after primary is deemed faulty
                {   
                    Console.WriteLine("Primary deemed faulty start sending new messages");
                    //ViewChange vc = new ViewChange(Serv.ServID, Serv.CurView);
                    //await Serv.Multicast(vc.SerializeToBuffer());
                }
            }
            //Prepare phase
            //await incomming PhaseMessages Where = MessageType.Prepare Add to Certificate Until Consensus Reached

            //Commit phase
            QCertificate qcertcom = new QCertificate(CertType.Committed, Serv.CurSeqNr, Serv.CurView);
            //Send async message Commit
            //await incomming PhaseMessages Where = MessageType.Commit Until Consensus Reached

            //Reply
            //Save the 2 Certificates
            Console.WriteLine($"Completing operation: {clireq.Message}");
            var rep = new Reply(Serv.ServID, Serv.CurSeqNr, Serv.CurView, true, clireq.Message,DateTime.Now.ToString());
            return rep;
        }
    }
}