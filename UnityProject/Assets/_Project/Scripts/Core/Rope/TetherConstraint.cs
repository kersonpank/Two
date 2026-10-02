using System;

namespace Two.Core
{
    /// <summary>
    /// A restrição de GAMEPLAY do fio entre os dois jogadores, aplicada apenas ao PRÓPRIO
    /// personagem usando a posição interpolada do parceiro (docs/03-netcode.md).
    ///
    /// É uma restrição POSICIONAL suave — não uma força: o motor é cinemático (estilo
    /// Celeste) e re-impõe a velocidade de controle a cada tick, então forças externas
    /// perdem a disputa. Puxar a posição exponencialmente + remover a velocidade radial
    /// de fuga converge sempre, sem oscilação, e divergência de centímetros entre os
    /// clientes vira correção minúscula que se autoanula.
    /// </summary>
    public static class TetherConstraint
    {
        /// <summary>
        /// Resolve a restrição no próprio personagem. Retorna o quanto a posição foi
        /// corrigida (Zero = estava dentro do alcance).
        /// </summary>
        public static Vec2 Solve(ref Vec2 selfPos, ref Vec2 selfVel, in Vec2 partnerPos, TetherTuning t, float dt)
        {
            Vec2 away = selfPos - partnerPos;
            float dist = away.Length;
            if (dist <= t.MaxLength || dist < 1e-5f)
                return Vec2.Zero;

            Vec2 n = away / dist;
            float excess = dist - t.MaxLength;

            // Amortece só a componente radial de afastamento (nunca freia movimento lateral/retorno)
            float radialSpeed = Vec2.Dot(selfVel, n);
            if (radialSpeed > 0f)
                selfVel -= n * (radialSpeed * MathF.Min(1f, t.Damping * dt));

            // Mola posicional exponencial, com teto de velocidade de puxada (nunca teleporta)
            float pull = MathF.Min(excess * MathF.Min(1f, t.Stiffness * dt), t.MaxPullSpeed * dt);
            Vec2 correction = -n * pull;
            selfPos += correction;
            return correction;
        }
    }
}
