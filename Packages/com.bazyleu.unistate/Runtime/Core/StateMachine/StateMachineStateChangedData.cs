using System;

namespace UniState
{
    public readonly struct StateMachineStateChangedData
    {
        public Type PreviousStateType { get; }
        public Type CurrentStateType { get; }
        public StateTransitionInfo PreviousTransition { get; }
        public StateTransitionInfo CurrentTransition { get; }
        public StateTransitionInfo RequestedTransition { get; }
        public StateMachineStateChangeType ChangeType { get; }

        public StateMachineStateChangedData(
            Type previousStateType,
            Type currentStateType,
            StateTransitionInfo previousTransition,
            StateTransitionInfo currentTransition,
            StateTransitionInfo requestedTransition,
            StateMachineStateChangeType changeType)
        {
            PreviousStateType = previousStateType;
            CurrentStateType = currentStateType;
            PreviousTransition = previousTransition;
            CurrentTransition = currentTransition;
            RequestedTransition = requestedTransition;
            ChangeType = changeType;
        }
    }
}
