/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: JWT token generation and validation helper
 */

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EVChargingBackend.Helpers;

/// <summary>
/// Helper class for JWT token operations
/// </summary>
public class JwtHelper
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly string _key;
    private readonly int _expirationMinutes;

    /// <summary>
    /// Initializes a new instance of the JwtHelper
    /// </summary>
    /// <param name="issuer">JWT issuer</param>
    /// <param name="audience">JWT audience</param>
    /// <param name="key">JWT signing key</param>
    /// <param name="expirationMinutes">Token expiration time in minutes</param>
    public JwtHelper(string issuer, string audience, string key, int expirationMinutes = 60)
    {
        _issuer = issuer;
        _audience = audience;
        _key = key;
        _expirationMinutes = expirationMinutes;
    }

    /// <summary>
    /// Generates a JWT token for a user
    /// </summary>
    /// <param name="userId">User ID or NIC</param>
    /// <param name="role">User role</param>
    /// <param name="additionalClaims">Additional claims to include</param>
    /// <returns>JWT token and expiration time</returns>
    public (string Token, DateTime ExpiresAt) GenerateToken(string userId, string role, Dictionary<string, string>? additionalClaims = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add additional claims if provided
        if (additionalClaims != null)
        {
            foreach (var claim in additionalClaims)
            {
                claims.Add(new Claim(claim.Key, claim.Value));
            }
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(_expirationMinutes);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expiresAt);
    }

    /// <summary>
    /// Generates a JWT token for an EV owner
    /// </summary>
    /// <param name="nic">EV owner NIC</param>
    /// <param name="name">EV owner name</param>
    /// <returns>JWT token and expiration time</returns>
    public (string Token, DateTime ExpiresAt) GenerateTokenForEVOwner(string nic, string name)
    {
        var additionalClaims = new Dictionary<string, string>
        {
            { "nic", nic },
            { "name", name }
        };

        return GenerateToken(nic, "EVOwner", additionalClaims);
    }

    /// <summary>
    /// Generates a JWT token for a system user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="username">Username</param>
    /// <param name="role">User role</param>
    /// <returns>JWT token and expiration time</returns>
    public (string Token, DateTime ExpiresAt) GenerateTokenForUser(string userId, string username, string role)
    {
        var additionalClaims = new Dictionary<string, string>
        {
            { "username", username }
        };

        return GenerateToken(userId, role, additionalClaims);
    }

    /// <summary>
    /// Validates a JWT token
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>Claims principal if valid, null otherwise</returns>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts user ID from a JWT token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User ID if found, null otherwise</returns>
    public string? GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        return principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Extracts role from a JWT token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Role if found, null otherwise</returns>
    public string? GetRoleFromToken(string token)
    {
        var principal = ValidateToken(token);
        return principal?.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Extracts NIC from a JWT token (for EV owners)
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>NIC if found, null otherwise</returns>
    public string? GetNICFromToken(string token)
    {
        var principal = ValidateToken(token);
        return principal?.FindFirst("nic")?.Value;
    }
}
