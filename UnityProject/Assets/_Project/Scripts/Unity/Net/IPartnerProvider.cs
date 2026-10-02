using Two.Core;

namespace Two.Unity
{
    /// <summary>Snapshot do próprio jogador publicado a cada tick para a rede.</summary>
    public struct SelfSnapshot
    {
        public Vec2 Position;
        public Vec2 Velocity;
        public PlayerStateKind State;
        public int FacingX;
    }

    /// <summary>
    /// A ÚNICA porta entre gameplay e rede. O PlayerAvatar publica seu estado e lê o do
    /// parceiro; quem está do outro lado (Fusion, bridge local, replay) é indiferente.
    /// Trocar de netcode = trocar a implementação disto, nada mais (docs/02, princípio nº 1).
    /// </summary>
    public interface IPartnerProvider
    {
        /// <summary>Estado interpolado do parceiro neste instante (Present=false se sozinho).</summary>
        PartnerInfo CurrentPartner { get; }

        /// <summary>Posição RENDERIZADA do parceiro (para corda visual e câmera).</summary>
        Vec2 PartnerRenderPosition { get; }

        void PublishSelf(in SelfSnapshot snapshot);
    }

    /// <summary>Desenvolvimento offline (Fase 0): sem parceiro, ou um parceiro fixo de teste.</summary>
    public sealed class OfflinePartnerProvider : IPartnerProvider
    {
        public bool SimulatePinnedPartner;
        public Vec2 PartnerPosition;

        public PartnerInfo CurrentPartner => SimulatePinnedPartner
            ? new PartnerInfo { Present = true, Position = PartnerPosition, IsPinned = true }
            : PartnerInfo.None;

        public Vec2 PartnerRenderPosition => PartnerPosition;

        public void PublishSelf(in SelfSnapshot snapshot) { /* ninguém ouvindo */ }
    }
}
