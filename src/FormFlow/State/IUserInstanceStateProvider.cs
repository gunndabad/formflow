using System;
using System.Collections.Generic;

namespace FormFlow.State;

public interface IUserInstanceStateProvider
{
    void CompleteInstance(JourneyInstanceId instanceId);

    JourneyInstance CreateInstance(
        string journeyName,
        JourneyInstanceId instanceId,
        Type stateType,
        object state,
        IReadOnlyDictionary<object, object>? properties);

    void DeleteInstance(JourneyInstanceId instanceId);

    JourneyInstance? GetInstance(JourneyInstanceId instanceId);

    void UpdateInstanceState(JourneyInstanceId instanceId, object state);
}
