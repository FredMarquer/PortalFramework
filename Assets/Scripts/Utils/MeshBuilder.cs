using UnityEngine;
using UnityEngine.Assertions;

namespace PortalFramework
{
    /// <summary>
    /// Utility class for mesh generation.
    /// </summary>
    public static class MeshBuilder
    {
        private static Mesh quadMesh;
        private static Mesh portalMesh;

        /// <summary>
        /// Return a quad mesh used for the portal raycast receiver.
        /// </summary>
        public static Mesh GetQuadMesh()
        {
            if (MeshBuilder.quadMesh == null)
            {
                MeshBuilder.quadMesh = MeshBuilder.BuildQuadMesh();
                Assert.IsNotNull(MeshBuilder.quadMesh);
            }

            return MeshBuilder.quadMesh;
        }

        /// <summary>
        /// Return a mesh used for the portal rendering.
        /// </summary>
        public static Mesh GetPortalMesh()
        {
            if (MeshBuilder.portalMesh == null)
            {
                MeshBuilder.portalMesh = MeshBuilder.BuildPortalMesh();
                Assert.IsNotNull(MeshBuilder.portalMesh);
            }

            return MeshBuilder.portalMesh;
        }

        private static Mesh BuildQuadMesh()
        {
            const float HalfSize = 0.5f;

            Mesh mesh = new Mesh();

            // Vertices
            mesh.vertices = new Vector3[4]
            {
                new Vector3(-HalfSize, -HalfSize, 0),
                new Vector3(-HalfSize, HalfSize, 0),
                new Vector3(HalfSize, HalfSize, 0),
                new Vector3(HalfSize, -HalfSize, 0),
            };

            // Normals
            mesh.normals = new Vector3[4]
            {
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
            };

            // UVs
            mesh.uv = new Vector2[4]
            {
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 0f),
            };

            // Triangles
            mesh.triangles = new int[]
            {
                0, 2, 1,
                0, 3, 2,
            };

            return mesh;
        }

        private static Mesh BuildPortalMesh()
        {
            Mesh mesh = new Mesh();

            // Vertices
            mesh.vertices = new Vector3[8]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.45f, -0.45f, -0.2f),
                new Vector3(-0.45f, 0.45f, -0.2f),
                new Vector3(0.45f, 0.45f, -0.2f),
                new Vector3(0.45f, -0.45f, -0.2f),
            };

            // Normals
            mesh.normals = new Vector3[8]
            {
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
            };

            // UVs
            mesh.uv = new Vector2[8]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f),
                new Vector2(0.05f, 0.05f),
                new Vector2(0.05f, 0.95f),
                new Vector2(0.95f, 0.95f),
                new Vector2(0.95f, 0.05f),
            };

            // Triangles
            mesh.triangles = new int[]
            {
                0, 2, 1,
                0, 3, 2,
                0, 4, 1,
                1, 4, 5,
                1, 5, 2,
                2, 5, 6,
                2, 6, 3,
                3, 6, 7,
                3, 7, 0,
                0, 7, 4,
                4, 6, 5,
                4, 7, 6,
            };

            return mesh;
        }
    }
}
