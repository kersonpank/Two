using System;
using UnityEngine;
using Two.Core;

namespace Two.Unity
{
    /// <summary>
    /// A casca Unity do jogador: roda a PlayerSim (core) em tick fixo, interpola o
    /// visual entre ticks e repassa eventos para juice (haptics/som/partículas).
    /// NÃO contém regra de jogo nenhuma — tudo vive em Two.Core.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public sealed class PlayerAvatar : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] PlayerTuningAsset tuningAsset;
        [SerializeField] LayerMask solidMask;
        [SerializeField] bool useTouchInput = true;

        /// <summary>Juice: HUD, haptics, áudio e partículas assinam aqui.</summary>
        public event Action<PlayerEvents> OnSimEvents;

        public PlayerSim Sim { get; private set; }
        public IInputSource Input { get; private set; }
        public IPartnerProvider Partner { get; set; } = new OfflinePartnerProvider();

        /// <summary>Posição suavizada p/ câmera e corda (interpolada entre ticks).</summary>
        public Vector2 RenderPosition { get; private set; }

        Vec2 _previousSimPosition;

        void Awake()
        {
            Sim = new PlayerSim(tuningAsset.Tuning, transform.position.ToCore());
            _previousSimPosition = Sim.Position;

            Input = useTouchInput && Application.isMobilePlatform
                ? (IInputSource)new TouchInputSource()
                : new KeyboardInputSource();
        }

        void Update()
        {
            Input.Sample();

            // Interpolação de render entre ticks fixos: 60Hz de sim, fluidez na tela
            // em qualquer refresh rate (90/120Hz nos celulares bons)
            float alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
            RenderPosition = Vec2.Lerp(_previousSimPosition, Sim.Position, alpha).ToUnity();
            transform.position = new Vector3(RenderPosition.x, RenderPosition.y, transform.position.z);

            // Facing
            var scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (Sim.FacingX >= 0 ? 1f : -1f);
            transform.localScale = scale;
        }

        void FixedUpdate()
        {
            _previousSimPosition = Sim.Position;

            var world = new PhysicsCollisionWorld(solidMask);
            var input = Input.Collect(Sim.FacingX);
            var hooks = HookPoint.GatherPositions();
            var partner = Partner.CurrentPartner;

            var events = Sim.Step(input, world, hooks, partner, Time.fixedDeltaTime);

            Partner.PublishSelf(new SelfSnapshot
            {
                Position = Sim.Position,
                Velocity = Sim.Velocity,
                State = Sim.State,
                FacingX = Sim.FacingX
            });

            if (HasAny(events)) OnSimEvents?.Invoke(events);
        }

        static bool HasAny(in PlayerEvents e)
            => e.Jumped || e.Landed || e.ThrewRope || e.ReleasedRope
            || e.Pinned || e.Unpinned || e.SwingApex;

        void OnDrawGizmosSelected()
        {
            if (tuningAsset == null) return;
            Gizmos.color = Color.cyan;
            var half = tuningAsset.Tuning.Motor.BodyHalfExtents.ToUnity();
            Gizmos.DrawWireCube(transform.position, half * 2f);
        }
    }
}
