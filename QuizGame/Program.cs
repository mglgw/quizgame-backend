using System.Text.Json.Serialization;
using NLog;
using NLog.Web;
using QuizGame.Hubs;
using QuizGame.Models.Requests;
using QuizGame.Services;

string myAllowSpecificOrigins = "_myAllowSpecificOrigins"; //albo config albo plik z CONSTami
var builder = WebApplication.CreateBuilder(args);

#region services
builder.Services.AddHttpContextAccessor();
builder.Host.UseNLog();
builder.Services.AddControllers()
    .AddJsonOptions(
        options => options.JsonSerializerOptions.ReferenceHandler =
            ReferenceHandler.IgnoreCycles
    );
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IGameSessionService, GameSessionService>();
builder.Services.AddDbContext<DataBaseContext>();
builder.Services.AddScoped<QuestionUploaderService>(); //interfejs
builder.Services.AddSingleton<MemoryService>(); //interfejs
builder.Services.AddHostedService<ScopedBackgroundService>();
builder.Services.AddSignalR(options => { options.EnableDetailedErrors = true; });
builder.Services.AddCors(options =>
{
    options.AddPolicy(myAllowSpecificOrigins,
        policy =>
        {
            policy.AllowAnyMethod();
            policy.AllowAnyHeader();
            policy.SetIsOriginAllowed(_ => true);
        });
});

#endregion
var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
try
{
    app.UseCors(myAllowSpecificOrigins);
    app.MapHub<GameHub>("/quiz"); //interfejs?
    app.MapControllers();
    app.Run();
}
catch (Exception e)
{
    logger.Error(e, "Stopped program because of exception");
    throw;
}
finally
{
   NLog.LogManager.Shutdown();
}