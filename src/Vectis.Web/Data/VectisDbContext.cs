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
    public DbSet<FamilyInvitationEntity> FamilyInvitations => Set<FamilyInvitationEntity>();
    public DbSet<NotificationPreferencesEntity> NotificationPreferences => Set<NotificationPreferencesEntity>();
    public DbSet<NotificationDeliveryEntity> NotificationDeliveries => Set<NotificationDeliveryEntity>();
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
            entity.HasOne<UserEntity>().WithMany().HasForeignKey(item => item.CreatorUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FamilyMemberEntity>(entity =>
        {
            entity.ToTable("family_members");
            entity.HasKey(item => new { item.UserId, item.FamilyId });
            entity.Property(item => item.Role).HasConversion<string>();
            entity.HasOne<UserEntity>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<FamilyEntity>().WithMany().HasForeignKey(item => item.FamilyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FamilyInvitationEntity>(entity =>
        {
            entity.ToTable("family_invitations");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.FamilyId, item.Email, item.Status });
            entity.Property(item => item.Email).HasMaxLength(320);
            entity.Property(item => item.Role).HasConversion<string>();
            entity.HasOne<FamilyEntity>().WithMany().HasForeignKey(item => item.FamilyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<UserEntity>().WithMany().HasForeignKey(item => item.InvitedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NotificationPreferencesEntity>(entity =>
        {
            entity.ToTable("notification_preferences");
            entity.HasKey(item => item.FamilyId);
            entity.HasOne<FamilyEntity>().WithMany().HasForeignKey(item => item.FamilyId).OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_notification_preferences_stock_threshold_positive", "\"StockLowBottleThreshold\" >= 0");
                table.HasCheckConstraint("CK_notification_preferences_expiring_hours_positive", "\"ExpiringSoonHours\" > 0");
                table.HasCheckConstraint("CK_notification_preferences_bottle_age_positive", "\"PreparedBottleAgeMinutes\" > 0");
            });
        });

        modelBuilder.Entity<NotificationDeliveryEntity>(entity =>
        {
            entity.ToTable("notification_deliveries");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.FamilyId, item.Kind, item.CreatedAt });
            entity.Property(item => item.Kind).HasConversion<string>();
            entity.Property(item => item.RecipientEmail).HasMaxLength(320);
            entity.HasOne<FamilyEntity>().WithMany().HasForeignKey(item => item.FamilyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BabyEntity>(entity =>
        {
            entity.ToTable("babies");
            entity.HasKey(item => item.Id);
            entity.HasOne<FamilyEntity>().WithMany().HasForeignKey(item => item.FamilyId).OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table => table.HasCheckConstraint("CK_babies_usual_bottle_positive", "\"UsualBottleMl\" > 0"));
        });

        modelBuilder.Entity<PumpingSessionEntity>(entity =>
        {
            entity.ToTable("pumping_sessions");
            entity.HasKey(item => item.Id);
            entity.HasOne<BabyEntity>().WithMany().HasForeignKey(item => item.BabyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<UserEntity>().WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_pumping_sessions_total_positive", "\"TotalQuantityMl\" > 0"));
        });

        modelBuilder.Entity<MilkContainerEntity>(entity =>
        {
            entity.ToTable("milk_containers");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Type).HasConversion<string>();
            entity.Property(item => item.Location).HasConversion<string>();
            entity.Property(item => item.Status).HasConversion<string>();
            entity.HasIndex(item => new { item.BabyId, item.EstimatedExpiresAt });
            entity.HasOne<PumpingSessionEntity>().WithMany().HasForeignKey(item => item.PumpingSessionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<BabyEntity>().WithMany().HasForeignKey(item => item.BabyId).OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_milk_containers_initial_positive", "\"InitialQuantityMl\" > 0");
                table.HasCheckConstraint("CK_milk_containers_remaining_range", "\"RemainingQuantityMl\" >= 0 AND \"RemainingQuantityMl\" <= \"InitialQuantityMl\"");
            });
        });

        modelBuilder.Entity<StockMovementEntity>(entity =>
        {
            entity.ToTable("stock_movements");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.PreviousLocation).HasConversion<string>();
            entity.Property(item => item.NewLocation).HasConversion<string>();
            entity.Property(item => item.PreviousStatus).HasConversion<string>();
            entity.Property(item => item.NewStatus).HasConversion<string>();
            entity.HasOne<MilkContainerEntity>().WithMany().HasForeignKey(item => item.ContainerId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<UserEntity>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PreparedBottleEntity>(entity =>
        {
            entity.ToTable("prepared_bottles");
            entity.HasKey(item => item.Id);
            entity.HasOne<BabyEntity>().WithMany().HasForeignKey(item => item.BabyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<UserEntity>().WithMany().HasForeignKey(item => item.PreparedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_prepared_bottles_total_positive", "\"TotalQuantityMl\" > 0"));
        });

        modelBuilder.Entity<PreparedBottleSourceEntity>(entity =>
        {
            entity.ToTable("prepared_bottle_sources");
            entity.HasKey(item => new { item.PreparedBottleId, item.ContainerId });
            entity.HasOne<PreparedBottleEntity>().WithMany().HasForeignKey(item => item.PreparedBottleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<MilkContainerEntity>().WithMany().HasForeignKey(item => item.ContainerId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_prepared_bottle_sources_quantity_positive", "\"QuantityMl\" > 0"));
        });

        modelBuilder.Entity<FeedingEntity>(entity =>
        {
            entity.ToTable("feedings");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Reaction).HasConversion<string>();
            entity.HasOne<BabyEntity>().WithMany().HasForeignKey(item => item.BabyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PreparedBottleEntity>().WithMany().HasForeignKey(item => item.PreparedBottleId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<UserEntity>().WithMany().HasForeignKey(item => item.FedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_feedings_prepared_positive", "\"PreparedQuantityMl\" > 0");
                table.HasCheckConstraint("CK_feedings_consumed_range", "\"ConsumedQuantityMl\" >= 0 AND \"ConsumedQuantityMl\" <= \"PreparedQuantityMl\"");
                table.HasCheckConstraint("CK_feedings_leftover_non_negative", "\"LeftoverQuantityMl\" >= 0");
            });
        });

        modelBuilder.Entity<ConservationRuleEntity>(entity =>
        {
            entity.ToTable("conservation_rules");
            entity.HasKey(item => item.Location);
            entity.Property(item => item.Location).HasConversion<string>();
            entity.ToTable(table => table.HasCheckConstraint("CK_conservation_rules_duration_positive", "\"DurationHours\" > 0"));
        });

        modelBuilder.Entity<AuditEntryEntity>(entity =>
        {
            entity.ToTable("audit_entries");
            entity.HasKey(item => item.Id);
            entity.HasOne<UserEntity>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
