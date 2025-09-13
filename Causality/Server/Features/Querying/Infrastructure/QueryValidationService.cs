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

        // Validate advanced operations
        ValidateOperations(query.Operations, entityConfig, result);

        // Validate GroupBy
        ValidateGroupBy(query.GroupBy, entityConfig, result);

        // Validate aggregations
        ValidateAggregations(query.Aggregations, entityConfig, result);

        // Validate joins
        ValidateJoins(query.Joins, result);

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

    private void ValidateOperations(List<QueryOperation> operations, EntityConfiguration entityConfig, QueryValidationResult result)
    {
        foreach (var operation in operations)
        {
            // Validate operation type is supported
            if (!FilterOperators.SupportedOperators.Contains(operation.Type))
            {
                result.Errors.Add($"Operation '{operation.Type}' is not supported");
                result.IsValid = false;
                continue;
            }

            // Validate specific operation requirements
            ValidateSpecificOperation(operation, entityConfig, result);
        }
    }

    private void ValidateSpecificOperation(QueryOperation operation, EntityConfiguration entityConfig, QueryValidationResult result)
    {
        switch (operation.Type)
        {
            case FilterOperators.SelectMany:
                if (!operation.Parameters.TryGetValue("field", out var field) || field == null)
                {
                    result.Errors.Add("SelectMany operation requires a 'field' parameter");
                    result.IsValid = false;
                }
                else if (!entityConfig.SelectableFields.Contains(field.ToString()!))
                {
                    result.Errors.Add($"Field '{field}' is not allowed for SelectMany operation");
                    result.IsValid = false;
                }
                break;

            case FilterOperators.Skip:
            case FilterOperators.Take:
                if (!operation.Parameters.TryGetValue("count", out var countObj) || 
                    countObj is not int count || count < 0)
                {
                    result.Errors.Add($"{operation.Type} operation requires a valid positive 'count' parameter");
                    result.IsValid = false;
                }
                else if (operation.Type == FilterOperators.Take && count > _config.MaxPageSize)
                {
                    result.Errors.Add($"Take count cannot exceed {_config.MaxPageSize}");
                    result.IsValid = false;
                }
                break;

            case FilterOperators.Any:
            case FilterOperators.All:
                if (!operation.Parameters.TryGetValue("collection", out var collection) || collection == null)
                {
                    result.Errors.Add($"{operation.Type} operation requires a 'collection' parameter");
                    result.IsValid = false;
                }
                break;
        }
    }

    private void ValidateGroupBy(GroupBySpecification? groupBy, EntityConfiguration entityConfig, QueryValidationResult result)
    {
        if (groupBy == null) return;

        foreach (var field in groupBy.Fields)
        {
            if (!entityConfig.SelectableFields.Contains(field))
            {
                result.Errors.Add($"Field '{field}' is not allowed for GroupBy on entity '{entityConfig.Name}'");
                result.IsValid = false;
            }
        }

        // Validate Having conditions
        ValidateFilters(groupBy.Having, entityConfig, result, 0);
    }

    private void ValidateAggregations(List<AggregationSpecification> aggregations, EntityConfiguration entityConfig, QueryValidationResult result)
    {
        var allowedFunctions = new[] { FilterOperators.Count, FilterOperators.Sum, FilterOperators.Average, FilterOperators.Min, FilterOperators.Max };

        foreach (var agg in aggregations)
        {
            if (!allowedFunctions.Contains(agg.Function))
            {
                result.Errors.Add($"Aggregation function '{agg.Function}' is not supported");
                result.IsValid = false;
                continue;
            }

            // Count doesn't require a field, others do
            if (agg.Function != FilterOperators.Count)
            {
                if (string.IsNullOrEmpty(agg.Field))
                {
                    result.Errors.Add($"Aggregation function '{agg.Function}' requires a field");
                    result.IsValid = false;
                }
                else if (!entityConfig.SelectableFields.Contains(agg.Field))
                {
                    result.Errors.Add($"Field '{agg.Field}' is not allowed for aggregation on entity '{entityConfig.Name}'");
                    result.IsValid = false;
                }
            }
        }
    }

    private void ValidateJoins(List<JoinSpecification> joins, QueryValidationResult result)
    {
        foreach (var join in joins)
        {
            if (string.IsNullOrEmpty(join.Entity))
            {
                result.Errors.Add("Join operation requires an entity name");
                result.IsValid = false;
            }

            if (!join.On.Any())
            {
                result.Errors.Add("Join operation requires at least one join condition");
                result.IsValid = false;
            }

            var allowedJoinTypes = new[] { FilterOperators.Join, FilterOperators.GroupJoin };
            if (!allowedJoinTypes.Contains(join.Type))
            {
                result.Errors.Add($"Join type '{join.Type}' is not supported");
                result.IsValid = false;
            }
        }
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