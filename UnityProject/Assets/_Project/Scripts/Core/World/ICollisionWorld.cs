namespace Two.Core
{
    /// <summary>
    /// Abstração do mundo sólido. O core nunca consulta física da engine diretamente:
    /// a Unity implementa isso com Physics2D.OverlapBox; os testes, com listas de AABBs.
    /// É o que permite rodar a simulação inteira em headless (testes, CI, servidor futuro).
    /// </summary>
    public interface ICollisionWorld
    {
        /// <summary>True se a caixa intersecta qualquer sólido do cenário.</summary>
        bool Collides(in AABB box);
    }
}
