using System.Collections.Concurrent;
using QuizGame.Models;

namespace QuizGame.Services;

public class MemoryService
{
    public Dictionary<Guid, Player> Players { get; set; } = new();
    public ConcurrentDictionary<Guid, GameSession> GameSessions { get; set; } = new();
    public Dictionary<int, GameSession> GameSessionsByInvCode { get; set; } = new();

    public void AddGameSession(GameSession gameSession, Guid guid)
    {
       GameSessions.TryAdd(guid, gameSession);
       GameSessionsByInvCode.TryAdd(gameSession.InvitationCode, gameSession);
    }
    
    public void AddPlayer(Player player, Guid guid)
    {
        Players.TryAdd(guid, player);
    }

    public void DeleteGameSession(GameSession gameSession)
    {
        GameSessions.TryRemove(gameSession.Id, out var gs);
        GameSessionsByInvCode.Remove(gameSession.InvitationCode);
    }
    public void DeletePlayerFromList(Player player)
    {
        Players.Remove(player.Id);
    }

}