/*
 * Author: EV Charging System
 * Date: 2025-09-23
 * Purpose: User model for authentication and authorization in the EV Charging System
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace EVChargingBackend.Models;


/// Represents a system user with authentication and authorization capabilities

public class User
{
    
    /// Unique identifier for the user
    
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    
    /// Unique username for login
    
    [BsonElement("username")]
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    
    /// Hashed password for authentication
    
    [BsonElement("password_hash")]
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    
    /// User role determining access permissions
    
    [BsonElement("role")]
    [Required]
    public string Role { get; set; } = string.Empty;

    
    /// Indicates if the user account is active
    
    [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;

    
    /// Timestamp when the user was created
    
    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}