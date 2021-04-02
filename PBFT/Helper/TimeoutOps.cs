using System;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using PBFT.Certificates;

namespace PBFT.Helper
{
    public static class TimeoutOps
    {
        //Client
        public static async Task<bool> TimeoutOperation(int length)
        {
            await Task.Delay(length);
            return false;
        } 
        
        //Server
        public static async CTask ProtocolTimeoutOperation(Source<ViewChangeCertificate> shutdown, int length)
        {
            await Task.Delay(length);
            Console.WriteLine("Timeout occurred");
            shutdown.Emit(null);
        }

        public static async CTask AbortableProtocolTimeoutOperation(
            Source<ViewChangeCertificate> shutdown, 
            int length,
            CancellationToken cancel
            )
        {
            try
            {
                await Task.Delay(length, cancel);
                Console.WriteLine("Timeout occurred");
                shutdown.Emit(null);
            }
            catch (TaskCanceledException te)
            {
                Console.WriteLine("Timeout cancelled!");
            }
        }
    }
}