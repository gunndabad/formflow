using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace FormFlow
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class FlowActionAttribute : Attribute, IActionModelConvention, IControllerModelConvention
    {
        public FlowActionAttribute(
            string key,
            Type stateType,
            bool useRandomExtension,
            params string[] idRouteDataKeys)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            StateType = stateType ?? throw new ArgumentNullException(nameof(stateType));
            IdRouteDataKeys = idRouteDataKeys;
            UseRandomExtension = useRandomExtension;
        }

        public string Key { get; }

        public IReadOnlyCollection<string> IdRouteDataKeys { get; }

        public Type StateType { get; }

        public bool UseRandomExtension { get; }

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
            var descriptor = new FlowDescriptor(Key, StateType, IdRouteDataKeys, UseRandomExtension);
            action.Properties.Add(typeof(FlowDescriptor), descriptor);
        }
    }
}
