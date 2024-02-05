using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuizGame.Hubs;
using QuizGame.Models;
using QuizGame.Models.Requests;
using QuizGame.Models.ViewModels;

namespace QuizGame.Services;

public class GameSessionService : IGameSessionService
{
    private readonly DataBaseContext _dataBaseContext;
    private readonly IHubContext<GameHub, IGameHub> _gameHub;
    private readonly MemoryService _memoryService;
    private readonly ILogger<GameSessionService> _logger;
    public static int GameSessionServiceCounter;

    public GameSessionService(DataBaseContext dataBaseContext, IHubContext<GameHub, IGameHub> gameHub, MemoryService memoryService, ILogger<GameSessionService> logger)
    {
        _dataBaseContext = dataBaseContext;
        _gameHub = gameHub;
        _memoryService = memoryService;
        _logger = logger;
        GameSessionServiceCounter++;
    }
    public int GetGssc()
    {
        return GameSessionServiceCounter;
    }
    
    public void SetReadyStatusForPlayer(string playerId, int invitationCode, string connectionId) // game session service
    {
        _logger.LogInformation("started checking if player is ready to play");
        if (!Guid.TryParse(playerId, out var playerGuid))
        {
            _gameHub.Clients.Client(connectionId).SendError("An error occured. Try again.");
            return;
        }
        var gameSession = _memoryService.GameSessions.FirstOrDefault(gameSession =>
            gameSession.Value.InvitationCode == invitationCode).Value;
        if (gameSession == null)
        {
            return;
        }
        var player = gameSession.PlayersInSession.FirstOrDefault(player => player.Id == playerGuid);
        if (player == null)
        {
            _gameHub.Clients.Client(connectionId).SendError("An error occured. Try again.");
            return;
        }
        player.IsReady = true;
        _logger.LogInformation("finished checking if player is ready to play - player is ready");
        CheckPlayersForReadyStatus(gameSession);
    }
    
    private void CheckPlayersForReadyStatus(GameSession gameSession) // game session service
    {
        _logger.LogInformation("started checking if players are ready for game. ");
        int playersCount = gameSession.PlayersInSession.Count;
        int readyPlayersCount = gameSession.PlayersInSession.Count(player => player.IsReady);
        var gameSessionInfoModel = new GameSessionViewModel()
        {
            PlayersInSession = gameSession.PlayersInSession,
            InvitationCode = gameSession.InvitationCode,
            IsGameOver = gameSession.IsGameOver,
            Id = gameSession.Id,
            CurrentRound = gameSession.CurrentRound
        };
        _gameHub.Clients.Groups(gameSession.SignalRGroupId).SendGameSessionInfo(gameSessionInfoModel);
        if (readyPlayersCount == playersCount)
        {
            _logger.LogInformation("players are ready, starting game");
            gameSession.ArePlayersReady = true;
        }
        _logger.LogInformation("players are not yet ready, waiting for isReady flag");
    }
    
    private void StartNewRound(GameSession gameSession)  // game session service
    {
        if (gameSession.IsGameOver || gameSession.CurrentRound.IsRoundOngoing || gameSession.CurrentRound.IsRoundEnding)
        {
            return;
        }
        gameSession.CurrentRound.RoundCounter++;
        gameSession.CurrentRound.RoundTimer = 10;
        gameSession.CurrentRound.IsRoundOngoing = true;
        gameSession.CurrentRound.IsRoundEnding = false;
        
        GetRandomCategories(gameSession);
        SetRandomQuestionFromCategory(gameSession);
        
        foreach (var player in gameSession.PlayersInSession)
        {
            player.SelectedAnswer = null;
        }
        
        //to moze byc jedna wiadomosc
        var roundInfo = new RoundViewModel
        {
            RoundCounter = gameSession.CurrentRound.RoundCounter,
            CategoryName = gameSession.CurrentRound.CurrentCategory.Name,
            QuestionContent = gameSession.CurrentRound.CurrentQuestion.Content,
            Answers = gameSession.CurrentRound.CurrentQuestion.Answers
        };
        _gameHub.Clients.Groups(gameSession.SignalRGroupId).SendRoundInfo(roundInfo);
    }
    
    private void GetRandomCategories(GameSession gameSession)  // game session service
    {
        if (gameSession.IsGameOver)
        {
            return;
        }
        var rnd = new Random();
        int maxCategoryItems = _dataBaseContext.Categories.Count();
        var category = _dataBaseContext.Categories.Skip(rnd.Next(0, maxCategoryItems - 1)).Take(1).First();

        while (gameSession.ExpiredCategories.FirstOrDefault(e => e.Id == category.Id) != null)
        {
            category = _dataBaseContext.Categories.Skip(rnd.Next(0, maxCategoryItems - 1)).Take(1).First();
        }
        var questions = _dataBaseContext.Questions
            .Include(question => question.Answers)
            .Include(question => question.CorrectAnswer)
            .Where(q => q.Category.Id == category.Id)
            .ToList();
        gameSession.ExpiredCategories.Add(category);
        gameSession.CurrentRound.CurrentCategory = category;
        gameSession.CurrentRound.CurrentCategory.Questions.AddRange(questions);
        _logger.LogInformation("Categories saved ");
    }
    
    private void SetRandomQuestionFromCategory(GameSession gameSession)  // game session service
    {
        var rnd = new Random();
        int maxQuestionItems = gameSession.CurrentRound.CurrentCategory.Questions.Count;
        var question = gameSession.CurrentRound.CurrentCategory.Questions.Skip(rnd.Next(0, maxQuestionItems)).Take(1)
            .First();

        if (gameSession.ExpiredQuestions.FirstOrDefault(q => q.Id == question.Id) != null)
        {
            return;
        }
        gameSession.CurrentRound.CurrentQuestion = question;
        gameSession.ExpiredQuestions.Add(question);
        _logger.LogInformation("Questions saved");
    }
    
    public void RoundController() // game session service
    {
        foreach (var gameSession in _memoryService.GameSessions.Values)
        {
            if (gameSession.IsGameOver || !gameSession.ArePlayersReady)
            {
                return;
            }
            if (gameSession.CurrentRound.RoundTimer <= 0)
            {
                StartNewRound(gameSession);
            }
            
            if (gameSession.CurrentRound.IsRoundOngoing)
            {
                SendTimer(gameSession);
            }
            
            if (gameSession.CurrentRound.IsRoundEnding)
            {
                EndRound(gameSession);
            }
        }
    }
    
    private void SendTimer(GameSession gameSession) // game session service
    {
        if (gameSession.IsTimerReady)
        {
            _logger.LogInformation("timer started");
            _gameHub.Clients.Groups(gameSession.SignalRGroupId).SendTimer(gameSession.CurrentRound.RoundTimer);
            gameSession.CurrentRound.RoundTimer--;
            if (gameSession.CurrentRound.RoundTimer < 0)
            {
                gameSession.IsTimerReady = false;
                gameSession.CurrentRound.IsRoundOngoing = false;
                gameSession.CurrentRound.IsRoundEnding = true;
                return;
            }
            
            _logger.LogInformation("timer finished, status of 'isRoundOngoing' => " + gameSession.CurrentRound.IsRoundOngoing);
        }
    }
    
    private void EndRound(GameSession gameSession) // game session service
    {
        _logger.LogInformation("started end round phase");
        CheckAnswers(gameSession);
        CheckGameOverConditions(gameSession);
        if (!gameSession.IsGameOver)
        {
            _logger.LogInformation("end round phase done, restarting timer of background services");
            gameSession.IsTimerReady = true;
        }
        else
        {
            _logger.LogInformation("end round phase finished, game is over");
        }
    }
    
    public void SetAnswer(string playerId, string answerId, string connectionId)  // game session service
    {
        if (!Guid.TryParse(playerId, out var playerGuid))
        {
            _gameHub.Clients.Client(connectionId).SendError("Error occured while registering your answer");
            return;
        }
        if (!Guid.TryParse(answerId, out var answerGuid))
        {
            _gameHub.Clients.Client(connectionId).SendError("Error occured while registering your answer");
            return;
        }
        _memoryService.Players.TryGetValue(playerGuid, out var player);
        if (player != null &&
            (player.SelectedAnswer?.Id != answerGuid || player.SelectedAnswer == null))
        {
            player.SelectedAnswer = _dataBaseContext.Answers.First(a => a.Id == answerGuid);
        }
    }
    
    private void CheckAnswers(GameSession gameSession)  // game session service 
    {
        _gameHub.Clients.Groups(gameSession.SignalRGroupId).SendInfoAboutRoundExpiration(true);
        _logger.LogInformation("started checking answers");
        foreach (var player in gameSession.PlayersInSession)
        {
            if (gameSession.CurrentRound.CurrentQuestion.CorrectAnswer != null && player.SelectedAnswer != null && player.SelectedAnswer.Id == gameSession.CurrentRound.CurrentQuestion.CorrectAnswer.Id)
            {
                player.Score += gameSession.CurrentRound.RoundCounter;
                player.Streak++;
            }
            else if (player.SelectedAnswer == null || gameSession.CurrentRound.CurrentQuestion.CorrectAnswer != null && player.SelectedAnswer.Id != gameSession.CurrentRound.CurrentQuestion.CorrectAnswer.Id)
            {
                player.Streak = 0;
            }
        }
        var gameSessionInfoModel = new GameSessionViewModel()
        {
            PlayersInSession = gameSession.PlayersInSession,
            InvitationCode = gameSession.InvitationCode,
            IsGameOver = gameSession.IsGameOver,
            Id = gameSession.Id,
            CurrentRound = gameSession.CurrentRound
        };
        _logger.LogInformation("finished checking answers");
        _gameHub.Clients.Groups(gameSession.SignalRGroupId).SendGameSessionInfo(gameSessionInfoModel);
    }
    
    private void CheckGameOverConditions(GameSession gameSession)  // game session service
    {
        _logger.LogInformation("started checking game over conditions");
        if (gameSession.CurrentRound.RoundCounter >= 10)
        {
            var scoreboard = gameSession.PlayersInSession.Where(p => p.Score >= 1).ToList().GroupBy(p => p.Score)
                .Select(s => new
                {
                    score = s.Key,
                    player = s.Select(p => p.Id),
                    count = s.Count()
                }).ToList().OrderByDescending(s => s.score).ToList();
            if (scoreboard.Count > 0 && scoreboard[0].count == 1)
            {
                gameSession.PlayersInSession.FirstOrDefault(p => p.Id == scoreboard[0].player.First())!.IsWinner = true;
                gameSession.IsGameOver = true;
                gameSession.IsTimerReady = false;
                gameSession.CurrentRound.IsRoundOngoing = false;
                var gameSessionInfoModel = new GameSessionViewModel()
                {
                    PlayersInSession = gameSession.PlayersInSession,
                    InvitationCode = gameSession.InvitationCode,
                    IsGameOver = gameSession.IsGameOver,
                    Id = gameSession.Id,
                    CurrentRound = gameSession.CurrentRound
                };
                _logger.LogInformation("finished checking game over conditions - got a winner and game over flag");
                _gameHub.Clients.Groups(gameSession.SignalRGroupId).SendGameSessionInfo(gameSessionInfoModel);
            }
            else
            {
                _logger.LogInformation("finished checking game over conditions - game is still on");
                gameSession.CurrentRound.IsRoundEnding = false;
            }
        }
        else
        {
            _logger.LogInformation("finished checking game over conditions - game is still on");
            gameSession.CurrentRound.IsRoundEnding = false;
        }
    }

    public void Dispose()
    {
        GameSessionServiceCounter--;
    }
    

}
