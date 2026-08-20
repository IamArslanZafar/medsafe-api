-- Seeds a couple of starter Sections for every Unit/Department that has none yet,
-- so the "Section" dropdown on the reporting form isn't empty ("No data") for any
-- unit other than Medical Ward (which already has "bay23"). Placeholder names —
-- editable/renamable any time via Configurations > Sections.
--
-- Purely additive — no existing rows touched. Idempotent: only inserts for a unit
-- when it currently has zero Section rows, so safe to run more than once.

USE [db_acd20d_medsafe001]
GO

DECLARE @Now DATETIME2 = SYSUTCDATETIME();

DECLARE @Units TABLE (UnitDepartmentId INT, SectionName NVARCHAR(200));
INSERT INTO @Units (UnitDepartmentId, SectionName)
SELECT ud.Id, s.Name
FROM dbo.UnitDepartment ud
CROSS APPLY (VALUES
    (CASE ud.Name
        WHEN 'Emergency Department (ED)' THEN 'Triage'
        WHEN 'Intensive Care Unit (ICU)' THEN 'Bay 1'
        WHEN 'Ward A' THEN 'Bay 1'
        WHEN 'Cardiology Ward' THEN 'Bay 1'
        WHEN 'Surgical Ward' THEN 'Bay 1'
        WHEN 'Pharmacy' THEN 'Dispensing Counter'
        WHEN 'Outpatient Clinic' THEN 'Clinic Room 1'
        WHEN 'Other' THEN 'General'
        ELSE 'Bay 1'
    END)
) AS s(Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Section sec WHERE sec.UnitDepartmentId = ud.Id)

UNION ALL

SELECT ud.Id, s.Name
FROM dbo.UnitDepartment ud
CROSS APPLY (VALUES
    (CASE ud.Name
        WHEN 'Emergency Department (ED)' THEN 'Resuscitation Bay'
        WHEN 'Intensive Care Unit (ICU)' THEN 'Bay 2'
        WHEN 'Ward A' THEN 'Bay 2'
        WHEN 'Cardiology Ward' THEN 'CCU Bay'
        WHEN 'Surgical Ward' THEN 'Pre-Op Bay'
        WHEN 'Pharmacy' THEN 'Compounding Room'
        WHEN 'Outpatient Clinic' THEN 'Clinic Room 2'
        WHEN 'Other' THEN NULL
        ELSE 'Bay 2'
    END)
) AS s(Name)
WHERE s.Name IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.Section sec WHERE sec.UnitDepartmentId = ud.Id);

INSERT INTO dbo.Section (Name, Description, UnitDepartmentId, IsActive, DisplayOrder, CreatedBy, CreatedDate)
SELECT
    u.SectionName,
    NULL,
    u.UnitDepartmentId,
    1,
    ISNULL((SELECT MAX(DisplayOrder) FROM dbo.Section sec WHERE sec.UnitDepartmentId = u.UnitDepartmentId), 0)
        + ROW_NUMBER() OVER (PARTITION BY u.UnitDepartmentId ORDER BY (SELECT NULL)),
    1,
    @Now
FROM @Units u;
GO

-- Verify
SELECT ud.Name AS Unit, sec.Name AS Section, sec.DisplayOrder
FROM dbo.UnitDepartment ud
LEFT JOIN dbo.Section sec ON sec.UnitDepartmentId = ud.Id
ORDER BY ud.Name, sec.DisplayOrder;
GO
