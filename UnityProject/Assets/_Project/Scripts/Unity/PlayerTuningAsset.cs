using UnityEngine;
using Two.Core;

namespace Two.Unity
{
    /// <summary>
    /// Embrulho ScriptableObject do tuning do core: designer edita no Inspector,
    /// Remote Config pode sobrescrever em produção, e o core continua sem saber
    /// que a Unity existe.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerTuning", menuName = "Two/Player Tuning")]
    public sealed class PlayerTuningAsset : ScriptableObject
    {
        public PlayerTuning Tuning = new PlayerTuning();
    }
}
