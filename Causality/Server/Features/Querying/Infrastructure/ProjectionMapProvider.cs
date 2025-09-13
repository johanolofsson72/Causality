using System.Linq.Expressions;
using Causality.Shared.DTOs;

namespace Causality.Server.Features.Querying.Infrastructure;

/// <summary>
/// Provides projection mappings from entities to DTOs
/// Implements ADR-0004: Projection-First approach
/// </summary>
public class ProjectionMapProvider : IProjectionMapProvider
{
    private readonly Dictionary<(Type entityType, Type dtoType), LambdaExpression> _projectionMaps;

    public ProjectionMapProvider()
    {
        _projectionMaps = new Dictionary<(Type entityType, Type dtoType), LambdaExpression>();
        InitializeProjectionMaps();
    }

    public Expression<Func<TEntity, TDto>>? GetProjectionExpression<TDto>(Type entityType) where TDto : class
    {
        var key = (entityType, typeof(TDto));
        if (_projectionMaps.TryGetValue(key, out var expression))
        {
            return (Expression<Func<TEntity, TDto>>)expression;
        }

        return null;
    }

    private void InitializeProjectionMaps()
    {
        // Sample projection maps - replace with actual entity mappings

        // User entity to UserDto
        if (TryGetUserEntityType(out var userEntityType))
        {
            var userProjection = CreateUserProjection(userEntityType);
            _projectionMaps.Add((userEntityType, typeof(UserDto)), userProjection);
        }

        // Product entity to ProductDto  
        if (TryGetProductEntityType(out var productEntityType))
        {
            var productProjection = CreateProductProjection(productEntityType);
            _projectionMaps.Add((productEntityType, typeof(ProductDto)), productProjection);
        }
    }

    private bool TryGetUserEntityType(out Type entityType)
    {
        // In a real implementation, you'd get this from your DbContext or entity configuration
        // For now, return a placeholder
        entityType = typeof(UserEntity); // Replace with actual entity type
        return true;
    }

    private bool TryGetProductEntityType(out Type entityType)
    {
        entityType = typeof(ProductEntity); // Replace with actual entity type
        return true;
    }

    private LambdaExpression CreateUserProjection(Type userEntityType)
    {
        // Create expression: user => new UserDto { ... }
        var parameter = Expression.Parameter(userEntityType, "user");

        var dtoConstructor = typeof(UserDto).GetConstructor(Type.EmptyTypes)!;
        var newExpression = Expression.New(dtoConstructor);

        var bindings = new List<MemberBinding>
        {
            Expression.Bind(typeof(UserDto).GetProperty(nameof(UserDto.Id))!, 
                Expression.Property(parameter, "Id")),
            Expression.Bind(typeof(UserDto).GetProperty(nameof(UserDto.Name))!, 
                Expression.Property(parameter, "Name")),
            Expression.Bind(typeof(UserDto).GetProperty(nameof(UserDto.Email))!, 
                Expression.Property(parameter, "Email")),
            Expression.Bind(typeof(UserDto).GetProperty(nameof(UserDto.CreatedAt))!, 
                Expression.Property(parameter, "CreatedAt")),
            Expression.Bind(typeof(UserDto).GetProperty(nameof(UserDto.LastLoginAt))!, 
                Expression.Property(parameter, "LastLoginAt")),
            Expression.Bind(typeof(UserDto).GetProperty(nameof(UserDto.IsActive))!, 
                Expression.Property(parameter, "IsActive"))
        };

        var memberInit = Expression.MemberInit(newExpression, bindings);
        return Expression.Lambda(memberInit, parameter);
    }

    private LambdaExpression CreateProductProjection(Type productEntityType)
    {
        var parameter = Expression.Parameter(productEntityType, "product");

        var dtoConstructor = typeof(ProductDto).GetConstructor(Type.EmptyTypes)!;
        var newExpression = Expression.New(dtoConstructor);

        var bindings = new List<MemberBinding>
        {
            Expression.Bind(typeof(ProductDto).GetProperty(nameof(ProductDto.Id))!, 
                Expression.Property(parameter, "Id")),
            Expression.Bind(typeof(ProductDto).GetProperty(nameof(ProductDto.Name))!, 
                Expression.Property(parameter, "Name")),
            Expression.Bind(typeof(ProductDto).GetProperty(nameof(ProductDto.Brand))!, 
                Expression.Property(parameter, "Brand")),
            Expression.Bind(typeof(ProductDto).GetProperty(nameof(ProductDto.Price))!, 
                Expression.Property(parameter, "Price")),
            Expression.Bind(typeof(ProductDto).GetProperty(nameof(ProductDto.Category))!, 
                Expression.Property(parameter, "Category")),
            Expression.Bind(typeof(ProductDto).GetProperty(nameof(ProductDto.CreatedAt))!, 
                Expression.Property(parameter, "CreatedAt")),
            Expression.Bind(typeof(ProductDto).GetProperty(nameof(ProductDto.IsAvailable))!, 
                Expression.Property(parameter, "IsAvailable")),
            Expression.Bind(typeof(ProductDto).GetProperty(nameof(ProductDto.Description))!, 
                Expression.Property(parameter, "Description"))
        };

        var memberInit = Expression.MemberInit(newExpression, bindings);
        return Expression.Lambda(memberInit, parameter);
    }
}

// Placeholder entity classes - replace with your actual entities
public class UserEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    // Additional entity-specific fields that shouldn't be exposed via DTO
    public string PasswordHash { get; set; } = string.Empty; // Never expose this!
    public string InternalNotes { get; set; } = string.Empty; // Internal only
}

public class ProductEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsAvailable { get; set; }
    public string? Description { get; set; }
    // Additional entity-specific fields
    public decimal CostPrice { get; set; } // Don't expose cost price in DTO
    public string InternalSku { get; set; } = string.Empty; // Internal only
}