/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Charging Station model representing EV charging infrastructure
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace EVChargingBackend.Models;


/// Represents a charging station with location, capacity, and scheduling information

public class ChargingStation
{
    
    /// Unique identifier for the charging station
    
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    
    /// Name of the charging station
    
    [BsonElement("name")]
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    
    /// Geographic location of the charging station
    
    [BsonElement("location")]
    [Required]
    public Location Location { get; set; } = new();

    
    /// Type of charging station (AC or DC)
    
    [BsonElement("type")]
    [Required]
    public string Type { get; set; } = string.Empty;

    
    /// Total number of charging slots available
    
    [BsonElement("total_slots")]
    [Range(1, 50)]
    public int TotalSlots { get; set; }

    
    /// Number of currently available slots
    
    [BsonElement("available_slots")]
    [Range(0, 50)]
    public int AvailableSlots { get; set; }

    
    /// Daily schedule for the charging station
    
    [BsonElement("schedule")]
    public List<DailySchedule> Schedule { get; set; } = new();

    
    /// Indicates if the charging station is active
    
    [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;

    
    /// ID of the operator managing this station
    
    [BsonElement("operator_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string OperatorId { get; set; } = string.Empty;

    
    /// Timestamp when the charging station was created
    
    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


/// Represents the geographic location of a charging station

public class Location
{
    
    /// Latitude coordinate
    
    [BsonElement("latitude")]
    [Required]
    [Range(-90, 90)]
    public double Latitude { get; set; }

    
    /// Longitude coordinate
    
    [BsonElement("longitude")]
    [Required]
    [Range(-180, 180)]
    public double Longitude { get; set; }

    
    /// Human-readable address
    
    [BsonElement("address")]
    [Required]
    [StringLength(200, MinimumLength = 10)]
    public string Address { get; set; } = string.Empty;
}


/// Represents the daily operating schedule for a charging station

public class DailySchedule
{
    
    /// Date for this schedule entry
    
    [BsonElement("date")]
    [Required]
    public DateTime Date { get; set; }

    
    /// Opening time for the day
    
    [BsonElement("open")]
    [Required]
    public TimeSpan Open { get; set; }

    
    /// Closing time for the day
    
    [BsonElement("close")]
    [Required]
    public TimeSpan Close { get; set; }

    
    /// Number of slots available on this day
    
    [BsonElement("slots_available")]
    [Range(0, 50)]
    public int SlotsAvailable { get; set; }
}