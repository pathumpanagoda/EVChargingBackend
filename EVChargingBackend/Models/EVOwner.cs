/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: EV Owner model representing electric vehicle owners in the charging system
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace EVChargingBackend.Models;

/// <summary>
/// Represents an electric vehicle owner who can book charging stations
/// </summary>
public class EVOwner
{
    /// <summary>
    /// National Identity Card number - primary key for EV owners
    /// </summary>
    [BsonId]
    [BsonElement("nic")]
    [Required]
    [StringLength(12, MinimumLength = 10)]
    public string NIC { get; set; } = string.Empty;

    /// <summary>
    /// Full name of the EV owner
    /// </summary>
    [BsonElement("name")]
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email address for communication
    /// </summary>
    [BsonElement("email")]
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Phone number for contact
    /// </summary>
    [BsonElement("phone")]
    [Required]
    [StringLength(15, MinimumLength = 10)]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Hashed password for authentication
    /// </summary>
    [BsonElement("password_hash")]
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the EV owner account is active
    /// </summary>
    [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp when the EV owner was created
    /// </summary>
    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the EV owner was last updated
    /// </summary>
    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}