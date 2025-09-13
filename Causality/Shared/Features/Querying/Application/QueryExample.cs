using System;
using Causality.Shared.Features.Querying.Domain;
using System.Text.Json;

namespace Causality.Shared.Features.Querying.Application;

/// <summary>
/// Example usage of the QueryBuilder and demonstration of the Abstract Query system
/// </summary>
public static class QueryExample
{
    /// <summary>
    /// Example: Build a query for active users created in the last month
    /// </summary>
    public static AbstractQuery BuildActiveUsersQuery()
    {
        var query = QueryBuilder
            .For("User")
            .Where("IsActive", true)
            .GreaterThan("CreatedAt", DateTime.Now.AddMonths(-1))
            .OrderByDescending("CreatedAt")
            .ThenBy("Name")
            .Select("Id", "Name", "Email", "CreatedAt", "LastLoginAt")
            .PageSize(25)
            .IncludeCount(true)
            .Build();

        return query;
    }

    /// <summary>
    /// Example: Build a complex query with string operations and logical conditions
    /// </summary>
    public static AbstractQuery BuildProductSearchQuery(string searchTerm, decimal? maxPrice = null)
    {
        var builder = QueryBuilder
            .For("Product")
            .Contains("Name", searchTerm)
            .Where("IsAvailable", true);

        if (maxPrice.HasValue)
        {
            builder.LessThanOrEqual("Price", maxPrice.Value);
        }

        return builder
            .OrderBy("Brand")
            .ThenBy("Name")
            .Select("Id", "Name", "Brand", "Price", "Category", "Description")
            .PageSize(50)
            .Build();
    }

    /// <summary>
    /// Example: Build query with IN operator for multiple categories
    /// </summary>
    public static AbstractQuery BuildProductsByCategoriesQuery(params string[] categories)
    {
        return QueryBuilder
            .For("Product")
            .In("Category", categories)
            .Where("IsAvailable", true)
            .OrderBy("Category")
            .ThenBy("Price")
            .Select("Id", "Name", "Brand", "Price", "Category")
            .PageSize(100)
            .Build();
    }

    /// <summary>
    /// Serialize query to JSON (for client-to-server transmission)
    /// </summary>
    public static string SerializeQuery(AbstractQuery query)
    {
        return JsonSerializer.Serialize(query, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }

    /// <summary>
    /// Deserialize query from JSON (for server processing)
    /// </summary>
    public static AbstractQuery? DeserializeQuery(string json)
    {
        return JsonSerializer.Deserialize<AbstractQuery>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        });
    }

    /// <summary>
    /// Example JSON output for reference
    /// </summary>
    public static void PrintExampleJson()
    {
        var query = BuildActiveUsersQuery();
        var json = SerializeQuery(query);
        
        Console.WriteLine("Example Abstract Query JSON:");
        Console.WriteLine(json);
        Console.WriteLine();

        // Example expected output:
        /*
        {
          "entity": "User",
          "filters": [
            {
              "field": "IsActive",
              "op": "eq",
              "value": true
            },
            {
              "field": "CreatedAt",
              "op": "gt",
              "value": "2024-08-13T12:00:00Z"
            }
          ],
          "sort": [
            {
              "field": "CreatedAt",
              "dir": "desc"
            },
            {
              "field": "Name",
              "dir": "asc"
            }
          ],
          "select": [
            "Id",
            "Name", 
            "Email",
            "CreatedAt",
            "LastLoginAt"
          ],
          "page": {
            "size": 25
          },
          "hints": {
            "includeCount": true
          }
        }
        */
    }
}