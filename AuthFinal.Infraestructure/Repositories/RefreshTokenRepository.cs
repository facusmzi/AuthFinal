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
    public class RefreshTokenRepository : GenericRepository<RefreshToken, Guid>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(DbContext context, ILogger<RefreshTokenRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbSet
                    .FirstOrDefaultAsync(r => r.Token == token, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting refresh token");
                throw;
            }
        }

        public async Task<RefreshToken?> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbSet
                    .FirstOrDefaultAsync(r => r.SessionId == sessionId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting refresh token by session ID {SessionId}", sessionId);
                throw;
            }
        }

        public async Task RevokeRefreshTokenAsync(string token, string reason, string? replacedByToken = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var refreshToken = await GetByTokenAsync(token, cancellationToken);
                if (refreshToken != null)
                {
                    refreshToken.IsRevoked = true;
                    refreshToken.ReasonRevoked = reason;
                    refreshToken.ReplacedByToken = replacedByToken ?? string.Empty;
                    refreshToken.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking refresh token");
                throw;
            }
        }
    }
}
