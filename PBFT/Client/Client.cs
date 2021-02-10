using System;
using System.Security.Cryptography;
using System.Threading;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using PBFT.Messages;

namespace PBFT.Client
{
    public class Client
    {
        
        public int ClientID {get; set;}
        private RSAParameters _prikey{get; set;} //Keep private key secret, can't leak info about: p,q & d
        public RSAParameters Pubkey {get; set;} //Contains only info for Exponent e & Modulus n

        public CDictionary<int, string> FinishedRequest;
        
        public CList<ServerInfo> ServerInformation;
        //add more fields later
        
        public Client(int id)
        {
            ClientID = id;
            using (RSA rsa = RSA.Create())
            {
                _prikey = rsa.ExportParameters(true);
                Pubkey = rsa.ExportParameters(false);
            }

            FinishedRequest = new CDictionary<int, string>();
        }

        public Client(int id, CDictionary<int,string> fin)
        {
            id = id;
            fin = fin;
            
        }

        public Request CreateRequest(string mes)
        {
            Request req = new Request(ClientID, mes, DateTime.Now.ToString());
            req.SignMessage(_prikey);
            return req;
        }

        public CList<string> CreateOperations()
        {
            CList<string> operations = new CList<string>();
            bool done = false;
            ConsoleKey resp = ConsoleKey.Clear;
            while (!done)
            {
                Console.WriteLine("Write operation:");
                string op = Console.ReadLine();
                if (String.IsNullOrEmpty(op)) continue;
                operations.Add(op);
                Console.WriteLine("Done creating operations?[y/n]"); //https://stackoverflow.com/questions/37359161/how-would-i-make-a-yes-no-prompt-in-console-using-c
                bool conf = false;
                while (!conf)
                {
                    confirmation: 
                    resp = Console.ReadKey(false).Key;
                    if (resp != ConsoleKey.Y && resp != ConsoleKey.N) goto confirmation;
                    conf = true;
                }

                if (resp == ConsoleKey.Y) done = true;
            }
            return operations;
        }

        public async CTask RunCommands(CList<string> ops)
        {
            foreach (string op in ops)
            {
                Request req = CreateRequest(op);
                await SendRequest(req);
                
            }
        }

        public async CTask SendRequest(Request req)
        {
            foreach (var servinfo in ServerInformation)
            {
                //todo add network send functionality call here for each server
            }
        }

        public async CTask ListenForResponse()
        {
            while (true)
            {
                
            }
        }
    }
}