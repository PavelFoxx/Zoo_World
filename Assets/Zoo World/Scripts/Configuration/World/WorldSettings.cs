using System;
using System.Collections.Generic;
using Zoo.Application;

namespace Zoo.Configuration
{
    [Serializable]
    public sealed class WorldSettings
    {
        public VectorSettings boundsMin;
        public VectorSettings boundsMax;
        public VectorSettings gravity;
        public float linearDamping;
        public BoxSettings[] boxes;

        public WorldBounds CreateBounds() => new WorldBounds(boundsMin.ToVector(), boundsMax.ToVector());

        public void Validate()
        {
            boundsMin.Validate("boundsMin");
            boundsMax.Validate("boundsMax");
            gravity.Validate("gravity");
            SettingsValidation.NonNegative(linearDamping, "linearDamping");
            SettingsValidation.Require(boundsMax.x > boundsMin.x && boundsMax.z > boundsMin.z, "World bounds must have positive XZ area.");
            SettingsValidation.Require(boxes != null && boxes.Length > 0, "World geometry is missing.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var box in boxes)
            {
                SettingsValidation.Require(box != null, "Null box.");
                box.Validate();
                SettingsValidation.Require(ids.Add(box.id), "Duplicate box id: " + box.id);
            }
        }
    }
}
