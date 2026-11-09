using System;
using UnityEditor;
using UnityEngine;
using Zoo.Unity;

namespace Zoo.Editor
{
    internal static class NavigationBakeMenu
    {
        [MenuItem("Zoo/Bake current level navigation")]
        private static void BakeCurrent()
        {
            var scene = UnityEngine.Object.FindFirstObjectByType<GameSceneBindings>();
            if (scene == null)
            {
                throw new InvalidOperationException("Open Game scene first.");
            }

            NavigationAuthoring.Bake(scene);
            AssetDatabase.SaveAssets();
        }
    }
}
