using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace FormFlow.Metadata
{
    public class FormFlowDescriptor
    {
        public FormFlowDescriptor(
            string key,
            Type stateType,
            IdGenerationSource idGenerationSource,
            IEnumerable<string> idRouteParameterNames = null)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            StateType = stateType ?? throw new ArgumentNullException(nameof(stateType));

            var idRouteParameterNamesArray = idRouteParameterNames?.ToArray() ?? Array.Empty<string>();

            if (idGenerationSource == IdGenerationSource.RouteValues &&
                idRouteParameterNamesArray.Length == 0)
            {
                throw new ArgumentException(
                    $"At least one route parameter name must be provided when {nameof(idGenerationSource)} is {IdGenerationSource.RouteValues}.",
                    nameof(idRouteParameterNames));
            }
            else if (idGenerationSource != IdGenerationSource.RouteValues &&
                idRouteParameterNamesArray.Length != 0)
            {
                throw new ArgumentException(
                    $"Route parameter names can only be provided when {nameof(idGenerationSource)} is {IdGenerationSource.RouteValues}.",
                    nameof(idRouteParameterNames));
            }

            IdGenerationSource = idGenerationSource;
            IdRouteParameterNames = idRouteParameterNamesArray;
        }

        public string Key { get; }
        public Type StateType { get; }
        public IdGenerationSource IdGenerationSource { get; }
        public IReadOnlyCollection<string> IdRouteParameterNames { get; }

        public static FormFlowDescriptor FromActionContext(ActionContext actionContext)
        {
            if (actionContext is null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            return actionContext.ActionDescriptor.GetProperty<FormFlowDescriptor>();
        }
    }
}
