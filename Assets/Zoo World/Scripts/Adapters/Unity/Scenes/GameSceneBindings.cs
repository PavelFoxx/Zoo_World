using System;
using UnityEngine;
using Zoo.Application;
using NVector = System.Numerics.Vector3;

namespace Zoo.Unity
{
    public sealed class GameSceneBindings : MonoBehaviour
    {
        public Camera WorldCamera;
        public Transform AnimalsRoot;
        public Transform PhysicsRoot;
        public ZooHudView Hud;
        public NavigationDataSO NavigationData;
        public NavigationBakeConfigSO BakeSettings;
        public BoxCollider Ground;
        public BoxCollider[] LevelColliders;
        public GameObject PhysicsBodyPrefab;

        public WorldBounds ReadBounds()
        {
            if (Ground == null || !Ground.enabled || !Ground.gameObject.activeInHierarchy)
            {
                throw new InvalidOperationException("Game scene requires an active Ground BoxCollider to mark the play area.");
            }

            var bounds = Ground.bounds;
            return new WorldBounds(new NVector(bounds.min.x, bounds.max.y, bounds.min.z), new NVector(bounds.max.x, bounds.max.y, bounds.max.z));
        }
    }
}
