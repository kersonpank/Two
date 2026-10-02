using System;

namespace Two.Core
{
    /// <summary>
    /// TODOS os números de feel do jogo vivem aqui, em unidades físicas legíveis
    /// (metros, segundos). A Unity embrulha isso num ScriptableObject; o Remote Config
    /// pode sobrescrever em produção. Nenhuma constante de gameplay solta pelo código.
    ///
    /// Valores iniciais calibrados no protótipo HTML (prototype/) — core, protótipo
    /// e Unity compartilham estes mesmos números.
    /// </summary>
    [Serializable]
    public class MotorTuning
    {
        // Corrida
        public float MaxRunSpeed = 7.0f;      // m/s
        public float GroundAccel = 85f;       // m/s² — chega à velocidade máxima em ~0.08s: resposta "cola no dedo"
        public float GroundDecel = 95f;       // parar é ainda mais rápido que acelerar (precisão em plataforma)
        public float AirAccel = 48f;
        public float AirDecel = 20f;          // no ar, soltar o direcional NÃO freia seco (preserva momento do balanço)

        // Pulo — definido por ALTURA e TEMPO, não por força: designer pensa em "2.7m em 0.38s",
        // gravidade e velocidade inicial são derivadas.
        public float JumpHeight = 2.7f;       // m
        public float TimeToApex = 0.38f;      // s
        public float FallGravityMult = 1.9f;  // cair mais rápido que subir = pulo "crocante" (padrão do gênero)
        public float JumpCutGravityMult = 3.2f; // soltou o botão subindo → gravidade extra = altura variável
        public float MaxFallSpeed = 17f;      // m/s — teto de queda para legibilidade e segurança da câmera

        // Assists (mobile precisa de mais que console)
        public float CoyoteTime = 0.12f;      // s após sair da borda em que o pulo ainda vale
        public float JumpBufferTime = 0.15f;  // s antes de aterrissar em que o toque de pulo fica guardado

        // Dimensões do corpo (AABB)
        public float BodyWidth = 0.55f;       // m
        public float BodyHeight = 0.95f;      // m

        public float Gravity => 2f * JumpHeight / (TimeToApex * TimeToApex);
        public float JumpVelocity => Gravity * TimeToApex;
        public Vec2 BodyHalfExtents => new Vec2(BodyWidth * 0.5f, BodyHeight * 0.5f);
    }

    [Serializable]
    public class SwingTuning
    {
        public float PumpAccel = 16f;         // m/s² — força horizontal de "embalar" durante o balanço
        public float ClimbSpeed = 2.5f;       // m/s — subir/descer pela corda (rapel)
        public float MinLength = 0.8f;        // m
        public float MaxLength = 6.0f;        // m — também é o alcance do arremesso
        public float ReleaseBoost = 1.06f;    // multiplicador de velocidade ao soltar — o "uau" do ápice
        public float SwingDrag = 0.12f;       // 1/s — amortecimento leve para o pêndulo não oscilar para sempre
    }

    [Serializable]
    public class TetherTuning
    {
        public float MaxLength = 9.0f;        // m — comprimento do fio entre os dois jogadores
        public float Stiffness = 12f;         // 1/s — taxa da mola POSICIONAL exponencial (suave, ver docs/03)
        public float Damping = 25f;           // 1/s — amortece só a velocidade radial de afastamento
        public float MaxPullSpeed = 12f;      // m/s — teto da puxada: a rede nunca gera tranco nem teleporte
    }

    [Serializable]
    public class RopeVisualTuning
    {
        public int PointCount = 20;
        public float Slack = 1.12f;           // corda visual 12% mais comprida que a distância = caimento bonito
        public int ConstraintIterations = 3;
        public float Damping = 0.995f;
        public float GravityScale = 0.6f;     // corda de LUZ cai mais devagar que corda de corda
    }

    [Serializable]
    public class AimTuning
    {
        public float MaxRange = 6.0f;           // = SwingTuning.MaxLength
        public float MaxAngleDeg = 55f;         // cone de perdão em volta da direção apontada
        public float AngleWeight = 1.0f;        // pontuação: ângulo pesa mais que distância
        public float DistanceWeight = 0.25f;    // (dedo impreciso erra ângulo, não distância)
    }

    /// <summary>Agregado raiz — uma instância destes é "o feel do jogo" inteiro.</summary>
    [Serializable]
    public class PlayerTuning
    {
        public MotorTuning Motor = new MotorTuning();
        public SwingTuning Swing = new SwingTuning();
        public TetherTuning Tether = new TetherTuning();
        public RopeVisualTuning RopeVisual = new RopeVisualTuning();
        public AimTuning Aim = new AimTuning();
    }
}
