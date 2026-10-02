using System;

namespace Two.Core
{
    /// <summary>
    /// Vetor 2D próprio do core. Mantém Two.Core 100% independente de UnityEngine;
    /// a camada Unity converte nas bordas (TwoCoreUnityExtensions).
    /// </summary>
    [Serializable]
    public struct Vec2 : IEquatable<Vec2>
    {
        public float X;
        public float Y;

        public Vec2(float x, float y) { X = x; Y = y; }

        public static readonly Vec2 Zero = new Vec2(0f, 0f);
        public static readonly Vec2 Up = new Vec2(0f, 1f);
        public static readonly Vec2 Right = new Vec2(1f, 0f);

        public float Length => MathF.Sqrt(X * X + Y * Y);
        public float LengthSq => X * X + Y * Y;

        public Vec2 Normalized
        {
            get
            {
                float len = Length;
                return len > 1e-6f ? new Vec2(X / len, Y / len) : Zero;
            }
        }

        /// <summary>Perpendicular (rotação de 90° anti-horária).</summary>
        public Vec2 Perp => new Vec2(-Y, X);

        public static float Dot(in Vec2 a, in Vec2 b) => a.X * b.X + a.Y * b.Y;
        public static float Distance(in Vec2 a, in Vec2 b) => (a - b).Length;

        public static Vec2 Lerp(in Vec2 a, in Vec2 b, float t)
            => new Vec2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

        public Vec2 ClampLength(float max)
        {
            float lenSq = LengthSq;
            if (lenSq <= max * max) return this;
            float len = MathF.Sqrt(lenSq);
            return new Vec2(X / len * max, Y / len * max);
        }

        public static Vec2 operator +(in Vec2 a, in Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(in Vec2 a, in Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator -(in Vec2 a) => new Vec2(-a.X, -a.Y);
        public static Vec2 operator *(in Vec2 a, float s) => new Vec2(a.X * s, a.Y * s);
        public static Vec2 operator *(float s, in Vec2 a) => new Vec2(a.X * s, a.Y * s);
        public static Vec2 operator /(in Vec2 a, float s) => new Vec2(a.X / s, a.Y / s);

        public bool Equals(Vec2 other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Vec2 v && Equals(v);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X:0.###}, {Y:0.###})";
    }
}
