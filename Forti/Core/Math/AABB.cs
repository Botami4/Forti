using System;
using System.Numerics;

namespace Forti.Core.Math
{
    public struct AABB
    {
        public Vector3 Min;
        public Vector3 Max;

        public AABB(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public static AABB FromCenterSize(Vector3 center, Vector3 size)
        {
            Vector3 half = size * 0.5f;
            return new AABB(center - half, center + half);
        }

        public readonly Vector3 Center => (Min + Max) * 0.5f;
        public readonly Vector3 Size => Max - Min;

        public readonly bool Contains(Vector3 point)
        {
            return point.X >= Min.X && point.X <= Max.X &&
                   point.Y >= Min.Y && point.Y <= Max.Y &&
                   point.Z >= Min.Z && point.Z <= Max.Z;
        }

        public readonly bool Intersects(AABB other)
        {
            return (Min.X <= other.Max.X && Max.X >= other.Min.X) &&
                   (Min.Y <= other.Max.Y && Max.Y >= other.Min.Y) &&
                   (Min.Z <= other.Max.Z && Max.Z >= other.Min.Z);
        }

        public readonly bool RayIntersects(Ray ray, out float distance)
        {
            distance = 0f;
            float tMin = 0f;
            float tMax = float.MaxValue;

            for (int i = 0; i < 3; i++)
            {
                float origin = i == 0 ? ray.Origin.X : (i == 1 ? ray.Origin.Y : ray.Origin.Z);
                float dir = i == 0 ? ray.Direction.X : (i == 1 ? ray.Direction.Y : ray.Direction.Z);
                float minVal = i == 0 ? Min.X : (i == 1 ? Min.Y : Min.Z);
                float maxVal = i == 0 ? Max.X : (i == 1 ? Max.Y : Max.Z);

                if (System.Math.Abs(dir) < 1e-6f)
                {
                    if (origin < minVal || origin > maxVal)
                        return false;
                }
                else
                {
                    float invD = 1.0f / dir;
                    float t0 = (minVal - origin) * invD;
                    float t1 = (maxVal - origin) * invD;
                    if (t0 > t1)
                    {
                        (t0, t1) = (t1, t0);
                    }

                    tMin = System.Math.Max(tMin, t0);
                    tMax = System.Math.Min(tMax, t1);
                    if (tMin > tMax)
                        return false;
                }
            }

            distance = tMin;
            return true;
        }
    }
}
