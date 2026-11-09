using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;

namespace Zoo.Headless.Physics
{
    internal struct ContactCallbacks : INarrowPhaseCallbacks
    {
        private readonly BepuPhysicsWorld _owner;

        public ContactCallbacks(BepuPhysicsWorld owner) => _owner = owner;

        public void Initialize(Simulation simulation)
        {
        }

        public void Dispose()
        {
        }

        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin) => a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;

        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) => true;

        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial)
            where TManifold : unmanaged, IContactManifold<TManifold>
        {
            pairMaterial = new PairMaterialProperties
            {
                FrictionCoefficient = 0,
                MaximumRecoveryVelocity = 2,
                SpringSettings = new SpringSettings(30, 1)
            };
            if (pair.A.Mobility == CollidableMobility.Dynamic && pair.B.Mobility == CollidableMobility.Dynamic)
            {
                for (var i = 0; i < manifold.Count; i++)
                {
                    if (manifold.GetDepth(ref manifold, i) >= 0)
                    {
                        _owner.RecordContact(pair.A.BodyHandle, pair.B.BodyHandle);
                        break;
                    }
                }
            }

            return true;
        }

        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold) => true;
    }
}
