using AuthFinal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthFinal.Domain.Interfaces
{
    public interface ISessionRepository : IGenericRepository<Session, Guid>
    {
        Task<Session?> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Session>> GetActiveSessionsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<Session?> GetSessionWithRefreshTokenAsync(string sessionId, CancellationToken cancellationToken = default);
        Task RevokeSessionAsync(string sessionId, string reason, CancellationToken cancellationToken = default);
        Task RevokeAllUserSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken = default);
        Task UpdateLastActiveAsync(string sessionId, CancellationToken cancellationToken = default);
    }
}
