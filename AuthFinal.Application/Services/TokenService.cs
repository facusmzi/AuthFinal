using AuthFinal.Application.Interfaces;
using AuthFinal.Domain.Entities;
using AuthFinal.Infraestructure.ExternalServices.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuthFinal.Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cacheService;
        private readonly ILogger<TokenService> _logger;

        public TokenService(
            IConfiguration configuration,
            ICacheService cacheService,
            ILogger<TokenService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TokenResponse> GenerateTokensAsync(User user, string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Configuración para Access Token (corta duración)
                var accessTokenExpiration = DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(_configuration["Authentication:AccessTokenExpirationMinutes"] ?? "15"));

                // Configuración para Refresh Token (larga duración)
                var refreshTokenExpiration = DateTime.UtcNow.AddDays(
                    Convert.ToDouble(_configuration["Authentication:RefreshTokenExpirationDays"] ?? "90"));

                // Crear JWT
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Authentication:SecretKey"] ??
                                                throw new InvalidOperationException("Secret key not configured"));

                // Solo incluimos el sessionId como claim - no datos sensibles
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, user.Id.ToString()),
                        new Claim("SessionId", sessionId),
                    }),
                    Expires = accessTokenExpiration,
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var accessToken = tokenHandler.WriteToken(token);

                // Generar refresh token
                var refreshToken = GenerateRefreshToken();

                // Almacenar sesión en Redis (para validación rápida sin BD)
                // Solo guardamos lo mínimo que necesitamos para validar
                var cacheSessionData = new
                {
                    UserId = user.Id,
                    IsActive = true,
                    SessionId = sessionId,
                };

                // Guardar en Redis con TTL igual al accessToken para optimizar validaciones
                var accessTokenTTL = accessTokenExpiration.Subtract(DateTime.UtcNow);
                await _cacheService.SetAsync(sessionId, cacheSessionData, accessTokenTTL);

                return new TokenResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    AccessTokenExpiration = accessTokenExpiration,
                    RefreshTokenExpiration = refreshTokenExpiration
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tokens for user {UserId}", user.Id);
                throw;
            }
        }

        public async Task<ClaimsPrincipal?> ValidateAccessTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Authentication:SecretKey"] ??
                                                 throw new InvalidOperationException("Secret key not configured"));

                // Solo validamos firma - no consultamos BD
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                var sessionId = principal.FindFirst("SessionId")?.Value;

                // Verificar si la sesión está en Redis (si no está, significa que fue revocada)
                if (sessionId != null && !await _cacheService.ExistsAsync(sessionId))
                {
                    return null; // Sesión no encontrada o revocada
                }

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating access token");
                return null;
            }
        }

        public async Task<string> ExtractSessionIdFromTokenAsync(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(x => x.Type == "SessionId")?.Value ?? string.Empty;
        }

        public async Task<bool> IsTokenRevokedAsync(string sessionId)
        {
            try
            {
                return !await _cacheService.ExistsAsync(sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if token is revoked for session {SessionId}", sessionId);
                return true; // Si hay error, consideramos como revocado por seguridad
            }
        }

        public async Task RevokeTokenAsync(string sessionId)
        {
            try
            {
                await _cacheService.RemoveAsync(sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token for session {SessionId}", sessionId);
                throw;
            }
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
