﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace FormFlow
{
    public class JourneyDescriptor
    {
        public JourneyDescriptor(
            string journeyName,
            Type stateType,
            IEnumerable<string> dependentRouteDataKeys,
            bool appendUniqueKey)
        {
            JourneyName = journeyName ?? throw new ArgumentNullException(nameof(journeyName));
            StateType = stateType ?? throw new ArgumentNullException(nameof(stateType));
            DependentRouteDataKeys = dependentRouteDataKeys?.ToArray() ?? Array.Empty<string>();
            AppendUniqueKey = appendUniqueKey;
        }

        public bool AppendUniqueKey { get; }

        public IReadOnlyCollection<string> DependentRouteDataKeys { get; }

        public string JourneyName { get; }

        public Type StateType { get; }

        public static JourneyDescriptor? FromActionContext(ActionContext actionContext)
        {
            if (actionContext is null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            return actionContext.ActionDescriptor.GetProperty<JourneyDescriptor>();
        }
    }
}
