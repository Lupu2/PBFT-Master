using System;
using System.Security.Cryptography;
using Cleipnir.ObjectDB.Persistency.Serialization.Helpers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.Rx;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Replica;

namespace PBFT.Tests
{
    [TestClass]
    public class ProtocolTests
    {
        [TestMethod]
        public void ProtocolExecutionPrimaryTestNoPersistency()
        {
            //Messages initialized
            RSAParameters _prikey;
            RSAParameters Pubkey;
            using (RSA rsa = RSA.Create())
            {
                _prikey = rsa.ExportParameters(true);
                Pubkey = rsa.ExportParameters(false);
            }

            Request clirequest = new Request(0,"Hello World",DateTime.Now.ToString());
            byte[] clidigest = Crypto.CreateDigest(clirequest);
            PhaseMessage prepare1 = new PhaseMessage(2, 1, 1, clidigest, PMessageType.Prepare);
            prepare1.SignMessage(_prikey);
            
            
            //Bridges initialized
            Source<Request> reqbridge = new Source<Request>();
        }
    }
}