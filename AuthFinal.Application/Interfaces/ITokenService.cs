using AuthFinal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace AuthFinal.Application.Interfaces
{
    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiration { get; set; }
        public DateTime RefreshTokenExpiration { get; set; }
    }

    public interface ITokenService
    {
        Task<TokenResponse> GenerateTokensAsync(User user, string sessionId, CancellationToken cancellationToken = default);
        Task<ClaimsPrincipal?> ValidateAccessTokenAsync(string token);
        Task<string> ExtractSessionIdFromTokenAsync(string token);
        Task<bool> IsTokenRevokedAsync(string sessionId);
        Task RevokeTokenAsync(string sessionId);
    }
}
