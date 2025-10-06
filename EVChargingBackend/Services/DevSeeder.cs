using EVChargingBackend.Models;

namespace EVChargingBackend.Services
{
    public static class DevSeeder
    {
        public static void Seed(List<User> users)
        {
            // Check if owner user already exists
            if (users.Any(u => u.Username == "owner"))
                return;

            // Add the dev user
            users.Add(new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = "owner",
                PasswordHash = PasswordHasher.Hash("owner123"),
                Role = "EVOwner",
                Nic = "123456789V",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
