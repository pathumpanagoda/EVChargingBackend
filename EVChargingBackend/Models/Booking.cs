/*
 * Author: EV Charging System
 * Date: 2025-09-23
 * Purpose: Booking model representing EV charging station reservations
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace EVChargingBackend.Models;


/// Represents a booking for an EV charging station

public class Booking
{
    
    /// Unique identifier for the booking
    
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    
    /// NIC of the EV owner who made the booking
    
    [BsonElement("ev_owner_nic")]
    [Required]
    [StringLength(12, MinimumLength = 10)]
    public string EVOwnerNIC { get; set; } = string.Empty;

    
    /// ID of the charging station being booked
    
    [BsonElement("station_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    [Required]
    public string StationId { get; set; } = string.Empty;

    
    /// Date when the booking was made
    
    [BsonElement("booking_date")]
    [Required]
    public DateTime BookingDate { get; set; } = DateTime.UtcNow;

    
    /// Date and time of the actual charging reservation
    
    [BsonElement("reservation_datetime")]
    [Required]
    public DateTime ReservationDateTime { get; set; }

    
    /// Current status of the booking
    
    [BsonElement("status")]
    [Required]
    public string Status { get; set; } = "Pending";

    
    /// QR code payload for booking verification
    
    [BsonElement("qr_payload")]
    public string? QRPayload { get; set; }

    
    /// Timestamp when the booking was created
    
    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    
    /// Timestamp when the booking was last updated
    
    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}


/// Enumeration of possible booking statuses

public static class BookingStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}