using System.Security.Cryptography;
using System;
using PBFT.Messages;

namespace PBFT.Helper
{
    public static class Crypto
    {
        
        /*//Most likely not useful later, delete pls...
        public static string CreateDigestBuffer(string buffer)
        {
            using( var shaalgo = SHA256.Create()){
                byte[] bytemes = Encoding.ASCII.GetBytes(buffer);
                var hash = shaalgo.ComputeHash(bytemes);
                return BitConverter.ToString(hash);
            }
        }*/

        public static (RSAParameters,RSAParameters) InitializeKeyPairs()
        {
            RSA rsa = RSA.Create();
            var prikey = rsa.ExportParameters(true);
            var pubkey = rsa.ExportParameters(false);
            return(prikey, pubkey);
        }

        public static byte[] CreateDigest(Request clientRequest)
        {
            using (var shaalgo = SHA256.Create()) //using= Dispose when finished with package 
            {
                var serareq = clientRequest.SerializeToBuffer();
                return shaalgo.ComputeHash(serareq);
            }
        }

        public static bool VerifySignature(byte[] signature, byte[] mesdig, RSAParameters pubkey, string hashpro="SHA256")
        {
            using(RSA rsa = RSA.Create())
            {
                byte[] hashmes;
                using(SHA256 sha = SHA256.Create())
                {
                    hashmes = sha.ComputeHash(mesdig);
                    Console.WriteLine("Hash2");
                    Console.WriteLine(BitConverter.ToString(hashmes));
                }
                rsa.ImportParameters(pubkey);
                RSAPKCS1SignatureDeformatter RSADeformatter = new RSAPKCS1SignatureDeformatter();
                RSADeformatter.SetHashAlgorithm(hashpro);
                RSADeformatter.SetKey(rsa);
                return RSADeformatter.VerifySignature(hashmes, signature);  
            }    
        }
    }
}