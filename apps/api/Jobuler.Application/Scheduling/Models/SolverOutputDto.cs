namespace Jobuler.Application.Scheduling.Models;

/// <summary>
/// Mirrors the Python solver's SolverOutput Pydantic model (solver_output.py).
/// Deserialized from the HTTP POST /solve response.
/// </summary>
public record SolverOutputDto(
    string RunId,
    bool Feasible,
    bool TimedOut,
    List<AssignmentResultDto> Assignments,
    List<string> UncoveredSlotIds,
    List<HardConflictDto> HardConflicts,
    double SoftPenaltyTotal,
    StabilityMetricsDto StabilityMetrics,
    List<FairnessMetricsDto> FairnessMetrics,
    List<string> ExplanationFragments);

public record AssignmentResultDto(string SlotId, string PersonId, string Source);

public record HardConflictDto(
    string ConstraintId,
    string RuleType,
    string Description,
    List<string> AffectedSlotIds,
    List<string> AffectedPersonIds);

public record StabilityMetricsDto(
    int TodayTomorrowChanges,
    int Days3To4Changes,
    int Days5To7Changes,
    double TotalStabilityPenalty);

public record FairnessMetricsDto(
    string PersonId,
    int HatedTasksAssigned,
    int DislikedTasksAssigned,
    int TotalAssigned);
