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
        public static async CTask ProtocolTimeoutOperation(Source<bool> shutdown, int length, int id)
        {
            await Task.Delay(length);
            Console.WriteLine("Timeout occurred " +id);
            shutdown.Emit(false);
        }

        public static async Task AbortableProtocolTimeoutOperation(
            Source<bool> shutdown, 
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
                    shutdown.Emit(false);
                });
            }
            catch (TaskCanceledException te)
            {
                Console.WriteLine("Timeout cancelled!");
            }
        }

        public static async CTask AbortableProtocolTimeoutOperationCTask(
            Source<bool> shutdown,
            int length,
            CancellationToken cancel)
        {
            try
            {
                Console.WriteLine("Starting timeout with length: " + length);
                await Task.Delay(length, cancel);
                Console.WriteLine("Timeout occurred");
                shutdown.Emit(false);
            }
            catch (TaskCanceledException te)
            {
                Console.WriteLine("Timeout cancelled!");
            }
        }
    }
}