using QuizGame.Models;

namespace QuizGame.Services;

public interface IGameSessionService : IDisposable
{
    public void SetReadyStatusForPlayer(string playerId, int invitationCode, string connectionId);
    public void SetAnswer(string playerId, string answerId, string connectionId);
    public void RoundController();
    public int GetGssc();
}