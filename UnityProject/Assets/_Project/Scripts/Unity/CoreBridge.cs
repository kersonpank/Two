using UnityEngine;
using Two.Core;

namespace Two.Unity
{
    /// <summary>Conversões nas bordas core ↔ engine. Único lugar onde os dois mundos se tocam.</summary>
    public static class CoreBridge
    {
        public static Vector2 ToUnity(this Vec2 v) => new Vector2(v.X, v.Y);
        public static Vector3 ToUnity3(this Vec2 v, float z = 0f) => new Vector3(v.X, v.Y, z);
        public static Vec2 ToCore(this Vector2 v) => new Vec2(v.x, v.y);
        public static Vec2 ToCore(this Vector3 v) => new Vec2(v.x, v.y);
    }
}
