using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using PBFT.Certificates;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Replica;

namespace PBFT.Tests.Replica.Protocol
{
    public class ViewTests
    {
    /*[TestClass]
    public class ViewTests
    {
        [TestMethod]
        public void RedoMessageLeaderTest()
        {
            var storageEngine = new InMemoryStorageEngine();
            var execEngine = ExecutionEngineFactory.StartNew(storageEngine);
            var req1 = new Request(1, "Hello", "12:00");
            var req2 = new Request(2, "Dad", "12:01");
            var req3 = new Request(1, "Mom", "12:01");
            var req4 = new Request(2, "Lets", "12:02");
            var req5 = new Request(1, "Go", "12:05");
            var requestList = new List<Request>(){req1, req2, req3, req4, req5};
            
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
                var sh = new SourceHandler(null, spm, null, null, null, null);
                var serv = new Server(1, 1, 4, null, 1, "127.0.0.1:9001", sh, new CDictionary<int, string>());
                serv.SignMessage(prepre1, MessageType.PhaseMessage);
                serv.SignMessage(prepre2, MessageType.PhaseMessage);
                serv.SignMessage(prepre3, MessageType.PhaseMessage);
                serv.SignMessage(prepre4, MessageType.PhaseMessage);
                serv.SignMessage(prepre5, MessageType.PhaseMessage);

                var protexec = new ProtocolExecution(serv, 1, sh.ProtocolSubject, sh.NewViewSubject, sh.ShutdownSubject);
                protexec.RedoMessage(preprelist);

                var (pri1, pub1) = Crypto.InitializeKeyPairs();
                var (pri2, pub2) = Crypto.InitializeKeyPairs();
                serv.ServPubKeyRegister[2] = pub1;
                serv.ServPubKeyRegister[3] = pub2;
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

                    spm.Emit(prepare1);
                    spm.Emit(prepare2);
                    spm.Emit(com1);
                    spm.Emit(com2);
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
            await RedoMessageTest(preprelist, serv, serv.Subjects.ProtocolSubject,1);
            bool test = true;
            Console.WriteLine("Verify Log Changes...");
            for (int i = 0; i < preprelist.Count; i++)
            {
                var certList = serv.GetProtocolCertificate(i);
                if (certList.Count < 2) test = false;
            }
            return test;
        }*/
        
        public async CTask RedoMessageTest(CList<PhaseMessage> oldpreList, Server Serv, Source<PhaseMessage> MesBridge, int failNum)
        {
            Console.WriteLine(oldpreList.Count);
            foreach (var prepre in oldpreList)
            {
                Serv.InitializeLog(prepre.SeqNr);
                var precert = new ProtocolCertificate(prepre.SeqNr, prepre.ViewNr, prepre.Digest, CertType.Prepared, prepre); //need a way to know request digest and request message
                var comcert = new ProtocolCertificate(prepre.SeqNr, prepre.ViewNr, prepre.Digest, CertType.Committed);
                if (!Serv.IsPrimary())
                {
                    var prepare = new PhaseMessage(Serv.ServID, prepre.SeqNr, prepre.ViewNr, prepre.Digest, PMessageType.Prepare);
                    Serv.SignMessage(prepare, MessageType.PhaseMessage);
                    //await Serv.Multicast(prepare.SerializeToBuffer(), MessageType.PhaseMessage);
                }
                
                var preps = MesBridge
                    .Where(pm => pm.PhaseType == PMessageType.Prepare)
                    .Where(pm => pm.ValidateRedo(Serv.ServPubKeyRegister[pm.ServID], prepre.ViewNr))
                    .Scan(precert.ProofList, (prooflist, message) =>
                    {
                        prooflist.Add(message);
                        return prooflist;
                    })
                    .Where(_ => precert.ValidateCertificate(failNum))
                    .Next();
                var coms = MesBridge
                    .Where(pm => pm.PhaseType == PMessageType.Commit)
                    .Where(pm => pm.ValidateRedo(Serv.ServPubKeyRegister[pm.ServID], prepre.ViewNr))
                    .Scan(comcert.ProofList, (prooflist, message) =>
                    {
                        prooflist.Add(message);
                        return prooflist;
                    })
                    .Where(_ => comcert.ValidateCertificate(failNum))
                    .Next();
                await preps;
                Serv.AddProtocolCertificate(prepre.SeqNr, precert);

                var commes = new PhaseMessage(Serv.ServID, prepre.SeqNr, prepre.ViewNr, prepre.Digest, PMessageType.Commit);
                Serv.SignMessage(commes, MessageType.PhaseMessage);
                //await Serv.Multicast(commes.SerializeToBuffer(), MessageType.PhaseMessage);
                Serv.EmitPhaseMessageLocally(commes);
                await coms;
                Serv.AddProtocolCertificate(prepre.SeqNr, comcert);
            }
            Console.WriteLine("Finished Redoing missing certificates!");
        }
    }
}