using System;
using System.Linq;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Cleipnir.Rx.ExecutionEngine;
using Cleipnir.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ReactiveTests
{
    [TestClass]
    public class WaitForOperatorTests
    {
        [TestMethod]
        public void WaitForOperatorEmitsCountNumberOfElementsNonScheduled()
        {
            var source = new Source<int>();
            var waitedFor = source.WaitFor(5, false);
            int[] emitted = null;

            async void Do() => emitted = (await waitedFor).ToArray();
            
            Do();

            for (var i = 0; i < 4; i++)
            {
                source.Emit(i);
                emitted.ShouldBeNull();
            }

            source.Emit(4);

            emitted.Length.ShouldBe(5);
            for (var i = 0; i < 5; i++)
                emitted[i].ShouldBe(i);
        }

        [TestMethod]
        public void WaitForOperatorThrowsTimeoutExceptionAfterTimeOut()
        {
            var scheduler = new TestFacade();

            var synced = new Synced<Exception>();

            async CTask Do()
            {
                var source = new Source<int>();
                var waitedFor = source.WaitFor(5, true, TimeSpan.FromMilliseconds(500));
                try
                {
                    await waitedFor;
                }
                catch (Exception e)
                {
                    synced.Value = e;
                }
            }

            synced.Value.ShouldBeNull();

            scheduler.Schedule(() => _ = Do());

            var exception = synced.WaitFor(e => e != null);
            (exception is TimeoutException).ShouldBeTrue();

            scheduler.Dispose();
        }

        [TestMethod]
        public void WaitForOperatorEmitsElementsBeforeTimeOut()
        {
            var testFacade = new TestFacade();

            var synced = new Synced<Tuple<Exception, int[]>>();

            async CTask Do()
            {
                var source = new Source<int>();
                var waitedFor = source.WaitFor(5, true, TimeSpan.FromMilliseconds(500));

                for (var i = 0; i < 5; i++)
                {
                    await Sleep.Until(10, false);
                    source.Emit(i);
                }

                try
                {
                    var elements = await waitedFor;
                    synced.Value = Tuple.Create(default(Exception), elements.ToArray());
                }
                catch (Exception e)
                {
                    synced.Value = Tuple.Create(e, default(int[]));
                }
            }

            synced.Value.ShouldBeNull();

            testFacade.Schedule(() => _ = Do());

            var (exception, elements) = synced.WaitFor(e => e != null);
            exception.ShouldBeNull();
            elements.Length.ShouldBe(5);
            for (var i = 0; i < 5; i++)
                elements[i].ShouldBe(i);
        }
    }
}
