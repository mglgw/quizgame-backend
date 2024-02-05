namespace QuizGame.Models.ViewModels;

public class QuestionViewModel
{
    public Guid Id { get; set; }
    public string Content { get; set; }
    public List<Answer> Answers { get; set; } = new();

    public static explicit operator QuestionViewModel(Question question)
    {
        return new QuestionViewModel
        {
            Id = question.Id,
            Content = question.Content,
            Answers = question.Answers
        };
    }
}