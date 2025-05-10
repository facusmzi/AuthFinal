using AuthFinal.Application.Dtos;
using AuthFinal.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthFinal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ITokenService tokenService,
            ILogger<AuthController> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                

                var result = await _authService.LoginAsync(request, ipAddress, userAgent, cancellationToken);

                if (!result.Success)
                {
                    return Unauthorized(new { message = result.Error });
                }

                // Opcionalmente configurar la cookie para el refresh token
                SetRefreshTokenCookie(result.User.RefreshToken);

                return Ok(result.User);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en login");
                return StatusCode(500, new { message = "Error del servidor al procesar la solicitud" });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
        {
            try
            {
                var refreshToken = Request.Cookies["refreshToken"] ?? Request.Headers["X-Refresh-Token"].ToString();

                if (string.IsNullOrEmpty(refreshToken))
                {
                    return BadRequest(new { message = "Token de actualización no proporcionado" });
                }

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress, userAgent, cancellationToken);

                if (!result.Success)
                {
                    return Unauthorized(new { message = result.Error });
                }

                // Actualizar la cookie
                SetRefreshTokenCookie(result.User.RefreshToken);

                return Ok(result.User);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al refrescar token");
                return StatusCode(500, new { message = "Error del servidor al procesar la solicitud" });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            try
            {
                // Extraer sessionId del token JWT
                var sessionId = User.Claims.FirstOrDefault(c => c.Type == "SessionId")?.Value;
                if (string.IsNullOrEmpty(sessionId))
                {
                    return BadRequest(new { message = "Sesión no encontrada en el token" });
                }

                if (string.IsNullOrEmpty(sessionId))
                {
                    return BadRequest(new { message = "Sesión no encontrada en el token" });
                }

                var result = await _authService.LogoutAsync(sessionId, cancellationToken);

                if (!result)
                {
                    return StatusCode(500, new { message = "Error al cerrar sesión" });
                }

                // Eliminar la cookie
                Response.Cookies.Delete("refreshToken");

                return Ok(new { message = "Sesión cerrada correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en logout");
                return StatusCode(500, new { message = "Error del servidor al procesar la solicitud" });
            }
        }

        [Authorize]
        [HttpPost("revoke-session/{sessionId}")]
        public async Task<IActionResult> RevokeSession(string sessionId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _authService.RevokeSessionAsync(sessionId, "Revocado por administrador", cancellationToken);

                if (!result)
                {
                    return NotFound(new { message = "Sesión no encontrada" });
                }

                return Ok(new { message = "Sesión revocada correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al revocar sesión {SessionId}", sessionId);
                return StatusCode(500, new { message = "Error del servidor al procesar la solicitud" });
            }
        }

        [Authorize]
        [HttpPost("revoke-all-sessions/{userId}")]
        public async Task<IActionResult> RevokeAllSessions(Guid userId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _authService.RevokeAllUserSessionsAsync(userId, "Revocado por administrador", cancellationToken);

                if (!result)
                {
                    return StatusCode(500, new { message = "Error al revocar todas las sesiones" });
                }

                return Ok(new { message = "Todas las sesiones revocadas correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al revocar todas las sesiones para usuario {UserId}", userId);
                return StatusCode(500, new { message = "Error del servidor al procesar la solicitud" });
            }
        }

        // Método auxiliar para configurar cookie HTTP-only
        private void SetRefreshTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Solo HTTPS
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(30) // Mismo tiempo que el refresh token
            };

            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }
    }
}
