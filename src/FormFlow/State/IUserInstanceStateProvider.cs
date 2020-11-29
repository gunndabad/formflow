using System;
using System.Collections.Generic;

namespace FormFlow.State
{
    public interface IUserInstanceStateProvider
    {
        void CompleteInstance(FormFlowInstanceId instanceId);

        FormFlowInstance CreateInstance(
            string key,
            FormFlowInstanceId instanceId,
            Type stateType,
            object state,
            IReadOnlyDictionary<object, object>? properties);

        void DeleteInstance(FormFlowInstanceId instanceId);

        FormFlowInstance? GetInstance(FormFlowInstanceId instanceId);

        void UpdateInstanceState(FormFlowInstanceId instanceId, object state);
    }
}
