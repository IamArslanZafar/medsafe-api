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

        modelBuilder.Entity<IncidentReport>()
            .HasOne<Route>()
            .WithMany()
            .HasForeignKey(i => i.RouteId)
            .HasConstraintName("FK_IncidentReports_Route")
            .OnDelete(DeleteBehavior.NoAction);

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
            .HasOne<Formulation>()
            .WithMany()
            .HasForeignKey(i => i.FormulationId)
            .HasConstraintName("FK_IncidentReports_Formulation")
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        modelBuilder.Entity<IncidentReport>()
            .HasOne<Frequency>()
            .WithMany()
            .HasForeignKey(i => i.FrequencyId)
            .HasConstraintName("FK_IncidentReports_Frequency")
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
            .HasOne<DoseUnit>()
            .WithMany()
            .HasForeignKey(i => i.DoseUnitId)
            .HasConstraintName("FK_IncidentReports_DoseUnit")
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
            .HasOne<IncidentReport>()
            .WithMany(r => r.Notifications)
            .HasForeignKey(n => n.IncidentReportId)
            .HasConstraintName("FK_IncidentNotifications_IncidentReport");

        modelBuilder.Entity<IncidentNotification>()
            .HasOne<NotificationRecipientType>()
            .WithMany()
            .HasForeignKey(n => n.NotificationTypeId)
            .HasConstraintName("FK_IncidentNotifications_NotificationRecipientType")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentNotification>()
            .HasOne<NotificationMethod>()
            .WithMany()
            .HasForeignKey(n => n.NotificationMethodId)
            .HasConstraintName("FK_IncidentNotifications_NotificationMethod")
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
            entity.Property(e => e.ReportingFacilityUnit).HasMaxLength(150);
            entity.Property(e => e.ReportStatus).HasMaxLength(30);
            entity.Property(e => e.PatientReferenceToken).HasMaxLength(35).IsUnicode(false);
            entity.Property(e => e.PatientSex).HasMaxLength(20);
            entity.Property(e => e.PatientWeightKg).HasPrecision(6, 2);
            entity.Property(e => e.MedicationName).HasMaxLength(250);
            entity.Property(e => e.GenericActiveIngredient).HasMaxLength(250);
            entity.Property(e => e.DoseValue).HasPrecision(18, 4);
            entity.Property(e => e.BatchLotNumber).HasMaxLength(100);
            entity.Property(e => e.ReportType).HasMaxLength(30);
            entity.Property(e => e.SuspectedCausality).HasMaxLength(100);
            entity.Property(e => e.HarmLevelCode).HasMaxLength(1).IsUnicode(false).IsFixedLength();
            entity.Property(e => e.IncidentLocation).HasMaxLength(200);
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
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(1000);
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
    }
}
