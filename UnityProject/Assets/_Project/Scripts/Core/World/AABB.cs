using System;

namespace Two.Core
{
    /// <summary>Caixa alinhada aos eixos — a forma de colisão do personagem e do cenário.</summary>
    [Serializable]
    public readonly struct AABB
    {
        public readonly Vec2 Center;
        public readonly Vec2 HalfExtents;

        public AABB(Vec2 center, Vec2 halfExtents)
        {
            Center = center;
            HalfExtents = halfExtents;
        }

        public float Left => Center.X - HalfExtents.X;
        public float Right => Center.X + HalfExtents.X;
        public float Bottom => Center.Y - HalfExtents.Y;
        public float Top => Center.Y + HalfExtents.Y;

        public AABB Shifted(in Vec2 delta) => new AABB(Center + delta, HalfExtents);

        public bool Overlaps(in AABB other)
            => Left < other.Right && Right > other.Left
            && Bottom < other.Top && Top > other.Bottom;

        public override string ToString() => $"AABB(c={Center}, h={HalfExtents})";
    }
}
