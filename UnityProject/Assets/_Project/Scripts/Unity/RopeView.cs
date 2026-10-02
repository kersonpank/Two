using UnityEngine;
using Two.Core;

namespace Two.Unity
{
    /// <summary>
    /// A corda VISUAL (fio de luz): VerletRope do core + LineRenderer com glow.
    /// Puramente cosmética e local (docs/03). Dois usos no jogo:
    /// 1) fio permanente entre os dois jogadores (modo Tether)
    /// 2) fio de balanço jogador→âncora enquanto em Swing (modo Swing)
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class RopeView : MonoBehaviour
    {
        public enum Mode { TetherToPartner, SwingAnchor }

        [SerializeField] PlayerAvatar player;
        [SerializeField] Mode mode = Mode.TetherToPartner;

        LineRenderer _line;
        VerletRope _rope;
        float _gravity;

        void Awake()
        {
            _line = GetComponent<LineRenderer>();
            var tuning = player.Sim.Tuning;
            _gravity = tuning.Motor.Gravity;
            _rope = new VerletRope(tuning.RopeVisual,
                player.RenderPosition.ToCore(), player.RenderPosition.ToCore());
            _line.positionCount = tuning.RopeVisual.PointCount;
            _line.useWorldSpace = true;
        }

        void LateUpdate()
        {
            Vec2 a = player.RenderPosition.ToCore();
            Vec2 b;
            bool visible;

            if (mode == Mode.SwingAnchor)
            {
                visible = player.Sim.State == PlayerStateKind.Swing;
                b = visible ? player.Sim.Swing.Anchor : a;
            }
            else
            {
                visible = player.Partner.CurrentPartner.Present;
                b = visible ? player.Partner.PartnerRenderPosition : a;
            }

            _line.enabled = visible;
            if (!visible) return;

            _rope.Step(a, b, _gravity, Time.deltaTime);

            for (int i = 0; i < _rope.Points.Length; i++)
                _line.SetPosition(i, _rope.Points[i].ToUnity3(transform.position.z));
        }
    }
}
