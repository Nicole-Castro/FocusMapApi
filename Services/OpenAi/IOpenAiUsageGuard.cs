namespace FocusMapApi.Services.OpenAi;

/// <summary>
/// Trava de custo para chamadas à OpenAI: liga/desliga por config (kill switch)
/// e impõe um teto diário de chamadas, para uma rodada de testes não sair caro
/// por causa de um loop indevido ou uso excessivo.
/// </summary>
public interface IOpenAiUsageGuard
{
    bool IsEnabled { get; }
    int DailyLimit { get; }

    /// <summary>
    /// Tenta consumir uma chamada da cota diária. Retorna false se a feature
    /// estiver desligada ou o teto do dia já tiver sido atingido.
    /// </summary>
    bool TryConsume(out int usedToday);
}
