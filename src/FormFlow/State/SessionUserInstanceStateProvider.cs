using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FormFlow.State;

public class SessionUserInstanceStateProvider : IUserInstanceStateProvider
{
    private readonly IStateSerializer _stateSerializer;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionUserInstanceStateProvider(
        IStateSerializer stateSerializer,
        IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(stateSerializer);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);

        _stateSerializer = stateSerializer;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<JourneyInstance> CreateInstanceAsync(
        string journeyName,
        JourneyInstanceId instanceId,
        Type stateType,
        object state,
        IReadOnlyDictionary<object, object>? properties)
    {
        ArgumentNullException.ThrowIfNull(journeyName);
        ArgumentNullException.ThrowIfNull(stateType);
        ArgumentNullException.ThrowIfNull(state);

        properties ??= PropertiesBuilder.CreateEmpty();

        var serializedState = _stateSerializer.Serialize(stateType, state);

        var entry = new SessionEntry()
        {
            JourneyName = journeyName,
            State = serializedState,
            Properties = properties,
            Completed = false
        };

        SetSessionEntry(instanceId, entry);

        return Task.FromResult(JourneyInstance.Create(this, journeyName, instanceId, stateType, state, properties));
    }

    public Task CompleteInstanceAsync(string journeyName, JourneyInstanceId instanceId, Type stateType)
    {
        ArgumentNullException.ThrowIfNull(journeyName);
        ArgumentNullException.ThrowIfNull(stateType);

        var session = GetSession();
        var storeKey = GetKeyForInstance(instanceId);

        if (session.TryGetValue(storeKey, out var serialized))
        {
            var entry = DeserializeSessionEntry(serialized);
            entry.Completed = true;
            SetSessionEntry(instanceId, entry, session);
        }
        else
        {
            throw new ArgumentException("Instance does not exist.", nameof(instanceId));
        }

        return Task.CompletedTask;
    }

    public Task DeleteInstanceAsync(string journeyName, JourneyInstanceId instanceId, Type stateType)
    {
        ArgumentNullException.ThrowIfNull(journeyName);
        ArgumentNullException.ThrowIfNull(stateType);

        var session = GetSession();
        var storeKey = GetKeyForInstance(instanceId);

        if (session.TryGetValue(storeKey, out var _))
        {
            session.Remove(storeKey);
        }
        else
        {
            throw new ArgumentException("Instance does not exist.", nameof(instanceId));
        }

        return Task.CompletedTask;
    }

    public Task<JourneyInstance?> GetInstanceAsync(string journeyName, JourneyInstanceId instanceId, Type stateType)
    {
        ArgumentNullException.ThrowIfNull(journeyName);
        ArgumentNullException.ThrowIfNull(stateType);

        var session = GetSession();
        var storeKey = GetKeyForInstance(instanceId);

        if (session.TryGetValue(storeKey, out var serialized))
        {
            var entry = DeserializeSessionEntry(serialized);
            var deserializedState = _stateSerializer.Deserialize(stateType, entry.State);

            return Task.FromResult((JourneyInstance?)JourneyInstance.Create(
                this,
                entry.JourneyName,
                instanceId,
                stateType,
                deserializedState,
                entry.Properties,
                entry.Completed));
        }
        else
        {
            return Task.FromResult((JourneyInstance?)null);
        }
    }

    public Task UpdateInstanceStateAsync(string journeyName, JourneyInstanceId instanceId, Type stateType, object state)
    {
        var session = GetSession();
        var storeKey = GetKeyForInstance(instanceId);

        if (session.TryGetValue(storeKey, out var serialized))
        {
            var entry = DeserializeSessionEntry(serialized);
            entry.State = _stateSerializer.Serialize(stateType, state);
            SetSessionEntry(instanceId, entry);
        }
        else
        {
            throw new ArgumentException("Instance does not exist.", nameof(instanceId));
        }

        return Task.CompletedTask;
    }

    private ISession GetSession()
    {
        var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No active HttpContext.");
        return httpContext.Session ?? throw new InvalidOperationException("No Session available on HttpContext.");
    }

    private void SetSessionEntry(JourneyInstanceId instanceId, SessionEntry entry) =>
        SetSessionEntry(instanceId, entry, GetSession());

    private static void SetSessionEntry(JourneyInstanceId instanceId, SessionEntry entry, ISession session)
    {
        var key = GetKeyForInstance(instanceId);
        var serializedEntry = SerializeSessionEntry(entry);
        session.Set(key, serializedEntry);
    }

    private static SessionEntry DeserializeSessionEntry(byte[] bytes) =>
        JsonSerializer.Deserialize<SessionEntry>(bytes)!;

    private static byte[] SerializeSessionEntry(SessionEntry entry) =>
        JsonSerializer.SerializeToUtf8Bytes(entry, typeof(SessionEntry));

    // TODO Make this configurable
    private static string GetKeyForInstance(string instanceId) =>
        $"FormFlowState:{instanceId}";

    private class SessionEntry
    {
        public string JourneyName { get; set; } = null!;
        public string State { get; set; } = null!;
        public IReadOnlyDictionary<object, object> Properties { get; set; } = null!;
        public bool Completed { get; set; }
    }
}
