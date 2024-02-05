using QuizGame.Models;

namespace QuizGame.Services;

public interface IGameService : IDisposable
{
    public Task CreateGameSession(string playerName, string connectionId);
    public Task JoinGameSession(int invitationCode, string playerName, string connectionId);
    public int GetGsc();

}