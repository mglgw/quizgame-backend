using Microsoft.EntityFrameworkCore;

namespace QuizGame.Models.Requests;

public class DataBaseContext : DbContext
{
    protected readonly IConfiguration Configuration;

    public DbSet<Answer> Answers { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Question> Questions { get; set; }
    
    public DataBaseContext(IConfiguration configuration)
    {
        Configuration = configuration;
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Question>()
            .HasOne(e => e.Category);
        modelBuilder.Entity<Question>()
            .HasOne(e => e.CorrectAnswer);
        modelBuilder.Entity<Question>()
            .HasMany(e => e.Answers)
            .WithOne(e => e.AlignedQuestion);
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(Configuration.GetConnectionString("WebApiDatabase"));
        optionsBuilder.EnableSensitiveDataLogging();
    }
}