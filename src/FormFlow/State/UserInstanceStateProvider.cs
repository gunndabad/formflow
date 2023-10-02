#nullable disable
using System;
using System.Collections.Generic;

namespace FormFlow.State
{
    public class UserInstanceStateProvider : IUserInstanceStateProvider
    {
        private readonly IStateSerializer _stateSerializer;
        private readonly IUserInstanceStateStore _store;

        public UserInstanceStateProvider(
            IStateSerializer stateSerializer,
            IUserInstanceStateStore store)
        {
            _stateSerializer = stateSerializer ?? throw new ArgumentNullException(nameof(stateSerializer));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public JourneyInstance CreateInstance(
            string journeyName,
            JourneyInstanceId instanceId,
            Type stateType,
            object state,
            IReadOnlyDictionary<object, object> properties)
        {
            if (journeyName == null)
            {
                throw new ArgumentNullException(nameof(journeyName));
            }

            if (stateType == null)
            {
                throw new ArgumentNullException(nameof(stateType));
            }

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            properties ??= PropertiesBuilder.CreateEmpty();

            var entry = new StoreEntry()
            {
                JourneyName = journeyName,
                StateTypeAssemblyQualifiedName = stateType.AssemblyQualifiedName,
                State = state,
                Properties = properties,
                Completed = false
            };
            var serialized = _stateSerializer.Serialize(entry);

            var storeKey = GetKeyForInstance(instanceId);
            _store.SetState(storeKey, serialized);

            return JourneyInstance.Create(this, journeyName, instanceId, stateType, state, properties);
        }

        public void CompleteInstance(JourneyInstanceId instanceId)
        {
            var storeKey = GetKeyForInstance(instanceId);

            if (_store.TryGetState(storeKey, out var serialized))
            {
                var entry = (StoreEntry)_stateSerializer.Deserialize(serialized);

                entry.Completed = true;

                var updateSerialized = _stateSerializer.Serialize(entry);
                _store.SetState(storeKey, updateSerialized);
            }
            else
            {
                throw new ArgumentException("Instance does not exist.", nameof(instanceId));
            }
        }

        public void DeleteInstance(JourneyInstanceId instanceId)
        {
            var storeKey = GetKeyForInstance(instanceId);

            if (_store.TryGetState(storeKey, out _))
            {
                _store.DeleteState(storeKey);
            }
            else
            {
                throw new ArgumentException("Instance does not exist.", nameof(instanceId));
            }
        }

        public JourneyInstance GetInstance(JourneyInstanceId instanceId)
        {
            var storeKey = GetKeyForInstance(instanceId);

            if (_store.TryGetState(storeKey, out var serialized))
            {
                var entry = (StoreEntry)_stateSerializer.Deserialize(serialized);

                var stateType = Type.GetType(entry.StateTypeAssemblyQualifiedName);

                return JourneyInstance.Create(
                    this,
                    entry.JourneyName,
                    instanceId,
                    stateType,
                    entry.State,
                    entry.Properties,
                    entry.Completed);
            }
            else
            {
                return null;
            }
        }

        public void UpdateInstanceState(JourneyInstanceId instanceId, object state)
        {
            var storeKey = GetKeyForInstance(instanceId);

            if (_store.TryGetState(storeKey, out var serialized))
            {
                var entry = (StoreEntry)_stateSerializer.Deserialize(serialized);

                entry.State = state;

                var updateSerialized = _stateSerializer.Serialize(entry);
                _store.SetState(storeKey, updateSerialized);
            }
            else
            {
                throw new ArgumentException("Instance does not exist.", nameof(instanceId));
            }
        }

        // TODO Make this configurable
        private static string GetKeyForInstance(string instanceId) =>
            $"FormFlowState:{instanceId}";

        private class StoreEntry
        {
            public string JourneyName { get; set; }
            public string StateTypeAssemblyQualifiedName { get; set; }
            public object State { get; set; }
            public IReadOnlyDictionary<object, object> Properties { get; set; }
            public bool Completed { get; set; }
        }
    }
}
