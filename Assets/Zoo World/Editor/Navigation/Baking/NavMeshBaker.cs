using System.IO;
using DotRecast.Core;
using DotRecast.Recast;
using DotRecast.Recast.Geom;
using DotRecast.Detour;
using DotRecast.Detour.Io;

namespace Zoo.Navigation.Baking
{
    public static class NavMeshBaker
    {
        public static byte[] Bake(float[] vertices, int[] triangles, float radius, float height,
            float climb, float slope, float cellSize = 0.15f)
        {
            var geometry = new RcSampleInputGeomProvider(vertices, triangles);
            var config = new RcConfig(
                RcPartition.WATERSHED, cellSize, 0.1f, slope, height, radius, climb,
                2, 8, 12, 1.3f, 6, 6, 1, true, true, true, new RcAreaModification(1), true);
            var result = new RcBuilder().Build(
                geometry, new RcBuilderConfig(config, geometry.GetMeshBoundsMin(), geometry.GetMeshBoundsMax()), false);
            var mesh = result.Mesh;
            var detail = result.MeshDetail;
            for (var i = 0; i < mesh.npolys; i++)
            {
                mesh.flags[i] = 1;
            }

            var data = DtNavMeshBuilder.CreateNavMeshData(new DtNavMeshCreateParams
            {
                verts = mesh.verts,
                vertCount = mesh.nverts,
                polys = mesh.polys,
                polyAreas = mesh.areas,
                polyFlags = mesh.flags,
                polyCount = mesh.npolys,
                nvp = mesh.nvp,
                detailMeshes = detail.meshes,
                detailVerts = detail.verts,
                detailVertsCount = detail.nverts,
                detailTris = detail.tris,
                detailTriCount = detail.ntris,
                walkableHeight = height,
                walkableRadius = radius,
                walkableClimb = climb,
                bmin = mesh.bmin,
                bmax = mesh.bmax,
                cs = cellSize,
                ch = 0.1f,
                buildBvTree = true
            });

            var navigation = new DtNavMesh();
            navigation.Init(data, 6, 0);
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            new DtMeshSetWriter().Write(writer, navigation, RcByteOrder.LITTLE_ENDIAN, false);
            writer.Flush();
            return stream.ToArray();
        }
    }
}
