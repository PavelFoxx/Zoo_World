using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using Zoo.Unity;

namespace Zoo.Editor
{
    internal sealed class NavigationBakeSource
    {
        private static readonly int[] BoxTriangles =
        {
            4,
            5,
            6,
            4,
            6,
            7,
            0,
            2,
            1,
            0,
            3,
            2,
            0,
            4,
            7,
            0,
            7,
            3,
            1,
            2,
            6,
            1,
            6,
            5,
            0,
            1,
            5,
            0,
            5,
            4,
            3,
            7,
            6,
            3,
            6,
            2
        };
        private static readonly Vector3[] BoxCorners =
        {
            new Vector3(-1, -1, -1),
            new Vector3(-1, -1, 1),
            new Vector3(1, -1, 1),
            new Vector3(1, -1, -1),
            new Vector3(-1, 1, -1),
            new Vector3(-1, 1, 1),
            new Vector3(1, 1, 1),
            new Vector3(1, 1, -1)
        };
        public float[] Vertices { get; }

        public int[] Triangles { get; }

        public string Hash { get; }

        private NavigationBakeSource(float[] vertices, int[] triangles, NavigationBakeConfigSO settings)
        {
            Vertices = vertices;
            Triangles = triangles;
            Hash = ComputeHash(settings);
        }

        public static NavigationBakeSource Capture(GameSceneBindings scene)
        {
            var vertices = new List<float>();
            var triangles = new List<int>();
            foreach (var box in scene.LevelColliders)
            {
                if (box == null || box.isTrigger || !box.enabled)
                {
                    throw new InvalidOperationException("Navigation requires enabled solid BoxColliders.");
                }

                var offset = vertices.Count / 3;
                foreach (var corner in BoxCorners)
                {
                    var point = box.transform.TransformPoint(box.center + Vector3.Scale(corner, box.size) * 0.5f);
                    vertices.Add(point.x);
                    vertices.Add(point.y);
                    vertices.Add(point.z);
                }

                foreach (var index in BoxTriangles)
                {
                    triangles.Add(offset + index);
                }
            }

            return new NavigationBakeSource(vertices.ToArray(), triangles.ToArray(), scene.BakeSettings);
        }

        private string ComputeHash(NavigationBakeConfigSO settings)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            foreach (var vertex in Vertices)
            {
                writer.Write(vertex);
            }

            foreach (var index in Triangles)
            {
                writer.Write(index);
            }

            writer.Write(settings.CellSize);
            foreach (var profile in settings.Profiles)
            {
                writer.Write(profile.ProfileId);
                writer.Write(profile.Radius);
                writer.Write(profile.Height);
                writer.Write(profile.MaxClimb);
                writer.Write(profile.MaxSlope);
            }

            writer.Flush();
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream.ToArray()));
        }
    }
}
