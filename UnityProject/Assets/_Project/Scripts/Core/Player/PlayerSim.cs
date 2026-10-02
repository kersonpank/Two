using System;
using System.Collections.Generic;

namespace Two.Core
{
    public enum PlayerStateKind
    {
        Move,     // chão/ar — PlayerMotor
        Swing,    // pendurado no fio — SwingSim
        Pinned,   // fixado: âncora viva para o parceiro (estático = previsível na rede)
        Carried,  // recolhido pelo parceiro (também cobre queda de conexão)
        Dormant   // app do dono em background / desconectado
    }

    /// <summary>O que a simulação precisa saber do parceiro (posição interpolada vinda da rede).</summary>
    public struct PartnerInfo
    {
        public bool Present;
        public Vec2 Position;
        public bool IsPinned;

        public static PartnerInfo None => default;
    }

    /// <summary>
    /// A simulação completa de UM jogador, headless. A camada Unity/rede fornece input,
    /// mundo de colisão, ganchos e o estado do parceiro; recebe de volta posição,
    /// velocidade, estado e eventos. Nenhuma referência a engine ou a rede aqui dentro —
    /// é o que permite: testes em CI, bot de parceiro, replay e simulação no servidor.
    /// </summary>
    public sealed class PlayerSim
    {
        public readonly PlayerTuning Tuning;
        public PlayerStateKind State { get; private set; } = PlayerStateKind.Move;
        public SwingState Swing;

        readonly PlayerMotor _motor;

        public Vec2 Position => _motor.Position;
        public Vec2 Velocity => _motor.Velocity;
        public bool Grounded => _motor.Grounded;
        public int FacingX => _motor.FacingX;
        public AABB Bounds => _motor.Bounds;

        public PlayerSim(PlayerTuning tuning, Vec2 startPosition)
        {
            Tuning = tuning;
            _motor = new PlayerMotor(tuning.Motor, startPosition);
        }

        public PlayerEvents Step(
            in PlayerInput input,
            ICollisionWorld world,
            IReadOnlyList<Vec2> hooks,
            in PartnerInfo partner,
            float dt)
        {
            switch (State)
            {
                case PlayerStateKind.Move: return StepMove(input, world, hooks, partner, dt);
                case PlayerStateKind.Swing: return StepSwing(input, world, partner, dt);
                case PlayerStateKind.Pinned: return StepPinned(input, partner, dt);
                case PlayerStateKind.Carried: return StepCarried(input, partner);
                case PlayerStateKind.Dormant: return PlayerEvents.None;
                default: return PlayerEvents.None;
            }
        }

        // ---------------------------------------------------------------- Move

        PlayerEvents StepMove(in PlayerInput input, ICollisionWorld world,
            IReadOnlyList<Vec2> hooks, in PartnerInfo partner, float dt)
        {
            var events = _motor.Step(input, world, dt);

            // Tether: restrição posicional suave DEPOIS do motor (o motor é cinemático
            // e venceria qualquer força externa — ver TetherConstraint)
            if (partner.Present)
            {
                Vec2 pos = _motor.Position;
                Vec2 vel = _motor.Velocity;
                TetherConstraint.Solve(ref pos, ref vel, partner.Position, Tuning.Tether, dt);
                _motor.Velocity = vel;

                // Aplica a correção eixo a eixo: o chão bloqueia a componente vertical
                // do fio sem anular a puxada horizontal (desliza, não trava)
                Vec2 half = Tuning.Motor.BodyHalfExtents;
                Vec2 applied = _motor.Position;
                if (!world.Collides(new AABB(new Vec2(pos.X, applied.Y), half)))
                    applied.X = pos.X;
                if (!world.Collides(new AABB(new Vec2(applied.X, pos.Y), half)))
                    applied.Y = pos.Y;
                _motor.Position = applied;
            }

            // Fixar-se (só no chão)
            if (input.PinPressed && _motor.Grounded)
            {
                State = PlayerStateKind.Pinned;
                _motor.OverrideState(_motor.Position, Vec2.Zero);
                events.Pinned = true;
                return events;
            }

            // Arremesso do fio: soltar o botão de ação dispara
            if (input.ActionReleased)
            {
                int hookIndex = HookSelector.Select(_motor.Position, input.AimDir, hooks, Tuning.Aim);
                bool partnerAsAnchor = false;

                // Parceiro fixado também é âncora válida (prioridade menor que gancho de mundo)
                if (hookIndex < 0 && partner.Present && partner.IsPinned)
                {
                    float dist = Vec2.Distance(_motor.Position, partner.Position);
                    partnerAsAnchor = dist <= Tuning.Aim.MaxRange;
                }

                if (hookIndex >= 0 || partnerAsAnchor)
                {
                    Vec2 anchor = hookIndex >= 0 ? hooks[hookIndex] : partner.Position;
                    float length = MathF.Min(
                        Vec2.Distance(_motor.Position, anchor),
                        Tuning.Swing.MaxLength);
                    Swing = new SwingState
                    {
                        Anchor = anchor,
                        Length = MathF.Max(length, Tuning.Swing.MinLength),
                        AnchoredToPartner = partnerAsAnchor
                    };
                    State = PlayerStateKind.Swing;
                    events.ThrewRope = true;
                }
            }

            return events;
        }

        // --------------------------------------------------------------- Swing

        PlayerEvents StepSwing(in PlayerInput input, ICollisionWorld world, in PartnerInfo partner, float dt)
        {
            var events = PlayerEvents.None;

            // Âncora viva: segue a posição replicada do parceiro fixado.
            // Se ele soltou o pin, a corda "escorrega" — falha graciosa (docs/03).
            if (Swing.AnchoredToPartner)
            {
                if (!partner.Present || !partner.IsPinned)
                {
                    ExitSwingToMove(SwingSim.ReleaseVelocity(_motor.Velocity, Tuning.Swing) * 0.5f);
                    events.ReleasedRope = true;
                    return events;
                }
                Swing.Anchor = partner.Position;
            }

            Vec2 position = _motor.Position;
            Vec2 velocity = _motor.Velocity;

            bool apex = SwingSim.Step(
                ref position, ref velocity, ref Swing,
                input.MoveX, input.MoveY,
                Tuning.Swing, Tuning.Motor.Gravity, dt);
            events.SwingApex = apex;

            // Balanço não atravessa sólido: resolve eixo a eixo para DESLIZAR ao longo
            // do obstáculo (pêndulo baixo raspa o chão em vez de congelar)
            Vec2 start = _motor.Position;
            Vec2 delta = position - start;
            Vec2 half = Tuning.Motor.BodyHalfExtents;
            Vec2 resolved = start;

            if (!world.Collides(new AABB(new Vec2(start.X + delta.X, start.Y), half)))
                resolved.X = start.X + delta.X;
            else
                velocity.X = 0f;

            if (!world.Collides(new AABB(new Vec2(resolved.X, start.Y + delta.Y), half)))
                resolved.Y = start.Y + delta.Y;
            else
                velocity.Y = 0f;

            position = resolved;
            _motor.OverrideState(position, velocity);

            // Soltar: pulo (dá o boost) ou ação de novo
            if (input.JumpPressed || input.ActionPressed)
            {
                ExitSwingToMove(SwingSim.ReleaseVelocity(velocity, Tuning.Swing));
                events.ReleasedRope = true;
            }

            return events;
        }

        void ExitSwingToMove(Vec2 releaseVelocity)
        {
            _motor.OverrideState(_motor.Position, releaseVelocity);
            State = PlayerStateKind.Move;
        }

        // -------------------------------------------------------------- Pinned

        PlayerEvents StepPinned(in PlayerInput input, in PartnerInfo partner, float dt)
        {
            var events = PlayerEvents.None;
            // Fixado é DELIBERADAMENTE estático: é isso que o torna previsível para o
            // cliente remoto e permite o parceiro balançar a partir dele sem latência.
            if (input.PinPressed || input.JumpPressed)
            {
                State = PlayerStateKind.Move;
                events.Unpinned = true;
            }
            return events;
        }

        // ------------------------------------------------------------- Carried

        PlayerEvents StepCarried(in PlayerInput input, in PartnerInfo partner)
        {
            if (partner.Present)
                _motor.OverrideState(partner.Position + new Vec2(0f, 0.9f), Vec2.Zero);

            if (input.JumpPressed) // pular para fora dos "ombros"
            {
                State = PlayerStateKind.Move;
                _motor.OverrideState(_motor.Position, new Vec2(0f, Tuning.Motor.JumpVelocity * 0.8f));
            }
            return PlayerEvents.None;
        }

        // ------------------------------------------------- transições externas

        /// <summary>Rede/ciclo de vida: app foi para background ou caiu a conexão.</summary>
        public void SetDormant(bool dormant)
        {
            if (dormant) { State = PlayerStateKind.Dormant; _motor.OverrideState(_motor.Position, Vec2.Zero); }
            else if (State == PlayerStateKind.Dormant) State = PlayerStateKind.Move;
        }

        /// <summary>Parceiro recolheu este jogador (Dormant ou a pedido).</summary>
        public void SetCarried() => State = PlayerStateKind.Carried;

        /// <summary>Respawn/checkpoint: teleporte autoritativo.</summary>
        public void Teleport(Vec2 position)
        {
            State = PlayerStateKind.Move;
            _motor.OverrideState(position, Vec2.Zero);
        }
    }
}
