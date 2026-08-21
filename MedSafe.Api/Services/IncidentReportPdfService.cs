using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace MedSafeAPI.Services;

// Server-side re-creation of the frontend's "Clinical Incident Report" PDF
// (src/components/form/IncidentReportPdf.js) — same navy section bands,
// zebra-striped label/value grids, and status/type/harm pills — so the emailed
// copy looks identical to the one a user can download from the Reports Hub.
// Deliberately excludes notification recipients, email delivery attempts,
// audit log entries and other internal/system IDs; only clinical content goes
// into the emailed copy. No HIPAA masking toggle server-side, so PHI is always
// shown as recorded (the masking option is a frontend-only viewing preference).
public class IncidentReportPdfService : IIncidentReportPdfService
{
    private const string Navy = "#203f66";
    private const string TextDark = "#111827";
    private const string TextMuted = "#5a6a7e";
    private const string Border = "#c9d3e0";
    private const string RowTint = "#eef2f8";

    private static readonly string LogoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "logo-with-bg.png");

    private readonly AppDbContext _db;

    public IncidentReportPdfService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> GenerateAsync(int incidentReportId, CancellationToken cancellationToken)
    {
        var report = await _db.IncidentReports
            .AsNoTracking()
            .Include(r => r.Medications)
            .Include(r => r.ContributingFactors)
            .Include(r => r.SeriousnessCriteria)
            .Include(r => r.AllergyLinks)
            .Include(r => r.CurrentMedicationLinks)
            .Include(r => r.ConcomitantMedications)
            .Include(r => r.HealthcareProfessionals)
            .Include(r => r.Witnesses)
            .Include(r => r.OtherDepartments)
            .Include(r => r.Reporters)
            .Include(r => r.ManualNotifications)
            .Include(r => r.Attachments)
            .Include(r => r.Review)
            .FirstOrDefaultAsync(r => r.Id == incidentReportId, cancellationToken);

        if (report == null)
            throw new InvalidOperationException($"Incident report {incidentReportId} was not found.");

        var lookups = await LoadLookupNamesAsync(cancellationToken);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(950, 650);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(TextDark).FontFamily("Rubik"));

                // Header is composed as the first item of Content (not via page.Header(),
                // which QuestPDF repeats on every page) — matches the frontend, which
                // only draws it once at the very top before the content flows down and
                // spills onto later pages.
                page.Content().Column(col =>
                {
                    col.Item().Element(ComposeHeader);
                    col.Item().PaddingTop(18).Element(c => ComposeContent(c, report, lookups));
                });
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                if (File.Exists(LogoPath))
                    row.ConstantItem(75).Image(LogoPath).FitWidth();
                else
                    row.ConstantItem(75);

                row.RelativeItem().Column(inner =>
                {
                    inner.Item().AlignCenter().Text("CLINICAL INCIDENT REPORT").FontSize(18).Bold().FontColor(TextDark);
                    inner.Item().AlignCenter().Text("Qatar Trauma Center Medication Reporting System (QTCMRS)").FontSize(9.5f).FontColor(Navy);
                });

                row.ConstantItem(140).Column(inner =>
                {
                    inner.Item().AlignRight().Text("Generated On").FontSize(8.5f).FontColor(TextMuted);
                    inner.Item().AlignRight().Text(DateTime.UtcNow.ToString("dd MMM yyyy, hh:mm tt")).FontSize(9.5f).Bold().FontColor(TextDark);
                });
            });

            col.Item().PaddingTop(10).LineHorizontal(1.6f).LineColor(Navy);
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(4).BorderTop(0.5f).BorderColor(Border);
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text("Confidential - For authorized use only.").FontSize(8).FontColor(TextMuted);
                row.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Page ").FontSize(8).FontColor(TextMuted);
                    x.CurrentPageNumber().FontSize(8).FontColor(TextMuted);
                    x.Span(" of ").FontSize(8).FontColor(TextMuted);
                    x.TotalPages().FontSize(8).FontColor(TextMuted);
                });
            });
        });
    }

    private static (string label, string bg, string color) StatusBadge(string status) => status switch
    {
        "Closed" => ("Closed", "#f6ffed", "#52c41a"),
        "UnderReview" => ("Under Review", "#f3e8ff", "#7c3aed"),
        "Pending" => ("Pending", "#fff7e6", "#fa8c16"),
        _ => ("Open", "#e6f4ff", "#1677ff")
    };

    private static (string label, string bg, string color) TypeBadge(string reportType) => reportType switch
    {
        "Near Miss" => ("Near Miss", "#f6ffed", "#52c41a"),
        "ADR" => ("ADR Reaction", "#fff1f0", "#ff4d4f"),
        _ => ("Medication Error", "#e6f4ff", "#1677ff")
    };

    private static readonly string[] HarmCodes = ["E", "F", "G", "H", "I"];
    private static (string label, string bg, string color) HarmBadge(string? harmLevelCode)
    {
        // ADR reports have no NCC MERP harm level.
        if (harmLevelCode == null)
            return ("N/A", "#f0f0f0", "#434343");

        var isHarm = HarmCodes.Contains(harmLevelCode);
        return ($"Cat {harmLevelCode}", isHarm ? "#fff1f0" : "#f0f0f0", isHarm ? "#ff4d4f" : "#434343");
    }

    private static void ComposeContent(IContainer container, IncidentReport r, LookupNames lookups)
    {
        var status = StatusBadge(r.ReportStatus);
        var type = TypeBadge(r.ReportType);
        var harm = HarmBadge(r.HarmLevelCode);

        container.Column(col =>
        {
            col.Spacing(14);

            col.Item().Element(c => Section(c, "Report Summary", (inner, next) =>
            {
                StatGrid(inner, next, 3,
                    ("Report ID", r.IncidentReportNumber, null),
                    ("Status", status.label, status),
                    ("Harm Category", harm.label, harm),
                    ("Unit / Location", r.SectionId.HasValue ? $"{r.IncidentLocation} / {lookups.Sections.GetValueOrDefault(r.SectionId.Value, "—")}" : r.IncidentLocation, null),
                    ("Report Type", type.label, type),
                    ("Stage", r.StageOfProcessId.HasValue ? lookups.StageOfProcesses.GetValueOrDefault(r.StageOfProcessId.Value, "—") : "—", null),
                    ("Reported Incident Severity", r.ReportedIncidentSeverityId.HasValue ? lookups.ReportedIncidentSeverities.GetValueOrDefault(r.ReportedIncidentSeverityId.Value, "—") : "—", null));

                if (r.OtherDepartments.Count > 0)
                {
                    var names = r.OtherDepartments.Select(d => lookups.UnitDepartments.GetValueOrDefault(d.UnitDepartmentId, "—"));
                    FullTextRow(inner, next, "Other Service(s)/Dept(s) Involved", string.Join(", ", names));
                }
            }));

            col.Item().Element(c => Section(c, "1. Patient Demographics", (inner, next) =>
            {
                StatGrid(inner, next, 3,
                    ("Patient Name", r.PatientName ?? "—", null),
                    ("Patient Ref", r.PatientReference ?? "—", null),
                    ("Sex", r.PatientSex, null),
                    ("Age", r.PatientAge.ToString(), null),
                    ("Date of Birth", r.PatientDateOfBirth.HasValue ? r.PatientDateOfBirth.Value.ToString("dd MMM yyyy") : "—", null),
                    ("Weight", r.PatientWeightKg.HasValue ? $"{r.PatientWeightKg:0.##} kg" : "—", null),
                    ("Admission Date", r.AdmissionDate.HasValue ? r.AdmissionDate.Value.ToString("dd MMM yyyy") : "—", null),
                    ("Current Diagnosis", string.IsNullOrWhiteSpace(r.CurrentDiagnosis) ? "—" : r.CurrentDiagnosis, null),
                    ("Known Allergies", r.AllergyLinks.Count > 0
                        ? string.Join(", ", r.AllergyLinks.Select(a => lookups.Allergies.GetValueOrDefault(a.AllergyId, "—")))
                        : "None specified", null));
            }));

            col.Item().Element(c => Section(c, "2. Medication Details", inner =>
            {
                MedicationTable(inner, r, lookups);
            }));

            col.Item().Element(c => Section(c, "3. Alerts & Healthcare Professional Involved", (inner, next) =>
            {
                StatGrid(inner, next, 2,
                    ("Profession", r.ProfessionId.HasValue ? lookups.Professions.GetValueOrDefault(r.ProfessionId.Value, "—") : "—", null),
                    ("Position", r.PositionId.HasValue ? lookups.Positions.GetValueOrDefault(r.PositionId.Value, "—") : "—", null),
                    ("Entered By Title", string.IsNullOrWhiteSpace(r.EnteredByTitle) ? "—" : r.EnteredByTitle, null),
                    ("Reporter Phone", string.IsNullOrWhiteSpace(r.ReporterPhoneNumber) ? "—" : r.ReporterPhoneNumber, null),
                    ("Visit Type", r.VisitTypeId.HasValue ? lookups.VisitTypes.GetValueOrDefault(r.VisitTypeId.Value, "—") : "—", null),
                    ("Reporting Source", r.ReportingSourceId.HasValue ? lookups.ReportingSources.GetValueOrDefault(r.ReportingSourceId.Value, "—") : "—", null));

                if (r.HealthcareProfessionals.Count > 0)
                {
                    SimpleTable(inner,
                        ["Name", "Profession", "Position", "Contact"],
                        r.HealthcareProfessionals.Select(hp => new[]
                        {
                            hp.Name,
                            lookups.Professions.GetValueOrDefault(hp.ProfessionId, "—"),
                            lookups.Positions.GetValueOrDefault(hp.PositionId, "—"),
                            hp.ContactNumber ?? "—",
                        }));
                }

                if (r.Reporters.Count > 0)
                {
                    SimpleTable(inner,
                        ["Reported By", "Profession", "Date"],
                        r.Reporters.Select(rep => new[]
                        {
                            rep.Name,
                            rep.ProfessionId.HasValue ? lookups.Professions.GetValueOrDefault(rep.ProfessionId.Value, "—") : "—",
                            rep.ReportedDate.ToString("dd MMM yyyy"),
                        }));
                }

                if (r.Witnesses.Count > 0)
                {
                    SimpleTable(inner,
                        ["Witness", "Address", "Phone"],
                        r.Witnesses.Select(w => new[]
                        {
                            w.Name,
                            w.Address ?? "—",
                            w.PhoneNumber ?? "—",
                        }));
                }

                if (r.ManualNotifications.Count > 0)
                {
                    SimpleTable(inner,
                        ["Notifications — Type of Person", "Name", "Date"],
                        r.ManualNotifications.Select(n => new[]
                        {
                            n.TypeOfPersonNotified,
                            n.Name,
                            n.NotifiedAt.ToString("dd MMM yyyy HH:mm"),
                        }));
                }
            }));

            var currentMeds = r.CurrentMedicationLinks.Select(m => lookups.CurrentMedications.GetValueOrDefault(m.CurrentMedicationId, "—")).ToList();
            if (currentMeds.Count > 0 || !string.IsNullOrWhiteSpace(r.RelevantMedicalHistory))
            {
                col.Item().Element(c => Section(c, "4. Clinical Background", (inner, next) =>
                {
                    if (currentMeds.Count > 0)
                        FullTextRow(inner, next, "Current Medications", string.Join(", ", currentMeds));
                    if (!string.IsNullOrWhiteSpace(r.RelevantMedicalHistory))
                        FullTextRow(inner, next, "Relevant Medical History", r.RelevantMedicalHistory);
                }));
            }

            col.Item().Element(c => Section(c, "5. Incident Classification", (inner, next) =>
            {
                StatGrid(inner, next, 2,
                    ("Type", type.label, type),
                    ("Harm Category", harm.label, harm),
                    ("Stage", r.StageOfProcessId.HasValue ? lookups.StageOfProcesses.GetValueOrDefault(r.StageOfProcessId.Value, "—") : "—", null),
                    ("Status", status.label, status));

                if (r.ReportType == "ADR")
                {
                    StatGrid(inner, next, 2,
                        ("ADR Severity", r.AdrSeverityId.HasValue ? lookups.AdrSeverities.GetValueOrDefault(r.AdrSeverityId.Value, "—") : "—", null),
                        ("Suspected Causality", r.SuspectedCausality ?? "—", null),
                        ("Reaction Started", r.ReactionStartAt.HasValue ? r.ReactionStartAt.Value.ToString("dd MMM yyyy HH:mm") : "—", null),
                        ("Reaction Stopped", r.ReactionStoppedAt.HasValue ? r.ReactionStoppedAt.Value.ToString("dd MMM yyyy HH:mm") : "—", null));
                    if (!string.IsNullOrWhiteSpace(r.AdrReactionDescription))
                        FullTextRow(inner, next, "ADR Reaction Description", r.AdrReactionDescription);
                    if (!string.IsNullOrWhiteSpace(r.AdrAdditionalInformation))
                        FullTextRow(inner, next, "Additional Information", r.AdrAdditionalInformation);
                }
                else if (r.ErrorCategoryId.HasValue)
                {
                    StatGrid(inner, next, 1, ("Error Category", lookups.ErrorCategories.GetValueOrDefault(r.ErrorCategoryId.Value, "—"), null));
                }

                if (r.IsResearchStudyRelated.HasValue)
                {
                    StatGrid(inner, next, 1,
                        ("Related to research study (MRC-approved, unanticipated problem)", r.IsResearchStudyRelated.Value ? "Yes" : "No", null));
                }

                StatGrid(inner, next, 1,
                    ("Patient Outcome", lookups.PatientOutcomes.GetValueOrDefault(r.PatientOutcomeId, "—"), null));

                if (r.ReportType == "ADR" && r.SeriousnessCriteria.Count > 0)
                {
                    FullTextRow(inner, next, "Seriousness Criteria",
                        string.Join(", ", r.SeriousnessCriteria.Select(sc => lookups.SeriousnessCriteria.GetValueOrDefault(sc.SeriousnessCriterionId, "—"))));
                }

                if (r.ContributingFactors.Count > 0)
                {
                    FullTextRow(inner, next, "Contributing Factors",
                        string.Join(", ", r.ContributingFactors.Select(cf => lookups.ContributingFactors.GetValueOrDefault(cf.ContributingFactorId, "—"))));
                }

                FullTextRow(inner, next, "Narrative", r.IncidentNarrative);
                if (!string.IsNullOrWhiteSpace(r.ImmediateActionTaken))
                    FullTextRow(inner, next, "Immediate Actions Taken", r.ImmediateActionTaken);
                if (!string.IsNullOrWhiteSpace(r.PatientOutcomeDetails))
                    FullTextRow(inner, next, "Outcome Detail", r.PatientOutcomeDetails);
            }));

            if (r.ReportType == "ADR" && r.ConcomitantMedications.Count > 0)
            {
                col.Item().Element(c => Section(c, "Concomitant Medications", inner =>
                {
                    SimpleTable(inner, ["Care Setting", "Medication"],
                        r.ConcomitantMedications.Select(m => new[]
                        {
                            m.CareSettingCode == "INPATIENT" ? "Inpatient" : "Outpatient",
                            m.MedicationText,
                        }));
                }));
            }

            var activeAttachments = r.Attachments.Where(a => !a.IsDeleted).ToList();
            if (activeAttachments.Count > 0)
            {
                col.Item().Element(c => Section(c, "Attachments", inner =>
                {
                    SimpleTable(inner, ["File Name", "Category", "Description"],
                        activeAttachments.Select(a => new[]
                        {
                            a.OriginalFileName,
                            a.Category ?? "—",
                            a.Description ?? "—",
                        }));
                }));
            }

            if (r.Review != null)
            {
                col.Item().Element(c => Section(c, "6. Clinical Review", (inner, next) =>
                {
                    StatGrid(inner, next, 3,
                        ("Reviewer", "Assigned", null),
                        ("Assessment", string.IsNullOrWhiteSpace(r.Review.ClinicalAssessmentNote) ? "-" : r.Review.ClinicalAssessmentNote, null),
                        ("Follow-up", string.IsNullOrWhiteSpace(r.Review.FollowUpActions) ? "-" : r.Review.FollowUpActions, null));
                }));
            }
        });
    }

    // `title.ToUpperInvariant()` navy band, then a bordered zebra-striped body.
    // The striper is shared by every call the caller makes inside `content`, so
    // a section's stat grid and its narrative rows read as one continuous table
    // — matching the frontend's stripeIndex behaviour exactly.
    private static void Section(IContainer container, string title, Action<ColumnDescriptor, Func<bool>> content)
    {
        var stripe = 0;
        bool Next() => stripe++ % 2 == 0;

        container.Column(col =>
        {
            col.Item().Background(Navy).Padding(6).Text(title.ToUpperInvariant()).FontSize(9.5f).Bold().FontColor("#ffffff");
            col.Item().Column(inner => content(inner, Next));
        });
    }

    private static void Section(IContainer container, string title, Action<ColumnDescriptor> content) =>
        Section(container, title, (inner, _) => content(inner));

    // Each label/value pair gets its OWN bordered box (matching the frontend's
    // per-cell doc.rect(...) calls) — a row of `cols` items is `cols` separate
    // boxes side by side, not one undivided strip. A partial last row (fewer
    // items than `cols`) is padded with blank spacers so every box keeps the
    // same width as a full row's, instead of stretching to fill the gap.
    private static void StatGrid(ColumnDescriptor col, Func<bool> nextStripe, int cols, params (string label, string value, (string label, string bg, string color)? badge)[] items)
    {
        for (var i = 0; i < items.Length; i += cols)
        {
            var rowItems = items.Skip(i).Take(cols).ToArray();
            var tint = nextStripe();
            col.Item().Row(row =>
            {
                foreach (var (label, value, badge) in rowItems)
                {
                    row.RelativeItem().Background(tint ? RowTint : "#ffffff").Border(0.6f).BorderColor(Border).Padding(4).Row(cell =>
                    {
                        cell.ConstantItem(85).Text(label).FontSize(8).Bold().FontColor(Navy);
                        cell.RelativeItem().BorderLeft(0.6f).BorderColor(Border).PaddingLeft(8).Element(e =>
                        {
                            if (badge.HasValue)
                                Pill(e, badge.Value.label, badge.Value.bg, badge.Value.color);
                            else
                                e.Text(value).FontSize(9).FontColor(TextDark);
                        });
                    });
                }
                for (var pad = rowItems.Length; pad < cols; pad++)
                    row.RelativeItem();
            });
        }
    }

    private static void FullTextRow(ColumnDescriptor col, Func<bool> nextStripe, string label, string value)
    {
        var tint = nextStripe();
        col.Item().Background(tint ? RowTint : "#ffffff").Border(0.6f).BorderColor(Border).Padding(6).Row(row =>
        {
            row.ConstantItem(150).Text(label).FontSize(8).Bold().FontColor(Navy);
            row.RelativeItem().BorderLeft(0.6f).BorderColor(Border).PaddingLeft(8)
                .Text(string.IsNullOrWhiteSpace(value) ? "—" : value).FontSize(9).FontColor(TextDark);
        });
    }

    private static void Pill(IContainer container, string text, string bg, string color) =>
        container.AlignLeft().Background(bg).Border(0.7f).BorderColor(color).CornerRadius(3).Padding(4).PaddingVertical(2)
            .Text(text).FontSize(8).Bold().FontColor(color);

    private static void MedicationTable(ColumnDescriptor col, IncidentReport r, LookupNames lookups)
    {
        if (r.Medications.Count == 0)
        {
            col.Item().Padding(6).Text("No medications recorded.").FontSize(9).Italic().FontColor(TextMuted);
            return;
        }

        var isAdr = r.ReportType == "ADR";
        var showGeneric = r.Medications.Any(m => !string.IsNullOrWhiteSpace(m.GenericName));
        var showDrugClass = r.Medications.Any(m => !string.IsNullOrWhiteSpace(m.DrugClass));

        var headers = new List<string> { "Drug" };
        if (showGeneric) headers.Add("Generic Name");
        if (showDrugClass) headers.Add("Drug Class");
        headers.AddRange(["Dose", "Route", "Frequency", "Formulation"]);
        if (isAdr) headers.AddRange(["Manufacturer", "Batch / Lot", "Therapy Period", "Expiry Date"]);

        var rows = r.Medications.Select(m =>
        {
            var cells = new List<string> { m.MedicationName };
            if (showGeneric) cells.Add(string.IsNullOrWhiteSpace(m.GenericName) ? "—" : m.GenericName);
            if (showDrugClass) cells.Add(string.IsNullOrWhiteSpace(m.DrugClass) ? "—" : m.DrugClass);
            cells.Add($"{m.DoseValue} {(m.DoseUnitId.HasValue ? lookups.DoseUnits.GetValueOrDefault(m.DoseUnitId.Value, "") : "")}".Trim());
            cells.Add(m.RouteId.HasValue ? lookups.Routes.GetValueOrDefault(m.RouteId.Value, "—") : "—");
            cells.Add(m.FrequencyId.HasValue ? lookups.Frequencies.GetValueOrDefault(m.FrequencyId.Value, "—") : "—");
            cells.Add(m.FormulationId.HasValue ? lookups.Formulations.GetValueOrDefault(m.FormulationId.Value, "—") : "—");
            if (isAdr)
            {
                cells.Add(string.IsNullOrWhiteSpace(m.Manufacturer) ? "—" : m.Manufacturer);
                cells.Add(string.IsNullOrWhiteSpace(m.BatchLotNumber) ? "—" : m.BatchLotNumber);
                cells.Add(m.TherapyStartAt.HasValue || m.TherapyStopAt.HasValue
                    ? $"{(m.TherapyStartAt.HasValue ? m.TherapyStartAt.Value.ToString("dd MMM yyyy HH:mm") : "—")} to {(m.TherapyStopAt.HasValue ? m.TherapyStopAt.Value.ToString("dd MMM yyyy HH:mm") : "—")}"
                    : "—");
                cells.Add(m.ExpiryDate.HasValue ? m.ExpiryDate.Value.ToString("dd MMM yyyy") : "—");
            }
            return cells.ToArray();
        });

        SimpleTable(col, [.. headers], rows);
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(RowTint).Border(0.6f).BorderColor(Border).Padding(6);

    private static IContainer BodyCell(IContainer container) =>
        container.Border(0.6f).BorderColor(Border).Padding(6);

    // Generic bordered/header table for the smaller child-record lists (Concomitant
    // Medications, Other Healthcare Professionals) — same visual style as
    // MedicationTable but without a fixed column shape.
    private static void SimpleTable(ColumnDescriptor col, string[] headers, IEnumerable<string[]> rows)
    {
        col.Item().PaddingTop(4).Table(table =>
        {
            table.ColumnsDefinition(cd =>
            {
                foreach (var _ in headers) cd.RelativeColumn();
            });

            table.Header(header =>
            {
                foreach (var h in headers)
                    header.Cell().Element(HeaderCell).Text(h).FontSize(8.5f).Bold().FontColor(Navy);
            });

            foreach (var row in rows)
            {
                foreach (var cell in row)
                    table.Cell().Element(BodyCell).Text(cell).FontSize(8.5f);
            }
        });
    }

    private sealed class LookupNames
    {
        public Dictionary<int, string> StageOfProcesses { get; init; } = [];
        public Dictionary<int, string> DoseUnits { get; init; } = [];
        public Dictionary<int, string> Routes { get; init; } = [];
        public Dictionary<int, string> Frequencies { get; init; } = [];
        public Dictionary<int, string> Formulations { get; init; } = [];
        public Dictionary<int, string> Allergies { get; init; } = [];
        public Dictionary<int, string> CurrentMedications { get; init; } = [];
        public Dictionary<int, string> Professions { get; init; } = [];
        public Dictionary<int, string> Positions { get; init; } = [];
        public Dictionary<int, string> ErrorCategories { get; init; } = [];
        public Dictionary<int, string> AdrSeverities { get; init; } = [];
        public Dictionary<int, string> VisitTypes { get; init; } = [];
        public Dictionary<int, string> ReportingSources { get; init; } = [];
        public Dictionary<int, string> Sections { get; init; } = [];
        public Dictionary<int, string> ReportedIncidentSeverities { get; init; } = [];
        public Dictionary<int, string> UnitDepartments { get; init; } = [];
        public Dictionary<int, string> PatientOutcomes { get; init; } = [];
        public Dictionary<int, string> ContributingFactors { get; init; } = [];
        public Dictionary<int, string> SeriousnessCriteria { get; init; } = [];
    }

    private async Task<LookupNames> LoadLookupNamesAsync(CancellationToken cancellationToken) => new()
    {
        StageOfProcesses = await _db.StageOfProcesses.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken),
        DoseUnits = await _db.DoseUnits.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken),
        Routes = await _db.Routes.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken),
        Frequencies = await _db.Frequencies.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken),
        Formulations = await _db.Formulations.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken),
        Allergies = await _db.Allergies.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken),
        CurrentMedications = await _db.CurrentMedications.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken),
        Professions = await _db.Professions.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken),
        Positions = await _db.Positions.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken),
        ErrorCategories = await _db.ErrorCategories.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken),
        AdrSeverities = await _db.AdrSeverities.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken),
        VisitTypes = await _db.VisitTypes.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken),
        ReportingSources = await _db.ReportingSources.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken),
        Sections = await _db.Sections.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken),
        ReportedIncidentSeverities = await _db.ReportedIncidentSeverities.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken),
        UnitDepartments = await _db.UnitDepartments.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken),
        PatientOutcomes = await _db.PatientOutcomes.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken),
        ContributingFactors = await _db.ContributingFactors.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken),
        SeriousnessCriteria = await _db.SeriousnessCriteria.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken)
    };
}
