using QuizGame.Models;

namespace QuizGame.Interfaces;

public interface IPlayerService
{
    public Player CreatePlayer(string playerName, string connectionId);
    public bool CheckNickname(string playerName, string connectionId);
    public void DeletePlayerOnDemand(string playerId);
}