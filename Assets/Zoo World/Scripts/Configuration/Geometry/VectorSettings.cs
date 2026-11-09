using System;
using System.Numerics;

namespace Zoo.Configuration
{
    [Serializable]
    public struct VectorSettings
    {
        public float x;
        public float y;
        public float z;

        public VectorSettings(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3 ToVector() => new Vector3(x, y, z);

        public void Validate(string name)
        {
            SettingsValidation.Finite(x, name + ".x");
            SettingsValidation.Finite(y, name + ".y");
            SettingsValidation.Finite(z, name + ".z");
        }
    }
}
