/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Charging Station model representing EV charging infrastructure
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace EVChargingBackend.Models;

/// <summary>
/// Represents a charging station with location, capacity, and scheduling information
/// </summary>
public class ChargingStation
{
    /// <summary>
    /// Unique identifier for the charging station
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Name of the charging station
    /// </summary>
    [BsonElement("name")]
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Geographic location of the charging station
    /// </summary>
    [BsonElement("location")]
    [Required]
    public Location Location { get; set; } = new();

    /// <summary>
    /// Type of charging station (AC or DC)
    /// </summary>
    [BsonElement("type")]
    [Required]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Total number of charging slots available
    /// </summary>
    [BsonElement("total_slots")]
    [Range(1, 50)]
    public int TotalSlots { get; set; }

    /// <summary>
    /// Number of currently available slots
    /// </summary>
    [BsonElement("available_slots")]
    [Range(0, 50)]
    public int AvailableSlots { get; set; }

    /// <summary>
    /// Daily schedule for the charging station
    /// </summary>
    [BsonElement("schedule")]
    public List<DailySchedule> Schedule { get; set; } = new();

    /// <summary>
    /// Indicates if the charging station is active
    /// </summary>
    [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// ID of the operator managing this station
    /// </summary>
    [BsonElement("operator_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string OperatorId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the charging station was created
    /// </summary>
    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the geographic location of a charging station
/// </summary>
public class Location
{
    /// <summary>
    /// Latitude coordinate
    /// </summary>
    [BsonElement("latitude")]
    [Required]
    [Range(-90, 90)]
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude coordinate
    /// </summary>
    [BsonElement("longitude")]
    [Required]
    [Range(-180, 180)]
    public double Longitude { get; set; }

    /// <summary>
    /// Human-readable address
    /// </summary>
    [BsonElement("address")]
    [Required]
    [StringLength(200, MinimumLength = 10)]
    public string Address { get; set; } = string.Empty;
}

/// <summary>
/// Represents the daily operating schedule for a charging station
/// </summary>
public class DailySchedule
{
    /// <summary>
    /// Date for this schedule entry
    /// </summary>
    [BsonElement("date")]
    [Required]
    public DateTime Date { get; set; }

    /// <summary>
    /// Opening time for the day
    /// </summary>
    [BsonElement("open")]
    [Required]
    public TimeSpan Open { get; set; }

    /// <summary>
    /// Closing time for the day
    /// </summary>
    [BsonElement("close")]
    [Required]
    public TimeSpan Close { get; set; }

    /// <summary>
    /// Number of slots available on this day
    /// </summary>
    [BsonElement("slots_available")]
    [Range(0, 50)]
    public int SlotsAvailable { get; set; }
}