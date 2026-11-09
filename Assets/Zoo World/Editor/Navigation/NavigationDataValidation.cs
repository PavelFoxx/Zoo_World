using System;
using Zoo.Unity;

namespace Zoo.Editor
{
    public static class NavigationDataValidation
    {
        public static void EnsureCurrent(GameSceneBindings scene)
        {
            var source = NavigationBakeSource.Capture(scene);
            if (scene.NavigationData == null || scene.NavigationData.GeometryHash != source.Hash)
            {
                throw new InvalidOperationException("Navigation is out of date. Run Zoo/Bake current level navigation.");
            }
        }
    }
}
