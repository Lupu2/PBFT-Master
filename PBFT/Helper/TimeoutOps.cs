using System;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;

namespace PBFT.Helper
{
    public static class TimeoutOps
    {
        //Client
        //TimeoutOperation simply creates a timeout operation for the given length of time in milliseconds.
        public static async Task<bool> TimeoutOperation(int length)
        {
            await Task.Delay(length);
            return false;
        } 
        
        //Server
        //ProtocolTimeoutOperation simply creates a timeout operation for the given length of time in milliseconds.
        //Supposed to be only used together with CTask asynchronous operations.
        public static async CTask ProtocolTimeoutOperation(Source<bool> shutdown, int length, int id)
        {
            await Sleep.Until(length);
            Console.WriteLine("Timeout occurred " +id);
            shutdown.Emit(false);
        }

        //AbortableProtocolTimeoutOperation creates an interruptible timeout operation for the given length of time in milliseconds.
        //If the timeout occurs, then a signal is emitted to the given shutdown Source<bool> object.
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

        //AbortableProtocolTimeoutOperationCTask creates an interruptible timeout operation for the given length of time in milliseconds.
        //If the timeout occurs, then a signal is emitted to the given shutdown Source<bool> object.
        //Supposed to be only used together with CTask asynchronous operations.
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