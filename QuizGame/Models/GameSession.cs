using System.Text.Json.Serialization;

namespace QuizGame.Models;

public class GameSession
{
    public Guid Id { get; set; }
    public int InvitationCode { get; set; }
    public List<Player> PlayersInSession { get; set; } = new();
    public List<Category> ExpiredCategories { get; set; } = new();

    public List<Question> ExpiredQuestions { get; set; } = new();

    // public List<Category> CategoriesSelectedForVoting { get; set; }
    // public Dictionary<Player, Guid> VotingList { get; set; }
    public string SignalRGroupId { get; set; } = "";
    [JsonIgnore]
    public Round CurrentRound { get; set; } = new();
    public bool IsGameOver { get; set; }
    public bool IsTimerReady { get; set; }
    public bool ArePlayersReady { get; set; }
}