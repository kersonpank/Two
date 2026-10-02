using System;

namespace Two.Core
{
    /// <summary>
    /// Motor de plataforma cinemático (estilo Celeste): o movimento é resolvido POR NÓS,
    /// eixo a eixo, em passos pequenos contra ICollisionWorld — não por rigidbody dinâmico.
    /// Isso dá controle total do feel, é determinístico o bastante e roda headless.
    /// Assists embutidos: coyote time, jump buffer, pulo de altura variável,
    /// gravidade de queda aumentada.
    /// </summary>
    public sealed class PlayerMotor
    {
        const float CollisionStep = 0.01f; // m — granularidade do move-and-collide

        public Vec2 Position;
        public Vec2 Velocity;
        public bool Grounded;
        public int FacingX = 1;

        float _coyoteTimer;
        float _jumpBufferTimer;
        bool _jumpConsumedSinceGround;

        readonly MotorTuning _t;

        public PlayerMotor(MotorTuning tuning, Vec2 startPosition)
        {
            _t = tuning;
            Position = startPosition;
        }

        public AABB Bounds => new AABB(Position, _t.BodyHalfExtents);

        /// <summary>Um tick de simulação. Retorna eventos para a camada de apresentação.</summary>
        public PlayerEvents Step(in PlayerInput input, ICollisionWorld world, float dt)
        {
            var events = PlayerEvents.None;
            bool wasGrounded = Grounded;
            float fallSpeedBefore = -Velocity.Y;

            // --- timers de assist ---
            if (input.JumpPressed) _jumpBufferTimer = _t.JumpBufferTime;
            else _jumpBufferTimer = MathF.Max(0f, _jumpBufferTimer - dt);
            _coyoteTimer = Grounded ? _t.CoyoteTime : MathF.Max(0f, _coyoteTimer - dt);

            // --- horizontal ---
            float targetX = input.MoveX * _t.MaxRunSpeed;
            bool accelerating = MathF.Abs(targetX) > 0.01f; // há input → accel; sem input → decel
            float rate = Grounded
                ? (accelerating ? _t.GroundAccel : _t.GroundDecel)
                : (accelerating ? _t.AirAccel : _t.AirDecel);
            Velocity.X = MoveToward(Velocity.X, targetX, rate * dt);
            if (MathF.Abs(input.MoveX) > 0.01f) FacingX = input.MoveX > 0f ? 1 : -1;

            // --- pulo (buffer + coyote) ---
            bool canJump = (Grounded || _coyoteTimer > 0f) && !_jumpConsumedSinceGround;
            if (_jumpBufferTimer > 0f && canJump)
            {
                Velocity.Y = _t.JumpVelocity;
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
                _jumpConsumedSinceGround = true;
                Grounded = false;
                events.Jumped = true;
            }

            // --- gravidade (variável: subindo segurando < subindo solto < caindo) ---
            float g = _t.Gravity;
            if (Velocity.Y < 0f) g *= _t.FallGravityMult;
            else if (Velocity.Y > 0f && !input.JumpHeld) g *= _t.JumpCutGravityMult;
            Velocity.Y = MathF.Max(Velocity.Y - g * dt, -_t.MaxFallSpeed);

            // --- mover e colidir, eixo a eixo ---
            bool hitGroundThisStep = MoveAxes(world, Velocity * dt);

            // --- sondar chão (2cm abaixo) ---
            bool groundBelow = world.Collides(Bounds.Shifted(new Vec2(0f, -0.02f)));
            if (groundBelow && Velocity.Y <= 0f)
            {
                if (!wasGrounded && (hitGroundThisStep || groundBelow))
                {
                    events.Landed = true;
                    events.LandedImpactSpeed = fallSpeedBefore;
                }
                Grounded = true;
                _jumpConsumedSinceGround = false;
            }
            else
            {
                Grounded = false;
            }

            return events;
        }

        /// <summary>
        /// Entrada externa de estado (balanço, tether, carregado): a state machine
        /// escreve posição/velocidade e o motor segue dali.
        /// </summary>
        public void OverrideState(Vec2 position, Vec2 velocity)
        {
            Position = position;
            Velocity = velocity;
            Grounded = false;
            _coyoteTimer = 0f;
        }

        bool MoveAxes(ICollisionWorld world, Vec2 delta)
        {
            // Horizontal
            if (MoveAxis(world, new Vec2(delta.X, 0f)))
                Velocity.X = 0f;

            // Vertical
            bool hitVertical = MoveAxis(world, new Vec2(0f, delta.Y));
            bool hitGround = hitVertical && delta.Y < 0f;
            if (hitVertical) Velocity.Y = 0f; // chão OU teto (bonk)
            return hitGround;
        }

        /// <summary>Avança em passos de 1cm; para no primeiro sólido. True se colidiu.</summary>
        bool MoveAxis(ICollisionWorld world, Vec2 delta)
        {
            float remaining = delta.Length;
            if (remaining < 1e-7f) return false;
            Vec2 dir = delta / remaining;

            while (remaining > 0f)
            {
                float step = MathF.Min(CollisionStep, remaining);
                Vec2 next = Position + dir * step;
                if (world.Collides(new AABB(next, _t.BodyHalfExtents)))
                    return true;
                Position = next;
                remaining -= step;
            }
            return false;
        }

        static float MoveToward(float current, float target, float maxDelta)
        {
            float diff = target - current;
            if (MathF.Abs(diff) <= maxDelta) return target;
            return current + MathF.Sign(diff) * maxDelta;
        }
    }
}
