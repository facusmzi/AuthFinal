using AuthFinal.Application.Interfaces;
using AuthFinal.Infraestructure.ExternalServices.Redis;

namespace AuthFinal.API.Middlewares
{
    public class JwtMiddleware : IMiddleware
    {
        private readonly ITokenService _tokenService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<JwtMiddleware> _logger;

        public JwtMiddleware(
            ITokenService tokenService,
            ICacheService cacheService,
            ILogger<JwtMiddleware> logger)
        {
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var path = context.Request.Path.Value?.ToLower();

            // Permitir acceso libre a rutas de autenticación y otras públicas
            if (path != null && (
                path.StartsWith("/api/auth/login") ||
                path.StartsWith("/api/auth/refresh-token") ||
                path.StartsWith("/api/public") ||
                path.StartsWith("/swagger")))
            {
                await next(context);
                return;
            }

            var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (string.IsNullOrEmpty(token))
            {
                await next(context);
                return;
            }

            try
            {
                // Validar firma del token (verificación rápida sin BD)
                var principal = await _tokenService.ValidateAccessTokenAsync(token);
                if (principal == null)
                {
                    // No establecer código 401 aquí - dejamos que el middleware de autenticación lo maneje
                    await next(context);
                    return;
                }

                // Extraer sessionId del token
                var sessionId = principal.Claims.FirstOrDefault(c => c.Type == "SessionId")?.Value;
                if (string.IsNullOrEmpty(sessionId))
                {
                    await next(context);
                    return;
                }

                // Verificar en Redis si la sesión sigue activa (sin ir a BD)
                if (!await _cacheService.ExistsAsync(sessionId))
                {
                    _logger.LogWarning("Token con SessionId {SessionId} ya no tiene una sesión activa en cache", sessionId);
                    await next(context);
                    return;
                }

                // Opcional: actualizar último acceso para auditoría (pero no en cada request)
                // Implementar alguna lógica para hacerlo con menor frecuencia (ej. una vez cada 10-15 min)
                // await _sessionRepository.UpdateLastActiveAsync(sessionId);

                // Todo en orden, continuar al siguiente middleware
                await next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando token JWT");
                await next(context);
            }
        }
    }
}
