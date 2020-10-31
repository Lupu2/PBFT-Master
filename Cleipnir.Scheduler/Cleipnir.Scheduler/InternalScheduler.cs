using System;
using System.Threading;
using Cleipnir.ExecutionEngine.DataStructures;
using Cleipnir.ObjectDB;

namespace Cleipnir.ExecutionEngine
{
    internal interface IScheduler : IDisposable
    {
        void Start();
        void Stop();
        void Schedule(Action toExecute, bool persistent);
        void FireAndForget(Action toExecute);
    }

    internal class InternalScheduler : IScheduler
    {
        private readonly object _sync = new object();
        private readonly ReadyToSchedules _readyToSchedules;

        private Thread _schedulingThread = null;

        private volatile bool _running = false;
        private bool _disposed = false;

        private readonly ObjectStore _objectStore;

        private readonly SynchronizationQueue _synchronizationQueue;
        private readonly Engine _engine;

        internal InternalScheduler(
            ObjectStore objectStore,
            ReadyToSchedules readyToSchedules,
            SynchronizationQueue synchronizationQueue, 
            Engine engine)
        {
            _objectStore = objectStore;

            _readyToSchedules = readyToSchedules;
            
            _synchronizationQueue = synchronizationQueue;
            _engine = engine;
        }

        public void Start()
        {
            lock (_sync)
            {
                if (_disposed)
                    throw new ObjectDisposedException("Scheduler has already been disposed");

                if (!(_schedulingThread == null || _schedulingThread.ThreadState == ThreadState.Stopped))
                    return;

                _running = true;

                _schedulingThread = new Thread(Execute) { Name = "Cleipnir_Scheduler" };

                _schedulingThread.Start();
            }
        }

        public void Stop()
        {
            Thread thread;
            lock (_sync)
            {
                if (!_running)
                    return;

                _running = false;
                thread = _schedulingThread;
            }

            thread.Join();
        }

        public void Schedule(Action toExecute, bool persistent)
        {
            lock (_sync)
                _readyToSchedules.Enqueue(toExecute, persistent);
        }

        public void FireAndForget(Action toExecute)
            => Schedule(toExecute, false);

        private void Execute()
        {
            SetAmbientContext();

            var toExecutes = new CArray<CAction>();
            
            while (_running)
            {
                lock (_sync)
                    _readyToSchedules.MoveAllTo(toExecutes);

                if (toExecutes.Count == 0)
                {
                    if (_synchronizationQueue.Empty) continue;
                    lock (_sync)
                    {
                        _synchronizationQueue.MoveAllTo(_readyToSchedules);

                        _objectStore.Persist();
                    }
                }
                else
                {
                    foreach (var toExecute in toExecutes)
                        try { toExecute.Action(); } catch { }
                    
                    toExecutes.Clear();
                }
            }
        }

        private void SetAmbientContext()
        {
            Scheduler.ThreadLocalScheduler.Value = this;
            Roots.Instance.Value = _objectStore.Roots;
            Sync.SynchronizationQueue.Value = _synchronizationQueue;
            Engine._current.Value = _engine;
        }

        public void Dispose()
        {
            lock (_sync)
                if (_disposed) return;
                else _disposed = true;

            Stop();

            _objectStore.Persist();
        }
    }
}
