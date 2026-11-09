using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Zoo.Application
{
    public interface ISceneLoadingService
    {
        UniTask Load(string scene, CancellationToken token, IProgress<float> progress = null);
        UniTask Unload();
    }
}
