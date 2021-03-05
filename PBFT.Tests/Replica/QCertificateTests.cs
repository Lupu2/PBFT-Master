using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Certificates;

namespace PBFT.Tests.Replica
{
    [TestClass]
    public class QCertificateTests
    {
        [TestMethod]
        public void ConstructorTests()
        {
            ProtocolCertificate qcertp = new ProtocolCertificate(1, 1, CertType.Prepared);
            PhaseMessage pc = new PhaseMessage(1, 2, 1, null, PMessageType.Commit);
            ProtocolCertificate qcertc = new ProtocolCertificate(2, 2, CertType.Committed, pc);
            //Tests for first certificate
            Assert.IsTrue(qcertp.ProofList.Count == 0);
            Assert.AreEqual(qcertp.SeqNr, 1);
            Assert.AreNotEqual(qcertp.SeqNr, 2);
            Assert.AreEqual(qcertp.ViewNr, 1);
            Assert.AreEqual(qcertp.CType, CertType.Prepared);
            Assert.AreNotEqual(qcertp.CType, CertType.Committed);
            
            //Tests for second certificate
            Assert.IsTrue(qcertc.ProofList.Count == 1);
            Assert.AreEqual(qcertc.SeqNr,2);
            Assert.AreNotEqual(qcertc.SeqNr,1);
            Assert.AreEqual(qcertc.ViewNr, 2);
            Assert.AreEqual(qcertc.CType, CertType.Committed);
            Assert.AreNotEqual(qcertc.CType, CertType.Prepared);
            Assert.AreEqual(qcertc.ProofList[0], pc);
        }
        
        [TestMethod]
        public void ValidateCertificateTest()
        {
            var (pri, pub) = Crypto.InitializeKeyPairs();
            Request req = new Request(1, "Hello World", DateTime.Now.ToString());
            req.SignMessage(pri);
            byte[] dig = req.SerializeToBuffer();
            ProtocolCertificate cert1 = new ProtocolCertificate(1, 1, CertType.Prepared); ;
            Assert.IsFalse(cert1.ValidateCertificate(1));
            //Correct Certification test
            PhaseMessage p1 = new PhaseMessage(1, 1, 1, dig, PMessageType.PrePrepare);
            p1.SignMessage(pri);
            PhaseMessage p2 = new PhaseMessage(2, 1, 1, dig, PMessageType.PrePrepare);
            p2.SignMessage(pri);
            PhaseMessage p3 = new PhaseMessage(3, 1, 1, dig, PMessageType.PrePrepare);
            p3.SignMessage(pri);
            PhaseMessage p4 = new PhaseMessage(4, 1, 1, dig, PMessageType.PrePrepare);
            p4.SignMessage(pri);
            cert1.ProofList.Add(p1);
            cert1.ProofList.Add(p2);
            cert1.ProofList.Add(p3);
            cert1.ProofList.Add(p4);
            Assert.IsTrue(cert1.ValidateCertificate(1));
            
            //Not valid Certification tests
            ProtocolCertificate cert2 = new ProtocolCertificate(2, 2, CertType.Committed);
            PhaseMessage p21 = new PhaseMessage(1, 2, 2, null, PMessageType.Commit);
            PhaseMessage p22 = new PhaseMessage(2, 2, 2, dig, PMessageType.Commit); //correct
            p22.SignMessage(pri);
            PhaseMessage p23 = new PhaseMessage(3, 2, 2, dig, PMessageType.Prepare);
            p23.SignMessage(pri);
            PhaseMessage p24 = new PhaseMessage(4, 3, 2, dig, PMessageType.Commit);
            p24.SignMessage(pri);
            PhaseMessage p25 = new PhaseMessage(2, 2, 2, dig, PMessageType.Commit);
            p25.SignMessage(pri);
            PhaseMessage p26 = new PhaseMessage(6, 2, 1, dig, PMessageType.Commit); 
            p26.SignMessage(pri);
            PhaseMessage p27 = new PhaseMessage(7, 2, 2, dig, PMessageType.Commit); //correct
            p27.SignMessage(pri);
            PhaseMessage p28 = new PhaseMessage(8, 2, 2, dig, PMessageType.Commit); //correct
            p28.SignMessage(pri);
            PhaseMessage p29 = new PhaseMessage(9, 2, 2, dig, PMessageType.Commit); //correct
            p29.SignMessage(pri);
            
            cert2.ProofList.Add(p21);
            cert2.ProofList.Add(p29);
            cert2.ProofList.Add(p27);
            cert2.ProofList.Add(p28);
            Assert.IsFalse(cert2.ValidateCertificate(1)); //Null digest & signature
            cert2.ResetCertificate();
            Assert.IsTrue(cert2.ProofList.Count == 0);
            
            cert2.ProofList.Add(p22);
            cert2.ProofList.Add(p23);
            cert2.ProofList.Add(p27);
            cert2.ProofList.Add(p28);
            Assert.IsFalse(cert2.ValidateCertificate(1)); //Different PhaseMessageType
            cert2.ResetCertificate();

            cert2.ProofList.Add(p22);
            cert2.ProofList.Add(p27);
            cert2.ProofList.Add(p24);
            cert2.ProofList.Add(p28);
            Assert.IsFalse(cert2.ValidateCertificate(1)); //Wrong viewNr
            cert2.ResetCertificate();
            
            cert2.ProofList.Add(p25);
            cert2.ProofList.Add(p29);
            cert2.ProofList.Add(p26);
            cert2.ProofList.Add(p28);
            Assert.IsFalse(cert2.ValidateCertificate(1)); //Wrong seqnr
            cert2.ResetCertificate();
            
            cert2.ProofList.Add(p22);
            cert2.ProofList.Add(p22);
            cert2.ProofList.Add(p29);
            Assert.IsFalse(cert2.ValidateCertificate(1)); //Duplicates
            cert2.ProofList.Add(p27);
            Assert.IsTrue(cert2.ValidateCertificate(1));
            
        }
    }
}