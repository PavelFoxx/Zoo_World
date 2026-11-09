using System;
using System.Numerics;

namespace Zoo.Configuration
{
    [Serializable]
    public struct RotationSettings
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Quaternion ToQuaternion() => new Quaternion(x, y, z, w);

        public void Validate()
        {
            SettingsValidation.Finite(x, "rotation.x");
            SettingsValidation.Finite(y, "rotation.y");
            SettingsValidation.Finite(z, "rotation.z");
            SettingsValidation.Finite(w, "rotation.w");
            SettingsValidation.Require(Math.Abs(ToQuaternion().LengthSquared() - 1) < 0.001f, "Box rotation must be normalized.");
        }
    }
}
