using System.Text;
using QuizGame.Models;
using QuizGame.Models.Requests;

namespace QuizGame.Services;

public class QuestionUploaderService
{
    private readonly DataBaseContext _dbContextContext;

    public QuestionUploaderService(DataBaseContext dataBaseContext)
    {
        _dbContextContext = dataBaseContext;
    }

    public Task UpdateRecordsFromFile(IFormFile file)
    {
        var result = new StringBuilder();
        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            while (reader.Peek() >= 0)
            {
                result.AppendLine(reader.ReadLine());
            }
        }
        
        var srd = new StringReader(result.ToString());
        string? line = srd.ReadLine();
        
        while (line != null)
        {
            Console.WriteLine(line);
            string[] wordsInLine = line.Split(',');
            var question = new Question();
            var category = _dbContextContext.Categories.FirstOrDefault(c => c.Name == wordsInLine[0]);
            if (category == null)
            {
                category = new Category();
                category.Name = wordsInLine[0];
                _dbContextContext.Categories.Add(category);
            }
            if (_dbContextContext.Questions.FirstOrDefault(q => q.Content == wordsInLine[1]) == null)
            {
                question.Category = category;
                question.Content = wordsInLine[1];
                for (int i = 0; i <= 3; i++)
                {
                    var answer = new Answer();
                    answer.Content = wordsInLine[2 + i];
                    question.Answers.Add(answer);
                    _dbContextContext.Answers.Add(answer);
                }
                _dbContextContext.Questions.Add(question);
                _dbContextContext.SaveChanges();
                foreach (var answer in question.Answers)
                {
                    if (answer.Content == wordsInLine[6])
                    {
                        question.CorrectAnswer =
                            _dbContextContext.Answers.FirstOrDefault(e =>
                                answer.Content == e.Content && answer.AlignedQuestion.Id == question.Id);
                    }
                }
                _dbContextContext.SaveChanges();
            }
            line = srd.ReadLine();
        }
        return Task.CompletedTask;
    }
}