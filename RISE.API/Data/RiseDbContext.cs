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
    public DbSet<FarmerAssociation> FarmerAssociations => Set<FarmerAssociation>();
    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<AssistanceProgram> AssistancePrograms => Set<AssistanceProgram>();
    public DbSet<Supply> Supplies => Set<Supply>();
    public DbSet<SupplyCategory> SupplyCategories => Set<SupplyCategory>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Distribution> Distributions => Set<Distribution>();
    public DbSet<DistributionItem> DistributionItems => Set<DistributionItem>();
    public DbSet<UtilizationMonitoring> UtilizationMonitorings => Set<UtilizationMonitoring>();
    public DbSet<PlantingRecord> PlantingRecords => Set<PlantingRecord>();
    public DbSet<CropMonitoringRecord> CropMonitoringRecords => Set<CropMonitoringRecord>();
    public DbSet<SupplyUsageRecord> SupplyUsageRecords => Set<SupplyUsageRecord>();
    public DbSet<HarvestRecord> HarvestRecords => Set<HarvestRecord>();
    public DbSet<HarvestVerificationRecord> HarvestVerificationRecords => Set<HarvestVerificationRecord>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SupportingDocument> SupportingDocuments => Set<SupportingDocument>();

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
            entity.Property(farmer => farmer.Address).HasMaxLength(300);
            entity.Property(farmer => farmer.Barangay).HasMaxLength(100);
            entity.Property(farmer => farmer.Municipality).HasMaxLength(100);
            entity.Property(farmer => farmer.IsActive).HasDefaultValue(true);

            entity.HasIndex(farmer => farmer.IsActive);
            entity.HasIndex(farmer => farmer.FullName);
            entity.HasIndex(farmer => new { farmer.FullName, farmer.ContactNumber })
                .IsUnique()
                .HasFilter("[IsActive] = 1");
            entity.HasIndex(farmer => farmer.Barangay);
            entity.HasIndex(farmer => farmer.Municipality);
            entity.HasIndex(farmer => farmer.FarmerAssociationId);

            entity.HasOne(farmer => farmer.Association)
                .WithMany(association => association.Farmers)
                .HasForeignKey(farmer => farmer.FarmerAssociationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FarmerAssociation>(entity =>
        {
            entity.Property(association => association.Name)
                .HasMaxLength(150)
                .IsRequired();
            entity.HasIndex(association => association.Name).IsUnique();
        });

        modelBuilder.Entity<Farm>(entity =>
        {
            entity.Property(farm => farm.Name).HasMaxLength(150).IsRequired();
            entity.Property(farm => farm.Barangay).HasMaxLength(100).IsRequired();
            entity.Property(farm => farm.Municipality).HasMaxLength(100).IsRequired();
            entity.Property(farm => farm.AreaHectares).HasPrecision(12, 4);
            entity.Property(farm => farm.SoilType).HasMaxLength(100);
            entity.Property(farm => farm.IsActive).HasDefaultValue(true);
            entity.HasIndex(farm => farm.FarmerId);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Farms_AreaHectares_Positive", "[AreaHectares] > 0"));
            entity.HasOne(farm => farm.Farmer).WithMany(farmer => farmer.Farms)
                .HasForeignKey(farm => farm.FarmerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AssistanceProgram>(entity =>
        {
            entity.Property(program => program.Name).HasMaxLength(200).IsRequired();
            entity.Property(program => program.Description).HasMaxLength(2000);
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
            entity.HasIndex(category => category.Name).IsUnique();
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.Property(supplier => supplier.Name).HasMaxLength(200).IsRequired();
            entity.Property(supplier => supplier.ContactPerson).HasMaxLength(150);
            entity.Property(supplier => supplier.PhoneNumber).HasMaxLength(30);
            entity.Property(supplier => supplier.Email).HasMaxLength(254);
            entity.Property(supplier => supplier.Address).HasMaxLength(500);
            entity.Property(supplier => supplier.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Supply>(entity =>
        {
            entity.Property(supply => supply.Name).HasMaxLength(200).IsRequired();
            entity.Property(supply => supply.UnitOfMeasure).HasMaxLength(30).IsRequired();
            entity.Property(supply => supply.UnitCost).HasPrecision(18, 2);
            entity.Property(supply => supply.StockQuantity).HasPrecision(18, 3);
            entity.Property(supply => supply.IsActive).HasDefaultValue(true);
            entity.Property(supply => supply.Description).HasMaxLength(500);
            entity.HasIndex(supply => supply.SupplyCategoryId);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Supplies_UnitCost_NonNegative", "[UnitCost] >= 0");
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
            entity.Property(item => item.UnitCost).HasPrecision(18, 2);
            entity.Property(item => item.LotNumber).HasMaxLength(100);
            entity.HasIndex(item => new { item.DistributionId, item.SupplyId }).IsUnique();
            entity.HasIndex(item => item.SupplyId);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_DistributionItems_Quantity_Positive", "[Quantity] > 0");
                table.HasCheckConstraint("CK_DistributionItems_UnitCost_NonNegative", "[UnitCost] >= 0");
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
            entity.Property(record => record.QuantityNotYetUsed).HasPrecision(18, 3);
            entity.Property(record => record.QuantityDisposed).HasPrecision(18, 3);
            entity.Property(record => record.Remarks).HasMaxLength(2000);
            entity.HasIndex(record => record.DistributionItemId).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_UtilizationMonitoring_NonNegative",
                    "([QuantityUsed] IS NULL OR [QuantityUsed] >= 0) AND " +
                    "([QuantityStillStocked] IS NULL OR [QuantityStillStocked] >= 0) AND " +
                    "([QuantityNotYetUsed] IS NULL OR [QuantityNotYetUsed] >= 0) AND " +
                    "([QuantityDisposed] IS NULL OR [QuantityDisposed] >= 0)");
            });
            entity.HasOne(record => record.DistributionItem)
                .WithOne(item => item.UtilizationMonitoring)
                .HasForeignKey<UtilizationMonitoring>(record => record.DistributionItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlantingRecord>(entity =>
        {
            entity.Property(record => record.CropName).HasMaxLength(100).HasDefaultValue("Rice");
            entity.Property(record => record.Variety).HasMaxLength(100);
            entity.Property(record => record.Season).HasMaxLength(50);
            entity.Property(record => record.AreaPlantedHectares).HasPrecision(12, 4);
            entity.Property(record => record.Status).HasMaxLength(20).HasDefaultValue("Growing");
            entity.HasIndex(record => record.FarmId);
            entity.HasIndex(record => record.PlantingDate);
            entity.HasIndex(record => record.SeedDistributionItemId);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_PlantingRecords_Area_Positive", "[AreaPlantedHectares] > 0");
                table.HasCheckConstraint("CK_PlantingRecords_Status",
                    "[Status] IN (N'Growing', N'Harvested', N'Failed', N'Cancelled')");
                table.HasCheckConstraint("CK_PlantingRecords_ExpectedHarvest",
                    "[ExpectedHarvestDate] IS NULL OR [ExpectedHarvestDate] >= [PlantingDate]");
            });
            entity.HasOne(record => record.Farm).WithMany(farm => farm.PlantingRecords)
                .HasForeignKey(record => record.FarmId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(record => record.SeedDistributionItem).WithMany(item => item.PlantingRecords)
                .HasForeignKey(record => record.SeedDistributionItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CropMonitoringRecord>(entity =>
        {
            entity.Property(record => record.ObservedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(record => record.GrowthStage).HasMaxLength(100).IsRequired();
            entity.Property(record => record.CropHealth).HasMaxLength(100);
            entity.Property(record => record.PestOrDisease).HasMaxLength(500);
            entity.Property(record => record.RainfallMillimeters).HasPrecision(10, 2);
            entity.Property(record => record.Notes).HasMaxLength(2000);
            entity.HasIndex(record => new { record.PlantingRecordId, record.ObservedAtUtc });
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_CropMonitoringRecords_Rainfall_NonNegative",
                "[RainfallMillimeters] IS NULL OR [RainfallMillimeters] >= 0"));
            entity.HasOne(record => record.PlantingRecord).WithMany(planting => planting.MonitoringRecords)
                .HasForeignKey(record => record.PlantingRecordId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(record => record.RecordedByUser).WithMany(user => user.CropMonitoringRecords)
                .HasForeignKey(record => record.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupplyUsageRecord>(entity =>
        {
            entity.Property(record => record.QuantityUsed).HasPrecision(18, 3);
            entity.Property(record => record.Notes).HasMaxLength(2000);
            entity.HasIndex(record => new { record.PlantingRecordId, record.UsageDate });
            entity.HasIndex(record => record.SupplyId);
            entity.HasIndex(record => record.DistributionItemId);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_SupplyUsageRecords_Quantity_Positive", "[QuantityUsed] > 0"));
            entity.HasOne(record => record.PlantingRecord).WithMany(planting => planting.SupplyUsageRecords)
                .HasForeignKey(record => record.PlantingRecordId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(record => record.Supply).WithMany(supply => supply.UsageRecords)
                .HasForeignKey(record => record.SupplyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(record => record.DistributionItem).WithMany(item => item.UsageRecords)
                .HasForeignKey(record => record.DistributionItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(record => record.RecordedByUser).WithMany(user => user.SupplyUsageRecords)
                .HasForeignKey(record => record.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HarvestRecord>(entity =>
        {
            entity.Property(record => record.QuantityKilograms).HasPrecision(18, 3);
            entity.Property(record => record.MoisturePercent).HasPrecision(5, 2);
            entity.Property(record => record.Grade).HasMaxLength(50);
            entity.Property(record => record.Notes).HasMaxLength(2000);
            entity.HasIndex(record => new { record.PlantingRecordId, record.HarvestDate });
            entity.HasIndex(record => record.HarvestDate);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_HarvestRecords_Quantity_Positive", "[QuantityKilograms] > 0");
                table.HasCheckConstraint("CK_HarvestRecords_Moisture_Range",
                    "[MoisturePercent] IS NULL OR ([MoisturePercent] >= 0 AND [MoisturePercent] <= 100)");
            });
            entity.HasOne(record => record.PlantingRecord).WithMany(planting => planting.HarvestRecords)
                .HasForeignKey(record => record.PlantingRecordId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HarvestVerificationRecord>(entity =>
        {
            entity.Property(record => record.VerifiedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(record => record.VerifiedQuantityKilograms).HasPrecision(18, 3);
            entity.Property(record => record.Status).HasMaxLength(20).HasDefaultValue("Pending");
            entity.Property(record => record.Remarks).HasMaxLength(2000);
            entity.HasIndex(record => record.HarvestRecordId);
            entity.HasIndex(record => record.VerifiedByUserId);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_HarvestVerificationRecords_Status",
                    "[Status] IN (N'Pending', N'Verified', N'Rejected')");
                table.HasCheckConstraint("CK_HarvestVerificationRecords_Quantity_NonNegative",
                    "[VerifiedQuantityKilograms] IS NULL OR [VerifiedQuantityKilograms] >= 0");
            });
            entity.HasOne(record => record.HarvestRecord).WithMany(harvest => harvest.Verifications)
                .HasForeignKey(record => record.HarvestRecordId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(record => record.VerifiedByUser).WithMany(user => user.HarvestVerifications)
                .HasForeignKey(record => record.VerifiedByUserId).OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<SupportingDocument>(entity =>
        {
            entity.Property(document => document.FileName).HasMaxLength(255).IsRequired();
            entity.Property(document => document.StoragePath).HasMaxLength(1000).IsRequired();
            entity.Property(document => document.ContentType).HasMaxLength(150).IsRequired();
            entity.Property(document => document.Sha256).HasMaxLength(64);
            entity.Property(document => document.UploadedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(document => document.FarmerId);
            entity.HasIndex(document => document.FarmId);
            entity.HasIndex(document => document.DistributionId);
            entity.HasIndex(document => document.PlantingRecordId);
            entity.HasIndex(document => document.HarvestRecordId);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_SupportingDocuments_FileSize_NonNegative", "[FileSizeBytes] >= 0");
                table.HasCheckConstraint(
                    "CK_SupportingDocuments_OneParent",
                    "((CASE WHEN [FarmerId] IS NULL THEN 0 ELSE 1 END) + " +
                    "(CASE WHEN [FarmId] IS NULL THEN 0 ELSE 1 END) + " +
                    "(CASE WHEN [DistributionId] IS NULL THEN 0 ELSE 1 END) + " +
                    "(CASE WHEN [PlantingRecordId] IS NULL THEN 0 ELSE 1 END) + " +
                    "(CASE WHEN [HarvestRecordId] IS NULL THEN 0 ELSE 1 END)) = 1");
            });
            entity.HasOne(document => document.Farmer).WithMany(farmer => farmer.SupportingDocuments)
                .HasForeignKey(document => document.FarmerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(document => document.Farm).WithMany(farm => farm.SupportingDocuments)
                .HasForeignKey(document => document.FarmId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(document => document.Distribution).WithMany(distribution => distribution.SupportingDocuments)
                .HasForeignKey(document => document.DistributionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(document => document.PlantingRecord).WithMany(planting => planting.SupportingDocuments)
                .HasForeignKey(document => document.PlantingRecordId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(document => document.HarvestRecord).WithMany(harvest => harvest.SupportingDocuments)
                .HasForeignKey(document => document.HarvestRecordId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(document => document.UploadedByUser).WithMany(user => user.SupportingDocuments)
                .HasForeignKey(document => document.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
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
    }
}