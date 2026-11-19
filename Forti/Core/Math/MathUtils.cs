using System;
using System.Numerics;

namespace Forti.Core.Math
{
    public static class MathUtils
    {
        public const float Deg2Rad = MathF.PI / 180.0f;
        public const float Rad2Deg = 180.0f / MathF.PI;

        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Clamp(t, 0f, 1f);
        }

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            float f = Clamp(t, 0f, 1f);
            return a + (b - a) * f;
        }
    }
}
