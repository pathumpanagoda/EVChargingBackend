/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Booking model representing EV charging station reservations
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace EVChargingBackend.Models;

/// <summary>
/// Represents a booking for an EV charging station
/// </summary>
public class Booking
{
    /// <summary>
    /// Unique identifier for the booking
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// NIC of the EV owner who made the booking
    /// </summary>
    [BsonElement("ev_owner_nic")]
    [Required]
    [StringLength(12, MinimumLength = 10)]
    public string EVOwnerNIC { get; set; } = string.Empty;

    /// <summary>
    /// ID of the charging station being booked
    /// </summary>
    [BsonElement("station_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    [Required]
    public string StationId { get; set; } = string.Empty;

    /// <summary>
    /// Date when the booking was made
    /// </summary>
    [BsonElement("booking_date")]
    [Required]
    public DateTime BookingDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time of the actual charging reservation
    /// </summary>
    [BsonElement("reservation_datetime")]
    [Required]
    public DateTime ReservationDateTime { get; set; }

    /// <summary>
    /// Current status of the booking
    /// </summary>
    [BsonElement("status")]
    [Required]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// QR code payload for booking verification
    /// </summary>
    [BsonElement("qr_payload")]
    public string? QRPayload { get; set; }

    /// <summary>
    /// Timestamp when the booking was created
    /// </summary>
    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the booking was last updated
    /// </summary>
    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Enumeration of possible booking statuses
/// </summary>
public static class BookingStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}