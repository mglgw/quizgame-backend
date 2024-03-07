using System.Text.Json.Serialization;

namespace QuizGame.Models;

public class Category
{
    public Guid? Id { get; set; }
    [JsonIgnore]
    public List<Question> Questions { get; set; }
    public string Name { get; set; }
}