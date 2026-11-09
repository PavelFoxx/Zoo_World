using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;
using Zoo.Unity;

namespace Zoo.Editor
{
    public sealed class NavigationBuildValidator : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null)
            {
                return;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var bindings in root.GetComponentsInChildren<GameSceneBindings>(true))
                {
                    try
                    {
                        NavigationDataValidation.EnsureCurrent(bindings);
                    }
                    catch (InvalidOperationException error)
                    {
                        throw new BuildFailedException(error.Message);
                    }
                }
            }
        }
    }
}
