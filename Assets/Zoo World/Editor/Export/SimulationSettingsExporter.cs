using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zoo.Application;
using Zoo.Composition;
using Zoo.Configuration;
using Zoo.Unity;

namespace Zoo.Editor
{
    public static class SimulationSettingsExporter
    {
        private const string BootstrapPath = "Assets/Zoo World/Scenes/Bootstrap.unity";
        private const string GamePath = "Assets/Zoo World/Scenes/Game.unity";
        [MenuItem("Zoo/Export simulation settings to JSON")]
        public static void ExportMenu()
        {
            var destination = EditorUtility.SaveFilePanel("Export simulation settings", "Exports", "zoo-world", "json");
            if (!string.IsNullOrEmpty(destination))
            {
                Export(destination);
                Debug.Log("Simulation configuration exported to " + destination);
            }
        }

        public static void Export(string destination)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Stop Play Mode before exporting authored configuration.");
            }

            var bootstrapScene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(BootstrapPath);
            var ownsBootstrap = !bootstrapScene.IsValid() || !bootstrapScene.isLoaded;
            if (ownsBootstrap)
            {
                bootstrapScene = EditorSceneManager.OpenPreviewScene(BootstrapPath);
            }

            try
            {
                var bootstrap = bootstrapScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BootstrapInstaller>(true)).Single();
                var gameScene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(GamePath);
                var ownsGame = !gameScene.IsValid() || !gameScene.isLoaded;
                if (ownsGame)
                {
                    gameScene = EditorSceneManager.OpenPreviewScene(GamePath);
                }

                try
                {
                    var scene = gameScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<GameSceneBindings>(true)).Single();
                    var snapshot = Capture(bootstrap, scene);
                    var json = JsonUtility.ToJson(snapshot, true);
                    WriteAtomically(destination, json);
                }
                finally
                {
                    if (ownsGame)
                    {
                        EditorSceneManager.ClosePreviewScene(gameScene);
                    }
                }
            }
            finally
            {
                if (ownsBootstrap)
                {
                    EditorSceneManager.ClosePreviewScene(bootstrapScene);
                }
            }
        }

        public static SimulationSettings Capture(BootstrapInstaller bootstrap, GameSceneBindings scene)
        {
            bootstrap.Catalog.CreateDefinitions();
            NavigationDataValidation.EnsureCurrent(scene);
            var snapshot = new SimulationSettings
            {
                schemaVersion = SimulationSettings.CurrentSchemaVersion,
                seed = bootstrap.Seed,
                randomizeSeed = bootstrap.RandomizeSeed,
                fixedDelta = GameSession.FixedDelta,
                world = WorldSettingsExporter.Capture(scene),
                animals = bootstrap.Catalog.Animals.Select(AnimalSettingsExporter.Capture).ToArray(),
                navigation = new NavigationSettings
                {
                    format = "dotrecast-2026.3.1",
                    geometryHash = scene.NavigationData.GeometryHash,
                    cellSize = scene.BakeSettings.CellSize,
                    profiles = scene.BakeSettings.Profiles.Select(profile => new NavigationProfileSettings
                    {
                        id = profile.ProfileId,
                        radius = profile.Radius,
                        height = profile.Height,
                        maxClimb = profile.MaxClimb,
                        maxSlope = profile.MaxSlope,
                        meshBase64 = Convert.ToBase64String(
                            scene.NavigationData.Profiles.Single(data => data.ProfileId == profile.ProfileId).Mesh)
                    }).ToArray()
                }
            };
            snapshot.CreateDefinitions();
            return snapshot;
        }

        private static void WriteAtomically(string destination, string json)
        {
            var path = Path.GetFullPath(destination);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, json, new UTF8Encoding(false));
                if (File.Exists(path))
                {
                    File.Replace(temporary, path, null);
                }
                else
                {
                    File.Move(temporary, path);
                }
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }
    }
}
