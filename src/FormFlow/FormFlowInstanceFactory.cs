using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FormFlow.Metadata;
using FormFlow.State;
using Microsoft.AspNetCore.Mvc;

namespace FormFlow
{
    public class FormFlowInstanceFactory
    {
        private readonly FormFlowDescriptor _flowDescriptor;
        private readonly ActionContext _actionContext;
        private readonly IUserInstanceStateProvider _stateProvider;
        private readonly InstanceResolver _instanceResolver;

        public FormFlowInstanceFactory(
            FormFlowDescriptor flowDescriptor,
            ActionContext actionContext,
            IUserInstanceStateProvider stateProvider)
        {
            _flowDescriptor = flowDescriptor ?? throw new ArgumentNullException(nameof(flowDescriptor));
            _actionContext = actionContext ?? throw new ArgumentNullException(nameof(actionContext));
            _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
            _instanceResolver = new InstanceResolver(_stateProvider);
        }
        
        public FormFlowInstance<TState> CreateInstance<TState>(
            TState state,
            IReadOnlyDictionary<object, object> properties = null)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (typeof(TState) != _flowDescriptor.StateType)
            {
                throw new InvalidOperationException(
                    $"{typeof(TState).Name} is not compatible with {_flowDescriptor.StateType.Name}.");
            }

            var instanceId = FormFlowInstanceId.Generate(
                _flowDescriptor,
                _actionContext.HttpContext.Request,
                _actionContext.RouteData);

            var instance = (FormFlowInstance<TState>)_stateProvider.CreateInstance(
                _flowDescriptor.Key,
                instanceId,
                _flowDescriptor.StateType,
                state, properties);

            _actionContext.HttpContext.Features.Set(new FormFlowInstanceFeature(instance));

            return instance;
        }

        public FormFlowInstance<TState> GetOrCreateInstance<TState>(
            Func<TState> createState,
            IReadOnlyDictionary<object, object> properties = null)
        {
            if (createState == null)
            {
                throw new ArgumentNullException(nameof(createState));
            }

            if (typeof(TState) != _flowDescriptor.StateType)
            {
                throw new InvalidOperationException(
                    $"{typeof(TState).Name} is not compatible with {_flowDescriptor.StateType.Name}.");
            }

            var currentInstance = GetExistingInstance();
            if (currentInstance != null)
            {
                return (FormFlowInstance<TState>)currentInstance;
            }

            var newState = createState();

            return CreateInstance(newState, properties);
        }

        public async Task<FormFlowInstance<TState>> GetOrCreateInstanceAsync<TState>(
            Func<Task<TState>> createState,
            IReadOnlyDictionary<object, object> properties = null)
        {
            if (createState == null)
            {
                throw new ArgumentNullException(nameof(createState));
            }

            if (typeof(TState) != _flowDescriptor.StateType)
            {
                throw new InvalidOperationException(
                    $"{typeof(TState).Name} is not compatible with {_flowDescriptor.StateType.Name}.");
            }

            var currentInstance = GetExistingInstance();
            if (currentInstance != null)
            {
                return (FormFlowInstance<TState>)currentInstance;
            }

            var newState = await createState();

            return CreateInstance(newState, properties);
        }

        private FormFlowInstance GetExistingInstance() =>
            _actionContext.HttpContext.Features.Get<FormFlowInstanceFeature>()?.Instance ??
                _instanceResolver.Resolve(_actionContext);
    }
}
