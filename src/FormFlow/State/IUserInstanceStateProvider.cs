using System;
using System.Collections.Generic;

namespace FormFlow.State;

public interface IUserInstanceStateProvider
{
    void CompleteInstance(string journeyName, JourneyInstanceId instanceId, Type stateType);

    JourneyInstance CreateInstance(
        string journeyName,
        JourneyInstanceId instanceId,
        Type stateType,
        object state,
        IReadOnlyDictionary<object, object>? properties);

    void DeleteInstance(string journeyName, JourneyInstanceId instanceId, Type stateType);

    JourneyInstance? GetInstance(string journeyName, JourneyInstanceId instanceId, Type stateType);

    void UpdateInstanceState(string journeyName, JourneyInstanceId instanceId, Type stateType, object state);
}
