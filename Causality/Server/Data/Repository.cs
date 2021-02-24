using Causality.Repositories;
using Causality.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Causality.Server.Data
{
    public class Repository<TEntity, TDataContext> : IRepository<TEntity>
        where TEntity : class
        where TDataContext : DbContext
    {
        protected readonly TDataContext context;
        internal DbSet<TEntity> dbSet;

        public Repository(TDataContext dataContext)
        {
            context = dataContext;
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            dbSet = context.Set<TEntity>();
        }

        public virtual async Task<bool> Delete(TEntity entityToDelete)
        {
            if (context.Entry(entityToDelete).State == EntityState.Detached)
            {
                dbSet.Attach(entityToDelete);
            }
            dbSet.Remove(entityToDelete);
            return await context.SaveChangesAsync() >= 1;
        }

        public virtual async Task<bool> Delete(object id)
        {
            TEntity entityToDelete = await dbSet.FindAsync(id);
            return await Delete(entityToDelete);
        }

        public virtual async Task<IEnumerable<TEntity>> Get(Expression<Func<TEntity, bool>> filter = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null)
        {
            try
            {
                // Get the dbSet from the Entity passed in                
                IQueryable<TEntity> query = dbSet;

                // Apply the filter
                if (filter != null)
                {
                    query = query.Where(filter);
                }

                // Sort
                if (orderBy != null)
                {
                    query = orderBy(query);
                }

                System.Diagnostics.Debug.WriteLine("");
                System.Diagnostics.Debug.WriteLine("== SQL ===================");
                System.Diagnostics.Debug.WriteLine(query.ToQueryString());
                System.Diagnostics.Debug.WriteLine("==========================");
                System.Diagnostics.Debug.WriteLine("");
                return await query.ToListAsync();
            }
            catch
            {
                return null;
            }
        }

        public async Task<TEntity> GetById(object id)
        {
            return await dbSet.FindAsync(id);
        }

        public virtual async Task<TEntity> Insert(TEntity entity)
        {
            await dbSet.AddAsync(entity);
            await context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<TEntity> Update(TEntity entityToUpdate)
        {
            var dbSet = context.Set<TEntity>();
            dbSet.Attach(entityToUpdate);
            context.Entry(entityToUpdate).State = EntityState.Modified;
            await context.SaveChangesAsync();
            return entityToUpdate;
        }
    }

}
