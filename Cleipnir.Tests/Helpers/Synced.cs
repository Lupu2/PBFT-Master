using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cleipnir.Tests.Helpers
{
    internal class Synced<T>
    {
        private readonly object _sync = new object();
        private T _value;
        
        public T Value
        {
            get
            {
                lock (_sync)
                    return _value;
            }
            set
            {
                lock (_sync)
                    _value = value;
            }
        }

        public T WaitFor(Func<T, bool> until)
        {
            while (true)
            {
                SpinWait.SpinUntil(() =>
                {
                    lock (_sync)
                        return until(_value);
                });

                lock (_sync)
                    if (until(_value))
                        return _value;
            }
        }
    }
}
