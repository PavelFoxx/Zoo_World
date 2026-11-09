using R3;
using UnityEngine;
using UnityEngine.UI;
using Zoo.Application;
using Zenject;

namespace Zoo.Unity
{
    public sealed class LoadingScreenView : MonoBehaviour
    {
        public CanvasGroup Group;
        public Text Stage;
        public Text Percentage;
        public Image Fill;
        public Button Retry;
        private System.IDisposable _binding;
        private LoadingScreenViewModel _model;

        [Inject]
        public void Bind(LoadingScreenViewModel model)
        {
            Unbind();
            _model = model;
            Retry.onClick.AddListener(RetryLoading);
            _binding = model.Status.Subscribe(Apply);
        }

        private void Apply(LoadingStatus status)
        {
            Group.alpha = status.Visible ? 1 : 0;
            Group.blocksRaycasts = status.Visible;
            Group.interactable = status.Visible;
            Stage.text = status.Error ?? status.Stage;
            Percentage.text = Mathf.RoundToInt(status.Progress * 100) + "%";
            Fill.fillAmount = status.Progress;
            Retry.gameObject.SetActive(status.Error != null);
        }

        private void RetryLoading() => _model.Retry();

        public void Unbind()
        {
            _binding?.Dispose();
            _binding = null;
            if (Retry != null)
            {
                Retry.onClick.RemoveListener(RetryLoading);
            }

            _model = null;
        }

        private void OnDestroy() => Unbind();
    }
}
