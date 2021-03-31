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
        public static async CTask TimeoutOperation(Source<ViewChangeCertificate> shutdown, int length)
        {
            await Task.Delay(length);
            Console.WriteLine("Timeout occurred");
            shutdown.Emit(null);
        }

        public static async CTask AbortableTimeoutOperation(Source<ViewChangeCertificate> shutdown, int length,
            CancellationToken cancel)
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