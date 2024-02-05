using Microsoft.AspNetCore.SignalR;
using QuizGame.Hubs;
using QuizGame.Models;
using QuizGame.Models.Requests;
using QuizGame.Models.ViewModels;

namespace QuizGame.Services;

public class GameService : IGameService
{
    private readonly DataBaseContext _dataBaseContext;
    private readonly IHubContext<GameHub, IGameHub> _gameHub;
    private readonly MemoryService _memoryService;
    private readonly ILogger<GameService> _logger;
    public static int GameServiceCounter;


    public GameService(MemoryService memoryService, DataBaseContext dataBaseContext,
        IHubContext<GameHub, IGameHub> gameHub, ILogger<GameService> logger)
    {
        _memoryService = memoryService;
        _dataBaseContext = dataBaseContext;
        _gameHub = gameHub;
        _logger = logger;
        GameServiceCounter++;
    }

    public int GetGsc()
    {
        return GameServiceCounter;
    }
    
    public async Task CreateGameSession(string playerName, string connectionId) // game service
    { 
        _logger.LogDebug("starting new session");
        if (CheckNickname(playerName, connectionId) == false)
        {
            _logger.LogError("Error caused by wrong nickname");
            return;
        }
        var rnd = new Random();
        var player = CreatePlayer(playerName, connectionId);
        var gameSession = new GameSession
        {
            Id = Guid.NewGuid(),
            InvitationCode = rnd.Next()
        };

        gameSession.PlayersInSession.Add(player);
        gameSession.SignalRGroupId = gameSession.Id.ToString();
        gameSession.IsTimerReady = true;

        _memoryService.AddGameSession(gameSession, gameSession.Id);
        _logger.LogInformation("CreatedGameSession");

        var gameSessionViewModel = (GameSessionViewModel)gameSession;
        await _gameHub.Groups.AddToGroupAsync(player.ConnectionId, gameSession.SignalRGroupId);
        await _gameHub.Clients.Client(connectionId).SendPlayerId(player.Id.ToString());
        await _gameHub.Clients.Groups(gameSession.SignalRGroupId).SendGameSessionInfo(gameSessionViewModel);
    }

    public async Task JoinGameSession(int invitationCode, string playerName, string connectionId)  // game service
    {
        if (CheckNickname(playerName, connectionId) == false)
        {
            _logger.LogError("Error caused by wrong nickname");
            return;
        }
        var found = _memoryService.GameSessionsByInvCode.TryGetValue(invitationCode, out var gameSession);
        if (found&& gameSession != null && gameSession.CurrentRound.RoundCounter >0 )
        {
            _logger.LogError("Error joining to lobby");
            await _gameHub.Clients.Client(connectionId).SendError("Unable to join, game already started!");
            return;
        }

        if (found && gameSession != null && gameSession.PlayersInSession.Count <= 3)
        {
            var gameSessionViewModel = (GameSessionViewModel)gameSession;
            var player = CreatePlayer(playerName, connectionId);

            gameSession.PlayersInSession.Add(player);
            _logger.LogInformation("Succeeded to join to lobby");

            await _gameHub.Groups.AddToGroupAsync(player.ConnectionId, gameSession.SignalRGroupId);
            await _gameHub.Clients.Client(connectionId).SendPlayerId(player.Id.ToString());
            await _gameHub.Clients.Groups(gameSession.SignalRGroupId).SendGameSessionInfo(gameSessionViewModel);
        }
        else
        {
            _logger.LogError("Error joining to lobby");
            await _gameHub.Clients.Client(connectionId).SendError("Unable to join lobby");
        }
    }

    private Player CreatePlayer(string playerName, string connectionId)  // game service
    {
        var player = new Player
        {
            Name = playerName,
            ConnectionId = connectionId,
            Id = Guid.NewGuid(),
            Score = 0
        };

        _memoryService.AddPlayer(player, player.Id);
        _logger.LogInformation("Player Created");

        return player;
    }
    
    private bool CheckNickname(string playerName, string connectionId) // game service
    {
        if (playerName != "" && playerName.Length >= 4)
        {
            return true;
        }
        _gameHub.Clients.Client(connectionId)
            .SendError("Invalid nickname. Your nickname cannot be shorter than 4 characters");
        
        return false;
    }
    public void Dispose()
    {
        _dataBaseContext.Dispose();
        GameServiceCounter--;
    }
    
}