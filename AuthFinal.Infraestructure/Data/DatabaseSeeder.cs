using AuthFinal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuthFinal.Infraestructure.Data
{
    public static class DatabaseSeeder
    {
        /// <summary>
        /// Seeds the database with initial test data
        /// </summary>
        public static async Task SeedDatabase(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

                try
                {
                    logger.LogInformation("Starting database seeding...");

                    // Apply any pending migrations
                    await dbContext.Database.MigrateAsync();

                    // Seed roles if they don't exist
                    await SeedRoles(dbContext, logger);

                    // Seed users if they don't exist
                    await SeedUsers(dbContext, logger);

                    logger.LogInformation("Database seeding completed successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while seeding the database.");
                    throw;
                }
            }
        }

        private static async Task SeedRoles(AppDbContext dbContext, ILogger logger)
        {
            if (!await dbContext.Roles.AnyAsync())
            {
                logger.LogInformation("Seeding roles...");

                var roles = new List<Role>
                {
                    new Role
                    {
                        Id = Guid.NewGuid(),
                        Name = "Admin",
                        Description = "Administrator with full access to all features"
                    },
                    new Role
                    {
                        Id = Guid.NewGuid(),
                        Name = "User",
                        Description = "Standard user with limited access"
                    }
                };

                await dbContext.Roles.AddRangeAsync(roles);
                await dbContext.SaveChangesAsync();

                logger.LogInformation("Roles seeded successfully.");
            }
            else
            {
                logger.LogInformation("Roles already exist in the database. Skipping role seeding.");
            }
        }

        private static async Task SeedUsers(AppDbContext dbContext, ILogger logger)
        {
            if (!await dbContext.Users.AnyAsync())
            {
                logger.LogInformation("Seeding users...");

                // Get roles to assign to users
                var adminRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                var userRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "User");

                if (adminRole == null || userRole == null)
                {
                    logger.LogWarning("Roles not found. Please ensure roles are seeded before users.");
                    return;
                }

                // Create admin user
                var adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Admin",
                    LastName = "User",
                    Email = "admin@example.com",
                    Password = HashPassword("Admin123!"),
                    EmailVerified = true,
                    IsActive = true,
                    Phone = "+1234567890"
                };
                adminUser.Roles.Add(adminRole);

                // Create standard user
                var standardUser = new User
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Standard",
                    LastName = "User",
                    Email = "user@example.com",
                    Password = HashPassword("User123!"),
                    EmailVerified = true,
                    IsActive = true,
                    Phone = "+0987654321"
                };
                standardUser.Roles.Add(userRole);

                await dbContext.Users.AddRangeAsync(new[] { adminUser, standardUser });
                await dbContext.SaveChangesAsync();

                logger.LogInformation("Users seeded successfully.");
            }
            else
            {
                logger.LogInformation("Users already exist in the database. Skipping user seeding.");
            }
        }

        /// <summary>
        /// Hashes a password using SHA256
        /// Note: In a production environment, you should use a more secure password hashing algorithm like BCrypt
        /// </summary>
        private static string HashPassword(string password)
        {
            // This is a simple hash for testing purposes
            // In a real application, use a proper password hashing library
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
