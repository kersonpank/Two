using System;

namespace Two.Core
{
    /// <summary>Estado do balanço: âncora (gancho do mundo ou parceiro fixado) + comprimento atual.</summary>
    public struct SwingState
    {
        public Vec2 Anchor;
        public float Length;
        public bool AnchoredToPartner; // âncora é o parceiro fixado (segue a posição replicada dele)
    }

    /// <summary>
    /// Pêndulo analítico — NÃO usamos joint de física: projetamos a posição no círculo da
    /// corda e removemos a componente radial de fuga da velocidade. Mais estável, mais
    /// tunável e 100% local (âncoras são estáticas por design — ver docs/03-netcode.md).
    /// </summary>
    public static class SwingSim
    {
        /// <summary>
        /// Um tick de balanço. Modifica posição/velocidade/estado. Retorna true ao cruzar
        /// o ápice (velocidade tangencial muda de sinal) — gatilho do haptic de timing.
        /// </summary>
        public static bool Step(
            ref Vec2 position, ref Vec2 velocity, ref SwingState state,
            float moveX, float climbY,
            SwingTuning t, float gravity, float dt)
        {
            float tangentialBefore = TangentialSpeed(position, velocity, state.Anchor);

            // Embalar: força horizontal simples — a restrição do círculo a converte em tangencial.
            velocity.X += moveX * t.PumpAccel * dt;

            // Gravidade + arrasto leve
            velocity.Y -= gravity * dt;
            velocity *= MathF.Max(0f, 1f - t.SwingDrag * dt);

            // Subir/descer pela corda
            if (MathF.Abs(climbY) > 0.01f)
                state.Length = Clamp(state.Length - climbY * t.ClimbSpeed * dt, t.MinLength, t.MaxLength);

            // Integra
            position += velocity * dt;

            // Restrição do círculo: corda só PUXA (dist > comprimento); frouxa = queda livre
            Vec2 radial = position - state.Anchor;
            float dist = radial.Length;
            if (dist > state.Length && dist > 1e-5f)
            {
                Vec2 n = radial / dist;
                position = state.Anchor + n * state.Length;
                float radialSpeed = Vec2.Dot(velocity, n);
                if (radialSpeed > 0f)
                    velocity -= n * radialSpeed; // remove só a fuga radial; tangencial intacta
            }

            float tangentialAfter = TangentialSpeed(position, velocity, state.Anchor);
            bool crossedApex = tangentialBefore != 0f && tangentialAfter != 0f
                && MathF.Sign(tangentialBefore) != MathF.Sign(tangentialAfter);
            return crossedApex;
        }

        /// <summary>Velocidade ao soltar a corda, com o boost que premia o timing do ápice.</summary>
        public static Vec2 ReleaseVelocity(in Vec2 velocity, SwingTuning t)
            => velocity * t.ReleaseBoost;

        static float TangentialSpeed(in Vec2 position, in Vec2 velocity, in Vec2 anchor)
        {
            Vec2 n = (position - anchor).Normalized;
            return Vec2.Dot(velocity, n.Perp);
        }

        static float Clamp(float v, float min, float max)
            => v < min ? min : (v > max ? max : v);
    }
}
