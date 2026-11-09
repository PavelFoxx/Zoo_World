using System;
using System.Numerics;
using R3;

namespace Zoo.Domain
{
    public readonly struct EntityPose : IEquatable<EntityPose>
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public EntityPose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public bool Equals(EntityPose other) => Position == other.Position && Rotation == other.Rotation;

        public override bool Equals(object obj) => obj is EntityPose other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Position, Rotation);
    }
}
