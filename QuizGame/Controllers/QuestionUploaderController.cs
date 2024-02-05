using Microsoft.AspNetCore.Mvc;
using QuizGame.Services;

namespace QuizGame.Controllers;

[ApiController]
[Route("api/quiz")] //nauczyć się o RESTcie
public class QuestionUploaderController : ControllerBase
{
    private readonly QuestionUploaderService _questionUploader;

    public QuestionUploaderController(QuestionUploaderService questionUploader) //interfejsy
    {
        _questionUploader = questionUploader;
    }

    [ActionName("AddQuestions")]
    [HttpPost]
    public IActionResult UpdateRecords() //plik jako stream pewnie albo bitowy
    {
        _questionUploader.UpdateRecordsFromFile();
        return Ok();
    }
}