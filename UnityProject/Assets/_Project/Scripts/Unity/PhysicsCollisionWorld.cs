using UnityEngine;
using Two.Core;

namespace Two.Unity
{
    /// <summary>
    /// ICollisionWorld sobre Physics2D: o cenário é feito de colliders comuns
    /// (Tilemap/BoxCollider2D) numa layer "Solid"; o core consulta via OverlapBox.
    /// O personagem NÃO tem rigidbody dinâmico — o motor do core é quem move.
    /// </summary>
    public sealed class PhysicsCollisionWorld : ICollisionWorld
    {
        readonly LayerMask _solidMask;
        readonly ContactFilter2D _filter;
        static readonly Collider2D[] Hits = new Collider2D[1];

        public PhysicsCollisionWorld(LayerMask solidMask)
        {
            _solidMask = solidMask;
            _filter = new ContactFilter2D { useLayerMask = true, layerMask = solidMask, useTriggers = false };
        }

        public bool Collides(in AABB box)
        {
            int count = Physics2D.OverlapBox(
                box.Center.ToUnity(),
                (box.HalfExtents * 2f).ToUnity(),
                0f, _filter, Hits);
            return count > 0;
        }
    }
}
