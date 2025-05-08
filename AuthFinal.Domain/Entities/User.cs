using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthFinal.Domain.Entities
{
    public class User:EntityBase<Guid>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EmailVerified { get; set; } = false;
        public string CodeEmail {  get; set; } = string.Empty;
        public string? Phone {  get; set; } = string.Empty;
        public bool PhoneVerified { get; set; } = false;

        public bool IsActive {  get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        public ICollection<Role> Roles { get; set; } = new HashSet<Role>();
        public ICollection<Session> Sessions { get; set; } = new HashSet<Session>();
    }
}
