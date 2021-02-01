using System;
using System.Security.Cryptography;
using System.Threading;
using Cleipnir.ObjectDB.PersistentDataStructures;
using PBFT.Messages;

namespace PBFT.Client
{
    public class Client
    {
        public int ClientID {get; set;}
        private RSAParameters _prikey{get; set;} //Keep private key secret, can't leak info about: p,q & d
        public RSAParameters Pubkey {get; set;} //Contains only info for Exponent e & Modulus n

        //add more fields later


        public Client(int id)
        {
            ClientID = id;
            using (RSA rsa = RSA.Create())
            {
                _prikey = rsa.ExportParameters(true);
                Pubkey = rsa.ExportParameters(false);
            }
        }

        public Request CreateRequest(string mes)
        {
            Request req = new Request(ClientID, mes, DateTime.Now.ToString());
            req.SignMessage(_prikey);
            return req;
        }
    }
}