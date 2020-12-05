using System;
using System.Collections.Generic;
using FormFlow.State;

namespace FormFlow
{
    public class JourneyInstance
    {
        private readonly IUserInstanceStateProvider _stateProvider;

        internal JourneyInstance(
            IUserInstanceStateProvider stateProvider,
            string journeyName,
            JourneyInstanceId instanceId,
            Type stateType,
            object state,
            IReadOnlyDictionary<object, object> properties,
            bool completed = false)
        {
            _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
            JourneyName = journeyName ?? throw new ArgumentNullException(nameof(journeyName));
            StateType = stateType ?? throw new ArgumentNullException(nameof(stateType));
            InstanceId = instanceId;
            Properties = properties ?? PropertiesBuilder.CreateEmpty();
            State = state ?? throw new ArgumentNullException(nameof(state));
            Completed = completed;
        }

        public bool Completed { get; internal set; }

        public bool Deleted { get; internal set; }

        public string JourneyName { get; }

        public JourneyInstanceId InstanceId { get; }

        public IReadOnlyDictionary<object, object> Properties { get; }

        public object State { get; private set; }

        public Type StateType { get; }

        public static JourneyInstance Create(
            IUserInstanceStateProvider stateProvider,
            string journeyName,
            JourneyInstanceId instanceId,
            Type stateType,
            object state,
            IReadOnlyDictionary<object, object> properties,
            bool completed = false)
        {
            var genericType = typeof(JourneyInstance<>).MakeGenericType(stateType);

            return (JourneyInstance)Activator.CreateInstance(
                genericType,
                stateProvider,
                journeyName,
                instanceId,
                state,
                properties,
                completed)!;
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

        internal static bool IsJourneyInstanceType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type == typeof(JourneyInstance) ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(JourneyInstance<>));
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

    public sealed class JourneyInstance<TState> : JourneyInstance
    {
        public JourneyInstance(
            IUserInstanceStateProvider stateProvider,
            string journeyName,
            JourneyInstanceId instanceId,
            TState state,
            IReadOnlyDictionary<object, object> properties,
            bool completed = false)
            : base(stateProvider, journeyName, instanceId, typeof(TState), state!, properties, completed)
        {
        }

        public new TState State => (TState)base.State;

        public void UpdateState(TState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            UpdateState((object)state);
        }

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
