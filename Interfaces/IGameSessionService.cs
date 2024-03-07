using QuizGame.Models;

namespace QuizGame.Interfaces;

public interface IGameSessionService
{
    public void SetReadyStatusForPlayer(string playerId, int invitationCode, string connectionId, bool isReady);
    public void SetAnswer(string playerId, string answerId, string connectionId, int invitationCode);
    public void RoundController();
    public Task CreateGameSession(Player player, string connectionId, int invitationCode);
    public Task JoinGameSession(int invitationCode, Player player, string connectionId);
    public Task StartNewGameWithSameLobby(string playerId, int invitationCode);
    public void DeletePlayerFromSession(int invitationCode, string playerId);
}