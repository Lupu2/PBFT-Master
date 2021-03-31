using System.Linq;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Certificates;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Replica;

namespace PBFT.Tests.Replica.Protocol
{
    [TestClass]
    public class ViewChangeCertificateTests
    {
        [TestMethod]
        public void ViewChangeConstructorTest()
        {
            ViewPrimary vp = new ViewPrimary(4);
            var digestpseudostate = Crypto.CreateDigest(new Request(1, "Pseudo", "12:00"));
            var checkstate = new CheckpointCertificate(5, digestpseudostate, null);
            ViewChangeCertificate vc = new ViewChangeCertificate(vp, checkstate);
            Assert.IsFalse(vc.Valid);
            Assert.AreEqual(vc.ProofList.Count, 0);
            Assert.IsTrue(vc.CurSystemState.StateDigest.SequenceEqual(digestpseudostate));
            Assert.AreEqual(vc.ViewInfo.ViewNr, vp.ViewNr);
            Assert.AreEqual(vc.ViewInfo.ServID, vp.ServID);
        }

        [TestMethod]
        public void ViewChangeProofValidationTest()
        {
            var (pri, pub) = Crypto.InitializeKeyPairs();
            Request req = new Request(1, "Hello World", "12:00");
            req.SignMessage(pri);
            byte[] dig = req.SerializeToBuffer();
            var checkstate = new CheckpointCertificate(5, dig, null);
            checkstate.Stable = true;
            var vp = new ViewPrimary(4);
            vp.NextPrimary();
            var viewcert = new ViewChangeCertificate(vp, checkstate);
            var viewcert2 = new ViewChangeCertificate(vp, null);
            Assert.IsFalse(viewcert.ValidateCertificate(1));

            var vcmes1 = new ViewChange(5,1,1,checkstate,new CDictionary<int, ProtocolCertificate>());
            var vcmes2 = new ViewChange(5, 2, 1, checkstate, new CDictionary<int, ProtocolCertificate>());
            var vcmes3 = new ViewChange(5, 3, 1, checkstate, new CDictionary<int, ProtocolCertificate>());
            vcmes1.SignMessage(pri);
            vcmes2.SignMessage(pri);
            vcmes3.SignMessage(pri);
            viewcert.AppendViewChange(vcmes1, pub);
            viewcert.AppendViewChange(vcmes2, pub);
            viewcert.AppendViewChange(vcmes3, pub);
            Assert.IsTrue(viewcert.ValidateCertificate(1));
            viewcert.ResetCertificate();
            Assert.IsFalse(viewcert.Valid);
            Assert.AreEqual(viewcert.ProofList.Count, 0);
            Assert.IsFalse(viewcert.ValidateCertificate(1));

            var vcmesn1 = new ViewChange(5, 1, 1, null, new CDictionary<int, ProtocolCertificate>());
            var vcmesn2 = new ViewChange(5, 1, 1, null, new CDictionary<int, ProtocolCertificate>());
            var vcmesn3 = new ViewChange(5, 1, 1, null, new CDictionary<int, ProtocolCertificate>());
            vcmesn1.SignMessage(pri);
            vcmesn2.SignMessage(pri);
            vcmesn3.SignMessage(pri);
            viewcert2.AppendViewChange(vcmesn1, pub);
            viewcert2.AppendViewChange(vcmesn2, pub);
            viewcert2.AppendViewChange(vcmesn3, pub);
            Assert.IsTrue(viewcert2.ValidateCertificate(1));
            viewcert2.ResetCertificate();
            Assert.IsFalse(viewcert2.Valid);
            Assert.AreEqual(viewcert2.ProofList.Count, 0);
            Assert.IsFalse(viewcert2.ValidateCertificate(1));
            
            var vcmest1 = new ViewChange(5, 1, 1, checkstate, new CDictionary<int, ProtocolCertificate>());
            var vcmest2 = new ViewChange(5, 2, 1, checkstate, new CDictionary<int, ProtocolCertificate>());
            vcmest1.SignMessage(pri);
            vcmest2.SignMessage(pri);
            viewcert.AppendViewChange(vcmest1, pub);
            viewcert.AppendViewChange(vcmest2, pub);
            viewcert.AppendViewChange(vcmest1, pub);
            Assert.IsFalse(viewcert.ValidateCertificate(1)); //Duplicate issue test
            viewcert.AppendViewChange(vcmes3, pub);
            Assert.IsTrue(viewcert.ValidateCertificate(1));
            viewcert.ResetCertificate();
            Assert.IsFalse(viewcert.Valid);
            Assert.AreEqual(viewcert.ProofList.Count, 0);
            Assert.IsFalse(viewcert.ValidateCertificate(1));

            var vwviewnrmes = new ViewChange(5, 3, 2, checkstate, new CDictionary<int, ProtocolCertificate>());
            vwviewnrmes.SignMessage(pri);
            viewcert.AppendViewChange(vcmes1, pub);
            viewcert.AppendViewChange(vcmes2, pub);
            viewcert.AppendViewChange(vwviewnrmes, pub);
            Assert.IsFalse(viewcert.ValidateCertificate(1)); //wrong view nr
            viewcert.ResetCertificate();
            Assert.IsFalse(viewcert.Valid);
            Assert.AreEqual(viewcert.ProofList.Count, 0);
            Assert.IsFalse(viewcert.ValidateCertificate(1));

            var vwstatemes = new ViewChange(5, 3, 1, null, new CDictionary<int, ProtocolCertificate>());
            vwstatemes.SignMessage(pri);
            viewcert.AppendViewChange(vcmes1, pub);
            viewcert.AppendViewChange(vcmes2, pub);
            viewcert.AppendViewChange(vwstatemes, pub);
            Assert.IsFalse(viewcert.ValidateCertificate(1)); //wrong checkpoint state
            Assert.AreEqual(viewcert.ProofList.Count,3);
            viewcert.ResetCertificate();
            Assert.IsFalse(viewcert.Valid);
            Assert.AreEqual(viewcert.ProofList.Count, 0);
            Assert.IsFalse(viewcert.ValidateCertificate(1));
        }
    }
}