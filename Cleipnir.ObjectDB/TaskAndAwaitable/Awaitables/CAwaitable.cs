using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cleipnir.ObjectDB.Helpers.FunctionalTypes;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;

namespace Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables
{
    public class CAwaitable : IPersistable
    {
        private CAppendOnlyList<NotYetCompletedAwaiter> _awaiters = new CAppendOnlyList<NotYetCompletedAwaiter>(1);
        private List<NotYetCompletedAwaiter> _ephemeralAwaiters = new List<NotYetCompletedAwaiter>(1);
        private Option<Exception> _thrownException = Option.None<Exception>();

        public bool Completed => IsSuccessfullyCompleted || _thrownException.HasValue;
        public bool IsSuccessfullyCompleted { get; private set; } = false;
        public bool IsExceptionThrown => _thrownException.HasValue;

        public CAwaitable() {}

        private CAwaitable(CAppendOnlyList<NotYetCompletedAwaiter> awaiters, bool successfullyCompleted, Option<Exception> thrownException)
        {
            _awaiters = awaiters;
            _thrownException = thrownException;
            IsSuccessfullyCompleted = successfullyCompleted;
        }

        public void SignalCompletion()
        {
            if (Completed)
                throw new InvalidOperationException("Completion or Exception has already been set for the awaitable");

            IsSuccessfullyCompleted = true;

            foreach (var awaiter in _awaiters)
                awaiter.SignalCompletion();

            foreach (var awaiter in _ephemeralAwaiters)
                awaiter.SignalCompletion();

            _awaiters = null;
            _ephemeralAwaiters = null;
        }

        public void SignalThrownException(Exception e)
        {
            if (Completed)
                throw new InvalidOperationException("Completion or Exception has already been set for the awaitable");

            _thrownException = Option.Some(e);

            foreach (var awaiter in _awaiters)
                awaiter.SignalThrownException(e);

            foreach(var awaiter in _ephemeralAwaiters)
                awaiter.SignalCompletion();

            _awaiters = null;
            _ephemeralAwaiters = null;
        }

        public Awaiter GetAwaiter()
        {
            if (IsSuccessfullyCompleted)
                return new SuccessfullyCompletedAwaiter();
            if (_thrownException.HasValue)
                return new FaultyCompletedAwaiter(_thrownException.GetValue);

            var awaiter = new NotYetCompletedAwaiter();
            _awaiters.Add(awaiter);
            return awaiter;
        }

        public Awaiter GetEphemeralAwaiter()
        {
            if (IsSuccessfullyCompleted)
                return new SuccessfullyCompletedAwaiter();
            if (_thrownException.HasValue)
                return new FaultyCompletedAwaiter(_thrownException.GetValue);

            var awaiter = new NotYetCompletedAwaiter();
            _ephemeralAwaiters.Add(awaiter);
            return awaiter;
        }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            sd.Set("IsSuccessfullyCompleted", IsSuccessfullyCompleted);
            sd.Set("Exception", _thrownException.GetOr(default(Exception)));
            sd.Set(nameof(_awaiters), _awaiters);
        }

        internal static CAwaitable Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            var isSuccessfullyCompleted = (bool) sd["IsSuccessfullyCompleted"];
            var exception = sd["Exception"] == null 
                ? Option.None<Exception>() 
                : Option.Some((Exception) sd["Exception"]);
            var awaiters = sd.Get<CAppendOnlyList<NotYetCompletedAwaiter>>(nameof(_awaiters));

            return new CAwaitable(awaiters, isSuccessfullyCompleted, exception);
        }

        public abstract class Awaiter : INotifyCompletion, IPersistable
        {
            public abstract bool IsCompleted { get; }
            public abstract void GetResult();
            public abstract void OnCompleted(Action continuation);
            internal abstract Option<Exception> ThrownException { get; }

            public abstract void Serialize(StateMap stateToSerialize, SerializationHelper helper);
        }

        internal class SuccessfullyCompletedAwaiter : Awaiter
        {
            public override bool IsCompleted => true;

            internal override Option<Exception> ThrownException { get; } = new None<Exception>();

            public override void OnCompleted(Action continuation) => continuation();

            public override void GetResult() { }
            public override void Serialize(StateMap stateToSerialize, SerializationHelper helper) { }

            internal static SuccessfullyCompletedAwaiter Deserialize() => new SuccessfullyCompletedAwaiter();
        }

        internal class FaultyCompletedAwaiter : Awaiter
        {
            internal FaultyCompletedAwaiter(Exception thrownException)
            {
                _thrownException = thrownException;
                ThrownException = Option.Some(thrownException);
            }

            private readonly Exception _thrownException;
            public override bool IsCompleted => true;

            internal override Option<Exception> ThrownException { get; }

            public override void OnCompleted(Action continuation) => continuation();

            public override void GetResult() => throw _thrownException;

            public override void Serialize(StateMap sd, SerializationHelper helper)
                => sd.Set("Exception", _thrownException);

            internal static FaultyCompletedAwaiter Deserialize(IReadOnlyDictionary<string, object> stateMap) 
                => new FaultyCompletedAwaiter((Exception) stateMap["Exception"]);
        }

        public class NotYetCompletedAwaiter : Awaiter
        {
            public NotYetCompletedAwaiter() {}

            private NotYetCompletedAwaiter(
                Action continuation, 
                bool isCompletedSuccessfully,
                Option<Exception> thrownException)
            {
                _continuation = continuation;
                _isCompletedSuccessfully = isCompletedSuccessfully;
                _thrownException = thrownException;
            }

            private Action _continuation;
            private bool _isCompletedSuccessfully = false;
            private Option<Exception> _thrownException = Option.None<Exception>();

            internal override Option<Exception> ThrownException => _thrownException;

            public override bool IsCompleted => _isCompletedSuccessfully || _thrownException.HasValue;

            internal void SignalCompletion()
            {
                _isCompletedSuccessfully = true;

                if (_continuation == null) return;
                
                _continuation();

                _continuation = null;
            }

            internal void SignalThrownException(Exception e)
            {
                _thrownException = Option.Some(e);

                if (_continuation == null) return;

                _continuation();

                _continuation = null;
            }

            public override void OnCompleted(Action continuation)
            {
                if (IsCompleted)
                    continuation();
                else
                    _continuation = continuation;
            }

            public override void GetResult()
            {
                if (_isCompletedSuccessfully)
                    return;

                if (_thrownException.HasValue)
                    throw _thrownException.GetValue;

                throw new InvalidOperationException("No result has been set yet. Please only call after OnCompleted callback");
            }

            public override void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(_isCompletedSuccessfully), _isCompletedSuccessfully);
                sd.Set(nameof(_thrownException), _thrownException.GetOr(default(Exception)));
                sd.Set(nameof(_continuation), _continuation);
            }

            internal static NotYetCompletedAwaiter Deserialize(IReadOnlyDictionary<string, object> sd)
            {
                var isSuccessfullyCompleted = (bool) sd[nameof(_isCompletedSuccessfully)];
                var exception = (Exception) sd[nameof(_thrownException)];
                var thrownException = exception == null ? Option.None<Exception>() : Option.Some(exception);
                var continuation = (Action) sd[nameof(_continuation)];

                var instance = new NotYetCompletedAwaiter(
                    continuation,
                    isSuccessfullyCompleted,
                    thrownException
                );

                return instance;
            }
        }
    }

    public class CAwaitable<T> : IPersistable
    {
        private CAppendOnlyList<NotYetCompletedAwaiter> _awaiters = new CAppendOnlyList<NotYetCompletedAwaiter>(1);
        private List<NotYetCompletedAwaiter> _ephemeralAwaiters = new List<NotYetCompletedAwaiter>(1);
        public bool Completed => IsSuccessfullyCompleted || _thrownException.HasValue;
        public bool IsSuccessfullyCompleted { get; private set; } = false;
        public bool IsExceptionThrown => _thrownException.HasValue;

        private T _result;
        private Option<Exception> _thrownException = Option.None<Exception>();

        public CAwaitable() { } 

        private CAwaitable(
            bool isSuccessfullyCompleted, 
            Option<Exception> thrownException, 
            T result, 
            CAppendOnlyList<NotYetCompletedAwaiter> awaiters) 
        {
            IsSuccessfullyCompleted = isSuccessfullyCompleted;
            _thrownException = thrownException;
            _result = result;
            _awaiters = awaiters;
        }

        public void SignalCompletion(T result)
        {
            if (Completed)
            {
                //Console.WriteLine(_thrownException.GetValue);
                throw new InvalidOperationException("Completion or Exception has already been set for the awaitable");
            }

            _result = result;
            IsSuccessfullyCompleted = true;

            foreach (var awaiter in _awaiters)
                awaiter.SignalCompletion(result);

            foreach (var awaiter in _ephemeralAwaiters)
            {
                if (awaiter.IsCompleted)
                {
                    Console.WriteLine("ISCOMPLETED");
                }
                awaiter.SignalCompletion(result);
            }
                

            _awaiters = null;
            _ephemeralAwaiters = null;
        }

        public void SignalThrownException(Exception e)
        {
            if (Completed)
            {
                Console.WriteLine(_thrownException.GetValue);
                throw new InvalidOperationException("Completion or Exception has already been set for the awaitable");
            }

            _thrownException = Option.Some(e);

            foreach (var awaiter in _awaiters)
                awaiter.SignalThrownException(e);

            foreach (var awaiter in _ephemeralAwaiters)
                awaiter.SignalThrownException(e);

            _awaiters = null;
            _ephemeralAwaiters = null;
        }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            sd.Set("IsSuccessfullyCompleted", IsSuccessfullyCompleted);
            sd.Set("Exception", _thrownException.GetOr(default(Exception)));
            sd.Set("Result", IsSuccessfullyCompleted ? _result : default);

            sd.Set(nameof(_awaiters), _awaiters);
        }

        internal static CAwaitable<T> Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            var isSuccessfullyCompleted = (bool) sd["IsSuccessfullyCompleted"];
            var exception = sd["Exception"] == null
                ? Option.None<Exception>()
                : Option.Some((Exception)sd["Exception"]);
            var result = (T) sd["Result"];

            var awaiters = sd.Get<CAppendOnlyList<NotYetCompletedAwaiter>>(nameof(_awaiters));
            
            return new CAwaitable<T>(isSuccessfullyCompleted, exception, result, awaiters);
        }

        public Awaiter GetAwaiter()
        {
            if (IsSuccessfullyCompleted)
                return new SuccessfullyCompletedAwaiter(_result);

            if (_thrownException.HasValue)
                return new FaultyCompletedAwaiter(_thrownException.GetValue);

            var awaiter = new NotYetCompletedAwaiter();
            _awaiters.Add(awaiter);
            return awaiter;
        }

        public Awaiter GetEphemeralAwaiter()
        {
            if (IsSuccessfullyCompleted)
                return new SuccessfullyCompletedAwaiter(_result);

            if (_thrownException.HasValue)
                return new FaultyCompletedAwaiter(_thrownException.GetValue);

            var awaiter = new NotYetCompletedAwaiter();
            _ephemeralAwaiters.Add(awaiter);
            return awaiter;
        }

        public abstract class Awaiter : INotifyCompletion, IPersistable
        {
            public abstract bool IsCompleted { get; }

            public abstract T GetResult();
            public abstract void OnCompleted(Action continuation);
            internal abstract Option<Exception> ThrownException { get; }
            public abstract void Serialize(StateMap stateToSerialize, SerializationHelper helper);
        }

        internal class SuccessfullyCompletedAwaiter : Awaiter
        {
            internal SuccessfullyCompletedAwaiter(T result)
            {
                _result = result;
            }

            private readonly T _result;

            internal override Option<Exception> ThrownException { get; } = new None<Exception>();

            public override bool IsCompleted => true;

            public override void OnCompleted(Action continuation) => continuation();

            public override T GetResult() => _result;
            public override void Serialize(StateMap sd, SerializationHelper helper) 
                => sd.Set("Result", _result);

            internal static SuccessfullyCompletedAwaiter Deserialize(IReadOnlyDictionary<string, object> sd)
                => new SuccessfullyCompletedAwaiter((T) sd["Result"]);
        }

        internal class FaultyCompletedAwaiter : Awaiter
        {
            internal FaultyCompletedAwaiter(Exception thrownException)
            {
                ThrownException = Option.Some(thrownException);
            }

            internal override Option<Exception> ThrownException { get; }

            public override bool IsCompleted => true;

            public override void OnCompleted(Action continuation) => continuation();

            public override T GetResult() => throw ThrownException.GetValue;

            public override void Serialize(StateMap sd, SerializationHelper helper)
                => sd.Set("Exception", ThrownException.GetValue);

            internal static FaultyCompletedAwaiter Deserialize(IReadOnlyDictionary<string, object> sd) 
                => new FaultyCompletedAwaiter((Exception) sd["Exception"]);
        }

        internal class NotYetCompletedAwaiter : Awaiter
        {
            internal override Option<Exception> ThrownException => _thrownException;

            private Action _continuation;
            private bool _isSuccessfullyCompleted = false;
            private Option<Exception> _thrownException = Option.None<Exception>();
            private T _result;
            public NotYetCompletedAwaiter() { }

            internal NotYetCompletedAwaiter(T result, bool isSuccessfullyCompleted, Option<Exception> thrownException, Action continuation)
            {
                _thrownException = thrownException;
                _result = result;
                _continuation = continuation;
                _isSuccessfullyCompleted = isSuccessfullyCompleted;
            }

            public override bool IsCompleted => _isSuccessfullyCompleted || _thrownException.HasValue;

            internal void SignalCompletion(T result)
            {
                _isSuccessfullyCompleted = true;
                _result = result;

                if (_continuation == null) return;

                _continuation();

                _continuation = null;
            }

            internal void SignalThrownException(Exception e)
            {
                _thrownException = Option.Some(e);

                _continuation();

                _continuation = null;
            }

            public override void OnCompleted(Action continuation)
            {
                if (IsCompleted)
                    continuation();
                else
                    _continuation = continuation;
            }

            public override T GetResult()
            {
                if (_isSuccessfullyCompleted)
                    return _result;
                if (_thrownException.HasValue)
                    throw _thrownException.GetValue;

                throw new InvalidOperationException("No result has been set yet. Please only call after OnCompleted callback");
            }

            public override void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(_isSuccessfullyCompleted), _isSuccessfullyCompleted);
                sd.Set(nameof(_thrownException), _thrownException.GetOr(default(Exception)));
                sd.Set(nameof(_result), _result);
                sd.Set(nameof(_continuation), _continuation);
            }

            internal static NotYetCompletedAwaiter Deserialize(IReadOnlyDictionary<string, object> sd)
            {
                var isSuccessfullyCompleted = (bool) sd[nameof(_isSuccessfullyCompleted)];
                var exception = (Exception) sd[nameof(_thrownException)];
                var thrownException = exception == null ? Option.None<Exception>() : Option.Some(exception);
                var result = (T) sd[nameof(_result)];
                var continuation = (Action) sd[nameof(_continuation)];

                var instance = new NotYetCompletedAwaiter(result, isSuccessfullyCompleted, thrownException, continuation);

                return instance;
            }
        }
    }
}