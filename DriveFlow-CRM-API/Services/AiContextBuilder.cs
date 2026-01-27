using System.Text.Json;
using DriveFlow_CRM_API.Models;
using DriveFlow_CRM_API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DriveFlow_CRM_API.Services;

/// <summary>
/// Builds comprehensive AI context for student chatbot interactions.
/// Aggregates driving progress data without any AI/LLM calls.
/// </summary>
public sealed class AiContextBuilder : IAiContextBuilder
{
    private readonly ApplicationDbContext _db;

    public AiContextBuilder(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public async Task<AiStudentContextResponse?> BuildStudentContextAsync(
        string studentId,
        int historySessions = 5,
        string language = "ro",
        CancellationToken cancellationToken = default)
    {
        // Validate historySessions parameter
        historySessions = Math.Clamp(historySessions, 1, 50);

        // Load student with school info
        var student = await _db.ApplicationUsers
            .Include(u => u.AutoSchool)
            .FirstOrDefaultAsync(u => u.Id == studentId, cancellationToken);

        if (student == null)
            return null;

        // Load all student files with related data (without ExamForm navigation – it belongs to ExamForm)
        var files = await _db.Files
            .Include(f => f.TeachingCategory)
                .ThenInclude(tc => tc!.License)
            .Include(f => f.Instructor)
            .Include(f => f.Vehicle)
            .Include(f => f.Appointments)
            .Where(f => f.StudentId == studentId)
            .ToListAsync(cancellationToken);

        // Load all session forms for student's appointments (separate query since Appointment -> SessionForm nav is not defined)
        var appointmentIds = files.SelectMany(f => f.Appointments).Select(a => a.AppointmentId).ToList();
        var sessionForms = await _db.SessionForms
            .Include(sf => sf.ExamForm)
                .ThenInclude(ef => ef.Items)
            .Where(sf => appointmentIds.Contains(sf.AppointmentId))
            .ToDictionaryAsync(sf => sf.AppointmentId, cancellationToken);

        // Build the context
        var context = BuildContext(student, files, sessionForms, historySessions);
        var systemPrompt = GetSystemPrompt(language);

        return new AiStudentContextResponse(
            GeneratedAt: DateTime.UtcNow,
            SystemPrompt: systemPrompt,
            Context: context
        );
    }

    private StudentContextDto BuildContext(
        ApplicationUser student,
        List<Models.File> files,
        Dictionary<int, SessionForm> sessionForms,
        int historySessions)
    {
        var now = DateTime.Now;
        var allSessionEvaluations = new List<(Models.File File, Appointment Appointment, SessionForm Form)>();

        // Collect all session data
        foreach (var file in files)
        {
            foreach (var appointment in file.Appointments)
            {
                if (sessionForms.TryGetValue(appointment.AppointmentId, out var form))
                {
                    allSessionEvaluations.Add((file, appointment, form));
                }
            }
        }

        // Order by date descending for recency
        allSessionEvaluations = allSessionEvaluations
            .OrderByDescending(x => x.Appointment.Date)
            .ThenByDescending(x => x.Form.CreatedAt)
            .ToList();

        // Build category progress
        var categories = new List<CategoryProgressDto>();
        var allMistakesAggregated = new Dictionary<string, (int totalCount, int sessionsAffected, int penaltyPoints)>();
        var categoriesWithoutSessions = new List<string>();
        var categoriesWithIncompleteData = new List<string>();

        foreach (var file in files)
        {
            var categoryProgress = BuildCategoryProgress(file, sessionForms, historySessions, now, allMistakesAggregated,
                categoriesWithoutSessions, categoriesWithIncompleteData);
            categories.Add(categoryProgress);
        }

        // Build overall progress
        var overallProgress = BuildOverallProgress(categories, allSessionEvaluations);

        // Build common mistakes (top 10 most frequent)
        var commonMistakes = allMistakesAggregated
            .OrderByDescending(m => m.Value.totalCount)
            .Take(10)
            .Select(m => new MistakeSummaryDto
            {
                Description = m.Key,
                TotalOccurrences = m.Value.totalCount,
                SessionsAffected = m.Value.sessionsAffected,
                Severity = GetSeverity(m.Value.penaltyPoints)
            })
            .ToList();

        // Determine strong skills and skills needing improvement
        var (strongSkills, skillsNeedingImprovement) = AnalyzeSkills(categories, allMistakesAggregated);

        // Build latest session highlights (top 5)
        var latestHighlights = allSessionEvaluations
            .Take(5)
            .Select(x => BuildSessionHighlight(x.File, x.Appointment, x.Form))
            .ToList();

        // Generate coaching notes
        var coachingNotes = GenerateCoachingNotes(categories, overallProgress, commonMistakes);

        // Build data availability info
        var dataAvailability = new DataAvailabilityDto
        {
            HasEnrollments = files.Any(),
            HasCompletedSessions = allSessionEvaluations.Any(),
            HasEvaluatedSessions = allSessionEvaluations.Any(x => x.Form.TotalPoints.HasValue),
            CategoriesWithoutSessions = categoriesWithoutSessions,
            CategoriesWithIncompleteData = categoriesWithIncompleteData,
            Warnings = BuildWarnings(files, allSessionEvaluations)
        };

        // Calculate totals
        var completedAppointments = files.SelectMany(f => f.Appointments)
            .Count(a => a.Date.Add(a.EndHour) < now);

        var firstSession = allSessionEvaluations.Any()
            ? DateOnly.FromDateTime(allSessionEvaluations.Min(x => x.Appointment.Date))
            : (DateOnly?)null;

        var lastSession = allSessionEvaluations.Any()
            ? DateOnly.FromDateTime(allSessionEvaluations.Max(x => x.Appointment.Date))
            : (DateOnly?)null;

        return new StudentContextDto
        {
            Student = new StudentSummaryDto
            {
                FullName = $"{student.FirstName} {student.LastName}".Trim(),
                Email = student.Email,
                SchoolName = student.AutoSchool?.Name,
                TotalEnrollments = files.Count,
                TotalCompletedSessions = completedAppointments,
                FirstSessionDate = firstSession,
                LastSessionDate = lastSession
            },
            Categories = categories,
            OverallProgress = overallProgress,
            CommonMistakes = commonMistakes,
            StrongSkills = strongSkills,
            SkillsNeedingImprovement = skillsNeedingImprovement,
            LatestSessionHighlights = latestHighlights,
            CoachingNotes = coachingNotes,
            DataAvailability = dataAvailability
        };
    }

    private CategoryProgressDto BuildCategoryProgress(
        Models.File file,
        Dictionary<int, SessionForm> sessionForms,
        int historySessions,
        DateTime now,
        Dictionary<string, (int totalCount, int sessionsAffected, int penaltyPoints)> globalMistakes,
        List<string> categoriesWithoutSessions,
        List<string> categoriesWithIncompleteData)
    {
        var categoryCode = file.TeachingCategory?.Code ?? "N/A";
        var licenseType = file.TeachingCategory?.License?.Type;

        var appointments = file.Appointments.ToList();
        var completedAppointments = appointments.Count(a => a.Date.Add(a.EndHour) < now);

        // Get sessions with forms
        var sessionsWithForms = appointments
            .Where(a => sessionForms.ContainsKey(a.AppointmentId))
            .OrderByDescending(a => a.Date)
            .ThenByDescending(a => sessionForms[a.AppointmentId].CreatedAt)
            .Take(historySessions)
            .ToList();

        if (!sessionsWithForms.Any())
        {
            categoriesWithoutSessions.Add(categoryCode);
        }

        var recentSessions = new List<SessionEvaluationDto>();
        var categoryMistakes = new Dictionary<string, (int totalCount, int sessionsAffected, int penaltyPoints)>();
        var pointsHistory = new List<int?>();

        foreach (var appointment in sessionsWithForms)
        {
            var form = sessionForms[appointment.AppointmentId];

            // Use the ExamForm linked directly from SessionForm for points & mistake metadata
            var examForm = form.ExamForm;
            var examItems = examForm?.Items?.ToDictionary(i => i.ItemId, i => i) ?? new();

            var mistakes = ParseMistakes(form.MistakesJson, examItems);

            recentSessions.Add(new SessionEvaluationDto
            {
                Date = DateOnly.FromDateTime(appointment.Date),
                TotalPoints = form.TotalPoints,
                MaxPoints = examForm?.MaxPoints ?? 21,
                Result = form.Result,
                Mistakes = mistakes
            });

            pointsHistory.Add(form.TotalPoints);

            // Aggregate mistakes
            foreach (var mistake in mistakes)
            {
                if (!categoryMistakes.ContainsKey(mistake.Description))
                {
                    categoryMistakes[mistake.Description] = (0, 0, mistake.PenaltyPoints);
                }
                var existing = categoryMistakes[mistake.Description];
                categoryMistakes[mistake.Description] = (
                    existing.totalCount + mistake.Count,
                    existing.sessionsAffected + 1,
                    mistake.PenaltyPoints
                );

                // Add to global mistakes
                if (!globalMistakes.ContainsKey(mistake.Description))
                {
                    globalMistakes[mistake.Description] = (0, 0, mistake.PenaltyPoints);
                }
                var globalExisting = globalMistakes[mistake.Description];
                globalMistakes[mistake.Description] = (
                    globalExisting.totalCount + mistake.Count,
                    globalExisting.sessionsAffected + 1,
                    mistake.PenaltyPoints
                );
            }
        }

        // Check for incomplete data
        if (sessionsWithForms.Any() && sessionsWithForms.Any(s => sessionForms[s.AppointmentId].TotalPoints == null))
        {
            categoriesWithIncompleteData.Add(categoryCode);
        }

        // Calculate trend
        var trend = CalculateTrend(pointsHistory);

        // Calculate statistics
        var evaluatedPoints = pointsHistory.Where(p => p.HasValue).Select(p => p!.Value).ToList();
        var averagePoints = evaluatedPoints.Any() ? evaluatedPoints.Average() : (double?)null;

        var results = sessionsWithForms
            .Where(s => sessionForms[s.AppointmentId].Result != null)
            .Select(s => sessionForms[s.AppointmentId].Result)
            .ToList();
        var passRate = results.Any()
            ? (double)results.Count(r => r == "OK") / results.Count * 100
            : (double?)null;

        // Top mistakes for this category
        var topMistakes = categoryMistakes
            .OrderByDescending(m => m.Value.totalCount)
            .Take(5)
            .Select(m => new MistakeSummaryDto
            {
                Description = m.Key,
                TotalOccurrences = m.Value.totalCount,
                SessionsAffected = m.Value.sessionsAffected,
                Severity = GetSeverity(m.Value.penaltyPoints)
            })
            .ToList();

        // Build vehicle info string
        string? vehicleInfo = null;
        if (file.Vehicle != null)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(file.Vehicle.Brand))
                parts.Add(file.Vehicle.Brand);
            if (!string.IsNullOrEmpty(file.Vehicle.Model))
                parts.Add(file.Vehicle.Model);
            if (file.Vehicle.YearOfProduction.HasValue)
                parts.Add($"({file.Vehicle.YearOfProduction})");
            vehicleInfo = parts.Any() ? string.Join(" ", parts) : null;
        }

        // Determine max exam points for this category from any evaluated session
        int? maxExamPoints = sessionsWithForms
            .Select(a => sessionForms[a.AppointmentId].ExamForm?.MaxPoints)
            .FirstOrDefault(mp => mp.HasValue);

        return new CategoryProgressDto
        {
            CategoryCode = categoryCode,
            LicenseType = licenseType,
            Status = file.Status.ToString().ToLower(),
            InstructorName = file.Instructor != null
                ? $"{file.Instructor.FirstName} {file.Instructor.LastName}".Trim()
                : null,
            VehicleInfo = vehicleInfo,
            TransmissionType = file.Vehicle?.TransmissionType.ToString(),
            StartDate = file.ScholarshipStartDate.HasValue
                ? DateOnly.FromDateTime(file.ScholarshipStartDate.Value)
                : null,
            TotalAppointments = appointments.Count,
            CompletedAppointments = completedAppointments,
            EvaluatedSessions = sessionsWithForms.Count(s => sessionForms[s.AppointmentId].TotalPoints != null),
            MinRequiredLessons = file.TeachingCategory?.MinDrivingLessonsReq ?? 0,
            SessionCost = file.TeachingCategory?.SessionCost ?? 0,
            SessionDurationMinutes = file.TeachingCategory?.SessionDuration ?? 0,
            RecentSessions = recentSessions,
            Trend = trend,
            AveragePenaltyPoints = averagePoints,
            MaxExamPoints = maxExamPoints,
            PassRate = passRate,
            TopMistakes = topMistakes
        };
    }

    private List<MistakeDetailDto> ParseMistakes(string mistakesJson, Dictionary<int, ExamItem> examItems)
    {
        var result = new List<MistakeDetailDto>();

        try
        {
            var mistakes = JsonSerializer.Deserialize<List<MistakeEntry>>(mistakesJson);
            if (mistakes == null) return result;

            foreach (var mistake in mistakes)
            {
                if (examItems.TryGetValue(mistake.id_item, out var item))
                {
                    result.Add(new MistakeDetailDto
                    {
                        Description = item.Description,
                        Count = mistake.count,
                        PenaltyPoints = item.PenaltyPoints,
                        TotalPenalty = mistake.count * item.PenaltyPoints
                    });
                }
            }
        }
        catch
        {
            // If JSON parsing fails, return empty list
        }

        return result;
    }

    private string CalculateTrend(List<int?> pointsHistory)
    {
        var validPoints = pointsHistory.Where(p => p.HasValue).Select(p => p!.Value).ToList();

        if (validPoints.Count < 3)
            return "insufficient_data";

        // Calculate simple linear trend (lower points = better performance)
        // Compare first half average vs second half average
        var halfIndex = validPoints.Count / 2;
        var firstHalf = validPoints.Take(halfIndex).ToList();
        var secondHalf = validPoints.Skip(halfIndex).ToList();

        if (!firstHalf.Any() || !secondHalf.Any())
            return "insufficient_data";

        var firstHalfAvg = firstHalf.Average();
        var secondHalfAvg = secondHalf.Average();

        // Note: In recent-first order, secondHalf is older, firstHalf is newer
        // Lower points = better, so if firstHalfAvg < secondHalfAvg, improving
        var difference = secondHalfAvg - firstHalfAvg;
        var percentageChange = secondHalfAvg != 0 ? (difference / secondHalfAvg) * 100 : 0;

        if (percentageChange > 10)
            return "improving";
        else if (percentageChange < -10)
            return "declining";
        else
            return "stable";
    }

    private OverallProgressDto BuildOverallProgress(
        List<CategoryProgressDto> categories,
        List<(Models.File File, Appointment Appointment, SessionForm Form)> allSessions)
    {
        var totalSessions = allSessions.Count;
        var evaluatedSessions = allSessions.Count(s => s.Form.TotalPoints.HasValue);

        var allPoints = allSessions
            .Where(s => s.Form.TotalPoints.HasValue)
            .Select(s => s.Form.TotalPoints!.Value)
            .ToList();

        var avgPoints = allPoints.Any() ? allPoints.Average() : (double?)null;

        var allResults = allSessions
            .Where(s => !string.IsNullOrEmpty(s.Form.Result))
            .Select(s => s.Form.Result!)
            .ToList();

        var passRate = allResults.Any()
            ? (double)allResults.Count(r => r == "OK") / allResults.Count * 100
            : (double?)null;

        var improvingCount = categories.Count(c => c.Trend == "improving");
        var decliningCount = categories.Count(c => c.Trend == "declining");

        // Calculate overall trend
        string overallTrend = "insufficient_data";
        if (categories.Any(c => c.Trend != "insufficient_data"))
        {
            if (improvingCount > decliningCount)
                overallTrend = "improving";
            else if (decliningCount > improvingCount)
                overallTrend = "declining";
            else if (improvingCount == decliningCount && improvingCount > 0)
                overallTrend = "stable";
        }

        var distinctMistakes = categories.SelectMany(c => c.TopMistakes).Select(m => m.Description).Distinct().Count();

        return new OverallProgressDto
        {
            TotalSessions = totalSessions,
            TotalEvaluatedSessions = evaluatedSessions,
            OverallPassRate = passRate.HasValue ? Math.Round(passRate.Value, 1) : null,
            AveragePenaltyPoints = avgPoints.HasValue ? Math.Round(avgPoints.Value, 1) : null,
            OverallTrend = overallTrend,
            CategoriesImproving = improvingCount,
            CategoriesDeclining = decliningCount,
            TotalDistinctMistakes = distinctMistakes,
            ImprovementAreas = categories
                .Where(c => c.Trend == "improving")
                .Select(c => c.CategoryCode)
                .ToList()
        };
    }

    private (List<string> strong, List<string> needsImprovement) AnalyzeSkills(
        List<CategoryProgressDto> categories,
        Dictionary<string, (int totalCount, int sessionsAffected, int penaltyPoints)> mistakes)
    {
        var strong = new List<string>();
        var needsImprovement = new List<string>();

        // Categories with high pass rate are strong
        foreach (var cat in categories.Where(c => c.PassRate >= 80))
        {
            strong.Add($"Categoria {cat.CategoryCode} - rată de promovare ridicată");
        }

        // Categories showing improvement
        foreach (var cat in categories.Where(c => c.Trend == "improving"))
        {
            strong.Add($"Categoria {cat.CategoryCode} - progres constant");
        }

        // Low penalty point averages
        foreach (var cat in categories.Where(c => c.AveragePenaltyPoints < 10 && c.AveragePenaltyPoints.HasValue))
        {
            strong.Add($"Categoria {cat.CategoryCode} - puține puncte de penalizare");
        }

        // Frequent mistakes need improvement
        var frequentMistakes = mistakes
            .Where(m => m.Value.totalCount >= 3 || m.Value.sessionsAffected >= 2)
            .OrderByDescending(m => m.Value.totalCount)
            .Take(5);

        foreach (var mistake in frequentMistakes)
        {
            needsImprovement.Add(mistake.Key);
        }

        // Categories with declining trend
        foreach (var cat in categories.Where(c => c.Trend == "declining"))
        {
            needsImprovement.Add($"Categoria {cat.CategoryCode} - tendință descendentă");
        }

        // Low pass rate categories
        foreach (var cat in categories.Where(c => c.PassRate < 50 && c.PassRate.HasValue))
        {
            needsImprovement.Add($"Categoria {cat.CategoryCode} - rată de promovare scăzută");
        }

        return (strong.Distinct().ToList(), needsImprovement.Distinct().ToList());
    }

    private SessionHighlightDto BuildSessionHighlight(Models.File file, Appointment appointment, SessionForm form, Dictionary<int, ExamItem>? examItemsOverride = null)
    {
        var categoryCode = file.TeachingCategory?.Code ?? "N/A";
        // Prefer the ExamForm attached to the SessionForm for max points
        var maxPoints = form.ExamForm?.MaxPoints ?? 21;
        var passed = form.Result == "OK";

        // Parse mistakes for top mistake
        string? topMistake = null;
        var examItems = examItemsOverride
                        ?? (form.ExamForm?.Items?.ToDictionary(i => i.ItemId, i => i)
                            ?? new Dictionary<int, ExamItem>());
        var mistakes = ParseMistakes(form.MistakesJson, examItems);
        if (mistakes.Any())
        {
            topMistake = mistakes.OrderByDescending(m => m.TotalPenalty).First().Description;
        }

        // Build summary
        string summary;
        if (!form.TotalPoints.HasValue)
        {
            summary = "Sesiune fără evaluare completă";
        }
        else if (passed)
        {
            summary = form.TotalPoints.Value == 0
                ? "Sesiune perfectă - fără greșeli"
                : $"Sesiune reușită cu {form.TotalPoints.Value} puncte penalizare";
        }
        else
        {
            summary = $"Sesiune nereușită - {form.TotalPoints.Value} puncte penalizare din {maxPoints} maxim";
        }

        return new SessionHighlightDto
        {
            CategoryCode = categoryCode,
            Date = DateOnly.FromDateTime(appointment.Date),
            Summary = summary,
            Passed = form.Result != null ? passed : null,
            PenaltyPoints = form.TotalPoints,
            MaxPoints = maxPoints,
            TopMistake = topMistake
        };
    }

    private List<string> GenerateCoachingNotes(
        List<CategoryProgressDto> categories,
        OverallProgressDto overall,
        List<MistakeSummaryDto> commonMistakes)
    {
        var notes = new List<string>();

        // Overall status
        if (!categories.Any())
        {
            notes.Add("Elevul nu are încă dosare active.");
            return notes;
        }

        if (overall.TotalSessions == 0)
        {
            notes.Add("Elevul nu a avut încă sesiuni de conducere.");
            return notes;
        }

        // Progress summary
        if (overall.TotalEvaluatedSessions > 0)
        {
            notes.Add($"Total {overall.TotalEvaluatedSessions} sesiuni evaluate din {overall.TotalSessions} completate.");
        }

        // Pass rate observation
        if (overall.OverallPassRate.HasValue)
        {
            if (overall.OverallPassRate >= 80)
                notes.Add($"Rata de promovare excelentă: {overall.OverallPassRate}%");
            else if (overall.OverallPassRate >= 50)
                notes.Add($"Rata de promovare moderată: {overall.OverallPassRate}% - potențial de îmbunătățire");
            else
                notes.Add($"Rata de promovare scăzută: {overall.OverallPassRate}% - necesită atenție sporită");
        }

        // Trend observation
        if (overall.OverallTrend == "improving")
            notes.Add("Tendință generală pozitivă - elevul face progrese.");
        else if (overall.OverallTrend == "declining")
            notes.Add("Tendință descendentă - recomandare pentru sesiuni de recapitulare.");
        else if (overall.OverallTrend == "stable")
            notes.Add("Performanță stabilă - menține nivelul actual.");

        // Top mistakes to focus on
        if (commonMistakes.Any())
        {
            var topMistake = commonMistakes.First();
            notes.Add($"Cea mai frecventă greșeală: \"{topMistake.Description}\" ({topMistake.TotalOccurrences} apariții).");
        }

        // Category-specific notes
        foreach (var cat in categories.Where(c => c.Trend == "declining"))
        {
            notes.Add($"Atenție la categoria {cat.CategoryCode} - performanță în declin.");
        }

        foreach (var cat in categories.Where(c => c.EvaluatedSessions > 0 && c.AveragePenaltyPoints > 15))
        {
            notes.Add($"Categoria {cat.CategoryCode} are media punctelor de penalizare ridicată ({cat.AveragePenaltyPoints:F1}).");
        }

        // Lesson requirements check
        foreach (var cat in categories)
        {
            if (cat.MinRequiredLessons > 0 && cat.CompletedAppointments < cat.MinRequiredLessons)
            {
                var remaining = cat.MinRequiredLessons - cat.CompletedAppointments;
                notes.Add($"Categoria {cat.CategoryCode}: mai sunt necesare {remaining} lecții pentru îndeplinirea cerinței minime.");
            }
        }

        return notes;
    }

    private List<string> BuildWarnings(
        List<Models.File> files,
        List<(Models.File File, Appointment Appointment, SessionForm Form)> sessions)
    {
        var warnings = new List<string>();

        if (!files.Any())
        {
            warnings.Add("Nu există dosare active pentru acest elev.");
            return warnings;
        }

        if (!sessions.Any())
        {
            warnings.Add("Nu există sesiuni de conducere înregistrate.");
        }
        else if (!sessions.Any(s => s.Form.TotalPoints.HasValue))
        {
            warnings.Add("Nicio sesiune nu are evaluare completă.");
        }

        var expiredFiles = files.Where(f => f.Status == FileStatus.EXPIRED);
        foreach (var file in expiredFiles)
        {
            warnings.Add($"Dosarul pentru categoria {file.TeachingCategory?.Code ?? "N/A"} a expirat.");
        }

        return warnings;
    }

    private static string GetSeverity(int penaltyPoints)
    {
        return penaltyPoints switch
        {
            <= 2 => "low",
            <= 5 => "medium",
            _ => "high"
        };
    }

    /// <summary>
    /// Returns the system prompt for the AI assistant based on language.
    /// </summary>
    private static string GetSystemPrompt(string language)
    {
        return language?.ToLower() switch
        {
            "en" => GetEnglishSystemPrompt(),
            _ => GetRomanianSystemPrompt()
        };
    }

    private static string GetRomanianSystemPrompt()
    {
        return """
            Ești un asistent virtual pentru un elev la școala de șoferi.

            Scopul tău este să sprijini elevul să înțeleagă progresul său,
            să își recunoască punctele forte și să își îmbunătățească
            abilitățile de conducere.

            Reguli obligatorii:
            - Folosește EXCLUSIV informațiile din contextul furnizat.
            - Nu inventa date, scoruri sau evaluări.
            - Dacă informația nu există în context, spune clar acest lucru.
            - Răspunde ca un instructor calm, empatic și constructiv.
            - Oferă sfaturi practice și concrete.
            - Nu menționa baze de date, API-uri sau sisteme interne.
            - Nu menționa că ești un model AI sau un LLM.
            - Vorbește la persoana a doua (tu) cu elevul.
            - Folosește un ton încurajator dar realist.

            Contextul de mai jos reprezintă istoricul real al elevului.
            Analizează datele și oferă răspunsuri personalizate bazate pe performanța reală.

            Când elevul întreabă despre progresul său:
            - Menționează tendințele (îmbunătățire/stagnare/declin)
            - Evidențiază punctele forte specifice
            - Sugerează zone concrete de îmbunătățire
            - Oferă sfaturi practice pentru greșelile frecvente
            """;
    }

    private static string GetEnglishSystemPrompt()
    {
        return """
            You are a virtual assistant for a driving school student.

            Your purpose is to help the student understand their progress,
            recognize their strengths, and improve their driving skills.

            Mandatory rules:
            - Use ONLY the information from the provided context.
            - Do not invent data, scores, or evaluations.
            - If information is not in the context, clearly state this.
            - Respond as a calm, empathetic, and constructive instructor.
            - Offer practical and concrete advice.
            - Do not mention databases, APIs, or internal systems.
            - Do not mention that you are an AI model or LLM.
            - Address the student directly using "you".
            - Use an encouraging but realistic tone.

            The context below represents the student's real history.
            Analyze the data and provide personalized responses based on actual performance.

            When the student asks about their progress:
            - Mention trends (improving/stagnating/declining)
            - Highlight specific strengths
            - Suggest concrete areas for improvement
            - Offer practical tips for frequent mistakes
            """;
    }
}

/// <summary>
/// Internal class for deserializing mistake entries from JSON.
/// </summary>
file sealed class MistakeEntry
{
    public int id_item { get; set; }
    public int count { get; set; }
}
