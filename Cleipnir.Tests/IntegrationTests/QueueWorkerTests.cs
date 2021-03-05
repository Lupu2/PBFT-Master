using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.Helpers;
using Cleipnir.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.IntegrationTests
{
    [TestClass]
    public class QueueWorkerTests
    {
        [TestMethod]
        public void EnqueuedWorkIsExecuted()
        {
            var value = new Synced<int>();

            var queueWorker = new QueueWorker();
            queueWorker.Do(() => value.Value = 10);

            value.WaitFor(i => i == 10);
        }

        [TestMethod]
        public void WorkQueueIsSwappedCorrectlyOnLongRunningWork()
        {
            var value1 = new Synced<bool>();
            var value2 = new Synced<bool>();

            var queueWorker = new QueueWorker();
            queueWorker.Do(() =>
            {
                Thread.Sleep(200);
                value1.Value = true;
            });

            Thread.Sleep(100);

            queueWorker.Do(() =>
            {
                Thread.Sleep(200);
                value2.Value = true;
            });

            value2.WaitFor(b => b);
            value1.Value.ShouldBe(true);
            
        }

        [TestMethod]
        public void WorkQueueIsSwappedCorrectlyOnLongRunningWorkAsync()
        {
            var value1 = new Synced<bool>();
            var value2 = new Synced<bool>();

            var queueWorker = new QueueWorker();
            queueWorker.Do(async () =>
            {
                await Task.Delay(500);
                value1.Value = true;
            });

            queueWorker.Do( () =>
            {
                value2.Value = true;
                return Task.CompletedTask;
            });

            value2.WaitFor(b => b);
            value1.Value.ShouldBe(true);
        }
    }
}
