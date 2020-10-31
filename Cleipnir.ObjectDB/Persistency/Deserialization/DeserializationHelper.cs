using System;
using System.Collections.Generic;

namespace Cleipnir.ObjectDB.Persistency.Deserialization
{
    public class DeserializationHelper
    {
        private readonly List<Action> _toDoAfterInstanceCreation = new List<Action>();
        public void DoPostInstanceCreation(Action action) => _toDoAfterInstanceCreation.Add(action);

        internal void ExecutePostInstanceCreationCallbacks()
        {
            foreach (var action in _toDoAfterInstanceCreation)
                action();
        }
    }
}
