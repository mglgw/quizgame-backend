namespace QuizGame.Models;

public class Round
{
    public int RoundCounter { get; set; }
    public Category CurrentCategory { get; set; }
    public Question CurrentQuestion { get; set; }
    public int RoundTimer { get; set; }
    public bool IsRoundOngoing { get; set; }
    public bool IsRoundEnding { get; set; }
    public int RoundBreakTimer { get; set; }
}