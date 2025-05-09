using AuthFinal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthFinal.Domain.Interfaces
{
    public interface IRefreshTokenRepository : IGenericRepository<RefreshToken, Guid>
    {
        Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<RefreshToken?> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
        Task RevokeRefreshTokenAsync(string token, string reason, string? replacedByToken = null, CancellationToken cancellationToken = default);
    }
}
