using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Causality.Shared.Data
{
    public static class ExpressionBuilder
    {
        public static Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? BuildOrderBy<TEntity>(string field, bool ascending)
        {
            if (!field.Equals(String.Empty))
            {
                try
                {
                    var source = Expression.Parameter(typeof(IQueryable<TEntity>), "source");
                    var item = Expression.Parameter(typeof(TEntity), "item");
                    var member = Expression.Property(item, field);
                    var selector = Expression.Quote(Expression.Lambda(member, item));
                    var body = Expression.Call(
                        typeof(Queryable), ascending ? "OrderBy" : "OrderByDescending",
                        new System.Type[] { item.Type, member.Type },
                        source, selector);
                    var expr = Expression.Lambda<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>>(body, source);
                    return expr.Compile();
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        // Kanske en bugg när man använder x
        public static Expression<Func<TEntity, bool>>? BuildFilter<TEntity>(string filter)
        {
            if (!filter.Equals(String.Empty))
            {
                var p = Expression.Parameter(typeof(TEntity), "x");
                var e = (Expression)DynamicExpressionParser.ParseLambda(new[] { p }, null, filter);
                return (Expression<Func<TEntity, bool>>)e;
            }
            return null; ;
        }

    }

    public static class ExpressionExtensions
    {
        public static Expression Simplify(this Expression expression)
        {
            var searcher = new ParameterlessExpressionSearcher();
            searcher.Visit(expression);
            return new ParameterlessExpressionEvaluator(searcher.ParameterlessExpressions).Visit(expression) ?? expression;
        }

        public static Expression<T> Simplify<T>(this Expression<T> expression)
        {
            return (Expression<T>)Simplify((Expression)expression);
        }


        private class ParameterlessExpressionSearcher : ExpressionVisitor
        {
            public HashSet<Expression> ParameterlessExpressions { get; } = new HashSet<Expression>();
            private bool containsParameter = false;

            public override Expression? Visit(Expression? node)
            {
                bool originalContainsParameter = containsParameter;
                containsParameter = false;
                base.Visit(node);
                if (!containsParameter)
                {
                    if (node?.NodeType == ExpressionType.Parameter)
                        containsParameter = true;
                    else if (node != null)
                        ParameterlessExpressions.Add(node);
                }
                containsParameter |= originalContainsParameter;

                return node;
            }
        }

        private class ParameterlessExpressionEvaluator : ExpressionVisitor
        {
            private HashSet<Expression> parameterlessExpressions;
            public ParameterlessExpressionEvaluator(HashSet<Expression> parameterlessExpressions)
            {
                this.parameterlessExpressions = parameterlessExpressions;
            }
            public override Expression? Visit(Expression? node)
            {
                if (node != null && parameterlessExpressions.Contains(node))
                    return Evaluate(node);
                else
                    return base.Visit(node);
            }

            private Expression Evaluate(Expression node)
            {
                if (node.NodeType == ExpressionType.Constant)
                {
                    return node;
                }
                object? value = Expression.Lambda(node).Compile().DynamicInvoke();
                return Expression.Constant(value, node.Type);
            }
        }

    }

}
