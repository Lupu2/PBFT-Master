using System;
using System.Threading.Tasks;

namespace Cleipnir.Helpers
{
    public class TaskCompletionSource
    {
        private readonly TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();

        public Task Task => _tcs.Task;

        public void SignalCompletion() => _tcs.SetResult(false);
        public void SetException(Exception e) => _tcs.SetException(e);
    }
}
