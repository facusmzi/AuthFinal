using AuthFinal.Application.Dtos;
using AuthFinal.Application.Interfaces;
using AuthFinal.Domain.Entities;
using AuthFinal.Domain.Interfaces;
using AuthFinal.Infraestructure.ExternalServices.Redis;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuthFinal.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ITokenService _tokenService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            ISessionRepository sessionRepository,
            IRefreshTokenRepository refreshTokenRepository,
            ITokenService tokenService,
            ICacheService cacheService,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _tokenService = tokenService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<AuthenticationResult> LoginAsync(LoginRequest loginRequest, string ipAddress, string deviceInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validar credenciales
                var user = await _userRepository.GetByEmailAsync(loginRequest.Email, cancellationToken: cancellationToken);
                if (user == null || !VerifyPassword(loginRequest.Password, user.Password))
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        Error = "Credenciales inválidas"
                    };
                }

                if (!user.IsActive)
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        Error = "La cuenta está desactivada"
                    };
                }

                // Cargar roles del usuario para incluirlos en la respuesta
                var userWithRoles = await _userRepository.GetUserWithRolesAsync(user.Id, cancellationToken: cancellationToken);

                // Crear un ID de sesión único
                var sessionId = Guid.NewGuid().ToString();

                // Generar tokens
                var tokenResponse = await _tokenService.GenerateTokensAsync(user, sessionId, cancellationToken);

                // Crear la sesión en la base de datos
                var session = new Session
                {
                    SessionId = sessionId,
                    UserId = user.Id,
                    IpAddress = ipAddress,
                    DeviceInfo = deviceInfo,
                    Location = GetLocationFromIp(ipAddress), // Implementar o usar servicio de geolocalización
                    LastActive = DateTime.UtcNow,
                    ExpiresAt = tokenResponse.RefreshTokenExpiration,
                    IsRevoked = false
                };

                var createdSession = await _sessionRepository.AddAsync(session, cancellationToken);

                // Crear refresh token en la base de datos
                var refreshToken = new RefreshToken
                {
                    Token = tokenResponse.RefreshToken,
                    ExpirationDate = tokenResponse.RefreshTokenExpiration,
                    SessionId = createdSession.Id,
                    IsRevoked = false
                };

                await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

                // Preparar respuesta con información del usuario
                var roles = userWithRoles?.Roles.Select(r => r.Name).ToList() ?? new List<string>();

                return new AuthenticationResult
                {
                    Success = true,
                    User = new LoginResponse
                    {
                        UserId = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        Roles = roles,
                        AccessToken = tokenResponse.AccessToken,
                        RefreshToken = tokenResponse.RefreshToken,
                        AccessTokenExpiration = tokenResponse.AccessTokenExpiration,
                        RefreshTokenExpiration = tokenResponse.RefreshTokenExpiration
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email {Email}", loginRequest.Email);
                return new AuthenticationResult
                {
                    Success = false,
                    Error = "Error interno del servidor"
                };
            }
        }

        public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken, string ipAddress, string deviceInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                // Buscar el refresh token en la base de datos
                var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken);
                if (storedToken == null || storedToken.IsRevoked || DateTime.UtcNow >= storedToken.ExpirationDate)
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        Error = "Token de actualización inválido o expirado"
                    };
                }

                // Obtener la sesión asociada
                var session = await _sessionRepository.GetByIdAsync(storedToken.SessionId, cancellationToken);
                if (session == null || session.IsRevoked || session.IsExpired)
                {
                    // Revocar token si la sesión ya no es válida
                    await _refreshTokenRepository.RevokeRefreshTokenAsync(refreshToken, "Sesión inválida o expirada", null, cancellationToken);
                    return new AuthenticationResult
                    {
                        Success = false,
                        Error = "Sesión inválida o expirada"
                    };
                }

                // Obtener usuario
                var user = await _userRepository.GetUserWithRolesAsync(session.UserId, cancellationToken: cancellationToken);
                if (user == null || !user.IsActive)
                {
                    await _refreshTokenRepository.RevokeRefreshTokenAsync(refreshToken, "Usuario inválido o inactivo", null, cancellationToken);
                    return new AuthenticationResult
                    {
                        Success = false,
                        Error = "Usuario inválido o inactivo"
                    };
                }

                // Generar nuevos tokens
                var tokenResponse = await _tokenService.GenerateTokensAsync(user, session.SessionId, cancellationToken);

                // Revocar token anterior
                await _refreshTokenRepository.RevokeRefreshTokenAsync(refreshToken, "Reemplazado por nuevo token", tokenResponse.RefreshToken, cancellationToken);

                // Crear nuevo refresh token
                var newRefreshToken = new RefreshToken
                {
                    Token = tokenResponse.RefreshToken,
                    ExpirationDate = tokenResponse.RefreshTokenExpiration,
                    SessionId = session.Id,
                    IsRevoked = false
                };

                await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

                // Actualizar la sesión
                session.LastActive = DateTime.UtcNow;
                session.IpAddress = ipAddress;
                session.DeviceInfo = deviceInfo;
                session.ExpiresAt = tokenResponse.RefreshTokenExpiration;
                await _sessionRepository.UpdateAsync(session, cancellationToken);

                // Preparar respuesta
                var roles = user.Roles.Select(r => r.Name).ToList();

                return new AuthenticationResult
                {
                    Success = true,
                    User = new LoginResponse
                    {
                        UserId = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        Roles = roles,
                        AccessToken = tokenResponse.AccessToken,
                        RefreshToken = tokenResponse.RefreshToken,
                        AccessTokenExpiration = tokenResponse.AccessTokenExpiration,
                        RefreshTokenExpiration = tokenResponse.RefreshTokenExpiration
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return new AuthenticationResult
                {
                    Success = false,
                    Error = "Error interno del servidor"
                };
            }
        }

        public async Task<bool> LogoutAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Revocar la sesión en la base de datos
                await _sessionRepository.RevokeSessionAsync(sessionId, "Logout por usuario", cancellationToken);

                // Eliminar la sesión de Redis
                await _tokenService.RevokeTokenAsync(sessionId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<bool> RevokeSessionAsync(string sessionId, string reason, CancellationToken cancellationToken = default)
        {
            try
            {
                // Obtener la sesión
                var session = await _sessionRepository.GetSessionWithRefreshTokenAsync(sessionId, cancellationToken);
                if (session == null)
                {
                    return false;
                }

                // Revocar el refresh token si existe
                if (session.RefreshToken != null)
                {
                    await _refreshTokenRepository.RevokeRefreshTokenAsync(session.RefreshToken.Token, reason, null, cancellationToken);
                }

                // Revocar la sesión
                await _sessionRepository.RevokeSessionAsync(sessionId, reason, cancellationToken);

                // Eliminar sesión de Redis
                await _tokenService.RevokeTokenAsync(sessionId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<bool> RevokeAllUserSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
        {
            try
            {
                // Obtener todas las sesiones activas del usuario
                var sessions = await _sessionRepository.GetActiveSessionsByUserIdAsync(userId, cancellationToken);

                // Revocar cada sesión en Redis
                foreach (var session in sessions)
                {
                    await _tokenService.RevokeTokenAsync(session.SessionId);
                }

                // Revocar todas las sesiones en la base de datos
                await _sessionRepository.RevokeAllUserSessionsAsync(userId, reason, cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking all sessions for user {UserId}", userId);
                return false;
            }
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            // Implementación de verificación de contraseña
            // Asumiendo que usas hash+salt, deberás adaptar esto a tu método específico
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes("your-salt-key"));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            // Comparación segura contra timing attacks
            return CryptographicOperations.FixedTimeEquals(
                computedHash,
                Convert.FromBase64String(storedHash));
        }

        private string GetLocationFromIp(string ipAddress)
        {
            // Implementar o utilizar un servicio de geolocalización
            // Esto es un placeholder - reemplazar con implementación real
            return "Unknown";
        }
    }   }
