using QuizGame.Models;
using QuizGame.Models.ViewModels;

namespace QuizGame.Hubs;

public interface IGameHub
{
    Task SendError(string message);
    Task SendPlayerId(string playerId);
    Task SendTimer(int timeLeft);
    Task SendInfoAboutRoundExpiration(bool roundExpired);
    Task SendGameSessionInfo(GameSessionViewModel gameSessionViewModel);
    Task SendRoundInfo(RoundViewModel roundViewModel);
}