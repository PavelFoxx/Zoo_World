using System.Threading;
using Cysharp.Threading.Tasks;
using UniState;

namespace Zoo.Application
{
    public sealed partial class GroundAnimalBrain
    {
        [StateBehaviour(ProhibitReturnToState = true)]
        private sealed class WanderState : StateBase
        {
            private const float WanderBeyondBounds = 1f;
            private readonly GroundAnimalBrain _owner;

            public WanderState(GroundAnimalBrain owner)
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
                    var path = await _owner._navigation.FindPath(_owner._animal.Pose.CurrentValue.Position,
                        target, _owner._animal.Definition.NavigationProfile, token);
                    await _owner.FollowPath(path, "Wander", token);
                }
                finally
                {
                    _owner._physics.Stop(_owner._animal.Id);
                }
            }
        }
    }
}
