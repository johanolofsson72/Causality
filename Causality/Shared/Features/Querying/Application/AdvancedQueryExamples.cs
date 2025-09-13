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

    // =============== COMPREHENSIVE LINQ OPERATOR EXAMPLES ===============

    /// <summary>
    /// DistinctBy - Remove duplicates by specific field (e.g., unique users by email domain)
    /// </summary>
    public static AbstractQuery DistinctByEmailDomain()
    {
        return QueryBuilder
            .For("User")
            .Where("IsActive", true)
            .DistinctBy("EmailDomain")  // One user per email domain
            .OrderBy("EmailDomain")
            .Select("Id", "Name", "Email", "EmailDomain")
            .Build();
    }

    /// <summary>
    /// SkipWhile - Skip elements while condition is true (skip inactive products until first active)
    /// </summary>
    public static AbstractQuery SkipWhileInactive()
    {
        return QueryBuilder
            .For("Product")
            .OrderBy("CreatedAt")
            .SkipWhile("IsActive", FilterOperators.Equal, false)  // Skip inactive products
            .Select("Id", "Name", "IsActive", "CreatedAt")
            .Build();
    }

    /// <summary>
    /// TakeWhile - Take elements while condition is true (take orders until cancelled)
    /// </summary>
    public static AbstractQuery TakeWhileNotCancelled()
    {
        return QueryBuilder
            .For("Order")
            .OrderBy("CreatedAt")
            .TakeWhile("Status", FilterOperators.NotEquals, "Cancelled")
            .Select("Id", "Status", "CreatedAt", "Amount")
            .Build();
    }

    /// <summary>
    /// SkipLast and TakeLast - Skip/take from the end
    /// </summary>
    public static AbstractQuery RecentOrdersExceptLatest()
    {
        return QueryBuilder
            .For("Order")
            .OrderBy("CreatedAt")
            .SkipLast(5)    // Skip the 5 most recent orders
            .TakeLast(10)   // Take 10 before those
            .Select("Id", "CreatedAt", "Status", "Amount")
            .Build();
    }

    /// <summary>
    /// Union - Combine two queries (premium and VIP customers)
    /// </summary>
    public static AbstractQuery PremiumAndVipCustomers()
    {
        var premiumQuery = QueryBuilder
            .For("Customer")
            .Where("Tier", "Premium")
            .Build();

        return QueryBuilder
            .For("Customer")
            .Where("Tier", "VIP")
            .Union(premiumQuery)  // Combine with premium customers
            .Distinct()           // Remove duplicates if any
            .Select("Id", "Name", "Tier", "TotalSpent")
            .Build();
    }

    /// <summary>
    /// UnionBy - Union by key field (combine customers by email uniqueness)
    /// </summary>
    public static AbstractQuery UniqueCustomersByEmail()
    {
        var inactiveQuery = QueryBuilder
            .For("Customer")
            .Where("IsActive", false)
            .Build();

        return QueryBuilder
            .For("Customer")
            .Where("IsActive", true)
            .UnionBy(inactiveQuery, "Email")  // Union by unique email addresses
            .Select("Id", "Name", "Email", "IsActive")
            .Build();
    }

    /// <summary>
    /// Intersect - Find common elements (customers who are both VIP and have recent orders)
    /// </summary>
    public static AbstractQuery VipCustomersWithRecentOrders()
    {
        var vipQuery = QueryBuilder
            .For("Customer")
            .Where("Tier", "VIP")
            .Build();

        return QueryBuilder
            .For("Customer")
            .Join("Order", "Id", "CustomerId")
            .GreaterThanOrEqual("Order.CreatedAt", DateTime.Now.AddMonths(-3))
            .Intersect(vipQuery)  // Only VIP customers with recent orders
            .Select("Id", "Name", "Tier")
            .Build();
    }

    /// <summary>
    /// Except - Exclude elements (all customers except cancelled ones)
    /// </summary>
    public static AbstractQuery ActiveCustomersExceptCancelled()
    {
        var cancelledQuery = QueryBuilder
            .For("Customer")
            .Where("Status", "Cancelled")
            .Build();

        return QueryBuilder
            .For("Customer")
            .Except(cancelledQuery)  // All customers except cancelled ones
            .Select("Id", "Name", "Status", "CreatedAt")
            .Build();
    }

    /// <summary>
    /// Concat - Concatenate sequences (combine current and archived orders)
    /// </summary>
    public static AbstractQuery AllOrdersIncludingArchived()
    {
        var archivedQuery = QueryBuilder
            .For("ArchivedOrder")
            .Select("Id", "CustomerId", "Amount", "Status")
            .Build();

        return QueryBuilder
            .For("Order")
            .Concat(archivedQuery)  // Concatenate with archived orders
            .OrderByDescending("CreatedAt")
            .Select("Id", "CustomerId", "Amount", "Status")
            .Build();
    }

    /// <summary>
    /// Zip - Combine two sequences element by element (pair products with recommendations)
    /// </summary>
    public static AbstractQuery ProductsWithRecommendations()
    {
        var recommendationsQuery = QueryBuilder
            .For("ProductRecommendation")
            .OrderBy("ProductId")
            .Build();

        return QueryBuilder
            .For("Product")
            .OrderBy("Id")
            .Zip(recommendationsQuery, "ProductRecommendationPair")
            .Select("Product.Name", "Recommendation.Score", "Recommendation.Type")
            .Build();
    }

    /// <summary>
    /// OfType - Filter by type (for polymorphic entities)
    /// </summary>
    public static AbstractQuery DigitalProductsOnly()
    {
        return QueryBuilder
            .For("Product")
            .OfType("DigitalProduct")  // Only digital products
            .Where("IsDownloadable", true)
            .Select("Id", "Name", "FileSize", "Format")
            .Build();
    }

    /// <summary>
    /// DefaultIfEmpty - Handle empty sequences
    /// </summary>
    public static AbstractQuery CustomersWithOptionalOrders()
    {
        return QueryBuilder
            .For("Customer")
            .GroupJoin("Order", "Id", "CustomerId")  // Left join
            .DefaultIfEmpty()  // Include customers with no orders
            .Select("Customer.Name", "Order.Amount", "Order.Status")
            .Build();
    }

    /// <summary>
    /// Chunk - Split into chunks for batch processing
    /// </summary>
    public static AbstractQuery ProductsBatchProcessing()
    {
        return QueryBuilder
            .For("Product")
            .Where("RequiresProcessing", true)
            .OrderBy("Priority")
            .Chunk(100)  // Process in batches of 100
            .Select("Id", "Name", "Priority")
            .Build();
    }

    /// <summary>
    /// SelectWithIndex - Include element index in projection
    /// </summary>
    public static AbstractQuery RankedTopProducts()
    {
        return QueryBuilder
            .For("Product")
            .OrderByDescending("Rating")
            .SelectWithIndex("Id", "Name", "Rating")  // Include ranking index
            .Take(20)
            .Build();
    }

    /// <summary>
    /// ElementAt and ElementAtOrDefault - Get specific positioned elements
    /// </summary>
    public static AbstractQuery ThirdHighestRatedProduct()
    {
        return QueryBuilder
            .For("Product")
            .OrderByDescending("Rating")
            .ElementAt(2)  // Zero-based index, so 2 = third element
            .Select("Id", "Name", "Rating")
            .Build();
    }

    /// <summary>
    /// Last and LastOrDefault - Get last elements
    /// </summary>
    public static AbstractQuery MostRecentOrder()
    {
        return QueryBuilder
            .For("Order")
            .Where("Status", "Completed")
            .OrderBy("CreatedAt")
            .Last()  // Get the last (most recent) order
            .Select("Id", "CustomerId", "Amount", "CreatedAt")
            .Build();
    }

    /// <summary>
    /// LongCount - Count with long return type for large datasets
    /// </summary>
    public static AbstractQuery TotalUserRegistrations()
    {
        return QueryBuilder
            .For("User")
            .LongCount("TotalUsers")  // Use long count for potentially large numbers
            .Build();
    }

    /// <summary>
    /// Aggregate - Custom aggregation operations
    /// </summary>
    public static AbstractQuery CustomAggregateExample()
    {
        return QueryBuilder
            .For("Order")
            .Where("Status", "Completed")
            .Aggregate("Amount", "SumOfSquares", "TotalVariance")  // Custom aggregate function
            .Build();
    }

    /// <summary>
    /// Contains - Check if sequence contains element
    /// </summary>
    public static AbstractQuery CheckForSpecificProduct()
    {
        return QueryBuilder
            .For("Product")
            .Where("CategoryId", 1)
            .ContainsElement("Special Item")  // Check if category contains specific product
            .Build();
    }

    /// <summary>
    /// SequenceEqual - Compare two sequences for equality
    /// </summary>
    public static AbstractQuery CompareProductLists()
    {
        var expectedQuery = QueryBuilder
            .For("ExpectedProduct")
            .OrderBy("Id")
            .Select("Id", "Name")
            .Build();

        return QueryBuilder
            .For("ActualProduct")
            .OrderBy("Id")
            .Select("Id", "Name")
            .SequenceEqual(expectedQuery)  // Check if sequences are identical
            .Build();
    }

    /// <summary>
    /// ToLookup - Create lookup/dictionary structure
    /// </summary>
    public static AbstractQuery ProductLookupByCategory()
    {
        return QueryBuilder
            .For("Product")
            .Where("IsActive", true)
            .ToLookup("CategoryId", "Name")  // Create lookup: CategoryId -> Product Names
            .Build();
    }

    /// <summary>
    /// Range - Generate sequence of numbers (for pagination or indexing)
    /// </summary>
    public static AbstractQuery GeneratePageNumbers()
    {
        return QueryBuilder
            .Range(1, 100)  // Generate numbers 1 to 100
            .Select("PageNumber")
            .Build();
    }

    /// <summary>
    /// Repeat - Repeat elements (for templates or defaults)
    /// </summary>
    public static AbstractQuery DefaultProductTemplate()
    {
        return QueryBuilder
            .Repeat(new { Name = "Default Product", Price = 0.0m }, 5)  // Repeat default 5 times
            .Select("Name", "Price")
            .Build();
    }

    /// <summary>
    /// Empty - Create empty sequence (for initialization or fallbacks)
    /// </summary>
    public static AbstractQuery EmptyProductList()
    {
        return QueryBuilder
            .Empty("Product")  // Empty product sequence
            .Select("Id", "Name")
            .Build();
    }

    /// <summary>
    /// Complex example combining multiple new LINQ operators
    /// </summary>
    public static AbstractQuery ComplexMultiOperatorQuery()
    {
        return QueryBuilder
            .For("Order")
            .Where("Status", FilterOperators.In, new[] { "Pending", "Processing", "Completed" })
            .SkipWhile("Priority", FilterOperators.Equal, "Low")  // Skip low priority orders
            .TakeWhile("Amount", FilterOperators.LessThan, 10000)  // Take orders under $10k
            .DistinctBy("CustomerId")  // One order per customer
            .OrderByDescending("CreatedAt")
            .ThenBy("Amount")
            .SkipLast(2)  // Skip the 2 most recent
            .SelectWithIndex("Id", "CustomerId", "Amount", "Priority", "CreatedAt")  // Include row numbers
            .Take(50)
            .Build();
    }
}