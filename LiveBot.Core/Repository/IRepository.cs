using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LiveBot.Core.Repository
{
    public interface IRepository<TModel> where TModel : BaseModel<TModel>
    {
        Task<TModel> GetAsync(long id);
        Task<IEnumerable<TModel>> GetAllAsync();
        Task<IEnumerable<TModel>> FindAsync(Expression<Func<TModel, bool>> predicate);
        Task<IEnumerable<TModel>> FindAsync(Expression<Func<TModel, bool>> predicate, int page, int pageSize);
        Task<int> GetPageCountAsync(Expression<Func<TModel, bool>> predicate, int pageSize);
        Task<TModel> SingleOrDefaultAsync(Expression<Func<TModel, bool>> predicate);
        Task AddAsync(TModel entity);
        Task AddAsync(IEnumerable<TModel> entities);
        Task UpdateAsync(TModel entity);
        Task AddOrUpdateAsync(TModel entity, Expression<Func<TModel, bool>> predicate);
        Task RemoveAsync(long id);
    }
}
