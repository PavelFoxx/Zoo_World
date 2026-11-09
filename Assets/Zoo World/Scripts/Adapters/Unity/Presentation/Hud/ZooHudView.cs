using R3;
using UnityEngine;
using UnityEngine.UI;
using Zoo.Application;

namespace Zoo.Unity
{
    public sealed class ZooHudView : MonoBehaviour
    {
        public Text Prey;
        public Text Predators;
        public Text Population;
        private CompositeDisposable _bindings;

        public void Bind(ZooViewModel model)
        {
            Unbind();
            _bindings = new CompositeDisposable();
            model.DeadPrey.Subscribe(v => Prey.text = "PREY LOST   " + v).AddTo(_bindings);
            model.DeadPredators.Subscribe(v => Predators.text = "PREDATORS LOST   " + v).AddTo(_bindings);
            model.Population.Subscribe(v => Population.text = "IN THE HABITAT   " + v).AddTo(_bindings);
        }

        public void Unbind()
        {
            _bindings?.Dispose();
            _bindings = null;
        }

        private void OnDestroy() => Unbind();
    }
}
