using System;
using System.Net;
using System.Text;
using System.Threading;

namespace Playground.SimpleNetwork
{
    public static class P
    {
        public static void Do()
        {
            var serverEndPoint = IPEndPoint.Parse("127.0.0.1:10000");
            var receiver = new MessageReceiver(serverEndPoint, MessageHandler);
            receiver.StartServing();
            
            new Thread(Sender) {IsBackground = true}.Start();
        }
        
        private static void MessageHandler(byte[] msg)
        {
            Console.WriteLine("RECEIVED: " + Encoding.UTF8.GetString(msg));
        }
        
        private static void Sender()
        {
            var sender = new MessageSender("127.0.0.1", 10_000);
            while (true)
            {
                var msg = Encoding.UTF8.GetBytes("Hello World!");
                sender.Send(msg);
                Thread.Sleep(1000);
            }
        }
    }
}