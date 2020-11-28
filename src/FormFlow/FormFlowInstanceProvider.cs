using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FormFlow.Metadata;
using FormFlow.State;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace FormFlow
{
    public class FormFlowInstanceProvider
    {
        private readonly IUserInstanceStateProvider _stateProvider;
        private readonly IActionContextAccessor _actionContextAccessor;

        public FormFlowInstanceProvider(
            IUserInstanceStateProvider stateProvider,
            IActionContextAccessor actionContextAccessor)
        {
            _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
            _actionContextAccessor = actionContextAccessor ?? throw new ArgumentNullException(nameof(actionContextAccessor));
        }

        public FormFlowInstance CreateInstance(
            object state,
            IReadOnlyDictionary<object, object> properties = null)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var actionContext = ResolveActionContext();
            var flowDescriptor = ResolveFlowDescriptor(actionContext);

            ThrowIfStateTypeIncompatible(state.GetType(), flowDescriptor);

            var instanceId = FormFlowInstanceId.Generate(
                flowDescriptor,
                actionContext.HttpContext.Request,
                actionContext.RouteData);

            if (_stateProvider.GetInstance(instanceId) != null)
            {
                throw new InvalidOperationException("Instance already exists with this ID.");
            }

            return _stateProvider.CreateInstance(
                flowDescriptor.Key,
                instanceId,
                flowDescriptor.StateType,
                state,
                properties);
        }

        public FormFlowInstance<TState> CreateInstance<TState>(
            TState state,
            IReadOnlyDictionary<object, object> properties = null)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var actionContext = ResolveActionContext();
            var flowDescriptor = ResolveFlowDescriptor(actionContext);

            ThrowIfStateTypeIncompatible(typeof(TState), flowDescriptor);

            var instanceId = FormFlowInstanceId.Generate(
                flowDescriptor,
                actionContext.HttpContext.Request,
                actionContext.RouteData);

            if (_stateProvider.GetInstance(instanceId) != null)
            {
                throw new InvalidOperationException("Instance already exists with this ID.");
            }

            return (FormFlowInstance<TState>)_stateProvider.CreateInstance(
                flowDescriptor.Key,
                instanceId,
                flowDescriptor.StateType,
                state,
                properties);
        }

        public FormFlowInstance GetInstance()
        {
            // Throw if ActionContext or FlowDescriptor are missing
            ResolveFlowDescriptor(ResolveActionContext());

            if (TryResolveExistingInstance(out var instance))
            {
                return instance;
            }
            else
            {
                return null;
            }
        }

        public FormFlowInstance<TState> GetInstance<TState>()
        {
            // Throw if ActionContext or FlowDescriptor are missing
            ResolveFlowDescriptor(ResolveActionContext());

            if (TryResolveExistingInstance(out var instance))
            {
                ThrowIfStateTypeIncompatible(typeof(TState), instance.StateType);

                return (FormFlowInstance<TState>)instance;
            }
            else
            {
                return null;
            }
        }

        public FormFlowInstance GetOrCreateInstance(
            Func<object> createState,
            IReadOnlyDictionary<object, object> properties = null)
        {
            if (createState == null)
            {
                throw new ArgumentNullException(nameof(createState));
            }

            var actionContext = ResolveActionContext();
            var flowDescriptor = ResolveFlowDescriptor(actionContext);

            if (TryResolveExistingInstance(out var instance))
            {
                return instance;
            }

            var newState = createState();

            ThrowIfStateTypeIncompatible(newState.GetType(), flowDescriptor);

            return CreateInstance(newState, properties);
        }

        public FormFlowInstance<TState> GetOrCreateInstance<TState>(
            Func<TState> createState,
            IReadOnlyDictionary<object, object> properties = null)
        {
            if (createState == null)
            {
                throw new ArgumentNullException(nameof(createState));
            }

            var actionContext = ResolveActionContext();
            var flowDescriptor = ResolveFlowDescriptor(actionContext);

            ThrowIfStateTypeIncompatible(typeof(TState), flowDescriptor);

            if (TryResolveExistingInstance(out var instance))
            {
                return (FormFlowInstance<TState>)instance;
            }

            var newState = createState();

            return CreateInstance(newState, properties);
        }

        public async Task<FormFlowInstance> GetOrCreateInstanceAsync(
            Func<Task<object>> createState,
            IReadOnlyDictionary<object, object> properties = null)
        {
            if (createState == null)
            {
                throw new ArgumentNullException(nameof(createState));
            }

            var actionContext = ResolveActionContext();
            var flowDescriptor = ResolveFlowDescriptor(actionContext);

            if (TryResolveExistingInstance(out var instance))
            {
                return instance;
            }

            var newState = await createState();

            ThrowIfStateTypeIncompatible(newState.GetType(), flowDescriptor);

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

            var actionContext = ResolveActionContext();
            var flowDescriptor = ResolveFlowDescriptor(actionContext);

            ThrowIfStateTypeIncompatible(typeof(TState), flowDescriptor);

            if (TryResolveExistingInstance(out var instance))
            {
                return (FormFlowInstance<TState>)instance;
            }

            var newState = await createState();

            return CreateInstance(newState, properties);
        }

        internal bool TryResolveExistingInstance(out FormFlowInstance instance)
        {
            instance = default;

            var actionContext = ResolveActionContext();

            var flowDescriptor = ResolveFlowDescriptor(actionContext, throwIfNotFound: false);

            if (flowDescriptor == null)
            {
                return false;
            }

            if (!FormFlowInstanceId.TryResolve(
                flowDescriptor,
                actionContext.HttpContext.Request,
                actionContext.RouteData,
                out var instanceId))
            {
                return false;
            }

            var persistedInstance = _stateProvider.GetInstance(instanceId);
            if (persistedInstance == null)
            {
                return false;
            }

            if (persistedInstance.Key != flowDescriptor.Key)
            {
                return false;
            }

            if (persistedInstance.StateType != flowDescriptor.StateType)
            {
                return false;
            }

            // Protect against stateProvider handing back a deleted instance
            if (persistedInstance.Deleted)
            {
                return false;
            }

            instance = persistedInstance;
            return true;
        }

        private static void ThrowIfStateTypeIncompatible(Type stateType, FormFlowDescriptor flowDescriptor) =>
            ThrowIfStateTypeIncompatible(stateType, flowDescriptor.StateType);

        private static void ThrowIfStateTypeIncompatible(Type stateType, Type instanceStateType)
        {
            if (stateType != instanceStateType)
            {
                throw new InvalidOperationException(
                    $"{stateType.FullName} is not compatible with the FormFlow metadata's state type ({instanceStateType.FullName}).");
            }
        }

        private ActionContext ResolveActionContext()
        {
            var actionContext = _actionContextAccessor.ActionContext;

            if (actionContext == null)
            {
                throw new InvalidOperationException("No active ActionContext.");
            }

            return actionContext;
        }

        private FormFlowDescriptor ResolveFlowDescriptor(
            ActionContext actionContext,
            bool throwIfNotFound = true)
        {
            var descriptor = FormFlowDescriptor.FromActionContext(actionContext);

            if (descriptor == null && throwIfNotFound)
            {
                throw new InvalidOperationException("No FormFlow metadata found on action.");
            }

            return descriptor;
        }
    }
}
