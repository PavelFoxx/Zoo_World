using System;
using System.Numerics;
using R3;

namespace Zoo.Domain
{
    public abstract class Entity : IDisposable
    {
        private readonly ReactiveProperty<EntityPose> _pose;
        public int Id { get; }

        public string TypeId { get; }

        public EntityKind Kind { get; }

        public bool IsActive { get; private set; } = true;
        public ReadOnlyReactiveProperty<EntityPose> Pose => _pose;

        protected Entity(int id, string typeId, EntityKind kind, Vector3 position)
        {
            Id = id;
            TypeId = typeId;
            Kind = kind;
            _pose = new ReactiveProperty<EntityPose>(new EntityPose(position, Quaternion.Identity));
        }

        public void AcceptPhysicsPose(EntityPose pose)
        {
            if (IsActive)
            {
                _pose.Value = pose;
            }
        }

        internal void Deactivate() => IsActive = false;

        public virtual void Dispose()
        {
            IsActive = false;
            _pose.Dispose();
        }
    }
}
