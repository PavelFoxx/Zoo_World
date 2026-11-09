using System;

namespace Zoo.Configuration
{
    [Serializable]
    public sealed class BoxSettings
    {
        public string id;
        public VectorSettings center;
        public VectorSettings size;
        public RotationSettings rotation;

        public void Validate()
        {
            SettingsValidation.Identifier(id, "Box id");
            center.Validate("box.center");
            size.Validate("box.size");
            SettingsValidation.Require(size.x > 0 && size.y > 0 && size.z > 0, "Box dimensions must be positive.");
            rotation.Validate();
        }
    }
}
