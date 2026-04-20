using Jobuler.Application.Scheduling;
using Jobuler.Application.Scheduling.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jobuler.Infrastructure.Scheduling;

public class SolverHttpClient : ISolverClient
{
    private readonly HttpClient _http;
    private readonly ILogger<SolverHttpClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SolverHttpClient(HttpClient http, ILogger<SolverHttpClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<SolverOutputDto> SolveAsync(SolverInputDto input, CancellationToken ct = default)
    {
        _logger.LogInformation("Calling solver: run_id={RunId} space_id={SpaceId}", input.RunId, input.SpaceId);

        var response = await _http.PostAsJsonAsync("/solve", input, JsonOptions, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SolverOutputDto>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Solver returned empty response.");

        _logger.LogInformation("Solver response: run_id={RunId} feasible={Feasible} timed_out={TimedOut}",
            result.RunId, result.Feasible, result.TimedOut);

        return result;
    }
}
