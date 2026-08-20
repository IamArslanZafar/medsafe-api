using Microsoft.EntityFrameworkCore;
using MedSafe.Models;

namespace MedSafe.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserAvailability> UserAvailabilities => Set<UserAvailability>();
    public DbSet<IncidentReport> IncidentReports => Set<IncidentReport>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<AlertRuleMatchMode> AlertRuleMatchModes => Set<AlertRuleMatchMode>();
    public DbSet<AlertConditionOperator> AlertConditionOperators => Set<AlertConditionOperator>();
    public DbSet<AlertConditionField> AlertConditionFields => Set<AlertConditionField>();
    public DbSet<AlertConditionFieldOperator> AlertConditionFieldOperators => Set<AlertConditionFieldOperator>();
    public DbSet<AlertRuleCondition> AlertRuleConditions => Set<AlertRuleCondition>();
    public DbSet<AlertRuleConditionValue> AlertRuleConditionValues => Set<AlertRuleConditionValue>();
    public DbSet<AlertRuleRecipient> AlertRuleRecipients => Set<AlertRuleRecipient>();
    public DbSet<AlertTriggerHistory> AlertTriggerHistories => Set<AlertTriggerHistory>();
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

    // Medication Error / ADR lookups
    public DbSet<ReportType> ReportTypes => Set<ReportType>();
    public DbSet<HarmLevel> HarmLevels => Set<HarmLevel>();
    public DbSet<SuspectedCausality> SuspectedCausalities => Set<SuspectedCausality>();
    public DbSet<AdrSeverity> AdrSeverities => Set<AdrSeverity>();
    public DbSet<VisitType> VisitTypes => Set<VisitType>();
    public DbSet<ReportingSource> ReportingSources => Set<ReportingSource>();
    public DbSet<UnitDepartment> UnitDepartments => Set<UnitDepartment>();
    public DbSet<IncidentReportConcomitantMedication> IncidentReportConcomitantMedications => Set<IncidentReportConcomitantMedication>();
    public DbSet<IncidentReportHealthcareProfessional> IncidentReportHealthcareProfessionals => Set<IncidentReportHealthcareProfessional>();
    public DbSet<ReportedIncidentSeverity> ReportedIncidentSeverities => Set<ReportedIncidentSeverity>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<IncidentReportWitness> IncidentReportWitnesses => Set<IncidentReportWitness>();
    public DbSet<IncidentReportOtherDepartment> IncidentReportOtherDepartments => Set<IncidentReportOtherDepartment>();
    public DbSet<IncidentReportReporter> IncidentReportReporters => Set<IncidentReportReporter>();
    public DbSet<IncidentReportManualNotification> IncidentReportManualNotifications => Set<IncidentReportManualNotification>();

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

        // Composite FK mirrors the same pattern used below for IncidentReport —
        // ties PositionId to the Position that actually belongs to the user's
        // ProfessionId, instead of allowing a mismatched profession/position pair.
        modelBuilder.Entity<User>()
            .HasOne<Position>()
            .WithMany()
            .HasForeignKey(u => new { u.ProfessionId, u.PositionId })
            .HasPrincipalKey(p => new { p.ProfessionId, p.Id })
            .HasConstraintName("FK_Users_ProfessionPosition")
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

        // Medication Error / ADR lookup + child tables — added directly on the live
        // DB via SQL script, same ExcludeFromMigrations convention as everything above.
        modelBuilder.Entity<ReportType>()
            .ToTable("ReportType", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<HarmLevel>()
            .ToTable("HarmLevel", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<SuspectedCausality>()
            .ToTable("SuspectedCausality", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<AdrSeverity>()
            .ToTable("AdrSeverity", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<VisitType>()
            .ToTable("VisitType", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<ReportingSource>()
            .ToTable("ReportingSource", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<UnitDepartment>()
            .ToTable("UnitDepartment", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<IncidentReportConcomitantMedication>()
            .ToTable("IncidentReportConcomitantMedication", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<IncidentReportHealthcareProfessional>()
            .ToTable("IncidentReportHealthcareProfessional", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<ReportedIncidentSeverity>()
            .ToTable("ReportedIncidentSeverity", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<Section>()
            .ToTable("Section", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<IncidentReportWitness>()
            .ToTable("IncidentReportWitness", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<IncidentReportOtherDepartment>()
            .ToTable("IncidentReportOtherDepartment", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<IncidentReportReporter>()
            .ToTable("IncidentReportReporter", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<IncidentReportManualNotification>()
            .ToTable("IncidentReportManualNotification", tb => tb.ExcludeFromMigrations());

        // Alert rule builder tables (and AlertRule's new columns) were added directly
        // on the live DB via SQL script, same as the IncidentReports rebuild above —
        // map to them for querying but never let `dotnet ef migrations add` touch them.
        modelBuilder.Entity<AlertRule>()
            .ToTable("AlertRules", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<AlertRuleMatchMode>()
            .ToTable("AlertRuleMatchMode", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<AlertConditionOperator>()
            .ToTable("AlertConditionOperator", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<AlertConditionField>()
            .ToTable("AlertConditionField", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<AlertConditionFieldOperator>()
            .ToTable("AlertConditionFieldOperator", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<AlertRuleCondition>()
            .ToTable("AlertRuleCondition", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<AlertRuleConditionValue>()
            .ToTable("AlertRuleConditionValue", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<AlertRuleRecipient>()
            .ToTable("AlertRuleRecipient", tb => tb.ExcludeFromMigrations());
        modelBuilder.Entity<AlertTriggerHistory>()
            .ToTable("AlertTriggerHistory", tb => tb.ExcludeFromMigrations());

        modelBuilder.Entity<AlertConditionFieldOperator>()
            .HasKey(x => new { x.ConditionFieldId, x.OperatorId });

        modelBuilder.Entity<AlertConditionFieldOperator>()
            .HasOne(x => x.ConditionField)
            .WithMany(x => x.FieldOperators)
            .HasForeignKey(x => x.ConditionFieldId);

        modelBuilder.Entity<AlertConditionFieldOperator>()
            .HasOne(x => x.Operator)
            .WithMany(x => x.FieldOperators)
            .HasForeignKey(x => x.OperatorId);

        modelBuilder.Entity<AlertRule>()
            .HasOne(x => x.MatchMode)
            .WithMany(x => x.AlertRules)
            .HasForeignKey(x => x.MatchModeId);

        modelBuilder.Entity<AlertRule>()
            .HasOne(x => x.NotificationUrgency)
            .WithMany()
            .HasForeignKey(x => x.UrgencyId);

        modelBuilder.Entity<AlertRule>()
            .HasMany(x => x.Conditions)
            .WithOne(x => x.AlertRule)
            .HasForeignKey(x => x.AlertRuleId);

        modelBuilder.Entity<AlertRule>()
            .HasMany(x => x.Recipients)
            .WithOne(x => x.AlertRule)
            .HasForeignKey(x => x.AlertRuleId);

        modelBuilder.Entity<AlertRuleCondition>()
            .HasOne(x => x.ConditionField)
            .WithMany()
            .HasForeignKey(x => x.ConditionFieldId);

        modelBuilder.Entity<AlertRuleCondition>()
            .HasOne(x => x.Operator)
            .WithMany()
            .HasForeignKey(x => x.OperatorId);

        modelBuilder.Entity<AlertRuleCondition>()
            .HasMany(x => x.Values)
            .WithOne(x => x.AlertRuleCondition)
            .HasForeignKey(x => x.AlertRuleConditionId);

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

        modelBuilder.Entity<IncidentReport>()
            .HasOne<ReportType>()
            .WithMany()
            .HasForeignKey(i => i.ReportTypeId)
            .HasConstraintName("FK_IncidentReports_ReportType")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReport>()
            .HasOne<HarmLevel>()
            .WithMany()
            .HasForeignKey(i => i.HarmLevelId)
            .HasConstraintName("FK_IncidentReports_HarmLevel")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReport>()
            .HasOne<SuspectedCausality>()
            .WithMany()
            .HasForeignKey(i => i.SuspectedCausalityId)
            .HasConstraintName("FK_IncidentReports_SuspectedCausality")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReport>()
            .HasOne<AdrSeverity>()
            .WithMany()
            .HasForeignKey(i => i.AdrSeverityId)
            .HasConstraintName("FK_IncidentReports_AdrSeverity")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReport>()
            .HasOne<UnitDepartment>()
            .WithMany()
            .HasForeignKey(i => i.IncidentUnitId)
            .HasConstraintName("FK_IncidentReports_IncidentUnit")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReport>()
            .HasOne<VisitType>()
            .WithMany()
            .HasForeignKey(i => i.VisitTypeId)
            .HasConstraintName("FK_IncidentReports_VisitType")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReport>()
            .HasOne<ReportingSource>()
            .WithMany()
            .HasForeignKey(i => i.ReportingSourceId)
            .HasConstraintName("FK_IncidentReports_ReportingSource")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReport>()
            .HasOne<ReportedIncidentSeverity>()
            .WithMany()
            .HasForeignKey(i => i.ReportedIncidentSeverityId)
            .HasConstraintName("FK_IncidentReports_ReportedIncidentSeverity")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReport>()
            .HasOne<Section>()
            .WithMany()
            .HasForeignKey(i => i.SectionId)
            .HasConstraintName("FK_IncidentReports_Section")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ErrorCategory>()
            .HasOne<StageOfProcess>()
            .WithMany()
            .HasForeignKey(e => e.StageOfProcessId)
            .HasConstraintName("FK_ErrorCategory_StageOfProcess")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Section>()
            .HasOne<UnitDepartment>()
            .WithMany()
            .HasForeignKey(s => s.UnitDepartmentId)
            .HasConstraintName("FK_Section_UnitDepartment")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReportConcomitantMedication>()
            .HasOne(m => m.IncidentReport)
            .WithMany(r => r.ConcomitantMedications)
            .HasForeignKey(m => m.IncidentReportId)
            .HasConstraintName("FK_IRConcomitantMedication_IncidentReport");

        modelBuilder.Entity<IncidentReportHealthcareProfessional>()
            .HasOne(p => p.IncidentReport)
            .WithMany(r => r.HealthcareProfessionals)
            .HasForeignKey(p => p.IncidentReportId)
            .HasConstraintName("FK_IRHealthcareProfessional_IncidentReport");

        modelBuilder.Entity<IncidentReportHealthcareProfessional>()
            .HasOne<Profession>()
            .WithMany()
            .HasForeignKey(p => p.ProfessionId)
            .HasConstraintName("FK_IRHealthcareProfessional_Profession")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReportHealthcareProfessional>()
            .HasOne<Position>()
            .WithMany()
            .HasForeignKey(p => p.PositionId)
            .HasConstraintName("FK_IRHealthcareProfessional_Position")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReportMedication>()
            .HasOne(m => m.IncidentReport)
            .WithMany(r => r.Medications)
            .HasForeignKey(m => m.IncidentReportId)
            .HasConstraintName("FK_IncidentReportMedication_IncidentReport");

        modelBuilder.Entity<IncidentReportWitness>()
            .HasOne(w => w.IncidentReport)
            .WithMany(r => r.Witnesses)
            .HasForeignKey(w => w.IncidentReportId)
            .HasConstraintName("FK_IRWitness_IncidentReport");

        modelBuilder.Entity<IncidentReportOtherDepartment>()
            .HasOne(d => d.IncidentReport)
            .WithMany(r => r.OtherDepartments)
            .HasForeignKey(d => d.IncidentReportId)
            .HasConstraintName("FK_IROtherDepartment_IncidentReport");

        modelBuilder.Entity<IncidentReportOtherDepartment>()
            .HasOne(d => d.UnitDepartment)
            .WithMany()
            .HasForeignKey(d => d.UnitDepartmentId)
            .HasConstraintName("FK_IROtherDepartment_UnitDepartment")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReportReporter>()
            .HasOne(p => p.IncidentReport)
            .WithMany(r => r.Reporters)
            .HasForeignKey(p => p.IncidentReportId)
            .HasConstraintName("FK_IRReporter_IncidentReport");

        modelBuilder.Entity<IncidentReportReporter>()
            .HasOne<Profession>()
            .WithMany()
            .HasForeignKey(p => p.ProfessionId)
            .HasConstraintName("FK_IRReporter_Profession")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentReportManualNotification>()
            .HasOne(n => n.IncidentReport)
            .WithMany(r => r.ManualNotifications)
            .HasForeignKey(n => n.IncidentReportId)
            .HasConstraintName("FK_IRManualNotification_IncidentReport");

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

        // Alert Rule automatic-notification relationships — the DB columns and FKs
        // were added directly via SQL script, same ExcludeFromMigrations convention.
        modelBuilder.Entity<IncidentNotification>()
            .HasOne(n => n.AlertRule)
            .WithMany()
            .HasForeignKey(n => n.AlertRuleId)
            .HasConstraintName("FK_IncidentNotifications_AlertRule")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentNotification>()
            .HasOne(n => n.RecipientUser)
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .HasConstraintName("FK_IncidentNotifications_RecipientUser")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentNotification>()
            .HasOne(n => n.Urgency)
            .WithMany()
            .HasForeignKey(n => n.UrgencyId)
            .HasConstraintName("FK_IncidentNotifications_Urgency")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentNotification>()
            .HasOne(n => n.NotificationMethod)
            .WithMany()
            .HasForeignKey(n => n.NotificationMethodId)
            .HasConstraintName("FK_IncidentNotifications_NotificationMethod")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentNotification>()
            .HasIndex(n => new { n.RecipientUserId, n.IsRead, n.CreatedDate });

        // AlertTriggerHistory — one row per rule match, fanning out to the
        // per-recipient IncidentNotification rows below it. Table/FKs/indexes were
        // added directly via SQL script, same ExcludeFromMigrations convention.
        modelBuilder.Entity<AlertTriggerHistory>()
            .HasOne(x => x.AlertRule)
            .WithMany()
            .HasForeignKey(x => x.AlertRuleId)
            .HasConstraintName("FK_AlertTriggerHistory_AlertRule")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<AlertTriggerHistory>()
            .HasOne(x => x.IncidentReport)
            .WithMany()
            .HasForeignKey(x => x.IncidentReportId)
            .HasConstraintName("FK_AlertTriggerHistory_IncidentReport")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<AlertTriggerHistory>()
            .HasOne(x => x.Urgency)
            .WithMany()
            .HasForeignKey(x => x.UrgencyId)
            .HasConstraintName("FK_AlertTriggerHistory_Urgency")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<AlertTriggerHistory>()
            .HasOne(x => x.AcknowledgedByUser)
            .WithMany()
            .HasForeignKey(x => x.AcknowledgedByUserId)
            .HasConstraintName("FK_AlertTriggerHistory_AcknowledgedBy")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<AlertTriggerHistory>()
            .HasOne(x => x.ResolvedByUser)
            .WithMany()
            .HasForeignKey(x => x.ResolvedByUserId)
            .HasConstraintName("FK_AlertTriggerHistory_ResolvedBy")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<AlertTriggerHistory>()
            .HasIndex(x => x.AlertTriggerNumber).IsUnique();

        modelBuilder.Entity<AlertTriggerHistory>()
            .HasIndex(x => x.DedupeKey).IsUnique();

        modelBuilder.Entity<AlertTriggerHistory>()
            .HasIndex(x => x.TriggeredAt);

        modelBuilder.Entity<AlertTriggerHistory>()
            .HasIndex(x => new { x.Status, x.TriggeredAt });

        modelBuilder.Entity<AlertTriggerHistory>()
            .HasIndex(x => new { x.AlertRuleId, x.TriggeredAt });

        modelBuilder.Entity<IncidentNotification>()
            .HasOne(n => n.AlertTrigger)
            .WithMany(x => x.Notifications)
            .HasForeignKey(n => n.AlertTriggerId)
            .HasConstraintName("FK_IncidentNotifications_AlertTriggerHistory")
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<IncidentNotification>()
            .HasIndex(n => n.AlertTriggerId);

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

        modelBuilder.Entity<UserAvailability>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserAvailability>()
            .HasIndex(a => new { a.UserId, a.DayOfWeek }).IsUnique();

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
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(1000);
        });

        modelBuilder.Entity<AlertTriggerHistory>(entity =>
        {
            entity.Property(e => e.AlertTriggerNumber).HasMaxLength(40);
            entity.Property(e => e.TriggerSource).HasMaxLength(50);
            entity.Property(e => e.ConditionSummary).HasMaxLength(1000);
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.DedupeKey).HasMaxLength(250);
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
            // Not unique — Name alone no longer identifies a row. Two entries can
            // share the same drug/strength label with different Dose/Unit/Route/
            // Frequency/Formulation (e.g. "Warfarin 5mg" given IV at 8mg vs at
            // 23mg are two distinct entries). Uniqueness is enforced at the
            // application level in CurrentMedicationsController on the full
            // (Name, DoseValue, DoseUnitId, RouteId, FrequencyId, FormulationId)
            // tuple instead — see Create()/Update().
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DoseValue).HasPrecision(18, 4);
        });

        modelBuilder.Entity<CurrentMedication>()
            .HasOne<DoseUnit>().WithMany().HasForeignKey(m => m.DoseUnitId)
            .HasConstraintName("FK_CurrentMedication_DoseUnit").OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CurrentMedication>()
            .HasOne<Route>().WithMany().HasForeignKey(m => m.RouteId)
            .HasConstraintName("FK_CurrentMedication_Route").OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CurrentMedication>()
            .HasOne<Frequency>().WithMany().HasForeignKey(m => m.FrequencyId)
            .HasConstraintName("FK_CurrentMedication_Frequency").OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CurrentMedication>()
            .HasOne<Formulation>().WithMany().HasForeignKey(m => m.FormulationId)
            .HasConstraintName("FK_CurrentMedication_Formulation").OnDelete(DeleteBehavior.NoAction);

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
            new Permission { Id = 24, Name = "Submit Medication Error Reports", PermissionTag = "incident_reports.submit_medication_error", ParentId = 1, SystemModuleId = 1 },
            new Permission { Id = 25, Name = "Submit ADR Reports", PermissionTag = "incident_reports.submit_adr", ParentId = 1, SystemModuleId = 1 },

            new Permission { Id = 5, Name = "Clinical Review", PermissionTag = "clinical_review", ParentId = null, SystemModuleId = 2 },
            new Permission { Id = 6, Name = "Start Review", PermissionTag = "clinical_review.start", ParentId = 5, SystemModuleId = 2 },
            new Permission { Id = 7, Name = "Sign Off Review", PermissionTag = "clinical_review.sign_off", ParentId = 5, SystemModuleId = 2 },

            new Permission { Id = 8, Name = "Alert Rules", PermissionTag = "alert_rules", ParentId = null, SystemModuleId = 3 },
            new Permission { Id = 9, Name = "View Alert Rules", PermissionTag = "alert_rules.view", ParentId = 8, SystemModuleId = 3 },
            new Permission { Id = 10, Name = "Manage Alert Rules", PermissionTag = "alert_rules.manage", ParentId = 8, SystemModuleId = 3 },
            new Permission { Id = 26, Name = "View Alert Triggers Dashboard", PermissionTag = "alert_rules.view_dashboard", ParentId = 8, SystemModuleId = 3 },

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
        // Submit Medication Error/ADR (24/25) default to every role that already has
        // the general Submit Report (2) grant, so nobody's ability to submit either
        // report type changes until an Admin deliberately unchecks one — the split
        // is opt-out, not opt-in, on top of the existing baseline. View Alert
        // Triggers Dashboard (26) is a standalone permission alongside View/Manage
        // Alert Rules — only Admin gets it by default, same as 8/9/10 were before.
        var adminAll = Enumerable.Range(1, 26).Select(pid => new RolePermission { RoleId = 3, PermissionId = pid });
        var physician = new[] { 1, 3, 5, 6, 7, 20, 21, 22, 23 }.Select(pid => new RolePermission { RoleId = 2, PermissionId = pid });
        var nurse = new[] { 1, 2, 15, 16, 20, 21, 22, 23, 24, 25 }.Select(pid => new RolePermission { RoleId = 1, PermissionId = pid });
        var pharmacist = new[] { 1, 2, 20, 21, 22, 23, 24, 25 }.Select(pid => new RolePermission { RoleId = 4, PermissionId = pid });

        modelBuilder.Entity<RolePermission>().HasData(
            adminAll.Concat(physician).Concat(nurse).Concat(pharmacist).ToArray()
        );
    }
}
