using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
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
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(TextDark).FontFamily(Fonts.Calibri));

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
    private static (string label, string bg, string color) HarmBadge(string harmLevelCode)
    {
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
                    ("Unit / Location", r.IncidentLocation, null),
                    ("Report Type", type.label, type),
                    ("Stage", r.StageOfProcessId.HasValue ? lookups.StageOfProcesses.GetValueOrDefault(r.StageOfProcessId.Value, "—") : "—", null));
            }));

            col.Item().Element(c => Section(c, "1. Patient Demographics", (inner, next) =>
            {
                StatGrid(inner, next, 3,
                    ("Patient Name", r.PatientName ?? "—", null),
                    ("Patient Ref", r.PatientReference ?? "—", null),
                    ("Sex", r.PatientSex, null),
                    ("Age", r.PatientAge.ToString(), null),
                    ("Weight", r.PatientWeightKg.HasValue ? $"{r.PatientWeightKg:0.##} kg" : "—", null),
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
                    ("Position", r.PositionId.HasValue ? lookups.Positions.GetValueOrDefault(r.PositionId.Value, "—") : "—", null));
            }));

            var currentMeds = r.CurrentMedicationLinks.Select(m => lookups.CurrentMedications.GetValueOrDefault(m.CurrentMedicationId, "—")).ToList();
            if (currentMeds.Count > 0)
            {
                col.Item().Element(c => Section(c, "4. Clinical Background", (inner, next) =>
                {
                    FullTextRow(inner, next, "Current Medications", string.Join(", ", currentMeds));
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
                    StatGrid(inner, next, 1, ("Suspected Causality", r.SuspectedCausality ?? "—", null));
                    if (!string.IsNullOrWhiteSpace(r.AdrReactionDescription))
                        FullTextRow(inner, next, "ADR Reaction Description", r.AdrReactionDescription);
                }

                FullTextRow(inner, next, "Narrative", r.IncidentNarrative);
                if (!string.IsNullOrWhiteSpace(r.ImmediateActionTaken))
                    FullTextRow(inner, next, "Immediate Actions Taken", r.ImmediateActionTaken);
                if (!string.IsNullOrWhiteSpace(r.PatientOutcomeDetails))
                    FullTextRow(inner, next, "Outcome Detail", r.PatientOutcomeDetails);
            }));

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
                        cell.ConstantItem(85).Text(label).FontSize(8).Bold().FontColor(TextMuted);
                        cell.RelativeItem().BorderLeft(0.6f).BorderColor(Border).PaddingLeft(8).Element(e =>
                        {
                            if (badge.HasValue)
                                Pill(e, badge.Value.label, badge.Value.bg, badge.Value.color);
                            else
                                e.Text(value).FontSize(9).Bold().FontColor(TextDark);
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
            row.ConstantItem(150).Text(label).FontSize(8).Bold().FontColor(TextMuted);
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

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(cd =>
            {
                cd.RelativeColumn(2);
                cd.RelativeColumn(1);
                cd.RelativeColumn(1.2f);
                cd.RelativeColumn(1.2f);
                cd.RelativeColumn(1.2f);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Drug").FontSize(8.5f).Bold().FontColor(Navy);
                header.Cell().Element(HeaderCell).Text("Dose").FontSize(8.5f).Bold().FontColor(Navy);
                header.Cell().Element(HeaderCell).Text("Route").FontSize(8.5f).Bold().FontColor(Navy);
                header.Cell().Element(HeaderCell).Text("Frequency").FontSize(8.5f).Bold().FontColor(Navy);
                header.Cell().Element(HeaderCell).Text("Formulation").FontSize(8.5f).Bold().FontColor(Navy);
            });

            foreach (var m in r.Medications)
            {
                table.Cell().Element(BodyCell).Text(m.MedicationName).FontSize(8.5f);
                table.Cell().Element(BodyCell).Text($"{m.DoseValue} {lookups.DoseUnits.GetValueOrDefault(m.DoseUnitId, "")}".Trim()).FontSize(8.5f);
                table.Cell().Element(BodyCell).Text(lookups.Routes.GetValueOrDefault(m.RouteId, "—")).FontSize(8.5f);
                table.Cell().Element(BodyCell).Text(m.FrequencyId.HasValue ? lookups.Frequencies.GetValueOrDefault(m.FrequencyId.Value, "—") : "—").FontSize(8.5f);
                table.Cell().Element(BodyCell).Text(m.FormulationId.HasValue ? lookups.Formulations.GetValueOrDefault(m.FormulationId.Value, "—") : "—").FontSize(8.5f);
            }
        });
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(RowTint).Padding(6).Border(0.6f).BorderColor(Border);

    private static IContainer BodyCell(IContainer container) =>
        container.Padding(6).Border(0.6f).BorderColor(Border);

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
        Positions = await _db.Positions.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken)
    };
}
