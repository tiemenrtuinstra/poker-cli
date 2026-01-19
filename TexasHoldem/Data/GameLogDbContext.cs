using Microsoft.EntityFrameworkCore;
using TexasHoldem.Data.Configuration;
using TexasHoldem.Data.Entities;

namespace TexasHoldem.Data;

public class GameLogDbContext : DbContext
{
    private readonly DatabaseSettings _settings;

    public DbSet<SessionEntity> Sessions => Set<SessionEntity>();
    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();
    public DbSet<HandEntity> Hands => Set<HandEntity>();
    public DbSet<HandParticipantEntity> HandParticipants => Set<HandParticipantEntity>();
    public DbSet<ActionEntity> Actions => Set<ActionEntity>();
    public DbSet<CommunityCardEntity> CommunityCards => Set<CommunityCardEntity>();
    public DbSet<OutcomeEntity> Outcomes => Set<OutcomeEntity>();

    // Settings tables (single row each)
    public DbSet<ProgramSettingsEntity> ProgramSettings => Set<ProgramSettingsEntity>();
    public DbSet<GameDefaultsEntity> GameDefaults => Set<GameDefaultsEntity>();

    public GameLogDbContext(DatabaseSettings settings)
    {
        _settings = settings;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(_settings.ConnectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Session configuration
        modelBuilder.Entity<SessionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StartedAt).IsRequired();
            entity.HasOne(e => e.Winner)
                .WithMany()
                .HasForeignKey(e => e.WinnerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Player configuration
        modelBuilder.Entity<PlayerEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PlayerType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Personality).HasMaxLength(50);
            entity.Property(e => e.AiProvider).HasMaxLength(50);
            entity.HasIndex(e => e.Name);
        });

        // Hand configuration
        modelBuilder.Entity<HandEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StartedAt).IsRequired();
            entity.HasOne(e => e.Session)
                .WithMany(s => s.Hands)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.HandNumber);
        });

        // HandParticipant configuration
        modelBuilder.Entity<HandParticipantEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FinalStatus).IsRequired().HasMaxLength(20);
            entity.Property(e => e.HoleCards).HasMaxLength(20);
            entity.HasOne(e => e.Hand)
                .WithMany(h => h.Participants)
                .HasForeignKey(e => e.HandId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Player)
                .WithMany(p => p.HandParticipations)
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.HandId);
            entity.HasIndex(e => e.PlayerId);
        });

        // Action configuration
        modelBuilder.Entity<ActionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BettingPhase).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ActionType).IsRequired().HasMaxLength(20);
            entity.HasOne(e => e.Hand)
                .WithMany(h => h.Actions)
                .HasForeignKey(e => e.HandId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Player)
                .WithMany(p => p.Actions)
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.HandId);
            entity.HasIndex(e => e.PlayerId);
            entity.HasIndex(e => new { e.HandId, e.ActionOrder });
        });

        // CommunityCard configuration
        modelBuilder.Entity<CommunityCardEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BettingPhase).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CardSuit).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CardRank).IsRequired().HasMaxLength(20);
            entity.HasOne(e => e.Hand)
                .WithMany(h => h.CommunityCards)
                .HasForeignKey(e => e.HandId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.HandId);
        });

        // Outcome configuration
        modelBuilder.Entity<OutcomeEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PotType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.HandStrength).HasMaxLength(50);
            entity.Property(e => e.HandDescription).HasMaxLength(200);
            entity.HasOne(e => e.Hand)
                .WithMany(h => h.Outcomes)
                .HasForeignKey(e => e.HandId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Player)
                .WithMany(p => p.Outcomes)
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.HandId);
            entity.HasIndex(e => e.PlayerId);
        });

        // ProgramSettings configuration (single row)
        modelBuilder.Entity<ProgramSettingsEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EnabledProviders).HasMaxLength(100);
            entity.Property(e => e.ClaudeModel).HasMaxLength(100);
            entity.Property(e => e.GeminiModel).HasMaxLength(100);
            entity.Property(e => e.OpenAiModel).HasMaxLength(100);
            entity.Property(e => e.DefaultHumanNames).HasMaxLength(500);
            entity.Property(e => e.CustomAiNames).HasMaxLength(1000);
            entity.Property(e => e.LogLevel).HasMaxLength(20);
        });

        // GameDefaults configuration (single row)
        modelBuilder.Entity<GameDefaultsEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
    }
}
