/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: User model for authentication and authorization in the EV Charging System
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace EVChargingBackend.Models;

/// <summary>
/// Represents a system user with authentication and authorization capabilities
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Unique username for login
    /// </summary>
    [BsonElement("username")]
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Hashed password for authentication
    /// </summary>
    [BsonElement("password_hash")]
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// User role determining access permissions
    /// </summary>
    [BsonElement("role")]
    [Required]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the user account is active
    /// </summary>
    [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp when the user was created
    /// </summary>
    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}