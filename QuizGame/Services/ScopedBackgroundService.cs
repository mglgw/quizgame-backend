namespace QuizGame.Services;

public sealed class ScopedBackgroundService : BackgroundService

{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ScopedBackgroundService> _logger;
    public ScopedBackgroundService(IServiceScopeFactory serviceScopeFactory, ILogger<ScopedBackgroundService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        using var scopedGameService = scope.ServiceProvider.GetRequiredService<IGameService>();
        using var scopedGameSessionService = scope.ServiceProvider.GetRequiredService<IGameSessionService>();
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogCritical("error in background service occured");
            }
            while (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("BACKGROUND service still working ... ");
                _logger.LogInformation("game service counter" + scopedGameService.GetGsc());
                _logger.LogInformation("game session service counter: " + scopedGameSessionService.GetGssc());
                scopedGameSessionService.RoundController();
                await Task.Delay(1000, cancellationToken);
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical(e.Message);
            throw;
        }
    }
}