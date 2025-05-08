using AuthFinal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AuthFinal.Domain.Interfaces
{
    public interface IGenericRepository<T, TId> where T : EntityBase<TId>
    {
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
        Task HardDeleteAsync(TId id, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetAllWithIncludesAsync(params Expression<Func<T, object>>[] includeProperties);
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            int pageIndex,
            int pageSize,
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            CancellationToken cancellationToken = default);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<IEnumerable<TResult>> SelectAsync<TResult>(
            Expression<Func<T, TResult>> selector,
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken cancellationToken = default);
    }
}
