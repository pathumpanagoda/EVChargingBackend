/*
 * Author: EV Charging System
 * Date: 2025-09-23
 * Purpose: EV Owner model representing electric vehicle owners in the charging system
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace EVChargingBackend.Models;


/// Represents an electric vehicle owner who can book charging stations

public class EVOwner
{
    
    /// National Identity Card number - primary key for EV owners
    
    [BsonId]
    [BsonElement("nic")]
    [Required]
    [StringLength(12, MinimumLength = 10)]
    public string NIC { get; set; } = string.Empty;

    
    /// Full name of the EV owner
    
    [BsonElement("name")]
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    
    /// Email address for communication
    
    [BsonElement("email")]
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    
    /// Phone number for contact
    
    [BsonElement("phone")]
    [Required]
    [StringLength(15, MinimumLength = 10)]
    public string Phone { get; set; } = string.Empty;

    
    /// Hashed password for authentication
    
    [BsonElement("password_hash")]
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    
    /// Indicates if the EV owner account is active
    
    [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;

    
    /// Timestamp when the EV owner was created
    
    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    
    /// Timestamp when the EV owner was last updated
    
    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}