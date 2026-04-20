using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Jobuler.Application.Exports.Commands;

public record ExportScheduleCsvCommand(
    Guid SpaceId,
    Guid VersionId,
    Guid RequestingUserId) : IRequest<ExportCsvResult>;

public record ExportCsvResult(byte[] Content, string FileName);

public class ExportScheduleCsvCommandHandler
    : IRequestHandler<ExportScheduleCsvCommand, ExportCsvResult>
{
    private readonly AppDbContext _db;

    public ExportScheduleCsvCommandHandler(AppDbContext db) => _db = db;

    public async Task<ExportCsvResult> Handle(ExportScheduleCsvCommand req, CancellationToken ct)
    {
        // Verify version belongs to this space
        var version = await _db.ScheduleVersions.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == req.VersionId && v.SpaceId == req.SpaceId, ct)
            ?? throw new KeyNotFoundException("Schedule version not found.");

        var rows = await _db.Assignments.AsNoTracking()
            .Where(a => a.ScheduleVersionId == req.VersionId && a.SpaceId == req.SpaceId)
            .Join(_db.People.AsNoTracking(), a => a.PersonId, p => p.Id,
                (a, p) => new { a, PersonName = p.DisplayName ?? p.FullName })
            .Join(_db.TaskSlots.AsNoTracking(), x => x.a.TaskSlotId, s => s.Id,
                (x, s) => new { x.a, x.PersonName, Slot = s })
            .Join(_db.TaskTypes.AsNoTracking(), x => x.Slot.TaskTypeId, t => t.Id,
                (x, t) => new
                {
                    x.PersonName,
                    TaskName = t.Name,
                    BurdenLevel = t.BurdenLevel.ToString(),
                    x.Slot.StartsAt,
                    x.Slot.EndsAt,
                    x.Slot.Location,
                    Source = x.a.Source.ToString()
                })
            .OrderBy(r => r.StartsAt)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Person,Task,Burden Level,Starts At,Ends At,Location,Source");

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(row.PersonName),
                CsvEscape(row.TaskName),
                CsvEscape(row.BurdenLevel),
                row.StartsAt.ToString("yyyy-MM-dd HH:mm"),
                row.EndsAt.ToString("yyyy-MM-dd HH:mm"),
                CsvEscape(row.Location ?? ""),
                CsvEscape(row.Source)));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"schedule-v{version.VersionNumber}-{DateTime.UtcNow:yyyyMMdd}.csv";

        return new ExportCsvResult(bytes, fileName);
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
