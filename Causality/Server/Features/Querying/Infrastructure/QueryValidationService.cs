using Causality.Shared.Features.Querying.Domain;
using Causality.Server.Features.Querying.Application;

namespace Causality.Server.Features.Querying.Infrastructure;

/// <summary>
/// Service for validating Abstract Queries against security guardrails
/// Implements ADR-0002 query guardrails
/// </summary>
public class QueryValidationService : IQueryValidationService
{
    private readonly QueryValidationConfiguration _config;
    private readonly ILogger<QueryValidationService> _logger;

    public QueryValidationService(
        QueryValidationConfiguration config,
        ILogger<QueryValidationService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<QueryValidationResult> ValidateAsync(AbstractQuery query, CancellationToken cancellationToken = default)
    {
        var result = new QueryValidationResult { IsValid = true };

        // Validate basic structure
        if (string.IsNullOrWhiteSpace(query.Entity))
        {
            result.Errors.Add("Entity name is required");
            result.IsValid = false;
        }

        // Get entity configuration
        if (!_config.EntityConfigurations.TryGetValue(query.Entity.ToLowerInvariant(), out var entityConfig))
        {
            result.Errors.Add($"Entity '{query.Entity}' is not allowed or does not exist");
            result.IsValid = false;
            return Task.FromResult(result);
        }

        // Validate filters
        ValidateFilters(query.Filters, entityConfig, result, 0);

        // Validate sorting
        ValidateSort(query.Sort, entityConfig, result);

        // Validate projection
        ValidateProjection(query.Select, entityConfig, result);

        // Validate pagination
        ValidatePagination(query.Page, result);

        // Log validation outcome
        if (result.IsValid)
        {
            _logger.LogDebug("Query validation passed for entity {Entity}", query.Entity);
        }
        else
        {
            _logger.LogWarning("Query validation failed for entity {Entity}: {Errors}",
                query.Entity, string.Join(", ", result.Errors));
        }

        return Task.FromResult(result);
    }

    private void ValidateFilters(List<FilterCondition> filters, EntityConfiguration entityConfig, 
        QueryValidationResult result, int depth)
    {
        // Check depth limit
        if (depth > _config.MaxDepth)
        {
            result.Errors.Add($"Maximum filter depth ({_config.MaxDepth}) exceeded");
            result.IsValid = false;
            return;
        }

        // Check node count limit
        if (CountFilterNodes(filters) > _config.MaxNodes)
        {
            result.Errors.Add($"Maximum filter nodes ({_config.MaxNodes}) exceeded");
            result.IsValid = false;
            return;
        }

        foreach (var filter in filters)
        {
            // Validate nested conditions
            if (filter.Conditions != null && filter.Conditions.Any())
            {
                ValidateFilters(filter.Conditions, entityConfig, result, depth + 1);
                continue;
            }

            // Validate field access
            if (!entityConfig.FilterableFields.Contains(filter.Field))
            {
                result.Errors.Add($"Field '{filter.Field}' is not allowed for filtering on entity '{entityConfig.Name}'");
                result.IsValid = false;
                continue;
            }

            // Validate operator
            if (!FilterOperators.SupportedOperators.Contains(filter.Operator))
            {
                result.Errors.Add($"Operator '{filter.Operator}' is not supported");
                result.IsValid = false;
                continue;
            }

            // Check if field supports this operator
            if (entityConfig.FieldOperators.TryGetValue(filter.Field, out var allowedOps) && 
                !allowedOps.Contains(filter.Operator))
            {
                result.Errors.Add($"Operator '{filter.Operator}' is not allowed for field '{filter.Field}'");
                result.IsValid = false;
                continue;
            }

            // Validate value requirements
            if (FilterOperators.ValueRequiredOperators.Contains(filter.Operator) && filter.Value == null)
            {
                result.Errors.Add($"Operator '{filter.Operator}' requires a value");
                result.IsValid = false;
            }

            if (FilterOperators.ValueNotRequiredOperators.Contains(filter.Operator) && filter.Value != null)
            {
                result.Errors.Add($"Operator '{filter.Operator}' should not have a value");
                result.IsValid = false;
            }
        }
    }

    private void ValidateSort(List<SortSpecification> sortSpecs, EntityConfiguration entityConfig, QueryValidationResult result)
    {
        foreach (var sort in sortSpecs)
        {
            if (!entityConfig.SortableFields.Contains(sort.Field))
            {
                result.Errors.Add($"Field '{sort.Field}' is not allowed for sorting on entity '{entityConfig.Name}'");
                result.IsValid = false;
            }

            if (sort.Direction != "asc" && sort.Direction != "desc")
            {
                result.Errors.Add($"Sort direction must be 'asc' or 'desc', got '{sort.Direction}'");
                result.IsValid = false;
            }
        }
    }

    private void ValidateProjection(List<string> selectedFields, EntityConfiguration entityConfig, QueryValidationResult result)
    {
        foreach (var field in selectedFields)
        {
            if (!entityConfig.SelectableFields.Contains(field))
            {
                result.Errors.Add($"Field '{field}' is not allowed for selection on entity '{entityConfig.Name}'");
                result.IsValid = false;
            }
        }
    }

    private void ValidatePagination(PageRequest? page, QueryValidationResult result)
    {
        if (page != null)
        {
            if (page.Size < 1 || page.Size > _config.MaxPageSize)
            {
                result.Errors.Add($"Page size must be between 1 and {_config.MaxPageSize}, got {page.Size}");
                result.IsValid = false;
            }
        }
    }

    private int CountFilterNodes(List<FilterCondition> filters)
    {
        var count = filters.Count;
        foreach (var filter in filters)
        {
            if (filter.Conditions != null)
            {
                count += CountFilterNodes(filter.Conditions);
            }
        }
        return count;
    }
}

/// <summary>
/// Configuration for query validation guardrails
/// </summary>
public class QueryValidationConfiguration
{
    public int MaxDepth { get; set; } = 4;
    public int MaxNodes { get; set; } = 200;
    public int MaxPageSize { get; set; } = 200;
    public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public Dictionary<string, EntityConfiguration> EntityConfigurations { get; set; } = new();
}

/// <summary>
/// Configuration for a specific entity's allowed operations
/// </summary>
public class EntityConfiguration
{
    public string Name { get; set; } = string.Empty;
    public HashSet<string> FilterableFields { get; set; } = new();
    public HashSet<string> SortableFields { get; set; } = new();
    public HashSet<string> SelectableFields { get; set; } = new();
    public Dictionary<string, HashSet<string>> FieldOperators { get; set; } = new();
}