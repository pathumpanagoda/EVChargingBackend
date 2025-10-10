/*
 * Author: EV Charging System
 * Date: 2024-12-19
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

    
    /// Date and time for the charging reservation
    
    [Required(ErrorMessage = "Reservation date and time is required")]
    public DateTime ReservationDateTime { get; init; }
}


/// Request DTO for updating an existing booking

public record BookingUpdateRequest
{
    
    /// New date and time for the charging reservation
    
    [Required(ErrorMessage = "Reservation date and time is required")]
    public DateTime ReservationDateTime { get; init; }
}


/// Request DTO for completing a booking via QR scan

public record BookingCompleteRequest
{
    
    /// QR payload string for verification
    
    [Required(ErrorMessage = "QR payload is required")]
    public string QRPayload { get; init; } = string.Empty;
}