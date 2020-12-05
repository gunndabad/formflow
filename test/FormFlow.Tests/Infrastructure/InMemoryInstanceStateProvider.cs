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

        public JourneyInstance CreateInstance(
            string journeyName,
            JourneyInstanceId instanceId,
            Type stateType,
            object state,
            IReadOnlyDictionary<object, object> properties)
        {
            _instances.Add(instanceId, new Entry()
            {
                JourneyName = journeyName,
                StateType = stateType,
                State = state,
                Properties = properties
            });

            var instance = JourneyInstance.Create(
                this,
                journeyName,
                instanceId,
                stateType,
                state,
                properties ?? PropertiesBuilder.CreateEmpty());

            return instance;
        }

        public void CompleteInstance(JourneyInstanceId instanceId)
        {
            _instances[instanceId].Completed = true;
        }

        public void DeleteInstance(JourneyInstanceId instanceId)
        {
            _instances.Remove(instanceId);
        }

        public JourneyInstance GetInstance(JourneyInstanceId instanceId)
        {
            _instances.TryGetValue(instanceId, out var entry);

            var instance = entry != null ?
                JourneyInstance.Create(this, entry.JourneyName, instanceId, entry.StateType, entry.State, entry.Properties, entry.Completed) :
                null;

            return instance;
        }

        public void UpdateInstanceState(JourneyInstanceId instanceId, object state)
        {
            _instances[instanceId].State = state;
        }

        private class Entry
        {
            public string JourneyName { get; set; }
            public IReadOnlyDictionary<object, object> Properties { get; set; }
            public object State { get; set; }
            public Type StateType { get; set; }
            public bool Completed { get; set; }
        }
    }
}
