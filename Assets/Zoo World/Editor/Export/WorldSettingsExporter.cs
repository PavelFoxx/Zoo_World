using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zoo.Configuration;
using Zoo.Unity;

namespace Zoo.Editor
{
    internal static class WorldSettingsExporter
    {
        public static WorldSettings Capture(GameSceneBindings scene)
        {
            var bounds = scene.ReadBounds();
            var prefab = scene.PhysicsBodyPrefab;
            var body = prefab.GetComponent<Rigidbody>();
            var sphere = prefab.GetComponent<SphereCollider>();
            if (body == null || sphere == null || body.isKinematic ||
                body.constraints != RigidbodyConstraints.FreezeRotation ||
                sphere.center != Vector3.zero || sphere.isTrigger ||
                prefab.transform.localScale != Vector3.one ||
                prefab.GetComponentsInChildren<Collider>(true).Length != 1)
            {
                throw new NotSupportedException("JSON physics requires the spherical, rotation-locked animal body prefab.");
            }

            var boxes = new List<BoxSettings>();
            foreach (var collider in scene.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Collider>()))
            {
                if (!collider.enabled || collider.isTrigger || UnityEngine.Physics.GetIgnoreLayerCollision(prefab.layer, collider.gameObject.layer))
                {
                    continue;
                }

                if (!(collider is BoxCollider box) || collider.attachedRigidbody != null)
                {
                    throw new NotSupportedException("Export supports static BoxCollider level geometry: " + collider.name);
                }

                if (collider.sharedMaterial != null && collider.sharedMaterial.bounciness != 0)
                {
                    throw new NotSupportedException("Export supports zero-restitution level materials: " + collider.name);
                }

                boxes.Add(Box(box, "level-" + boxes.Count + "-" + box.name));
            }

            return new WorldSettings
            {
                boundsMin = new VectorSettings(bounds.Min.X, bounds.Min.Y, bounds.Min.Z),
                boundsMax = new VectorSettings(bounds.Max.X, bounds.Max.Y, bounds.Max.Z),
                gravity = Vector(UnityEngine.Physics.gravity),
                linearDamping = body.linearDamping,
                boxes = boxes.ToArray()
            };
        }

        private static BoxSettings Box(BoxCollider box, string id)
        {
            var transform = box.transform;
            var x = transform.TransformVector(Vector3.right).normalized;
            var y = transform.TransformVector(Vector3.up).normalized;
            var z = transform.TransformVector(Vector3.forward).normalized;
            if (Mathf.Abs(Vector3.Dot(x, y)) > 0.0001f || Mathf.Abs(Vector3.Dot(x, z)) > 0.0001f || Mathf.Abs(Vector3.Dot(y, z)) > 0.0001f)
            {
                throw new NotSupportedException("Remove transform shear before exporting box: " + box.name);
            }

            var scale = transform.lossyScale;
            if (scale.x <= 0 || scale.y <= 0 || scale.z <= 0)
            {
                throw new NotSupportedException("Export requires positive BoxCollider scale: " + box.name);
            }

            var rotation = transform.rotation;
            return new BoxSettings
            {
                id = id,
                center = Vector(transform.TransformPoint(box.center)),
                size = Vector(Vector3.Scale(box.size, scale)),
                rotation = new RotationSettings
                {
                    x = rotation.x,
                    y = rotation.y,
                    z = rotation.z,
                    w = rotation.w
                }
            };
        }

        private static VectorSettings Vector(Vector3 value) => new VectorSettings(value.x, value.y, value.z);
    }
}
