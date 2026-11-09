using System.Numerics;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UniState;

namespace Zoo.Application
{
    internal sealed partial class MovementMachine
    {
        [StateBehaviour(ProhibitReturnToState = true)]
        private sealed class TravelState : StateBase
        {
            private readonly MovementMachine _owner;

            public TravelState(MovementMachine owner)
            {
                _owner = owner;
            }

            public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
            {
                if (_owner.Arrived)
                {
                    return Transition.GoToExit();
                }

                var distance = Vector3.Distance(_owner._animal.Pose.CurrentValue.Position, _owner._path[_owner._corner]);
                _owner._plan = _owner._animal.Definition.Movement.Plan(distance);
                _owner._traveling = true;
                _owner._animal.SetBehavior(_owner._label + " / Travel");
                token.ThrowIfCancellationRequested();
                _owner._animal.BeginMovement(_owner._plan);
                token.ThrowIfCancellationRequested();
                _owner.SetDirection(true);
                using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
                var arrived = new UniTaskCompletionSource();
                using var subscription = _owner._arrived.Subscribe(_ => arrived.TrySetResult());
                using var registration = lifetime.Token.Register(() => arrived.TrySetCanceled(lifetime.Token));
                try
                {
                    await UniTask.WhenAny(_owner._clock.Delay(_owner._plan.Duration, lifetime.Token), arrived.Task);
                }
                finally
                {
                    _owner._traveling = false;
                    lifetime.Cancel();
                }

                if (_owner.Arrived)
                {
                    return Transition.GoToExit();
                }

                return _owner._plan.Rest > 0 ? Transition.GoTo<RestState>() : Transition.GoTo<TravelState>();
            }

            public override void Dispose()
            {
                _owner._traveling = false;
                base.Dispose();
            }
        }
    }
}
