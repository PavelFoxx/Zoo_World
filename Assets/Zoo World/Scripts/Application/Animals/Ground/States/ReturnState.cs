using System.Threading;
using Cysharp.Threading.Tasks;
using UniState;

namespace Zoo.Application
{
    public sealed partial class GroundAnimalBrain
    {
        [StateBehaviour(ProhibitReturnToState = true)]
        private sealed class ReturnState : StateBase
        {
            private const float ReturnMargin = 1.5f;
            private readonly GroundAnimalBrain _owner;

            public ReturnState(GroundAnimalBrain owner)
            {
                _owner = owner;
            }

            public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
            {
                await _owner.RunActivity(Return, token);
                _owner._returnRequested = !_owner._bounds.Contains(_owner._animal.Pose.CurrentValue.Position);
                return _owner._returnRequested ? Transition.GoTo<ReturnState>() : Transition.GoTo<WanderState>();
            }

            private async UniTask Return(CancellationToken token)
            {
                try
                {
                    _owner._animal.SetBehavior("Return to ground");
                    token.ThrowIfCancellationRequested();
                    var position = _owner._animal.Pose.CurrentValue.Position;
                    position.Y = _owner._bounds.Min.Y;
                    var target = _owner._bounds.Clamp(position, ReturnMargin);
                    var path = await _owner._navigation.FindPath(_owner._animal.Pose.CurrentValue.Position,
                        target, _owner._animal.Definition.NavigationProfile, token);
                    token.ThrowIfCancellationRequested();
                    if (path.Length < 2 || !_owner._bounds.Contains(path[path.Length - 1]))
                    {
                        // The closest point inside the play area may be occupied by an obstacle.
                        target = _owner._bounds.Sample(_owner._random, ReturnMargin);
                        path = await _owner._navigation.FindPath(_owner._animal.Pose.CurrentValue.Position,
                            target, _owner._animal.Definition.NavigationProfile, token);
                    }

                    await _owner.FollowPath(path, "Return", token);
                }
                finally
                {
                    _owner._physics.Stop(_owner._animal.Id);
                }
            }
        }
    }
}
