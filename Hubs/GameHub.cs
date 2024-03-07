using Microsoft.AspNetCore.SignalR;
using QuizGame.Interfaces;
using QuizGame.Services;

namespace QuizGame.Hubs;

public class GameHub : Hub<IGameHub>
{
    private readonly IGameSessionService _gameSessionService;
    private readonly PlayerService _playerService;

    public GameHub(IGameSessionService gameSessionService, PlayerService playerService)
    {
        _gameSessionService = gameSessionService;
        _playerService = playerService;
    }

    public void CreateNewGameSession(string playerName, int invitationCode)
    {
        var player = _playerService.CreatePlayer(playerName, Context.ConnectionId);
        if (player != null) 
        {
            _gameSessionService.CreateGameSession(player, Context.ConnectionId, invitationCode);
        }
        
    }
    
    public void JoinGame(int invitationCode, string playerName)
    {
        var player = _playerService.CreatePlayer(playerName, Context.ConnectionId);
        if (player != null)
        {
            _gameSessionService.JoinGameSession(invitationCode, player, Context.ConnectionId);
        }
    }
    
    public void SetPlayerAnswer(string playerId, string answerId, int invitationCode)
    {
        _gameSessionService.SetAnswer(playerId, answerId, Context.ConnectionId, invitationCode);
    }
    
    public void SetReadyStatus(string playerId, int invitationCode, bool isReady)
    {
        _gameSessionService.SetReadyStatusForPlayer(playerId, invitationCode, Context.ConnectionId, isReady);
    }
    
    public void RestartGameWithSameLobby(string playerId, int invitationCode)
    {
        _gameSessionService.StartNewGameWithSameLobby(playerId, invitationCode);
    }
    
    public void QuitGame(string playerId, int invitationCode)
    {
        _gameSessionService.DeletePlayerFromSession(invitationCode, playerId);
        _playerService.DeletePlayerOnDemand(playerId);
    }
}