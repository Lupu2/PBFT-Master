using System;
using System.IO;
using System.Security.Cryptography;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Replica;
using System.Threading;
using Cleipnir.StorageEngine.SimpleFile;
using Cleipnir.ExecutionEngine;
using PBFT.Network;
using Cleipnir.Helpers;
using Cleipnir.ObjectDB;

namespace PBFT
{
    class Program
    {
        static void Main(string[] args)
        {
            //var storageEngine = new SimpleFileStorageEngine(@"./PBFT.txt", false);
            //var scheduler = ExecutionEngineFactory.StartNew(storageEngine);
            
            //Initialize Server/Client

            //DEBUG/Testing
            Console.WriteLine("PBFT Testing");
            Request cliRequest = new Request(1, "Hello PBFT", DateTime.Now.ToString());
            Console.WriteLine(cliRequest.Timestamp);
            byte[] bufferrequest = Crypto.CreateDigest(cliRequest);
            string hash2 = BitConverter.ToString(bufferrequest);
            Console.WriteLine(hash2);
            
            Request cliRequest2 = new Request(0, "Hello ZAWARUDO",DateTime.Now.ToString());
            byte[] buff = cliRequest.SerializeToBuffer();
            Thread.Sleep(1000);
            Request copyRequest = Request.DeSerializeToObject(buff);
            
             RSA rsa = RSA.Create();
             var prikey = rsa.ExportParameters(true);
             var pubkey = rsa.ExportParameters(false);
             Console.WriteLine(cliRequest2.ToString());
             //byte [] sermes = cliRequest2.SerializeToBuffer();
             var digest2 = Crypto.CreateDigest(cliRequest2);
             cliRequest2.SignMessage(prikey);
             var sign = cliRequest2.Signature;
             sign = Deserializer.DeserializeHash(Serializer.SerializeHash(sign));
             byte [] seriacopy = cliRequest2.SerializeToBuffer();
             Console.WriteLine("Signature");
             Console.WriteLine(BitConverter.ToString(sign));
             byte[] sermes = cliRequest2.CreateCopyTemplate().SerializeToBuffer();
             sermes = Deserializer.DeserializeHash((Serializer.SerializeHash(sermes))); //testing serialize/deserialize for byte[] array for Cleipnir
             Console.WriteLine(cliRequest2.ToString());
             Console.WriteLine(Crypto.VerifySignature(sign, sermes, pubkey));
             
             // RSA rsa = RSA.Create();
             // var prikey = rsa.ExportParameters(true);
             // var pubkey = rsa.ExportParameters(false);
             // var sessionmes = new SessionMessage(DeviceType.Client,pubkey,1);
             // Console.WriteLine(BitConverter.ToString(pubkey.Exponent));
             // Console.WriteLine(pubkey.D);
             // var sermes = sessionmes.SerializeToBuffer();
             // var deser = SessionMessage.DeSerializeToObject(sermes);
             // Console.WriteLine(BitConverter.ToString(deser.publickey.Exponent));
             // Console.WriteLine(deser.publickey.P);
                
            //Actual initialization
            //Console.WriteLine(args[0]);
            // var storageEngine = new SimpleFileStorageEngine(".PBFTStorage.txt", true); //change to false when done debugging
            // Console.WriteLine(args.Length);
            //
            // var test = LoadJSONValues.GetServerData("serverInfo.json", 0).Result;
            // var id = test.Item1;
            // var ipaddr = test.Item2;
            // Console.WriteLine("Result");
            // Console.WriteLine(id);
            // Console.WriteLine(ipaddr);
            //
            // if (args.Length > 0) //add arguments by editing configuration program arguments or by adding paramters behind executable directly
            // {
            //     foreach (var arg in args)
            //     {
            //         Console.WriteLine(arg);
            //     }
            // }
            // var con = File.Exists("./PBFTStorage.txt");
            // Engine scheduler;
            // if (!con)
            // {
            //     scheduler = ExecutionEngineFactory.StartNew(storageEngine);    
            // }
            // else
            // {
            //     scheduler = ExecutionEngineFactory.Continue(storageEngine);
            // }
            // Server serv = new Server(id, 0, scheduler, 10);

        }
    }
}
