using FocusMapApi.Data;
using Microsoft.EntityFrameworkCore;

namespace FocusMapApi.Services.Sessions;

/// <summary>
/// Trava de segurança: fecha sozinha qualquer sessão que ficou aberta além do tempo
/// máximo permitido (ex.: alguém esqueceu de encerrar, o app travou, o celular morreu).
/// Roda em background e não depende do app/cliente estar vivo — é a garantia real,
/// diferente do timer do lado do app, que só funciona se o app continuar rodando.
/// </summary>
public class SessionAutoCloseService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _maxDuration;
    private readonly TimeSpan _checkInterval;

    public SessionAutoCloseService(IServiceScopeFactory scopeFactory, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _maxDuration = TimeSpan.FromHours(config.GetValue<double?>("Session:MaxDurationHours") ?? 2);
        _checkInterval = TimeSpan.FromMinutes(
            config.GetValue<double?>("Session:AutoCloseCheckIntervalMinutes") ?? 10
        );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CloseExpiredSessionsAsync(stoppingToken);
            }
            catch (Exception)
            {
                // Uma falha nesse ciclo não deve derrubar o loop — tenta de novo no próximo.
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Encerramento normal do host, ignora.
            }
        }
    }

    private async Task CloseExpiredSessionsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoff = DateTime.UtcNow - _maxDuration;
        var expiredSessions = await context
            .Sessions.Where(s => s.SessionEndTime == null && s.SessionStartTime <= cutoff)
            .ToListAsync(stoppingToken);

        if (expiredSessions.Count == 0)
            return;

        foreach (var session in expiredSessions)
        {
            // Fecha exatamente no limite (não no momento da varredura), pra a duração
            // registrada refletir o teto configurado.
            session.SessionEndTime = session.SessionStartTime + _maxDuration;
        }

        await context.SaveChangesAsync(stoppingToken);
    }
}
