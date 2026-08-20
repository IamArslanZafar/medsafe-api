namespace MedSafe.Models;

// "Other Service(s)/Dept(s) Involved" — secondary units involved in the incident,
// beyond the primary Unit/Department already captured on the report.
public class IncidentReportOtherDepartment
{
    public int Id { get; set; }
    public int IncidentReportId { get; set; }
    public int UnitDepartmentId { get; set; }

    public IncidentReport IncidentReport { get; set; } = null!;
    public UnitDepartment UnitDepartment { get; set; } = null!;
}
