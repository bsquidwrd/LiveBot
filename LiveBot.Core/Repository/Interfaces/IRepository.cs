using LiveBot.Core.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LiveBot.Core.Repository.Interfaces
{
    /// <summary>Defines a generic repository that implements the common interactions we would have on entities.</summary>
    /// <typeparam name="TModel">The BaseModel for this repository.</typeparam>
    public interface IRepository<TModel> where TModel : BaseModel<TModel>
    {
        /// <summary>Gets an entity based on it's Id.</summary>
        /// <param name="Id">The Id of the entity.</param>
        /// <returns><see cref="Task"/>.</returns>
        Task<TModel> GetAsync(int Id);

        /// <summary>Gets all of the entities in the database.</summary>
        /// <returns><see cref="Task"/>.</returns>
        Task<IEnumerable<TModel>> GetAllAsync();

        /// <summary>Finds all entities matching the provided predicate.</summary>
        /// <param name="predicate">The filter used to find the entities.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task<IEnumerable<TModel>> FindAsync(Expression<Func<TModel, bool>> predicate);

        /// <summary>Finds all entities matching the provided predicate. Allows for paging of the results.</summary>
        /// <param name="predicate">The filter used to find the entities.</param>
        /// <param name="page">The page you want returned.</param>
        /// <param name="pageSize">The size of each page. <seealso cref="GetPageCountAsync"/></param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task<IEnumerable<TModel>> FindAsync(Expression<Func<TModel, bool>> predicate, int page, int pageSize);

        /// <summary>Finds all entities matching the provided predicate. Allows for paging of the results. Also allows Ordering of the results.</summary>
        /// <param name="predicate">The filter used to find the entities.</param>
        /// <param name="order">The numeric property to use for the ordering.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task<IEnumerable<TModel>> FindInOrderAsync(Expression<Func<TModel, bool>> predicate, Expression<Func<TModel, long>> order);

        /// <summary>Finds all entities matching the provided predicate. Allows for paging of the results. Also allows Ordering of the results.</summary>
        /// <param name="predicate">The filter used to find the entities.</param>
        /// <param name="order">The numeric property to use for the ordering.</param>
        /// <param name="page">The page you want returned.</param>
        /// <param name="pageSize">The size of each page. <seealso cref="GetPageCountAsync"/></param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task<IEnumerable<TModel>> FindInOrderAsync(Expression<Func<TModel, bool>> predicate, Expression<Func<TModel, long>> order, int page, int pageSize);

        /// <summary>Given a number of entities per page returns how many pages of entities are in the database.</summary>
        /// <param name="predicate">The filter for finding entities.</param>
        /// <param name="pageSize">The size of your pages.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task<int> GetPageCountAsync(Expression<Func<TModel, bool>> predicate, int pageSize);

        /// <summary>Finds an entity with the given filter, or returns default if one can't be found.</summary>
        /// <param name="predicate">The filter for finding the entity.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task<TModel> SingleOrDefaultAsync(Expression<Func<TModel, bool>> predicate);

        /// <summary>Adds an entity to the database, tracks the Id back to the given entity.</summary>
        /// <param name="entity">The entity to add to the database.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task AddAsync(TModel entity);

        /// <summary>Adds a collection of entities to the database and tracks Id's back to the individual entities.</summary>
        /// <param name="entities">The entities to add to the database.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task AddAsync(IEnumerable<TModel> entities);

        /// <summary>Updates the provided entity in the database, based on its Id.</summary>
        /// <param name="entity">The entity to update.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task UpdateAsync(TModel entity);

        /// <summary>Adds the provided entity to the database, or updates the database if an entity with this filter already exists.</summary>
        /// <param name="entity">The entity to add or update.</param>
        /// <param name="predicate">The filter that is used to find existing entities.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task AddOrUpdateAsync(TModel entity, Expression<Func<TModel, bool>> predicate);

        /// <summary>Removes an entity from the database based on its Id.</summary>
        /// <param name="Id">The id of the entity to remove.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task RemoveAsync(int Id);
    }
}