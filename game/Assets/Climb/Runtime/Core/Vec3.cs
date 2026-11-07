using System;

namespace Climb.Core
{
    /// <summary>Engine-independent metres / metres-per-second vector. X right, Y up, Z forward.</summary>
    public readonly struct Vec3
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }
        public static Vec3 Zero => new Vec3(0f, 0f, 0f);
        public float LengthSquared => X * X + Y * Y + Z * Z;
        public float Length => (float)Math.Sqrt(LengthSquared);
        public bool IsFinite => Finite(X) && Finite(Y) && Finite(Z);
        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 a, float s) => new Vec3(a.X * s, a.Y * s, a.Z * s);
        public static Vec3 operator *(float s, Vec3 a) => a * s;
        public override string ToString() => string.Format("({0:0.###}, {1:0.###}, {2:0.###})", X, Y, Z);
    }
}
