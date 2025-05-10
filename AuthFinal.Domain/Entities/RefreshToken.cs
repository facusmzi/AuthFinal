using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace AuthFinal.Domain.Entities
{
    public class RefreshToken:EntityBase<Guid>
    {
        public string Token { get; set; }
        public DateTime ExpirationDate { get; set; }
        public bool IsRevoked { get; set; }
        public string? ReplacedByToken { get; set; }
        public string? ReasonRevoked { get; set; }

        // Relación con Session
        public Guid SessionId { get; set; }
        public Session? Session { get; set; }
    }
}
