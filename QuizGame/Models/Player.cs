namespace QuizGame.Models;

public class Player
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string ConnectionId { get; set; }
    public int Score { get; set; }
    public Answer? SelectedAnswer { get; set; }
    public bool IsWinner { get; set; }
    public bool IsReady { get; set; }
    public int Streak { get; set; }
}