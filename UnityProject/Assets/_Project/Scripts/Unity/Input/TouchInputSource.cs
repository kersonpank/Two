using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Two.Core;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Two.Unity
{
    /// <summary>
    /// Controles touch do jogo (ver docs/01):
    /// - Metade ESQUERDA: joystick flutuante — nasce onde o polegar toca, raio curto.
    /// - Metade DIREITA: terço inferior = PULO; acima = AÇÃO (segurar mira, soltar arremessa).
    ///   Toque longo parado no chão = FIXAR (pin).
    /// Sem GameObjects de UI para a lógica: zonas por fração de tela, HUD é só desenho.
    /// </summary>
    public sealed class TouchInputSource : IInputSource
    {
        const float JoystickRadiusPx = 110f;   // raio curto: polegar quase não desliza
        const float DeadzoneFraction = 0.18f;
        const float PinHoldSeconds = 0.45f;    // segurar AÇÃO parado = fixar

        // Estado do joystick flutuante
        int _stickTouchId = -1;
        Vector2 _stickOrigin;
        Vector2 _stickValue;

        // Estado dos botões
        int _jumpTouchId = -1;
        int _actionTouchId = -1;
        float _actionHoldTime;

        // Bordas acumuladas entre ticks (nunca perder um toque entre frames)
        bool _jumpPressedEdge;
        bool _actionPressedEdge;
        bool _actionReleasedEdge;
        bool _pinEdge;
        bool _jumpHeld;
        bool _actionHeld;

        public TouchInputSource()
        {
            EnhancedTouchSupport.Enable();
        }

        public Vector2 StickValue => _stickValue;          // p/ HUD desenhar o joystick
        public Vector2 StickOrigin => _stickOrigin;
        public bool StickActive => _stickTouchId >= 0;
        public bool Aiming => _actionHeld;                 // p/ câmera lenta local + arco de mira

        public void Sample()
        {
            float halfWidth = Screen.width * 0.5f;
            float jumpZoneTop = Screen.height * 0.38f;

            foreach (var touch in Touch.activeTouches)
            {
                switch (touch.phase)
                {
                    case UnityEngine.InputSystem.TouchPhase.Began:
                        if (touch.screenPosition.x < halfWidth && _stickTouchId < 0)
                        {
                            _stickTouchId = touch.touchId;
                            _stickOrigin = touch.screenPosition;   // flutuante: nasce sob o dedo
                            _stickValue = Vector2.zero;
                        }
                        else if (touch.screenPosition.x >= halfWidth)
                        {
                            if (touch.screenPosition.y < jumpZoneTop && _jumpTouchId < 0)
                            {
                                _jumpTouchId = touch.touchId;
                                _jumpPressedEdge = true;
                                _jumpHeld = true;
                            }
                            else if (_actionTouchId < 0)
                            {
                                _actionTouchId = touch.touchId;
                                _actionPressedEdge = true;
                                _actionHeld = true;
                                _actionHoldTime = 0f;
                            }
                        }
                        break;

                    case UnityEngine.InputSystem.TouchPhase.Moved:
                    case UnityEngine.InputSystem.TouchPhase.Stationary:
                        if (touch.touchId == _stickTouchId)
                        {
                            Vector2 delta = touch.screenPosition - _stickOrigin;
                            _stickValue = Vector2.ClampMagnitude(delta / JoystickRadiusPx, 1f);
                            if (_stickValue.magnitude < DeadzoneFraction) _stickValue = Vector2.zero;
                        }
                        if (touch.touchId == _actionTouchId)
                            _actionHoldTime += Time.deltaTime;
                        break;

                    case UnityEngine.InputSystem.TouchPhase.Ended:
                    case UnityEngine.InputSystem.TouchPhase.Canceled:
                        if (touch.touchId == _stickTouchId) { _stickTouchId = -1; _stickValue = Vector2.zero; }
                        if (touch.touchId == _jumpTouchId) { _jumpTouchId = -1; _jumpHeld = false; }
                        if (touch.touchId == _actionTouchId)
                        {
                            _actionTouchId = -1;
                            _actionHeld = false;
                            if (_actionHoldTime >= PinHoldSeconds && _stickValue == Vector2.zero)
                                _pinEdge = true;            // segurou parado = fixar
                            else
                                _actionReleasedEdge = true; // soltou = arremesso
                        }
                        break;
                }
            }
        }

        public PlayerInput Collect(int facingX)
        {
            // Mira: direção do joystick; sem joystick, diagonal para cima no sentido do facing
            Vec2 aim = _stickValue.sqrMagnitude > 0.1f
                ? new Vec2(_stickValue.x, Mathf.Max(_stickValue.y, 0.15f))
                : new Vec2(facingX * 0.7f, 0.7f);

            var input = new PlayerInput
            {
                MoveX = _stickValue.x,
                MoveY = _stickValue.y,
                JumpPressed = _jumpPressedEdge,
                JumpHeld = _jumpHeld,
                ActionPressed = _actionPressedEdge,
                ActionHeld = _actionHeld,
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
