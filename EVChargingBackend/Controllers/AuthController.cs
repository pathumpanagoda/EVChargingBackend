using Microsoft.AspNetCore.Mvc;
using EVChargingBackend.Services;
using EVChargingBackend.Models.Auth;
using EVChargingBackend.Data;
using EVChargingBackend.Models;
using MongoDB.Driver;

namespace EVChargingBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserStore _store;
        private readonly JwtHelper _jwt;
        private readonly MongoDbContext _mongoContext;

        public AuthController(UserStore store, JwtHelper jwt, MongoDbContext mongoContext)
        {
            _store = store;
            _jwt = jwt;
            _mongoContext = mongoContext;
        }

        [HttpGet("test")]
        public ActionResult Test()
        {
            return Ok(new { 
                userCount = _store.Users.Count,
                users = _store.Users.Select(u => new { u.Username, u.Role, u.IsActive }).ToList()
            });
        }

        [HttpGet("evowners")]
        public async Task<ActionResult> GetEVOwners()
        {
            try
            {
                var evOwners = await _mongoContext.EVOwners.Find(Builders<EVOwner>.Filter.Empty).ToListAsync();
                return Ok(new { 
                    count = evOwners.Count,
                    evOwners = evOwners.Select(e => new { 
                        e.NIC, 
                        e.Name, 
                        e.Email, 
                        e.Phone, 
                        e.IsActive, 
                        e.CreatedAt
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("create-test-user")]
        public async Task<ActionResult> CreateTestUser()
        {
            try
            {
                var testUser = new EVOwner
                {
                    NIC = "999999999V",
                    Name = "Test User",
                    Email = "test@example.com",
                    Phone = "0771234567",
                    PasswordHash = PasswordHasher.Hash("Password123"),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _mongoContext.EVOwners.InsertOneAsync(testUser);
                return Ok(new { message = "Test user created successfully", nic = testUser.NIC, password = "Password123" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("reset-password/{nic}")]
        public async Task<ActionResult> ResetPassword(string nic, [FromBody] string newPassword)
        {
            try
            {
                var filter = Builders<EVOwner>.Filter.Eq(e => e.NIC, nic);
                var update = Builders<EVOwner>.Update
                    .Set(e => e.PasswordHash, PasswordHasher.Hash(newPassword))
                    .Set(e => e.UpdatedAt, DateTime.UtcNow);

                var result = await _mongoContext.EVOwners.UpdateOneAsync(filter, update);
                
                if (result.MatchedCount == 0)
                {
                    return NotFound(new { error = "EV owner not found" });
                }

                return Ok(new { message = "Password reset successfully", nic = nic, newPassword = newPassword });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
        {
            Console.WriteLine($"Login attempt for username: {req.Username}");
            
            try
            {
                // Search for EV owner by NIC in MongoDB database only
                var evOwner = await _mongoContext.EVOwners
                    .Find(Builders<EVOwner>.Filter.Eq(e => e.NIC, req.Username))
                    .FirstOrDefaultAsync();
                
                if (evOwner == null || !evOwner.IsActive)
                {
                    Console.WriteLine($"EV owner not found or inactive: {req.Username}");
                    return Unauthorized(new LoginResponse { Success = false, Error = "Invalid credentials" });
                }
                
                Console.WriteLine($"Found EV owner in MongoDB: {evOwner.Name} ({evOwner.NIC})");
                Console.WriteLine($"Password hash from DB: {evOwner.PasswordHash}");
                
                // Verify password using the stored hash
                var passwordValid = PasswordHasher.Verify(req.Password, evOwner.PasswordHash);
                Console.WriteLine($"Password valid: {passwordValid}");
                
                if (!passwordValid)
                {
                    Console.WriteLine($"Password mismatch for EV owner: {evOwner.NIC}");
                    return Unauthorized(new LoginResponse { Success = false, Error = "Invalid credentials" });
                }
                
                // Create a User object for JWT token generation
                var user = new User
                {
                    Id = evOwner.NIC,
                    Username = evOwner.Name,
                    Nic = evOwner.NIC,
                    Role = "EVOwner",
                    IsActive = evOwner.IsActive,
                    PasswordHash = evOwner.PasswordHash
                };
                
                var (token, exp) = _jwt.CreateToken(user);
                return Ok(new LoginResponse
                {
                    Success = true,
                    Data = new TokenData
                    {
                        Token = token,
                        Role = user.Role,
                        UserId = user.Id,
                        Nic = user.Nic,
                        ExpiresAt = exp.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during login: {ex.Message}");
                return Unauthorized(new LoginResponse { Success = false, Error = "Invalid credentials" });
            }
        }
    }
}