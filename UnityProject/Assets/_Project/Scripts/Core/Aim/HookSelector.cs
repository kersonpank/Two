using System;
using System.Collections.Generic;

namespace Two.Core
{
    /// <summary>
    /// Aim assist do arremesso de fio: dado o toque impreciso de um polegar, escolhe o
    /// melhor gancho dentro de um cone generoso. Ângulo pesa mais que distância
    /// (dedo erra direção, não alcance). Retorna -1 se nada alcançável.
    /// </summary>
    public static class HookSelector
    {
        public static int Select(in Vec2 origin, in Vec2 aimDir, IReadOnlyList<Vec2> hooks, AimTuning t)
        {
            Vec2 dir = aimDir.LengthSq > 1e-6f ? aimDir.Normalized : Vec2.Up;
            float cosMax = MathF.Cos(t.MaxAngleDeg * MathF.PI / 180f);

            int best = -1;
            float bestScore = float.MaxValue;

            for (int i = 0; i < hooks.Count; i++)
            {
                Vec2 to = hooks[i] - origin;
                float dist = to.Length;
                if (dist < 1e-4f || dist > t.MaxRange) continue;

                float cos = Vec2.Dot(to / dist, dir);
                if (cos < cosMax) continue;

                float angleCost = (1f - cos) * t.AngleWeight;          // 0 = mira perfeita
                float distCost = (dist / t.MaxRange) * t.DistanceWeight;
                float score = angleCost + distCost;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
            return best;
        }
    }
}
