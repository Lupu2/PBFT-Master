using System;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Providers;
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

        public static async Task AbortableProtocolTimeoutOperation(
            Source<ViewChangeCertificate> shutdown, 
            int length,
            CancellationToken cancel,
            Engine scheduler
            )
        {
            try
            {
                Console.WriteLine("Starting timeout with length: " + length);
                await Task.Delay(length, cancel);
                Console.WriteLine("Timeout occurred");
                await scheduler.Schedule(() =>
                {
                    shutdown.Emit(null);
                });

            }
            catch (TaskCanceledException te)
            {
                Console.WriteLine("Timeout cancelled!");
            }
        }
    }
}