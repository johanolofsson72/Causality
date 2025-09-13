using Microsoft.AspNetCore.Mvc;
using Causality.Shared.Features.Querying.Domain;
using Causality.Shared.DTOs;
using System.Diagnostics;

namespace Causality.Server.Features.Querying.Application;

/// <summary>
/// Controller for executing Abstract Queries (AQ)
/// </summary>
[ApiController]
[Route("api/query")]
public class QueryController : ControllerBase
{
    private readonly IQueryValidationService _validationService;
    private readonly IQueryTranslator _queryTranslator;
    private readonly ILogger<QueryController> _logger;

    public QueryController(
        IQueryValidationService validationService,
        IQueryTranslator queryTranslator,
        ILogger<QueryController> logger)
    {
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _queryTranslator = queryTranslator ?? throw new ArgumentNullException(nameof(queryTranslator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Execute an Abstract Query
    /// </summary>
    /// <param name="query">The abstract query to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Query results as DTOs</returns>
    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteQuery(
        [FromBody] AbstractQuery query, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var queryHash = ComputeQueryHash(query);

        try
        {
            _logger.LogInformation("Executing query for entity {Entity} with hash {QueryHash}", 
                query.Entity, queryHash);

            // Step 1: Validate the query against guardrails
            var validationResult = await _validationService.ValidateAsync(query, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Query validation failed for entity {Entity}: {Errors}",
                    query.Entity, string.Join(", ", validationResult.Errors));

                return BadRequest(new 
                { 
                    Error = "Query validation failed", 
                    Details = validationResult.Errors 
                });
            }

            // Step 2: Execute query based on entity type
            var result = query.Entity.ToLowerInvariant() switch
            {
                "user" => await ExecuteQueryForEntity<UserDto>(query, cancellationToken),
                "product" => await ExecuteQueryForEntity<ProductDto>(query, cancellationToken),
                _ => throw new ArgumentException($"Unsupported entity type: {query.Entity}")
            };

            stopwatch.Stop();

            // Step 3: Build response with metadata
            result.Meta.ElapsedMs = stopwatch.ElapsedMilliseconds;
            result.Meta.QueryHash = queryHash;

            _logger.LogInformation("Query executed successfully for entity {Entity} in {ElapsedMs}ms, returned {Count} items",
                query.Entity, stopwatch.ElapsedMilliseconds, result.Items.Count);

            return Ok(result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Error executing query for entity {Entity} with hash {QueryHash}", 
                query.Entity, queryHash);

            return StatusCode(500, new 
            { 
                Error = "Internal server error during query execution",
                QueryHash = queryHash,
                ElapsedMs = stopwatch.ElapsedMilliseconds
            });
        }
    }

    private async Task<QueryResponse<T>> ExecuteQueryForEntity<T>(
        AbstractQuery query, 
        CancellationToken cancellationToken) where T : class
    {
        // Translate Abstract Query to IQueryable and execute
        return await _queryTranslator.ExecuteAsync<T>(query, cancellationToken);
    }

    private string ComputeQueryHash(AbstractQuery query)
    {
        // Simple hash for now - in production, use a proper hashing algorithm
        var content = System.Text.Json.JsonSerializer.Serialize(query);
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hashBytes)[..12]; // Take first 12 characters
    }
}

/// <summary>
/// Service interface for validating Abstract Queries
/// </summary>
public interface IQueryValidationService
{
    Task<QueryValidationResult> ValidateAsync(AbstractQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service interface for translating and executing Abstract Queries
/// </summary>
public interface IQueryTranslator
{
    Task<QueryResponse<T>> ExecuteAsync<T>(AbstractQuery query, CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// Result of query validation
/// </summary>
public class QueryValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}