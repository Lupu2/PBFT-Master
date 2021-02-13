using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using PBFT.Messages;

namespace PBFT.Client
{
    public class Client
    {
        
        public int ClientID { get; }
        private RSAParameters _prikey{ get; } //Keep private key secret, can't leak info about: p,q & d
        public RSAParameters Pubkey { get; } //Contains only info for Exponent e & Modulus n

        public Dictionary<int, string> FinishedRequest;
        
        public List<ServerInfo> ServerInformation;
        //add more fields later
        
        public Client(int id)
        {
            ClientID = id;
            using (RSA rsa = RSA.Create())
            {
                _prikey = rsa.ExportParameters(true);
                Pubkey = rsa.ExportParameters(false);
            }

            FinishedRequest = new Dictionary<int, string>();
        }
        
        public Request CreateRequest(string mes)
        {
            Request req = new Request(ClientID, mes, DateTime.Now.ToString()); //G, or empty
            req.SignMessage(_prikey);
            return req;
        }

        public List<string> CreateOperations()
        {
            List<string> operations = new List<string>();
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

        public async Task RunCommands(List<string> ops)
        {
            foreach (string op in ops)
            {
                Request req = CreateRequest(op);
                await SendRequest(req);
            }
        }

        public async Task SendRequest(Request req)
        {
            foreach (var servinfo in ServerInformation)
            {
                //todo add network send functionality call here for each server
            }
        }

        public async Task ListenForResponse()
        {
            while (true)
            {
                
            }
        }
    }
}