using System.Threading;
using Cysharp.Threading.Tasks;

namespace Zoo.Application
{
    public interface IGameSessionHost
    {
        UniTask Prepare(CancellationToken token);
        void Start();
        void Stop();
    }
}
