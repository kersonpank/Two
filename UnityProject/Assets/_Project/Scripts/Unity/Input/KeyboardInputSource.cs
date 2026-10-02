using UnityEngine;
using UnityEngine.InputSystem;
using Two.Core;

namespace Two.Unity
{
    /// <summary>
    /// Input de teclado para iterar no editor: A/D ou setas movem, Espaço pula,
    /// J segura/solta arremesso, K fixa. Mira com as teclas de direção.
    /// </summary>
    public sealed class KeyboardInputSource : IInputSource
    {
        bool _jumpPressedEdge, _actionPressedEdge, _actionReleasedEdge, _pinEdge;

        public void Sample()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.spaceKey.wasPressedThisFrame) _jumpPressedEdge = true;
            if (kb.jKey.wasPressedThisFrame) _actionPressedEdge = true;
            if (kb.jKey.wasReleasedThisFrame) _actionReleasedEdge = true;
            if (kb.kKey.wasPressedThisFrame) _pinEdge = true;
        }

        public PlayerInput Collect(int facingX)
        {
            var kb = Keyboard.current;
            float x = 0f, y = 0f;
            bool jumpHeld = false, actionHeld = false;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
                jumpHeld = kb.spaceKey.isPressed;
                actionHeld = kb.jKey.isPressed;
            }

            Vec2 aim = (x != 0f || y != 0f)
                ? new Vec2(x, Mathf.Max(y, 0.15f))
                : new Vec2(facingX * 0.7f, 0.7f);

            var input = new PlayerInput
            {
                MoveX = x,
                MoveY = y,
                JumpPressed = _jumpPressedEdge,
                JumpHeld = jumpHeld,
                ActionPressed = _actionPressedEdge,
                ActionHeld = actionHeld,
                ActionReleased = _actionReleasedEdge,
                PinPressed = _pinEdge,
                AimDir = aim.Normalized
            };

            _jumpPressedEdge = false;
            _actionPressedEdge = false;
            _actionReleasedEdge = false;
            _pinEdge = false;
            return input;
        }
    }
}
