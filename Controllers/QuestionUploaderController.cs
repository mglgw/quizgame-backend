using Microsoft.AspNetCore.Mvc;
using QuizGame.Services;

namespace QuizGame.Controllers;

[ApiController]
[Route("api/quizuploader")] //nauczyć się o RESTcie
public class QuestionUploaderController : ControllerBase
{
    private readonly QuestionUploaderService _questionUploader;

    public QuestionUploaderController(QuestionUploaderService questionUploader) //interfejsy
    {
        _questionUploader = questionUploader;
    }

    [ActionName("AddQuestions")]
    [HttpPost]
    public async Task<IActionResult> UpdateRecords(IFormFile file) //plik jako stream pewnie albo bitowy
    {
        await _questionUploader.UpdateRecordsFromFile(file);
        return Ok();
    }
}