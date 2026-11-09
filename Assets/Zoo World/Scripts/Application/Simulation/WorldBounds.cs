using System;
using System.Numerics;

namespace Zoo.Application
{
    public readonly struct WorldBounds
    {
        public readonly Vector3 Min;
        public readonly Vector3 Max;

        public WorldBounds(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public bool Contains(Vector3 point, float margin = 0)
        {
            return point.X >= Min.X + margin && point.X <= Max.X - margin &&
                   point.Z >= Min.Z + margin && point.Z <= Max.Z - margin;
        }

        public Vector3 Clamp(Vector3 point, float margin = 0)
        {
            return new Vector3(
                Math.Max(Min.X + margin, Math.Min(Max.X - margin, point.X)),
                point.Y,
                Math.Max(Min.Z + margin, Math.Min(Max.Z - margin, point.Z)));
        }

        public Vector3 Sample(Random random, float margin)
        {
            return new Vector3(
                Min.X + margin + (float)random.NextDouble() * (Max.X - Min.X - 2 * margin),
                Min.Y,
                Min.Z + margin + (float)random.NextDouble() * (Max.Z - Min.Z - 2 * margin));
        }
    }
}
