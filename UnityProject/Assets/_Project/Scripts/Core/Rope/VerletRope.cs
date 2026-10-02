using System;

namespace Two.Core
{
    /// <summary>
    /// Corda VISUAL por integração de Verlet. Puramente cosmética e local: nunca trafega
    /// na rede e nunca afeta gameplay (a restrição de gameplay é TetherConstraint).
    /// Ancorada nas duas posições RENDERIZADAS — por construção, impossível dessincronizar
    /// do que o jogador vê. (Decisão central do projeto, docs/03-netcode.md.)
    /// </summary>
    public sealed class VerletRope
    {
        public readonly Vec2[] Points;
        readonly Vec2[] _previous;
        readonly RopeVisualTuning _t;

        public VerletRope(RopeVisualTuning tuning, Vec2 anchorA, Vec2 anchorB)
        {
            _t = tuning;
            Points = new Vec2[tuning.PointCount];
            _previous = new Vec2[tuning.PointCount];
            for (int i = 0; i < Points.Length; i++)
            {
                Points[i] = Vec2.Lerp(anchorA, anchorB, i / (float)(Points.Length - 1));
                _previous[i] = Points[i];
            }
        }

        public void Step(Vec2 anchorA, Vec2 anchorB, float gravity, float dt)
        {
            int n = Points.Length;

            // Integração de Verlet nos pontos internos
            Vec2 gravityStep = new Vec2(0f, -gravity * _t.GravityScale * dt * dt);
            for (int i = 1; i < n - 1; i++)
            {
                Vec2 current = Points[i];
                Points[i] += (current - _previous[i]) * _t.Damping + gravityStep;
                _previous[i] = current;
            }

            // Comprimento de segmento com folga: dá o caimento (catenária aproximada)
            float span = Vec2.Distance(anchorA, anchorB);
            float segment = MathF.Max(span, 0.1f) * _t.Slack / (n - 1);

            // Relaxation: pinos nas pontas + restrições de distância
            for (int iter = 0; iter < _t.ConstraintIterations; iter++)
            {
                Points[0] = anchorA;
                Points[n - 1] = anchorB;

                for (int i = 0; i < n - 1; i++)
                {
                    Vec2 delta = Points[i + 1] - Points[i];
                    float dist = delta.Length;
                    if (dist < 1e-6f) continue;
                    float error = (dist - segment) / dist;

                    if (i == 0)
                        Points[i + 1] -= delta * error;           // ponta A fixa
                    else if (i == n - 2)
                        Points[i] += delta * error;               // ponta B fixa
                    else
                    {
                        Points[i] += delta * (0.5f * error);
                        Points[i + 1] -= delta * (0.5f * error);
                    }
                }
            }

            Points[0] = anchorA;
            Points[n - 1] = anchorB;
        }

        /// <summary>Quanto o ponto médio cai abaixo da linha reta entre as âncoras (p/ testes e efeitos).</summary>
        public float MidpointSag()
        {
            Vec2 chordMid = Vec2.Lerp(Points[0], Points[Points.Length - 1], 0.5f);
            return chordMid.Y - Points[Points.Length / 2].Y;
        }
    }
}
