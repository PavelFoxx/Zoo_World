using System;
using System.Collections.Generic;
using UnityEditor;
using Zoo.Navigation.Baking;
using Zoo.Unity;

namespace Zoo.Editor
{
    public static class NavigationAuthoring
    {
        public static void Bake(GameSceneBindings scene)
        {
            var source = NavigationBakeSource.Capture(scene);
            var profiles = new List<NavigationDataSO.Entry>();
            var ids = new HashSet<string>();
            foreach (var profile in scene.BakeSettings.Profiles)
            {
                if (!ids.Add(profile.ProfileId))
                {
                    throw new InvalidOperationException("Duplicate navigation profile.");
                }

                profiles.Add(new NavigationDataSO.Entry
                {
                    ProfileId = profile.ProfileId,
                    Mesh = NavMeshBaker.Bake(source.Vertices, source.Triangles, profile.Radius,
                        profile.Height, profile.MaxClimb, profile.MaxSlope, scene.BakeSettings.CellSize)
                });
            }

            scene.NavigationData.Profiles = profiles.ToArray();
            scene.NavigationData.GeometryHash = source.Hash;
            EditorUtility.SetDirty(scene.NavigationData);
        }
    }
}
