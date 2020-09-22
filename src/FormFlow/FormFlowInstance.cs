using System;
using System.Collections.Generic;
using FormFlow.State;

namespace FormFlow
{
    public class FormFlowInstance
    {
        private readonly IUserInstanceStateProvider _stateProvider;

        private protected FormFlowInstance(
            IUserInstanceStateProvider stateProvider,
            string key,
            FormFlowInstanceId instanceId,
            Type stateType,
            object state,
            IReadOnlyDictionary<object, object> properties,
            bool completed = false)
        {
            _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
            Key = key ?? throw new ArgumentNullException(nameof(key));
            StateType = stateType ?? throw new ArgumentNullException(nameof(stateType));
            InstanceId = instanceId;
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            State = state ?? throw new ArgumentNullException(nameof(state));
            Completed = completed;
        }

        public bool Completed { get; private set; }

        public bool Deleted { get; private set; }

        public string Key { get; }

        public FormFlowInstanceId InstanceId { get; }

        public IReadOnlyDictionary<object, object> Properties { get; }

        public object State { get; private set; }

        public Type StateType { get; }

        public static FormFlowInstance Create(
            IUserInstanceStateProvider stateProvider,
            string key,
            FormFlowInstanceId instanceId,
            Type stateType,
            object state,
            IReadOnlyDictionary<object, object> properties,
            bool completed = false)
        {
            var genericType = typeof(FormFlowInstance<>).MakeGenericType(stateType);

            return (FormFlowInstance)Activator.CreateInstance(
                genericType,
                stateProvider,
                key,
                instanceId,
                state,
                properties,
                completed);
        }

        public void Complete()
        {
            if (Completed)
            {
                return;
            }

            if (Deleted)
            {
                throw new InvalidOperationException("Instance has been deleted.");
            }

            _stateProvider.CompleteInstance(InstanceId);
            Completed = true;
        }

        public void Delete()
        {
            if (Deleted)
            {
                return;
            }

            _stateProvider.DeleteInstance(InstanceId);
            Deleted = true;
        }

        internal static bool IsFormFlowInstanceType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type == typeof(FormFlowInstance) ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(FormFlowInstance<>));
        }

        protected void UpdateState(object state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.GetType() != StateType)
            {
                throw new ArgumentException($"State must be type: '{StateType.FullName}'.", nameof(state));
            }

            if (Completed)
            {
                throw new InvalidOperationException("Instance has been completed.");
            }

            if (Deleted)
            {
                throw new InvalidOperationException("Instance has been deleted.");
            }

            _stateProvider.UpdateInstanceState(InstanceId, state);
            State = state;
        }
    }

    public sealed class FormFlowInstance<TState> : FormFlowInstance
    {
        public FormFlowInstance(
            IUserInstanceStateProvider stateProvider,
            string key,
            FormFlowInstanceId instanceId,
            TState state,
            IReadOnlyDictionary<object, object> properties,
            bool completed = false)
            : base(stateProvider, key, instanceId, typeof(TState), state, properties, completed)
        {
        }

        public new TState State => (TState)base.State;

        public void UpdateState(TState state) => UpdateState((object)state);

        public void UpdateState(Action<TState> update)
        {
            update(State);
            UpdateState(State);
        }

        public void UpdateState(Func<TState, TState> update)
        {
            if (update == null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            var newState = update(State);
            UpdateState(newState);
        }
    }
}
