using System;
using System.Collections.Generic;
using static Two.Core.Tests.Harness;

namespace Two.Core.Tests
{
    public static class Program
    {
        const float Dt = 1f / 60f; // mesmo tick do jogo

        public static int Main()
        {
            MotorTests();
            SwingTests();
            RopeTests();
            TetherTests();
            HookTests();
            ScenarioTests();
            return RunAll();
        }

        // ------------------------------------------------------------- helpers

        static (PlayerMotor motor, TestWorld world, MotorTuning t) GroundedMotor()
        {
            var t = new MotorTuning();
            var world = new TestWorld();
            var motor = new PlayerMotor(t, new Vec2(0f, t.BodyHeight * 0.5f + 0.001f));
            // assenta no chão
            for (int i = 0; i < 30; i++) motor.Step(default, world, Dt);
            return (motor, world, t);
        }

        static PlayerInput Hold(float moveX = 0f, bool jumpHeld = false) =>
            new PlayerInput { MoveX = moveX, JumpHeld = jumpHeld };

        static PlayerInput PressJump(float moveX = 0f) =>
            new PlayerInput { MoveX = moveX, JumpPressed = true, JumpHeld = true };

        // --------------------------------------------------------------- Motor

        static void MotorTests()
        {
            Test("Motor: assenta e fica grounded no chão", () =>
            {
                var (motor, _, _) = GroundedMotor();
                IsTrue(motor.Grounded, "grounded após assentar");
            });

            Test("Motor: altura do pulo bate com o tuning (JumpHeight)", () =>
            {
                var (motor, world, t) = GroundedMotor();
                float startY = motor.Position.Y;
                motor.Step(PressJump(), world, Dt);
                float apex = startY;
                for (int i = 0; i < 240; i++)
                {
                    motor.Step(Hold(jumpHeld: true), world, Dt);
                    apex = Math.Max(apex, motor.Position.Y);
                    if (motor.Grounded) break;
                }
                InRange(apex - startY, t.JumpHeight * 0.85f, t.JumpHeight * 1.05f, "altura do apex");
            });

            Test("Motor: soltar o botão corta o pulo (altura variável)", () =>
            {
                var (m1, w1, t) = GroundedMotor();
                m1.Step(PressJump(), w1, Dt);
                float fullApex = 0f;
                for (int i = 0; i < 240 && !m1.Grounded; i++)
                { m1.Step(Hold(jumpHeld: true), w1, Dt); fullApex = Math.Max(fullApex, m1.Position.Y); }

                var (m2, w2, _) = GroundedMotor();
                m2.Step(PressJump(), w2, Dt);
                m2.Step(Hold(jumpHeld: true), w2, Dt); // segura só 1 tick extra
                float cutApex = 0f;
                for (int i = 0; i < 240 && !m2.Grounded; i++)
                { m2.Step(Hold(jumpHeld: false), w2, Dt); cutApex = Math.Max(cutApex, m2.Position.Y); }

                IsTrue(cutApex < fullApex * 0.65f,
                    $"pulo cortado ({cutApex:0.00}) < 65% do pulo cheio ({fullApex:0.00})");
            });

            Test("Motor: coyote time permite pular logo após sair da borda", () =>
            {
                var (motor, world, t) = GroundedMotor();
                world.FloorEnabled = false;                    // "saiu da borda"
                for (int i = 0; i < 4; i++) motor.Step(default, world, Dt); // ~0.067s < 0.12s
                var ev = motor.Step(PressJump(), world, Dt);
                IsTrue(ev.Jumped, "pulou dentro da janela de coyote");
            });

            Test("Motor: coyote time expira (não é pulo infinito no ar)", () =>
            {
                var (motor, world, _) = GroundedMotor();
                world.FloorEnabled = false;
                for (int i = 0; i < 20; i++) motor.Step(default, world, Dt); // 0.33s > 0.12s
                var ev = motor.Step(PressJump(), world, Dt);
                IsTrue(!ev.Jumped, "não pulou após a janela expirar");
            });

            Test("Motor: jump buffer executa o pulo ao aterrissar", () =>
            {
                var t = new MotorTuning();
                var world = new TestWorld();
                var motor = new PlayerMotor(t, new Vec2(0f, 2.5f)); // caindo de 2.5m
                bool jumped = false;
                bool pressed = false;
                for (int i = 0; i < 300; i++)
                {
                    PlayerInput input = default;
                    // aperta pulo UMA vez ainda no ar, perto do chão
                    if (!pressed && motor.Position.Y < 1.1f && !motor.Grounded)
                    { input = PressJump(); pressed = true; }
                    var ev = motor.Step(input, world, Dt);
                    if (ev.Jumped) { jumped = true; break; }
                }
                IsTrue(pressed, "setup: chegou a apertar no ar");
                IsTrue(jumped, "o toque guardado virou pulo na aterrissagem");
            });

            Test("Motor: corre à velocidade máxima e para rápido no chão", () =>
            {
                var (motor, world, t) = GroundedMotor();
                for (int i = 0; i < 60; i++) motor.Step(Hold(moveX: 1f), world, Dt);
                Approx(motor.Velocity.X, t.MaxRunSpeed, 0.1f, "velocidade de corrida");
                int ticksToStop = 0;
                while (Math.Abs(motor.Velocity.X) > 0.05f && ticksToStop < 60)
                { motor.Step(default, world, Dt); ticksToStop++; }
                IsTrue(ticksToStop <= 10, $"parou em {ticksToStop} ticks (≤10 = resposta no dedo)");
            });

            Test("Motor: parede bloqueia e zera velocidade horizontal", () =>
            {
                var (motor, world, t) = GroundedMotor();
                world.Solids.Add(new AABB(new Vec2(2f, 1f), new Vec2(0.2f, 1f)));
                for (int i = 0; i < 120; i++) motor.Step(Hold(moveX: 1f), world, Dt);
                IsTrue(motor.Position.X < 2f, "não atravessou a parede");
                Approx(motor.Velocity.X, 0f, 0.01f, "velocidade zerada na parede");
            });
        }

        // --------------------------------------------------------------- Swing

        static SwingTuning NoDragSwing() => new SwingTuning { SwingDrag = 0f, PumpAccel = 0f };

        static void SwingTests()
        {
            Test("Swing: corda nunca estica além do comprimento", () =>
            {
                var t = NoDragSwing();
                float g = new MotorTuning().Gravity;
                var state = new SwingState { Anchor = Vec2.Zero, Length = 3f };
                Vec2 pos = new Vec2(2.598f, -1.5f); // 60° da vertical
                Vec2 vel = Vec2.Zero;
                for (int i = 0; i < 600; i++) // 5s a 120Hz
                {
                    SwingSim.Step(ref pos, ref vel, ref state, 0f, 0f, t, g, 1f / 120f);
                    IsTrue(Vec2.Distance(pos, state.Anchor) <= state.Length + 0.002f,
                        $"dist {Vec2.Distance(pos, state.Anchor):0.000} <= {state.Length}");
                }
            });

            Test("Swing: energia não explode (pêndulo estável)", () =>
            {
                var t = NoDragSwing();
                float g = new MotorTuning().Gravity;
                var state = new SwingState { Anchor = Vec2.Zero, Length = 3f };
                Vec2 pos = new Vec2(2.598f, -1.5f);
                Vec2 vel = Vec2.Zero;
                float E0 = 0.5f * vel.LengthSq + g * pos.Y;
                for (int i = 0; i < 1200; i++)
                    SwingSim.Step(ref pos, ref vel, ref state, 0f, 0f, t, g, 1f / 120f);
                float E1 = 0.5f * vel.LengthSq + g * pos.Y;
                IsTrue(E1 <= E0 + Math.Abs(E0) * 0.02f + 0.5f,
                    $"energia final {E1:0.00} não excede inicial {E0:0.00}");
            });

            Test("Swing: embalar (pump) aumenta a amplitude", () =>
            {
                var t = new SwingTuning { SwingDrag = 0f }; // pump padrão, sem drag
                float g = new MotorTuning().Gravity;
                var state = new SwingState { Anchor = Vec2.Zero, Length = 3f };
                Vec2 pos = new Vec2(0.5f, -2.958f); // ângulo pequeno
                Vec2 vel = Vec2.Zero;
                float initialAmplitude = Math.Abs(pos.X);
                float maxAmplitude = initialAmplitude;
                for (int i = 0; i < 480; i++) // 4s
                {
                    float pump = Math.Sign(vel.X == 0f ? 1f : vel.X); // embala a favor do movimento
                    SwingSim.Step(ref pos, ref vel, ref state, pump, 0f, t, g, 1f / 120f);
                    maxAmplitude = Math.Max(maxAmplitude, Math.Abs(pos.X));
                }
                IsTrue(maxAmplitude > initialAmplitude * 2f,
                    $"amplitude cresceu de {initialAmplitude:0.00} para {maxAmplitude:0.00}");
            });

            Test("Swing: subir pela corda encurta até o mínimo", () =>
            {
                var t = new SwingTuning();
                float g = new MotorTuning().Gravity;
                var state = new SwingState { Anchor = Vec2.Zero, Length = 4f };
                Vec2 pos = new Vec2(0f, -4f);
                Vec2 vel = Vec2.Zero;
                for (int i = 0; i < 600; i++)
                    SwingSim.Step(ref pos, ref vel, ref state, 0f, 1f, t, g, 1f / 120f);
                Approx(state.Length, t.MinLength, 0.05f, "comprimento após subir 5s");
            });

            Test("Swing: soltar aplica o boost de release", () =>
            {
                var t = new SwingTuning();
                var vel = new Vec2(5f, 0f);
                var released = SwingSim.ReleaseVelocity(vel, t);
                Approx(released.Length, 5f * t.ReleaseBoost, 0.001f, "módulo com boost");
            });
        }

        // ---------------------------------------------------------------- Rope

        static void RopeTests()
        {
            Test("Verlet: pontas sempre pinadas nas âncoras", () =>
            {
                var t = new RopeVisualTuning();
                var a = new Vec2(0f, 0f);
                var b = new Vec2(4f, 0f);
                var rope = new VerletRope(t, a, b);
                for (int i = 0; i < 240; i++) rope.Step(a, b, 37f, 1f / 60f);
                IsTrue(rope.Points[0].Equals(a), "ponta A pinada");
                IsTrue(rope.Points[t.PointCount - 1].Equals(b), "ponta B pinada");
            });

            Test("Verlet: corda assenta com caimento (sag) entre âncoras horizontais", () =>
            {
                var t = new RopeVisualTuning();
                var a = new Vec2(0f, 0f);
                var b = new Vec2(4f, 0f);
                var rope = new VerletRope(t, a, b);
                for (int i = 0; i < 240; i++) rope.Step(a, b, 37f, 1f / 60f);
                IsTrue(rope.MidpointSag() > 0.1f, $"sag = {rope.MidpointSag():0.000} > 0.1");
            });

            Test("Verlet: comprimento total coerente com a folga configurada", () =>
            {
                var t = new RopeVisualTuning();
                var a = new Vec2(0f, 0f);
                var b = new Vec2(4f, 0f);
                var rope = new VerletRope(t, a, b);
                for (int i = 0; i < 240; i++) rope.Step(a, b, 37f, 1f / 60f);
                float total = 0f;
                for (int i = 0; i < rope.Points.Length - 1; i++)
                    total += Vec2.Distance(rope.Points[i], rope.Points[i + 1]);
                float span = Vec2.Distance(a, b);
                InRange(total, span, span * t.Slack * 1.08f, "comprimento da polilinha");
            });

            Test("Verlet: âncoras em movimento rápido não explodem a corda", () =>
            {
                var t = new RopeVisualTuning();
                var rope = new VerletRope(t, Vec2.Zero, new Vec2(2f, 0f));
                var rng = new Random(42);
                for (int i = 0; i < 300; i++)
                {
                    var a = new Vec2((float)rng.NextDouble() * 8f, (float)rng.NextDouble() * 4f);
                    var b = a + new Vec2(3f, 1f);
                    rope.Step(a, b, 37f, 1f / 60f);
                    foreach (var p in rope.Points)
                        IsTrue(p.Length < 100f, "ponto dentro de limites sãos");
                }
            });
        }

        // -------------------------------------------------------------- Tether

        static void TetherTests()
        {
            Test("Tether: nenhuma correção dentro do alcance", () =>
            {
                var t = new TetherTuning();
                Vec2 pos = new Vec2(t.MaxLength - 0.5f, 0f);
                Vec2 vel = new Vec2(3f, 0f);
                var correction = TetherConstraint.Solve(ref pos, ref vel, Vec2.Zero, t, Dt);
                IsTrue(correction.Equals(Vec2.Zero), "sem correção dentro do MaxLength");
                Approx(vel.X, 3f, 0.001f, "velocidade intocada dentro do alcance");
            });

            Test("Tether: além do alcance, corrige em direção ao parceiro e amortece fuga", () =>
            {
                var t = new TetherTuning();
                Vec2 pos = new Vec2(t.MaxLength + 1f, 0f);
                Vec2 vel = new Vec2(5f, 0f); // fugindo
                var correction = TetherConstraint.Solve(ref pos, ref vel, Vec2.Zero, t, Dt);
                IsTrue(correction.X < 0f, "correção aponta para o parceiro");
                IsTrue(vel.X < 5f, "velocidade radial de fuga amortecida");
                IsTrue(correction.Length <= t.MaxPullSpeed * Dt + 1e-4f, "respeita o teto de puxada");
            });

            Test("Tether: converge para o alcance sem oscilar (restrição posicional)", () =>
            {
                var t = new TetherTuning();
                Vec2 partner = Vec2.Zero;
                Vec2 pos = new Vec2(t.MaxLength + 2f, 0f);
                Vec2 vel = Vec2.Zero;
                float prevDist = pos.Length;
                for (int i = 0; i < 300; i++) // 5s
                {
                    pos += vel * Dt;
                    TetherConstraint.Solve(ref pos, ref vel, partner, t, Dt);
                    float d = pos.Length;
                    IsTrue(d <= prevDist + 1e-4f, "distância monotonicamente não-crescente");
                    prevDist = d;
                }
                IsTrue(prevDist <= t.MaxLength + 0.05f,
                    $"convergiu para {prevDist:0.00} (alcance {t.MaxLength})");
            });
        }

        // --------------------------------------------------------------- Hooks

        static void HookTests()
        {
            Test("AimAssist: escolhe o gancho mais alinhado com a mira", () =>
            {
                var t = new AimTuning();
                var hooks = new List<Vec2> { new Vec2(0f, 4f), new Vec2(3f, 3f), new Vec2(0f, 10f) };
                int up = HookSelector.Select(Vec2.Zero, new Vec2(0f, 1f), hooks, t);
                IsTrue(up == 0, $"mira pra cima escolhe (0,4), escolheu índice {up}");
                int diag = HookSelector.Select(Vec2.Zero, new Vec2(1f, 1f).Normalized, hooks, t);
                IsTrue(diag == 1, $"mira na diagonal escolhe (3,3), escolheu índice {diag}");
            });

            Test("AimAssist: fora de alcance ou fora do cone = nada", () =>
            {
                var t = new AimTuning();
                var farOnly = new List<Vec2> { new Vec2(0f, t.MaxRange + 2f) };
                IsTrue(HookSelector.Select(Vec2.Zero, new Vec2(0f, 1f), farOnly, t) == -1, "longe demais");
                var behind = new List<Vec2> { new Vec2(0f, -3f) };
                IsTrue(HookSelector.Select(Vec2.Zero, new Vec2(0f, 1f), behind, t) == -1, "atrás do cone");
            });
        }

        // ------------------------------------------------------------ Scenario

        static void ScenarioTests()
        {
            Test("Cenário: correr → arremessar → balançar → soltar → aterrissar adiante", () =>
            {
                var tuning = new PlayerTuning();
                var world = new TestWorld();
                var hooks = new List<Vec2> { new Vec2(6f, 5f) };
                var sim = new PlayerSim(tuning, new Vec2(0f, tuning.Motor.BodyHeight * 0.5f + 0.001f));
                var partner = PartnerInfo.None;

                // assenta
                for (int i = 0; i < 30; i++) sim.Step(default, world, hooks, partner, Dt);

                // corre até perto do gancho
                int guard = 0;
                while (sim.Position.X < 4f && guard++ < 600)
                    sim.Step(new PlayerInput { MoveX = 1f }, world, hooks, partner, Dt);
                IsTrue(sim.Position.X >= 4f, "chegou ao ponto de arremesso");

                // arremessa mirando o gancho
                Vec2 aim = (hooks[0] - sim.Position).Normalized;
                var ev = sim.Step(new PlayerInput { ActionReleased = true, AimDir = aim },
                    world, hooks, partner, Dt);
                IsTrue(ev.ThrewRope, "fio prendeu no gancho");
                IsTrue(sim.State == PlayerStateKind.Swing, "entrou em Swing");

                float throwX = sim.Position.X;

                // embala por 1.2s
                for (int i = 0; i < 72; i++)
                    sim.Step(new PlayerInput { MoveX = 1f }, world, hooks, partner, Dt);

                // solta com pulo
                sim.Step(new PlayerInput { JumpPressed = true }, world, hooks, partner, Dt);
                IsTrue(sim.State == PlayerStateKind.Move, "voltou para Move após soltar");

                // voa e aterrissa
                for (int i = 0; i < 300 && !sim.Grounded; i++)
                    sim.Step(default, world, hooks, partner, Dt);

                IsTrue(sim.Grounded, "aterrissou");
                IsTrue(sim.Position.X > throwX + 1f,
                    $"avançou com o balanço: soltou em x={throwX:0.0}, pousou em x={sim.Position.X:0.0}");
            });

            Test("Cenário: tether impede a dupla de se separar além do fio", () =>
            {
                var tuning = new PlayerTuning();
                var world = new TestWorld();
                var hooks = new List<Vec2>();
                var sim = new PlayerSim(tuning, new Vec2(0f, tuning.Motor.BodyHeight * 0.5f + 0.001f));
                var partner = new PartnerInfo { Present = true, Position = Vec2.Zero, IsPinned = true };

                for (int i = 0; i < 30; i++) sim.Step(default, world, hooks, partner, Dt);

                // tenta correr para longe do parceiro por 10s
                for (int i = 0; i < 600; i++)
                    sim.Step(new PlayerInput { MoveX = 1f }, world, hooks, partner, Dt);

                float dist = Vec2.Distance(sim.Position, partner.Position);
                IsTrue(dist < tuning.Tether.MaxLength + 1.5f,
                    $"distância estabilizou em {dist:0.0}m (fio de {tuning.Tether.MaxLength}m)");
            });

            Test("Cenário: balançar a partir do parceiro fixado; se ele solta, a corda escorrega", () =>
            {
                var tuning = new PlayerTuning();
                var world = new TestWorld { FloorEnabled = true };
                var hooks = new List<Vec2>();
                var sim = new PlayerSim(tuning, new Vec2(0f, tuning.Motor.BodyHeight * 0.5f + 0.001f));
                var partner = new PartnerInfo { Present = true, Position = new Vec2(3f, 4f), IsPinned = true };

                for (int i = 0; i < 30; i++) sim.Step(default, world, hooks, partner, Dt);

                // arremessa no parceiro fixado (sem ganchos de mundo por perto)
                Vec2 aim = (partner.Position - sim.Position).Normalized;
                var ev = sim.Step(new PlayerInput { ActionReleased = true, AimDir = aim },
                    world, hooks, partner, Dt);
                IsTrue(ev.ThrewRope && sim.Swing.AnchoredToPartner, "prendeu no parceiro fixado");

                // parceiro solta o pin no meio do balanço → falha graciosa, sem travar
                partner.IsPinned = false;
                var ev2 = sim.Step(default, world, hooks, partner, Dt);
                IsTrue(ev2.ReleasedRope, "corda escorregou quando o pin sumiu");
                IsTrue(sim.State == PlayerStateKind.Move, "voltou ao controle normal");
            });
        }
    }
}
