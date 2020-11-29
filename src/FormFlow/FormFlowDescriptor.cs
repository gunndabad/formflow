using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace FormFlow
{
    public class FlowDescriptor
    {
        public FlowDescriptor(
            string key,
            Type stateType,
            IEnumerable<string> dependentRouteDataKeys,
            bool useRandomExtension)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            StateType = stateType ?? throw new ArgumentNullException(nameof(stateType));
            DependentRouteDataKeys = dependentRouteDataKeys?.ToArray() ?? Array.Empty<string>();
            UseRandomExtension = useRandomExtension;
        }

        public IReadOnlyCollection<string> DependentRouteDataKeys { get; }

        public string Key { get; }

        public Type StateType { get; }

        public bool UseRandomExtension { get; }

        public static FlowDescriptor? FromActionContext(ActionContext actionContext)
        {
            if (actionContext is null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            return actionContext.ActionDescriptor.GetProperty<FlowDescriptor>();
        }
    }
}
