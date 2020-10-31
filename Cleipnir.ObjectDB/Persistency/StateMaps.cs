using System.Collections.Generic;
using Cleipnir.ObjectDB.Helpers.DataStructures;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.ObjectDB.Persistency
{
    internal class StateMaps
    {
        private readonly DictionaryWithDefault<long, StateMap> _stateMaps;

        public StateMaps(Serializers serializers)
        {
            _stateMaps = new DictionaryWithDefault<long, StateMap>(_ => new StateMap(serializers));
        }

        public StateMap this[long id]
        {
            get => _stateMaps[id];
            set => _stateMaps[id] = value;
        }

        public StateMap Get(long id) => _stateMaps[id];
        public IEnumerable<long> Ids => _stateMaps.Keys;

        public void Remove(long id) => _stateMaps.Remove(id);
    }
}
