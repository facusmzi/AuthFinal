using AuthFinal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AuthFinal.Domain.Interfaces
{
    public interface IUserRepository : IGenericRepository<User, Guid>
    {
        // Métodos con opciones para incluir usuarios inactivos y/o eliminados
        Task<IEnumerable<User>> GetAllAsync(bool includeInactive = false, bool includeDeleted = false, CancellationToken cancellationToken = default);
        Task<User?> GetByIdAsync(Guid id, bool includeInactive = false, bool includeDeleted = false, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, bool includeInactive = false, bool includeDeleted = false, CancellationToken cancellationToken = default);
        Task<User?> GetUserWithRolesAsync(Guid userId, bool includeInactive = false, bool includeDeleted = false, CancellationToken cancellationToken = default);  
        Task<bool> ExistsByEmailAsync(string email, bool includeInactive = false, bool includeDeleted = false, CancellationToken cancellationToken = default);
        Task<IEnumerable<User>> GetActiveUsersAsync(bool includeDeleted = false, CancellationToken cancellationToken = default);
        Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName, bool includeInactive = false, bool includeDeleted = false, CancellationToken cancellationToken = default);

        // Métodos para actualizar estados
        Task SoftDeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task VerifyEmailAsync(Guid userId, CancellationToken cancellationToken = default);

        // Búsqueda y paginación
        Task<(IEnumerable<User> Items, int TotalCount)> SearchUsersAsync(
            string? searchTerm,
            int pageIndex,
            int pageSize,
            bool includeInactive = false,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default);
    }
}

