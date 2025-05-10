using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthFinal.Domain.Entities
{
    public class Session:EntityBase<Guid>
    {
        public string SessionId { get; set; } // Identificador único de sesión
        public DateTime LastActive { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string IpAddress { get; set; }
        public string DeviceInfo { get; set; }
        public string Location { get; set; } // Opcional, podría implementarse con un servicio de geolocalización
        public bool IsRevoked { get; set; }
        public string? ReasonRevoked { get; set; }

        // Relación con el usuario
        public Guid UserId { get; set; }
        public User User { get; set; }

        // Relación con refresh token
        public RefreshToken RefreshToken { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}
