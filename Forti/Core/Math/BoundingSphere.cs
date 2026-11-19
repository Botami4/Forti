using System;
using System.Numerics;

namespace Forti.Core.Math
{
    public struct BoundingSphere
    {
        public Vector3 Center;
        public float Radius;

        public BoundingSphere(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public readonly bool Contains(Vector3 point)
        {
            return Vector3.DistanceSquared(Center, point) <= Radius * Radius;
        }

        public readonly bool Intersects(BoundingSphere other)
        {
            float r = Radius + other.Radius;
            return Vector3.DistanceSquared(Center, other.Center) <= r * r;
        }

        public readonly bool RayIntersects(Ray ray, out float distance)
        {
            distance = 0f;
            Vector3 m = ray.Origin - Center;
            float b = Vector3.Dot(m, ray.Direction);
            float c = Vector3.Dot(m, m) - Radius * Radius;

            if (c > 0.0f && b > 0.0f)
                return false;

            float discr = b * b - c;
            if (discr < 0.0f)
                return false;

            float t = -b - MathF.Sqrt(discr);
            if (t < 0.0f)
                t = 0.0f;

            distance = t;
            return true;
        }
    }
}
