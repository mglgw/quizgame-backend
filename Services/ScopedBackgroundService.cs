using QuizGame.Interfaces;

namespace QuizGame.Services;

public sealed class ScopedBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ScopedBackgroundService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(5000, cancellationToken);
        using var scope = _serviceScopeFactory.CreateScope();
        var scopedGameSessionService = scope.ServiceProvider.GetRequiredService<IGameSessionService>();
        while (!cancellationToken.IsCancellationRequested)
        {
            scopedGameSessionService.RoundController();
            await Task.Delay(1000, cancellationToken);
        }
    }
}