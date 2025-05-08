using AuthFinal.Domain.Entities;
using AuthFinal.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AuthFinal.Infraestructure.Repositories
{
    public class GenericRepository<T, TId> : IGenericRepository<T, TId> where T : EntityBase<TId>
    {
        protected readonly DbContext _context;
        protected readonly DbSet<T> _dbSet;
        protected readonly ILogger<GenericRepository<T, TId>> _logger;

        public GenericRepository(DbContext context, ILogger<GenericRepository<T, TId>> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = context.Set<T>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbSet.ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all entities of type {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public virtual async Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting entity of type {EntityType} with ID {EntityId}", typeof(T).Name, id);
                throw;
            }
        }

        public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dbSet.AddAsync(entity, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                return result.Entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding entity of type {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            try
            {
                entity.UpdatedAt = DateTime.UtcNow;
                _context.Entry(entity).State = EntityState.Modified;
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating entity of type {EntityType} with ID {EntityId}", typeof(T).Name, entity.Id);
                throw;
            }
        }

        public virtual async Task HardDeleteAsync(TId id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await GetByIdAsync(id, cancellationToken);
                if (entity != null)
                {
                    _dbSet.Remove(entity);
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hard deleting entity of type {EntityType} with ID {EntityId}", typeof(T).Name, id);
                throw;
            }
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding entities of type {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public virtual async Task<IEnumerable<T>> GetAllWithIncludesAsync(params Expression<Func<T, object>>[] includeProperties)
        {
            try
            {
                IQueryable<T> query = _dbSet;

                foreach (var includeProperty in includeProperties)
                {
                    query = query.Include(includeProperty);
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all entities with includes of type {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            int pageIndex,
            int pageSize,
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                IQueryable<T> query = _dbSet;

                if (filter != null)
                {
                    query = query.Where(filter);
                }

                int totalCount = await query.CountAsync(cancellationToken);

                if (orderBy != null)
                {
                    query = orderBy(query);
                }

                var items = await query
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged entities of type {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (predicate == null)
                {
                    return await _dbSet.CountAsync(cancellationToken);
                }
                return await _dbSet.CountAsync(predicate, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting entities of type {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbSet.AnyAsync(predicate, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of entity of type {EntityType}", typeof(T).Name);
                throw;
            }
        }

        public virtual async Task<IEnumerable<TResult>> SelectAsync<TResult>(
            Expression<Func<T, TResult>> selector,
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                IQueryable<T> query = _dbSet;

                if (predicate != null)
                {
                    query = query.Where(predicate);
                }

                return await query.Select(selector).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting from entities of type {EntityType}", typeof(T).Name);
                throw;
            }
        }
    }
}
