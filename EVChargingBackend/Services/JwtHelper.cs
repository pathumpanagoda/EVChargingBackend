using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using EVChargingBackend.Models;

namespace EVChargingBackend.Services
{
    public class JwtHelper
    {
        private readonly string _key;
        
        public JwtHelper(IConfiguration cfg) => _key = cfg["Jwt:Key"] ?? "dev-secret-key-change";

        public (string token, DateTime expires) CreateToken(User u)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, u.Username),
                new(ClaimTypes.Role, u.Role)
            };
            if (!string.IsNullOrWhiteSpace(u.Nic)) claims.Add(new("nic", u.Nic));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(1);

            var jwt = new JwtSecurityToken(
                issuer: "EVCharging", 
                audience: "EVClients",
                claims: claims, 
                expires: expires, 
                signingCredentials: creds);

            return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
        }
    }
}
