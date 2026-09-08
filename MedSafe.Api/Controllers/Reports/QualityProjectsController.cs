using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedSafe.Infrastructure.Data;
using MedSafe.Models;
using MedSafeAPI.DTOs;
using MedSafeAPI.Services;

namespace MedSafeAPI.Controllers;

// Backs the "Quality Project Tracker" module's Dashboard + New Project
// (charter + PDSA cycles) pages.
[ApiController]
[Route("api/quality-projects")]
[Authorize]
public class QualityProjectsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public QualityProjectsController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Create(QualityProjectCreateDto dto)
    {
        var project = new QualityProject
        {
            ProjectCode = $"QPT-{Random.Shared.Next(1000000, 9999999)}",
            ProjectTitle = dto.ProjectTitle,
            OrgName = dto.OrgName,
            SponsorName = dto.SponsorName,
            AimStatement = dto.AimStatement,
            Problem = dto.Problem,
            Reason = dto.Reason,
            Outcomes = dto.Outcomes,
            OutcomeMeasures = dto.OutcomeMeasures,
            ProcessMeasures = dto.ProcessMeasures,
            InitialActivities = dto.InitialActivities,
            Barriers = dto.Barriers,
            Stakeholders = dto.Stakeholders,
            Status = dto.Status,
            CreatedByUserId = _currentUser.UserId,
            CreatedByRole = _currentUser.Role,
        };

        _db.QualityProjects.Add(project);
        await _db.SaveChangesAsync();
        return Ok(await MapToDto(project.Id));
    }

    // Matches the "manage_configurations" permission already gating the dashboard route.
    // Admin (or a user granted "Admin Data Access") sees every project; everyone
    // else sees only the ones they themselves created — same rule as GET /incident-reports.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = _db.QualityProjects.AsQueryable();
        if (_currentUser.Role != "Admin" && !_currentUser.HasFullDataAccess)
            query = query.Where(p => p.CreatedByUserId == _currentUser.UserId);

        var projects = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        var result = new List<QualityProjectDto>();
        foreach (var p in projects)
        {
            result.Add(await MapToDto(p.Id, p));
        }
        return Ok(result);
    }

    // True for Admin, a user granted "Admin Data Access", or the project's own creator —
    // guards GetById/Update/UpsertCycle below so a project id can't be viewed or edited
    // by a user it doesn't belong to just by knowing/guessing its numeric id.
    private bool CanAccessProject(QualityProject project) =>
        _currentUser.Role == "Admin" || _currentUser.HasFullDataAccess || project.CreatedByUserId == _currentUser.UserId;

    // The tracker page is opened directly by id (/quality-project-tracker/:id),
    // not from an already-fetched list — unlike the other 4 modules, this one
    // genuinely needs a detail-by-id endpoint.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var project = await _db.QualityProjects.FindAsync(id);
        if (project == null) return NotFound();
        if (!CanAccessProject(project)) return Forbid();
        return Ok(await MapToDto(id, project));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, QualityProjectUpdateDto dto)
    {
        var project = await _db.QualityProjects.FindAsync(id);
        if (project == null) return NotFound();
        if (!CanAccessProject(project)) return Forbid();

        project.ProjectTitle = dto.ProjectTitle;
        project.OrgName = dto.OrgName;
        project.SponsorName = dto.SponsorName;
        project.AimStatement = dto.AimStatement;
        project.Problem = dto.Problem;
        project.Reason = dto.Reason;
        project.Outcomes = dto.Outcomes;
        project.OutcomeMeasures = dto.OutcomeMeasures;
        project.ProcessMeasures = dto.ProcessMeasures;
        project.InitialActivities = dto.InitialActivities;
        project.Barriers = dto.Barriers;
        project.Stakeholders = dto.Stakeholders;
        project.Status = dto.Status;
        project.Achievements = dto.Achievements;
        project.ProgressNotes = dto.ProgressNotes;

        await _db.SaveChangesAsync();
        return Ok(await MapToDto(id, project));
    }

    // One upsert for both "add cycle" and "edit cycle" — PDSACycleTab.jsx
    // already treats both as the same save action.
    [HttpPost("{id:int}/cycles")]
    public async Task<IActionResult> UpsertCycle(int id, QualityProjectCycleUpsertDto dto)
    {
        var project = await _db.QualityProjects.FindAsync(id);
        if (project == null) return NotFound();
        if (!CanAccessProject(project)) return Forbid();

        QualityProjectCycle cycle;
        if (dto.Id > 0)
        {
            var existing = await _db.QualityProjectCycles
                .FirstOrDefaultAsync(c => c.Id == dto.Id && c.QualityProjectId == id);
            if (existing == null) return NotFound();
            cycle = existing;
        }
        else
        {
            cycle = new QualityProjectCycle { QualityProjectId = id };
            _db.QualityProjectCycles.Add(cycle);
        }

        cycle.ChangeIdea = dto.ChangeIdea;
        cycle.TesterJson = dto.TesterJson;
        cycle.Timeframe = dto.Timeframe;
        cycle.Location = dto.Location;
        cycle.ParticipantsJson = dto.ParticipantsJson;
        cycle.LearningGoal = dto.LearningGoal;
        cycle.PredictionsJson = dto.PredictionsJson;
        cycle.Observations = dto.Observations;
        cycle.StudyResults = dto.StudyResults;
        cycle.StudyLearning = dto.StudyLearning;
        cycle.ActPlan = dto.ActPlan;
        cycle.Decision = dto.Decision;
        cycle.Status = dto.Status;

        await _db.SaveChangesAsync();
        return Ok(MapCycleToDto(cycle));
    }

    private async Task<QualityProjectDto> MapToDto(int id, QualityProject? project = null)
    {
        project ??= await _db.QualityProjects.FindAsync(id);
        var cycles = await _db.QualityProjectCycles
            .Where(c => c.QualityProjectId == id)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return new QualityProjectDto
        {
            Id = project!.Id,
            ProjectCode = project.ProjectCode,
            ProjectTitle = project.ProjectTitle,
            OrgName = project.OrgName,
            SponsorName = project.SponsorName,
            AimStatement = project.AimStatement,
            Problem = project.Problem,
            Reason = project.Reason,
            Outcomes = project.Outcomes,
            OutcomeMeasures = project.OutcomeMeasures,
            ProcessMeasures = project.ProcessMeasures,
            InitialActivities = project.InitialActivities,
            Barriers = project.Barriers,
            Stakeholders = project.Stakeholders,
            Status = project.Status,
            Achievements = project.Achievements,
            ProgressNotes = project.ProgressNotes,
            CreatedByUserId = project.CreatedByUserId,
            CreatedByRole = project.CreatedByRole,
            CreatedAt = project.CreatedAt,
            Cycles = cycles.Select(MapCycleToDto).ToList(),
        };
    }

    private static QualityProjectCycleDto MapCycleToDto(QualityProjectCycle c) => new()
    {
        Id = c.Id,
        ChangeIdea = c.ChangeIdea,
        TesterJson = c.TesterJson,
        Timeframe = c.Timeframe,
        Location = c.Location,
        ParticipantsJson = c.ParticipantsJson,
        LearningGoal = c.LearningGoal,
        PredictionsJson = c.PredictionsJson,
        Observations = c.Observations,
        StudyResults = c.StudyResults,
        StudyLearning = c.StudyLearning,
        ActPlan = c.ActPlan,
        Decision = c.Decision,
        Status = c.Status,
        CreatedAt = c.CreatedAt,
    };
}
