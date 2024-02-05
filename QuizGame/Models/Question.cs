using System.Text.Json.Serialization;

namespace QuizGame.Models;

public class Question
{
    public Guid Id { get; set; }
    public string Content { get; set; }
    public List<Answer> Answers { get; set; } = new();
    public Answer? CorrectAnswer { get; set; }
    [JsonIgnore]
    public Category Category { get; set; }
}