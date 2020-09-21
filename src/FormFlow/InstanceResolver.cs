using System;
using FormFlow.Metadata;
using FormFlow.State;
using Microsoft.AspNetCore.Mvc;

namespace FormFlow
{
    internal class InstanceResolver
    {
        private readonly IUserInstanceStateProvider _stateProvider;

        public InstanceResolver(IUserInstanceStateProvider stateProvider)
        {
            _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
        }

        public FormFlowInstance Resolve(ActionContext actionContext)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            var flowDescriptor = FormFlowDescriptor.FromActionContext(actionContext);
            if (flowDescriptor == null)
            {
                return null;
            }

            if (!FormFlowInstanceId.TryResolve(
                flowDescriptor,
                actionContext.HttpContext.Request,
                actionContext.RouteData,
                out var instanceId))
            {
                return null;
            }

            var instance = _stateProvider.GetInstance(instanceId);
            if (instance == null)
            {
                return null;
            }

            if (instance.Key != flowDescriptor.Key)
            {
                return null;
            }

            if (instance.StateType != flowDescriptor.StateType)
            {
                return null;
            }

            return instance;
        }
    }
}
