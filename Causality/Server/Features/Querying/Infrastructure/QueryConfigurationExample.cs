using Causality.Shared.Features.Querying.Domain;

namespace Causality.Server.Features.Querying.Infrastructure;

/// <summary>
/// Example configuration for query validation and entity whitelists
/// Shows how to configure the security guardrails per ADR-0002
/// </summary>
public static class QueryConfigurationExample
{
    /// <summary>
    /// Create example query validation configuration
    /// </summary>
    public static QueryValidationConfiguration CreateConfiguration()
    {
        var config = new QueryValidationConfiguration
        {
            MaxDepth = 4,
            MaxNodes = 200,
            MaxPageSize = 200,
            ExecutionTimeout = TimeSpan.FromSeconds(5),
            EntityConfigurations = new Dictionary<string, EntityConfiguration>()
        };

        // Configure User entity
        config.EntityConfigurations["user"] = CreateUserEntityConfiguration();
        
        // Configure Product entity
        config.EntityConfigurations["product"] = CreateProductEntityConfiguration();

        return config;
    }

    private static EntityConfiguration CreateUserEntityConfiguration()
    {
        return new EntityConfiguration
        {
            Name = "User",
            
            // Fields that can be used in WHERE clauses
            FilterableFields = new HashSet<string>
            {
                "Id", "Name", "Email", "CreatedAt", "LastLoginAt", "IsActive"
                // Note: Sensitive fields like PasswordHash are NOT included
            },
            
            // Fields that can be used in ORDER BY clauses
            SortableFields = new HashSet<string>
            {
                "Id", "Name", "Email", "CreatedAt", "LastLoginAt", "IsActive"
            },
            
            // Fields that can be included in SELECT projection
            SelectableFields = new HashSet<string>
            {
                "Id", "Name", "Email", "CreatedAt", "LastLoginAt", "IsActive"
                // Again, no sensitive internal fields
            },
            
            // Specific operators allowed per field
            FieldOperators = new Dictionary<string, HashSet<string>>
            {
                ["Id"] = new HashSet<string> 
                { 
                    FilterOperators.Equal, FilterOperators.NotEquals, 
                    FilterOperators.In, FilterOperators.LessThan, FilterOperators.GreaterThan 
                },
                ["Name"] = new HashSet<string> 
                { 
                    FilterOperators.Equal, FilterOperators.NotEquals, 
                    FilterOperators.Contains, FilterOperators.StartsWith, FilterOperators.EndsWith,
                    FilterOperators.EqualsIgnoreCase, FilterOperators.IsNull, FilterOperators.IsNotNull
                },
                ["Email"] = new HashSet<string> 
                { 
                    FilterOperators.Equal, FilterOperators.NotEquals,
                    FilterOperators.Contains, FilterOperators.StartsWith, FilterOperators.EndsWith,
                    FilterOperators.EqualsIgnoreCase, FilterOperators.IsNull, FilterOperators.IsNotNull
                },
                ["CreatedAt"] = new HashSet<string> 
                { 
                    FilterOperators.Equal, FilterOperators.NotEquals,
                    FilterOperators.LessThan, FilterOperators.LessThanOrEqual,
                    FilterOperators.GreaterThan, FilterOperators.GreaterThanOrEqual,
                    FilterOperators.IsNull, FilterOperators.IsNotNull
                },
                ["LastLoginAt"] = new HashSet<string> 
                { 
                    FilterOperators.Equal, FilterOperators.NotEquals,
                    FilterOperators.LessThan, FilterOperators.LessThanOrEqual,
                    FilterOperators.GreaterThan, FilterOperators.GreaterThanOrEqual,
                    FilterOperators.IsNull, FilterOperators.IsNotNull
                },
                ["IsActive"] = new HashSet<string> 
                { 
                    FilterOperators.Equal, FilterOperators.NotEquals 
                }
            }
        };
    }

    private static EntityConfiguration CreateProductEntityConfiguration()
    {
        return new EntityConfiguration
        {
            Name = "Product",
            
            FilterableFields = new HashSet<string>
            {
                "Id", "Name", "Brand", "Price", "Category", "CreatedAt", "IsAvailable"
                // Note: CostPrice and InternalSku are NOT exposed for security
            },
            
            SortableFields = new HashSet<string>
            {
                "Id", "Name", "Brand", "Price", "Category", "CreatedAt", "IsAvailable"
            },
            
            SelectableFields = new HashSet<string>
            {
                "Id", "Name", "Brand", "Price", "Category", "CreatedAt", "IsAvailable", "Description"
            },
            
            FieldOperators = new Dictionary<string, HashSet<string>>
            {
                ["Id"] = new HashSet<string> 
                { 
                    FilterOperators.Equal, FilterOperators.NotEquals, FilterOperators.In,
                    FilterOperators.LessThan, FilterOperators.GreaterThan 
                },
                ["Name"] = new HashSet<string> 
                { 
                    FilterOperators.Equal, FilterOperators.NotEquals,
                    FilterOperators.Contains, FilterOperators.StartsWith, FilterOperators.EndsWith,
                    FilterOperators.EqualsIgnoreCase, FilterOperators.IsNull, FilterOperators.IsNotNull
                },
                ["Brand"] = new HashSet<string> 
                { 
                    FilterOperators.Equal, FilterOperators.NotEquals, FilterOperators.In,
                    FilterOperators.Contains, FilterOperators.StartsWith, FilterOperators.EndsWith,
                    FilterOperators.EqualsIgnoreCase
                },
                ["Price"] = new HashSet<string> 
                { 
                    FilterOperators.Equal, FilterOperators.NotEquals,
                    FilterOperators.LessThan, FilterOperators.LessThanOrEqual,
                    FilterOperators.GreaterThan, FilterOperators.GreaterThanOrEqual,
                    FilterOperators.In
                },
                ["Category"] = new HashSet<string> 
                { 
                    FilterOperators.Equal, FilterOperators.NotEquals, FilterOperators.In,
                    FilterOperators.Contains, FilterOperators.EqualsIgnoreCase
                },
                ["CreatedAt"] = new HashSet<string> 
                { 
                    FilterOperators.Equal, FilterOperators.NotEquals,
                    FilterOperators.LessThan, FilterOperators.LessThanOrEqual,
                    FilterOperators.GreaterThan, FilterOperators.GreaterThanOrEqual
                },
                ["IsAvailable"] = new HashSet<string> 
                { 
                    FilterOperators.Equal, FilterOperators.NotEquals 
                }
            }
        };
    }

    /// <summary>
    /// Example of how to register services in DI container (for ASP.NET Core)
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddQueryInfrastructure(this IServiceCollection services)
        {
            // Register query validation configuration
            var config = CreateConfiguration();
            services.AddSingleton(config);
            
            // Register query services
            services.AddScoped<IQueryValidationService, QueryValidationService>();
            services.AddScoped<IQueryTranslator, QueryTranslator>();
            services.AddScoped<IProjectionMapProvider, ProjectionMapProvider>();
            services.AddScoped<ICursorPagingService, CursorPagingService>();
            
            return services;
        }
    }
}