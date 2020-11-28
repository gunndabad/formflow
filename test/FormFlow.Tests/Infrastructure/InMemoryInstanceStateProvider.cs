using System;
using System.Collections.Generic;
using FormFlow.State;

namespace FormFlow.Tests.Infrastructure
{
    public class InMemoryInstanceStateProvider : IUserInstanceStateProvider
    {
        private readonly Dictionary<string, Entry> _instances;

        public InMemoryInstanceStateProvider()
        {
            _instances = new Dictionary<string, Entry>();
        }

        public void Clear() => _instances.Clear();

        public FormFlowInstance CreateInstance(
            string key,
            FormFlowInstanceId instanceId,
            Type stateType,
            object state,
            IReadOnlyDictionary<object, object> properties)
        {
            _instances.Add(instanceId, new Entry()
            {
                Key = key,
                StateType = stateType,
                State = state,
                Properties = properties
            });

            var instance = FormFlowInstance.Create(
                this,
                key,
                instanceId,
                stateType,
                state,
                properties ?? new Dictionary<object, object>());

            return instance;
        }

        public void CompleteInstance(FormFlowInstanceId instanceId)
        {
            _instances[instanceId].Completed = true;
        }

        public void DeleteInstance(FormFlowInstanceId instanceId)
        {
            _instances.Remove(instanceId);
        }

        public FormFlowInstance GetInstance(FormFlowInstanceId instanceId)
        {
            _instances.TryGetValue(instanceId, out var entry);

            var instance = entry != null ?
                FormFlowInstance.Create(this, entry.Key, instanceId, entry.StateType, entry.State, entry.Properties, entry.Completed) :
                null;

            return instance;
        }

        public void UpdateInstanceState(FormFlowInstanceId instanceId, object state)
        {
            _instances[instanceId].State = state;
        }

        private class Entry
        {
            public string Key { get; set; }
            public IReadOnlyDictionary<object, object> Properties { get; set; }
            public object State { get; set; }
            public Type StateType { get; set; }
            public bool Completed { get; set; }
        }
    }
}
