-- Consolidates the "Other Dashboards" module data (Clinical Pharmacy
-- Intervention, CPD & Education, Quality Project Tracker) that the Main
-- Dashboard's Overall view needs for its Incident Trend chart, Review Status
-- card, and KPI counts into ONE stored procedure — same pattern already used
-- elsewhere for a consolidated endpoint (the SP does the aggregation, the
-- API layer is a thin pass-through) instead of the frontend making 3 separate
-- list calls and re-aggregating them client-side in JavaScript.
--
-- Idempotent — safe to run more than once (CREATE OR ALTER). No embedded
-- USE: run with `sqlcmd -d <database>` against each target (local dev DB
-- and live DB).

GO

CREATE OR ALTER PROCEDURE dbo.sp_GetModulesOverviewSummary
    @StartDate DATE,
    @EndDate   DATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IsHourly BIT = CASE WHEN @StartDate = @EndDate THEN 1 ELSE 0 END;
    DECLARE @RangeEndExclusive DATETIME2 = DATEADD(DAY, 1, CAST(@EndDate AS DATETIME2));
    DECLARE @RangeStart DATETIME2 = CAST(@StartDate AS DATETIME2);

    -- ── Bucket calendar — 24 hourly buckets (single-day range) or one bucket
    -- per calendar day in the range, same convention as DashboardService's
    -- own Incident Trend bucketing (h tt / dd MMM), same bucket count/order,
    -- so the frontend can align these counts to that trend's labels by index. ──
    IF OBJECT_ID('tempdb..#Buckets') IS NOT NULL DROP TABLE #Buckets;
    CREATE TABLE #Buckets (BucketIndex INT PRIMARY KEY, BucketLabel NVARCHAR(20));

    IF @IsHourly = 1
    BEGIN
        ;WITH Hours AS (
            SELECT 0 AS h
            UNION ALL SELECT h + 1 FROM Hours WHERE h < 23
        )
        INSERT INTO #Buckets (BucketIndex, BucketLabel)
        SELECT h, FORMAT(DATEADD(HOUR, h, CAST('2000-01-01' AS DATETIME2)), 'h tt', 'en-US')
        FROM Hours
        OPTION (MAXRECURSION 24);
    END
    ELSE
    BEGIN
        DECLARE @DayCount INT = DATEDIFF(DAY, @StartDate, @EndDate);
        ;WITH Days AS (
            SELECT 0 AS d
            UNION ALL SELECT d + 1 FROM Days WHERE d < @DayCount
        )
        INSERT INTO #Buckets (BucketIndex, BucketLabel)
        SELECT d, FORMAT(DATEADD(DAY, d, @RangeStart), 'dd MMM', 'en-US')
        FROM Days
        OPTION (MAXRECURSION 400);
    END

    -- ── Result set 1: totals for the whole range (KPI cards) ──
    SELECT
        (SELECT COUNT(*) FROM dbo.ClinicalPharmacyInterventions WHERE CreatedAt >= @RangeStart AND CreatedAt < @RangeEndExclusive) AS CpiCount,
        (SELECT COUNT(*) FROM dbo.CpdActivities                 WHERE CreatedAt >= @RangeStart AND CreatedAt < @RangeEndExclusive) AS CpdCount,
        (SELECT COUNT(*) FROM dbo.QualityProjects                WHERE CreatedAt >= @RangeStart AND CreatedAt < @RangeEndExclusive) AS QptCount;

    -- ── Result set 2: one row per bucket, in order (Incident Trend chart lines) ──
    SELECT
        b.BucketIndex,
        b.BucketLabel,
        (SELECT COUNT(*) FROM dbo.ClinicalPharmacyInterventions c
         WHERE (@IsHourly = 1 AND CAST(c.CreatedAt AS DATE) = @StartDate AND DATEPART(HOUR, c.CreatedAt) = b.BucketIndex)
            OR (@IsHourly = 0 AND CAST(c.CreatedAt AS DATE) = DATEADD(DAY, b.BucketIndex, @StartDate))) AS CpiCount,
        (SELECT COUNT(*) FROM dbo.CpdActivities a
         WHERE (@IsHourly = 1 AND CAST(a.CreatedAt AS DATE) = @StartDate AND DATEPART(HOUR, a.CreatedAt) = b.BucketIndex)
            OR (@IsHourly = 0 AND CAST(a.CreatedAt AS DATE) = DATEADD(DAY, b.BucketIndex, @StartDate))) AS CpdCount,
        (SELECT COUNT(*) FROM dbo.QualityProjects p
         WHERE (@IsHourly = 1 AND CAST(p.CreatedAt AS DATE) = @StartDate AND DATEPART(HOUR, p.CreatedAt) = b.BucketIndex)
            OR (@IsHourly = 0 AND CAST(p.CreatedAt AS DATE) = DATEADD(DAY, b.BucketIndex, @StartDate))) AS QptCount
    FROM #Buckets b
    ORDER BY b.BucketIndex;

    -- ── Result set 3: CPI status breakdown (Review Status card) ──
    SELECT DraftStatus AS StatusName, COUNT(*) AS StatusCount
    FROM dbo.ClinicalPharmacyInterventions
    WHERE CreatedAt >= @RangeStart AND CreatedAt < @RangeEndExclusive
    GROUP BY DraftStatus;

    -- ── Result set 4: CPD status breakdown ──
    SELECT [Status] AS StatusName, COUNT(*) AS StatusCount
    FROM dbo.CpdActivities
    WHERE CreatedAt >= @RangeStart AND CreatedAt < @RangeEndExclusive
    GROUP BY [Status];

    -- ── Result set 5: QPT status breakdown ──
    SELECT [Status] AS StatusName, COUNT(*) AS StatusCount
    FROM dbo.QualityProjects
    WHERE CreatedAt >= @RangeStart AND CreatedAt < @RangeEndExclusive
    GROUP BY [Status];

    DROP TABLE #Buckets;
END
GO
