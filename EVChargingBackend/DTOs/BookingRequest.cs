/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Booking request DTO for charging station reservations
 */

using System.ComponentModel.DataAnnotations;

namespace EVChargingBackend.DTOs;

/// <summary>
/// Request DTO for creating a new booking
/// </summary>
public record BookingRequest
{
    /// <summary>
    /// ID of the charging station to book
    /// </summary>
    [Required(ErrorMessage = "Station ID is required")]
    public string StationId { get; init; } = string.Empty;

    /// <summary>
    /// Date and time for the charging reservation
    /// </summary>
    [Required(ErrorMessage = "Reservation date and time is required")]
    public DateTime ReservationDateTime { get; init; }
}

/// <summary>
/// Request DTO for updating an existing booking
/// </summary>
public record BookingUpdateRequest
{
    /// <summary>
    /// New date and time for the charging reservation
    /// </summary>
    [Required(ErrorMessage = "Reservation date and time is required")]
    public DateTime ReservationDateTime { get; init; }
}

/// <summary>
/// Request DTO for completing a booking via QR scan
/// </summary>
public record BookingCompleteRequest
{
    /// <summary>
    /// QR payload string for verification
    /// </summary>
    [Required(ErrorMessage = "QR payload is required")]
    public string QRPayload { get; init; } = string.Empty;
}