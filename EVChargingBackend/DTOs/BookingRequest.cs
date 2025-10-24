/*
 * Author: EV Charging System
 * Date: 2025-09-23
 * Purpose: Booking request DTO for charging station reservations
 */

using System.ComponentModel.DataAnnotations;

namespace EVChargingBackend.DTOs;


/// Request DTO for creating a new booking

public record BookingRequest
{
    
    /// ID of the charging station to book
    
    [Required(ErrorMessage = "Station ID is required")]
    public string StationId { get; init; } = string.Empty;

    
    /// Start date and time for the charging reservation
    
    [Required(ErrorMessage = "Reservation start date and time is required")]
    public DateTime ReservationDateTime { get; init; }

    
    /// End date and time for the charging reservation (optional - defaults to start + 1 hour)
    
    public DateTime? EndDateTime { get; init; }
}


/// Request DTO for updating an existing booking

public record BookingUpdateRequest
{
    
    /// New start date and time for the charging reservation
    
    [Required(ErrorMessage = "Reservation start date and time is required")]
    public DateTime ReservationDateTime { get; init; }

    
    /// New end date and time for the charging reservation (optional - defaults to start + 1 hour)
    
    public DateTime? EndDateTime { get; init; }
}


/// Request DTO for completing a booking via QR scan

public record BookingCompleteRequest
{
    
    /// QR payload string for verification
    
    [Required(ErrorMessage = "QR payload is required")]
    public string QRPayload { get; init; } = string.Empty;
}