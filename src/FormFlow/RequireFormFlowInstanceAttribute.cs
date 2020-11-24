using System;
using FormFlow.Filters;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace FormFlow
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RequireFormFlowInstanceAttribute : Attribute, IActionModelConvention, IControllerModelConvention
    {
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
            action.Properties.Add(typeof(RequiresInstanceMarker), RequiresInstanceMarker.Instance);
        }
    }
}
