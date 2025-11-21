using LedgerCore.Core.Models.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LedgerCore.Persistence.Repository
{
    public static class QueryHelpers
    {
        public static async Task<PagedResult<T>> ApplyPagingAsync<T>(
            IQueryable<T> query,
            PagingParams? paging,
            CancellationToken cancellationToken = default)
            where T : class
        {
            if (paging == null)
            {
                var all = await query.ToListAsync(cancellationToken);
                return new PagedResult<T>(all, all.Count, 1, all.Count == 0 ? 1 : all.Count);
            }

            if (!string.IsNullOrWhiteSpace(paging.OrderBy))
            {
                try
                {
                    query = ApplyOrdering(query, paging.OrderBy);
                }
                catch
                {
                    /* ignore invalid order */
                }
            }

            var total = await query.CountAsync(cancellationToken);

            var page = Math.Max(1, paging.PageNumber);
            var size = Math.Clamp(paging.PageSize, 1, 1000);

            var items = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync(cancellationToken);

            return new PagedResult<T>(items, total, page, size);
        }

        private static IQueryable<T> ApplyOrdering<T>(IQueryable<T> source, string orderBy)
        {
            var orders = orderBy
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s));

            IOrderedQueryable<T>? orderedQuery = null;

            foreach (var ord in orders)
            {
                var parts = ord.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var propertyPath = parts[0];
                var descending = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

                var parameter = Expression.Parameter(typeof(T), "x");
                Expression propertyAccess = parameter;

                foreach (var member in propertyPath.Split('.'))
                {
                    propertyAccess = Expression.PropertyOrField(propertyAccess, member);
                }

                var propertyType = propertyAccess.Type;
                var delegateType = typeof(Func<,>).MakeGenericType(typeof(T), propertyType);
                var lambda = Expression.Lambda(delegateType, propertyAccess, parameter);

                var methodName = GetMethodName(descending, orderedQuery == null);

                var method = typeof(Queryable)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Single(m => m.Name == methodName && m.GetParameters().Length == 2)
                    .MakeGenericMethod(typeof(T), propertyType);

                if (orderedQuery == null)
                {
                    orderedQuery = (IOrderedQueryable<T>)method.Invoke(null, new object[] { source, lambda })!;
                }
                else
                {
                    orderedQuery = (IOrderedQueryable<T>)method.Invoke(null, new object[] { orderedQuery, lambda })!;
                }
            }

            return orderedQuery ?? source;
        }

        private static string GetMethodName(bool descending, bool isFirst)
        {
            if (isFirst)
            {
                return descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy);
            }
            else
            {
                return descending ? nameof(Queryable.ThenByDescending) : nameof(Queryable.ThenBy);
            }
        }
    }
}
