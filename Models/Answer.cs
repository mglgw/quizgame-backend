using System.Text.Json.Serialization;

namespace QuizGame.Models;

public class Answer
{
    public Guid Id { get; set; }
    public string Content { get; set; }
    [JsonIgnore]
    public Question? AlignedQuestion { get; set; }
}