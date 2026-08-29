namespace FocusMapApi.Services.OpenAi;

/// <summary>
/// Implementação em memória do teto diário de chamadas à OpenAI. Basta para uma
/// rodada de testes com uma única instância da API; se a API rodar com múltiplas
/// instâncias, cada instância teria sua própria cota (o teto real seria
/// DailyLimit * nº de instâncias). Para esse cenário, trocar por um contador
/// compartilhado (ex.: tabela no banco ou Redis).
/// </summary>
public class OpenAiUsageGuard : IOpenAiUsageGuard
{
    private readonly object _lock = new();
    private DateOnly _currentDay;
    private int _count;

    public OpenAiUsageGuard(IConfiguration config)
    {
        IsEnabled = config.GetValue<bool?>("OpenAI:Enabled") ?? true;
        DailyLimit = config.GetValue<int?>("OpenAI:DailyCallLimit") ?? 500;
        _currentDay = DateOnly.FromDateTime(DateTime.UtcNow);
        _count = 0;
    }

    public bool IsEnabled { get; }
    public int DailyLimit { get; }

    public bool TryConsume(out int usedToday)
    {
        lock (_lock)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (today != _currentDay)
            {
                _currentDay = today;
                _count = 0;
            }

            if (!IsEnabled || _count >= DailyLimit)
            {
                usedToday = _count;
                return false;
            }

            _count++;
            usedToday = _count;
            return true;
        }
    }
}
