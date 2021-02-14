using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Helpers;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.Persistency.Persistency;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine
{
    public class CMethodBuilder : IPersistable
    {
        public static CMethodBuilder Create() => new CMethodBuilder();
        
        public CTask Task { get; private set;  } 
        private IAsyncStateMachine StateMachine { get; set; }

        public CMethodBuilder()
        {
            Task = new CTask();
            //todo check what the persistent context is and rootify this object / (maybe weakly rootify it)
            //todo Rootify this object if persistent context is true when created
            //todo unrootify this object when set result has been invoked
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            StateMachine = stateMachine; //box state machine straight away
            StateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) => StateMachine = stateMachine;

        public void SetException(Exception exception) => Task.SignalThrownException(exception);

        public void SetResult() => Task.SignalCompletion();

        private void MoveNext() => StateMachine.MoveNext();

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine  
            => awaiter.OnCompleted(MoveNext);

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => awaiter.OnCompleted(MoveNext);

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            StateMachine.SerializeReferencesUsingReflectionInto(
                sd,
                helper
            );

            sd.Set("StmType", StateMachine.GetType().SimpleQualifiedName());
            sd.Set("Task", helper.GetReference(Task));
        }

        internal static CMethodBuilder Deserialize(IReadOnlyDictionary<string, object> stateMap)
        {
            var taskReference = (Reference) stateMap["Task"];
            var stmType = Type.GetType(stateMap["StmType"].ToString());
            var stmInstance = (IAsyncStateMachine) Activator.CreateInstance(stmType);
            stateMap.DeserializeReferencesUsingReflectionInto(stmInstance, null, "Task", "StmType", "Type"); //todo add the life time scope

            var builder = new CMethodBuilder();

            stmType.GetField("<>t__builder").SetValue(stmInstance, builder); //consider if this is even necessary

            taskReference.DoWhenResolved<CTask>(t => builder.Task = t);
            builder.StateMachine = stmInstance;
            return builder;
        }
    }

    public class CorumsMethodBuilder<T> : IPersistable
    {
        public static CorumsMethodBuilder<T> Create() => new CorumsMethodBuilder<T>();

        public CorumsMethodBuilder()
        {
            Task = new CTask<T>();
            //todo check what the persistent context is and rootify this object / (maybe weakly rootify it)
            //todo Rootify this object if persistent context is true when created
            //todo unrootify this object when set result has been invoked
        }

        private IAsyncStateMachine StateMachine { get; set; }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            StateMachine = stateMachine; //box immediately
            StateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) => StateMachine = stateMachine; //todo can we skip this due to the way Start boxes straight away

        public void SetException(Exception exception) => Task.SignalThrownException(exception);

        public void SetResult(T result) => Task.SignalCompletion(result);
        public CTask<T> Task { get; private set; }

        private void MoveNext() => StateMachine.MoveNext();

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine 
            => awaiter.OnCompleted(MoveNext);

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => awaiter.OnCompleted(MoveNext);

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            StateMachine.SerializeReferencesUsingReflectionInto(sd, helper);

            sd.Set("StmType", StateMachine.GetType().SimpleQualifiedName());
            sd.Set("Task", helper.GetReference(Task));
        }

        internal static CorumsMethodBuilder<T> Deserialize(IReadOnlyDictionary<string, object> stateMap)
        {
            var taskReference =  (Reference) stateMap["Task"];
            var stmType = Type.GetType(stateMap["StmType"].ToString());
            var stmInstance = (IAsyncStateMachine) Activator.CreateInstance(stmType);
            stateMap.DeserializeReferencesUsingReflectionInto(stmInstance, null, "Task", "StmType", "Type", "IsManuallyPersisted"); //todo add the life time scope

            var builder = new CorumsMethodBuilder<T>();
            stmType.GetField("<>t__builder").SetValue(stmInstance, builder); //consider if this is necessary
            taskReference.DoWhenResolved<CTask<T>>(t => builder.Task = t);
            builder.StateMachine = stmInstance;
            return builder;
        }
    }
}