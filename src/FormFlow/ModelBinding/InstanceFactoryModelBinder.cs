using System;
using System.Threading.Tasks;
using FormFlow.Metadata;
using FormFlow.State;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FormFlow.ModelBinding
{
    public class InstanceFactoryModelBinder : IModelBinder
    {
        private readonly IUserInstanceStateProvider _stateProvider;

        public InstanceFactoryModelBinder(IUserInstanceStateProvider stateProvider)
        {
            _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType != typeof(FormFlowInstanceFactory))
            {
                return Task.CompletedTask;
            }

            var flowDescriptor = FormFlowDescriptor.FromActionContext(bindingContext.ActionContext);
            if (flowDescriptor == null)
            {
                return Task.CompletedTask;
            }

            var instanceFactory = new FormFlowInstanceFactory(flowDescriptor, bindingContext.ActionContext, _stateProvider);
            bindingContext.Result = ModelBindingResult.Success(instanceFactory);

            return Task.CompletedTask;
        }
    }
}
