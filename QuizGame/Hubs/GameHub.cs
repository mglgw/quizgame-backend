using Microsoft.AspNetCore.SignalR;
using QuizGame.Models;
using QuizGame.Services;

namespace QuizGame.Hubs;

public class GameHub : Hub<IGameHub>
{
    private readonly IGameService _gameService;
    private readonly IGameSessionService _gameSessionService;

    public GameHub(IGameService gameService, IGameSessionService gameSessionService)
    {
        _gameService = gameService;
        _gameSessionService = gameSessionService;
    }

    public void CreateNewGameSession(string playerName)
    {
        _gameService.CreateGameSession(playerName, Context.ConnectionId);
    }
    
    public void JoinGame(int invitationCode, string playerName)
    {
        _gameService.JoinGameSession(invitationCode, playerName, Context.ConnectionId);
    }
    
    public void SetPlayerAnswer(string playerId, string answerId)
    {
        _gameSessionService.SetAnswer(playerId, answerId, Context.ConnectionId);
    }
    
    public void SetReadyStatus(string playerId, int invitationCode)
    {
        _gameSessionService.SetReadyStatusForPlayer(playerId, invitationCode, Context.ConnectionId);
    }
}