using Microsoft.EntityFrameworkCore;
using Vectis.Web.Data.Entities;

namespace Vectis.Web.Data;

public sealed class VectisDbContext : DbContext
{
    public VectisDbContext(DbContextOptions<VectisDbContext> options) : base(options)
    {
    }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<FamilyEntity> Families => Set<FamilyEntity>();
    public DbSet<FamilyMemberEntity> FamilyMembers => Set<FamilyMemberEntity>();
    public DbSet<BabyEntity> Babies => Set<BabyEntity>();
    public DbSet<PumpingSessionEntity> PumpingSessions => Set<PumpingSessionEntity>();
    public DbSet<MilkContainerEntity> MilkContainers => Set<MilkContainerEntity>();
    public DbSet<StockMovementEntity> StockMovements => Set<StockMovementEntity>();
    public DbSet<PreparedBottleEntity> PreparedBottles => Set<PreparedBottleEntity>();
    public DbSet<PreparedBottleSourceEntity> PreparedBottleSources => Set<PreparedBottleSourceEntity>();
    public DbSet<FeedingEntity> Feedings => Set<FeedingEntity>();
    public DbSet<ConservationRuleEntity> ConservationRules => Set<ConservationRuleEntity>();
    public DbSet<AuditEntryEntity> AuditEntries => Set<AuditEntryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Email).IsUnique();
            entity.Property(item => item.Email).HasMaxLength(320);
        });

        modelBuilder.Entity<FamilyEntity>(entity =>
        {
            entity.ToTable("families");
            entity.HasKey(item => item.Id);
        });

        modelBuilder.Entity<FamilyMemberEntity>(entity =>
        {
            entity.ToTable("family_members");
            entity.HasKey(item => new { item.UserId, item.FamilyId });
            entity.Property(item => item.Role).HasConversion<string>();
        });

        modelBuilder.Entity<BabyEntity>(entity =>
        {
            entity.ToTable("babies");
            entity.HasKey(item => item.Id);
        });

        modelBuilder.Entity<PumpingSessionEntity>(entity =>
        {
            entity.ToTable("pumping_sessions");
            entity.HasKey(item => item.Id);
        });

        modelBuilder.Entity<MilkContainerEntity>(entity =>
        {
            entity.ToTable("milk_containers");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Type).HasConversion<string>();
            entity.Property(item => item.Location).HasConversion<string>();
            entity.Property(item => item.Status).HasConversion<string>();
            entity.HasIndex(item => new { item.BabyId, item.EstimatedExpiresAt });
        });

        modelBuilder.Entity<StockMovementEntity>(entity =>
        {
            entity.ToTable("stock_movements");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.PreviousLocation).HasConversion<string>();
            entity.Property(item => item.NewLocation).HasConversion<string>();
            entity.Property(item => item.PreviousStatus).HasConversion<string>();
            entity.Property(item => item.NewStatus).HasConversion<string>();
        });

        modelBuilder.Entity<PreparedBottleEntity>(entity =>
        {
            entity.ToTable("prepared_bottles");
            entity.HasKey(item => item.Id);
        });

        modelBuilder.Entity<PreparedBottleSourceEntity>(entity =>
        {
            entity.ToTable("prepared_bottle_sources");
            entity.HasKey(item => new { item.PreparedBottleId, item.ContainerId });
        });

        modelBuilder.Entity<FeedingEntity>(entity =>
        {
            entity.ToTable("feedings");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Reaction).HasConversion<string>();
        });

        modelBuilder.Entity<ConservationRuleEntity>(entity =>
        {
            entity.ToTable("conservation_rules");
            entity.HasKey(item => item.Location);
            entity.Property(item => item.Location).HasConversion<string>();
        });

        modelBuilder.Entity<AuditEntryEntity>(entity =>
        {
            entity.ToTable("audit_entries");
            entity.HasKey(item => item.Id);
        });
    }
}
