﻿using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;

namespace Cleipnir.Rx
{
    public static class StreamsLinq
    {
        // ** DO ** //
        public static Stream<T> Do<T>(this Stream<T> s, Action<T> toDo)
            => s.DecorateStream(new DoOperator<T>(toDo));

        private class DoOperator<T> : IPersistableOperator<T, T>
        {
            private readonly Action<T> _toDo;
            private bool _serialized;

            public DoOperator(Action<T> toDo)
            {
                _toDo = toDo;
            }

            public void Operator(T next, Action<T> notify)
            {
                _toDo(next);
                notify(next);
            }

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                if (_serialized) return;
                _serialized = true;

                sd.Set(nameof(_toDo), _toDo);
            }

            private static DoOperator<T> Deserialize(IReadOnlyDictionary<string, object> sd)
                => new DoOperator<T>(sd.Get<Action<T>>(nameof(_toDo))) { _serialized = true };
        }

        // ** PIN ** //
        public static Stream<T> Pin<T>(this Stream<T> s) => new PinnedOperator<T>(s);

        // ** WHERE ** //
        public static Stream<T> Where<T>(this Stream<T> s, Func<T, bool> predicate)
            => s.DecorateStream<T>(new WhereOperator<T>(predicate));

        private class WhereOperator<T> : IPersistableOperator<T, T>
        {
            private readonly Func<T, bool> _predicate;
            private bool _serialized = false;

            public WhereOperator(Func<T, bool> predicate) => _predicate = predicate;

            public void Operator(T next, Action<T> notify)
            {
                if (_predicate(next))
                    notify(next);
            }

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                if (_serialized) return;
                _serialized = true;

                sd.Set(nameof(_predicate), _predicate);
            }

            private static WhereOperator<T> Deserialize(IReadOnlyDictionary<string, object> sd)
                => new WhereOperator<T>(sd.Get<Func<T, bool>>(nameof(_predicate)));
        }

        // ** MAP ** //
        public static Stream<TOut> Map<TIn, TOut>(this Stream<TIn> s, Func<TIn, TOut> mapper)
            => s.DecorateStream(new MapOperator<TIn, TOut>(mapper));

        private class MapOperator<TIn, TOut> : IPersistableOperator<TIn, TOut>
        {
            private readonly Func<TIn, TOut> _mapper;

            public MapOperator(Func<TIn, TOut> mapper) => _mapper = mapper;

            public void Operator(TIn next, Action<TOut> notify)
                => notify(_mapper(next));

            public void Serialize(StateMap sd, SerializationHelper helper)
                => sd.Set(nameof(_mapper), _mapper);

            private static MapOperator<TIn, TOut> Deserialize(IReadOnlyDictionary<string, object> sd)
                => new MapOperator<TIn, TOut>(sd.Get<Func<TIn, TOut>>(nameof(_mapper)));
        }

        public static Stream<TOut> Select<TIn, TOut>(this Stream<TIn> s, Func<TIn, TOut> mapper) => Map(s, mapper);

        // ** SCAN ** //
        public static Stream<TOut> Scan<TIn, TOut>(this Stream<TIn> s, TOut seed, Func<TOut, TIn, TOut> folder)
            => s.DecorateStream(new ScanOperator<TIn, TOut>(seed, folder));

        private class ScanOperator<TIn, TOut> : IPersistableOperator<TIn, TOut>
        {
            private TOut _curr;
            private Func<TOut, TIn, TOut> _folder;

            public ScanOperator(TOut seed, Func<TOut, TIn, TOut> folder)
            {
                _curr = seed;
                _folder = folder;
            }

            public void Operator(TIn next, Action<TOut> notify)
                => notify(_curr = _folder(_curr, next));

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(_curr), _curr);
                sd.Set(nameof(_folder), _folder);
            }

            private static ScanOperator<TIn, TOut> Deserialize(IReadOnlyDictionary<string, object> sd)
                => new ScanOperator<TIn, TOut>(
                    sd.Get<TOut>(nameof(_curr)),
                    sd.Get<Func<TOut, TIn, TOut>>(nameof(_folder))
                );
        }

        // ** MAX ** //
        public static Stream<int> Max(this Stream<int> s)
            => s.DecorateStream(new MaxIntOperator(false, int.MinValue));

        private class MaxIntOperator : IPersistableOperator<int, int>
        {
            private bool _any;
            private int _currentMax;

            public MaxIntOperator(bool any, int currentMax)
            {
                _any = any;
                _currentMax = currentMax;
            }

            public void Operator(int next, Action<int> notify)
            {
                if (!_any)
                {
                    _any = true;
                    _currentMax = next;
                    notify(next);
                }

                if (next <= _currentMax)
                    return;

                _currentMax = next;
                notify(next);
            }

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(_any), _any);
                sd.Set(nameof(_currentMax), _currentMax);
            }

            private static MaxIntOperator Deserialize(IReadOnlyDictionary<string, object> sd)
                => new MaxIntOperator(
                    sd.Get<bool>(nameof(_any)),
                    sd.Get<int>(nameof(_currentMax))
                );
        }

        // ** MAX ** //
        public static Stream<DateTime> Max(this Stream<DateTime> s)
            => s.DecorateStream(new MaxDateTimeOperator(false, DateTime.MinValue));

        private class MaxDateTimeOperator : IPersistableOperator<DateTime, DateTime>
        {
            private bool _any;
            private DateTime _currentMax;

            public MaxDateTimeOperator(bool any, DateTime currentMax)
            {
                _any = any;
                _currentMax = currentMax;
            }

            public void Operator(DateTime next, Action<DateTime> notify)
            {
                if (!_any)
                {
                    _any = true;
                    _currentMax = next;
                    notify(next);
                }

                if (next <= _currentMax)
                    return;

                _currentMax = next;
                notify(next);
            }

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(_any), _any);
                sd.Set(nameof(_currentMax), _currentMax);
            }

            private static MaxDateTimeOperator Deserialize(IReadOnlyDictionary<string, object> sd)
                => new MaxDateTimeOperator(
                    sd.Get<bool>(nameof(_any)),
                    sd.Get<DateTime>(nameof(_currentMax))
                );
        }


        

        // ** EPHEMERAL OPERATOR ** //
        public static Stream<T> Ephemeral<T>(this Stream<T> s) => new EphemeralOperator<T>(s, true);

        private class EphemeralOperator<T> : Stream<T>
        {
            private Stream<T> _inner;

            public EphemeralOperator(Stream<T> inner, bool subscribe)
            {
                _inner = inner;
                if (subscribe)
                    _inner.Subscribe(this, Notify);
            } 

            public override void Serialize(StateMap sd, SerializationHelper helper) 
                => sd.Set(nameof(_inner), _inner);

            private static EphemeralOperator<T> Deserialize(IReadOnlyDictionary<string, object> sd, DeserializationHelper helper)
            {
                var inner = sd.Get<Stream<T>>(nameof(_inner));
                var instance = new EphemeralOperator<T>(inner, false);
                helper.DoPostInstanceCreation(() => inner.Unsubscribe(instance));

                return instance;
            }

            public override void Dispose() => _inner?.Unsubscribe(this);
        }

       

        // ** DISPOSE ON ** //
        public static Stream<T> DisposeOn<T>(this Stream<T> s, CAwaitable disposeOn)
            => new DisposeOnOperator<T>(s, disposeOn);

        public static Stream<TStream> DisposeOn<TStream, TDisposeOn>(this Stream<TStream> s, CAwaitable<TDisposeOn> disposeOn)
            => new DisposeOnOperator<TStream, TDisposeOn>(s, disposeOn);

        private class DisposeOnOperator<T> : Stream<T>
        {
            private readonly Stream<T> _inner;

            private bool _serialized;

            public DisposeOnOperator(Stream<T> inner, CAwaitable awaitable)
            {
                _inner = inner;

                inner.Subscribe(this, Notify);
                awaitable.GetAwaiter().OnCompleted(Dispose);
            }

            private DisposeOnOperator(Stream<T> inner, IReadOnlyDictionary<string, object> sd) : base(sd)
            {
                _inner = inner;
            }

            public override void Dispose() => _inner.Unsubscribe(this);

            private static DisposeOnOperator<T> Deserialize(IReadOnlyDictionary<string, object> sd)
            {
                return new DisposeOnOperator<T>(
                    sd.Get<Stream<T>>(nameof(_inner)), 
                    sd
                    ) {_serialized = true};
            }

            public override void Serialize(StateMap sd, SerializationHelper helper)
            {
                base.Serialize(sd, helper);
                if (_serialized) return; _serialized = true;

                sd.Set(nameof(_inner), _inner);
            }
        }

        private class DisposeOnOperator<TStream, TDisposeOn> : Stream<TStream>
        {
            private readonly Stream<TStream> _inner;

            private bool _serialized;

            public DisposeOnOperator(Stream<TStream> inner, CAwaitable<TDisposeOn> awaitable)
            {
                _inner = inner;

                inner.Subscribe(this, Notify);
                awaitable.GetAwaiter().OnCompleted(Dispose);
            }

            private DisposeOnOperator(Stream<TStream> inner, IReadOnlyDictionary<string, object> sd) : base(sd)
            {
                _inner = inner;
            }

            public override void Dispose() => _inner.Unsubscribe(this);

            private static DisposeOnOperator<TStream, TDisposeOn> Deserialize(IReadOnlyDictionary<string, object> sd)
            {
                return new DisposeOnOperator<TStream, TDisposeOn>(
                        sd.Get<Stream<TStream>>(nameof(_inner)),
                        sd
                    )
                    { _serialized = true };
            }

            public override void Serialize(StateMap sd, SerializationHelper helper)
            {
                base.Serialize(sd, helper);
                if (_serialized) return; _serialized = true;

                sd.Set(nameof(_inner), _inner);
            }
        }
    }
}