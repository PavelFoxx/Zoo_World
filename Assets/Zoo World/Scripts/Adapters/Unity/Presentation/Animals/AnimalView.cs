using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Zoo.Domain;

namespace Zoo.Unity
{
    public sealed class AnimalView : EntityView
    {
        [SerializeField]
        private Transform body;
        [SerializeField]
        private Text label;
        private CancellationTokenSource _lifetime;
        private CancellationTokenSource _labelLifetime;
        private Tween _jump;
        public bool IsLabelVisible => label != null && label.gameObject.activeSelf;
        public event Action Unbound;

        public void BeginActivation()
        {
            Unbind();
            _lifetime = new CancellationTokenSource();
        }

        internal void Apply(EntityPose pose)
        {
            transform.position = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z);
            body.localRotation = new Quaternion(pose.Rotation.X, pose.Rotation.Y, pose.Rotation.Z, pose.Rotation.W);
        }

        internal void Jump(MovementPlan plan)
        {
            _jump?.Kill();
            _jump = DOVirtual.Float(0, 1, plan.Duration, t => body.localPosition = Vector3.up * (4 * plan.Height * t * (1 - t))).SetEase(Ease.Linear);
        }

        internal void ShowLabel(string message)
        {
            _labelLifetime?.Cancel();
            _labelLifetime?.Dispose();
            _labelLifetime = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            label.text = message;
            label.gameObject.SetActive(true);
            HideLabel(_labelLifetime.Token).Forget(Debug.LogException);
        }

        private async UniTask HideLabel(CancellationToken token)
        {
            if (!await UniTask.Delay(TimeSpan.FromSeconds(0.9), cancellationToken: token).SuppressCancellationThrow())
            {
                label.gameObject.SetActive(false);
            }
        }

        public override void Unbind()
        {
            Unbound?.Invoke();
            _lifetime?.Cancel();
            _labelLifetime?.Cancel();
            _labelLifetime?.Dispose();
            _labelLifetime = null;
            _lifetime?.Dispose();
            _lifetime = null;
            _jump?.Kill();
            _jump = null;
            if (body != null)
            {
                body.localPosition = Vector3.zero;
            }

            if (label != null)
            {
                label.text = string.Empty;
                label.gameObject.SetActive(false);
            }
        }

        private void OnDisable() => Unbind();
    }
}
