using Microsoft.EntityFrameworkCore;
using MedSafe.Models;

namespace MedSafe.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<IncidentReport> IncidentReports => Set<IncidentReport>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<DropdownDefinition> DropdownDefinitions => Set<DropdownDefinition>();
    public DbSet<DropdownValue> DropdownValues => Set<DropdownValue>();
    public DbSet<ContributingFactor> ContributingFactors => Set<ContributingFactor>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<PatientOutcome> PatientOutcomes => Set<PatientOutcome>();
    public DbSet<StageOfProcess> StageOfProcesses => Set<StageOfProcess>();
    public DbSet<SeriousnessCriterion> SeriousnessCriteria => Set<SeriousnessCriterion>();
    public DbSet<Formulation> Formulations => Set<Formulation>();
    public DbSet<Frequency> Frequencies => Set<Frequency>();
    public DbSet<ErrorCategory> ErrorCategories => Set<ErrorCategory>();
    public DbSet<DoseUnit> DoseUnits => Set<DoseUnit>();
    public DbSet<Profession> Professions => Set<Profession>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<IncidentReportMedication> IncidentReportMedications => Set<IncidentReportMedication>();
    public DbSet<IncidentReportContributingFactor> IncidentReportContributingFactors => Set<IncidentReportContributingFactor>();
    public DbSet<IncidentReportSeriousnessCriterion> IncidentReportSeriousnessCriteria => Set<IncidentReportSeriousnessCriterion>();
    public DbSet<IncidentReportAttachment> IncidentReportAttachments => Set<IncidentReportAttachment>();
    public DbSet<IncidentNotification> IncidentNotifications => Set<IncidentNotification>();
    public DbSet<NotificationRecipientType> NotificationRecipientTypes => Set<NotificationRecipientType>();
    public DbSet<NotificationMethod> NotificationMethods => Set<NotificationMethod>();
    public DbSet<NotificationStatus> NotificationStatuses => Set<NotificationStatus>();
    public DbSet<NotificationUrgency> NotificationUrgencies => Set<NotificationUrgency>();
    public DbSet<Allergy> Allergies => Set<Allergy>();
    public DbSet<CurrentMedication> CurrentMedications => Set<CurrentMedication>();
    public DbSet<IncidentReportAllergy> IncidentReportAllergies => Set<IncidentReportAllergy>();
    public DbSet<IncidentReportCurrentMedication> IncidentReportCurrentMedications => Set<IncidentReportCurrentMedication>();
    public DbSet<IncidentReportReview> IncidentReportReviews => Set<IncidentReportReview>();
    public DbSet<IncidentReportStatusHistory> IncidentReportStatusHistories => Set<IncidentReportStatusHistory>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<SystemModule> SystemModules => Set<SystemModule>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<User>()
            .HasOne<Profession>()
            .WithMany()
            .HasForeignKey(u => u.ProfessionId)
            .HasConstraintName("FK_Users_Profession")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Position>()
            .HasOne<Profession>()
            .WithMany()
            .HasForeignKey(p => p.ProfessionId)
            .HasConstraintName("FK_Position_Profession")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Position>()
            .HasIndex(p => new { p.ProfessionId, p.Name })
            .IsUnique()
            .HasDatabaseName("UQ_Position_Profession_Name");

        // These tables already exist live (built outside this project's migrations) —
        // map to them for querying but never let `dotnet ef migrations add` touch them.
        modelBuilder.Entity<ContributingFactor>()
            .ToTable("ContributingFactor", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<Route>()
            .ToTable("Route", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<PatientOutcome>()
            .ToTable("PatientOutcome", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<StageOfProcess>()
            .ToTable("StageOfProcess", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<SeriousnessCriterion>()
            .ToTable("SeriousnessCriterion", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<Formulation>()
            .ToTable("Formulation", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<Frequency>()
            .ToTable("Frequency", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<ErrorCategory>()
            .ToTable("ErrorCategory", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<DoseUnit>()
            .ToTable("DoseUnit", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<Profession>()
            .ToTable("Profession", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<Position>()
            .ToTable("Position", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<Allergy>()
            .ToTable("Allergy", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<CurrentMedication>()
            .ToTable("CurrentMedication", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<IncidentReportAllergy>()
            .ToTable("IncidentReportAllergy", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<IncidentReportCurrentMedication>()
            .ToTable("IncidentReportCurrentMedication", tb => tb.ExcludeFromMigrations());

        // IncidentReports and its child tables were rebuilt directly on the live DB
        // (outside this project's migrations) to the clean schema below — map to them
        // for querying but never let `dotnet ef migrations add` touch their structure.
        modelBuilder.Entity<IncidentReport>()
            .ToTable("IncidentReports", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<IncidentReportMedication>()
            .ToTable("IncidentReportMedication", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<IncidentReportContributingFactor>()
            .ToTable("IncidentReportContributingFactor", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<IncidentReportSeriousnessCriterion>()
            .ToTable("IncidentReportSeriousnessCriterion", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<IncidentReportAttachment>()
            .ToTable("IncidentReportAttachment", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<IncidentNotification>()
            .ToTable("IncidentNotifications", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<NotificationRecipientType>()
            .ToTable("NotificationRecipientType", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<NotificationMethod>()
            .ToTable("NotificationMethod", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<NotificationStatus>()
            .ToTable("NotificationStatus", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<NotificationUrgency>()
            .ToTable("NotificationUrgency", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<IncidentReportReview>()
            .ToTable("IncidentReportReviews", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<IncidentReportStatusHistory>()
            .ToTable("IncidentReportStatusHistory", tb => tb.ExcludeFromMigrations());

        modelBuilder.Entity<IncidentReport>()
            .HasOne<PatientOutcome>()
            .WithMany()
            .HasForeignKey(i => i.PatientOutcomeId)
            .HasConstraintName("FK_IncidentReports_PatientOutcome")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReport>()
            .HasOne<StageOfProcess>()
            .WithMany()
            .HasForeignKey(i => i.StageOfProcessId)
            .HasConstraintName("FK_IncidentReports_StageOfProcess")
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        modelBuilder.Entity<IncidentReport>()
            .HasOne<ErrorCategory>()
            .WithMany()
            .HasForeignKey(i => i.ErrorCategoryId)
            .HasConstraintName("FK_IncidentReports_ErrorCategory")
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        modelBuilder.Entity<IncidentReport>()
            .HasOne<Profession>()
            .WithMany()
            .HasForeignKey(i => i.ProfessionId)
            .HasConstraintName("FK_IncidentReports_Profession")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Position>()
            .HasAlternateKey(p => new { p.ProfessionId, p.Id });

        modelBuilder.Entity<IncidentReport>()
            .HasOne<Position>()
            .WithMany()
            .HasForeignKey(i => new { i.ProfessionId, i.PositionId })
            .HasPrincipalKey(p => new { p.ProfessionId, p.Id })
            .HasConstraintName("FK_IncidentReports_ProfessionPosition")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReportMedication>()
            .HasOne(m => m.IncidentReport)
            .WithMany(r => r.Medications)
            .HasForeignKey(m => m.IncidentReportId)
            .HasConstraintName("FK_IncidentReportMedication_IncidentReport");

        modelBuilder.Entity<IncidentReportMedication>()
            .HasOne<DoseUnit>()
            .WithMany()
            .HasForeignKey(m => m.DoseUnitId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReportMedication>()
            .HasOne<Route>()
            .WithMany()
            .HasForeignKey(m => m.RouteId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReportMedication>()
            .HasOne<Frequency>()
            .WithMany()
            .HasForeignKey(m => m.FrequencyId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReportMedication>()
            .HasOne<Formulation>()
            .WithMany()
            .HasForeignKey(m => m.FormulationId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReportContributingFactor>()
            .HasOne<ContributingFactor>()
            .WithMany()
            .HasForeignKey(f => f.ContributingFactorId)
            .HasConstraintName("FK_IRCF_ContributingFactor")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReportContributingFactor>()
            .HasOne<IncidentReport>()
            .WithMany(r => r.ContributingFactors)
            .HasForeignKey(f => f.IncidentReportId)
            .HasConstraintName("FK_IRCF_IncidentReport");

        modelBuilder.Entity<IncidentReportContributingFactor>()
            .HasIndex(f => new { f.IncidentReportId, f.ContributingFactorId }).IsUnique();

        modelBuilder.Entity<IncidentReportSeriousnessCriterion>()
            .HasOne<SeriousnessCriterion>()
            .WithMany()
            .HasForeignKey(c => c.SeriousnessCriterionId)
            .HasConstraintName("FK_IRSC_SeriousnessCriterion")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReportSeriousnessCriterion>()
            .HasOne<IncidentReport>()
            .WithMany(r => r.SeriousnessCriteria)
            .HasForeignKey(c => c.IncidentReportId)
            .HasConstraintName("FK_IRSC_IncidentReport");

        modelBuilder.Entity<IncidentReportSeriousnessCriterion>()
            .HasIndex(c => new { c.IncidentReportId, c.SeriousnessCriterionId }).IsUnique();

        modelBuilder.Entity<IncidentReportAttachment>()
            .HasOne<IncidentReport>()
            .WithMany(r => r.Attachments)
            .HasForeignKey(a => a.IncidentReportId)
            .HasConstraintName("FK_IncidentReportAttachment_Incident");

        modelBuilder.Entity<IncidentReportAllergy>()
            .HasOne<Allergy>()
            .WithMany()
            .HasForeignKey(a => a.AllergyId)
            .HasConstraintName("FK_IRAllergy_Allergy")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReportAllergy>()
            .HasOne<IncidentReport>()
            .WithMany(r => r.AllergyLinks)
            .HasForeignKey(a => a.IncidentReportId)
            .HasConstraintName("FK_IRAllergy_IncidentReport");

        modelBuilder.Entity<IncidentReportAllergy>()
            .HasIndex(a => new { a.IncidentReportId, a.AllergyId }).IsUnique();

        modelBuilder.Entity<IncidentReportCurrentMedication>()
            .HasOne<CurrentMedication>()
            .WithMany()
            .HasForeignKey(m => m.CurrentMedicationId)
            .HasConstraintName("FK_IRCurrentMedication_CurrentMedication")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReportCurrentMedication>()
            .HasOne<IncidentReport>()
            .WithMany(r => r.CurrentMedicationLinks)
            .HasForeignKey(m => m.IncidentReportId)
            .HasConstraintName("FK_IRCurrentMedication_IncidentReport");

        modelBuilder.Entity<IncidentReportCurrentMedication>()
            .HasIndex(m => new { m.IncidentReportId, m.CurrentMedicationId }).IsUnique();

        modelBuilder.Entity<IncidentNotification>()
            .HasOne(n => n.IncidentReport)
            .WithMany(r => r.Notifications)
            .HasForeignKey(n => n.IncidentReportId)
            .HasConstraintName("FK_IncidentNotifications_IncidentReport");

        modelBuilder.Entity<IncidentNotification>()
            .HasOne<NotificationRecipientType>()
            .WithMany()
            .HasForeignKey(n => n.NotificationTypeId)
            .HasConstraintName("FK_IncidentNotifications_NotificationRecipientType")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReportReview>()
            .HasOne(r => r.IncidentReport)
            .WithOne(i => i.Review)
            .HasForeignKey<IncidentReportReview>(r => r.IncidentReportId)
            .HasConstraintName("FK_IncidentReportReviews_IncidentReport")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReportReview>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.ReviewerUserId)
            .HasConstraintName("FK_IncidentReportReviews_Reviewer")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReportReview>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.ActionOwnerUserId)
            .HasConstraintName("FK_IncidentReportReviews_ActionOwner")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReportStatusHistory>()
            .HasOne(h => h.IncidentReport)
            .WithMany(i => i.StatusHistory)
            .HasForeignKey(h => h.IncidentReportId)
            .HasConstraintName("FK_IncidentReportStatusHistory_IncidentReport")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReportStatusHistory>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .HasConstraintName("FK_IncidentReportStatusHistory_ChangedByUser")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<DropdownDefinition>()
            .HasIndex(d => d.Key).IsUnique();

        modelBuilder.Entity<DropdownValue>()
            .HasOne(v => v.DropdownDefinition)
            .WithMany(d => d.Values)
            .HasForeignKey(v => v.DropdownDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DropdownValue>()
            .HasIndex(v => new { v.DropdownDefinitionId, v.Value }).IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasOne(r => r.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(r => r.UserId);

        modelBuilder.Entity<AuditLog>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .IsRequired(false);

        modelBuilder.Entity<IncidentReport>(entity =>
        {
            entity.HasIndex(e => e.IncidentReportNumber).IsUnique();
            entity.Property(e => e.IncidentReportNumber).HasMaxLength(30);
            entity.Property(e => e.SubmittedByRole).HasMaxLength(50);
            entity.Property(e => e.ReportStatus).HasMaxLength(30);
            entity.Property(e => e.PatientReferenceToken).HasMaxLength(35).IsUnicode(false);
            entity.Property(e => e.PatientReference).HasMaxLength(100);
            entity.Property(e => e.PatientName).HasMaxLength(200);
            entity.Property(e => e.PatientSex).HasMaxLength(20);
            entity.Property(e => e.PatientWeightKg).HasPrecision(6, 2);
            entity.Property(e => e.ReportType).HasMaxLength(30);
            entity.Property(e => e.SuspectedCausality).HasMaxLength(100);
            entity.Property(e => e.HarmLevelCode).HasMaxLength(1).IsUnicode(false).IsFixedLength();
            entity.Property(e => e.IncidentLocation).HasMaxLength(200);
        });

        modelBuilder.Entity<IncidentReportMedication>(entity =>
        {
            entity.Property(e => e.MedicationName).HasMaxLength(250);
            entity.Property(e => e.DoseValue).HasPrecision(18, 4);
        });

        modelBuilder.Entity<IncidentReportReview>(entity =>
        {
            entity.Property(e => e.ResolutionStatus).HasMaxLength(30);
        });

        modelBuilder.Entity<IncidentReportStatusHistory>(entity =>
        {
            entity.Property(e => e.FromStatus).HasMaxLength(30);
            entity.Property(e => e.ToStatus).HasMaxLength(30);
            entity.Property(e => e.Reason).HasMaxLength(250);
        });

        modelBuilder.Entity<IncidentReportAttachment>(entity =>
        {
            entity.Property(e => e.OriginalFileName).HasMaxLength(255);
            entity.Property(e => e.StorageKey).HasMaxLength(500);
            entity.Property(e => e.ContentType).HasMaxLength(150);
            entity.Property(e => e.Sha256Hash).HasMaxLength(64).IsUnicode(false).IsFixedLength();
        });

        modelBuilder.Entity<IncidentNotification>(entity =>
        {
            entity.Property(e => e.PersonName).HasMaxLength(200);
        });

        modelBuilder.Entity<NotificationRecipientType>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<NotificationMethod>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<NotificationStatus>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<NotificationUrgency>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Allergy>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<CurrentMedication>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // ── Role / Permission ────────────────────────────────────────────
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<SystemModule>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.PermissionTag).HasMaxLength(150);
            entity.HasIndex(e => e.PermissionTag).IsUnique();

            entity.HasOne<SystemModule>()
                .WithMany(m => m.Permissions)
                .HasForeignKey(p => p.SystemModuleId)
                .HasConstraintName("FK_Permissions_SystemModule")
                .OnDelete(DeleteBehavior.NoAction);

            // Self-referencing tree — a permission's parent within the same module.
            entity.HasOne<Permission>()
                .WithMany()
                .HasForeignKey(p => p.ParentId)
                .HasConstraintName("FK_Permissions_Parent")
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            entity.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .HasConstraintName("FK_RolePermissions_Role")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .HasConstraintName("FK_RolePermissions_Permission")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>()
            .HasOne<Role>()
            .WithMany()
            .HasForeignKey(u => u.RoleId)
            .HasConstraintName("FK_Users_Role")
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        SeedRolePermissionData(modelBuilder);
    }

    // Starting catalog matching MedSafe's current feature set and the existing
    // frontend ROLE_PERMISSIONS map — editable afterwards through the Roles UI,
    // this just gives every environment (dev/live) the same non-empty baseline
    // instead of starting with zero roles/permissions defined.
    private static void SeedRolePermissionData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Nurse", Description = "Frontline clinical staff who submit incident reports", CreatedAt = new DateTime(2026, 1, 1) },
            new Role { Id = 2, Name = "Physician", Description = "Clinical reviewer who signs off on incident reports", CreatedAt = new DateTime(2026, 1, 1) },
            new Role { Id = 3, Name = "Admin", Description = "Full administrative access", CreatedAt = new DateTime(2026, 1, 1) },
            new Role { Id = 4, Name = "Pharmacist", Description = "Pharmacy staff", CreatedAt = new DateTime(2026, 1, 1) }
        );

        modelBuilder.Entity<SystemModule>().HasData(
            new SystemModule { Id = 1, Name = "Incident Reports", Description = "Submitting and viewing incident reports", DisplayOrder = 1 },
            new SystemModule { Id = 2, Name = "Clinical Review", Description = "Reviewing and signing off on reports", DisplayOrder = 2 },
            new SystemModule { Id = 3, Name = "Alert Rules", Description = "Configuring notification/alert rules", DisplayOrder = 3 },
            new SystemModule { Id = 4, Name = "Configurations", Description = "Managing dropdown/lookup configuration data", DisplayOrder = 4 },
            new SystemModule { Id = 5, Name = "User Management", Description = "Managing user accounts and roles", DisplayOrder = 5 },
            new SystemModule { Id = 6, Name = "Feedback", Description = "Submitting and reviewing app feedback", DisplayOrder = 6 },
            new SystemModule { Id = 7, Name = "Audit Log", Description = "Viewing the HIPAA audit trail", DisplayOrder = 7 },
            new SystemModule { Id = 8, Name = "Dashboard", Description = "Viewing analytics dashboard", DisplayOrder = 8 },
            new SystemModule { Id = 9, Name = "Training & Support", Description = "Viewing training/reference and support resources", DisplayOrder = 9 }
        );

        modelBuilder.Entity<Permission>().HasData(
            new Permission { Id = 1, Name = "Incident Reports", PermissionTag = "incident_reports", ParentId = null, SystemModuleId = 1 },
            new Permission { Id = 2, Name = "Submit Report", PermissionTag = "incident_reports.submit", ParentId = 1, SystemModuleId = 1 },
            new Permission { Id = 3, Name = "View All Reports", PermissionTag = "incident_reports.view_all", ParentId = 1, SystemModuleId = 1 },
            new Permission { Id = 4, Name = "Export Reports", PermissionTag = "incident_reports.export", ParentId = 1, SystemModuleId = 1 },

            new Permission { Id = 5, Name = "Clinical Review", PermissionTag = "clinical_review", ParentId = null, SystemModuleId = 2 },
            new Permission { Id = 6, Name = "Start Review", PermissionTag = "clinical_review.start", ParentId = 5, SystemModuleId = 2 },
            new Permission { Id = 7, Name = "Sign Off Review", PermissionTag = "clinical_review.sign_off", ParentId = 5, SystemModuleId = 2 },

            new Permission { Id = 8, Name = "Alert Rules", PermissionTag = "alert_rules", ParentId = null, SystemModuleId = 3 },
            new Permission { Id = 9, Name = "View Alert Rules", PermissionTag = "alert_rules.view", ParentId = 8, SystemModuleId = 3 },
            new Permission { Id = 10, Name = "Manage Alert Rules", PermissionTag = "alert_rules.manage", ParentId = 8, SystemModuleId = 3 },

            new Permission { Id = 11, Name = "Configurations", PermissionTag = "configurations", ParentId = null, SystemModuleId = 4 },
            new Permission { Id = 12, Name = "Manage Configurations", PermissionTag = "configurations.manage", ParentId = 11, SystemModuleId = 4 },

            new Permission { Id = 13, Name = "User Management", PermissionTag = "user_management", ParentId = null, SystemModuleId = 5 },
            new Permission { Id = 14, Name = "Manage Users", PermissionTag = "user_management.manage", ParentId = 13, SystemModuleId = 5 },

            new Permission { Id = 15, Name = "Feedback", PermissionTag = "feedback", ParentId = null, SystemModuleId = 6 },
            new Permission { Id = 16, Name = "Submit Feedback", PermissionTag = "feedback.submit", ParentId = 15, SystemModuleId = 6 },
            new Permission { Id = 17, Name = "Review Feedback", PermissionTag = "feedback.review", ParentId = 15, SystemModuleId = 6 },

            new Permission { Id = 18, Name = "Audit Log", PermissionTag = "audit_log", ParentId = null, SystemModuleId = 7 },
            new Permission { Id = 19, Name = "View Audit Log", PermissionTag = "audit_log.view", ParentId = 18, SystemModuleId = 7 },

            new Permission { Id = 20, Name = "Dashboard", PermissionTag = "dashboard", ParentId = null, SystemModuleId = 8 },
            new Permission { Id = 21, Name = "View Dashboard", PermissionTag = "dashboard.view", ParentId = 20, SystemModuleId = 8 },

            new Permission { Id = 22, Name = "Training & Support", PermissionTag = "training", ParentId = null, SystemModuleId = 9 },
            new Permission { Id = 23, Name = "View Training & Support", PermissionTag = "training.view", ParentId = 22, SystemModuleId = 9 }
        );

        // Admin = every permission. Physician = clinical review + report basics.
        // Nurse = submit report + feedback + dashboard. Pharmacist = baseline only.
        // Training & Support (22/23) defaults to every role — it's a help/reference
        // resource, not a restricted feature. Matches the current frontend
        // ROLE_PERMISSIONS map plus sensible baseline access.
        var adminAll = Enumerable.Range(1, 23).Select(pid => new RolePermission { RoleId = 3, PermissionId = pid });
        var physician = new[] { 1, 3, 5, 6, 7, 20, 21, 22, 23 }.Select(pid => new RolePermission { RoleId = 2, PermissionId = pid });
        var nurse = new[] { 1, 2, 15, 16, 20, 21, 22, 23 }.Select(pid => new RolePermission { RoleId = 1, PermissionId = pid });
        var pharmacist = new[] { 1, 2, 20, 21, 22, 23 }.Select(pid => new RolePermission { RoleId = 4, PermissionId = pid });

        modelBuilder.Entity<RolePermission>().HasData(
            adminAll.Concat(physician).Concat(nurse).Concat(pharmacist).ToArray()
        );
    }
}
