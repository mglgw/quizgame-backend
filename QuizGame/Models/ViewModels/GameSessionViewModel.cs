namespace QuizGame.Models.ViewModels;

public class GameSessionViewModel
{
    public Guid Id { get; set; }
    public int InvitationCode { get; set; }
    public List<Player> PlayersInSession { get; set; } = new();
    public Round? CurrentRound { get; set; }
    public bool IsGameOver { get; set; }

    public static explicit operator GameSessionViewModel(GameSession gameSession)
    {
        return new GameSessionViewModel
        {
            Id = gameSession.Id,
            InvitationCode = gameSession.InvitationCode,
            IsGameOver = gameSession.IsGameOver,
            PlayersInSession = gameSession.PlayersInSession
        };
    }
}