using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FormFlow.State;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace FormFlow
{
    public class JourneyInstanceProvider
    {
        private readonly IUserInstanceStateProvider _stateProvider;
        private readonly IActionContextAccessor _actionContextAccessor;

        public JourneyInstanceProvider(
            IUserInstanceStateProvider stateProvider,
            IActionContextAccessor actionContextAccessor)
        {
            _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
            _actionContextAccessor = actionContextAccessor ?? throw new ArgumentNullException(nameof(actionContextAccessor));
        }

        public JourneyInstance CreateInstance(
            object state,
            IReadOnlyDictionary<object, object>? properties = null)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var actionContext = ResolveActionContext();
            var journeyDescriptor = ResolveJourneyDescriptor(actionContext)!;

            ThrowIfStateTypeIncompatible(state.GetType(), journeyDescriptor);

            var instanceId = JourneyInstanceId.Create(
                journeyDescriptor,
                actionContext.HttpContext.Request);

            if (_stateProvider.GetInstance(instanceId) != null)
            {
                throw new InvalidOperationException("Instance already exists with this ID.");
            }

            return _stateProvider.CreateInstance(
                journeyDescriptor.JourneyName,
                instanceId,
                journeyDescriptor.StateType,
                state,
                properties);
        }

        public JourneyInstance<TState> CreateInstance<TState>(
            TState state,
            IReadOnlyDictionary<object, object>? properties = null)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var actionContext = ResolveActionContext();
            var journeyDescriptor = ResolveJourneyDescriptor(actionContext)!;

            ThrowIfStateTypeIncompatible(typeof(TState), journeyDescriptor);

            var instanceId = JourneyInstanceId.Create(
                journeyDescriptor,
                actionContext.HttpContext.Request);

            if (_stateProvider.GetInstance(instanceId) != null)
            {
                throw new InvalidOperationException("Instance already exists with this ID.");
            }

            return (JourneyInstance<TState>)_stateProvider.CreateInstance(
                journeyDescriptor.JourneyName,
                instanceId,
                journeyDescriptor.StateType,
                state,
                properties);
        }

        public JourneyInstance? GetInstance()
        {
            // Throw if ActionContext or JourneyDescriptor are missing
            ResolveJourneyDescriptor(ResolveActionContext());

            if (TryResolveExistingInstance(out var instance))
            {
                return instance;
            }
            else
            {
                return null;
            }
        }

        public JourneyInstance<TState>? GetInstance<TState>()
        {
            // Throw if ActionContext or JourneyDescriptor are missing
            ResolveJourneyDescriptor(ResolveActionContext());

            if (TryResolveExistingInstance(out var instance))
            {
                ThrowIfStateTypeIncompatible(typeof(TState), instance.StateType);

                return (JourneyInstance<TState>)instance;
            }
            else
            {
                return null;
            }
        }

        public JourneyInstance GetOrCreateInstance(
            Func<object> createState,
            IReadOnlyDictionary<object, object>? properties = null)
        {
            if (createState == null)
            {
                throw new ArgumentNullException(nameof(createState));
            }

            var actionContext = ResolveActionContext();
            var journeyDescriptor = ResolveJourneyDescriptor(actionContext)!;

            if (TryResolveExistingInstance(out var instance))
            {
                return instance;
            }

            var newState = createState();

            ThrowIfStateTypeIncompatible(newState.GetType(), journeyDescriptor);

            return CreateInstance(newState, properties);
        }

        public JourneyInstance<TState> GetOrCreateInstance<TState>(
            Func<TState> createState,
            IReadOnlyDictionary<object, object>? properties = null)
        {
            if (createState == null)
            {
                throw new ArgumentNullException(nameof(createState));
            }

            var actionContext = ResolveActionContext();
            var journeyDescriptor = ResolveJourneyDescriptor(actionContext)!;

            ThrowIfStateTypeIncompatible(typeof(TState), journeyDescriptor);

            if (TryResolveExistingInstance(out var instance))
            {
                return (JourneyInstance<TState>)instance;
            }

            var newState = createState();

            return CreateInstance(newState, properties);
        }

        public async Task<JourneyInstance> GetOrCreateInstanceAsync(
            Func<Task<object>> createState,
            IReadOnlyDictionary<object, object>? properties = null)
        {
            if (createState == null)
            {
                throw new ArgumentNullException(nameof(createState));
            }

            var actionContext = ResolveActionContext();
            var journeyDescriptor = ResolveJourneyDescriptor(actionContext)!;

            if (TryResolveExistingInstance(out var instance))
            {
                return instance;
            }

            var newState = await createState();

            ThrowIfStateTypeIncompatible(newState.GetType(), journeyDescriptor);

            return CreateInstance(newState, properties);
        }

        public async Task<JourneyInstance<TState>> GetOrCreateInstanceAsync<TState>(
            Func<Task<TState>> createState,
            IReadOnlyDictionary<object, object>? properties = null)
        {
            if (createState == null)
            {
                throw new ArgumentNullException(nameof(createState));
            }

            var actionContext = ResolveActionContext();
            var journeyDescriptor = ResolveJourneyDescriptor(actionContext)!;

            ThrowIfStateTypeIncompatible(typeof(TState), journeyDescriptor);

            if (TryResolveExistingInstance(out var instance))
            {
                return (JourneyInstance<TState>)instance;
            }

            var newState = await createState();

            return CreateInstance(newState, properties);
        }

        public bool IsCurrentInstance(JourneyInstance instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return IsCurrentInstance(instance.InstanceId);
        }

        public bool IsCurrentInstance(JourneyInstanceId instanceId)
        {
            TryResolveExistingInstance(out var currentInstance);

            return currentInstance?.InstanceId == instanceId;
        }

        internal bool TryResolveExistingInstance([MaybeNullWhen(false)] out JourneyInstance instance)
        {
            instance = default;

            var actionContext = ResolveActionContext();

            var journeyDescriptor = ResolveJourneyDescriptor(actionContext, throwIfNotFound: false);

            if (journeyDescriptor == null)
            {
                return false;
            }

            if (!JourneyInstanceId.TryResolve(
                journeyDescriptor,
                actionContext.HttpContext.Request,
                out var instanceId))
            {
                return false;
            }

            var persistedInstance = _stateProvider.GetInstance(instanceId);
            if (persistedInstance == null)
            {
                return false;
            }

            if (persistedInstance.JourneyName != journeyDescriptor.JourneyName)
            {
                return false;
            }

            if (persistedInstance.StateType != journeyDescriptor.StateType)
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

        private static void ThrowIfStateTypeIncompatible(Type stateType, JourneyDescriptor journeyDescriptor) =>
            ThrowIfStateTypeIncompatible(stateType, journeyDescriptor.StateType);

        private static void ThrowIfStateTypeIncompatible(Type stateType, Type instanceStateType)
        {
            if (stateType != instanceStateType)
            {
                throw new InvalidOperationException(
                    $"{stateType.FullName} is not compatible with the journey's state type ({instanceStateType.FullName}).");
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

        private JourneyDescriptor? ResolveJourneyDescriptor(
            ActionContext actionContext,
            bool throwIfNotFound = true)
        {
            var descriptor = JourneyDescriptor.FromActionContext(actionContext);

            if (descriptor == null && throwIfNotFound)
            {
                throw new InvalidOperationException("No journey metadata found on action.");
            }

            return descriptor;
        }
    }
}
