using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;

namespace MedSafeAPI.Services;

// Turns a lookup-backed condition field's numeric id into the human-readable name
// an Admin actually recognizes — shared by rule authoring (AlertRuleService) and
// rule evaluation (AlertRuleEvaluationService) so "why did this alert trigger"
// reads the same way everywhere it's shown.
public static class AlertConditionLabelResolver
{
    public static async Task<string?> ResolveNameAsync(AppDbContext db, string fieldCode, int lookupValueId, CancellationToken cancellationToken) =>
        fieldCode switch
        {
            "ERROR_CATEGORY" => (await db.ErrorCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == lookupValueId, cancellationToken))?.Name,
            "STAGE_OF_PROCESS" => (await db.StageOfProcesses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == lookupValueId, cancellationToken))?.Name,
            "PATIENT_OUTCOME" => (await db.PatientOutcomes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == lookupValueId, cancellationToken))?.Name,
            "SERIOUSNESS_CRITERIA" => (await db.SeriousnessCriteria.AsNoTracking().FirstOrDefaultAsync(x => x.Id == lookupValueId, cancellationToken))?.Name,
            "CONTRIBUTING_FACTOR" => (await db.ContributingFactors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == lookupValueId, cancellationToken))?.Name,
            _ => null
        };
}
