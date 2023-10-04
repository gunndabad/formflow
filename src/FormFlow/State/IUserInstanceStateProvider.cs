using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FormFlow.State;

public interface IUserInstanceStateProvider
{
    Task CompleteInstanceAsync(string journeyName, JourneyInstanceId instanceId, Type stateType);

    Task<JourneyInstance> CreateInstanceAsync(
        string journeyName,
        JourneyInstanceId instanceId,
        Type stateType,
        object state,
        IReadOnlyDictionary<object, object>? properties);

    Task DeleteInstanceAsync(string journeyName, JourneyInstanceId instanceId, Type stateType);

    Task<JourneyInstance?> GetInstanceAsync(string journeyName, JourneyInstanceId instanceId, Type stateType);

    Task UpdateInstanceStateAsync(string journeyName, JourneyInstanceId instanceId, Type stateType, object state);
}
