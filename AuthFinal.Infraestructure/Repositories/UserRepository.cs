using AuthFinal.Domain.Entities;
using AuthFinal.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthFinal.Infraestructure.Repositories
{
    public class UserRepository : GenericRepository<User, Guid>, IUserRepository
    {
        public UserRepository(DbContext context, ILogger<UserRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<User>> GetAllAsync(bool includeInactive = false, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            try
            {
                IQueryable<User> query = _dbSet;

                if (!includeInactive)
                    query = query.Where(u => u.IsActive);

                if (!includeDeleted)
                    query = query.Where(u => !u.IsDeleted);

                return await query.ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                throw;
            }
        }

        public async Task<User?> GetByIdAsync(Guid id, bool includeInactive = false, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _dbSet.Where(u => u.Id == id);

                if (!includeInactive)
                    query = query.Where(u => u.IsActive);

                if (!includeDeleted)
                    query = query.Where(u => !u.IsDeleted);

                return await query.FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID {UserId}", id);
                throw;
            }
        }

        public async Task<User?> GetByEmailAsync(string email, bool includeInactive = false, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _dbSet.Where(u => u.Email == email);

                if (!includeInactive)
                    query = query.Where(u => u.IsActive);

                if (!includeDeleted)
                    query = query.Where(u => !u.IsDeleted);

                return await query.FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email {Email}", email);
                throw;
            }
        }

        public async Task<User?> GetUserWithRolesAsync(Guid userId, bool includeInactive = false, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _dbSet.Include(u => u.Roles).Where(u => u.Id == userId);

                if (!includeInactive)
                    query = query.Where(u => u.IsActive);

                if (!includeDeleted)
                    query = query.Where(u => !u.IsDeleted);

                return await query.FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user with roles for ID {UserId}", userId);
                throw;
            }
        }

        public async Task<User?> GetUserWithSessionsAsync(Guid userId, bool includeInactive = false, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _dbSet.Include(u => u.Sessions).Where(u => u.Id == userId);

                if (!includeInactive)
                    query = query.Where(u => u.IsActive);

                if (!includeDeleted)
                    query = query.Where(u => !u.IsDeleted);

                return await query.FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user with sessions for ID {UserId}", userId);
                throw;
            }
        }

        public async Task<User?> GetUserWithRolesAndSessionsAsync(Guid userId, bool includeInactive = false, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _dbSet.Include(u => u.Roles).Include(u => u.Sessions).Where(u => u.Id == userId);

                if (!includeInactive)
                    query = query.Where(u => u.IsActive);

                if (!includeDeleted)
                    query = query.Where(u => !u.IsDeleted);

                return await query.FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user with roles and sessions for ID {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ExistsByEmailAsync(string email, bool includeInactive = false, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _dbSet.Where(u => u.Email == email);

                if (!includeInactive)
                    query = query.Where(u => u.IsActive);

                if (!includeDeleted)
                    query = query.Where(u => !u.IsDeleted);

                return await query.AnyAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if email exists: {Email}", email);
                throw;
            }
        }

        public async Task<IEnumerable<User>> GetActiveUsersAsync(bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _dbSet.Where(u => u.IsActive);

                if (!includeDeleted)
                    query = query.Where(u => !u.IsDeleted);

                return await query.ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active users");
                throw;
            }
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName, bool includeInactive = false, bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _dbSet
                    .Include(u => u.Roles)
                    .Where(u => u.Roles.Any(r => r.Name == roleName));

                if (!includeInactive)
                    query = query.Where(u => u.IsActive);

                if (!includeDeleted)
                    query = query.Where(u => !u.IsDeleted);

                return await query.ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by role {Role}", roleName);
                throw;
            }
        }

        public async Task SoftDeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await GetByIdAsync(userId, true, true, cancellationToken);
            if (user != null)
            {
                user.IsDeleted = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await GetByIdAsync(userId, true, false, cancellationToken);
            if (user != null)
            {
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await GetByIdAsync(userId, true, false, cancellationToken);
            if (user != null)
            {
                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task VerifyEmailAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await GetByIdAsync(userId, true, false, cancellationToken);
            if (user != null)
            {
                user.EmailVerified = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task VerifyPhoneAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await GetByIdAsync(userId, true, false, cancellationToken);
            if (user != null)
            {
                user.PhoneVerified = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<(IEnumerable<User> Items, int TotalCount)> SearchUsersAsync(
            string? searchTerm,
            int pageIndex,
            int pageSize,
            bool includeInactive = false,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _dbSet.AsQueryable();

                if (!includeInactive)
                    query = query.Where(u => u.IsActive);

                if (!includeDeleted)
                    query = query.Where(u => !u.IsDeleted);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var lowered = searchTerm.ToLower();
                    query = query.Where(u =>
                        u.FirstName.ToLower().Contains(lowered) ||
                        u.LastName.ToLower().Contains(lowered) ||
                        u.Email.ToLower().Contains(lowered) ||
                        (u.Phone != null && u.Phone.Contains(lowered))
                    );
                }

                var totalCount = await query.CountAsync(cancellationToken);

                var items = await query
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users");
                throw;
            }
        }
    }
}
