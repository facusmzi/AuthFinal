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
    public class SessionRepository : GenericRepository<Session, Guid>, ISessionRepository
    {
        public SessionRepository(DbContext context, ILogger<SessionRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<Session?> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbSet
                    .Where(s => s.SessionId == sessionId && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session by SessionId {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<IEnumerable<Session>> GetActiveSessionsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbSet
                    .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active sessions for user ID {UserId}", userId);
                throw;
            }
        }

        public async Task<Session?> GetSessionWithRefreshTokenAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbSet
                    .Include(s => s.RefreshToken)
                    .Where(s => s.SessionId == sessionId)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session with refresh token for SessionId {SessionId}", sessionId);
                throw;
            }
        }

        public async Task RevokeSessionAsync(string sessionId, string reason, CancellationToken cancellationToken = default)
        {
            try
            {
                var session = await _dbSet
                    .Where(s => s.SessionId == sessionId && !s.IsRevoked)
                    .FirstOrDefaultAsync(cancellationToken);

                if (session != null)
                {
                    session.IsRevoked = true;
                    session.ReasonRevoked = reason;
                    session.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task RevokeAllUserSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
        {
            try
            {
                var sessions = await _dbSet
                    .Where(s => s.UserId == userId && !s.IsRevoked)
                    .ToListAsync(cancellationToken);

                foreach (var session in sessions)
                {
                    session.IsRevoked = true;
                    session.ReasonRevoked = reason;
                    session.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking all sessions for user {UserId}", userId);
                throw;
            }
        }

        public async Task UpdateLastActiveAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var session = await _dbSet
                    .Where(s => s.SessionId == sessionId && !s.IsRevoked)
                    .FirstOrDefaultAsync(cancellationToken);

                if (session != null)
                {
                    session.LastActive = DateTime.UtcNow;
                    session.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last active time for session {SessionId}", sessionId);
                throw;
            }
        }
    }
}
