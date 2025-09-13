using System;
using System.Collections.Generic;
using System.Linq;
using Causality.Shared.Features.Querying.Domain;

namespace Causality.Shared.Features.Querying.Application;

/// <summary>
/// Comprehensive examples showing usage of the expanded Abstract Query system with full LINQ operator support
/// Demonstrates SelectMany, GroupBy, Aggregations, Joins, and other advanced operators
/// </summary>
public static class AdvancedQueryExamples
{
    /// <summary>
    /// Advanced query using SelectMany to flatten collections (e.g., User -> Orders -> OrderItems)
    /// </summary>
    public static AbstractQuery SelectManyExample()
    {
        return QueryBuilder
            .For("User")
            .Where("IsActive", true)
            .SelectMany("Orders", "userOrders")  // Flatten user orders
            .Where("Status", "Completed")        // Filter on order status
            .GreaterThan("TotalAmount", 50)      // Filter on order amount
            .OrderByDescending("CreatedAt")
            .Select("UserId", "UserName", "OrderId", "TotalAmount", "CreatedAt")
            .PageSize(50)
            .Build();
    }

    /// <summary>
    /// Distinct results to remove duplicates
    /// </summary>
    public static AbstractQuery DistinctBrandsQuery()
    {
        return QueryBuilder
            .For("Product")
            .Where("IsActive", true)
            .Select("Brand")
            .Distinct()  // Remove duplicate brands
            .OrderBy("Brand")
            .Build();
    }

    /// <summary>
    /// GroupBy for analytics - product sales by category
    /// </summary>
    public static AbstractQuery SalesByCategory()
    {
        return QueryBuilder
            .For("Order")
            .Join("Product", "ProductId", "Id", "CategoryId", "CategoryName")
            .Where("Status", "Completed")
            .GroupBy("CategoryId", "CategoryName")
            .Count("TotalOrders")
            .Sum("Amount", "TotalRevenue")
            .Average("Amount", "AvgOrderValue")
            .Having("TotalRevenue", FilterOperators.GreaterThan, 1000)  // Only categories with >$1000 revenue
            .OrderByDescending("TotalRevenue")
            .Build();
    }

    /// <summary>
    /// Pagination using Skip/Take (alternative to cursor-based)
    /// </summary>
    public static AbstractQuery SkipTakeExample(int page, int pageSize)
    {
        return QueryBuilder
            .For("Product")
            .Where("IsActive", true)
            .OrderBy("Name")
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select("Id", "Name", "Price")
            .Build();
    }

    /// <summary>
    /// Complex aggregation query - sales summary
    /// </summary>
    public static AbstractQuery SalesSummary()
    {
        return QueryBuilder
            .For("Order")
            .Where("Status", "Completed")
            .GreaterThanOrEqual("CreatedAt", DateTime.Now.AddMonths(-1))  // Last month
            .Count("TotalOrders")
            .Sum("Amount", "TotalRevenue")
            .Average("Amount", "AvgOrderValue")
            .Min("Amount", "MinOrderValue")
            .Max("Amount", "MaxOrderValue")
            .Build();
    }

    /// <summary>
    /// Collection predicate evaluation - users with any high-value orders
    /// </summary>
    public static AbstractQuery UsersWithHighValueOrders()
    {
        return QueryBuilder
            .For("User")
            .Where("IsActive", true)
            .Any("Orders", "Amount", FilterOperators.GreaterThan, 1000)  // Users with any order > $1000
            .Select("Id", "Name", "Email")
            .OrderBy("Name")
            .Build();
    }

    /// <summary>
    /// Collection predicate evaluation - users where all orders are completed
    /// </summary>
    public static AbstractQuery UsersWithAllCompletedOrders()
    {
        return QueryBuilder
            .For("User")
            .Where("IsActive", true)
            .All("Orders", "Status", FilterOperators.Equal, "Completed")  // All user orders are completed
            .Select("Id", "Name", "Email")
            .OrderBy("Name")
            .Build();
    }

    /// <summary>
    /// Single element selection with validation
    /// </summary>
    public static AbstractQuery GetSingleUser(int userId)
    {
        return QueryBuilder
            .For("User")
            .Where("Id", userId)
            .Single()  // Expects exactly one result
            .Select("Id", "Name", "Email", "CreatedAt")
            .Build();
    }

    /// <summary>
    /// First element with fallback
    /// </summary>
    public static AbstractQuery GetNewestProduct()
    {
        return QueryBuilder
            .For("Product")
            .Where("IsActive", true)
            .OrderByDescending("CreatedAt")
            .FirstOrDefault()  // Get newest or null
            .Select("Id", "Name", "Price", "CreatedAt")
            .Build();
    }

    /// <summary>
    /// Complex multi-entity join query
    /// </summary>
    public static AbstractQuery UserOrdersWithProducts()
    {
        return QueryBuilder
            .For("User")
            .Where("IsActive", true)
            .Join("Order", "Id", "UserId", "Id", "Amount", "Status", "CreatedAt")
            .Join("Product", "ProductId", "Id", "Name", "Price", "Category")
            .Where("Order.Status", "Completed")
            .GreaterThan("Order.Amount", 50)
            .OrderByDescending("Order.CreatedAt")
            .Select("User.Name", "Order.Amount", "Product.Name", "Order.CreatedAt")
            .PageSize(25)
            .Build();
    }

    /// <summary>
    /// Reverse ordering for special cases
    /// </summary>
    public static AbstractQuery ReverseOrderedProducts()
    {
        return QueryBuilder
            .For("Product")
            .Where("IsActive", true)
            .OrderBy("Name")  // First order by name ascending
            .Reverse()        // Then reverse the entire result set
            .Select("Id", "Name", "Price")
            .Take(10)
            .Build();
    }

    /// <summary>
    /// Example showing multiple operations chained together
    /// </summary>
    public static AbstractQuery ComplexChainedQuery()
    {
        return QueryBuilder
            .For("Order")
            .Where("Status", "Completed")
            .GreaterThan("Amount", 25)
            .SelectMany("OrderItems", "items")  // Flatten to order items
            .Join("Product", "ProductId", "Id", "Name", "Category", "Price")
            .Where("Product.IsActive", true)
            .GroupBy("Product.Category")
            .Sum("Quantity", "TotalQuantity")
            .Sum("LineTotal", "TotalValue")
            .Count("UniqueOrders")
            .Having("TotalValue", FilterOperators.GreaterThan, 500)
            .OrderByDescending("TotalValue")
            .Take(20)
            .Build();
    }

    /// <summary>
    /// Advanced search with case-insensitive and multiple criteria
    /// </summary>
    public static AbstractQuery AdvancedProductSearch(string searchTerm, List<string> categories, decimal? minPrice, decimal? maxPrice)
    {
        var query = QueryBuilder
            .For("Product")
            .Where("IsActive", true);

        // Add search term if provided
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Contains("Name", searchTerm);
            // Future: Support OR operations for searching in multiple fields
        }

        // Add category filter if provided
        if (categories?.Count > 0)
        {
            query = query.In("Category", categories.ToArray());
        }

        // Add price range if provided
        if (minPrice.HasValue)
        {
            query = query.GreaterThanOrEqual("Price", minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            query = query.LessThanOrEqual("Price", maxPrice.Value);
        }

        return query
            .OrderBy("Category")
            .ThenBy("Name")
            .Select("Id", "Name", "Description", "Price", "Category", "Brand")
            .PageSize(50)
            .IncludeCount(true)
            .Build();
    }

    /// <summary>
    /// Analytics query - monthly revenue trends
    /// </summary>
    public static AbstractQuery MonthlyRevenueTrends()
    {
        return QueryBuilder
            .For("Order")
            .Where("Status", "Completed")
            .GreaterThanOrEqual("CreatedAt", DateTime.Now.AddMonths(-12))
            .GroupBy("Year(CreatedAt)", "Month(CreatedAt)")  // Group by year and month
            .Sum("Amount", "MonthlyRevenue")
            .Count("OrderCount")
            .Average("Amount", "AvgOrderValue")
            .OrderBy("Year(CreatedAt)")
            .ThenBy("Month(CreatedAt)")
            .Build();
    }

    /// <summary>
    /// Top customers query with joins and aggregation
    /// </summary>
    public static AbstractQuery TopCustomers()
    {
        return QueryBuilder
            .For("User")
            .Join("Order", "Id", "UserId", "Amount", "CreatedAt")
            .Where("Order.Status", "Completed")
            .GreaterThanOrEqual("Order.CreatedAt", DateTime.Now.AddMonths(-12))
            .GroupBy("User.Id", "User.Name", "User.Email")
            .Sum("Order.Amount", "TotalSpent")
            .Count("OrderCount")
            .Average("Order.Amount", "AvgOrderValue")
            .Having("TotalSpent", FilterOperators.GreaterThan, 500)
            .OrderByDescending("TotalSpent")
            .Take(50)
            .Build();
    }

    /// <summary>
    /// Product performance analysis with complex conditions
    /// </summary>
    public static AbstractQuery ProductPerformanceAnalysis()
    {
        return QueryBuilder
            .For("Product")
            .Join("OrderItem", "Id", "ProductId", "Quantity", "LineTotal")
            .Join("Order", "OrderItem.OrderId", "Id", "CreatedAt", "Status")
            .Where("Order.Status", "Completed")
            .GreaterThanOrEqual("Order.CreatedAt", DateTime.Now.AddMonths(-6))
            .GroupBy("Product.Id", "Product.Name", "Product.Category")
            .Sum("OrderItem.Quantity", "TotalSold")
            .Sum("OrderItem.LineTotal", "TotalRevenue")
            .Count("OrderCount")
            .Average("OrderItem.LineTotal", "AvgSaleValue")
            .Having("TotalSold", FilterOperators.GreaterThan, 10)
            .OrderByDescending("TotalRevenue")
            .Take(100)
            .Build();
    }

    /// <summary>
    /// Inventory management query using Any/All predicates
    /// </summary>
    public static AbstractQuery LowStockProducts()
    {
        return QueryBuilder
            .For("Product")
            .Where("IsActive", true)
            .LessThan("StockQuantity", 10)  // Low stock threshold
            .All("OrderItems", "CreatedAt", FilterOperators.LessThan, DateTime.Now.AddDays(-30))  // No recent orders
            .Select("Id", "Name", "StockQuantity", "Category", "ReorderLevel")
            .OrderBy("StockQuantity")
            .ThenBy("Category")
            .Build();
    }

    /// <summary>
    /// Customer segmentation query
    /// </summary>
    public static AbstractQuery CustomerSegmentation()
    {
        return QueryBuilder
            .For("User")
            .Join("Order", "Id", "UserId")
            .Where("Order.Status", "Completed")
            .GreaterThanOrEqual("Order.CreatedAt", DateTime.Now.AddMonths(-12))
            .GroupBy("User.Id", "User.Name")
            .Sum("Order.Amount", "TotalSpent")
            .Count("OrderCount")
            .Having("TotalSpent", FilterOperators.GreaterThanOrEqual, 1000)  // High-value customers
            .Having("OrderCount", FilterOperators.GreaterThanOrEqual, 5)     // Frequent customers
            .OrderByDescending("TotalSpent")
            .Select("User.Name", "TotalSpent", "OrderCount")
            .Build();
    }
}