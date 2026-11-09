using System.Threading;
using Cysharp.Threading.Tasks;
using UniState;

namespace Zoo.Application
{
    internal sealed partial class MovementMachine
    {
        [StateBehaviour(ProhibitReturnToState = true)]
        private sealed class RestState : StateBase
        {
            private readonly MovementMachine _owner;

            public RestState(MovementMachine owner)
            {
                _owner = owner;
            }

            public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
            {
                _owner._physics.Stop(_owner._animal.Id);
                _owner._animal.SetBehavior(_owner._label + " / Rest");
                await _owner._clock.Delay(_owner._plan.Rest, token);
                return _owner.Arrived ? Transition.GoToExit() : Transition.GoTo<TravelState>();
            }
        }
    }
}
