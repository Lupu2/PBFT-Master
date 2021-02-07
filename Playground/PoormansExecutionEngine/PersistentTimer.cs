using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Playground.PoormansExecutionEngine
{
    public class PersistentTimer : IPersistable
    {
        private readonly DateTime _expiration;
        private readonly Action _action;
        private bool _hasBeenExecuted;
        private readonly PoormansExecutionEngine _executionEngine;

        public PersistentTimer(DateTime expiration, Action action, bool hasBeenExecuted, PoormansExecutionEngine executionEngine)
        {
            _expiration = expiration;
            _action = action;
            _hasBeenExecuted = hasBeenExecuted;
            _executionEngine = executionEngine;

            if (!_hasBeenExecuted)
                executionEngine.Schedule(Start);
        }

        private void Start()
        {
            _executionEngine.Entangle(this);
            var now = DateTime.Now;
            var delay = _expiration - now;

            if (delay.Ticks < 0)
                delay = TimeSpan.Zero;
            
            Task.Delay(delay).ContinueWith(_ =>
            {
                _executionEngine.Schedule(() =>
                {
                    _action();
                    _hasBeenExecuted = true;
                    _executionEngine.Untangle(this);
                }); 
            });
        }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            sd.Set(nameof(_expiration), _expiration);
            sd.Set(nameof(_action), _action);
            sd.Set(nameof(_hasBeenExecuted), _hasBeenExecuted);
        }

        private static PersistentTimer Deserialize(IReadOnlyDictionary<string, object> sd, PoormansExecutionEngine executionEngine)
        {
            var expiration = sd.Get<DateTime>(nameof(_expiration));
            var action = sd.Get<Action>(nameof(_action));
            var hasBeenExecuted = sd.Get<bool>(nameof(_hasBeenExecuted));
            
            return new PersistentTimer(expiration, action, hasBeenExecuted, executionEngine);
        }
    }
}