using System;
using System.Numerics;
using BepuPhysics;
using BepuUtilities;

namespace Zoo.Headless.Physics
{
    internal struct GravityIntegrator : IPoseIntegratorCallbacks
    {
        private readonly Vector3 _gravity;
        private readonly float _linearDamping;
        private Vector3Wide _gravityStep;
        private Vector<float> _dampingStep;

        public GravityIntegrator(Vector3 gravity, float linearDamping) : this()
        {
            _gravity = gravity;
            _linearDamping = linearDamping;
        }

        public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
        public bool AllowSubstepsForUnconstrainedBodies => false;
        public bool IntegrateVelocityForKinematics => false;

        public void Initialize(Simulation simulation)
        {
        }

        public void PrepareForIntegration(float delta)
        {
            _gravityStep = Vector3Wide.Broadcast(_gravity * delta);
            _dampingStep = new Vector<float>(Math.Max(0, 1 - _linearDamping * delta));
        }

        public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> delta, ref BodyVelocityWide velocity)
        {
            velocity.Linear = (velocity.Linear + _gravityStep) * _dampingStep;
        }
    }
}
