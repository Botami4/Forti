using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Forti.Rendering
{
    public static class ProceduralMeshBuilder
    {
        public static MeshGeometry3D CreateBox(float width, float height, float depth)
        {
            var mesh = new MeshGeometry3D();
            float w = width * 0.5f;
            float h = height * 0.5f;
            float d = depth * 0.5f;

            // 8 corners
            Point3D[] corners = new Point3D[]
            {
                new Point3D(-w, -h, -d), // 0
                new Point3D( w, -h, -d), // 1
                new Point3D( w,  h, -d), // 2
                new Point3D(-w,  h, -d), // 3
                new Point3D(-w, -h,  d), // 4
                new Point3D( w, -h,  d), // 5
                new Point3D( w,  h,  d), // 6
                new Point3D(-w,  h,  d)  // 7
            };

            // 6 faces (each face has 4 vertices with distinct UVs for proper texture mapping)
            AddQuad(mesh, corners[0], corners[1], corners[2], corners[3], new Vector3D(0, 0, -1)); // Front (-Z)
            AddQuad(mesh, corners[5], corners[4], corners[7], corners[6], new Vector3D(0, 0, 1));  // Back (+Z)
            AddQuad(mesh, corners[4], corners[0], corners[3], corners[7], new Vector3D(-1, 0, 0)); // Left (-X)
            AddQuad(mesh, corners[1], corners[5], corners[6], corners[2], new Vector3D(1, 0, 0));  // Right (+X)
            AddQuad(mesh, corners[3], corners[2], corners[6], corners[7], new Vector3D(0, 1, 0));  // Top (+Y)
            AddQuad(mesh, corners[4], corners[5], corners[1], corners[0], new Vector3D(0, -1, 0)); // Bottom (-Y)

            mesh.Freeze();
            return mesh;
        }

        public static MeshGeometry3D CreateCylinder(float radius, float height, int slices = 12)
        {
            var mesh = new MeshGeometry3D();
            float halfH = height * 0.5f;
            double step = 2.0 * System.Math.PI / slices;

            for (int i = 0; i < slices; i++)
            {
                double a0 = i * step;
                double a1 = (i + 1) * step;

                float x0 = (float)(System.Math.Cos(a0) * radius);
                float z0 = (float)(System.Math.Sin(a0) * radius);
                float x1 = (float)(System.Math.Cos(a1) * radius);
                float z1 = (float)(System.Math.Sin(a1) * radius);

                Point3D b0 = new Point3D(x0, -halfH, z0);
                Point3D b1 = new Point3D(x1, -halfH, z1);
                Point3D t1 = new Point3D(x1,  halfH, z1);
                Point3D t0 = new Point3D(x0,  halfH, z0);

                Vector3D normal = new Vector3D((x0 + x1) * 0.5, 0, (z0 + z1) * 0.5);
                AddQuad(mesh, b0, b1, t1, t0, normal);

                // Top lid
                AddTriangle(mesh, new Point3D(0, halfH, 0), t0, t1, new Vector3D(0, 1, 0));
                // Bottom lid
                AddTriangle(mesh, new Point3D(0, -halfH, 0), b1, b0, new Vector3D(0, -1, 0));
            }

            mesh.Freeze();
            return mesh;
        }

        public static MeshGeometry3D CreatePlane(float width, float length, int tilesX = 1, int tilesZ = 1)
        {
            var mesh = new MeshGeometry3D();
            float halfW = width * 0.5f;
            float halfL = length * 0.5f;

            Point3D p0 = new Point3D(-halfW, 0, -halfL);
            Point3D p1 = new Point3D( halfW, 0, -halfL);
            Point3D p2 = new Point3D( halfW, 0,  halfL);
            Point3D p3 = new Point3D(-halfW, 0,  halfL);

            int baseIdx = mesh.Positions.Count;
            mesh.Positions.Add(p0);
            mesh.Positions.Add(p1);
            mesh.Positions.Add(p2);
            mesh.Positions.Add(p3);

            mesh.Normals.Add(new Vector3D(0, 1, 0));
            mesh.Normals.Add(new Vector3D(0, 1, 0));
            mesh.Normals.Add(new Vector3D(0, 1, 0));
            mesh.Normals.Add(new Vector3D(0, 1, 0));

            mesh.TextureCoordinates.Add(new Point(0, 0));
            mesh.TextureCoordinates.Add(new Point(tilesX, 0));
            mesh.TextureCoordinates.Add(new Point(tilesX, tilesZ));
            mesh.TextureCoordinates.Add(new Point(0, tilesZ));

            mesh.TriangleIndices.Add(baseIdx);
            mesh.TriangleIndices.Add(baseIdx + 2);
            mesh.TriangleIndices.Add(baseIdx + 1);

            mesh.TriangleIndices.Add(baseIdx);
            mesh.TriangleIndices.Add(baseIdx + 3);
            mesh.TriangleIndices.Add(baseIdx + 2);

            mesh.Freeze();
            return mesh;
        }

        private static void AddQuad(MeshGeometry3D mesh, Point3D p0, Point3D p1, Point3D p2, Point3D p3, Vector3D normal)
        {
            int baseIdx = mesh.Positions.Count;
            mesh.Positions.Add(p0);
            mesh.Positions.Add(p1);
            mesh.Positions.Add(p2);
            mesh.Positions.Add(p3);

            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);

            mesh.TextureCoordinates.Add(new Point(0, 1));
            mesh.TextureCoordinates.Add(new Point(1, 1));
            mesh.TextureCoordinates.Add(new Point(1, 0));
            mesh.TextureCoordinates.Add(new Point(0, 0));

            mesh.TriangleIndices.Add(baseIdx);
            mesh.TriangleIndices.Add(baseIdx + 1);
            mesh.TriangleIndices.Add(baseIdx + 2);

            mesh.TriangleIndices.Add(baseIdx);
            mesh.TriangleIndices.Add(baseIdx + 2);
            mesh.TriangleIndices.Add(baseIdx + 3);
        }

        private static void AddTriangle(MeshGeometry3D mesh, Point3D p0, Point3D p1, Point3D p2, Vector3D normal)
        {
            int baseIdx = mesh.Positions.Count;
            mesh.Positions.Add(p0);
            mesh.Positions.Add(p1);
            mesh.Positions.Add(p2);

            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);

            mesh.TextureCoordinates.Add(new Point(0.5, 0.5));
            mesh.TextureCoordinates.Add(new Point(0, 1));
            mesh.TextureCoordinates.Add(new Point(1, 1));

            mesh.TriangleIndices.Add(baseIdx);
            mesh.TriangleIndices.Add(baseIdx + 1);
            mesh.TriangleIndices.Add(baseIdx + 2);
        }
    }
}
