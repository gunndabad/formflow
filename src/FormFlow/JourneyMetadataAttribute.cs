using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace FormFlow
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class JourneyMetadataAttribute : Attribute, IActionModelConvention, IControllerModelConvention
    {
        public JourneyMetadataAttribute(
            string journeyName,
            Type stateType,
            bool appendUniqueKey,
            params string[] idRouteDataKeys)
        {
            JourneyName = journeyName ?? throw new ArgumentNullException(nameof(journeyName));
            StateType = stateType ?? throw new ArgumentNullException(nameof(stateType));
            IdRouteDataKeys = idRouteDataKeys;
            AppendUniqueKey = appendUniqueKey;
        }

        public bool AppendUniqueKey { get; }

        public string JourneyName { get; }

        public IReadOnlyCollection<string> IdRouteDataKeys { get; }

        public Type StateType { get; }

        void IActionModelConvention.Apply(ActionModel action)
        {
            AddMetadataToAction(action);
        }

        void IControllerModelConvention.Apply(ControllerModel controller)
        {
            foreach (var action in controller.Actions)
            {
                AddMetadataToAction(action);
            }
        }

        private void AddMetadataToAction(ActionModel action)
        {
            var descriptor = new JourneyDescriptor(JourneyName, StateType, IdRouteDataKeys, AppendUniqueKey);
            action.Properties.Add(typeof(JourneyDescriptor), descriptor);
        }
    }
}
