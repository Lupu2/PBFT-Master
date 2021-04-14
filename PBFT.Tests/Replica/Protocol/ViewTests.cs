using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Certificates;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Replica;

namespace PBFT.Tests.Replica.Protocol
{
    [TestClass]
    public class ViewTests
    {
        //TODO fix this test with the updated RedoMessages that actually works
        /*[TestMethod]
        public void RedoMessageLeaderTest()
        {
            var storageEngine = new InMemoryStorageEngine();
            var execEngine = ExecutionEngineFactory.StartNew(storageEngine);
            var req1 = new Request(1, "Hello", "12:00");
            var req2 = new Request(2, "Dad", "12:01");
            var req3 = new Request(1, "Mom", "12:01");
            var req4 = new Request(2, "Lets", "12:02");
            var req5 = new Request(1, "Go", "12:05");
            var requestList = new List<Request>() {req1, req2, req3, req4, req5};

            var prepre1 = new PhaseMessage(1, 0, 1, Crypto.CreateDigest(req1), PMessageType.PrePrepare);
            var prepre2 = new PhaseMessage(1, 1, 1, Crypto.CreateDigest(req2), PMessageType.PrePrepare);
            var prepre3 = new PhaseMessage(1, 2, 1, Crypto.CreateDigest(req3), PMessageType.PrePrepare);
            var prepre4 = new PhaseMessage(1, 3, 1, Crypto.CreateDigest(req4), PMessageType.PrePrepare);
            var prepre5 = new PhaseMessage(1, 4, 1, Crypto.CreateDigest(req5), PMessageType.PrePrepare);

            CList<PhaseMessage> preprelist = new CList<PhaseMessage>();
            preprelist.Add(prepre1);
            preprelist.Add(prepre2);
            preprelist.Add(prepre3);
            preprelist.Add(prepre4);
            preprelist.Add(prepre5);
            var spm = new Source<PhaseMessage>();
            var redistpm = new Source<PhaseMessage>();
            var sh = new SourceHandler(null, spm, null, null, null, redistpm,null);
            var serv = new Server(1, 1, 4, null, 1, "127.0.0.1:9001", sh, new CDictionary<int, string>());
            serv.SignMessage(prepre1, MessageType.PhaseMessage);
            serv.SignMessage(prepre2, MessageType.PhaseMessage);
            serv.SignMessage(prepre3, MessageType.PhaseMessage);
            serv.SignMessage(prepre4, MessageType.PhaseMessage);
            serv.SignMessage(prepre5, MessageType.PhaseMessage);

            //Server server, int fnodes, Source<PhaseMessage> mesbridge, Source<PhaseMessage> remesbridge, Source<PhaseMessage> shutdownphase, Source<bool> viewchangebridge, Source<NewView> newviewbridge, Source<bool> shutbridge
            var protexec = new ProtocolExecution(serv, 1, sh.ProtocolSubject, redistpm, null, null, sh.NewViewSubject, sh.ShutdownSubject);
            protexec.Active = false;
            //protexec.RedoMessage(preprelist);

            var (pri1, pub1) = Crypto.InitializeKeyPairs();
            var (pri2, pub2) = Crypto.InitializeKeyPairs();
            serv.ServPubKeyRegister[2] = pub1;
            serv.ServPubKeyRegister[3] = pub2;
            serv.ProtocolActive = false;
            
            var execution = PerformTest(serv, preprelist).GetAwaiter();
            for (int i = 0; i < preprelist.Count; i++)
            {
                var prepare1 = new PhaseMessage(2, i, 1, Crypto.CreateDigest(requestList[i]),
                    PMessageType.Prepare);
                prepare1.SignMessage(pri1);
                var prepare2 = new PhaseMessage(3, i, 1, Crypto.CreateDigest(requestList[i]),
                    PMessageType.Prepare);
                prepare2.SignMessage(pri2);
                var com1 = new PhaseMessage(2, i, 1, Crypto.CreateDigest(requestList[i]), PMessageType.Commit);
                com1.SignMessage(pri1);
                var com2 = new PhaseMessage(3, i, 1, Crypto.CreateDigest(requestList[i]), PMessageType.Commit);
                com2.SignMessage(pri2);

                redistpm.Emit(prepare1);
                redistpm.Emit(prepare2);
                redistpm.Emit(com1);
                redistpm.Emit(com2);
            }

            bool success = execution.GetResult();
            Assert.IsTrue(success);
            Assert.IsTrue(prepre1.Compare(serv.GetProtocolCertificate(0)[0].ProofList[0]));
            Assert.IsTrue(prepre2.Compare(serv.GetProtocolCertificate(1)[0].ProofList[0]));
            Assert.IsTrue(prepre3.Compare(serv.GetProtocolCertificate(2)[0].ProofList[0]));
            Assert.IsTrue(prepre4.Compare(serv.GetProtocolCertificate(3)[0].ProofList[0]));
            Assert.IsTrue(prepre5.Compare(serv.GetProtocolCertificate(4)[0].ProofList[0]));
            Assert.IsTrue(Crypto.CreateDigest(req1).SequenceEqual(serv.GetProtocolCertificate(0)[0].CurReqDigest));
            Assert.IsTrue(Crypto.CreateDigest(req1).SequenceEqual(serv.GetProtocolCertificate(0)[1].CurReqDigest));
            for (int j = 0; j < preprelist.Count; j++)
            {
                for (int k = 0; k < 2; k++)
                {
                    Console.WriteLine(j);
                    Console.WriteLine(k);
                    Console.WriteLine(serv.GetProtocolCertificate(j)[k].IsValid);
                    Assert.IsTrue(serv.GetProtocolCertificate(j)[k].IsValid);
                }

            }
        }

        public async Task<bool> PerformTest(Server serv, CList<PhaseMessage> preprelist)
        {
            await RedoMessageTest(preprelist, serv, serv.Subjects.ProtocolSubject, 1);
            bool test = true;
            Console.WriteLine("Verify Log Changes...");
            for (int i = 0; i < preprelist.Count; i++)
            {
                var certList = serv.GetProtocolCertificate(i);
                if (certList.Count < 2) test = false;
            }

            return test;
        }

       public async CTask RedoMessageTest(CList<PhaseMessage> oldpreList, Server Serv, Source<PhaseMessage> ReMesBridge, int FailureNr)
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
                //Serv.Multicast(prepare.SerializeToBuffer(), MessageType.PhaseMessage);
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
            //Serv.Multicast(commes.SerializeToBuffer(), MessageType.PhaseMessage);
            Serv.EmitRedistPhaseMessageLocally(commes);
            
            await coms;
            Console.WriteLine("Commit certificate: " + comcert.SeqNr + " is finished");
            Serv.AddProtocolCertificate(prepre.SeqNr, comcert);
        }
    }*/
    }
}