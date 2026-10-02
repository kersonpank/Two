using Two.Core;

namespace Two.Unity
{
    /// <summary>
    /// Fonte de input da simulação. Implementações: TouchInputSource (device),
    /// KeyboardInputSource (editor), futuramente ReplayInputSource e BotInputSource.
    /// Acumula eventos entre ticks; Collect() entrega o frame e limpa as bordas.
    /// </summary>
    public interface IInputSource
    {
        /// <summary>Chamado pela camada de apresentação a cada frame de render.</summary>
        void Sample();

        /// <summary>Chamado pela simulação a cada tick fixo; consome as bordas acumuladas.</summary>
        PlayerInput Collect(int facingX);
    }
}
