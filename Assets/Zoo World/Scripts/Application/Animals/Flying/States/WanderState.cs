using System.Threading;
using Cysharp.Threading.Tasks;
using UniState;

namespace Zoo.Application
{
    public sealed partial class FlyingAnimalBrain
    {
        [StateBehaviour(ProhibitReturnToState = true)]
        private sealed class WanderState : StateBase
        {
            private const float WanderBeyondBounds = 1f;
            private readonly FlyingAnimalBrain _owner;

            public WanderState(FlyingAnimalBrain owner)
            {
                _owner = owner;
            }

            public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
            {
                await _owner.RunActivity(Wander, token);
                return _owner._returnRequested ? Transition.GoTo<ReturnState>() : Transition.GoTo<WanderState>();
            }

            private async UniTask Wander(CancellationToken token)
            {
                try
                {
                    _owner._animal.SetBehavior("Wander");
                    token.ThrowIfCancellationRequested();
                    var target = _owner._bounds.Sample(_owner._random, -WanderBeyondBounds);
                    await _owner.FollowPath(_owner.FindPath(target), "Wander", token);
                }
                finally
                {
                    _owner._physics.Stop(_owner._animal.Id);
                }
            }
        }
    }
}
