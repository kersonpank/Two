using System;

namespace Two.Core
{
    /// <summary>
    /// O frame de input que a simulação consome. Quem produz isso pode ser o touch local,
    /// um replay gravado, um bot de teste ou a rede — a simulação não sabe nem quer saber.
    /// </summary>
    [Serializable]
    public struct PlayerInput
    {
        public float MoveX;          // -1..1
        public float MoveY;          // -1..1 (subir/descer corda, mirar)
        public bool JumpPressed;     // borda de subida neste tick
        public bool JumpHeld;
        public bool ActionPressed;   // arremessar fio / interagir
        public bool ActionHeld;      // segurando = mirando (câmera lenta local)
        public bool ActionReleased;  // soltou = dispara o arremesso
        public bool PinPressed;      // alternar fixar-se (âncora viva)
        public Vec2 AimDir;          // direção de mira normalizada (do joystick ou padrão)
    }

    /// <summary>
    /// Eventos de apresentação emitidos por um Step — a casca (Unity/HTML) consome para
    /// haptics, som e partículas sem a simulação conhecer nada disso.
    /// </summary>
    public struct PlayerEvents
    {
        public bool Jumped;
        public bool Landed;
        public float LandedImpactSpeed;
        public bool ThrewRope;
        public bool ReleasedRope;
        public bool Pinned;
        public bool Unpinned;
        public bool SwingApex;      // momento de soltar — haptic sutil que ensina o timing

        public static PlayerEvents None => default;
    }
}
