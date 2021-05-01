using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;

namespace Cleipnir.Rx
{
    //Currently supported operations:
    /*
     * OfType (currently not working properly) - emit only those items from the stream that pass a predicate test
     * Do - register an action to take upon a variety of stream lifecycle events, you register callbacks that will call when certain events take place, called independently.
     * Pin - Root/Entangle the operator so that it won't be send to the garbage collector
     * Where/Filter - emit only those items from the stream that pass a predicate test, filters a stream by only allowing items through that pass a test that you specify in a form of a predicate function.
     * Map - transform the items emitted by the stream by applying a function to each item, applies a function of your choosing to each item emitted by the source stream, and returns a stream that emits the results of these functions applications.
     * Select - 
     * Scan - Apply a function to each item emitted by a stream, sequentially, and emit each successive value. Applies function to the first item emitted by the source and then emits the result of that function as its own first emission. It also feeds the result of the function back into the function along with the second item emitted by the source in order to generate its second emission.
     * Distinct By -  suppress duplicate items emitted by a stream. Filters the stream by only allowing items through that may have not already been emitted.
     * Max - emits the item from the source that had the maximum value. The Max operator operates on an Observable that emits numbers and emits a single item: the item with the largest number.
     * Min - emits the item from the source that had the minimum value. The Min operator operates on an Observable that emits numbers and emits a single item: the item with the smallest number.
     * MaxDateTime - Emits the datetime item from the source that has the latest time based value.
     * MinDateTime - Emits the datetime item from the source that has the earliest time based value.
     * Ephemeral - Not persisted objects are called ephermal objects. Whatever comes after this operator will not be persisted or serialized/deserialized in the system.
     * Dispose on - (based on its previous name UnsubscribeOn I assume this operator will unsubscribe from the stream if the condition given is met)
     */
    
    public static class StreamsLinq
    {
        internal class OfType<TFrom, TTo> : IPersistableOperator<TFrom, TTo>
        {
            public void Operator(TFrom next, Action<TTo> notify)
            {
                if (next.GetType().IsSubclassOf(typeof(TTo)))
                    notify((TTo) (object) next);
            }

            public void Serialize(StateMap sd, SerializationHelper helper) { }

            private static OfType<TFrom, TTo> Deserialize() => new OfType<TFrom, TTo>();
        }

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

        //** Select **/
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
        
        // ** DISTINCT BY ** //
        public static Stream<T> DistinctBy<T, TDistinctBy>(this Stream<T> s, Func<T, TDistinctBy> selector)
            => s.DecorateStream(new DistinctByOperator<T, TDistinctBy>(selector));
        
        private class DistinctByOperator<T, TDistinctBy> : IPersistableOperator<T, T>
        {
            private readonly Func<T, TDistinctBy> _selector;
            private readonly CSet<TDistinctBy> _set;

            private bool _isSerialized;

            public DistinctByOperator(Func<T, TDistinctBy> selector, CSet<TDistinctBy> set = null)
            {
                _selector = selector;
                _set = set ?? new CSet<TDistinctBy>();
            } 

            public void Operator(T next, Action<T> notify)
            {
                if (_set.Add(_selector(next)))
                    notify(next);
            }

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                if (_isSerialized) return; _isSerialized = true;
                
                sd.Set(nameof(_selector), _selector);
                sd.Set(nameof(_set), _set);
            }

            private static DistinctByOperator<T, TDistinctBy> Deserialize(IReadOnlyDictionary<string, object> sd)
                => new DistinctByOperator<T, TDistinctBy>(
                    sd.Get<Func<T, TDistinctBy>>(nameof(_selector)),
                    sd.Get<CSet<TDistinctBy>>(nameof(_set))
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

        /*Min*/
        public static Stream<int> Min(this Stream<int> s)
        => s.DecorateStream(new MinIntOperator(false, int.MaxValue));
        
        private class MinIntOperator : IPersistableOperator<int, int>
        {
            private bool _exist;
            private int _currentMin;

            public MinIntOperator(bool exist, int currentMin)
            {
                _exist = exist;
                _currentMin = currentMin;
            }

            public void Operator(int next, Action<int> notify)
            {
                if (!_exist)
                {
                    _exist = true;
                    _currentMin = next;
                    notify(next);
                }

                if (next >= _currentMin) return;
                _currentMin = next;
                notify(next);
            }

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(_exist), _exist);
                sd.Set(nameof(_currentMin), _currentMin);
            }

            private static MinIntOperator Deserialize(IReadOnlyDictionary<string, object> sd) 
                => new MinIntOperator(sd.Get<bool>(nameof(_exist)), 
                                  sd.Get<int>(nameof(_currentMin))
                );
        }

        public static Stream<DateTime> Min(this Stream<DateTime> s)
            => s.DecorateStream(new MinDateTimeOperator(false, DateTime.MaxValue));
        
        private class MinDateTimeOperator : IPersistableOperator<DateTime, DateTime> //in, out <-- stream
        {
            private bool _exist;
            private DateTime _currentMin;

            public MinDateTimeOperator(bool exist, DateTime currentMin)
            {
                _exist = exist;
                _currentMin = currentMin;
            }

            public void Operator(DateTime next, Action<DateTime> notify)
            {
                if (!_exist)
                {
                    _exist = true;
                    _currentMin = next;
                    notify(next);
                }

                if (next >= _currentMin) return;
                _currentMin = next;
                notify(next);
            }
            
            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(_exist), _exist);
                sd.Set(nameof(_currentMin), _currentMin);
            }

            private static MinDateTimeOperator Deserialize(IReadOnlyDictionary<string, object> sd) 
                => new MinDateTimeOperator(sd.Get<bool>(nameof(_exist)), 
                    sd.Get<DateTime>(nameof(_currentMin))
                );
        }
        
        /*Count*/
        public static Stream<int> Count<T>(this Stream<T> s)
            => s.DecorateStream(new CountOperator<T>());

        private class CountOperator<T> : IPersistableOperator<T, int>
        {
            private int _count = 0;
            
            public void Operator(T next, Action<int> notify) => notify(++_count);

            public void Serialize(StateMap sd, SerializationHelper helper) => sd.Set(nameof(_count), _count);

            private static CountOperator<T> Deserialize(IReadOnlyDictionary<string, object> sd)
                => new CountOperator<T>() {_count = sd.Get<int>(nameof(_count))};
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

            private static EphemeralOperator<T> Deserialize(IReadOnlyDictionary<string, object> sd)
            {
                var inner = sd.Get<Stream<T>>(nameof(_inner));
                var instance = new EphemeralOperator<T>(inner, false);
                //todo helper.DoPostInstanceCreation(() => inner.Unsubscribe(instance));

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
        
        // ** MERGE OPERATOR ** //
        public static Stream<T> Merge<T>(this Stream<T> s1, Stream<T> s2)
            => new MergeOperator<T>(s1, s2);

        private class MergeOperator<T> : Stream<T>
        {
            private readonly Stream<T> _inner1;
            private readonly Stream<T> _inner2;
            
            public MergeOperator(Stream<T> inner1, Stream<T> inner2)
            {
                _inner1 = inner1;
                _inner2 = inner2;
            }
            
            internal MergeOperator(Stream<T> inner1, Stream<T> inner2, IReadOnlyDictionary<string, object> sd) : base(sd)
            {
                _inner1 = inner1;
                _inner2 = inner2;
            }

            internal override void Subscribe(object subscriber, Action<T> onNext)
            {
                if (NumberOfObservers == 0)
                {
                    _inner1.Subscribe(this, Notify);
                    _inner2.Subscribe(this, Notify);
                }
                
                base.Subscribe(subscriber, onNext);
            }

            internal override void Unsubscribe(object subscriber)
            {
                base.Unsubscribe(subscriber);

                if (NumberOfObservers == 0)
                {
                    //_inner1.Unsubscribe(subscriber);
                    //_inner2.Unsubscribe(subscriber);
                    _inner1.Unsubscribe(this);
                    _inner2.Unsubscribe(this);
                }
            }           

            public override void Dispose()
            {
                _inner1.Unsubscribe(this);
                _inner2.Unsubscribe(this);
            }

            public override void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd[nameof(_inner1)] = _inner1;
                sd[nameof(_inner2)] = _inner2;
                
                base.Serialize(sd, helper);
            }

            private static MergeOperator<T> Deserialize(IReadOnlyDictionary<string, object> sd)
                => new MergeOperator<T>(
                    sd.Get<Stream<T>>(nameof(_inner1)),
                    sd.Get<Stream<T>>(nameof(_inner2)),
                    sd
                );
        }
        
        
        /*ALL operator does not work! issue in regards to generics, keeping track of previous events as well as not really doing what All is supposed to.*/
        public static Stream<bool> All<T,CList>(this Stream<T> s, Func<T, CList<T>, bool> criteria) => s.DecorateStream(new AllOperator<T,CList>(criteria));

        private class AllOperator<T,CList> : IPersistableOperator<T, bool>
        {
            private readonly CList<T> _events;

            private readonly Func<T, CList<T>, bool> _criteria;

            public AllOperator(Func<T, CList<T>, bool> crit)
            {
                _events = new CList<T>();
                _criteria = crit;   
            }
            
            public AllOperator(CList<T> events, Func<T, CList<T>, bool> crit)
            {
                _events = events;
                _criteria = crit;
            }
            public void Operator(T next, Action<bool> notify)
            {  
                _events.Add(next);
                notify(_criteria(next, _events));
            }

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(_events), _events);
                sd.Set(nameof(_criteria), _criteria); //Might present a security risk!!! Serializing delegates is a big nono, discuss with Thomas
            }

            private static AllOperator<T,CList> Deserialize(IReadOnlyDictionary<string, object> sd)
                => new AllOperator<T,CList>(
                        sd.Get<CList<T>>(nameof(_events)), 
                        sd.Get<Func<T, CList<T>, bool>>(nameof(_criteria))
                    );
        }
    }
}