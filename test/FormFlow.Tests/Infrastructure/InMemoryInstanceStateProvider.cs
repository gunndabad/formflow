using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FormFlow.State;

namespace FormFlow.Tests.Infrastructure;

public class InMemoryInstanceStateProvider : IUserInstanceStateProvider
{
    private readonly Dictionary<string, Entry> _instances;

    public InMemoryInstanceStateProvider()
    {
        _instances = new Dictionary<string, Entry>();
    }

    public void Clear() => _instances.Clear();

    public Task<JourneyInstance> CreateInstanceAsync(
        string journeyName,
        JourneyInstanceId instanceId,
        Type stateType,
        object state,
        IReadOnlyDictionary<object, object>? properties)
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

        return Task.FromResult(instance);
    }

    public Task CompleteInstanceAsync(string journeyName, JourneyInstanceId instanceId, Type stateType)
    {
        _instances[instanceId].Completed = true;
        return Task.CompletedTask;
    }

    public Task DeleteInstanceAsync(string journeyName, JourneyInstanceId instanceId, Type stateType)
    {
        _instances.Remove(instanceId);
        return Task.CompletedTask;
    }

    public Task<JourneyInstance?> GetInstanceAsync(string journeyName, JourneyInstanceId instanceId, Type stateType)
    {
        _instances.TryGetValue(instanceId, out var entry);

        var instance = entry != null ?
            JourneyInstance.Create(this, entry.JourneyName!, instanceId, entry.StateType!, entry.State!, entry.Properties!, entry.Completed) :
            null;

        return Task.FromResult(instance);
    }

    public Task UpdateInstanceStateAsync(string journeyName, JourneyInstanceId instanceId, Type stateType, object state)
    {
        _instances[instanceId].State = state;
        return Task.CompletedTask;
    }

    private class Entry
    {
        public string? JourneyName { get; set; }
        public IReadOnlyDictionary<object, object>? Properties { get; set; }
        public object? State { get; set; }
        public Type? StateType { get; set; }
        public bool Completed { get; set; }
    }
}
