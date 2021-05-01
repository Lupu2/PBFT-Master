using System;
using System.Linq;
using Cleipnir.Rx;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ReactiveTests
{
    [TestClass]
    public class QueueTests
    {
        [TestMethod]
        public void AddToQueueBeforeDequeuing()
        {
            var source = new Source<int>();
            var queue = source.ToQueue();

            source.Emit(1);
            source.Emit(2);

            int[] emits = null;
            var awaiter = queue.Dequeue.GetAwaiter();
            awaiter.OnCompleted(() => emits = awaiter.GetResult().ToArray());

            emits.Length.ShouldBe(2);
            emits[0].ShouldBe(1);
            emits[1].ShouldBe(2);
        }

        [TestMethod]
        public void DequeueFromQueueBeforeEnqueueing()
        {
            var source = new Source<int>();
            var queue = source.ToQueue();

            int[] emits = null;
            var awaiter = queue.Dequeue.GetAwaiter();
            awaiter.OnCompleted(() => emits = awaiter.GetResult().ToArray());

            emits.ShouldBeNull();

            source.Emit(1);
            source.Emit(2);
            source.Emit(3);

            emits.Length.ShouldBe(1);
            emits[0].ShouldBe(1);

            awaiter = queue.Dequeue.GetAwaiter();
            awaiter.OnCompleted(() => emits = awaiter.GetResult().ToArray());

            emits.Length.ShouldBe(2);
            emits[0].ShouldBe(2);
            emits[1].ShouldBe(3);
        }

        [TestMethod]
        public void DisposedQueueShouldThrowException()
        {
            var source = new Source<int>();
            var queue = source.ToQueue();

            Exception thrownException = null;
            var awaiter = queue.Dequeue.GetAwaiter();
            awaiter.OnCompleted(() =>
            {
                try { awaiter.GetResult(); }
                catch (Exception e) { thrownException = e; }
            });

            queue.Dispose();
            thrownException.ShouldNotBeNull();
            (thrownException is ObjectDisposedException).ShouldBeTrue();

            thrownException = null;

            queue.Dispose();
            thrownException.ShouldBeNull();
        }
    }
}
