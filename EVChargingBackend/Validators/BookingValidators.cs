/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: FluentValidation validators for Booking DTOs
 */

using EVChargingBackend.DTOs;
using FluentValidation;

namespace EVChargingBackend.Validators;


/// Validator for BookingRequest DTO

public class BookingRequestValidator : AbstractValidator<BookingRequest>
{
    
    /// Initializes a new instance of the BookingRequestValidator
    
    public BookingRequestValidator()
    {
        RuleFor(x => x.StationId)
            .NotEmpty().WithMessage("Station ID is required");

        RuleFor(x => x.ReservationDateTime)
            .NotEmpty().WithMessage("Reservation date and time is required")
            .GreaterThan(DateTime.UtcNow).WithMessage("Reservation time must be in the future")
            .Must(BeWithinSevenDays).WithMessage("Reservation must be within 7 days from booking date");
    }

    
    /// Validates that the reservation date is within 7 days
    
    /// <param name="reservationDateTime">Reservation date and time</param>
    /// <returns>True if within 7 days, false otherwise</returns>
    private static bool BeWithinSevenDays(DateTime reservationDateTime)
    {
        var sevenDaysFromNow = DateTime.UtcNow.AddDays(7);
        return reservationDateTime <= sevenDaysFromNow;
    }
}


/// Validator for BookingUpdateRequest DTO

public class BookingUpdateRequestValidator : AbstractValidator<BookingUpdateRequest>
{
    
    /// Initializes a new instance of the BookingUpdateRequestValidator
    
    public BookingUpdateRequestValidator()
    {
        RuleFor(x => x.ReservationDateTime)
            .NotEmpty().WithMessage("Reservation date and time is required")
            .GreaterThan(DateTime.UtcNow).WithMessage("Reservation time must be in the future")
            .Must(BeWithinSevenDays).WithMessage("Reservation must be within 7 days from booking date");
    }

    
    /// Validates that the reservation date is within 7 days
    
    /// <param name="reservationDateTime">Reservation date and time</param>
    /// <returns>True if within 7 days, false otherwise</returns>
    private static bool BeWithinSevenDays(DateTime reservationDateTime)
    {
        var sevenDaysFromNow = DateTime.UtcNow.AddDays(7);
        return reservationDateTime <= sevenDaysFromNow;
    }
}


/// Validator for BookingCompleteRequest DTO

public class BookingCompleteRequestValidator : AbstractValidator<BookingCompleteRequest>
{
    
    /// Initializes a new instance of the BookingCompleteRequestValidator
    
    public BookingCompleteRequestValidator()
    {
        RuleFor(x => x.QRPayload)
            .NotEmpty().WithMessage("QR payload is required")
            .Must(BeValidQRPayload).WithMessage("Invalid QR payload format");
    }

    
    /// Validates that the QR payload has the correct format
    
    /// <param name="qrPayload">QR payload string</param>
    /// <returns>True if valid format, false otherwise</returns>
    private static bool BeValidQRPayload(string qrPayload)
    {
        if (string.IsNullOrEmpty(qrPayload))
            return false;

        var parts = qrPayload.Split(':');
        return parts.Length == 5 && parts[0] == "BOOKING";
    }
}
