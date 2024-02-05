namespace QuizGame.Models.ViewModels;

public class RoundViewModel
{
    public string CategoryName { get; set; }
    public string QuestionContent { get; set; }
    public int RoundCounter { get; set; }
    public List<Answer> Answers { get; set; }
}