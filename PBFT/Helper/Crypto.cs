using System.Security.Cryptography;
using System;
using System.Linq;
using System.Text;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Newtonsoft.Json;
using PBFT.Messages;

namespace PBFT.Helper
{
    public static class Crypto
    {
        //InitializeKeyPairs initializes a RSA to create and return a private, public key key pair.
        public static (RSAParameters, RSAParameters) InitializeKeyPairs()
        {
            RSA rsa = RSA.Create();
            var prikey = rsa.ExportParameters(true);
            var pubkey = rsa.ExportParameters(false);
            byte[] test = prikey.D;
            //testing conversion
            string test4 = Convert.ToBase64String(test);
            byte[] test5 = Convert.FromBase64String(test4);
            Console.WriteLine(test.SequenceEqual(test5));
            
            return(prikey, pubkey);
        }

        //CreateDigest creates a digest for the given Request object.
        public static byte[] CreateDigest(Request clientRequest)
        {
            using (var shaalgo = SHA256.Create()) //using: Dispose when finished with package 
            {
                var serareq = clientRequest.SerializeToBuffer();
                return shaalgo.ComputeHash(serareq);
            }
        }
        
        //MakeStateDigest creates a digest of the given app state.
        public static byte[] MakeStateDigest(CList<string> appstate)
        {
            Console.WriteLine("AppState");
            foreach (string state in appstate)
                Console.WriteLine(state);
            var seriastate = JsonConvert.SerializeObject((appstate));
            var bytesstate = Encoding.ASCII.GetBytes(seriastate);
            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(bytesstate);
            }
        }
        
        //VerifySignature verifies the signature for the given mesdigest and public key is correct.
        public static bool VerifySignature(byte[] signature, byte[] mesdig, RSAParameters pubkey, string hashpro="SHA256")
        { //Original Source: https://docs.microsoft.com/en-us/dotnet/standard/security/cryptographic-signatures
            Console.WriteLine("Verifying Signature");
            if (signature == null) return false;
            using(RSA rsa = RSA.Create())
            {
                byte[] hashmes;
                using(SHA256 sha = SHA256.Create())
                {
                    hashmes = sha.ComputeHash(mesdig);

                }
                rsa.ImportParameters(pubkey);
                RSAPKCS1SignatureDeformatter RSADeformatter = new RSAPKCS1SignatureDeformatter();
                RSADeformatter.SetHashAlgorithm(hashpro);
                RSADeformatter.SetKey(rsa);
                var sign = RSADeformatter.VerifySignature(hashmes, signature);
                Console.WriteLine("Verify signature result: " + sign);
                return sign;
            }
        }
    }
}