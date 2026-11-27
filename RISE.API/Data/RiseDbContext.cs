using Microsoft.EntityFrameworkCore;
using RISE.API.Models;

namespace RISE.API.Data;

public class RiseDbContext : DbContext
{
    public RiseDbContext(DbContextOptions<RiseDbContext> options)
        : base(options)
    {
    }

    public DbSet<Farmer> Farmers => Set<Farmer>();
    public DbSet<AssistanceProgram> AssistancePrograms => Set<AssistanceProgram>();
    public DbSet<Supply> Supplies => Set<Supply>();
    public DbSet<SupplyCategory> SupplyCategories => Set<SupplyCategory>();
    public DbSet<Distribution> Distributions => Set<Distribution>();
    public DbSet<DistributionItem> DistributionItems => Set<DistributionItem>();
    public DbSet<UtilizationMonitoring> UtilizationMonitorings => Set<UtilizationMonitoring>();
    public DbSet<UtilizationSeasonRecord> UtilizationSeasonRecords => Set<UtilizationSeasonRecord>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Farmer>(entity =>
        {
            entity.Property(farmer => farmer.FullName).HasMaxLength(150);
            entity.Property(farmer => farmer.FirstName).HasMaxLength(60);
            entity.Property(farmer => farmer.LastName).HasMaxLength(60);
            entity.Property(farmer => farmer.MiddleName).HasMaxLength(60);
            entity.Property(farmer => farmer.Province).HasMaxLength(100);
            entity.Property(farmer => farmer.ContactNumber).HasMaxLength(30);
            entity.Property(farmer => farmer.Barangay).HasMaxLength(100);
            entity.Property(farmer => farmer.Municipality).HasMaxLength(100);
            entity.Property(farmer => farmer.IsActive).HasDefaultValue(true);

            entity.HasIndex(farmer => farmer.IsActive);
            entity.HasIndex(farmer => farmer.FullName);
            entity.HasIndex(farmer => new { farmer.FullName, farmer.ContactNumber, farmer.OwnerUserId })
                .IsUnique()
                .HasFilter("[IsActive] = 1");
            entity.HasIndex(farmer => farmer.Barangay);
            entity.HasIndex(farmer => farmer.Municipality);
            entity.HasIndex(farmer => farmer.OwnerUserId);
        });

        modelBuilder.Entity<AssistanceProgram>(entity =>
        {
            entity.Property(program => program.Name).HasMaxLength(200).IsRequired();
            entity.Property(program => program.IsActive).HasDefaultValue(true);
            entity.HasIndex(program => program.Name).IsUnique();
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_AssistancePrograms_DateRange",
                "[EndDate] IS NULL OR [EndDate] >= [StartDate]"));
        });

        modelBuilder.Entity<SupplyCategory>(entity =>
        {
            entity.Property(category => category.Name).HasMaxLength(100).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(500);
            entity.HasIndex(category => new { category.Name, category.OwnerUserId }).IsUnique();
            entity.HasIndex(category => category.OwnerUserId);
        });

        modelBuilder.Entity<Supply>(entity =>
        {
            entity.Property(supply => supply.Name).HasMaxLength(200).IsRequired();
            entity.Property(supply => supply.UnitOfMeasure).HasMaxLength(30).IsRequired();
            entity.Property(supply => supply.StockQuantity).HasPrecision(18, 3);
            entity.Property(supply => supply.IsActive).HasDefaultValue(true);
            entity.HasIndex(supply => supply.SupplyCategoryId);
            entity.HasIndex(supply => supply.OwnerUserId);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Supplies_StockQuantity_NonNegative", "[StockQuantity] >= 0");
            });
            entity.HasOne(supply => supply.Category).WithMany(category => category.Supplies)
                .HasForeignKey(supply => supply.SupplyCategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Distribution>(entity =>
        {
            entity.Property(distribution => distribution.ReferenceNumber).HasMaxLength(80);
            entity.Property(distribution => distribution.Status).HasMaxLength(20).HasDefaultValue("Draft");
            entity.Property(distribution => distribution.Notes).HasMaxLength(2000);
            entity.Property(distribution => distribution.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(distribution => distribution.FarmerId);
            entity.HasIndex(distribution => distribution.AssistanceProgramId);
            entity.HasIndex(distribution => distribution.DistributionDate);
            entity.HasIndex(distribution => distribution.ReferenceNumber)
                .IsUnique().HasFilter("[ReferenceNumber] IS NOT NULL");
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Distributions_Status",
                "[Status] IN (N'Draft', N'Approved', N'Distributed', N'Cancelled')"));
            entity.HasOne(distribution => distribution.Farmer).WithMany(farmer => farmer.Distributions)
                .HasForeignKey(distribution => distribution.FarmerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(distribution => distribution.AssistanceProgram).WithMany(program => program.Distributions)
                .HasForeignKey(distribution => distribution.AssistanceProgramId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(distribution => distribution.CreatedByUser).WithMany(user => user.CreatedDistributions)
                .HasForeignKey(distribution => distribution.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DistributionItem>(entity =>
        {
            entity.Property(item => item.Quantity).HasPrecision(18, 3);
            entity.HasIndex(item => new { item.DistributionId, item.SupplyId }).IsUnique();
            entity.HasIndex(item => item.SupplyId);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_DistributionItems_Quantity_Positive", "[Quantity] > 0");
            });
            entity.HasOne(item => item.Distribution).WithMany(distribution => distribution.Items)
                .HasForeignKey(item => item.DistributionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Supply).WithMany(supply => supply.DistributionItems)
                .HasForeignKey(item => item.SupplyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UtilizationMonitoring>(entity =>
        {
            entity.Property(record => record.QuantityUsed).HasPrecision(18, 3);
            entity.Property(record => record.QuantityStillStocked).HasPrecision(18, 3);
            entity.Property(record => record.QuantityDisposed).HasPrecision(18, 3);
            entity.Property(record => record.Remarks).HasMaxLength(2000);
            entity.HasIndex(record => record.DistributionItemId).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_UtilizationMonitoring_NonNegative",
                    "([QuantityUsed] IS NULL OR [QuantityUsed] >= 0) AND " +
                    "([QuantityStillStocked] IS NULL OR [QuantityStillStocked] >= 0) AND " +
                    "([QuantityDisposed] IS NULL OR [QuantityDisposed] >= 0)");
            });
            entity.HasOne(record => record.DistributionItem)
                .WithOne(item => item.UtilizationMonitoring)
                .HasForeignKey<UtilizationMonitoring>(record => record.DistributionItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(user => user.UserName).HasMaxLength(100).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(254).IsRequired();
            entity.Property(user => user.DisplayName).HasMaxLength(150).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(user => user.IsActive).HasDefaultValue(true);
            entity.Property(user => user.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
    entity.Property(user => user.ProfilePicturePath).HasMaxLength(500);
            entity.HasIndex(user => user.UserName).IsUnique();
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.Property(role => role.Name).HasMaxLength(100).IsRequired();
            entity.Property(role => role.Description).HasMaxLength(500);
            entity.HasIndex(role => role.Name).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(userRole => new { userRole.UserId, userRole.RoleId });
            entity.Property(userRole => userRole.AssignedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(userRole => userRole.RoleId);
            entity.HasOne(userRole => userRole.User).WithMany(user => user.UserRoles)
                .HasForeignKey(userRole => userRole.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(userRole => userRole.Role).WithMany(role => role.UserRoles)
                .HasForeignKey(userRole => userRole.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(log => log.Action).HasMaxLength(50).IsRequired();
            entity.Property(log => log.EntityName).HasMaxLength(150).IsRequired();
            entity.Property(log => log.EntityId).HasMaxLength(100);
            entity.Property(log => log.OccurredAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(log => log.OldValuesJson).HasColumnType("nvarchar(max)");
            entity.Property(log => log.NewValuesJson).HasColumnType("nvarchar(max)");
            entity.Property(log => log.IpAddress).HasMaxLength(45);
            entity.HasIndex(log => log.OccurredAtUtc);
            entity.HasIndex(log => new { log.EntityName, log.EntityId });
            entity.HasIndex(log => log.UserId);
            entity.HasOne(log => log.User).WithMany(user => user.AuditLogs)
                .HasForeignKey(log => log.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.Property(token => token.Token).HasMaxLength(500).IsRequired();
            entity.HasIndex(token => token.Token).IsUnique();
            entity.HasIndex(token => token.UserId);
            entity.HasIndex(token => token.ExpiresAtUtc);
            entity.HasOne(token => token.User)
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UtilizationSeasonRecord>(entity =>
        {
            entity.Property(record => record.SeasonKey).HasMaxLength(50).IsRequired();
            entity.Property(record => record.QuantityUsed).HasPrecision(18, 3);
            entity.Property(record => record.QuantityStillStocked).HasPrecision(18, 3);
            entity.Property(record => record.QuantityDisposed).HasPrecision(18, 3);
            entity.HasIndex(record => new { record.SeasonKey, record.DistributionItemId });
            entity.HasOne(record => record.DistributionItem).WithMany()
                .HasForeignKey(record => record.DistributionItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasIndex(setting => setting.Key).IsUnique();
            entity.HasData(new AppSetting { AppSettingId = 1, Key = "ActiveSeason", Value = "2026-Wet" });
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(item => item.Type).HasMaxLength(50).IsRequired();
            entity.Property(item => item.Title).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Message).HasMaxLength(1000).IsRequired();
            entity.HasIndex(item => new { item.UserId, item.IsRead, item.CreatedAtUtc });
            entity.HasOne(item => item.User).WithMany()
                .HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}