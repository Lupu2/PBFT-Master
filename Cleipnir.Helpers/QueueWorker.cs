using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cleipnir.Helpers
{
    public class QueueWorker
    {
        private readonly Queue<Func<Task>> _workQueue1 = new Queue<Func<Task>>();
        private readonly Queue<Func<Task>> _workQueue2 = new Queue<Func<Task>>();

        private Queue<Func<Task>> _workQueue;

        private readonly object _sync = new object();
        private bool _working = false;

        public QueueWorker() => _workQueue = _workQueue1;

        public void Do(Action work) => Do(() => { work(); return Task.CompletedTask; });

        public void Do(Func<Task> work)
        {
            lock (_sync)
            {
                _workQueue.Enqueue(work);
                if (_working) return;

                _working = true;
                Task.Run(WorkLoop);
            }
        }

        private async Task WorkLoop()
        {
            while (true)
            {
                Queue<Func<Task>> currQueue;
                
                lock (_sync)
                {
                    if (_workQueue.Count == 0)
                    {
                        _working = false;
                        return;
                    }

                    currQueue = _workQueue;

                    //swap the work queues
                    _workQueue = _workQueue == _workQueue1 
                        ? _workQueue2 
                        : _workQueue1;
                }

                while (currQueue.Count > 0)
                    await currQueue.Dequeue().Invoke();
            }
        }
    }
}
