using System;
using FormFlow.Filters;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace FormFlow
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RequireJourneyInstanceAttribute :
        Attribute,
        IActionModelConvention,
        IControllerModelConvention
    {
        private int? _errorStatusCode;

        public int ErrorStatusCode
        {
            get
            {
                return _errorStatusCode!.Value;  // yuk
            }
            set
            {
                if (value < 400 || value > 599)
                {
                    throw new ArgumentOutOfRangeException();
                }

                _errorStatusCode = value;
            }
        }

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
            action.Properties.Add(
                typeof(RequireInstanceMarker),
                new RequireInstanceMarker(_errorStatusCode));
        }
    }
}
