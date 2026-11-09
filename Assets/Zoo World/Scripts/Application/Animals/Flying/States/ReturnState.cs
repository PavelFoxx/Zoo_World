using System.Threading;
using Cysharp.Threading.Tasks;
using UniState;

namespace Zoo.Application
{
    public sealed partial class FlyingAnimalBrain
    {
        [StateBehaviour(ProhibitReturnToState = true)]
        private sealed class ReturnState : StateBase
        {
            private const float ReturnMargin = 1.5f;
            private readonly FlyingAnimalBrain _owner;

            public ReturnState(FlyingAnimalBrain owner)
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
                    _owner._animal.SetBehavior("Return");
                    token.ThrowIfCancellationRequested();
                    var target = _owner._bounds.Clamp(_owner._animal.Pose.CurrentValue.Position, ReturnMargin);
                    var path = _owner.FindPath(target);
                    if (path.Length < 2)
                    {
                        target = _owner._bounds.Sample(_owner._random, ReturnMargin);
                        path = _owner.FindPath(target);
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
