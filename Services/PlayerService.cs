using Microsoft.AspNetCore.SignalR;
using QuizGame.Hubs;
using QuizGame.Interfaces;
using QuizGame.Models;

namespace QuizGame.Services;

public class PlayerService : IPlayerService
{
    private readonly MemoryService _memoryService;
    private readonly ILogger<PlayerService> _logger;
    private readonly IHubContext<GameHub, IGameHub> _gameHub;
  
    public PlayerService(MemoryService memoryService, ILogger<PlayerService> logger, IHubContext<GameHub, IGameHub> gameHub )
    {
        _memoryService = memoryService;
        _logger = logger;
        _gameHub = gameHub;
    }
    
    public Player CreatePlayer(string playerName, string connectionId)
    {
        if (!CheckNickname(playerName, connectionId))
        {
            return null;
        }
        var player = new Player
        {
            Name = playerName,
            ConnectionId = connectionId,
            Id = Guid.NewGuid(),
            Score = 0
        };

        _memoryService.AddPlayer(player, player.Id);
        _logger.LogInformation("Player Created" + playerName);

        return player;
    }
    
    public bool CheckNickname(string playerName, string connectionId)
    {
        if (string.IsNullOrWhiteSpace(playerName) || playerName.Length <= 2)
        {
            _gameHub.Clients.Client(connectionId)
                .SendError("Invalid nickname. Your nickname has to contain at least 3 alphanumeric characters.");
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(playerName)|| playerName.Length <= 10)
        {
            return true;
        }
        _gameHub.Clients.Client(connectionId)
            .SendError("Invalid nickname. Your nickname has to contain at least 3 and no more than 10 alphanumeric characters.");
        
        return false;
    }
    public void DeletePlayerOnDemand(string playerId)
    {
        bool found = _memoryService.Players.TryGetValue(Guid.Parse(playerId), out var player);
        if (found && player!=null && !player.IsReady)
        {
            _memoryService.DeletePlayerFromList(player);
        }
    }
}
