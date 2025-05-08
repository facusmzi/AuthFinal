using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthFinal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthFinal.Infraestructure.Data
{
    public class AppDbContext: DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Session> Sessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuración de la relación muchos a muchos entre User y Role
            modelBuilder.Entity<User>()
            .HasMany(u => u.Roles)
            .WithMany(r => r.users);

            // Configuración de la relación entre Session y RefreshToken (1:1)
            modelBuilder.Entity<Session>()
                .HasOne(s => s.RefreshToken)
                .WithOne(rt => rt.Session)
                .HasForeignKey<RefreshToken>(rt => rt.SessionId);

            // Configuración de la relación entre User y Session (1:N)
            modelBuilder.Entity<Session>()
                .HasOne(s => s.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(s => s.UserId);
        }
    }
}
