# UniState portability patch

Upstream: https://github.com/bazyleu/UniState, version 1.12.1, commit `cd7788b4bdc7049315bbf3957da70ffe9f1cfffb`.

The embedded package preserves the upstream state machine and public API. Its MIT license is included in `LICENSE`.

- Core assembly references UniTask only and uses `noEngineReferences: true`.
- DI integrations use a separate assembly with their original conditional symbols and package version defines. For Zenject imported under Assets, define `UNISTATE_ZENJECT_SUPPORT` when using that optional adapter. Zoo's per-NPC resolver does not need the adapter.
- Default `StateMachine.HandleError` rethrows the original exception using `ExceptionDispatchInfo`, instead of calling `UnityEngine.Debug.LogException`. The owning simulation observes failures. This deliberately changes the upstream log-and-recover default into fail-fast behavior.

`Tests/Headless` compiles these same Core sources against the official .NET UniTask package. Keep this small patch when updating upstream; do not edit the generated PackageCache copy.
