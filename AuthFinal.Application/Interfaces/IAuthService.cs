using AuthFinal.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthFinal.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthenticationResult> LoginAsync(LoginRequest loginRequest, string ipAddress, string deviceInfo, CancellationToken cancellationToken = default);
        Task<AuthenticationResult> RefreshTokenAsync(string refreshToken, string ipAddress, string deviceInfo, CancellationToken cancellationToken = default);
        Task<bool> LogoutAsync(string sessionId, CancellationToken cancellationToken = default);
        Task<bool> RevokeSessionAsync(string sessionId, string reason, CancellationToken cancellationToken = default);
        Task<bool> RevokeAllUserSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken = default);
    }
}
