using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuizGame.Hubs;
using QuizGame.Interfaces;
using QuizGame.Models;
using QuizGame.Models.Requests;
using QuizGame.Models.ViewModels;

namespace QuizGame.Services;

public class GameSessionService : IGameSessionService
{
    private readonly DataBaseContext _dataBaseContext;
    private readonly IHubContext<GameHub, IGameHub> _gameHub;
    private readonly ILogger<GameSessionService> _logger;
    private readonly MemoryService _memoryService;

    public GameSessionService(DataBaseContext dataBaseContext,
        IHubContext<GameHub, IGameHub> gameHub,
        MemoryService memoryService,
        ILogger<GameSessionService> logger)
    {
        _dataBaseContext = dataBaseContext;
        _gameHub = gameHub;
        _memoryService = memoryService;
        _logger = logger;
    }

    #region Create&Join
    
    public async Task CreateGameSession(Player player, string connectionId, int invitationCode)
    {
        _logger.LogInformation("starting new session");
        var rnd = new Random();
        var gameSession = new GameSession
        {
            Id = Guid.NewGuid(),
            InvitationCode = rnd.Next()
        };
        while (_memoryService.GameSessionsByInvCode.ContainsKey(gameSession.InvitationCode))
        {
            gameSession.InvitationCode = rnd.Next();
        }
        if (invitationCode != 0 && _memoryService.GameSessionsByInvCode.TryGetValue(invitationCode, out var gs))
        {
            _memoryService.DeleteGameSession(gs);
            gameSession.InvitationCode = invitationCode;
        }

        gameSession.PlayersInSession.Add(player);
        player.LastActivity = DateTime.UtcNow;
        gameSession.SignalRGroupId = gameSession.Id.ToString();
        gameSession.IsTimerReady = true;

        _memoryService.AddGameSession(gameSession, gameSession.Id);
        _logger.LogInformation("CreatedGameSession");

        await _gameHub.Groups.AddToGroupAsync(player.ConnectionId, gameSession.SignalRGroupId);
        await _gameHub.Clients.Client(connectionId).SendPlayerId(player.Id.ToString());
        SendGameSessionInfo(gameSession);
    }

    public async Task JoinGameSession(int invitationCode, Player player, string connectionId)
    {
        bool found = _memoryService.GameSessionsByInvCode.TryGetValue(invitationCode, out var gameSession);
        if (found && gameSession != null && gameSession.CurrentRound.RoundCounter > 0)
        {
            if (gameSession.IsGameOver)
            {
                _logger.LogError("Error joining to lobby - game is already over");
                await _gameHub.Clients.Client(connectionId).SendError("Unable to join, game is already over!");
                return;
            }
            if (gameSession.CurrentRound.RoundCounter != 0)
            {
                _logger.LogError("Error joining to lobby - game already started");
                await _gameHub.Clients.Client(connectionId).SendError("Unable to join, game already started!");
                return;
            }
        }
        if (!found)
        {
            _logger.LogError("Error joining to lobby - wrong inv code");
            await _gameHub.Clients.Client(connectionId).SendError("Unable to join, check your invitation code!");
            return;
        }

        if (found && gameSession != null && gameSession.PlayersInSession.Count <= 6)
        {
            gameSession.PlayersInSession.Add(player);
            player.LastActivity = DateTime.UtcNow;
            _logger.LogInformation("Succeeded to join to lobby");

            await _gameHub.Groups.AddToGroupAsync(player.ConnectionId, gameSession.SignalRGroupId);
            await _gameHub.Clients.Client(connectionId).SendPlayerId(player.Id.ToString());
            SendGameSessionInfo(gameSession);
        }
        else
        {
            _logger.LogError("Error joining to lobby - lobby reached max amount of players");
            await _gameHub.Clients.Client(connectionId)
                .SendError("Unable to join lobby, reached maximum number of players");
        }
    }
    
    public void DeletePlayerFromSession(int invitationCode, string playerId)
    {
        bool found = _memoryService.GameSessionsByInvCode.TryGetValue(invitationCode, out var gameSession);
        if (!found || gameSession == null || !Guid.TryParse(playerId, out var playerGuid))
        {
            return;
        }
        var player = gameSession.PlayersInSession.FirstOrDefault(p => p.Id == playerGuid);
        if (player!=null)
        {
            gameSession.PlayersInSession.Remove(player);
            CheckPlayersForReadyStatus(gameSession);
            SendGameSessionInfo(gameSession);
        }
    }
    
    public async Task StartNewGameWithSameLobby(string playerId, int invitationCode)
    {
        if (Guid.TryParse(playerId, out var playerGuid) && _memoryService.Players.TryGetValue(playerGuid,
                                                            out var player)
                                                        && _memoryService.GameSessionsByInvCode.TryGetValue(
                                                            invitationCode, out var gameSession))
        {
            ResetPlayerProgress(player);
            if (gameSession.IsGameOver)
            {
                await CreateGameSession(player, player.ConnectionId, invitationCode);
                await _gameHub.Clients.Client(player.ConnectionId).SendInfoAboutLobbyWipe(true);
            }
            else if (gameSession.ArePlayersReady)
            {
                await CreateGameSession(player, player.ConnectionId, 0);
                await _gameHub.Clients.Client(player.ConnectionId).SendInfoAboutLobbyWipe(true);
            }
            else
            {
                await JoinGameSession(invitationCode, player, player.ConnectionId);
                await _gameHub.Clients.Client(player.ConnectionId).SendInfoAboutLobbyWipe(true);
            }
        }
    }
    
    private void CheckPlayersForReadyStatus(GameSession gameSession)
    {
        int playersCount = gameSession.PlayersInSession.Count;
        int readyPlayersCount = gameSession.PlayersInSession.Count(player => player.IsReady);
        SendGameSessionInfo(gameSession);
        if (readyPlayersCount == playersCount)
        {
            _logger.LogInformation("players are ready, starting game");
            gameSession.ArePlayersReady = true;
            gameSession.CurrentRound.RoundBreakTimer = 1;
            return;
        }
        _logger.LogInformation("players are not yet ready, waiting for isReady flag");
    }

    #endregion

    #region PlayersInSessionControllers
    
    public void SetReadyStatusForPlayer(string playerId, int invitationCode, string connectionId, bool isReady)
    {
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
        if (gameSession.ArePlayersReady)
        {
            _gameHub.Clients.Client(connectionId).SendError("Too late to back out!");
            return;
        }
        player.IsReady = isReady switch
        {
            true => true,
            false => false
        };
        _logger.LogInformation("finished checking if player is ready to play - player is ready");
        CheckPlayersForReadyStatus(gameSession);
    }
    
    public void SetAnswer(string playerId, string answerId, string connectionId, int invitationCode)
    {
        bool found = _memoryService.GameSessionsByInvCode.TryGetValue(invitationCode, out var gameSession);
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
        if (found && gameSession!= null)
        {
            _memoryService.Players.TryGetValue(playerGuid, out var player);
            if (player != null &&
                (player.SelectedAnswer?.Id != answerGuid || player.SelectedAnswer == null))
            {
                player.SelectedAnswer = _dataBaseContext.Answers.First(a => a.Id == answerGuid);
            }
            SendGameSessionInfo(gameSession);
        }
    }
    
    private static void ResetPlayerProgress(Player player)
    {
        player.IsReady = false;
        player.IsWinner = false;
        player.Streak = 0;
        player.Score = 0;
        player.SelectedAnswer = null;
    }
    #endregion

    #region Game&RoundFlow
    public void RoundController()
    {
        foreach (var gameSession in _memoryService.GameSessions.Values)
        {
            if (gameSession.IsGameOver || !gameSession.ArePlayersReady)
            {
                CleanUpGameSession(gameSession);
                continue;
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
        
        foreach (var player in _memoryService.Players.Where(p 
                     => p.Value.LastActivity.AddMinutes(30) < DateTime.UtcNow))
        {
            _memoryService.DeletePlayerFromList(player.Value);
        }
    }
    private void StartNewRound(GameSession gameSession)
    {
        if (gameSession.IsGameOver || gameSession.CurrentRound.IsRoundOngoing || gameSession.CurrentRound.IsRoundEnding)
        {
            return;
        }
        if (gameSession.CurrentRound.RoundBreakTimer >= 0)
        {
            if (gameSession.CurrentRound.RoundCounter == 0)
            {
                AwaitForNextRound(gameSession, "Game is starting, brace yourself! ");
                return;
            }
            AwaitForNextRound(gameSession, "End of round! Prepare for next question! ");
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

        var roundInfo = new RoundViewModel
        {
            RoundCounter = gameSession.CurrentRound.RoundCounter,
            CategoryName = gameSession.CurrentRound.CurrentCategory.Name,
            QuestionContent = gameSession.CurrentRound.CurrentQuestion.Content,
            Answers = gameSession.CurrentRound.CurrentQuestion.Answers
        };
        _gameHub.Clients.Groups(gameSession.SignalRGroupId).SendRoundInfo(roundInfo);
        SendGameSessionInfo(gameSession);
    }
    
    private void GetRandomCategories(GameSession gameSession)
    {
        if (gameSession.IsGameOver)
        {
            return;
        }
        var rnd = new Random();
        int maxCategoryItems = _dataBaseContext.Categories.Count();
        if (gameSession.ExpiredCategories.Count != maxCategoryItems)
        {
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
        }
    }

    private void SetRandomQuestionFromCategory(GameSession gameSession)
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
    
    private void CheckAnswers(GameSession gameSession)
    {
        _gameHub.Clients.Groups(gameSession.SignalRGroupId).SendInfoAboutRoundExpiration(true);
        _logger.LogInformation("started checking answers");
        foreach (var player in gameSession.PlayersInSession)
        {
            if (gameSession.CurrentRound.CurrentQuestion.CorrectAnswer != null && player.SelectedAnswer != null &&
                player.SelectedAnswer.Content == gameSession.CurrentRound.CurrentQuestion.CorrectAnswer.Content)
            {
                player.Score += gameSession.CurrentRound.RoundCounter;
                player.Streak++;
            }
            else if (player.SelectedAnswer == null || gameSession.CurrentRound.CurrentQuestion.CorrectAnswer != null &&
                     player.SelectedAnswer.Id != gameSession.CurrentRound.CurrentQuestion.CorrectAnswer.Id)
            {
                player.Streak = 0;
            }
        }
        _logger.LogInformation("finished checking answers");
        SendGameSessionInfo(gameSession);
    }
    
    private void CheckGameOverConditions(GameSession gameSession)
    {
        _logger.LogInformation("started checking game over conditions");
        if (gameSession.CurrentRound.RoundCounter >= 10)
        {
            var scoreboard = gameSession.PlayersInSession.Where(p => p.Score >= 0).ToList().GroupBy(p => p.Score)
                .Select(s => new
                {
                    score = s.Key,
                    player = s.Select(p => p.Id),
                    count = s.Count()
                }).ToList().OrderByDescending(s => s.score).ToList();
            if (scoreboard.Count > 0 && scoreboard[0].count == 1)
            {
                gameSession.PlayersInSession.FirstOrDefault(p => p.Id == scoreboard[0].player.First())!.IsWinner = true;
                EndGame(gameSession);
                _logger.LogInformation("finished checking game over conditions - got a winner and game over flag");
                SendGameSessionInfo(gameSession);
            }
            else if (scoreboard.Count > 0 && scoreboard[0].count == 0)
            {
                EndGame(gameSession);
                SendGameSessionInfo(gameSession);
                _logger.LogInformation(
                    "finished checking game over conditions - no winner was selected, 0 points achieved");
            }
            else if (scoreboard.Count > 0 && scoreboard[0].count > 1)
            {
                EndGame(gameSession);
                foreach (var playerGuid in scoreboard[0].player)
                {
                    gameSession.PlayersInSession.First(player => player.Id == playerGuid).IsWinner = true;
                }
                SendGameSessionInfo(gameSession);
                _logger.LogInformation("finished checking game over conditions - there is draw between players");
            }
            else
            {
                _logger.LogInformation("finished checking game over conditions - game is still on");
                gameSession.CurrentRound.IsRoundEnding = false;
                gameSession.CurrentRound.RoundBreakTimer = 1;
            }
        }
        else
        {
            _logger.LogInformation("finished checking game over conditions - game is still on");
            gameSession.CurrentRound.IsRoundEnding = false;
            gameSession.CurrentRound.RoundBreakTimer = 1;
        }
    }
    private void AwaitForNextRound(GameSession gameSession, string message)
    {
        if (gameSession.CurrentRound.RoundBreakTimer >= 0)
        {
            _gameHub.Clients.Groups(gameSession.SignalRGroupId).SendMessageToPlayers(
                message);
            gameSession.CurrentRound.RoundBreakTimer--;
        }
    }
    
    private void SendTimer(GameSession gameSession)
    {
        if (gameSession.CurrentRound.RoundTimer >= 0)
        {
            _logger.LogInformation("timer started");
            _gameHub.Clients.Groups(gameSession.SignalRGroupId).SendTimer(gameSession.CurrentRound.RoundTimer);
            gameSession.CurrentRound.RoundTimer--;
            if (gameSession.CurrentRound.RoundTimer < 0)
            {
                gameSession.IsTimerReady = false;
                gameSession.CurrentRound.IsRoundOngoing = false;
                gameSession.CurrentRound.IsRoundEnding = true;
            }
        }
    }
    
    private void EndRound(GameSession gameSession)
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
    
    private void EndGame(GameSession gameSession)
    {
        gameSession.IsGameOver = true;
        gameSession.GameOverDate = DateTime.UtcNow;
        gameSession.IsTimerReady = false;
        gameSession.CurrentRound.IsRoundOngoing = false;
    }
    #endregion

    #region Utillity
    private void SendGameSessionInfo(GameSession gameSession)
    {
        _gameHub.Clients.Groups(gameSession.SignalRGroupId).SendGameSessionInfo((GameSessionViewModel)gameSession);
    }
    private void CleanUpGameSession(GameSession gameSession)
    {
        _logger.LogInformation("Remaining GameSessions:");
        foreach (var gs in _memoryService.GameSessions)
        {
            _logger.LogInformation(gs.Value.InvitationCode.ToString());
        }
        
        if (gameSession.GameSessionCreatedDate.AddMinutes(10) < DateTime.UtcNow && gameSession.CurrentRound.RoundCounter == 0)
        {
            _gameHub.Clients.Groups(gameSession.SignalRGroupId)
                .SendMessageToPlayers("Session closed due to inactivity");
            _memoryService.DeleteGameSession(gameSession);
            return;
        }
        
        if (!gameSession.IsGameOver || gameSession.GameOverDate.AddMinutes(10) > DateTime.UtcNow)
        {
            return;
        }
        
        _logger.LogInformation("GameSession with inv code:" + gameSession.InvitationCode +
                               "expired. Attempting to delete");
        _gameHub.Clients.Groups(gameSession.SignalRGroupId)
            .SendMessageToPlayers("Session closed due to inactivity");
        _memoryService.DeleteGameSession(gameSession);
    }
    

    #endregion
    
    
}