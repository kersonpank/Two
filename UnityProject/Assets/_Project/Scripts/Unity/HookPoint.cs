using System.Collections.Generic;
using UnityEngine;
using Two.Core;

namespace Two.Unity
{
    /// <summary>
    /// Ponto de gancho do cenário: componente vazio + gizmo. Registra-se num registry
    /// estático consultado pela simulação (alloc-free por tick).
    /// </summary>
    public sealed class HookPoint : MonoBehaviour
    {
        static readonly List<HookPoint> Active = new List<HookPoint>();
        static readonly List<Vec2> PositionsCache = new List<Vec2>(32);

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        /// <summary>Posições de todos os ganchos ativos (lista reutilizada — não guardar referência).</summary>
        public static IReadOnlyList<Vec2> GatherPositions()
        {
            PositionsCache.Clear();
            for (int i = 0; i < Active.Count; i++)
                PositionsCache.Add(Active[i].transform.position.ToCore());
            return PositionsCache;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }
    }
}
