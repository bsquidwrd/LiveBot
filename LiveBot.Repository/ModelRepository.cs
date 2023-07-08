﻿using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LiveBot.Repository
{
    /// <inheritdoc/>
    public class ModelRepository<TEntity> : IRepository<TEntity>
        where TEntity : BaseModel<TEntity>
    {
        /// <summary>
        /// The Entity Framework Database Context to use.
        /// </summary>
        protected readonly DbContext Context;

        /// <summary>
        /// The db set that contains the entities.
        /// </summary>
        protected readonly DbSet<TEntity> DbSet;

        /// <summary>
        /// The lock used to synchronize threads during certain actions as EF is not entirely multi
        /// thread capable.
        /// </summary>
        private readonly SemaphoreSlim syncLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelRepository{TEntity}"/> class.
        /// </summary>
        /// <param name="context">The Entity Framework Database Context to use.</param>
        protected ModelRepository(DbContext context)
        {
            Context = context;
            DbSet = context.Set<TEntity>();
        }

        /// <summary>
        /// Include all related objects automatically
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns>Pre-Filtered object to exclude deleted items</returns>
        private IQueryable<TEntity> GetQueryable(Expression<Func<TEntity, bool>> predicate = null)
        {
            Expression<Func<TEntity, bool>> deletedPredicate = (d => d.Deleted == false);
            IQueryable<TEntity> queryable = DbSet
                .Where(deletedPredicate);

            if (predicate != null)
                queryable = queryable.Where(predicate);

            var properties = typeof(TEntity).GetProperties().Where(x => x.PropertyType != typeof(string));
            foreach (var prop in properties.Where(x => x.PropertyType.IsClass))
                queryable = queryable.Include(prop.Name);

            return queryable;
        }

        /// <inheritdoc/>
        public Task<long> LongCountAsync() =>
            DbSet.LongCountAsync();

        /// <inheritdoc/>
        public Task<long> LongCountAsync(Expression<Func<TEntity, bool>> predicate) =>
            GetQueryable(predicate).LongCountAsync();

        /// <inheritdoc/>
        public Task<TEntity> GetAsync(long Id)
        {
            return DbSet
                .FindAsync(Id)
                .AsTask();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await GetQueryable()
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await GetQueryable(predicate)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="predicate">predicate</paramref> is null.
        /// </exception>
        public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, int page, int pageSize)
        {
            return await GetQueryable(predicate)
                .Skip((page * pageSize) - pageSize)
                .Take(pageSize)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="predicate">predicate</paramref> is null.
        /// </exception>
        public virtual async Task<IEnumerable<TEntity>> FindInOrderAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, long>> order)
        {
            return await GetQueryable(predicate)
                .OrderByDescending(order)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="predicate">predicate</paramref> is null.
        /// </exception>
        public virtual async Task<IEnumerable<TEntity>> FindInOrderAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, long>> order, int page, int pageSize)
        {
            return await GetQueryable(predicate)
                .OrderByDescending(order)
                .Skip((page * pageSize) - pageSize)
                .Take(pageSize)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="predicate">predicate</paramref> is null.
        /// </exception>
        public async Task<int> GetPageCountAsync(Expression<Func<TEntity, bool>> predicate, int pageSize)
        {
            var count = await GetQueryable(predicate)
                .CountAsync()
                .ConfigureAwait(false);
            if (count <= pageSize)
                return 1;

            return (int)Math.Ceiling((decimal)count / pageSize);
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="predicate">predicate</paramref> is null.
        /// </exception>
        public virtual Task<TEntity> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return GetQueryable(predicate)
                .FirstOrDefaultAsync();
        }

        /// <inheritdoc/>
        public async Task AddAsync(TEntity entity)
        {
            try
            {
                await syncLock.WaitAsync().ConfigureAwait(false);
                DbSet.Add(entity);
                await Context.SaveChangesAsync().ConfigureAwait(false);
            }
            finally
            {
                syncLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task AddAsync(IEnumerable<TEntity> entities)
        {
            try
            {
                await syncLock.WaitAsync().ConfigureAwait(false);
                DbSet.AddRange(entities);
                await Context.SaveChangesAsync().ConfigureAwait(false);
            }
            finally
            {
                syncLock.Release();
            }
        }

        /// <inheritdoc/>
        /// <exception cref="SemaphoreFullException">
        /// The <see cref="T:System.Threading.SemaphoreSlim"></see> has already reached its maximum size.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The current instance has already been disposed.
        /// </exception>
        public async Task UpdateAsync(TEntity entity)
        {
            try
            {
                await syncLock.WaitAsync().ConfigureAwait(false);
                DbSet.Attach(entity);
                Context.Entry(entity).State = EntityState.Modified;
                await Context.SaveChangesAsync().ConfigureAwait(false);
            }
            finally
            {
                syncLock.Release();
            }
        }

        /// <inheritdoc/>
        /// <exception cref="SemaphoreFullException">
        /// The <see cref="T:System.Threading.SemaphoreSlim"></see> has already reached its maximum size.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The current instance has already been disposed.
        /// </exception>
        public virtual async Task AddOrUpdateAsync(TEntity entity, Expression<Func<TEntity, bool>> predicate)
        {
            await syncLock.WaitAsync().ConfigureAwait(false);
            var exists = await SingleOrDefaultAsync(predicate).ConfigureAwait(false);
            if (exists == null) // No entry in database, just save it now.
            {
                syncLock.Release(); // Was creating a deadlock here.
                await AddAsync(entity).ConfigureAwait(false);
                return;
            }
            else if (exists == entity)
            {
                // Both the provided entity and the db entity are the same.
                syncLock.Release();
                return;
            }
            else // Update the db entry from the entity and save.
            {
                entity.Id = exists.Id;
                Context.Entry(exists).CurrentValues.SetValues(entity);
                await Context.SaveChangesAsync().ConfigureAwait(false);
            }
            syncLock.Release();
        }

        /// <inheritdoc/>
        /// <exception cref="SemaphoreFullException">
        /// The <see cref="T:System.Threading.SemaphoreSlim"></see> has already reached its maximum size.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The current instance has already been disposed.
        /// </exception>
        public async Task RemoveAsync(long Id)
        {
            try
            {
                await syncLock.WaitAsync().ConfigureAwait(false);
                TEntity entity = await DbSet.FindAsync(Id).ConfigureAwait(false);
                DbSet.Remove(entity);
                await Context.SaveChangesAsync().ConfigureAwait(false);
            }
            finally
            {
                syncLock.Release();
            }
        }
    }
}
