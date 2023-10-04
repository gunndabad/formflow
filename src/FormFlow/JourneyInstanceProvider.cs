using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FormFlow.State;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace FormFlow;

public class JourneyInstanceProvider
{
    private readonly IUserInstanceStateProvider _stateProvider;
    private readonly IOptions<FormFlowOptions> _optionsAccessor;

    public JourneyInstanceProvider(
        IUserInstanceStateProvider stateProvider,
        IOptions<FormFlowOptions> optionsAccessor)
    {
        ArgumentNullException.ThrowIfNull(stateProvider);
        ArgumentNullException.ThrowIfNull(optionsAccessor);

        _stateProvider = stateProvider;
        _optionsAccessor = optionsAccessor;
    }

    public JourneyInstance CreateInstance(
        ActionContext actionContext,
        object state,
        IReadOnlyDictionary<object, object>? properties = null)
    {
        ArgumentNullException.ThrowIfNull(actionContext);
        ArgumentNullException.ThrowIfNull(state);

        var journeyDescriptor = ResolveJourneyDescriptor(actionContext)!;

        ThrowIfStateTypeIncompatible(state.GetType(), journeyDescriptor);

        var valueProvider = CreateValueProvider(actionContext);

        var instanceId = JourneyInstanceId.Create(
            journeyDescriptor,
            valueProvider);

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
        ActionContext actionContext,
        TState state,
        IReadOnlyDictionary<object, object>? properties = null)
    {
        ArgumentNullException.ThrowIfNull(actionContext);
        ArgumentNullException.ThrowIfNull(state);

        var journeyDescriptor = ResolveJourneyDescriptor(actionContext)!;

        ThrowIfStateTypeIncompatible(typeof(TState), journeyDescriptor);

        var valueProvider = CreateValueProvider(actionContext);

        var instanceId = JourneyInstanceId.Create(
            journeyDescriptor,
            valueProvider);

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

    public JourneyInstance? GetInstance(ActionContext actionContext)
    {
        ArgumentNullException.ThrowIfNull(actionContext);

        // Throw if JourneyDescriptor is missing
        ResolveJourneyDescriptor(actionContext);

        if (TryResolveExistingInstance(actionContext, out var instance))
        {
            return instance;
        }
        else
        {
            return null;
        }
    }

    public JourneyInstance<TState>? GetInstance<TState>(ActionContext actionContext)
    {
        ArgumentNullException.ThrowIfNull(actionContext);

        // Throw if JourneyDescriptor is missing
        ResolveJourneyDescriptor(actionContext);

        if (TryResolveExistingInstance(actionContext, out var instance))
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
        ActionContext actionContext,
        Func<object> createState,
        IReadOnlyDictionary<object, object>? properties = null)
    {
        ArgumentNullException.ThrowIfNull(actionContext);
        ArgumentNullException.ThrowIfNull(createState);

        var journeyDescriptor = ResolveJourneyDescriptor(actionContext)!;

        if (TryResolveExistingInstance(actionContext, out var instance))
        {
            return instance;
        }

        var newState = createState();

        ThrowIfStateTypeIncompatible(newState.GetType(), journeyDescriptor);

        return CreateInstance(actionContext, newState, properties);
    }

    public JourneyInstance<TState> GetOrCreateInstance<TState>(
        ActionContext actionContext,
        IReadOnlyDictionary<object, object>? properties = null)
        where TState : new()
    {
        return GetOrCreateInstance<TState>(actionContext, () => new TState(), properties);
    }

    public JourneyInstance<TState> GetOrCreateInstance<TState>(
        ActionContext actionContext,
        Func<TState> createState,
        IReadOnlyDictionary<object, object>? properties = null)
    {
        ArgumentNullException.ThrowIfNull(actionContext);
        ArgumentNullException.ThrowIfNull(createState);

        var journeyDescriptor = ResolveJourneyDescriptor(actionContext)!;

        ThrowIfStateTypeIncompatible(typeof(TState), journeyDescriptor);

        if (TryResolveExistingInstance(actionContext, out var instance))
        {
            return (JourneyInstance<TState>)instance;
        }

        var newState = createState();

        return CreateInstance(actionContext, newState, properties);
    }

    public async Task<JourneyInstance> GetOrCreateInstanceAsync(
        ActionContext actionContext,
        Func<Task<object>> createState,
        IReadOnlyDictionary<object, object>? properties = null)
    {
        ArgumentNullException.ThrowIfNull(actionContext);
        ArgumentNullException.ThrowIfNull(createState);

        var journeyDescriptor = ResolveJourneyDescriptor(actionContext)!;

        if (TryResolveExistingInstance(actionContext, out var instance))
        {
            return instance;
        }

        var newState = await createState();

        ThrowIfStateTypeIncompatible(newState.GetType(), journeyDescriptor);

        return CreateInstance(actionContext, newState, properties);
    }

    public async Task<JourneyInstance<TState>> GetOrCreateInstanceAsync<TState>(
        ActionContext actionContext,
        Func<Task<TState>> createState,
        IReadOnlyDictionary<object, object>? properties = null)
    {
        ArgumentNullException.ThrowIfNull(actionContext);
        ArgumentNullException.ThrowIfNull(createState);

        var journeyDescriptor = ResolveJourneyDescriptor(actionContext)!;

        ThrowIfStateTypeIncompatible(typeof(TState), journeyDescriptor);

        if (TryResolveExistingInstance(actionContext, out var instance))
        {
            return (JourneyInstance<TState>)instance;
        }

        var newState = await createState();

        return CreateInstance(actionContext, newState, properties);
    }

    public bool IsCurrentInstance(ActionContext actionContext, JourneyInstance instance)
    {
        ArgumentNullException.ThrowIfNull(actionContext);
        ArgumentNullException.ThrowIfNull(instance);

        return IsCurrentInstance(actionContext, instance.InstanceId);
    }

    public bool IsCurrentInstance(ActionContext actionContext, JourneyInstanceId instanceId)
    {
        ArgumentNullException.ThrowIfNull(actionContext);

        TryResolveExistingInstance(actionContext, out var currentInstance);
        return currentInstance?.InstanceId == instanceId;
    }

    internal JourneyDescriptor? ResolveJourneyDescriptor(
        ActionContext actionContext,
        bool throwIfNotFound = true)
    {
        var actionJourneyMetadata = actionContext.GetActionJourneyMetadata();
        if (actionJourneyMetadata == null)
        {
            if (throwIfNotFound)
            {
                throw new InvalidOperationException("No journey metadata found on action.");
            }
            else
            {
                return null;
            }
        }

        var journeyDescriptor = _optionsAccessor.Value.JourneyRegistry.GetJourneyByName(actionJourneyMetadata.JourneyName);
        if (journeyDescriptor is null)
        {
            throw new InvalidOperationException($"No journey named '{actionJourneyMetadata.JourneyName}' found in JourneyRegistry.");
        }

        return journeyDescriptor;
    }

    internal bool TryResolveExistingInstance(ActionContext actionContext, [MaybeNullWhen(false)] out JourneyInstance instance)
    {
        ArgumentNullException.ThrowIfNull(actionContext);

        instance = default;

        // If we've already created a JourneyInstance for this request, use that
        if (actionContext.HttpContext.Items.TryGetValue(typeof(JourneyInstance), out var existingInstanceObj) && existingInstanceObj is not null)
        {
            instance = (JourneyInstance)existingInstanceObj;
            return true;
        }

        var journeyDescriptor = ResolveJourneyDescriptor(actionContext, throwIfNotFound: false);

        if (journeyDescriptor == null)
        {
            return false;
        }

        var valueProvider = CreateValueProvider(actionContext);

        if (!JourneyInstanceId.TryResolve(
            journeyDescriptor,
            valueProvider,
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

        actionContext.HttpContext.Items.TryAdd(typeof(JourneyInstance), persistedInstance);

        // There's a race here; another thread could resolve an instance and beat us to adding it to cache.
        // Ensure we return the cached instance.
        instance = (JourneyInstance)actionContext.HttpContext.Items[typeof(JourneyInstance)]!;

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

    private IValueProvider CreateValueProvider(ActionContext actionContext)
    {
        if (actionContext.HttpContext.Items.TryGetValue(typeof(ValueProviderCacheEntry), out var cacheEntry) && cacheEntry is not null)
        {
            return ((ValueProviderCacheEntry)cacheEntry).ValueProvider;
        }

        var valueProviders = new List<IValueProvider>();

        foreach (var valueProviderFactory in _optionsAccessor.Value.ValueProviderFactories)
        {
            var ctx = new ValueProviderFactoryContext(actionContext);

            // All the in-box implementations of IValueProviderFactory complete synchronously
            // and making this method async forces the entire API to be sync.
            valueProviderFactory.CreateValueProviderAsync(ctx).GetAwaiter().GetResult();

            valueProviders.AddRange(ctx.ValueProviders);
        }

        var valueProvider = new CompositeValueProvider(valueProviders);

        actionContext.HttpContext.Items.TryAdd(
            typeof(ValueProviderCacheEntry),
            new ValueProviderCacheEntry(valueProvider));

        return valueProvider;
    }

    //private ActionContext ResolveActionContext()
    //{
    //    var actionContext = _actionContextAccessor.ActionContext;

    //    if (actionContext == null)
    //    {
    //        throw new InvalidOperationException("No active ActionContext.");
    //    }

    //    return actionContext;
    //}

    private class ValueProviderCacheEntry
    {
        public ValueProviderCacheEntry(IValueProvider valueProvider)
        {
            ValueProvider = valueProvider;
        }

        public IValueProvider ValueProvider { get; }
    }
}
