using System;
using R3;
using Zoo.Application;
using Zoo.Domain;

namespace Zoo.Unity
{
    // One presenter per prefab clone; subscriptions belong to the current pool activation.
    public sealed class AnimalViewPresenter : IDisposable
    {
        private CompositeDisposable _bindings;
        public AnimalView View { get; }

        public AnimalViewPresenter(AnimalView view)
        {
            View = view;
            View.Unbound += ClearBindings;
        }

        public void Bind(AnimalViewModel model)
        {
            Unbind();
            View.BeginActivation();
            _bindings = new CompositeDisposable();
            model.Pose.Subscribe(View.Apply).AddTo(_bindings);
            if (model.Model.Definition.Movement is JumpMovement)
            {
                model.MovementStarted.Where(plan => plan.Height > 0).Subscribe(View.Jump).AddTo(_bindings);
            }

            model.Ate.Where(meal => !string.IsNullOrEmpty(meal.Message))
                .Subscribe(meal => View.ShowLabel(meal.Message)).AddTo(_bindings);
        }

        private void ClearBindings()
        {
            _bindings?.Dispose();
            _bindings = null;
        }

        public void Unbind()
        {
            ClearBindings();
            if (View != null)
            {
                View.Unbind();
            }
        }

        public void Dispose()
        {
            Unbind();
            if (View != null)
            {
                View.Unbound -= ClearBindings;
            }
        }
    }
}
